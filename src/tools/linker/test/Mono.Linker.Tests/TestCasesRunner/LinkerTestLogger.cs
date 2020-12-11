using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class LinkerTestLogger : ILogger
	{
		readonly List<MessageContainer> MessageContainers;

		public LinkerTestLogger ()
		{
			MessageContainers = new List<MessageContainer> ();
		}

		public List<MessageContainer> GetLoggedMessages ()
		{
			return MessageContainers;
		}

		public void LogMessage (MessageContainer message)
		{
			MessageContainers.Add (message);
		}
	}
}