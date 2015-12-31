using libcore;

namespace libmongcc.message
{
	class MessageNumber
	{
		public MessageNumber(Message msg)
		{
			byte[] data = msg.getData();
			BinStream stream = new BinStream(data);
			m_number = stream.readInt();
			stream.close();
		}

		public int getNumber()
		{
			return m_number;
		}

		public static Message createMessage(int num)
		{
			int msgLen = 4;
			byte[] data = new byte[msgLen];
			SystemUtils.int2bytes(num, data);

			return new Message((int)EMessageType.eNumber, msgLen, data);
		}

		private int m_number;
	}
}
