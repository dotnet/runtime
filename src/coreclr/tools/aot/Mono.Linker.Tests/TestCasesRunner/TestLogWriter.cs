// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILCompiler;
using ILCompiler.Logging;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestLogWriter : ILogWriter
	{
		private readonly StringWriter _infoStringWriter;
		private readonly TextWriter _infoWriter;

		private readonly List<MessageContainer> _messageContainers;

		public TestLogWriter ()
		{
			_infoStringWriter = new StringWriter ();
			_infoWriter = TextWriter.Synchronized (_infoStringWriter);
			_messageContainers = new List<MessageContainer> ();
		}

		public TextWriter Writer => _infoWriter;

		public List<MessageContainer> GetLoggedMessages ()
		{
			return _messageContainers;
		}

		public void WriteError (MessageContainer error)
		{
			lock (_messageContainers) {
				_messageContainers.Add (error);
			}
		}

		public void WriteMessage (MessageContainer message)
		{
			lock (_messageContainers) {
				_messageContainers.Add (message);
			}
		}

		public void WriteWarning (MessageContainer warning)
		{
			lock (_messageContainers) {
				_messageContainers.Add (warning);
			}
		}
	}
}
