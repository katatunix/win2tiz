namespace libcore
{
	public class TProcessResult
	{
		public bool		wasExec;
		public int		exitCode;
		public string	outputText;
		public int		spentTimeMs;

		public TProcessResult(bool _wasExec, int _exitCode, string _outputText, int _spentTimeMs)
		{
			wasExec		= _wasExec;
			exitCode	= _exitCode;
			outputText	= _outputText;
			spentTimeMs	= _spentTimeMs;
		}
	}
}
