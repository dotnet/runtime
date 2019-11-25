using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class LinkerTestLogger : ILogger
	{
		public struct MessageRecord
		{
			public MessageImportance Importance;
			public string Message;
		}

		public List<MessageRecord> Messages { get; private set; } = new List<MessageRecord>();

		public void LogMessage(MessageImportance importance, string message, params object[] values)
		{
			Messages.Add(new MessageRecord
			{
				Importance = importance,
				Message = string.Format(message, values)
			});
		}
	}
}