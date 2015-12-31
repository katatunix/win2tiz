using libcore;

namespace libmongcc.message
{
	class MessageFreeHandlerNumberRequest
	{
		public MessageFreeHandlerNumberRequest(Message msg)
		{
			
		}

		public static Message createMessage()
		{
			return new Message((int)EMessageType.eFreeHandlerNumberRequest, 0, null);
		}

	}
}
