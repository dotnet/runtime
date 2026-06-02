// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using SdtEventSources;
using Xunit;

namespace BasicEventSourceTests
{
    public partial class TestsWriteEvent
    {

        public static TheoryData<Listener,bool> GetListenerConfigurations()
        {
            TheoryData<Listener, bool> data = new TheoryData<Listener, bool>();
            foreach(var listener in GetListeners())
            {
                data.Add(listener, /*self-describing*/ false);
            }
            // listener objects are used once and then disposed, so we need to create a 2nd set
            foreach (var listener in GetListeners())
            {
                data.Add(listener, /*self-describing*/ true);
            }
            return data;
        }

        public static TheoryData<Listener> GetListeners()
        {
            TheoryData<Listener> data = new TheoryData<Listener>();
            if (PlatformDetection.IsNetCore && PlatformDetection.IsNotAndroid && PlatformDetection.IsNotBrowser &&
                (PlatformDetection.IsNotMonoRuntime || PlatformDetection.IsMacCatalyst))
            {
                data.Add(new EventPipeListener());
            }
            data.Add(new EventListenerListener());
            data.Add(new EventListenerListener(true));
            return data;
        }

        [Fact]
        public void Test_WriteEvent_NoAttribute()
        {
            using (EventSourceNoAttribute es = new EventSourceNoAttribute())
            {
                Listener el = new EventListenerListener(true);
                var tests = new List<SubTest>();
                string arg = "a sample string";

                tests.Add(new SubTest("Write/Basic/EventWith9Strings",
                delegate ()
                {
                    es.EventNoAttributes(arg);
                },
                delegate (Event evt)
                {
                    Assert.Equal(es.Name, evt.ProviderName);
                    Assert.Equal("EventNoAttributes", evt.EventName);
                    Assert.Equal(arg, (string)evt.PayloadValue(0, null));
                }));

                EventTestHarness.RunTests(tests, el, es);
            }
        }

        [Theory]
        [MemberData(nameof(GetListenerConfigurations))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/21295", TargetFrameworkMonikers.NetFramework)]
        public void Test_WriteEvent(Listener listener, bool useSelfDescribingEvents)
        {
            using (listener)
            {
                Test_WriteEvent_Helper(listener, useSelfDescribingEvents);
            }
        }


        private void Test_WriteEvent_Helper(Listener listener, bool useSelfDescribingEvents)
        {
            using (var logger = new EventSourceTest(useSelfDescribingEvents))
            {
                var tests = new List<SubTest>();

                /*************************************************************************/
                tests.Add(new SubTest("WriteEvent/Basic/EventII",
                    delegate ()
                    { logger.EventII(10, 11); },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventII", evt.EventName);
                        Assert.Equal(10, evt.PayloadValue(0, "arg1"));
                        Assert.Equal(11, evt.PayloadValue(1, "arg2"));
                    }));
                /*************************************************************************/
                tests.Add(new SubTest("WriteEvent/Basic/EventSS",
                    delegate ()
                    { logger.EventSS("one", "two"); },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventSS", evt.EventName);
                        Assert.Equal("one", evt.PayloadValue(0, "arg1"));
                        Assert.Equal("two", evt.PayloadValue(1, "arg2"));
                    }));
                /*************************************************************************/
                tests.Add(new SubTest("Write/Basic/EventWith7Strings",
                    delegate ()
                    {
                        logger.EventWith7Strings("s0", "s1", "s2", "s3", "s4", "s5", "s6");
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventWith7Strings", evt.EventName);
                        Assert.Equal("s0", (string)evt.PayloadValue(0, "s0"));
                        Assert.Equal("s6", (string)evt.PayloadValue(6, "s6"));
                    }));
                /*************************************************************************/
                tests.Add(new SubTest("Write/Basic/EventWith9Strings",
                    delegate ()
                    {
                        logger.EventWith9Strings("s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7", "s8");
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventWith9Strings", evt.EventName);
                        Assert.Equal("s0", (string)evt.PayloadValue(0, "s0"));
                        Assert.Equal("s8", (string)evt.PayloadValue(8, "s8"));
                    }));
                /*************************************************************************/

                tests.Add(new SubTest("Write/Basic/EventWithManyTypeArgs",
                delegate ()
                {
                    logger.EventWithManyTypeArgs("Hello", 1, 2, 3, 'a', 4, 5, 6, 7,
                        (float)10.0, (double)11.0, logger.Guid);
                },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("EventWithManyTypeArgs", evt.EventName);
                    Assert.Equal("Hello", evt.PayloadValue(0, "msg"));
                    Assert.Equal((float)10.0, evt.PayloadValue(9, "f"));
                    Assert.Equal((double)11.0, evt.PayloadValue(10, "d"));
                    Assert.Equal(logger.Guid, evt.PayloadValue(11, "guid"));
                }));

