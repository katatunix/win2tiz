namespace libmongcc.message
{
	enum EMessageType
	{
		eNone = 0,
		eFile,
		eNumber,
		eCompileRequest,
		ePidAndCompileRequest,
		eCompileResponse,
		eFreeHandlerNumberRequest
	}
}
