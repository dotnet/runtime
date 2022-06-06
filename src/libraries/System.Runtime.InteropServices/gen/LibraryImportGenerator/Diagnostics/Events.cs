// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Interop.Diagnostics
{
    [EventSource(Name = "Microsoft-Interop-SourceGeneration-Events")]
    internal sealed class Events : EventSource
    {
        public static class Keywords
        {
            public const EventKeywords SourceGeneration = (EventKeywords)1;
        }

        public static readonly Events Logger = new Events();

        private const int StartSourceGenerationEventId = 1;
        private const int StopSourceGenerationEventId = StartSourceGenerationEventId + 1;

        private Events()
        { }

        /// <summary>
        /// Utility function that wraps emitting start/stop events for the source generation event.
        /// </summary>
        /// <param name="methodCount">The number of methods being generated</param>
        /// <returns>An <see cref="IDisposable"/> instance that will fire the "stop" event when Disposed.</returns>
        [NonEvent]
        public static IDisposable SourceGenerationStartStop(int methodCount)
        {
            return new StartStopEvent(methodCount);
        }

        // N.B. The 'Start' and 'Stop' suffixes for event names (i.e. "xxxStart" and "xxxStop")
        //  have special meaning in EventSource. They enable creating 'activities' if they are
        //  paired and the Stop event's ID is +1 the Start event's ID.
        //  See https://blogs.msdn.microsoft.com/vancem/2015/09/14/exploring-eventsource-activity-correlation-and-causation-features/

        /// <summary>
        /// Indicates the interop's DllImport Roslyn Source Generator has started source generation.
        /// </summary>
        /// <param name="methodCount">The number of methods being generated</param>
        [Event(StartSourceGenerationEventId, Level = EventLevel.Informational, Keywords = Keywords.SourceGeneration)]
        public void SourceGenerationStart(int methodCount)
        {
            WriteEvent(StartSourceGenerationEventId, methodCount);
        }

        /// <summary>
        /// Indicates the interop's DllImport Roslyn Source Generator has stopped source generation.
        /// </summary>
        [Event(StopSourceGenerationEventId, Level = EventLevel.Informational, Keywords = Keywords.SourceGeneration)]
        public void SourceGenerationStop()
        {
            WriteEvent(StopSourceGenerationEventId);
        }

        private sealed class StartStopEvent : IDisposable
        {
            public StartStopEvent(int methodCount) => Logger.SourceGenerationStart(methodCount);
            public void Dispose() => Logger.SourceGenerationStop();
        }
    }
}
