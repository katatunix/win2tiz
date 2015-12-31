using System;
using System.Collections.Generic;

using libcore;

namespace libmongcc
{
	public class Agent
	{
		public Agent(bool autoStopSession = true)
		{
			m_sessions = new List<Session>();
			m_autoStopSession = autoStopSession;
			m_lock = new Object();
		}

		public TCompileResult compile(int sid, string cmd, string workingDir)
		{
			Session session = null;

			lock (m_lock)
			{
				removeUnusedSessions();
				session = getSessionById(sid);
				if (session == null)
				{
					session = new Session(sid, m_autoStopSession);
					m_sessions.Add(session);
				}
			}

			return session.compile(cmd, workingDir);
		}

		public void signalToStopSession(int sid)
		{
			Session session = null;
			lock (m_lock)
			{
				removeUnusedSessions();
				session = getSessionById(sid);
				if (session == null) return;
			}
			session.signalToStopAndEndSession();
		}

		public void signalToStopAllSessions()
		{
			lock (m_lock)
			{
				removeUnusedSessions();
				foreach (Session session in m_sessions)
				{
					session.signalToStopAndEndSession();
				}
			}
		}

		private Session getSessionById(int sid)
		{
			foreach (Session s in m_sessions)
			{
				if (s.getSID() == sid)
				{
					return s;
				}
			}
			return null;
		}

		private void removeUnusedSessions()
		{
			for (int i = m_sessions.Count - 1; i >= 0; i--)
			{
				Session s = m_sessions[i];
				if (!s.isAllowCompiling())
				{
					m_sessions.Remove(s);
				}
			}
		}

		//===============================================================================
		private List<Session> m_sessions;
		private bool m_autoStopSession;

		private Object m_lock;
    }
}
