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
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Tracing.Tests.RundownValidation
{

    public class RundownValidation
    {
        public static int Main(string[] args)
        {
            // This test validates that the rundown events are present
            // and that the rundown contains the necessary events to get
            // symbols in a nettrace file.

            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose)
            };

            return IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024, _DoesRundownContainMethodEvents);
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            { "Microsoft-Windows-DotNETRuntimeRundown", -1 }
        };

        // We only care about rundown so skip generating any events.
        private static Action _eventGeneratingAction = () => { };

        private static Func<EventPipeEventSource, Func<int>> _DoesRundownContainMethodEvents = (source) =>
        {
            bool hasMethodDCStopVerbose = false;
            bool hasMethodILToNativeMap = false;
            ClrRundownTraceEventParser rundownParser = new ClrRundownTraceEventParser(source);
            rundownParser.MethodDCStopVerbose += (eventData) => hasMethodDCStopVerbose = true;
            rundownParser.MethodILToNativeMapDCStop += (eventData) => hasMethodILToNativeMap = true;
            return () => hasMethodDCStopVerbose && hasMethodILToNativeMap ? 100 : -1;
        };
    }
}
