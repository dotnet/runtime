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
using Xunit;

namespace Tracing.Tests.RundownValidation
{

    public class RundownValidation
    {
        [Fact]
        public static int TestEntryPoint()
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
            bool hasRuntimeStart = false;
            bool hasMethodDCStopInit = false;
            bool hasMethodDCStopComplete = false;
            bool hasLoaderModuleDCStop = false;
            bool hasLoaderDomainModuleDCStop = false;
            bool hasAssemblyModuleDCStop = false;
            bool hasMethodDCStopVerbose = false;
            bool hasMethodILToNativeMap = false;
            bool hasAppDomainDCStop = false;

            ClrRundownTraceEventParser rundownParser = new ClrRundownTraceEventParser(source);
            rundownParser.RuntimeStart += (eventData) => hasRuntimeStart = true;
            rundownParser.MethodDCStopInit += (eventData) => hasMethodDCStopInit = true;
            rundownParser.MethodDCStopComplete += (eventData) => hasMethodDCStopComplete = true;
            rundownParser.LoaderModuleDCStop += (eventData) => hasLoaderModuleDCStop = true;
            rundownParser.LoaderDomainModuleDCStop += (eventData) => hasLoaderDomainModuleDCStop = true;
            rundownParser.LoaderAssemblyDCStop += (eventData) => hasAssemblyModuleDCStop = true;
            rundownParser.MethodDCStopVerbose += (eventData) => hasMethodDCStopVerbose = true;
            rundownParser.MethodILToNativeMapDCStop += (eventData) => hasMethodILToNativeMap = true;
            rundownParser.LoaderAppDomainDCStop += (eventData) => hasAppDomainDCStop = true;
            return () =>
                hasRuntimeStart && hasMethodDCStopInit && hasMethodDCStopComplete &&
                hasLoaderModuleDCStop && hasLoaderDomainModuleDCStop && hasAssemblyModuleDCStop &&
                hasMethodDCStopVerbose && hasMethodILToNativeMap && hasAppDomainDCStop ? 100 : -1;
        };
    }
}
