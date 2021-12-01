// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;

/// <summary>
/// Performance events specific to ReadyToRun.
/// </summary>
namespace ILCompiler
{
    [EventSource(Name = "Microsoft-ILCompiler-Perf")]
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

            public static void CommandLineProcessingStart()
            {
                if (Log.IsEnabled())
                {
                    Log.CommandLineProcessingStart();
                }
            }

            public static void CommandLineProcessingStop()
            {
                if (Log.IsEnabled())
                {
                    Log.CommandLineProcessingStop();
                }
            }

            public static StartStopEvents LoadingEvents()
            {
                if (!Log.IsEnabled())
                    return new StartStopEvents();

                Log.LoadingStart();
                return new StartStopEvents(Log.LoadingStop);
            }

            public static StartStopEvents EmittingEvents()
            {
                if (!Log.IsEnabled())
                    return new StartStopEvents();

                Log.EmittingStart();
                return new StartStopEvents(Log.EmittingStop);
            }

            public static StartStopEvents CompilationEvents()
            {
                if (!Log.IsEnabled())
                    return new StartStopEvents();

                Log.CompilationStart();
                return new StartStopEvents(Log.CompilationStop);
            }

            public static StartStopEvents JitEvents()
            {
                if (!Log.IsEnabled())
                    return new StartStopEvents();

                Log.JitStart();
                return new StartStopEvents(Log.JitStop);
            }

            public static StartStopEvents JitMethodEvents()
            {
                if (!Log.IsEnabled())
                    return new StartStopEvents();

                Log.JitMethodStart();
                return new StartStopEvents(Log.JitMethodStop);
            }
        }

        // The event IDs here must not collide with the ones used by DependencyAnalysis' PerfEventSource
        [Event(1, Level = EventLevel.Informational)]
        private void LoadingStart() { WriteEvent(1); }
        [Event(2, Level = EventLevel.Informational)]
        private void LoadingStop() { WriteEvent(2); }

        [Event(3, Level = EventLevel.Informational)]
        private void EmittingStart() { WriteEvent(3); }
        [Event(4, Level = EventLevel.Informational)]
        private void EmittingStop() { WriteEvent(4); }

        [Event(5, Level = EventLevel.Informational)]
        private void CompilationStart() { WriteEvent(5); }
        [Event(6, Level = EventLevel.Informational)]
        private void CompilationStop() { WriteEvent(6); }

        [Event(7, Level = EventLevel.Informational)]
        private void JitStart() { WriteEvent(7); }
        [Event(8, Level = EventLevel.Informational)]
        private void JitStop() { WriteEvent(8); }

        [Event(9, Level = EventLevel.Informational)]
        private void JitMethodStart() { WriteEvent(9); }
        [Event(10, Level = EventLevel.Informational)]
        private void JitMethodStop() { WriteEvent(10); }
        [Event(11, Level = EventLevel.Informational)]
        private void CommandLineProcessingStart() { WriteEvent(11); }
        [Event(12, Level = EventLevel.Informational)]
        private void CommandLineProcessingStop() { WriteEvent(12); }
    }
}
