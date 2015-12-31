using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.IO;

using libcore;
using libmongcc.message;
using libmongcc.cmdparser;

namespace libmongcc
{
	class ServerHandler
	{
		public ServerHandler(IServer server)
		{
			m_server = server;

			m_isRunning = false;
			m_threadStart = new ThreadStart(threadHandle);
			m_thread = null;

			m_processCompile = new ProcessHelper();
			m_messageStream = new MessageStream();

			m_listRoot = new List<string>();
			m_lock = new Object();
		}

		/// <summary>
		/// Make sure isRunning() == false
		/// Make sure this method is thread-safety
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="tempdir"></param>
		/// <returns></returns>
		public bool runThread(Socket socket, string tempdir)
		{
			if (isRunning()) return false;

			setRunning(true);

			socket.ReceiveTimeout = s_kTimeout;
			m_clientId = socket.RemoteEndPoint.ToString() + "_" + getNewSessionId();
			m_messageStream.setStream(new SocketStream(socket));

			m_tempdir = tempdir;

			m_thread = new Thread(m_threadStart);
			m_thread.Start();

			return true;
		}

		public bool isRunning()
		{
			lock (m_lock)
			{
				return m_isRunning;
			}
		}

		public void signalToStop()
		{
			m_processCompile.kill();
			m_messageStream.close();
		}

		public void join()
		{
			if (m_thread != null)
			{
				m_thread.Join();
			}
		}

		//===========================================================================================
		//===========================================================================================

		private void setRunning(bool v)
		{
			lock (m_lock)
			{
				m_isRunning = v;
			}
		}

		private void threadHandle()
		{
			try
			{
				threadHandle2();
			}
			catch (Exception ex)
			{
				//SystemUtils.sendEmail("katatunix@gmail.com", "Nghia Bui Van", xyz(),
				//	"katatunix@gmail.com",
				//	"Exception report from mongcc",
				//	ex.Message + "\n" + ex.Data + "\n" + ex.StackTrace + "\n" + ex.Source);
				File.AppendAllText(
					@"\\sa2wks0151\ex_log\mongcc_log.txt",
					//@".\mongcc_log.txt",
					"*****************************************************************\n" +
					DateTime.Now + "\n" +
					ex.Message + "\n" + ex.Data + "\n" + ex.StackTrace + "\n" + ex.Source + "\n\n");

				signalToStop();
				deleteSessionFolder();
				setRunning(false);
			}
		}

		private void threadHandle2()
		{
			m_listRoot.Clear();
			m_debugPrefixMap = "";

			m_sessionFolderPath = Path.GetFullPath(m_tempdir + "\\" + PathUtils.makeValidPath(m_clientId)) + "\\";
			deleteSessionFolder();

			while (true)
			{
				CConsole.writeInfoLine(string.Format("{0} Waiting for a message...", cur()));

				Message msg = m_messageStream.readMessage();
				if (msg == null) // disconnected
				{
					break;
				}

				switch ((EMessageType)msg.getType())
				{
					case EMessageType.eFreeHandlerNumberRequest:
					{
						handleMessageFreeNum();
						break;
					}

					case EMessageType.eFile:
					{
						handleMessageFile(msg);
						break;
					}

					case EMessageType.eCompileRequest:
					{
						handleMessageCompile(msg);
						break;
					}
				}
			}

			// Finish the session
			m_messageStream.close();

			deleteSessionFolder();

			CConsole.writeInfoLine(string.Format("{0} Disconnected!", cur()));

			// Now we are free
			setRunning(false);
		}

		private void deleteSessionFolder()
		{
			if (!Directory.Exists(m_sessionFolderPath)) return;
			try
			{
				IOUtils.deleteFolder(m_sessionFolderPath);
			}
			catch (Exception)
			{
				CConsole.writeInfoLine(string.Format("{0} warning: could not delete the temp dir {1}",
					cur(), m_sessionFolderPath));
			}
		}

		private void handleMessageFreeNum()
		{
			int num = m_server.getFreeHandlerNumber();
			CConsole.writeInfoLine(string.Format("{0} Recv free num request, send response = {1}", cur(), num));
			Message msg = MessageNumber.createMessage(num);
			m_messageStream.writeMessage(msg);
		}

