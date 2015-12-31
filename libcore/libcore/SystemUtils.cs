using System;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.Mail;

namespace libcore
{
	public class SystemUtils
	{
		public static void assert(bool condition, string message = "")
		{
			if (!condition)
			{
				string msg = "ASSERT FAILED";
				msg = string.IsNullOrEmpty(message) ? msg : (msg + ": " + message);
				throw new Exception(msg);
			}
		}

		public static int getCurrentTimeMs()
		{
			return Environment.TickCount;
		}

		public static int getParentProcessId()
		{
			var myId = Process.GetCurrentProcess().Id;
			var query = string.Format("SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {0}", myId);
			var search = new ManagementObjectSearcher("root\\CIMV2", query);
			var results = search.Get().GetEnumerator();
			if (!results.MoveNext()) throw new Exception("Huh?");
			var queryObj = results.Current;
			uint parentId = (uint)queryObj["ParentProcessId"];
			return (int)parentId;
		}
		
		public static void killProcessAndChildren(int pid, bool isKillRoot)
		{
			ManagementObjectSearcher searcher = new ManagementObjectSearcher(
				"Select * From Win32_Process Where ParentProcessID=" + pid);
			ManagementObjectCollection moc = searcher.Get();

			if (isKillRoot)
			{
				try
				{
					Process proc = Process.GetProcessById(pid);

					CConsole.writeWarning(string.Format("Kill process [{0}] {1}\n", pid, proc.ProcessName));
					proc.Kill();
				}
				catch (Exception)
				{
					// Process already exited
				}
			}

			foreach (ManagementObject mo in moc)
			{
				killProcessAndChildren(Convert.ToInt32(mo["ProcessID"]), true);
			}
		}

		public static bool isProcessRunning(int pid)
		{
			try
			{
				Process.GetProcessById(pid);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static string getEnvVarValue(string var)
		{
			string value = Environment.GetEnvironmentVariable(var, EnvironmentVariableTarget.Process);
			if (value != null) return value;

			value = Environment.GetEnvironmentVariable(var, EnvironmentVariableTarget.User);
			if (value != null) return value;

			return Environment.GetEnvironmentVariable(var, EnvironmentVariableTarget.Machine);
		}

		public static string getWorkingDir()
		{
			return Environment.CurrentDirectory;
		}

		public static string getCurrentProcessPath()
		{
			return Process.GetCurrentProcess().MainModule.FileName;
		}

		public static void memcpy(byte[] dst, int dstOffset, byte[] src, int srcOffset, int len)
		{
			if (len <= 0) return;

			Buffer.BlockCopy(src, srcOffset, dst, dstOffset, len);
		}

		public static void memset(byte[] dst, int offset, int len, byte val)
		{
			for (int i = offset; i < offset + len; i++)
			{
				dst[i] = val;
			}
		}

		public static int setByte(int x, int offset, byte value)
		{
			x = (~(0xFF << (offset * 8))) & x;
			int tmp = (int)value;
			return (tmp << (offset * 8)) | x;
		}

		public static void int2bytes(int x, byte[] bytes, int offset = 0)
		{
			int mask = 0xFF;
			bytes[offset + 0] = (byte)(x & mask);
			bytes[offset + 1] = (byte)((x & (mask << 8)) >> 8);
			bytes[offset + 2] = (byte)((x & (mask << 16)) >> 16);
			bytes[offset + 3] = (byte)((x & (mask << 24)) >> 24);
		}

		public static void uint2bytes(uint x, byte[] bytes, int offset = 0)
		{
			int mask = 0xFF;
			bytes[offset + 0] = (byte)(x & mask);
			bytes[offset + 1] = (byte)((x & (mask << 8)) >> 8);
			bytes[offset + 2] = (byte)((x & (mask << 16)) >> 16);
			bytes[offset + 3] = (byte)((x & (mask << 24)) >> 24);
		}


		public static void sendEmail(string from, string name, string password, string to, string subject, string body)
		{
			var fromAddress = new MailAddress(from, name);
            var toAddress = new MailAddress(to, name);
			var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, password)
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                //You can also use SendAsync method instead of Send so your application begin invoking instead of waiting for send mail to complete. SendAsync(MailMessage, Object) :- Sends the specified e-mail message to an SMTP server for delivery. This method does not block the calling thread and allows the caller to pass an object to the method that is invoked when the operation completes. 
                smtp.Send(message);
            }
		}
	}
}
