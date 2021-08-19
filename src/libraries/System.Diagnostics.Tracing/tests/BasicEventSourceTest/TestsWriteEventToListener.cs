// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.Threading;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using SdtEventSources;

namespace BasicEventSourceTests
{
    public partial class TestsWriteEventToListener
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/21569", TargetFrameworkMonikers.NetFramework)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51382", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public unsafe void Test_WriteEvent_ArgsBasicTypes()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");

            using (var log = new EventSourceTest())
            {
                using (var el = new LoudListener(log))
                {
                    var sources = EventSource.GetSources();
                    Assert.Contains(log, sources);

                    Assert.NotNull(EventSource.GenerateManifest(typeof(SimpleEventSource), string.Empty, EventManifestOptions.OnlyIfNeededForRegistration));
                    Assert.Null(EventSource.GenerateManifest(typeof(EventSourceTest), string.Empty, EventManifestOptions.OnlyIfNeededForRegistration));

                    log.Event0();
                    Assert.Equal(1, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(0, LoudListener.t_lastEvent.Payload.Count);

                    #region Validate "int" arguments

                    log.EventI(10);
                    Assert.Equal(2, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal(10, (int)LoudListener.t_lastEvent.Payload[0]);

                    log.EventII(10, 11);
                    Assert.Equal(3, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(2, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal(10, (int)LoudListener.t_lastEvent.Payload[0]);
                    Assert.Equal(11, (int)LoudListener.t_lastEvent.Payload[1]);

                    log.EventIII(10, 11, 12);
                    Assert.Equal(4, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(3, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal(10, (int)LoudListener.t_lastEvent.Payload[0]);
                    Assert.Equal(11, (int)LoudListener.t_lastEvent.Payload[1]);
                    Assert.Equal(12, (int)LoudListener.t_lastEvent.Payload[2]);

                    #endregion

                    #region Validate "long" arguments

                    log.EventL(10);
                    Assert.Equal(5, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal(10, (long)LoudListener.t_lastEvent.Payload[0]);

                    log.EventLL(10, 11);
                    Assert.Equal(6, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(2, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal(10, (long)LoudListener.t_lastEvent.Payload[0]);
                    Assert.Equal(11, (long)LoudListener.t_lastEvent.Payload[1]);

                    log.EventLLL(10, 11, 12);
                    Assert.Equal(7, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(3, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal(10, (long)LoudListener.t_lastEvent.Payload[0]);
                    Assert.Equal(11, (long)LoudListener.t_lastEvent.Payload[1]);
                    Assert.Equal(12, (long)LoudListener.t_lastEvent.Payload[2]);

                    #endregion

                    #region Validate "string" arguments

                    log.EventS("10");
                    Assert.Equal(8, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal("10", (string)LoudListener.t_lastEvent.Payload[0]);

                    log.EventSS("10", "11");
                    Assert.Equal(9, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(2, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal("10", (string)LoudListener.t_lastEvent.Payload[0]);
                    Assert.Equal("11", (string)LoudListener.t_lastEvent.Payload[1]);

                    log.EventSSS("10", "11", "12");
                    Assert.Equal(10, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(3, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal("10", (string)LoudListener.t_lastEvent.Payload[0]);
                    Assert.Equal("11", (string)LoudListener.t_lastEvent.Payload[1]);
                    Assert.Equal("12", (string)LoudListener.t_lastEvent.Payload[2]);

                    #endregion

                    #region Validate byte array arguments

                    var rng = new Random(42);

                    byte[] arr = new byte[20];
                    rng.NextBytes(arr);
                    log.EventWithByteArray(arr);
                    Assert.Equal(52, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal(arr, (byte[])LoudListener.t_lastEvent.Payload[0]);

                    log.EventWithByteArray(new byte[0]);
                    Assert.Equal(52, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Same(Array.Empty<byte>(), (byte[])LoudListener.t_lastEvent.Payload[0]);

                    log.EventWithByteArray(null);
                    Assert.Equal(52, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Same(Array.Empty<byte>(), (byte[])LoudListener.t_lastEvent.Payload[0]);

                    arr = new byte[20];
                    rng.NextBytes(arr);
                    log.EventWithByteArrayCustom(arr);
                    Assert.Equal(53, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal(arr, (byte[])LoudListener.t_lastEvent.Payload[0]);

                    log.EventWithByteArrayCustom(new byte[0]);
                    Assert.Equal(53, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Same(Array.Empty<byte>(), (byte[])LoudListener.t_lastEvent.Payload[0]);

                    arr = new byte[20];
                    rng.NextBytes(arr);
                    fixed (byte* arrPtr = arr)
                    {
                        log.EventWithBytePointer(arrPtr, arr.Length);
                    }
                    Assert.Equal(54, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal(arr, (byte[])LoudListener.t_lastEvent.Payload[0]);

                    log.EventWithBytePointer((byte*)IntPtr.Zero, 0);
                    Assert.Equal(54, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Same(Array.Empty<byte>(), (byte[])LoudListener.t_lastEvent.Payload[0]);

                    #endregion

                    #region Validate mixed type arguments

                    log.EventSI("10", 11);
                    Assert.Equal(11, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(2, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal("10", (string)LoudListener.t_lastEvent.Payload[0]);
                    Assert.Equal(11, (int)LoudListener.t_lastEvent.Payload[1]);

                    log.EventSL("10", 11);
                    Assert.Equal(12, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(2, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal("10", (string)LoudListener.t_lastEvent.Payload[0]);
                    Assert.Equal(11, (long)LoudListener.t_lastEvent.Payload[1]);

                    log.EventSII("10", 11, 12);
                    Assert.Equal(13, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(3, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal("10", (string)LoudListener.t_lastEvent.Payload[0]);
                    Assert.Equal(11, (int)LoudListener.t_lastEvent.Payload[1]);
                    Assert.Equal(12, (int)LoudListener.t_lastEvent.Payload[2]);

                    #endregion

                    #region Validate enums/flags

                    log.EventEnum(MyColor.Blue);
                    Assert.Equal(19, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal(MyColor.Blue, (MyColor)LoudListener.t_lastEvent.Payload[0]);

                    log.EventEnum1(MyColor.Green);
                    Assert.Equal(20, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal(MyColor.Green, (MyColor)LoudListener.t_lastEvent.Payload[0]);

                    log.EventFlags(MyFlags.Flag1);
                    Assert.Equal(21, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal(MyFlags.Flag1, (MyFlags)LoudListener.t_lastEvent.Payload[0]);

                    log.EventFlags1(MyFlags.Flag1);
                    Assert.Equal(22, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal(MyFlags.Flag1, (MyFlags)LoudListener.t_lastEvent.Payload[0]);

                    #endregion

                    #region Validate DateTime
                    Test_WriteEvent_ArgsBasicTypes_Etw_Validate_DateTime(log);
                    #endregion
                }
            }

            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        static partial void Test_WriteEvent_ArgsBasicTypes_Etw_Validate_DateTime(EventSourceTest log);

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/21569", TargetFrameworkMonikers.NetFramework)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51382", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void Test_WriteEvent_ArgsCornerCases()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");
            using (var log = new EventSourceTest())
            {
                using (var el = new LoudListener(log))
                {
                    // coverage for EventSource.SendCommand()
                    var options = new Dictionary<string, string>() { { "arg", "val" } };
                    EventSource.SendCommand(log, EventCommand.SendManifest, options);


                    log.EventWith7Strings("s0", "s1", "s2", "s3", "s4", "s5", "s6");
                    Assert.Equal(26, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(7, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal("s0", (string)LoudListener.t_lastEvent.Payload[0]);
                    Assert.Equal("s6", (string)LoudListener.t_lastEvent.Payload[6]);

                    log.EventWith9Strings("s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7", "s8");
                    Assert.Equal(27, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(9, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal("s0", (string)LoudListener.t_lastEvent.Payload[0]);
                    Assert.Equal("s8", (string)LoudListener.t_lastEvent.Payload[8]);

                    Test_WriteEvent_ArgsCornerCases_TestEtw(log);
                }
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        static partial void Test_WriteEvent_ArgsCornerCases_TestEtw(EventSourceTest log);

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51382", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void Test_WriteEvent_InvalidCalls()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");
            using (var log = new InvalidCallsToWriteEventEventSource())
            {
                using (var el = new LoudListener(log))
                {
                    log.WriteTooManyArgs("Hello");
                    Assert.Equal(2, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);           // Faked count (compat)
                    Assert.Equal("Hello", LoudListener.t_lastEvent.Payload[0]);

                    log.WriteTooFewArgs(10, 100);
                    Assert.Equal(1, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);           // Real # of args passed to WriteEvent
                    Assert.Equal(10, LoudListener.t_lastEvent.Payload[0]);
                }
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51382", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void Test_WriteEvent_ToChannel_Coverage()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");

            using (var log = new SimpleEventSource())
            using (var el = new LoudListener(log))
            {
                log.WriteIntToAdmin(10);
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/21569", TargetFrameworkMonikers.NetFramework)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51382", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void Test_WriteEvent_ZeroKwds()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");

            using (var log = new EventSourceTest())
            {
                using (var el = new LoudListener(log))
                {
                    // match any kwds == 0
                    el.EnableEvents(log, 0, 0);

                    // Fire an event without a kwd: EventWithEscapingMessage

                    // 1. Validate that the event fires when ETW event method called unconditionally
                    log.EventWithEscapingMessage("Hello world!", 10);
                    Assert.NotNull(LoudListener.t_lastEvent);
                    Assert.Equal(2, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal("Hello world!", (string)LoudListener.t_lastEvent.Payload[0]);
                    Assert.Equal(10, (int)LoudListener.t_lastEvent.Payload[1]);

                    // reset LastEvent
                    LoudListener.t_lastEvent = null;

                    // 2. Validate that the event fires when ETW event method call is guarded by IsEnabled
                    if (log.IsEnabled(EventLevel.Informational, 0))
                        log.EventWithEscapingMessage("Goodbye skies!", 100);
                    Assert.NotNull(LoudListener.t_lastEvent);
                    Assert.Equal(2, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal("Goodbye skies!", (string)LoudListener.t_lastEvent.Payload[0]);
                    Assert.Equal(100, (int)LoudListener.t_lastEvent.Payload[1]);
                }
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51382", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void Test_EventSourceCreatedEvents_BeforeListener()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");

            EventSource log = null;
            EventSource log2 = null;
            EventListenerListener el = null;

            try
            {
                string esName = "EventSourceName_HopefullyUnique";
                string esName2 = "EventSourceName_HopefullyUnique2";
                bool esNameHit = false;
                bool esName2Hit = false;

                log = new EventSource(esName);
                log2 = new EventSource(esName2);


                using (var listener = new EventListenerListener())
                {
                    List<EventSource> eventSourceNotificationsReceived = new List<EventSource>();
                    listener.EventSourceCreated += (s, a) =>
                    {
                        if (a.EventSource.Name.Equals(esName))
                        {
                            esNameHit = true;
                        }

                        if (a.EventSource.Name.Equals(esName2))
                        {
                            esName2Hit = true;
                        }
                    };

                    Thread.Sleep(1000);

                    Assert.True(esNameHit);
                    Assert.True(esName2Hit);
                }
            }
            finally
            {
                if (log != null)
                {
                    log.Dispose();
                }

                if (log2 != null)
                {
                    log2.Dispose();
                }

                if (el != null)
                {
                    el.Dispose();
                }
            }

            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51382", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void Test_EventSourceCreatedEvents_AfterListener()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");

            EventSource log = null;
            EventSource log2 = null;
            EventListenerListener el = null;

            try
            {
                using (var listener = new EventListenerListener())
                {
                    string esName = "EventSourceName_HopefullyUnique";
                    string esName2 = "EventSourceName_HopefullyUnique2";
                    bool esNameHit = false;
                    bool esName2Hit = false;

                    List<EventSource> eventSourceNotificationsReceived = new List<EventSource>();
                    listener.EventSourceCreated += (s, a) =>
                    {
                        if (a.EventSource.Name.Equals(esName))
                        {
                            esNameHit = true;
                        }

                        if (a.EventSource.Name.Equals(esName2))
                        {
                            esName2Hit = true;
                        }
                    };

                    log = new EventSource(esName);
                    log2 = new EventSource(esName2);

                    Thread.Sleep(1000);

                    Assert.True(esNameHit);
                    Assert.True(esName2Hit);
                }
            }
            finally
            {
                if (log != null)
                {
                    log.Dispose();
                }

                if (log2 != null)
                {
                    log2.Dispose();
                }

                if (el != null)
                {
                    el.Dispose();
                }
            }

            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Test_EventListenerThrows_ExceptionIsNotRethrownToCaller(bool setThrowOnEventWriteErrorsFlag)
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");

            using (var log = new EventSourceTest(throwOnEventWriteErrors: setThrowOnEventWriteErrorsFlag))
            {
                using (var listener = new EventListenerListener())
                {
                    listener.EventSourceSynchronousEnable(log);

                    var thrownException = new Exception("Oops");
                    string outOfBandMessage = null;

                    listener.EventWritten += (_, e) =>
                    {
                        if (e.EventId == 0)
                        {
                            outOfBandMessage = e.Message;
                        }

                        throw thrownException;
                    };

                    try
                    {
                        log.Event0();
                        Assert.False(setThrowOnEventWriteErrorsFlag);
                    }
                    catch (EventSourceException ex)
                    {
                        Assert.True(setThrowOnEventWriteErrorsFlag);
                        Assert.Same(thrownException, ex.InnerException);
                    }

                    Assert.Contains(thrownException.Message, outOfBandMessage);
                }
            }

            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }
    }
}
