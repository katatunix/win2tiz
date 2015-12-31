using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;

using libcore;
using libmongcc.message;
using libmongcc.cmdparser;

namespace libmongcc
{
	class Remote
	{
		/// <summary>
		/// Make sure timeoutMs is also valid if host and port are valid
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <param name="timeoutMs"></param>
		public Remote(string host, int port, int timeoutMs)
		{
			m_host = host;
			m_port = port;
			m_timeoutMs = timeoutMs;

			m_isCompiling = false;
			m_lockIsCompiling = new Object();

			m_isAllowCompiling = true;
			m_lockAllowCompiling = new Object();

			m_messageStream = new MessageStream();
			m_processHelper = new ProcessHelper();

			m_lastConnectTimeMs = 0;

			m_sentFiles = new List<string>();
		}

		public bool canConnect()
		{
			return !string.IsNullOrEmpty(m_host) && m_port > 0;
		}

		/// <summary>
		/// Make sure: isConnected() == false and isCompiling() == false and canConnect() == true
		/// Make sure this method is thread-safety
		/// </summary>
		/// <returns></returns>
		public bool connect()
		{
			m_lastConnectTimeMs = SystemUtils.getCurrentTimeMs();

			const int k_timeout = 5000;

			TcpClient tcpClient = new TcpClient();
			bool connected = false;

			IAsyncResult result = tcpClient.BeginConnect(m_host, m_port, null, null);
			bool success = result.AsyncWaitHandle.WaitOne(k_timeout, true);

			if (success && tcpClient.Connected)
			{
				tcpClient.EndConnect(result);
				connected = true;
			}
			else
			{
				tcpClient.Close();
			}

			if (connected)
			{
				try
				{
					tcpClient.Client.ReceiveTimeout = m_timeoutMs;

					byte[] tmp = new byte[1];
					int pingLen = tcpClient.Client.Receive(tmp, 0, 1, SocketFlags.None);
					if (pingLen <= 0 || tmp[0] == 0)
					{
						tcpClient.Close();
						connected = false;
					}
					else
					{
						m_messageStream.setStream(new SocketStream(tcpClient.Client));

						m_sentFiles.Clear();
						connected = true;
					}
				}
				catch (Exception)
				{
					connected = false;
				}
			}
			
			return connected;
		}

