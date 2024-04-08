// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Tracing.Tests.Common;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Xunit;

namespace Tracing.Tests.EventSourceError
{
    // Regression test for https://github.com/dotnet/runtime/issues/38639 
    public class GCDumpTest
    {
        private static bool _seenGCStart = false;
        private static bool _seenGCStop = false;
        private static int _bulkTypeCount = 0;
        private static int _bulkNodeCount = 0;
        private static int _bulkEdgeCount = 0;
        private static int _bulkRootEdgeCount = 0;
        private static int _bulkRootStaticVarCount = 0;

        private static readonly ulong GC_HeapDump_Keyword = 0x100000UL;
        private static ManualResetEvent _gcStopReceived = new ManualResetEvent(false);

        [Fact]
        public static int TestEntryPoint()
        {
            // This test validates that if an EventSource generates an error
            // during construction it gets emitted over EventPipe

            List<EventPipeProvider> providers = new List<EventPipeProvider>
            {
                new EventPipeProvider("Microsoft-Windows-DotNETRuntime", eventLevel: EventLevel.Verbose, keywords: (long)ClrTraceEventParser.Keywords.GCHeapSnapshot)
            };

            bool enableRundown = TestLibrary.Utilities.IsNativeAot? false: true;
            return IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024, _DoesRundownContainMethodEvents, enableRundownProvider: enableRundown);
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            // This space intentionally left blank
        };

        private static Action _eventGeneratingAction = () =>
        {
            // Wait up to 10 seconds to receive GCStop event.
            _gcStopReceived.WaitOne(10000);
        };

        private static Func<EventPipeEventSource, Func<int>> _DoesRundownContainMethodEvents = (source) =>
        {
            source.Clr.GCStart += (GCStartTraceData data) =>
            {
                _seenGCStart = true;
            };

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

            source.Clr.GCStop += (GCEndTraceData data) =>
            {
                _seenGCStop = true;
                _gcStopReceived.Set();
            };

            return () => 
            {
                // Hopefully it is low enough to be resilient to changes in the runtime
                // and high enough to catch issues. There should be between hundreds and thousands
                // for each, but the number is variable and the point of the test is to verify
                // that we get any events at all.
                
                if (_seenGCStart
                     && _seenGCStop
                     && _bulkTypeCount > 50
                     && _bulkNodeCount > 50
                     && _bulkEdgeCount > 50)
                {
                    // Native AOT hasn't yet implemented statics. Hence _bulkRootStaticVarCount is zero and _bulkRootEdgeCount can be low
                    if ((TestLibrary.Utilities.IsNativeAot && _bulkRootEdgeCount > 20) || (_bulkRootStaticVarCount > 50 && _bulkRootEdgeCount > 50))
                        return 100;
                }

                Console.WriteLine($"Test failed due to missing GC heap events.");
                Console.WriteLine($"_seenGCStart =            {_seenGCStart}");
                Console.WriteLine($"_seenGCStop =             {_seenGCStop}");
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
