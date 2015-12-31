namespace win2tiz
{
	struct TBuildResult
	{
		public bool isSuccess;
		public bool isNew;

		public TBuildResult(bool _isSuccess, bool _isNew)
		{
			isSuccess = _isSuccess;
			isNew = _isNew;
		}
	}
}