                if (!listener.IsEventPipe) // EventPipe doesn't encode this correctly
                {
                    tests.Add(new SubTest("Write/Activity/EventWithXferWeirdArgs",
                    delegate ()
                    {
                        var actid = Guid.NewGuid();
                        logger.EventWithXferWeirdArgs(actid,
                            (IntPtr)128,
                            true,
                            SdtEventSources.MyLongEnum.LongVal1);
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);

                        // We log EventWithXferWeirdArgs in one case and
                        // WorkWeirdArgs/Send in the other
                        Assert.Contains("WeirdArgs", evt.EventName);

                        Assert.Equal("128", evt.PayloadValue(0, "iptr").ToString());
                        Assert.True((bool)evt.PayloadValue(1, "b"));
                        Assert.Equal((long)SdtEventSources.MyLongEnum.LongVal1, ((IConvertible)evt.PayloadValue(2, "le")).ToInt64(null));
                    }));
                }


                /*************************************************************************/
                /*************************** ENUM TESTING *******************************/
                /*************************************************************************/

                /*************************************************************************/
                tests.Add(new SubTest("WriteEvent/Enum/EventEnum",
                    delegate ()
                    {
                        logger.EventEnum(SdtEventSources.MyColor.Blue);
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventEnum", evt.EventName);

                        Assert.Equal(1, ((IConvertible)evt.PayloadValue(0, "x")).ToInt32(null));
                        if (evt.IsEnumValueStronglyTyped(useSelfDescribingEvents, writeEvent:true))
                            Assert.Equal("Blue", evt.PayloadString(0, "x"));
                    }));

                tests.Add(new SubTest("WriteEvent/Enum/EventEnum1",
                   delegate ()
                   {
                       logger.EventEnum1(MyColor.Blue);
                   },
                   delegate (Event evt)
                   {
                       Assert.Equal(logger.Name, evt.ProviderName);
                       Assert.Equal("EventEnum1", evt.EventName);

                       Assert.Equal(1, ((IConvertible)evt.PayloadValue(0, "x")).ToInt32(null));
                       if (evt.IsEnumValueStronglyTyped(useSelfDescribingEvents, writeEvent: true))
                           Assert.Equal("Blue", evt.PayloadString(0, "x"));
                   }));

