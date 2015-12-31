using System;
using System.IO.Pipes;
using System.Threading;

using libcore;
using libmongcc.message;

namespace libmongcc
{
	public class Mongcc
	{
		public Mongcc()
		{
			m_sid = 0;
			m_server = null;
			m_agentHandlers = null;
			m_compiling = false;
			m_lockCompiling = new Object();
		}
		
		/// <summary>
		/// Make sure this method is thread-safety
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public int main(string[] args)
		{
			bool result = true;

			if (args.Length < 1)
			{
				printAbout();
				printUsage();
			}
			else if (args[0] == "--server")
			{
				printAbout();
				result = startServer(args);
			}
			else if (args[0] == "--agent")
			{
				printAbout();
				result = startAgent();
			}
			else
			{
				int sid = 0;
				if (args[0] == "-sid" && args.Length > 1)
				{
					try { sid = int.Parse(args[1]); }
					catch (Exception) { sid = 0; }
				}
				result = compile(sid, StringUtils.concat(args, " "), SystemUtils.getWorkingDir());
			}

			return result ? 0 : 1;
		}

		public void signalToStop()
		{
			if (m_server != null)
			{
				m_server.signalToStop();
			}

			if (m_agentHandlers != null)
			{
				for (int i = 0; i < m_agentHandlers.Length; i++)
				{
					m_agentHandlers[i].signalToStop();
				}
			}

			if (getCompiling())
			{
				signalToStopCompiling();
			}
		}

		//========================================================================================

		/// <summary>
		/// mongcc.exe --server -port <num> -backlog <num> -tempdir e:\x
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		private bool startServer(string[] args)
		{
			// Default values
			int k_port			= Config.s_kMongccPort;
			int k_backlog		= Config.s_kMongccBacklog;
			string k_tempdir	= ".";

			int port			= k_port;
			int backlog			= k_backlog;
			string tempdir		= k_tempdir;

			for (int i = 1; i < args.Length; i++)
			{
				if (args[i] == "-port")
				{
					if (i + 1 >= args.Length) break;
					port = StringUtils.convertString2Int(args[i + 1]);
					i++;
				}
				else if (args[i] == "-backlog")
				{
					if (i + 1 >= args.Length) break;
					backlog = StringUtils.convertString2Int(args[i + 1]);
					i++;
				}
				else if (args[i] == "-tempdir")
				{
					if (i + 1 >= args.Length) break;
					tempdir = args[i + 1];
					i++;
				}
			}

			if (port <= 0) port = k_port;
			if (backlog <= 0 || backlog > k_backlog) backlog = k_backlog;

			m_server = new Server();
			return m_server.run(port, backlog, tempdir);
		}

		/// <summary>
		/// mongcc.exe --agent
		/// </summary>
		/// <returns></returns>
		private bool startAgent()
		{
			// Make sure we have only ONE instance of agent process (mongcc.exe --agent)
			NamedPipeServerStream singlePipe;
			try
			{
				singlePipe = new NamedPipeServerStream("MONGCC_SINGLE_PIPE", PipeDirection.InOut, 1);
			}
			catch (Exception)
			{
				CConsole.writeInfoLine("The agent was already started.");
				return false;
			}
			
			if (isAgentRunning())
			{
				CConsole.writeInfoLine("The agent was already started.");
				return false;
			}

			CConsole.writeInfoLine("Starting agent...");

			Agent agent = new Agent();
			
			m_agentHandlers = new AgentHandler[Config.s_kMaxAgentHandlers];
			for (int i = 0; i < m_agentHandlers.Length; i++)
			{
				m_agentHandlers[i] = new AgentHandler(agent);
			}

			CConsole.writeInfoLine("Agent started OK!");

			for (int i = 0; i < m_agentHandlers.Length; i++)
			{
				m_agentHandlers[i].join();
			}

			// Only reach here when the agent handlers were signaled to stop
			// So we can make sure all the agent.compile() were finished

			agent.signalToStopAllSessions(); // just disconnect all remotes' connections

			singlePipe.Close();

			CConsole.writeInfoLine("Agent stopped!");

			return true;
		}

