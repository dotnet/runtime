// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Collections.Generic;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;

namespace Tracing.Tests.GCFinalizers
{
    public class ProviderValidation
    {
        public static int Main(string[] args)
        {
            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose),
                //GCKeyword (0x1): 0b1
                new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, 0b1)
            };

            return IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024, _DoesTraceContainEvents);
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            { "Microsoft-Windows-DotNETRuntime", -1 },
            { "Microsoft-Windows-DotNETRuntimeRundown", -1 },
            { "Microsoft-DotNETCore-SampleProfiler", -1 }
        };

        private static Action _eventGeneratingAction = () =>
        {
            for (int i = 0; i < 50; i++)
            {
                if (i % 10 == 0)
                    Logger.logger.Log($"Called GC.WaitForPendingFinalizers() {i} times...");
                ProviderValidation providerValidation = new ProviderValidation();
                providerValidation = null;
                GC.WaitForPendingFinalizers();
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        };

        private static Func<EventPipeEventSource, Func<int>> _DoesTraceContainEvents = (source) =>
        {
            int GCFinalizersEndEvents = 0;
            source.Clr.GCFinalizersStop += (eventData) => GCFinalizersEndEvents += 1;
            int GCFinalizersStartEvents = 0;
            source.Clr.GCFinalizersStart += (eventData) => GCFinalizersStartEvents += 1;
            return () => {
                Logger.logger.Log("Event counts validation");
                Logger.logger.Log("GCFinalizersEndEvents: " + GCFinalizersEndEvents);
                Logger.logger.Log("GCFinalizersStartEvents: " + GCFinalizersStartEvents);
                return GCFinalizersEndEvents >= 50 && GCFinalizersStartEvents >= 50 ? 100 : -1;
            };
        };
    }
}
