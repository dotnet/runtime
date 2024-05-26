// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TrimmingTestLogger : ILogger
	{
		readonly List<MessageContainer> MessageContainers;

		public TrimmingTestLogger ()
		{
			MessageContainers = new List<MessageContainer> ();
		}

		public ImmutableArray<MessageContainer> GetLoggedMessages ()
		{
			return MessageContainers.ToImmutableArray();
		}

		public void LogMessage (MessageContainer message)
		{
			// This is to force Cecil to load all the information from the assembly
			// When the message is logged, the assembly is still opened by ILLink and available
			// later on during validation, it may already be closed and Cecil's lazy loading might fail.
			message.ToString ();

			MessageContainers.Add (message);
		}
	}
}
