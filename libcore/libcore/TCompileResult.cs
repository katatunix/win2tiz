namespace libcore
{
	public class TCompileResult : TProcessResult
	{
		public string host;

		public TCompileResult(bool _wasExec, int _exitCode, string _outputText, int _spentTimeMs, string _host)
			: base(_wasExec, _exitCode, _outputText, _spentTimeMs)
		{
			host = _host;
		}
	}
}
