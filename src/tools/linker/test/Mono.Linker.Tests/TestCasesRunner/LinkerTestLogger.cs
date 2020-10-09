using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class LinkerTestLogger : ILogger
	{
		StringWriter _stringWriter;
		public List<MessageContainer> MessageContainers { get; private set; }

		public LinkerTestLogger ()
		{
			MessageContainers = new List<MessageContainer> ();
			StringBuilder sb = new StringBuilder ();
			_stringWriter = new StringWriter (sb);
			Console.SetOut (_stringWriter);
		}

		public List<string> GetLoggedMessages ()
		{
			string allWarningsAsOneString = _stringWriter.GetStringBuilder ().ToString ();
			return allWarningsAsOneString.Split (Environment.NewLine.ToCharArray (), StringSplitOptions.RemoveEmptyEntries).ToList ();
		}

		public void LogMessage (MessageContainer message)
		{
			MessageContainers.Add (message);
			Console.WriteLine (message.ToString ());
		}
	}
}