                tests.Add(new SubTest("WriteEvent/Basic/EventWithIntIntString",
                    delegate ()
                    { logger.EventWithIntIntString(10, 11, "test"); },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventWithIntIntString", evt.EventName);
                        Assert.Equal(10, evt.PayloadValue(0, "i1"));
                        Assert.Equal(11, evt.PayloadValue(1, "i2"));
                        Assert.Equal("test", evt.PayloadValue(2, "str"));
                    }));

                tests.Add(new SubTest("WriteEvent/Basic/EventWithIntLongString",
                    delegate ()
                    { logger.EventWithIntLongString(10, (long)11, "test"); },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventWithIntLongString", evt.EventName);
                        Assert.Equal(10, evt.PayloadValue(0, "i1"));
                        Assert.Equal(evt.PayloadValue(1, "l1"), (long)11);
                        Assert.Equal("test", evt.PayloadValue(2, "str"));
                    }));

                tests.Add(new SubTest("WriteEvent/Basic/EventWithString",
                    delegate ()
                    { logger.EventWithString(null); },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal(1, evt.PayloadCount);
                        Assert.Equal("", evt.PayloadValue(0, null));
                    }));


                tests.Add(new SubTest("WriteEvent/Basic/EventWithIntAndString",
                    delegate ()
                    { logger.EventWithIntAndString(12, null); },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal(2, evt.PayloadCount);
                        Assert.Equal(12, evt.PayloadValue(0, null));
                        Assert.Equal("", evt.PayloadValue(1, null));
                    }));

                tests.Add(new SubTest("WriteEvent/Basic/EventWithLongAndString",
                    delegate ()
                    { logger.EventWithLongAndString(120L, null); },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal(2, evt.PayloadCount);
                        Assert.Equal(120L, evt.PayloadValue(0, null));
                        Assert.Equal("", evt.PayloadValue(1, null));
                    }));

                tests.Add(new SubTest("WriteEvent/Basic/EventWithStringAndInt",
                    delegate ()
                    { logger.EventWithStringAndInt(null, 12); },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal(2, evt.PayloadCount);
                        Assert.Equal("", evt.PayloadValue(0, null));
                        Assert.Equal(12, evt.PayloadValue(1, null));
                    }));

                tests.Add(new SubTest("WriteEvent/Basic/EventWithStringAndIntAndInt",
                    delegate ()
                    { logger.EventWithStringAndIntAndInt(null, 12, 13); },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal(3, evt.PayloadCount);
                        Assert.Equal("", evt.PayloadValue(0, null));
                        Assert.Equal(12, evt.PayloadValue(1, null));
                        Assert.Equal(13, evt.PayloadValue(2, null));
                    }));

                tests.Add(new SubTest("WriteEvent/Basic/EventWithStringAndLong",
                    delegate ()
                    { logger.EventWithStringAndLong(null, 120L); },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal(2, evt.PayloadCount);
                        Assert.Equal("", evt.PayloadValue(0, null));
                        Assert.Equal(120L, evt.PayloadValue(1, null));
                    }));

                tests.Add(new SubTest("WriteEvent/Basic/EventWithStringAndString",
                    delegate ()
                    { logger.EventWithStringAndString(null, null); },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal(2, evt.PayloadCount);
                        Assert.Equal("", evt.PayloadValue(0, null));
                        Assert.Equal("", evt.PayloadValue(1, null));
                    }));

                tests.Add(new SubTest("WriteEvent/Basic/EventWithStringAndStringAndString",
                    delegate ()
                    { logger.EventWithStringAndStringAndString(null, null, null); },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal(3, evt.PayloadCount);
                        Assert.Equal("", evt.PayloadValue(0, null));
                        Assert.Equal("", evt.PayloadValue(1, null));
                        Assert.Equal("", evt.PayloadValue(2, null));
                    }));

                // Self-describing ETW+EventPipe do not support NULL arguments.
                if (useSelfDescribingEvents && listener.IsSelfDescribingNullArgSupported)
                {
                    tests.Add(new SubTest("WriteEvent/Basic/EventVarArgsWithString",
                        delegate ()
                        { logger.EventVarArgsWithString(1, 2, 12, null); },
                        delegate (Event evt)
                        {
                            Assert.Equal(logger.Name, evt.ProviderName);
                            Assert.Equal(4, evt.PayloadCount);
                            Assert.Equal(1, evt.PayloadValue(0, null));
                            Assert.Equal(2, evt.PayloadValue(1, null));
                            Assert.Equal(12, evt.PayloadValue(2, null));
                            Assert.Equal("", evt.PayloadValue(3, null));
                        }));
                }

                // Probably belongs in the user TestUsersErrors.cs.
                if (!useSelfDescribingEvents)
                {
                    tests.Add(new SubTest("WriteEvent/Basic/EventWithIncorrectNumberOfParameters",
                        delegate ()
                        {
                            logger.EventWithIncorrectNumberOfParameters("TestMessage", "TestPath", 10);
                        },
                        delegate (List<Event> evts)
                        {
                            Assert.True(0 < evts.Count);

                            // We give an error message in EventListener case but not the ETW case.
                            if (1 < evts.Count)
                            {
                                Assert.Equal(2, evts.Count);
                                Assert.Equal(logger.Name, evts[0].ProviderName);
                                Assert.Equal("EventSourceMessage", evts[0].EventName);
                                string errorMsg = evts[0].PayloadString(0, "message");
                                Assert.Matches("called with 1.*defined with 3", errorMsg);
                            }

                            int eventIdx = evts.Count - 1;
                            Assert.Equal(logger.Name, evts[eventIdx].ProviderName);
                            Assert.Equal("EventWithIncorrectNumberOfParameters", evts[eventIdx].EventName);
                            Assert.Equal("{TestPath:10}TestMessage", evts[eventIdx].PayloadString(0, "message"));
                        }));
                }

                // If you only wish to run one or several of the tests you can filter them here by
                // Uncommenting the following line.
                // tests = tests.FindAll(test => Regex.IsMatch(test.Name, "ventWithByteArray"));

                // Next run the same tests with the TraceLogging path.
                EventTestHarness.RunTests(tests, listener, logger);
            }
        }

        /**********************************************************************/

        [Theory]
        [MemberData(nameof(GetListeners))]
        public void Test_WriteEvent_ComplexData_SelfDescribing(Listener listener)
        {
            using (listener)
            {
                Test_WriteEvent_ComplexData_SelfDescribing_Helper(listener);
            }
        }

        private void Test_WriteEvent_ComplexData_SelfDescribing_Helper(Listener listener)
        {
            using (var logger = new EventSourceTestSelfDescribingOnly())
            {
                var tests = new List<SubTest>();

                byte[] byteArray = { 0, 1, 2, 3 };
                tests.Add(new SubTest("WriteEvent/SelfDescribingOnly/Byte[]",
                    delegate ()
                    {
                        logger.EventByteArrayInt(byteArray, 5);
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventByteArrayInt", evt.EventName);

                        var eventArray = evt.PayloadValue(0, "array");
                        Array.Equals(eventArray, byteArray);
                        Assert.Equal(5, evt.PayloadValue(1, "anInt"));
                    }));

                if(!listener.IsEventPipe) // EventPipe doesn't correctly encode metadata for this
                {
                    tests.Add(new SubTest("WriteEvent/SelfDescribingOnly/UserData",
                    delegate ()
                    {
                        logger.EventUserDataInt(new UserData() { x = 3, y = 8 }, 5);
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventUserDataInt", evt.EventName);

                        var aClass = (IDictionary<string, object>)evt.PayloadValue(0, "aClass");
                        Assert.Equal(3, (int)aClass["x"]);
                        Assert.Equal(8, (int)aClass["y"]);
                        Assert.Equal(5, evt.PayloadValue(1, "anInt"));
                    }));
                }

                if (listener.IsSelfDescribingNullableSupported)
                {
                    int? nullableInt = 12;
                    tests.Add(new SubTest("WriteEvent/SelfDescribingOnly/Int12",
                        delegate ()
                        {
                            logger.EventNullableIntInt(nullableInt, 5);
                        },
                        delegate (Event evt)
                        {
                            Assert.Equal(logger.Name, evt.ProviderName);
                            Assert.Equal("EventNullableIntInt", evt.EventName);

                            var payload = evt.PayloadValue(0, "nullableInt");
                            Assert.Equal(nullableInt, TestUtilities.UnwrapNullable<int>(payload));
                            Assert.Equal(5, evt.PayloadValue(1, "anInt"));
                        }));

                    int? nullableInt2 = null;
                    tests.Add(new SubTest("WriteEvent/SelfDescribingOnly/IntNull",
                        delegate ()
                        {
                            logger.EventNullableIntInt(nullableInt2, 5);
                        },
                        delegate (Event evt)
                        {
                            Assert.Equal(logger.Name, evt.ProviderName);
                            Assert.Equal("EventNullableIntInt", evt.EventName);

                            var payload = evt.PayloadValue(0, "nullableInt");
                            Assert.Equal(nullableInt2, TestUtilities.UnwrapNullable<int>(payload));
                            Assert.Equal(5, evt.PayloadValue(1, "anInt"));
                        }));

                    DateTime? nullableDate = DateTime.Now;
                    tests.Add(new SubTest("WriteEvent/SelfDescribingOnly/DateTimeNow",
                        delegate ()
                        {
                            logger.EventNullableDateTimeInt(nullableDate, 5);
                        },
                        delegate (Event evt)
                        {
                            Assert.Equal(logger.Name, evt.ProviderName);
                            Assert.Equal("EventNullableDateTimeInt", evt.EventName);

                            var payload = evt.PayloadValue(0, "nullableDate");
                            Assert.Equal(nullableDate, TestUtilities.UnwrapNullable<DateTime>(payload));
                            Assert.Equal(5, evt.PayloadValue(1, "anInt"));
                        }));

                    DateTime? nullableDate2 = null;
                    tests.Add(new SubTest("WriteEvent/SelfDescribingOnly/DateTimeNull",
                        delegate ()
                        {
                            logger.EventNullableDateTimeInt(nullableDate2, 5);
                        },
                        delegate (Event evt)
                        {
                            Assert.Equal(logger.Name, evt.ProviderName);
                            Assert.Equal("EventNullableDateTimeInt", evt.EventName);

                            var payload = evt.PayloadValue(0, "nullableDate");
                            Assert.Equal(nullableDate2, TestUtilities.UnwrapNullable<DateTime>(nullableDate2));
                            Assert.Equal(5, evt.PayloadValue(1, "anInt"));
                        }));
                }

                // If you only wish to run one or several of the tests you can filter them here by
                // Uncommenting the following line.
                // tests = tests.FindAll(test => Regex.IsMatch(test.Name, "ventWithByteArray"));

                // Next run the same tests with the TraceLogging path.
                EventTestHarness.RunTests(tests, listener, logger);
            }
        }

        [Theory]
        [MemberData(nameof(GetListenerConfigurations))]
        private void Test_WriteEvent_ByteArray(Listener listener, bool useSelfDescribingEvents)
        {
            using(listener)
            {
                Test_WriteEvent_ByteArray_Helper(listener, useSelfDescribingEvents);
            }
        }

        private void Test_WriteEvent_ByteArray_Helper(Listener listener, bool useSelfDescribingEvents)
        {
            EventSourceSettings settings = EventSourceSettings.EtwManifestEventFormat;
            if (useSelfDescribingEvents)
                settings = EventSourceSettings.EtwSelfDescribingEventFormat;

            using (var logger = new EventSourceTestByteArray(settings))
            {
                var tests = new List<SubTest>();
                /*************************************************************************/
                /**************************** byte[] TESTING *****************************/
                /*************************************************************************/
                // We only support arrays of any type with the SelfDescribing case.
                /*************************************************************************/
                byte[] blob = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                tests.Add(new SubTest("Write/Array/EventWithByteArrayArg",
                    delegate ()
                    {
                        logger.EventWithByteArrayArg(blob, 1000);
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventWithByteArrayArg", evt.EventName);

                        if (evt.IsEventListener)
                        {
                            byte[] retBlob = (byte[])evt.PayloadValue(0, "blob");
                            Assert.NotNull(retBlob);
                            Assert.True(Equal(blob, retBlob));
                            Assert.Equal(1000, (int)evt.PayloadValue(1, "n"));
                        }
                    }));

                if (!useSelfDescribingEvents)
                {
                    if (!listener.IsEventPipe) // EventPipe doesn't correctly encode metadata for this
                    {
                        /*************************************************************************/
                        tests.Add(new SubTest("Write/Array/NonEventCallingEventWithBytePtrArg",
                            delegate ()
                            {
                                logger.NonEventCallingEventWithBytePtrArg(blob, 2, 4, 1001);
                            },
                            delegate (Event evt)
                            {
                                Assert.Equal(logger.Name, evt.ProviderName);
                                Assert.Equal("EventWithBytePtrArg", evt.EventName);

                                if (evt.IsSizeAndPointerCoallescedIntoSingleArg)
                                {
                                    Assert.Equal(2, evt.PayloadCount);
                                    byte[] retBlob = (byte[])evt.PayloadValue(0, "blob");
                                    Assert.Equal(4, retBlob.Length);
                                    Assert.Equal(retBlob[0], blob[2]);
                                    Assert.Equal(retBlob[3], blob[2 + 3]);
                                    Assert.Equal(1001, (int)evt.PayloadValue(1, "n"));
                                }
                                else
                                {
                                    Assert.Equal(3, evt.PayloadCount);
                                    byte[] retBlob = (byte[])evt.PayloadValue(1, "blob");
                                    Assert.Equal(4, retBlob.Length);
                                    Assert.Equal(retBlob[0], blob[2]);
                                    Assert.Equal(retBlob[3], blob[2 + 3]);
                                    Assert.Equal(1001, (int)evt.PayloadValue(2, "n"));
                                }
                            }));
                    }
                }

                if (!listener.IsEventPipe) // EventPipe doesn't correctly encode metadata for this
                {
                    tests.Add(new SubTest("Write/Array/EventWithLongByteArray",
                        delegate ()
                        {
                            logger.EventWithLongByteArray(blob, 1000);
                        },
                        delegate (Event evt)
                        {
                            Assert.Equal(logger.Name, evt.ProviderName);
                            Assert.Equal("EventWithLongByteArray", evt.EventName);

                            Assert.Equal(2, evt.PayloadCount);
                            byte[] retBlob = (byte[])evt.PayloadValue(0, "blob");
                            Assert.True(Equal(blob, retBlob));

                            Assert.Equal(1000, (long)evt.PayloadValue(1, "lng"));
                        }));
                }

                /* TODO: NULL byte array does not seem to be supported.
                 * An EventSourceMessage event is written for this case.
                tests.Add(new SubTest("Write/Array/EventWithNullByteArray",
                    delegate ()
                    {
                        logger.EventWithByteArrayArg(null, 0);
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventWithByteArrayArg", evt.EventName);

                        if (evt.IsEventListener)
                        {
                            byte[] retBlob = (byte[])evt.PayloadValue(0, "blob");
                            Assert.Null(retBlob);
                            Assert.Equal(0, (int)evt.PayloadValue(1, "n"));
                        }
                    }));
                */

                if (!listener.IsEventPipe) // EventPipe doesn't correctly encode metadata for this
                {
                    tests.Add(new SubTest("Write/Array/EventWithEmptyByteArray",
                    delegate ()
                    {
                        logger.EventWithByteArrayArg(Array.Empty<byte>(), 0);
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EventWithByteArrayArg", evt.EventName);

                        Assert.Equal(2, evt.PayloadCount);
                        byte[] retBlob = (byte[])evt.PayloadValue(0, "blob");
                        Assert.True(Equal(Array.Empty<byte>(), retBlob));

                        Assert.Equal(0, (int)evt.PayloadValue(1, "n"));
                    }));
                }

                // If you only wish to run one or several of the tests you can filter them here by
                // Uncommenting the following line.
                // tests = tests.FindAll(test => Regex.IsMatch(test.Name, "ventWithByteArray"));

                // Next run the same tests with the TraceLogging path.
                EventTestHarness.RunTests(tests, listener, logger);
            }
        }

        /**********************************************************************/
        // Helper that compares two arrays for equality.
        private static bool Equal(byte[] blob1, byte[] blob2)
        {
            if (blob1.Length != blob2.Length)
                return false;
            for (int i = 0; i < blob1.Length; i++)
                if (blob1[i] != blob2[i])
                    return false;
            return true;
        }
    }

    [EventData]
    public class UserData
    {
        public int x { get; set; }
        public int y { get; set; }
    }

    /// <summary>
    /// Used to show the more complex data type that
    /// </summary>
    public sealed class EventSourceTestSelfDescribingOnly : EventSource
    {
        public EventSourceTestSelfDescribingOnly() : base(EventSourceSettings.EtwSelfDescribingEventFormat) { }
        public void EventByteArrayInt(byte[] array, int anInt) { WriteEvent(1, array, anInt); }
        public void EventUserDataInt(UserData aClass, int anInt) { WriteEvent(2, aClass, anInt); }
        public void EventNullableIntInt(int? nullableInt, int anInt) { WriteEvent(3, nullableInt, anInt); }
        public void EventNullableDateTimeInt(DateTime? nullableDate, int anInt) { WriteEvent(4, nullableDate, anInt); }
    }

    public sealed class EventSourceTestByteArray : EventSource
    {
        public EventSourceTestByteArray(EventSourceSettings settings) : base(settings) { }

        // byte[] args not supported on 4.5
        [Event(1, Level = EventLevel.Informational, Message = "Int arg after byte array: {1}")]
        public void EventWithByteArrayArg(byte[] blob, int n)
        { WriteEvent(1, blob, n); }

        [NonEvent]
        public unsafe void NonEventCallingEventWithBytePtrArg(byte[] blob, int start, uint size, int n)
        {
            if (blob == null || start + size > blob.Length)
                throw new ArgumentException("start + size must be smaller than blob.Length");
            fixed (byte* p = blob)
                EventWithBytePtrArg(size, p + start, n);
        }

        [Event(2, Level = EventLevel.Informational, Message = "Int arg after byte ptr: {2}")]
        public unsafe void EventWithBytePtrArg(uint blobSize, byte* blob, int n)
        {
            if (IsEnabled())
            {
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[3];
                    descrs[0].DataPointer = (IntPtr)(&blobSize);
                    descrs[0].Size = 4;
                    descrs[1].DataPointer = (IntPtr)blob;
                    descrs[1].Size = (int)blobSize;
                    descrs[2].Size = 4;
                    descrs[2].DataPointer = (IntPtr)(&n);
                    WriteEventCore(2, 3, descrs);
                }
            }
        }

        [Event(3, Level = EventLevel.Informational, Message = "long after byte array: {1}")]
        public void EventWithLongByteArray(byte[] blob, long lng)
        { WriteEvent(3, blob, lng); }
    }
}
