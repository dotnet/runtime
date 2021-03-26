// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;

namespace Tracing.Tests.GCFinalizers
{
    public class ProviderValidation
    {
        public static int Main(string[] args)
        {
            var providers = new List<Provider>()
            {
                new Provider("Microsoft-DotNETCore-SampleProfiler"),
                //ThreadingKeyword (0x10000)
                new Provider("Microsoft-Windows-DotNETRuntime", 0x10000, EventLevel.Verbose)
            };
            
            var configuration = new SessionConfiguration(circularBufferSizeMB: 1024, format: EventPipeSerializationFormat.NetTrace,  providers: providers);
            return IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, configuration, _DoesTraceContainEvents);
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            { "Microsoft-Windows-DotNETRuntime", -1 },
            { "Microsoft-Windows-DotNETRuntimeRundown", -1 },
            { "Microsoft-DotNETCore-SampleProfiler", -1 }
        };

        private static Action _eventGeneratingAction = () => 
        {
            Task[] tasks = new Task[50];
            for (int i = 0; i < 50; i++)
            {
                tasks[i] = Task.Run(async () => {
                    long total = 0;
                    await Task.Delay(10);
                    var rnd = new Random();
                    for (int n = 1; n <= 100; n++)
                        total += rnd.Next(0, 100);
                    return total;
                });
            }
            Task.WaitAll(tasks);
        };

        private static Func<EventPipeEventSource, Func<int>> _DoesTraceContainEvents = (source) => 
        {
            int threadPoolEventsCount = 0;
            source.Clr.ThreadPoolWorkerThreadStart += (_) => threadPoolEventsCount += 1;
            source.Clr.ThreadPoolWorkerThreadWait += (_) => threadPoolEventsCount += 1;
            source.Clr.ThreadPoolWorkerThreadStop += (_) => threadPoolEventsCount += 1;
            source.Clr.ThreadPoolWorkerThreadAdjustmentSample += (_) => threadPoolEventsCount += 1;
            source.Clr.ThreadPoolWorkerThreadAdjustmentAdjustment += (_) => threadPoolEventsCount += 1;
            return () => {
                Logger.logger.Log("Event counts validation");
                return threadPoolEventsCount >= 50 ? 100 : -1;
            };
        };
    }
}
