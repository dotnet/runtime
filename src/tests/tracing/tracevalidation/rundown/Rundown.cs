// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System.IO;
using System.Linq;
using Tracing.Tests.Common;

namespace Tracing.Tests
{
    public static class TraceValidationRundown
    {
        public static int Main(string[] args)
        {
            // Additional assemblies will be seen, but these are ones we must see
            string[] AssembliesExpected = new string[] {
                "Common",
                "Rundown", // this assembly
                "System.Runtime",
                "Microsoft.Diagnostics.Tracing.TraceEvent",
                "System.Private.CoreLib"
            };

            using (var netPerfFile = NetPerfFile.Create(args))
            {
                Console.WriteLine("\tStart: Enable tracing.");
                TraceControl.EnableDefault(netPerfFile.Path);
                Console.WriteLine("\tEnd: Enable tracing.\n");

                // Since all we care about is rundown, there is nothing to do there

                Console.WriteLine("\tStart: Disable tracing.");
                TraceControl.Disable();
                Console.WriteLine("\tEnd: Disable tracing.\n");

                Console.WriteLine("\tStart: Process the trace file.");

                var assembliesLoaded = new List<string>();
                int nonMatchingEventCount = 0;

                using (var trace = TraceEventDispatcher.GetDispatcherFromFileName(netPerfFile.Path))
                {
                    var rundownParser = new ClrRundownTraceEventParser(trace);

                    rundownParser.LoaderAssemblyDCStop += delegate(AssemblyLoadUnloadTraceData data)
                    {
                        var nameIndex = Array.IndexOf(data.PayloadNames, ("FullyQualifiedAssemblyName"));
                        if(nameIndex >= 0)
                        {
                            // Add the assembly name to a set to verify later
                            assembliesLoaded.Add(((string)data.PayloadValue(nameIndex)).Split(',')[0]);
                        }
                        else
                        {
                            nonMatchingEventCount++;
                        }
                    };

                    trace.Process();
                }
                Console.WriteLine("\tEnd: Processing events from file.\n");

                foreach (var expected in AssembliesExpected)
                {
                    Assert.True($"Assembly {expected} in loaded assemblies", assembliesLoaded.Any(loaded => String.Equals(loaded, expected, StringComparison.OrdinalIgnoreCase)));
                }
                Assert.Equal(nameof(nonMatchingEventCount), nonMatchingEventCount, 0);
            }

            return 100;
        }
    }
}
