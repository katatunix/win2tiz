using libcore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace libmongcc
{
	class StandardStream : IStream
	{
		public StandardStream(Stream stream)
		{
			m_stream = stream;
		}

		public int read(byte[] buffer, int offset, int count)
		{
			int len = 0;
			try
			{
				len = m_stream.Read(buffer, offset, count);
			}
			catch (Exception)
			{
				len = 0;
			}
			return len;
		}

		public int write(byte[] buffer, int offset, int count)
		{
			int len = 0;
			try
			{
				m_stream.Write(buffer, offset, count);
				len = count;
			}
			catch (Exception)
			{
				len = 0;
			}
			return len;
		}

		public void close()
		{
			m_stream.Close();
		}

		private Stream m_stream;
	}
}
