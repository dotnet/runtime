// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;

/// <summary>
/// Performance events releated to the dependency graph.
/// </summary>
namespace ILCompiler.DependencyAnalysisFramework
{
    // The event IDs here must not collide with the ones used by ReadyToRunPerfEventSource.cs
    [EventSource(Name = "Microsoft-ILCompiler-Perf")]
    class PerfEventSource : EventSource
    {
        [Event(1001, Level = EventLevel.Informational)]
        public void GraphProcessingStart() { WriteEvent(1001); }
        [Event(1002, Level = EventLevel.Informational)]
        public void GraphProcessingStop() { WriteEvent(1002); }

        [Event(1003, Level = EventLevel.Informational)]
        public void DependencyAnalysisStart() { WriteEvent(1003); }
        [Event(1004, Level = EventLevel.Informational)]
        public void DependencyAnalysisStop() { WriteEvent(1004); }

        [Event(1005, Level = EventLevel.Informational)]
        public void AddedNodeToMarkStack() { WriteEvent(1005); }

        public static PerfEventSource Log = new PerfEventSource();
    }
}
