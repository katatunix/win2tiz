namespace win2tiz
{
	class TCommand
	{
		public string		command;
		public string		verboseString;
		public string		workingDir;
		public string		alias;
		public ECommandType	type;
		public string		prjName;

		public TCommand()
		{

		}

		public TCommand(
			string			_command,
			string			_verboseString,
			string			_workingDir,
			string			_alias,
			ECommandType	_type,
			string			_prjName)
		{
			command			= _command;
			verboseString	= _verboseString;
			workingDir		= _workingDir;
			alias			= _alias;
			type			= _type;
			prjName			= _prjName;
		}
	}
}
