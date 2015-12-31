using System;
using System.Timers;

using libcore;

namespace libmongcc
{
	class Session
	{
		public Session(int sid, bool autoStop)
		{
			m_sid = sid;

			m_config = Config.getConfig();

			int numRemotes = m_config.maxJobs;
			if (m_config.enable && m_config.hosts != null
				&& m_config.hosts.Length > 0 && m_config.hosts.Length > numRemotes)
			{
				numRemotes = m_config.hosts.Length;
			}

			m_remotes = new Remote[numRemotes];

			int i = 0;
			if (m_config.enable && m_config.hosts != null && m_config.hosts.Length > 0)
			{
				for (; i < m_remotes.Length && i < m_config.hosts.Length; i++)
				{
					m_remotes[i] = new Remote(m_config.hosts[i], m_config.port, m_config.timeout);
				}
			}

			for (; i < m_remotes.Length; i++)
			{
				m_remotes[i] = new Remote(null, 0, 0); // for local compiling
			}

			m_isAllowCompiling = true;
			m_lockAllowCompiling = new Object();

			m_lockSelectBestRemote = new Object();
			m_lockLast = new Object();

			setLast();

			if (autoStop)
			{
				m_timer = new Timer(s_kAutoStopTimerInterval);
				m_timer.Enabled = true;
				m_timer.Elapsed += timer_Elapsed;
				m_timer.Start();
			}
			else
			{
				m_timer = null;
			}
		}

		public int getSID()
		{
			return m_sid;
		}

		/// <summary>
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="workingDir"></param>
		/// <returns></returns>
		public TCompileResult compile(string cmd, string workingDir)
		{
			TCompileResult res = null;

			if (!isAllowCompiling())
			{
				res = new TCompileResult(false, 1, "error: Compiling is stopped!", 0, null);
			}

			setLast(0);

			Remote remote = selectBestRemote();
			if (remote == null)
			{
				res = new TCompileResult(false, 1, "error: Overloading, current MONGCC_MAX_JOBS=" + m_config.maxJobs
					+ ", but your own jobs is greater!", 0, null);
			}
			else
			{
				res = remote.compile(cmd, workingDir);
				if (m_config.enable && (m_config.hosts == null || m_config.hosts.Length == 0))
				{
					res.outputText = "warning: MONGCC_HOSTS is empty!\n" + res.outputText;
				}
			}

			setLast();
			return res;
		}

		/// <summary>
		/// After this method, this session cannot be used anymore
		/// </summary>
		public void signalToStopAndEndSession()
		{
			setAllowCompiling(false);
			if (m_timer != null)
			{
				m_timer.Enabled = false;
			}
			foreach (Remote remote in m_remotes)
			{
				remote.signalToStopAndDisconnect();
			}
		}

		public bool isAllowCompiling()
		{
			lock (m_lockAllowCompiling)
			{
				return m_isAllowCompiling;
			}
		}

		//============================================================================================
		private Remote selectBestRemote()
		{
			lock (m_lockSelectBestRemote)
			{
				Remote best = null;

				// Check full jobs
				int countCompiling = 0;
				foreach (Remote remote in m_remotes)
				{
					if (remote.isCompiling())
					{
						countCompiling++;
					}
				}
				if (countCompiling >= m_config.maxJobs)
				{
					goto my_end;
				}

				// Get a connected remote
				//int countConnected = 0;
				foreach (Remote remote in m_remotes)
				{
					if (remote.isConnected())
					{
						//countConnected++;
						if (!remote.isCompiling())
						{
							best = remote;
							goto my_end;
						}
					}
				}

				// Connect to other remote
				int[] orders = new int[m_remotes.Length];
				MathUtils.shakeOrder(orders);

				for (int i = 0; i < orders.Length; i++)
				{
					Remote remote = m_remotes[orders[i]];
					if (remote.canConnect()
						&& !remote.isConnected() && !remote.isCompiling()
						&& SystemUtils.getCurrentTimeMs() - remote.getLastConnectTimeMs() >= m_config.retryConnectTime
						&& remote.connect())
					{
						best = remote;
						goto my_end;
					}
				}

				// Try local
				foreach (Remote remote in m_remotes)
				{
					if (!remote.isConnected() && !remote.isCompiling())
					{
						best = remote;
						goto my_end;
					}
				}

				my_end:

				if (best != null)
				{
					best.setCompilingTrue();
				}
				return best;
			}
		}

		private void setLast(int v = -1)
		{
			lock (m_lockLast)
			{
				m_lastActiveTime = v == -1 ? SystemUtils.getCurrentTimeMs(): v;
			}
		}

		private int getLast()
		{
			lock (m_lockLast)
			{
				return m_lastActiveTime;
			}
		}

		private void timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			int sid = getSID();
			if (sid == 0) return;
			int last = getLast();
			if (!SystemUtils.isProcessRunning(sid)
				|| (last != 0 && SystemUtils.getCurrentTimeMs() - last > s_kAutoStopTimerInterval))
			{
				signalToStopAndEndSession();
			}
		}

		private void setAllowCompiling(bool v)
		{
			lock (m_lockAllowCompiling)
			{
				m_isAllowCompiling = v;
			}
		}

		//============================================================================================
		private readonly int m_sid;

		private Remote[] m_remotes;
		private TConfig m_config;

		private Timer m_timer;
		private int m_lastActiveTime;

		private Object m_lockSelectBestRemote;
		private Object m_lockLast;

		private bool m_isAllowCompiling;
		private Object m_lockAllowCompiling;

		private static readonly int s_kAutoStopTimerInterval = 30000;
	}
}
