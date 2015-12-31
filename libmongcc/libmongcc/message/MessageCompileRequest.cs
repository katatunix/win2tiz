using libcore;

namespace libmongcc.message
{
	class MessageCompileRequest
	{
		public MessageCompileRequest(Message msg)
		{
			byte[] data = msg.getData();
			BinStream stream = new BinStream(data);
			m_cmd = stream.readString();
			stream.close();
		}

		public string getCmd()
		{
			return m_cmd;
		}

		public static Message createMessage(string compileCmd)
		{
			int msgLen = compileCmd.Length;
			byte[] data = new byte[msgLen];
			StringUtils.getBytesFromString(compileCmd, data);
			return new Message((int)EMessageType.eCompileRequest, msgLen, data);
		}

		private string m_cmd;
	}
}
