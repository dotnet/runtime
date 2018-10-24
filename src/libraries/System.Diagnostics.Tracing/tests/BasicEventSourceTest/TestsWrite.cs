// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using Xunit;
using System.Diagnostics;

namespace BasicEventSourceTests
{
    internal enum Color { Red, Blue, Green };
    internal enum ColorUInt32 : uint { Red, Blue, Green };
    internal enum ColorByte : byte { Red, Blue, Green };
    internal enum ColorSByte : sbyte { Red, Blue, Green };
    internal enum ColorInt16 : short { Red, Blue, Green };
    internal enum ColorUInt16 : ushort { Red, Blue, Green };
    internal enum ColorInt64 : long { Red, Blue, Green };
    internal enum ColorUInt64 : ulong { Red, Blue, Green };

    public partial class TestsWrite
    {

        [EventData]
        private struct PartB_UserInfo
        {
            public string UserName { get; set; }
        }

        /// <summary>
        /// Tests the EventSource.Write[T] method (can only use the self-describing mechanism).  
        /// Tests the EventListener code path
        /// </summary>
        [Fact]
        [ActiveIssue("dotnet/corefx #19455", TargetFrameworkMonikers.NetFramework)]
        public void Test_Write_T_EventListener()
        {
            using (var listener = new EventListenerListener())
            {
                Test_Write_T(listener);
            }
        }

        /// <summary>
        /// Tests the EventSource.Write[T] method (can only use the self-describing mechanism).  
        /// Tests the EventListener code path using events instead of virtual callbacks.
        /// </summary>
        [Fact]
        [ActiveIssue("dotnet/corefx #19455", TargetFrameworkMonikers.NetFramework)]
        public void Test_Write_T_EventListener_UseEvents()
        {
            Test_Write_T(new EventListenerListener(true));
        }

        /// <summary>
        /// Te
        /// </summary>
        /// <param name="listener"></param>
        private void Test_Write_T(Listener listener)
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");

