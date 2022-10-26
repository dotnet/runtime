// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
