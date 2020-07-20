// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Tracing.Tests.Common;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Tracing.Tests.EventSourceError
{
    // Regression test for https://github.com/dotnet/runtime/issues/38639 
    public class GCDumpTest
    {
        private static int _bulkTypeCount = 0;
        private static int _bulkNodeCount = 0;
        private static int _bulkEdgeCount = 0;
        private static int _bulkRootEdgeCount = 0;
        private static int _bulkRootStaticVarCount = 0;

        private static readonly ulong GC_HeapDump_Keyword = 0x100000UL;

        public static int Main(string[] args)
        {
            // This test validates that if an EventSource generates an error
            // during construction it gets emitted over EventPipe

            List<Provider> providers = new List<Provider>
            {
                new Provider("Microsoft-Windows-DotNETRuntime", eventLevel: EventLevel.Verbose, keywords: (ulong)ClrTraceEventParser.Keywords.GCHeapSnapshot)
            };

            var configuration = new SessionConfiguration(circularBufferSizeMB: 1024, format: EventPipeSerializationFormat.NetTrace,  providers: providers);
            return IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, configuration, _DoesRundownContainMethodEvents);
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            // This space intentionally left blank
        };

        private static Action _eventGeneratingAction = () =>
        {
            // This space intentionally left blank
        };

        private static Func<EventPipeEventSource, Func<int>> _DoesRundownContainMethodEvents = (source) =>
        {
            source.Clr.TypeBulkType += (GCBulkTypeTraceData data) =>
            {
                _bulkTypeCount += data.Count;
            };

            source.Clr.GCBulkNode += delegate (GCBulkNodeTraceData data)
            {
                _bulkNodeCount += data.Count;
            };

            source.Clr.GCBulkEdge += (GCBulkEdgeTraceData data) =>
            {
                _bulkEdgeCount += data.Count;
            };

            source.Clr.GCBulkRootEdge += (GCBulkRootEdgeTraceData data) =>
            {
                _bulkRootEdgeCount += data.Count;
            };

            source.Clr.GCBulkRootStaticVar += (GCBulkRootStaticVarTraceData data) =>
            {
                _bulkRootStaticVarCount += data.Count;
            };

            return () => 
            {
                // Hopefully it is low enough to be resilient to changes in the runtime
                // and high enough to catch issues. There should be between hundreds and thousands
                // for each, but the number is variable and the point of the test is to verify
                // that we get any events at all.
                if (_bulkTypeCount > 50
                     && _bulkNodeCount > 50
                     && _bulkEdgeCount > 50
                     && _bulkRootEdgeCount > 50
                     && _bulkRootStaticVarCount > 50)
                {
                    return 100;
                }


                Console.WriteLine($"Test failed due to missing GC heap events.");
                Console.WriteLine($"_bulkTypeCount =          {_bulkTypeCount}");
                Console.WriteLine($"_bulkNodeCount =          {_bulkNodeCount}");
                Console.WriteLine($"_bulkEdgeCount =          {_bulkEdgeCount}");
                Console.WriteLine($"_bulkRootEdgeCount =      {_bulkRootEdgeCount}");
                Console.WriteLine($"_bulkRootStaticVarCount = {_bulkRootStaticVarCount}");
                return -1;
            };
        };
    }
}
