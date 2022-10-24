// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace Mono.Linker
{
    [EventSource(Name = "Microsoft-DotNET-Linker")]
    sealed class LinkerEventSource : EventSource
    {
        public static LinkerEventSource Log { get; } = new LinkerEventSource();

        [Event(1)]
        public void LinkerStart(string args) => WriteEvent(1, args);

        [Event(2)]
        public void LinkerStop() => WriteEvent(2);

        [Event(3, Keywords = Keywords.Step)]
        public void LinkerStepStart(string stepName) => WriteEvent(3, stepName);

        [Event(4, Keywords = Keywords.Step)]
        public void LinkerStepStop(string stepName) => WriteEvent(4, stepName);

        public static class Keywords
        {
            public const EventKeywords Step = (EventKeywords)(1 << 1);
        }
    }
}
