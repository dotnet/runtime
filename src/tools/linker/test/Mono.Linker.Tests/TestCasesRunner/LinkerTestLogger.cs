using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class LinkerTestLogger : ILogger
	{
		public struct MessageRecord
		{
			public string Message;
		}

		public List<MessageRecord> Messages { get; private set; } = new List<MessageRecord> ();

		public void LogMessage (MessageContainer msBuildMessage)
		{
			Messages.Add (new MessageRecord {
				Message = msBuildMessage.ToString ()
			});
		}
	}
}