namespace win2tiz
{
	struct TFileSpecific
	{
		public string		name;
		public string[]		cflags;
		public string[]		defines;

		public TFileSpecific(string _name, string[] _cflags, string[] _defines)
		{
			name		= _name;
			cflags		= _cflags;
			defines		= _defines;
		}
	}
}