		/// <summary>
		/// Make sure isCompiling() == true as setCompilingTrue() was called
		/// Make sure all parameters are not null
		/// Make sure this method is thread-safety: it cannot be called at the same time
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="workingDir"></param>
		/// <returns></returns>
		public TCompileResult compile(string cmd, string workingDir)
		{
			int oldTick = SystemUtils.getCurrentTimeMs();

			ICmdParser parser = CmdParserFactory.createCmdParser(cmd);

			List<string> inputFilePaths = null;
			string outputFilePath = null;

			TCompileResult result = null;

			if (!isAllowCompiling()) { result = new TCompileResult(false, 1, "error: Compiling is stopped!", 0, null); goto my_end; }

			try
			{
				parser.getInOutFilePath(workingDir, out inputFilePaths, out outputFilePath);
			}
			catch (Exception ex)
			{
				// Error
				result = new TCompileResult(false, 1, "error: " + ex.Message, 0, null);
				goto my_end;
			}

			if (inputFilePaths == null || inputFilePaths.Count == 0 || outputFilePath == null)
			{
				// Error
				result = new TCompileResult(false, 1, "error: Invalid command, input file is mandatory!", 0, null);
				goto my_end;
			}

			outputFilePath = PathUtils.combine(workingDir, outputFilePath);

			if (!isConnected())
			{
				// Local
				goto my_end;
			}

			#region Distribute

			// Send the input files to server
			foreach (string inputFilePath in inputFilePaths)
			{
				string f = PathUtils.combine(workingDir, inputFilePath);
				string fNormal = PathUtils.normalizePath(f);
				if (!m_sentFiles.Contains(fNormal))
				{
					if (sendFile(f))
					{
						m_sentFiles.Add(fNormal);
					}
					else
					{
						// Local
						goto my_end;
					}
				}
			}

			// Send compile request to server
			string cmdToSend = parser.makeNetworkCmd();
			Message msg = MessageCompileRequest.createMessage(cmdToSend);
			if (!m_messageStream.writeMessage(msg))
			{
				goto my_end;
			}

			if (!isAllowCompiling()) { result = new TCompileResult(false, 1, "error: Compiling is stopped!", 0, null); goto my_end; }

			// Receive the compile result
			msg = m_messageStream.readMessage();
			if (msg == null)
			{
				goto my_end;
			}

			if (!isAllowCompiling()) { result = new TCompileResult(false, 1, "error: Compiling is stopped!", 0, null); goto my_end; }

			// Parse and check the compile result
			MessageCompileResponse msgCompileRes = new MessageCompileResponse(msg);

            if (msgCompileRes.getWasExec() && msgCompileRes.getExitCode() != 0)
            {
                // Compile error
                // Nghia: TODO rem this for the hot fix
                //result = new TCompileResult(true, msgCompileRes.getExitCode(), msgCompileRes.getOutputText(), 0, m_host);
                goto my_end;
            }
			if (!msgCompileRes.getWasExec())
			{
				goto my_end;
			}

			// Compile OK by server
			// Save the file to disk
			if (!IOUtils.writeFile_Bytes(outputFilePath, msgCompileRes.getOFileData(),
										msgCompileRes.getOFileOffset(), msgCompileRes.getOFileSize()))
			{
				// No need to compile local as this is a serious error
				result = new TCompileResult(true, 1, "error: Could not save the output file to disk", 0, m_host);
				goto my_end;
			}

			// Everything is OK
			result = new TCompileResult(true, 0,
				"Remotely compiled from " + m_host + "\n" + msgCompileRes.getOutputText(), 0, m_host);

			#endregion

			my_end:

			if (result == null)
			{
				// Local
				string specOutFilePath = parser.getLocalSpecificOutputFilePath();
				string specCmd = parser.getLocalSpecificCommand();
				string specWorkingDir = parser.getLocalSpecificWorkingDir();

				if (specOutFilePath == null) specOutFilePath = outputFilePath;
				if (specCmd == null) specCmd = cmd;
				if (specWorkingDir == null) specWorkingDir = workingDir;

				specOutFilePath = PathUtils.combine(specWorkingDir, specOutFilePath);

				TProcessResult res = null;

				if (parser.needToLockLocal())
				{
					lock (s_lock)
					{
						doLocal(specCmd, specWorkingDir, specOutFilePath, ref oldTick, ref res);
					}
				}
				else
				{
					doLocal(specCmd, specWorkingDir, specOutFilePath, ref oldTick, ref res);
				}

				result = new TCompileResult(res.wasExec, res.exitCode, "Locally compiled\n" + res.outputText, 0, null);

				if (result.wasExec && result.exitCode == 0 && specOutFilePath != outputFilePath)
				{
					File.Move(specOutFilePath, outputFilePath);
				}
			}

			result.spentTimeMs = SystemUtils.getCurrentTimeMs() - oldTick;

			setCompiling(false);

			return result;
		}

		//=====================================================================================================
		
		public void signalToStopAndDisconnect()
		{
			setAllowCompiling(false);
			m_processHelper.kill();
			m_messageStream.close();
		}

		public void allowCompiling()
		{
			setAllowCompiling(true);
		}

		public bool isConnected()
		{
			return m_messageStream.isOpenned();
		}

		public bool isCompiling()
		{
			lock (m_lockIsCompiling)
			{
				return m_isCompiling;
			}
		}

		public int getLastConnectTimeMs()
		{
			return m_lastConnectTimeMs;
		}

		public void setCompilingTrue()
		{
			setCompiling(true);
		}

		//=============================================================================================

		private bool isAllowCompiling()
		{
			lock (m_lockAllowCompiling)
			{
				return m_isAllowCompiling;
			}
		}
		
		private void setAllowCompiling(bool v)
		{
			lock (m_lockAllowCompiling)
			{
				m_isAllowCompiling = v;
			}
		}

		private void setCompiling(bool v)
		{
			lock (m_lockIsCompiling)
			{
				m_isCompiling = v;
			}
		}

		private bool sendFile(string path)
		{
			Message msg = MessageFile.createMessage(path);
			if (msg == null) return false;
			return m_messageStream.writeMessage(msg);
		}

		private void doLocal(string cmd, string workingDir, string outputFilePath, ref int oldTick, ref TProcessResult res)
		{
			if (isAllowCompiling())
			{
				oldTick = SystemUtils.getCurrentTimeMs();
				res = m_processHelper.execute(cmd, workingDir, outputFilePath);
			}
			else
			{
				res = new TCompileResult(false, 1, "error: Compiling is stopped!", 0, null);
			}
		}

		//=============================================================================================

		private string m_host;
		private int m_port;
		private int m_timeoutMs;

		private bool m_isCompiling;
		private Object m_lockIsCompiling;

		private bool m_isAllowCompiling;
		private Object m_lockAllowCompiling;

		private int m_lastConnectTimeMs;

		private MessageStream m_messageStream;
		private ProcessHelper m_processHelper;

		private List<string> m_sentFiles;

		//=============================================================================================

		private static Object s_lock = new Object(); // for UB files
	}
}
