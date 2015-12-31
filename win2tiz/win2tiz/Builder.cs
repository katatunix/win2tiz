using System;
using System.Collections.Generic;
using System.IO;

using libcore;

namespace win2tiz
{
	class Builder : ICompileNotifier
	{
		public Builder()
		{
			m_lock = new Object();
			m_batchingCompiler = new BatchingCompiler();
			m_isVerbose = false;
			m_countCompile = 0;
			m_totalCompile = 0;
			m_commands = new List<TCommand>();
		}

		public void clearCommands()
		{
			m_commands.Clear();
			m_totalCompile = 0;
		}

		public void addCommand(TCommand cmd)
		{
			m_commands.Add(cmd);
			if (cmd.type == ECommandType.eCompile)
			{
				m_totalCompile++;
			}
		}

		public void addCommands(List<TCommand> list)
		{
			foreach (TCommand cmd in list)
			{
				addCommand(cmd);
			}
		}

		public bool build(bool isVerbose, int jobs)
		{
			CConsole.writeInfo("Total: " + m_totalCompile + " files to be compiled\n\n");
			m_isVerbose = isVerbose;

			// Compile

			lock (m_lock)
			{
				m_countCompile = 0;
			}

			if (m_totalCompile > 0)
			{
				m_batchingCompiler.compile(m_commands, jobs, this);
				CConsole.writeLine();
			}

			lock (m_lock)
			{
				if (m_countCompile == -1) return false; // stop build when compile error
			}

			// Link static
			if (!executeAllCommandsWithType(ECommandType.eLinkStatic)) return false;

			// Link dynamic
			if (!executeAllCommandsWithType(ECommandType.eLinkDynamic)) return false;

			// Generate DSYM
			if (!executeAllCommandsWithType(ECommandType.eGenerateDsym)) return false;

			// Copy
			if (!executeAllCommandsWithType(ECommandType.eCopy)) return false;

			// Strip
			if (!executeAllCommandsWithType(ECommandType.eStrip)) return false;

			return true;
		}

		public void signalToStop()
		{
			lock (m_lock)
			{
				m_countCompile = -1;
				m_batchingCompiler.signalToStop(true);
			}
		}

		public void onFinishCompile(TCommand cmd, TCompileResult res)
		{
			lock (m_lock)
			{
				if (m_countCompile == -1) return;

				m_countCompile++;
				CConsole.writeInfo(m_countCompile + "/" + m_totalCompile + ". " + cmd.prjName + ". Compile: " + cmd.alias);

				if (m_isVerbose && !string.IsNullOrEmpty(cmd.verboseString))
				{
					CConsole.writeVerbose("\n" + cmd.verboseString);
				}
				if (!string.IsNullOrEmpty(res.host))
				{
					CConsole.writeMongcc(" (" + res.host + ")");
				}

				bool success = checkProcessResult(res);
				
				CConsole.writeLine();
				if (!success)
				{
					m_countCompile = -1;
					m_batchingCompiler.signalToStop(false);
				}
			}
		}

		//=============================================================================

		private bool executeAllCommandsWithType(ECommandType type)
		{
			foreach (TCommand cmd in m_commands)
			{
				if (cmd.type != type || cmd.type == ECommandType.eCompile) continue;

				switch (type)
				{
					case ECommandType.eLinkStatic:
						CConsole.writeInfo("Link static: " + cmd.alias);
						break;
					case ECommandType.eLinkDynamic:
						CConsole.writeInfo("Link dynamic: " + cmd.alias);
						break;
					case ECommandType.eGenerateDsym:
						CConsole.writeInfo("Generate DSYM: " + cmd.alias);
						break;
					case ECommandType.eCopy:
						CConsole.writeInfo("Copy: " + cmd.alias);
						break;
					case ECommandType.eStrip:
						CConsole.writeInfo("Strip: " + cmd.alias);
						break;
				}
				
				if (m_isVerbose)
				{
					if (type == ECommandType.eCopy)
					{
						CConsole.writeVerbose("\n" + cmd.command + " -> " + cmd.verboseString);
					}
					else if (!string.IsNullOrEmpty(cmd.verboseString))
					{
						CConsole.writeVerbose("\n" + cmd.verboseString);
					}
				}

				bool ok = true;

				if (type == ECommandType.eCopy)
				{
					int oldTick = SystemUtils.getCurrentTimeMs();
					string error = null;
					try
					{
						File.Copy(cmd.command, cmd.verboseString, true);
					}
					catch (Exception ex)
					{
						error = ex.Message;
					}

					int duration = SystemUtils.getCurrentTimeMs() - oldTick;
					CConsole.writeSpentTime(" (" + (float)duration / 1000.0f + "s)");

					if (error == null)
					{
						CConsole.writeSuccess(" (success)");
					}
					else
					{
						CConsole.writeError(" (error)\n" + error);
						ok = false;
					}
				}
				else
				{
					TProcessResult pr = ProcessHelper.executeStatic(cmd.command, cmd.workingDir);
					if (!checkProcessResult(pr)) ok = false;
				}

				CConsole.writeInfo("\n\n");

				if (!ok) return false;
			}

			return true;
		}

		private bool checkProcessResult(TProcessResult pr)
		{
			int time = pr.spentTimeMs;
			if (time > 0)
			{
				CConsole.writeSpentTime(" (" + (float)time / 1000.0f + "s)");
			}
			bool error = !pr.wasExec || pr.exitCode != 0;
			if (error)
			{
				CConsole.writeError(" (error)");
			}
			else
			{
				CConsole.writeSuccess(" (success)");
			}

			// Only print output text when error?
			if (!string.IsNullOrEmpty(pr.outputText) && (m_isVerbose || error))
			{
				CConsole.writeOutputText("\n" + pr.outputText);
			}
			return !error;
		}

		//=============================================================================

		private BatchingCompiler m_batchingCompiler;

		private bool m_isVerbose;
		private int m_countCompile;
		private int m_totalCompile;

		private List<TCommand> m_commands;

		private Object m_lock;
	}
}
