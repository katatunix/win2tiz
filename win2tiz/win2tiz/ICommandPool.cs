using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace win2tiz
{
	interface ICommandPool
	{
		TCommand getNextCommand();
	}
}
