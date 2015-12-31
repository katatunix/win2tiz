using System;
using System.Collections.Generic;
using System.Threading;

using libcore;
using libmongcc;

namespace win2tiz
{
	class BatchingCompiler : ICommandPool
	{
		public BatchingCompiler()
		{
			m_lockRunning = new Object();
			m_running = false;

			m_agent = new Agent(false);

			m_compileThreads = new CompileThread[s_kMaxJobs];
			for (int i = 0; i < m_compileThreads.Length; i++)
			{
				m_compileThreads[i] = new CompileThread(m_agent);
			}

			m_jobs = 0;
			m_curCmdIdx = 0;
			m_commands = null;
			m_lockCommand = new Object();
		}

		public bool compile(List<TCommand> commands, int jobs, ICompileNotifier notifier)
		{
			if (isRunning()) return false;
			setRunning(true);

			m_curCmdIdx = 0;
			m_commands = commands;

			m_jobs = jobs;
			if (m_jobs < 1) m_jobs = 1;
			if (m_jobs > s_kMaxJobs) m_jobs = s_kMaxJobs;

			for (int i = 0; i < jobs; i++)
			{
				m_compileThreads[i].start(this, notifier);
			}

			for (int i = 0; i < jobs; i++)
			{
				m_compileThreads[i].join();
			}

			m_agent.signalToStopAllSessions(); // just disconnect all remotes' connections

			setRunning(false);
			return true;
		}

		public void signalToStop(bool isImmediate)
		{
			for (int i = 0; i < m_jobs; i++)
			{
				m_compileThreads[i].signalToStop();
			}
			if (isImmediate)
			{
				m_agent.signalToStopAllSessions();
			}
		}

		//=======================================================================================

		private bool isRunning()
		{
			lock (m_lockRunning)
			{
				return m_running;
			}
		}

		private void setRunning(bool r)
		{
			lock (m_lockRunning)
			{
				m_running = r;
			}
		}
		
		//=======================================================================================

		public TCommand getNextCommand()
		{
			lock (m_lockCommand)
			{
				if (m_curCmdIdx >= m_commands.Count) return null;

				while (m_curCmdIdx < m_commands.Count && m_commands[m_curCmdIdx].type != ECommandType.eCompile)
				{
					m_curCmdIdx++;
				}

				if (m_curCmdIdx >= m_commands.Count) return null;
				return m_commands[m_curCmdIdx++];
			}
		}

		//=======================================================================================

		private static readonly int s_kMaxJobs = 8;

		//=======================================================================================

		private CompileThread[] m_compileThreads;

		private Object m_lockRunning;
		private bool m_running;

		private int m_jobs;
		private Agent m_agent;

		private int m_curCmdIdx;
		private List<TCommand> m_commands;
		private Object m_lockCommand;
	}
}
