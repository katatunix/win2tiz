using System;
using System.Net.Sockets;

using libcore;

namespace libmongcc
{
	class SocketStream : IStream
	{
		public SocketStream(Socket socket)
		{
			m_socket = socket;
		}

		public int read(byte[] buffer, int offset, int count)
		{
			if (m_socket == null) return 0;

			int len = 0;
			try
			{
				len = m_socket.Receive(buffer, offset, count, SocketFlags.None);
			}
			catch (SocketException ex)
			{
				if (ex.SocketErrorCode == SocketError.WouldBlock ||
					ex.SocketErrorCode == SocketError.IOPending ||
					ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
				{
					// Socket buffer is probably empty, wait and try again
					len = -1;
				}
				else
				{
					len = 0;
				}
			}
			catch (Exception)
			{
				len = 0;
			}
			return len;
		}

		public int write(byte[] buffer, int offset, int count)
		{
			if (m_socket == null) return 0;

			int len = 0;
			try
			{
				len = m_socket.Send(buffer, offset, count, SocketFlags.None);
			}
			catch (Exception)
			{
				len = 0;
			}
			return len;
		}

		public void close()
		{
			if (m_socket == null) return;
			try
			{
				m_socket.Shutdown(SocketShutdown.Both);
				m_socket.Close();
			}
			catch (Exception)
			{
			}
			m_socket = null;
		}

		private Socket m_socket;
	}
}