            using (var logger = new EventSource("EventSourceName"))
            {
                var tests = new List<SubTest>();
                /*************************************************************************/
                tests.Add(new SubTest("Write/Basic/String",
                    delegate ()
                    {
                        logger.Write("Greeting", new { msg = "Hello, world!" });
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("Greeting", evt.EventName);

                        Assert.Equal(evt.PayloadValue(0, "msg"), "Hello, world!");
                    }));
                /*************************************************************************/
                decimal myMoney = 300;
                tests.Add(new SubTest("Write/Basic/decimal",
                    delegate ()
                    {
                        logger.Write("Decimal", new { money = myMoney });
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("Decimal", evt.EventName);

                        var eventMoney = evt.PayloadValue(0, "money");
                        // TOD FIX ME - Fix TraceEvent to return decimal instead of double.
                        //Assert.Equal((decimal)eventMoney, (decimal)300);
                    }));
                /*************************************************************************/
                DateTime now = DateTime.Now;
                tests.Add(new SubTest("Write/Basic/DateTime",
                    delegate ()
                    {
                        logger.Write("DateTime", new { nowTime = now });
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("DateTime", evt.EventName);
                        var eventNow = evt.PayloadValue(0, "nowTime");

                        Assert.Equal(eventNow, now);
                    }));
                /*************************************************************************/
                byte[] byteArray = { 0, 1, 2, 3 };
                tests.Add(new SubTest("Write/Basic/byte[]",
                    delegate ()
                    {
                        logger.Write("Bytes", new { bytes = byteArray });
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("Bytes", evt.EventName);

                        var eventArray = evt.PayloadValue(0, "bytes");
                        Array.Equals(eventArray, byteArray);
                    }));
                /*************************************************************************/
                int? nullableInt = 12;
                tests.Add(new SubTest("Write/Basic/int?/12",
                    delegate ()
                    {
                        logger.Write("Int12", new { nInteger = nullableInt });
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("Int12", evt.EventName);

                        var payload = evt.PayloadValue(0, "nInteger");
                        Assert.Equal(nullableInt, TestUtilities.UnwrapNullable<int>(payload));
                    }));
                /*************************************************************************/
                int? nullableInt2 = null;
                tests.Add(new SubTest("Write/Basic/int?/null",
                    delegate ()
                    {
                        logger.Write("IntNull", new { nInteger = nullableInt2 });
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("IntNull", evt.EventName);

                        var payload = evt.PayloadValue(0, "nInteger");
                        Assert.Equal(nullableInt2, TestUtilities.UnwrapNullable<int>(payload));
                    }));
                ///*************************************************************************/
                DateTime? nullableDate = DateTime.Now;
                tests.Add(new SubTest("Write/Basic/DateTime?/Now",
                    delegate ()
                    {
                        logger.Write("DateTimeNow", new { nowTime = nullableDate });
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("DateTimeNow", evt.EventName);

                        var payload = evt.PayloadValue(0, "nowTime");
                        Assert.Equal(nullableDate, TestUtilities.UnwrapNullable<DateTime>(payload));
                    }));
                /*************************************************************************/
                DateTime? nullableDate2 = null;
                tests.Add(new SubTest("Write/Basic/DateTime?/Null",
                    delegate ()
                    {
                        logger.Write("DateTimeNull", new { nowTime = nullableDate2 });
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("DateTimeNull", evt.EventName);

                        var payload = evt.PayloadValue(0, "nowTime");
                        Assert.Equal(nullableDate2, TestUtilities.UnwrapNullable<DateTime>(payload));
                    }));
                /*************************************************************************/
                tests.Add(new SubTest("Write/Basic/PartBOnly",
                    delegate ()
                    {
                        // log just a PartB
                        logger.Write("UserInfo", new EventSourceOptions { Keywords = EventKeywords.None },
                                              new { _1 = new PartB_UserInfo { UserName = "Someone Else" } });
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("UserInfo", evt.EventName);

                        var structValue = evt.PayloadValue(0, "PartB_UserInfo");
                        var structValueAsDictionary = structValue as IDictionary<string, object>;
                        Assert.NotNull(structValueAsDictionary);
                        Assert.Equal(structValueAsDictionary["UserName"], "Someone Else");
                    }));

                /*************************************************************************/
                tests.Add(new SubTest("Write/Basic/PartBAndC",
                    delegate ()
                    {
                        // log a PartB and a PartC
                        logger.Write("Duration", new EventSourceOptions { Keywords = EventKeywords.None },
                                              new { _1 = new PartB_UserInfo { UserName = "Myself" }, msec = 10 });
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("Duration", evt.EventName);

                        var structValue = evt.PayloadValue(0, "PartB_UserInfo");
                        var structValueAsDictionary = structValue as IDictionary<string, object>;
                        Assert.NotNull(structValueAsDictionary);
                        Assert.Equal(structValueAsDictionary["UserName"], "Myself");

                        Assert.Equal(evt.PayloadValue(1, "msec"), 10);
                    }));

                /*************************************************************************/
                /*************************** ENUM TESTING *******************************/
                /*************************************************************************/

                /*************************************************************************/
                GenerateEnumTest<Color>(ref tests, logger, Color.Green);
                GenerateEnumTest<ColorUInt32>(ref tests, logger, ColorUInt32.Green);
                GenerateEnumTest<ColorByte>(ref tests, logger, ColorByte.Green);
                GenerateEnumTest<ColorSByte>(ref tests, logger, ColorSByte.Green);
                GenerateEnumTest<ColorInt16>(ref tests, logger, ColorInt16.Green);
                GenerateEnumTest<ColorUInt16>(ref tests, logger, ColorUInt16.Green);
                GenerateEnumTest<ColorInt64>(ref tests, logger, ColorInt64.Green);
                GenerateEnumTest<ColorUInt64>(ref tests, logger, ColorUInt64.Green);
                /*************************************************************************/
                /*************************** ARRAY TESTING *******************************/
                /*************************************************************************/

                /*************************************************************************/

                GenerateArrayTest<bool>(ref tests, logger, new bool[] { false, true, false });
                GenerateArrayTest<byte>(ref tests, logger, new byte[] { 1, 10, 100 });
                GenerateArrayTest<sbyte>(ref tests, logger, new sbyte[] { 1, 10, 100 });
                GenerateArrayTest<short>(ref tests, logger, new short[] { 1, 10, 100 });
                GenerateArrayTest<ushort>(ref tests, logger, new ushort[] { 1, 10, 100 });
                GenerateArrayTest<int>(ref tests, logger, new int[] { 1, 10, 100 });
                GenerateArrayTest<uint>(ref tests, logger, new uint[] { 1, 10, 100 });
                GenerateArrayTest<long>(ref tests, logger, new long[] { 1, 10, 100 });
                GenerateArrayTest<ulong>(ref tests, logger, new ulong[] { 1, 10, 100 });
                GenerateArrayTest<char>(ref tests, logger, new char[] { 'a', 'c', 'b' });
                GenerateArrayTest<double>(ref tests, logger, new double[] { 1, 10, 100 });
                GenerateArrayTest<float>(ref tests, logger, new float[] { 1, 10, 100 });
                GenerateArrayTest<IntPtr>(ref tests, logger, new IntPtr[] { (IntPtr)1, (IntPtr)10, (IntPtr)100 });
                GenerateArrayTest<UIntPtr>(ref tests, logger, new UIntPtr[] { (UIntPtr)1, (UIntPtr)10, (UIntPtr)100 });
                GenerateArrayTest<Guid>(ref tests, logger, new Guid[] { Guid.Empty, new Guid("121a11ee-3bcb-49cc-b425-f4906fb14f72") });

                /*************************************************************************/
                /*********************** DICTIONARY TESTING ******************************/
                /*************************************************************************/

                var dict = new Dictionary<string, string>() { { "elem1", "10" }, { "elem2", "20" } };
                var dictInt = new Dictionary<string, int>() { { "elem1", 10 }, { "elem2", 20 } };

                /*************************************************************************/
                tests.Add(new SubTest("Write/Dict/EventWithStringDict_C",
                    delegate ()
                    {
                        // log a dictionary
                        logger.Write("EventWithStringDict_C", new
                        {
                            myDict = dict,
                            s = "end"
                        });
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventWithStringDict_C", evt.EventName);

                        var keyValues = evt.PayloadValue(0, "myDict");
                        IDictionary<string, object> vDict = GetDictionaryFromKeyValueArray(keyValues);
                        Assert.Equal(vDict["elem1"], "10");
                        Assert.Equal(vDict["elem2"], "20");
                        Assert.Equal(evt.PayloadValue(1, "s"), "end");
                    }));
                /*************************************************************************/
                tests.Add(new SubTest("Write/Dict/EventWithStringDict_BC",
                    delegate ()
                    {
                        // log a PartB and a dictionary as a PartC
                        logger.Write("EventWithStringDict_BC", new
                        {
                            PartB_UserInfo = new { UserName = "Me", LogTime = "Now" },
                            PartC_Dict = dict,
                            s = "end"
                        });
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventWithStringDict_BC", evt.EventName);

                        var structValue = evt.PayloadValue(0, "PartB_UserInfo");
                        var structValueAsDictionary = structValue as IDictionary<string, object>;
                        Assert.NotNull(structValueAsDictionary);
                        Assert.Equal(structValueAsDictionary["UserName"], "Me");
                        Assert.Equal(structValueAsDictionary["LogTime"], "Now");

                        var keyValues = evt.PayloadValue(1, "PartC_Dict");
                        var vDict = GetDictionaryFromKeyValueArray(keyValues);
                        Assert.NotNull(dict);
                        Assert.Equal(vDict["elem1"], "10");    // string values.
                        Assert.Equal(vDict["elem2"], "20");

                        Assert.Equal(evt.PayloadValue(2, "s"), "end");
                    }));
                /*************************************************************************/
                tests.Add(new SubTest("Write/Dict/EventWithIntDict_BC",
                    delegate ()
                    {
                        // log a Dict<string, int> as a PartC
                        logger.Write("EventWithIntDict_BC", new
                        {
                            PartB_UserInfo = new { UserName = "Me", LogTime = "Now" },
                            PartC_Dict = dictInt,
                            s = "end"
                        });

                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventWithIntDict_BC", evt.EventName);

                        var structValue = evt.PayloadValue(0, "PartB_UserInfo");
                        var structValueAsDictionary = structValue as IDictionary<string, object>;
                        Assert.NotNull(structValueAsDictionary);
                        Assert.Equal(structValueAsDictionary["UserName"], "Me");
                        Assert.Equal(structValueAsDictionary["LogTime"], "Now");

                        var keyValues = evt.PayloadValue(1, "PartC_Dict");
                        var vDict = GetDictionaryFromKeyValueArray(keyValues);
                        Assert.NotNull(vDict);
                        Assert.Equal(vDict["elem1"], 10);  // Notice they are integers, not strings.
                        Assert.Equal(vDict["elem2"], 20);

                        Assert.Equal(evt.PayloadValue(2, "s"), "end");
                    }));
                /*************************************************************************/
                /**************************** Empty Event TESTING ************************/
                /*************************************************************************/
                tests.Add(new SubTest("Write/Basic/Message",
                delegate ()
                {
                    logger.Write("EmptyEvent");
                },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("EmptyEvent", evt.EventName);
                }));

