using System;
using System.Threading;

namespace libcore
{
	public class MessageStream
	{
		public MessageStream() : this(null)
		{
			
		}

		public MessageStream(IStream stream)
		{
			m_stream = stream;

			m_readBuffer = new byte[s_kReadBufferSize];
			m_readOffset = 0;
			m_readLen = 0;

			m_lock = new Object();
		}

		public void setStream(IStream stream)
		{
			lock (m_lock)
			{
				m_stream = stream;
			}
		}

		public bool isOpenned()
		{
			return getStream() != null;
		}

		public Message readMessage()
		{
			IStream stream = getStream();
			if (stream == null) return null;

			Message msg = new Message();

			while (true)
			{
				while (m_readLen > 0)
				{
					int k = msg.consume(m_readBuffer, m_readOffset, m_readOffset + m_readLen - 1);
					//SystemUtils.assert(k > 0);
					if (k <= 0)
					{
						close();
						return null;
					}
					m_readOffset += k;
					m_readLen -= k;

					if (msg.isFull()) return msg;
				}

				// Now m_readLen == 0
				m_readOffset = 0;

				const int k_maxRetryCount = 10;
				int retryCount = 0;
				
				while (true)
				{
					m_readLen = stream.read(m_readBuffer, 0, m_readBuffer.Length);
					if (m_readLen == -1 && retryCount < k_maxRetryCount)
					{
						retryCount++;
						Thread.Sleep(100);
						continue;
					}
					
					if (m_readLen <= 0)
					{
						close();
						return null;
					}

					// m_readLen > 0, now consume it
					break;
				}
			}
		}

		public bool writeMessage(Message msg)
		{
			IStream stream = getStream();
			if (stream == null) return false;

			bool res = false;
			
			int len = msg.getLength();
			byte[] data = msg.getData();

			byte[] bytes = new byte[4];

			SystemUtils.int2bytes(msg.getType(), bytes);
			if (stream.write(bytes, 0, 4) == 0) goto my_end;

			SystemUtils.int2bytes(len, bytes);
			if (stream.write(bytes, 0, 4) == 0) goto my_end;

			if (len > 0)
			{
				SystemUtils.assert(data != null);
				if (stream.write(data, 0, len) == 0) goto my_end;
			}

			res = true;

			my_end:
			if (!res) close();

			return res;
		}

		public void close()
		{
			lock (m_lock)
			{
				if (m_stream != null)
				{
					try
					{
						m_stream.close();
					}
					catch (Exception)
					{
					}
					m_stream = null;
				}
			}
		}

		//=====================================================================================================

		private IStream getStream()
		{
			lock (m_lock)
			{
				return m_stream;
			}
		}

		private static readonly int s_kReadBufferSize = 1024 * 1024 * 2; // 2 MB

		protected IStream m_stream;

		private byte[]	m_readBuffer;
		private int		m_readOffset;
		private int		m_readLen;

		private Object m_lock;
	}
}
