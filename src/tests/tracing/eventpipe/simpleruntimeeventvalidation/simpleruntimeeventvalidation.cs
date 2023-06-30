// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;
using Microsoft.Diagnostics.NETCore.Client;

namespace Tracing.Tests.SimpleRuntimeEventValidation
{
    public class RuntimeEventValidation
    {
        public static int Main()
        {
            // This test validates GC and Exception events in the runtime
            var ret = IpcTraceTest.RunAndValidateEventCounts(
                // Validation is done with _DoesTraceContainEvents
                new Dictionary<string, ExpectedEventCount>(){{ "Microsoft-Windows-DotNETRuntime", -1 }},
                _eventGeneratingActionForGC, 
                //GCKeyword (0x1): 0b1
                new List<EventPipeProvider>(){new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, 0b1)}, 
                1024, _DoesTraceContainEvents, enableRundownProvider:false);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Run the 2nd test scenario only if the first one passes
            if(ret== 100)
            {
                ret = IpcTraceTest.RunAndValidateEventCounts(
                    // Validate the exception type and message after the below issue is fixed. For now, check that the event is fired
                    // https://github.com/dotnet/runtime/issues/87978
                    new Dictionary<string, ExpectedEventCount>(){{ "Microsoft-Windows-DotNETRuntime", 1000 }}, 
                    _eventGeneratingActionForExceptions, 
                    //ExceptionKeyword (0x8000): 0b1000_0000_0000_0000
                    new List<EventPipeProvider>(){new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Warning, 0b1000_0000_0000_0000)}, 
                    1024, enableRundownProvider:false);
            }

            if (ret < 0)
                return ret;
            else
                return 100;
        }

        private static Action _eventGeneratingActionForGC = () => 
        {
            for (int i = 0; i < 50; i++)
            {
                if (i % 10 == 0)
                    Logger.logger.Log($"Called GC.Collect() {i} times...");
                RuntimeEventValidation eventValidation = new RuntimeEventValidation();
                eventValidation = null;
                GC.Collect();
            }
        };

        private static Action _eventGeneratingActionForExceptions = () => 
        {
            for (int i = 0; i < 1000; i++)
            {
                if (i % 100 == 0)
                    Logger.logger.Log($"Thrown an exception {i} times...");
                try
                {
                    throw new ArgumentNullException("Throw ArgumentNullException");
                }
                catch (Exception e)
                {
                    //Do nothing
                }
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