		private bool compile(int sid, string cmd, string workingDir)
		{
			if (!isAgentRunning())
			{
				string processPath = SystemUtils.getCurrentProcessPath();
				if (!ProcessHelper.justExecuteStatic(processPath + " --agent", SystemUtils.getWorkingDir()))
				{
					CConsole.writeError("error: could not start the mongcc agent.\n");
					return false;
				}

				Thread.Sleep(s_kWaitingForAgentStartTime); // note: is it enough?
			}

			// Check again to make sure the agent is running
			if (!isAgentRunning())
			{
				CConsole.writeError("error: could not start the mongcc agent.\n");
				return false;
			}

			// Connect to the agent and send data
			NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", Config.s_kAgentName);
			try
			{
				pipeClient.Connect(s_kPipeConnectTimeout);
			}
			catch (Exception)
			{
				CConsole.writeError("error: could not connect to the mongcc agent.\n");
				return false;
			}

			if (!pipeClient.IsConnected)
			{
				CConsole.writeError("error: could not connect to the mongcc agent.\n");
				return false;
			}

			bool success = false;

			m_sid = sid == 0 ? SystemUtils.getParentProcessId() : sid;

			MessageStream stream = new MessageStream(new StandardStream(pipeClient));
			Message msg = MessagePidAndCompileRequest.createMessage(m_sid, cmd, workingDir);

			if (!stream.writeMessage(msg))
			{
				CConsole.writeError("error: could not send data to the mongcc agent.\n");
				goto my_end;
			}

			setCompiling(true);

			msg = stream.readMessage(); //============= take long time here
			if (msg == null)
			{
				CConsole.writeError("error: could not receive data from the mongcc agent.\n");
				goto my_end;
			}
			MessageCompileResponse res = new MessageCompileResponse(msg);
			success = res.getWasExec() && res.getExitCode() == 0;
			if (success)
				CConsole.writeInfo(res.getOutputText() + "\n");
			else
				CConsole.writeError(res.getOutputText() + "\n");

			my_end:

			pipeClient.Close();

			#region temp
			//Console.WriteLine("OK");
			//Console.ReadLine();
			#endregion

			return success;
		}

		private void signalToStopCompiling()
		{
			if (m_sid == 0) return;
			// Connect to the agent and send signal to stop
			NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", Config.s_kAgentName);
			try
			{
				pipeClient.Connect(s_kPipeConnectTimeout);
			}
			catch (Exception)
			{
				return;
			}
			MessageStream stream = new MessageStream(new StandardStream(pipeClient));
			Message msg = MessageNumber.createMessage(m_sid);
			if (!stream.writeMessage(msg))
			{
				CConsole.writeError("error: could not send data to the mongcc agent.\n");
			}
			pipeClient.Close();
		}

		private bool isAgentRunning()
		{
			return IOUtils.namedPipeExist(Config.s_kAgentName);
		}
		
		private bool getCompiling()
		{
			lock (m_lockCompiling)
			{
				return m_compiling;
			}
		}

		private void setCompiling(bool v)
		{
			lock (m_lockCompiling)
			{
				m_compiling = v;
			}
		}
		
		private static void printUsage()
		{
			CConsole.setColor(EConsoleColor.eGreen);

			CConsole.writeLine("Client usage: mongcc.exe <compile command>");

			CConsole.writeLine();
			CConsole.writeLine("Server usage: mongcc.exe --server [-port <num>] [-backlog <num>] [-tempdir <str>]");
			CConsole.writeLine("  -port      port number to listen, default: " + Config.s_kMongccPort);
			CConsole.writeLine("  -backlog   max number of clients that server can handle at the same time, default: " + Config.s_kMongccBacklog);
			CConsole.writeLine("  -tempdir   path to the folder that server can write temporary data, this should be short, default: current");
		}

		private static void printAbout()
		{
			CConsole.setColor(EConsoleColor.eWhite);
			CConsole.writeLine();
			CConsole.writeLine("mongcc (c) katatunix@gmail.com, Summer 2014 - FIFA World Cup Brazil");
			CConsole.writeLine("(Since Spring 2013)");
			CConsole.writeLine();
		}

		//========================================================================================

		private int m_sid;
		private Server m_server;
		private AgentHandler[] m_agentHandlers;
		private bool m_compiling;
		private Object m_lockCompiling;

		//========================================================================================

		private static readonly int s_kPipeConnectTimeout = 3000;
		private static readonly int s_kWaitingForAgentStartTime = 2000;
	}
}
