// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

namespace Mono.Linker
{
	[EventSource (Name = "Microsoft-DotNET-Linker")]
	sealed class LinkerEventSource : EventSource
	{
		public static LinkerEventSource Log { get; } = new LinkerEventSource ();

		[Event (1)]
		public void LinkerStart (string args) => WriteEvent (1, args);

		[Event (2)]
		public void LinkerStop () => WriteEvent (2);

		[Event (3, Keywords = Keywords.Step)]
		public void LinkerStepStart (string stepName) => WriteEvent (3, stepName);

		[Event (4, Keywords = Keywords.Step)]
		public void LinkerStepStop (string stepName) => WriteEvent (4, stepName);

		public static class Keywords
		{
			public const EventKeywords Step = (EventKeywords) (1 << 1);
		}
	}
}
