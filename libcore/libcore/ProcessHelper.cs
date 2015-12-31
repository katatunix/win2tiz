using System;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace libcore
{
	public class ProcessHelper
	{
		public static TProcessResult executeStatic(string cmd, string workingDir, int timeoutMs = 300000)
		{
			return new ProcessHelper().execute(cmd, workingDir, null, timeoutMs);
		}

		public static bool justExecuteStatic(string cmd, string workingDir)
		{
			int spaceIndex = cmd.IndexOf(" ");
			if (spaceIndex == -1)
			{
				return false;
			}

			ProcessStartInfo psi = new ProcessStartInfo();
			psi.UseShellExecute = false;
			psi.WindowStyle = ProcessWindowStyle.Hidden;
			psi.CreateNoWindow = true;
			psi.ErrorDialog = false;

			psi.FileName = cmd.Substring(0, spaceIndex);
			psi.Arguments = cmd.Substring(spaceIndex + 1);
			psi.WorkingDirectory = workingDir;

			try
			{
				Process.Start(psi);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		//===================================================================================================

		public ProcessHelper()
		{
			m_lock = new Object();
			m_process = null;
			m_cmd = null;
			m_workingDir = null;
			m_outputFilePath = null;
			m_processId = 0;
		}

		public TProcessResult execute(string cmd, string workingDir, string outputFilePath = null, int timeoutMs = 300000)
		{
			int spaceIndex = cmd.IndexOf(" ");
			if (spaceIndex == -1)
			{
				return new TProcessResult(false, 1, "error: Invalid command!", 0);
			}

			ProcessStartInfo psi = new ProcessStartInfo();
			psi.UseShellExecute = false;
			psi.RedirectStandardError = true;
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardInput = true;
			psi.WindowStyle = ProcessWindowStyle.Hidden;
			psi.CreateNoWindow = true;
			psi.ErrorDialog = false;

			psi.FileName = cmd.Substring(0, spaceIndex);
			psi.Arguments = cmd.Substring(spaceIndex + 1);
			psi.WorkingDirectory = workingDir;

			m_cmd = cmd;
			m_workingDir = workingDir;
			m_outputFilePath = outputFilePath;

			TProcessResult ret = null;

			try
			{
				int oldTick = SystemUtils.getCurrentTimeMs();

				using (m_process = Process.Start(psi))
				{
					setProcessId(m_process.Id);

					using (ManualResetEvent mreOut = new ManualResetEvent(false), mreErr = new ManualResetEvent(false))
					{
						string output = "";

						m_process.OutputDataReceived += (o, e) => { if (e.Data == null) mreOut.Set(); else { output += e.Data + "\r\n"; } };
						m_process.BeginOutputReadLine();
						m_process.ErrorDataReceived += (o, e) => { if (e.Data == null) mreErr.Set(); else { output += e.Data + "\r\n"; } };
						m_process.BeginErrorReadLine();

						m_process.StandardInput.Close();

						bool exited = m_process.WaitForExit(timeoutMs); // block
						if (!exited)
						{
							ret = new TProcessResult(false, 1, "error: the process takes too long time!", 0);
							kill();
						}
						else
						{
							mreOut.WaitOne();
							mreErr.WaitOne();
							string outputText = output.Trim();
							ret = m_process.ExitCode != 0 && outputText.Length == 0 ?
								new TProcessResult(false, 0, outputText, SystemUtils.getCurrentTimeMs() - oldTick) :
								new TProcessResult(true, m_process.ExitCode, outputText, SystemUtils.getCurrentTimeMs() - oldTick);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ret = new TProcessResult(false, 1, "error: " + ex.Message + " [" + psi.FileName + "]", 0);
			}

			setProcessId(0);
			return ret;
		}

		public void kill()
		{
			int pid = getProcessId();
			if (pid > 0)
			{
				SystemUtils.killProcessAndChildren(pid, true);

				if (m_outputFilePath != null)
				{
					CConsole.writeInfoLine("Delete: " + m_outputFilePath);
					try
					{
						File.Delete(m_outputFilePath);
					}
					catch (Exception)
					{
					}
				}
			}
		}

		//=======================================================================================================

		private int getProcessId()
		{
			lock (m_lock)
			{
				return m_processId;
			}
		}

		private void setProcessId(int v)
		{
			lock (m_lock)
			{
				m_processId = v;
			}
		}

		//=======================================================================================================

		private Process m_process;
		private string m_cmd;
		private string m_workingDir;
		private string m_outputFilePath;

		private int m_processId;
		private Object m_lock;
	}
}
