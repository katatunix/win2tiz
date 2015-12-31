using libcore;

namespace win2tiz
{
	interface ICompileNotifier
	{
		void onFinishCompile(TCommand cmd, TCompileResult res);
	}
}
