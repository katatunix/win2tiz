using libcore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libmongcc.message
{
	class MessagePidAndCompileRequest
	{
		public MessagePidAndCompileRequest(Message msg)
		{
			byte[] data = msg.getData();
			BinStream stream = new BinStream(data);
			m_pid = stream.readInt();

			int len = stream.readInt();
			m_cmd = stream.readString(len);

			m_woringDir = stream.readString();

			stream.close();
		}

		public int getPid()
		{
			return m_pid;
		}

		public string getCmd()
		{
			return m_cmd;
		}

		public string getWorkingDir()
		{
			return m_woringDir;
		}

		public static Message createMessage(int pid, string compileCmd, string workingDir)
		{
			int msgLen = 4 + 4 + compileCmd.Length + workingDir.Length;
			byte[] data = new byte[msgLen];

			int offset = 0;

			SystemUtils.int2bytes(pid, data, offset);
			offset += 4;

			SystemUtils.int2bytes(compileCmd.Length, data, offset);
			offset += 4;

			StringUtils.getBytesFromString(compileCmd, data, offset);
			offset += compileCmd.Length;

			StringUtils.getBytesFromString(workingDir, data, offset);
			offset += workingDir.Length;

			return new Message((int)EMessageType.ePidAndCompileRequest, msgLen, data);
		}

		private int m_pid;
		private string m_cmd;
		private string m_woringDir;
	}
}
