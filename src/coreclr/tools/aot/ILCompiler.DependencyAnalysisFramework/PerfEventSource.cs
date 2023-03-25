// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;

/// <summary>
/// Performance events specific to the dependency graph.
/// </summary>
namespace ILCompiler.DependencyAnalysisFramework
{
    [EventSource(Name = "Microsoft-ILCompiler-Graph-Perf")]
    public class PerfEventSource : EventSource
    {
        private PerfEventSource() { }

        public static PerfEventSource Log = new PerfEventSource();

        public struct StartStopEvents : IDisposable
        {
            private Action _stopAction;

            private StartStopEvents(Action stopAction)
            {
                _stopAction = stopAction;
            }

            public void Dispose()
            {
                _stopAction?.Invoke();
            }

            public static StartStopEvents GraphProcessingEvents()
            {
                if (!Log.IsEnabled())
                    return default(StartStopEvents);

                Log.GraphProcessingStart();
                return new StartStopEvents(Log.GraphProcessingStop);
            }

            public static StartStopEvents DependencyAnalysisEvents()
            {
                if (!Log.IsEnabled())
                    return default(StartStopEvents);

                Log.DependencyAnalysisStart();
                return new StartStopEvents(Log.DependencyAnalysisStop);
            }
        }

        // The event IDs here must not collide with the ones used by ReadyToRunPerfEventSource.cs
        [Event(1001, Level = EventLevel.Informational)]
        private void GraphProcessingStart() { WriteEvent(1001); }
        [Event(1002, Level = EventLevel.Informational)]
        private void GraphProcessingStop() { WriteEvent(1002); }

        [Event(1003, Level = EventLevel.Informational)]
        private void DependencyAnalysisStart() { WriteEvent(1003); }
        [Event(1004, Level = EventLevel.Informational)]
        private void DependencyAnalysisStop() { WriteEvent(1004); }

        [Event(1005, Level = EventLevel.Informational)]
        public void AddedNodeToMarkStack() { WriteEvent(1005); }
    }

}
