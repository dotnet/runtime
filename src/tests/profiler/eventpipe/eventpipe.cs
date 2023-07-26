// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Profiler.Tests;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;

namespace EventPipeTests
{
    class EventPipe
    {
        static readonly Guid EventPipeWritingProfilerGuid = new Guid("2726B5B4-3F88-462D-AEC0-4EFDC8D7B921");

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest();
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "EventPipeBasic",
                                          profilerClsid: EventPipeWritingProfilerGuid);
        }

        public static int RunTest()
        {
            ArrayPool<char>.Shared.Rent(1); // workaround for https://github.com/dotnet/runtime/issues/1892

            bool success = true;
            int allTypesEventCount = 0;
            int arrayTypeEventCount = 0;
            int emptyEventCount = 0;
            int simpleEventCount = 0;
            try
            {
                List<EventPipeProvider> providers = new List<EventPipeProvider>
                {
                    new EventPipeProvider("MySuperAwesomeEventPipeProvider", EventLevel.Verbose)
                };

                using (EventPipeSession session = ProfilerControlHelpers.AttachEventPipeSessionToSelf(providers))
                {
                    using (EventPipeSession session2 = ProfilerControlHelpers.AttachEventPipeSessionToSelf(providers))
                    {
                        // Trigger multiple session logic
                        Console.WriteLine("Session 2 opened");
                        TriggerMethod();


                        var source2 = new EventPipeEventSource(session2.EventStream);
                        Task.Run(() => source2.Process());
                    }

                    ManualResetEvent allEventsReceivedEvent = new ManualResetEvent(false);

                    var source = new EventPipeEventSource(session.EventStream);
                    source.Dynamic.All += (TraceEvent traceEvent) =>
                    {
                        if (traceEvent.ProviderName != "MySuperAwesomeEventPipeProvider")
                        {
                            return;
                        }

                        if (traceEvent.EventName == "AllTypesEvent")
                        {
                            success &= ValidateAllTypesEvent(traceEvent);
                            ++allTypesEventCount;
                        }
                        else if (traceEvent.EventName == "EmptyEvent")
                        {
                            success &= ValidateEmptyEvent(traceEvent);
                            ++emptyEventCount;
                        }
                        else if(traceEvent.EventName == "SimpleEvent")
                        {
                            success &= ValidateSimpleEvent(traceEvent, simpleEventCount);
                            ++simpleEventCount;
                        }
                        else if(traceEvent.EventName == "ArrayTypeEvent")
                        {
                            success &= ValidateArrayTypeEvent(traceEvent);
                            ++arrayTypeEventCount;
                        }

                        if (AllEventsReceived(allTypesEventCount, arrayTypeEventCount, emptyEventCount, simpleEventCount))
                        {
                            allEventsReceivedEvent.Set();
                        }
                    };

                    Task processTask = Task.Run(() =>
                    {
                        source.Process();
                    });

                    // The events are fired in the JITCompilationStarted callback for TriggerMethod,
                    // so by the time we are here, all events should be fired.
                    session.Stop();

                    allEventsReceivedEvent.WaitOne(TimeSpan.FromSeconds(90));
                    processTask.Wait();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"Exception {e.Message} when trying to attach");
                success = false;
            }

            if (success && AllEventsReceived(allTypesEventCount, arrayTypeEventCount, emptyEventCount, simpleEventCount))
            {
                return 100;
            }
            else
            {
                Console.WriteLine("Test validation failed (EventPipeClient.exe)");
                Console.WriteLine($"    success={success}");
                Console.WriteLine($"    allTypesEventCount={allTypesEventCount} ");
                Console.WriteLine($"    arrayTypeEventCount={arrayTypeEventCount} ");
                Console.WriteLine($"    emptyEventCount={emptyEventCount}");
                Console.WriteLine($"    simpleEventCount={simpleEventCount}");
                return -1;
            }
        }

        public static bool AllEventsReceived(int allTypesEventCount, int arrayTypeEvent, int emptyEventCount, int simpleEventCount)
        {
            return allTypesEventCount == 1
                    && arrayTypeEvent == 1
                    && emptyEventCount == 10
                    && simpleEventCount == 10000;
        }

        public static bool ValidateAllTypesEvent(TraceEvent traceEvent)
        {
            if (traceEvent.EventName != "AllTypesEvent")
            {
                Console.WriteLine("Got an event that didn't match while parsing AllTypesEvent");
                return false;
            }

            if ((int)traceEvent.ID != 1)
            {
                Console.WriteLine("AllTypesEvent ID != 1");
                return false;
            }

            const int expectedPayload = 15;
            string[] payloadNames = traceEvent.PayloadNames;
            if (payloadNames.Length != expectedPayload)
            {
                Console.WriteLine($"AllTypesEvent payloadNames.Length={payloadNames.Length} instead of {expectedPayload} as expected...");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_BOOLEAN, L"Boolean" }
            // CopyToBuffer<bool>(buffer, true, &offset);
            bool b = (bool)traceEvent.PayloadValue(0);
            if (payloadNames[0] != "Boolean" || b != true)
            {
                Console.WriteLine($"Argument 0 failed to parse, got {b}");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_CHAR, L"Char" }
            // CopyToBuffer<WCHAR>(buffer, L'A', &offset);
            char ch = (char)traceEvent.PayloadValue(1);
            if (payloadNames[1] != "Char" || ch != 'A')
            {
                Console.WriteLine($"Argument 1 failed to parse, got {ch}");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_SBYTE, L"SByte" }
            // CopyToBuffer<int8_t>(buffer, -124, &offset);
            SByte s = (SByte)traceEvent.PayloadValue(2);
            if (payloadNames[2] != "SByte" || s != -124)
            {
                Console.WriteLine($"Argument 2 failed to parse, got {s}");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_BYTE, L"Byte" }
            // CopyToBuffer<uint8_t>(buffer, 125, &offset);
            Byte by = (Byte)traceEvent.PayloadValue(3);
            if (payloadNames[3] != "Byte" || by != 125)
            {
                Console.WriteLine($"Argument 3 failed to parse, got {by}");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_INT16, L"Int16" }
            // CopyToBuffer<int16_t>(buffer, -35, &offset);
            // For some reason TraceEvent parses this as an int32 instead of int16
            Int32 i16 = (Int32)traceEvent.PayloadValue(4);
            if (payloadNames[4] != "Int16" || i16 != -35)
            {
                Console.WriteLine($"Argument 4 failed to parse, got {i16}");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_UINT16, L"UInt16" }
            // CopyToBuffer<uint16_t>(buffer, 98, &offset);
            UInt16 ui16 = (UInt16)traceEvent.PayloadValue(5);
            if (payloadNames[5] != "UInt16" || ui16 != 98)
            {
                Console.WriteLine($"Argument 5 failed to parse, got {ui16}");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_INT32, L"Int32" }
            // CopyToBuffer<int32_t>(buffer, -560, &offset);
            Int32 i32 = (Int32)traceEvent.PayloadValue(6);
            if (payloadNames[6] != "Int32" || i32 != -560)
            {
                Console.WriteLine($"Argument 6 failed to parse, got {i32}");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_UINT32, L"UInt32" }
            // CopyToBuffer<uint32_t>(buffer, 561, &offset);
            UInt32 ui32 = (UInt32)traceEvent.PayloadValue(7);
            if (payloadNames[7] != "UInt32" || ui32 != 561)
            {
                Console.WriteLine($"Argument 7 failed to parse, got {ui32}");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_INT64, L"Int64" }
            // CopyToBuffer<int64_t>(buffer, 2147483648LL, &offset);
            Int64 i64 = (Int64)traceEvent.PayloadValue(8);
            if (payloadNames[8] != "Int64" || i64 != 2147483648L)
            {
                Console.WriteLine($"Argument 8 failed to parse, got {i64}");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_UINT64, L"UInt64" }
            // CopyToBuffer<uint64_t>(buffer, 2147483649LL, &offset);
            UInt64 ui64 =  (UInt64)traceEvent.PayloadValue(9);
            if (payloadNames[9] != "UInt64" || ui64 != 2147483649L)
            {
                Console.WriteLine($"Argument 9 failed to parse, got {ui64}");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_SINGLE, L"Single" }
            // CopyToBuffer<float>(buffer, 3.0f, &offset);
            float fl = (float)traceEvent.PayloadValue(10);
            if (payloadNames[10] != "Single" || fl != 3.0f)
            {
                Console.WriteLine($"Argument 10 failed to parse, got {fl}");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_DOUBLE, L"Double" }
            // CopyToBuffer<double>(buffer, 3.023, &offset);
            double d = (double)traceEvent.PayloadValue(11);
            if (payloadNames[11] != "Double" || d != 3.023)
            {
                Console.WriteLine("Argument 11 failed to parse");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_GUID, L"Guid" }
            // CopyToBuffer<GUID>(buffer, { 0x176FBED1,0xA55C,0x4796, { 0x98,0xCA,0xA9,0xDA,0x0E,0xF8,0x83,0xE7 }}, &offset);
            Guid guid = (Guid)traceEvent.PayloadValue(12);
            if (payloadNames[12] != "Guid" || guid != new Guid("176FBED1-A55C-4796-98CA-A9DA0EF883E7"))
            {
                Console.WriteLine($"Argument 12 failed to parse, got {guid}");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_STRING, L"String" }
            // CopyToBuffer<LPWSTR>(buffer, L"Hello, this is a string!", &offset);
            string str = (string)traceEvent.PayloadValue(13);
            if (payloadNames[13] != "String" || str != "Hello, this is a string!")
            {
                Console.WriteLine($"Argument 13 failed to parse, got {str}");
                return false;
            }

            // // { COR_PRF_EVENTPIPE_DATETIME, L"DateTime" }
            // CopyToBuffer<uint64_t>(buffer, 132243707160000000ULL, &offset);
            DateTime dt = ((DateTime)traceEvent.PayloadValue(14)).ToUniversalTime();
            if (payloadNames[14] != "DateTime" || dt != DateTime.Parse("1/24/2020 8:18:36 PM", CultureInfo.InvariantCulture))
            {
                Console.WriteLine($"Argument 14 failed to parse, got {dt}");
                return false;
            }

            return true;
        }

        public static bool ValidateArrayTypeEvent(TraceEvent traceEvent)
        {
            if (traceEvent.EventName != "ArrayTypeEvent")
            {
                Console.WriteLine("Got an event that didn't match while parsing EmptyEvent");
                return false;
            }

            if ((int)traceEvent.ID != 3)
            {
                Console.WriteLine("ArrayTypeEvent ID != 3");
                return false;
            }

            // Could be 0 (old TraceEvent doesn't parse array types)
            if (traceEvent.PayloadNames.Length == 0)
            {
                return true;
            }

            // Or could be 1 if TraceEvent gets updated
            if (traceEvent.PayloadNames.Length != 1)
            {
                Console.WriteLine($"Expected 1 event arg for ArrayTypeEvent but got {traceEvent.PayloadNames.Length}.");
                return false;
            }

            int[] intArray = (int[])traceEvent.PayloadValue(0);
            if (traceEvent.PayloadNames[0] != "IntArray"
                || !Enumerable.SequenceEqual(intArray, Enumerable.Range(1, 100).OrderByDescending(x => x)))
            {
                Console.WriteLine($"IntArray failed to parse, got {intArray}");
                return false;
            }

            return true;
        }

        public static bool ValidateEmptyEvent(TraceEvent traceEvent)
        {
            if (traceEvent.EventName != "EmptyEvent")
            {
                Console.WriteLine("Got an event that didn't match while parsing EmptyEvent");
                return false;
            }

            if ((int)traceEvent.ID != 2032)
            {
                Console.WriteLine("EmptyEvent ID != 2032");
                return false;
            }

            const int expectedPayload = 0;
            string[] payloadNames = traceEvent.PayloadNames;
            if (payloadNames.Length != expectedPayload)
            {
                Console.WriteLine($"EmptyEvent payloadNames.Length={payloadNames.Length} instead of {expectedPayload} as expected...");
                return false;
            }

            return true;
        }

        public static bool ValidateSimpleEvent(TraceEvent traceEvent, int eventCounter)
        {
            if (traceEvent.EventName != "SimpleEvent")
            {
                Console.WriteLine("Got an event that didn't match while parsing SimpleEvent");
                return false;
            }


            if ((int)traceEvent.ID != 2)
            {
                Console.WriteLine("SimpleEvent ID != 2");
                return false;
            }

            const int expectedPayload = 1;
            string[] payloadNames = traceEvent.PayloadNames;
            if (payloadNames.Length != expectedPayload)
            {
                Console.WriteLine($"SimpleEvent payloadNames.Length={payloadNames.Length} instead of {expectedPayload} as expected...");
                return false;
            }

            int eventValue = (int)traceEvent.PayloadValue(0);
            if (eventValue != eventCounter)
            {
                Console.WriteLine($"SimpleEvent got an out of order event expected={eventCounter} actual={eventValue}.");
                return false;
            }

            return true;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void TriggerMethod()
        {
            Console.WriteLine("This method being jitted should trigger events firing...");
        }
    }
}