		private void handleMessageFile(Message msg)
		{
			MessageFile msgFile = null;
			try
			{
				msgFile = new MessageFile(msg);
			}
			catch (Exception)
			{
				// Ignore
				return;
			}

			string filePath = msgFile.getFilePath();
			int fileSize = msgFile.getFileSize();
			CConsole.writeInfoLine(string.Format("{0} Recv file {1}|{2}", cur(), filePath, fileSize));

			string saveFilePath = m_sessionFolderPath + PathUtils.makeValidPath(filePath);
			string saveFolderPath = Path.GetDirectoryName(saveFilePath);
			if (!Directory.Exists(saveFolderPath))
			{
				Directory.CreateDirectory(saveFolderPath);
			}

			IOUtils.writeFile_Bytes(saveFilePath, msgFile.getData(), msgFile.getOffset(), fileSize);

			string clientRoot = Path.GetPathRoot(filePath);
			if (!m_listRoot.Contains(clientRoot))
			{
				m_listRoot.Add(clientRoot);

				string serverRoot = m_sessionFolderPath + PathUtils.makeValidPath(clientRoot);
				if (m_debugPrefixMap.Length > 0)
				{
					m_debugPrefixMap += " ";
				}
				
				m_debugPrefixMap += GccCmdParser.makeDebugPrefixMapString(serverRoot, clientRoot);
			}
		}

		private void handleMessageCompile(Message msg)
		{
			MessageCompileRequest msgCompile = null;
			try
			{
				msgCompile = new MessageCompileRequest(msg);
			}
			catch (Exception)
			{
				// Ignore
				return;
			}

			string cmd = msgCompile.getCmd();

			ICmdParser parser = CmdParserFactory.createCmdParser(cmd);

			string outputFileName;
			cmd = parser.makeServerCmd(m_sessionFolderPath, out outputFileName, m_debugPrefixMap);
			string alias = parser.getAlias();

			Message respondMessage = null;
			bool ok = false;

			if (alias != null && outputFileName != null)
			{
				// Compile
				CConsole.writeInfoLine(string.Format("{0} Recv a compile request: {1}", cur(), alias));

				string outputFilePath = m_sessionFolderPath + outputFileName;

				string workingDir = parser.getLocalSpecificWorkingDir();
				if (workingDir == null) workingDir = m_sessionFolderPath;
				
				TProcessResult pr = m_processCompile.execute(cmd, workingDir, outputFilePath);

				if (!pr.wasExec || pr.exitCode != 0)
				{
					CConsole.writeInfoLine(
						string.Format("{0} Compile error: wasExec=[{1}], exitCode=[{2}], cmd=[{3}], outputText=[{4}]",
							cur(), pr.wasExec, pr.exitCode, cmd, pr.outputText)
					);
					respondMessage = MessageCompileResponse.createMessage(pr.wasExec, pr.exitCode, pr.outputText, null, 0);
				}
				else
				{
					ok = true;
					// Read the output file from disk
					byte[] buffer = IOUtils.readFile_Bytes(outputFilePath);
					int fileSize = buffer == null ? 0 : buffer.Length;
					respondMessage = MessageCompileResponse.createMessage(
						pr.wasExec, pr.exitCode, pr.outputText, buffer, fileSize);
				}
			}
			else
			{
				CConsole.writeInfoLine(string.Format("{0} Receive a compile request but it is a invalid command", cur()));
				respondMessage = MessageCompileResponse.createMessage(false, 0, "error: Invalid compile command!", null, 0);
			}

			CConsole.writeInfoLine(string.Format("{0} Send compile result: {1}, success = {2}",
				cur(), alias != null ? alias : "[no file]", ok));

			m_messageStream.writeMessage(respondMessage);
		}

		//===========================================================================================
		//===========================================================================================

		private string cur()
		{
			return string.Format("[{0} {1}]", m_clientId, DateTime.Now);
		}
		
		private static int getNewSessionId()
		{
			lock (s_lockSession)
			{
				return ++s_sessionId;
			}
		}

		//===========================================================================================
		//===========================================================================================

		private static readonly int s_kTimeout = 1800000;

		private static int s_sessionId = 0;
		private static Object s_lockSession = new Object();

		//===========================================================================================
		//===========================================================================================

		private MessageStream m_messageStream;
		private string m_clientId;
		private string m_sessionFolderPath;

		private ThreadStart m_threadStart;
		private Thread m_thread;
		private bool m_isRunning;
		private string m_tempdir;
		private IServer m_server;

		private Object m_lock;

		private ProcessHelper m_processCompile;

		private List<string> m_listRoot;
		private string m_debugPrefixMap;
	}
}
