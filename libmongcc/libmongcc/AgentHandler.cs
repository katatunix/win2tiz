using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

using libcore;
using libmongcc.message;

namespace libmongcc
{
	class AgentHandler
	{
		public AgentHandler(Agent agent)
		{
			m_agent = agent;
			m_cancelEvent = new AutoResetEvent(false);

			m_thread = new Thread(new ThreadStart(threadHandle));
			m_thread.Start();
		}

		public void join()
		{
			m_thread.Join();
		}

		public void signalToStop()
		{
			m_cancelEvent.Set();
		}

		//=========================================================================================

		private void threadHandle()
		{
			while (true)
			{
				using (AutoResetEvent connectEvent = new AutoResetEvent(false))
				{
					m_cancelEvent.Reset();

					NamedPipeServerStream pipeServer = new NamedPipeServerStream(
						Config.s_kAgentName, PipeDirection.InOut,
						Config.s_kMaxAgentHandlers, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

					pipeServer.BeginWaitForConnection(ar =>
					{
						try
						{
							pipeServer.EndWaitForConnection(ar);
							processStream(pipeServer);
						}
						catch (Exception)
						{
						}
						connectEvent.Set();
					}, null);

					int index = WaitHandle.WaitAny(new WaitHandle[] { connectEvent, m_cancelEvent });
					pipeServer.Close(); // this will stop the processStream() if it is still running
					if (index == 1) // signalToStop() was called
					{
						connectEvent.WaitOne(); // wait for processStream() is completely stopped
						break;
					}
				}
			}
		}

		private void processStream(Stream stream)
		{
			MessageStream msgStream = new MessageStream(new StandardStream(stream));

			Message msg = msgStream.readMessage();
			if (msg == null)
			{
				return;
			}

			EMessageType mType = (EMessageType)msg.getType();
			if (mType == EMessageType.eNumber)
			{
				// End a session
				MessageNumber response = new MessageNumber(msg);
				int sid = response.getNumber();
				m_agent.signalToStopSession(sid);
			}
			else if (mType == EMessageType.ePidAndCompileRequest)
			{
				MessagePidAndCompileRequest request = new MessagePidAndCompileRequest(msg);
				TCompileResult cr = m_agent.compile(request.getPid(), request.getCmd(), request.getWorkingDir());

				// The output file was already saved to local disk by the agent, now respond the result only
				msg = MessageCompileResponse.createMessage(cr.wasExec, cr.exitCode, cr.outputText, null, 0);
				msgStream.writeMessage(msg);
			}
			else
			{
				// WTF???
			}
		}

		//===============================================================================================

		private Thread m_thread;
		private Agent m_agent;
		private AutoResetEvent m_cancelEvent;
	}
}
