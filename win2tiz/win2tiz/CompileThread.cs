using System;
using System.Threading;

using libcore;
using libmongcc;

namespace win2tiz
{
	class CompileThread
	{
		public CompileThread(Agent agent)
		{
			m_agent = agent;
			m_notifier = null;

			m_lockRunning = new Object();
			m_running = false;

			m_lockForcingStop = new Object();
			m_forcingStop = false;

			m_threadStart = new ThreadStart(callback);
			m_thread = null;
		}

		public bool start(ICommandPool commandPool, ICompileNotifier notifier)
		{
			if (isRunning()) return false;

			setRunning(true);

			m_forcingStop = false;
			m_commandPool = commandPool;
			m_notifier = notifier;

			m_thread = new Thread(m_threadStart);
			m_thread.Start();

			return true;
		}

		public void signalToStop()
		{
			setForcingStop(true);
		}

		public void join()
		{
			if (m_thread != null)
			{
				m_thread.Join();
			}
		}

		//========================================================================================

		private void callback()
		{
			TCommand cmd;
			while ((cmd = m_commandPool.getNextCommand()) != null)
			{
				TCompileResult res = m_agent.compile(s_kSID, cmd.command, cmd.workingDir);

				if (m_notifier != null)
				{
					m_notifier.onFinishCompile(cmd, res);
				}

				if (isForcingStop()) break;
			}

			setRunning(false);
		}

		private void setRunning(bool v)
		{
			lock (m_lockRunning)
			{
				m_running = v;
			}
		}

		public bool isRunning()
		{
			lock (m_lockRunning)
			{
				return m_running;
			}
		}

		private void setForcingStop(bool v)
		{
			lock (m_lockForcingStop)
			{
				m_forcingStop = v;
			}
		}

		private bool isForcingStop()
		{
			lock (m_lockForcingStop)
			{
				return m_forcingStop;
			}
		}

		//=====================================================================================

		private Thread m_thread;
		private ThreadStart m_threadStart;

		private Object m_lockRunning;
		private bool m_running;

		private Object m_lockForcingStop;
		private bool m_forcingStop;

		private ICompileNotifier m_notifier;

		private Agent m_agent;
		private ICommandPool m_commandPool;

		private static readonly int s_kSID = 1;
	}
}
