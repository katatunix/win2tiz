using libcore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libmongcc.cmdparser
{
	public class CmdParserFactory
	{
		public static ICmdParser createCmdParser(string cmd)
		{
			string tool = StringUtils.getFirstToken(cmd);
			if (tool.EndsWith("PVRTexTool.exe"))
			{
				return new PvrTexToolCmdParser(cmd);
			}
			if (tool.EndsWith("PVRTexTool34.exe"))
			{
				return new PvrTexTool34CmdParser(cmd);
			}
			if (tool.EndsWith("etcpack.exe"))
			{
				return new EtcPackCmdParser(cmd);
			}
			if (tool.EndsWith("Mojito.exe"))
			{
				return new MojitoCmdParser(cmd);
			}
			if (tool.EndsWith("PngOptimizerCL.exe"))
			{
				return new PngOptimizerCmdParser(cmd);
			}
			return new GccCmdParser(cmd);
		}
	}
}
