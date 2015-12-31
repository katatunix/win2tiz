using System;
using System.IO;

using libcore;

namespace libmongcc.message
{
	class MessageFile
	{
		public MessageFile(Message msg)
		{
			byte[] data = msg.getData();
			BinStream stream = new BinStream(data);
			int filePathLen = stream.readInt();
			m_filePath = stream.readString(filePathLen); // sure the path is normalized
			m_fileSize = stream.remainBytes();
			m_offset = stream.currentPosition();
			m_data = data;

			stream.close();
		}

		public string getFilePath()
		{
			return m_filePath;
		}

		public int getOffset()
		{
			return m_offset;
		}

		public int getFileSize()
		{
			return m_fileSize;
		}

		public byte[] getData()
		{
			return m_data;
		}

		public static Message createMessage(string path)
		{
			try
			{
				int fileSize = IOUtils.getFileSize(path);
				int msgLen = 4 + path.Length + fileSize;

				byte[] data = new byte[msgLen];
					
				SystemUtils.int2bytes(path.Length, data);
				StringUtils.getBytesFromString(path, data, 4);

				if (fileSize > 0)
				{
					IOUtils.readFile_Bytes(path, data, 4 + path.Length, fileSize);
				}

				return new Message((int)EMessageType.eFile, msgLen, data);
			}
			catch (Exception)
			{
				return null;
			}
		}

		private string m_filePath;
		private int m_fileSize;
		private byte[] m_data;
		private int m_offset;
	}
}
