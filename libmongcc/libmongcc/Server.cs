using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using libcore;

namespace libmongcc
{
	class Server : IServer
	{
		public Server()
		{
			m_handers = null;
			m_running = false;
			m_listener = null;
		}

		public bool run(int port, int backlog, string tempdir)
		{
			if (isRunning()) return true;

			if (backlog <= 0 || backlog > s_kMaxHandlers) backlog = s_kMaxHandlers;

			CConsole.writeInfo("Start mongcc server...\n");

			IPAddress ipAd = IPAddress.Any;
			m_listener = new TcpListener(ipAd, port);
			try
			{
				m_listener.Start(backlog);
			}
			catch (Exception ex)
			{
				CConsole.writeError("Could not start the server: " + ex.Message);
				return false;
			}

			setRunning(true);

			CConsole.writeInfo("Clear the tempdir " + tempdir + " ...\n");
			IOUtils.clearTempFolder(tempdir);

			m_handers = new ServerHandler[backlog];
			for (int i = 0; i < m_handers.Length; i++)
			{
				m_handers[i] = new ServerHandler(this);
			}

			CConsole.writeInfoLine("The local end point is " + m_listener.LocalEndpoint + "\n");

			int lastTick = SystemUtils.getCurrentTimeMs();

			while (true)
			{
				CConsole.writeInfoLine("Waiting for a connection...");

				Socket socket = null;
				try
				{
					socket = m_listener.AcceptSocket();
				}
				catch (Exception)
				{
					break;
				}

				CConsole.writeInfoLine("Accept a connection from: " + socket.RemoteEndPoint);

				int slot = -1;
				for (int i = 0; i < m_handers.Length; i++)
				{
					if (!m_handers[i].isRunning())
					{
						slot = i;
						break;
					}
				}

				if (slot == -1)
				{
					CConsole.writeInfoLine("Full slot! Could not handle more connection!");
					byte[] tmp = new byte[1];
					tmp[0] = 0;
					socket.Send(tmp, 0, 1, SocketFlags.None);
					socket.Shutdown(SocketShutdown.Both);
					socket.Close();
				}
				else
				{
					byte[] tmp = new byte[1];
					tmp[0] = 1;
					socket.Send(tmp, 0, 1, SocketFlags.None);

					m_handers[slot].runThread(socket, tempdir);
				}

				int TIME_OUT_DELETE = 6 * 60 * 60 * 1000;
				if (SystemUtils.getCurrentTimeMs() - lastTick >= TIME_OUT_DELETE) {
					IOUtils.clearOldTempFolder(tempdir, TIME_OUT_DELETE);
					lastTick = SystemUtils.getCurrentTimeMs();
				}
			}

			CConsole.writeWarning("Waiting for all handlers finish...\n");
			for (int i = 0; i < m_handers.Length; i++)
			{
				if (m_handers[i].isRunning())
				{
					m_handers[i].join();
				}
			}

			CConsole.writeWarning("Server is stopped!\n");
			setRunning(false);

			return true;
		}

		public void signalToStop()
		{
			if (m_listener != null)
			{
				CConsole.writeWarning("Stopping server can take a long time, please be patient...\n");
				for (int i = 0; i < m_handers.Length; i++)
				{
					if (m_handers[i].isRunning())
					{
						m_handers[i].signalToStop();
					}
				}
				m_listener.Stop();
			}
		}

		public int getFreeHandlerNumber()
		{
			if (m_handers == null || !isRunning()) return 0;
			int num = 0;
			for (int i = 0; i < m_handers.Length; i++)
			{
				if (!m_handers[i].isRunning())
				{
					num++;
				}
			}
			return num;
		}

		//==========================================================================
		private bool isRunning()
		{
			lock (m_lock)
			{
				return m_running;
			}
		}

		private void setRunning(bool r)
		{
			lock (m_lock)
			{
				m_running = r;
			}
		}

		//==========================================================================
		private static readonly int s_kMaxHandlers = 8;

		//==========================================================================
		private ServerHandler[] m_handers;
		private bool m_running;
		private Object m_lock = new Object();
		private TcpListener m_listener;
	}
}