                /*************************************************************************/
                /**************************** EventSourceOptions TESTING *****************/
                /*************************************************************************/
                EventSourceOptions options = new EventSourceOptions();
                options.Level = EventLevel.LogAlways;
                options.Keywords = EventKeywords.All;
                options.Opcode = EventOpcode.Info;
                options.Tags = EventTags.None;
                tests.Add(new SubTest("Write/Basic/MessageOptions",
                delegate ()
                {
                    logger.Write("EmptyEvent", options);
                },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("EmptyEvent", evt.EventName);
                }));

                tests.Add(new SubTest("Write/Basic/WriteOfTWithOptios",
                delegate ()
                {
                    logger.Write("OptionsEvent", options, new { OptionsEvent = "test options!" });
                },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("OptionsEvent", evt.EventName);
                    Assert.Equal(evt.PayloadValue(0, "OptionsEvent"), "test options!");
                }));

                tests.Add(new SubTest("Write/Basic/WriteOfTWithRefOptios",
                delegate ()
                {
                    var v = new { OptionsEvent = "test ref options!" };
                    logger.Write("RefOptionsEvent", ref options, ref v);
                },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("RefOptionsEvent", evt.EventName);
                    Assert.Equal(evt.PayloadValue(0, "OptionsEvent"), "test ref options!");
                }));

                tests.Add(new SubTest("Write/Basic/WriteOfTWithNullString",
                delegate ()
                {
                    string nullString = null;
                    logger.Write("NullStringEvent", new { a = (string)null, b = nullString });
                },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("NullStringEvent", evt.EventName);
                    Assert.Equal(evt.PayloadValue(0, "a"), "");
                    Assert.Equal(evt.PayloadValue(1, "b"), "");
                }));

                // This test only applies to ETW and will fail on EventListeners due to different behavior
                // for strings with embedded NULL characters.
                Test_Write_T_AddEtwTests(listener, tests, logger);

                Guid activityId = new Guid("00000000-0000-0000-0000-000000000001");
                Guid relActivityId = new Guid("00000000-0000-0000-0000-000000000002");
                tests.Add(new SubTest("Write/Basic/WriteOfTWithOptios",
                delegate ()
                {
                    var v = new { ActivityMsg = "test activity!" };
                    logger.Write("ActivityEvent", ref options, ref activityId, ref relActivityId, ref v);
                },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("ActivityEvent", evt.EventName);
                    Assert.Equal(evt.PayloadValue(0, "ActivityMsg"), "test activity!");
                }));


                // If you only wish to run one or several of the tests you can filter them here by 
                // Uncommenting the following line.  
                // tests = tests.FindAll(test => Regex.IsMatch(test.Name, "Write/Basic/EventII"));

                // Here is where we actually run tests.   First test the ETW path 
                EventTestHarness.RunTests(tests, listener, logger);
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        static partial void Test_Write_T_AddEtwTests(Listener listener, List<SubTest> tests, EventSource logger);

        [Fact]
        [ActiveIssue("dotnet/corefx #18806", TargetFrameworkMonikers.NetFramework)]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/27106")]
        public void Test_Write_T_In_Manifest_Serialization()
        {
            using (var eventListener = new EventListenerListener())
            {
                var listenerGenerators = new List<Func<Listener>> { () => eventListener };

                Test_Write_T_In_Manifest_Serialization_Impl(listenerGenerators);
            }
        }

        /// <summary>
        /// This is not a user error but it is something unusual.   
        /// You can use the Write API in a EventSource that was did not
        /// Declare SelfDescribingSerialization.  In that case THOSE
        /// events MUST use SelfDescribing serialization.  
        /// </summary>
        private static void Test_Write_T_In_Manifest_Serialization_Impl(
            IEnumerable<Func<Listener>> listenerGenerators)
        {
            foreach (var listenerGenerator in listenerGenerators)
            {
                var events = new List<Event>();
                using (var listener = listenerGenerator())
                {
                    Debug.WriteLine("Testing Listener " + listener);
                    // Create an eventSource with manifest based serialization
                    using (var logger = new SdtEventSources.EventSourceTest())
                    {
                        listener.OnEvent = delegate (Event data)
                        { events.Add(data); };
                        listener.EventSourceSynchronousEnable(logger);

                        // Use the Write<T> API.   This is OK
                        logger.Write("MyTestEvent", new { arg1 = 3, arg2 = "hi" });
                    }
                }

                Assert.Equal(events.Count, 1);
                Event _event = events[0];
                Assert.Equal("MyTestEvent", _event.EventName);
                Assert.Equal(3, (int)_event.PayloadValue(0, "arg1"));
                Assert.Equal("hi", (string)_event.PayloadValue(1, "arg2"));
            }
        }

        private void GenerateEnumTest<T>(ref List<SubTest> tests, EventSource logger, T enumValue)
        {
            string subTestName = enumValue.GetType().ToString();
            tests.Add(new SubTest("Write/Enum/EnumEvent" + subTestName,
            delegate ()
            {
                T c = enumValue;
                // log an array
                logger.Write("EnumEvent" + subTestName, new { b = "start", v = c, s = "end" });
            },
            delegate (Event evt)
            {
                Assert.Equal(logger.Name, evt.ProviderName);
                Assert.Equal("EnumEvent" + subTestName, evt.EventName);
                Assert.Equal(evt.PayloadValue(0, "b"), "start");
                if (evt.IsEtw)
                {
                    var value = evt.PayloadValue(1, "v");
                    Assert.Equal(2, int.Parse(value.ToString()));          // Green has the int value of 2. 
                }
                else
                {
                    Assert.Equal(evt.PayloadValue(1, "v"), enumValue);
                }
                Assert.Equal(evt.PayloadValue(2, "s"), "end");
            }));
        }

        private void GenerateArrayTest<T>(ref List<SubTest> tests, EventSource logger, T[] array)
        {
            string typeName = array.GetType().GetElementType().ToString();
            tests.Add(new SubTest("Write/Array/" + typeName,
                 delegate ()
                 {
                     // log an array
                     logger.Write("SomeEvent" + typeName, new { a = array, s = "end" });
                 },
                 delegate (Event evt)
                 {
                     Assert.Equal(logger.Name, evt.ProviderName);
                     Assert.Equal("SomeEvent" + typeName, evt.EventName);

                     var eventArray = evt.PayloadValue(0, "a");
                     Array.Equals(array, eventArray);

                     Assert.Equal("end", evt.PayloadValue(1, "s"));
                 }));
        }

        /// <summary>
        /// Convert an array of key value pairs (as ETW structs) into a dictionary with those values.  
        /// </summary>
        /// <param name="structValue"></param>
        /// <returns></returns>
        private IDictionary<string, object> GetDictionaryFromKeyValueArray(object structValue)
        {
            var ret = new Dictionary<string, object>();
            var asArray = structValue as object[];
            Assert.NotNull(asArray);

            foreach (var item in asArray)
            {
                var keyValue = item as IDictionary<string, object>;
                Assert.Equal(keyValue.Count, 2);
                ret.Add((string)keyValue["Key"], keyValue["Value"]);
            }
            return ret;
        }
    }
}
