// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker
{
	public class ConsoleLogger : ILogger
	{
		public void LogMessage (MessageContainer message)
		{
			Console.WriteLine (message.ToString ());
		}
	}
}
