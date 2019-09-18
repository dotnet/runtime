// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;

/// <summary>
/// Performance events specific to ReadyToRun.
/// </summary>
namespace ILCompiler
{
    // The event IDs here must not collide with the ones used by DependencyAnalysis' PerfEventSource
    [EventSource(Name = "Microsoft-ILCompiler-Perf")]
    public class PerfEventSource : EventSource
    {
        [Event(1, Level = EventLevel.Informational)]
        public void LoadingStart() { WriteEvent(1); }
        [Event(2, Level = EventLevel.Informational)]
        public void LoadingStop() { WriteEvent(2); }

        [Event(3, Level = EventLevel.Informational)]
        public void EmittingStart() { WriteEvent(3); }
        [Event(4, Level = EventLevel.Informational)]
        public void EmittingStop() { WriteEvent(4); }

        [Event(5, Level = EventLevel.Informational)]
        public void CompilationStart() { WriteEvent(5); }
        [Event(6, Level = EventLevel.Informational)]
        public void CompilationStop() { WriteEvent(6); }

        [Event(7, Level = EventLevel.Informational)]
        public void JitStart() { WriteEvent(7); }
        [Event(8, Level = EventLevel.Informational)]
        public void JitStop() { WriteEvent(8); }

        public static PerfEventSource Log = new PerfEventSource();
    }
}
