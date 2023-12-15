// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;
using Microsoft.Diagnostics.NETCore.Client;
using Xunit;

namespace Tracing.Tests.GCEvents
{
    public class ProviderValidation
    {
        [Fact]
        public static int TestEntryPoint()
        {
            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose),
                //GCKeyword (0x1): 0b1
                new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, 0b1)
            };

            bool enableRundown = TestLibrary.Utilities.IsNativeAot? false: true;
            Dictionary<string, ExpectedEventCount> _expectedEventCounts = TestLibrary.Utilities.IsNativeAot? _expectedEventCountsNativeAOT: _expectedEventCountsCoreCLR;
            
            return IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024, _DoesTraceContainEvents, enableRundownProvider:enableRundown);
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCountsCoreCLR = new Dictionary<string, ExpectedEventCount>()
        {
            { "Microsoft-Windows-DotNETRuntime", -1 },
            { "Microsoft-Windows-DotNETRuntimeRundown", -1 },
            { "Microsoft-DotNETCore-SampleProfiler", -1 }
        };

        private static Dictionary<string, ExpectedEventCount> _expectedEventCountsNativeAOT = new Dictionary<string, ExpectedEventCount>()
        {
            { "Microsoft-Windows-DotNETRuntime", -1 }
        };


        private static Action _eventGeneratingAction = () =>
        {
            for (int i = 0; i < 50; i++)
            {
                if (i % 10 == 0)
                    Logger.logger.Log($"Called GC.Collect() {i} times...");
                ProviderValidation providerValidation = new ProviderValidation();
                providerValidation = null;
                GC.Collect();
            }
        };

        private static Func<EventPipeEventSource, Func<int>> _DoesTraceContainEvents = (source) =>
        {
            int GCStartEvents = 0;
            int GCEndEvents = 0;
            source.Clr.GCStart += (eventData) => GCStartEvents += 1;
            source.Clr.GCStop += (eventData) => GCEndEvents += 1;

            int GCRestartEEStartEvents = 0;
            int GCRestartEEStopEvents = 0;
            source.Clr.GCRestartEEStart += (eventData) => GCRestartEEStartEvents += 1;
            source.Clr.GCRestartEEStop += (eventData) => GCRestartEEStopEvents += 1;

            int GCSuspendEEEvents = 0;
            int GCSuspendEEEndEvents = 0;
            source.Clr.GCSuspendEEStart += (eventData) => GCSuspendEEEvents += 1;
            source.Clr.GCSuspendEEStop += (eventData) => GCSuspendEEEndEvents += 1;

            return () => {
                Logger.logger.Log("Event counts validation");

                Logger.logger.Log("GCStartEvents: " + GCStartEvents);
                Logger.logger.Log("GCEndEvents: " + GCEndEvents);
                bool GCStartStopResult = GCStartEvents >= 50 && GCEndEvents >= 50 && Math.Abs(GCStartEvents - GCEndEvents) <=2;
                Logger.logger.Log("GCStartStopResult check: " + GCStartStopResult);

                Logger.logger.Log("GCRestartEEStartEvents: " + GCRestartEEStartEvents);
                Logger.logger.Log("GCRestartEEStopEvents: " + GCRestartEEStopEvents);
                bool GCRestartEEStartStopResult = GCRestartEEStartEvents >= 50 && GCRestartEEStopEvents >= 50;
                Logger.logger.Log("GCRestartEEStartStopResult check: " + GCRestartEEStartStopResult);

                Logger.logger.Log("GCSuspendEEEvents: " + GCSuspendEEEvents);
                Logger.logger.Log("GCSuspendEEEndEvents: " + GCSuspendEEEndEvents);
                bool GCSuspendEEStartStopResult = GCSuspendEEEvents >= 50 && GCSuspendEEEndEvents >= 50;
                Logger.logger.Log("GCSuspendEEStartStopResult check: " + GCSuspendEEStartStopResult);

                return GCStartStopResult && GCRestartEEStartStopResult && GCSuspendEEStartStopResult ? 100 : -1;
            };
        };
    }
}
