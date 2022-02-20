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
#if (UPDATED_NETCORE_CLIENT == true)
using Microsoft.Diagnostics.NETCore.Client;
#else
using Microsoft.Diagnostics.Tools.RuntimeClient;
#endif
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
#if (UPDATED_NETCORE_CLIENT == true)
            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose)
            };

            return IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024, _DoesRundownContainMethodEvents);
#else
            var providers = new List<Provider>()
            {
                new Provider("Microsoft-DotNETCore-SampleProfiler")
            };

            var configuration = new SessionConfiguration(circularBufferSizeMB: 1024, format: EventPipeSerializationFormat.NetTrace,  providers: providers);
            return IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, configuration, _DoesRundownContainMethodEvents);
#endif
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
