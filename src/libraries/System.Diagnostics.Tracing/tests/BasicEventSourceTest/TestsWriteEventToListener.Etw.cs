// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using SdtEventSources;
using Xunit;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif

namespace BasicEventSourceTests
{
    public partial class TestsWriteEventToListener
    {
        // Specifies whether the process is elevated or not.
        private static readonly Lazy<bool> s_isElevated = new Lazy<bool>(AdminHelpers.IsProcessElevated);
        private static bool IsProcessElevated => s_isElevated.Value;

        [ConditionalFact(nameof(IsProcessElevated))]
        public void Test_WriteEvent_TransferEvents()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");
            using (var log = new EventSourceTest())
            {
                using (var el = new LoudListener(log))
                {
                    Guid actid = Guid.NewGuid();
                    log.LogTaskScheduled(actid, "Hello from a test");
                    Assert.Equal(17, LoudListener.LastEvent.EventId);
                    Assert.Equal(actid, LoudListener.LastEvent.RelatedActivityId);
                    Assert.Equal(1, LoudListener.LastEvent.Payload.Count);
                    Assert.Equal("Hello from a test", (string)LoudListener.LastEvent.Payload[0]);

                    actid = Guid.NewGuid();
                    log.LogTaskScheduledBad(actid, "Hello again");
                    Assert.Equal(23, LoudListener.LastEvent.EventId);
                    Assert.Equal(actid, LoudListener.LastEvent.RelatedActivityId);
                    Assert.Equal(1, LoudListener.LastEvent.Payload.Count);
                    Assert.Equal("Hello again", (string)LoudListener.LastEvent.Payload[0]);

                    actid = Guid.NewGuid();
                    Guid guid = Guid.NewGuid();
                    log.EventWithXferManyTypeArgs(actid, 0, 0, 0, 'a', 0, 0, 0, 0, (float)10.0, (double)11.0, guid);
                    Assert.Equal(29, LoudListener.LastEvent.EventId);
                    Assert.Equal(actid, LoudListener.LastEvent.RelatedActivityId);
                    Assert.Equal(11, LoudListener.LastEvent.Payload.Count);
                    Assert.Equal(0, (long)LoudListener.LastEvent.Payload[0]);
                    Assert.Equal((uint)0, (uint)LoudListener.LastEvent.Payload[1]);
                    Assert.Equal((ulong)0, (ulong)LoudListener.LastEvent.Payload[2]);
                    Assert.Equal('a', (char)LoudListener.LastEvent.Payload[3]);
                    Assert.Equal((byte)0, (byte)LoudListener.LastEvent.Payload[4]);
                    Assert.Equal((sbyte)0, (sbyte)LoudListener.LastEvent.Payload[5]);
                    Assert.Equal((short)0, (short)LoudListener.LastEvent.Payload[6]);
                    Assert.Equal((ushort)0, (ushort)LoudListener.LastEvent.Payload[7]);
                    Assert.Equal((float)10.0, (float)LoudListener.LastEvent.Payload[8]);
                    Assert.Equal((double)11.0, (double)LoudListener.LastEvent.Payload[9]);
                    Assert.Equal(guid, (Guid)LoudListener.LastEvent.Payload[10]);

                    actid = Guid.NewGuid();
                    log.EventWithXferWeirdArgs(actid, IntPtr.Zero, true, MyLongEnum.LongVal1 /*, 9999999999999999999999999999m*/);
                    Assert.Equal(31, LoudListener.LastEvent.EventId);
                    Assert.Equal(actid, LoudListener.LastEvent.RelatedActivityId);
                    Assert.Equal(3 /*4*/, LoudListener.LastEvent.Payload.Count);
                    Assert.Equal(IntPtr.Zero, (IntPtr)LoudListener.LastEvent.Payload[0]);
                    Assert.Equal(true, (bool)LoudListener.LastEvent.Payload[1]);
                    Assert.Equal(MyLongEnum.LongVal1, (MyLongEnum)LoudListener.LastEvent.Payload[2]);
                    // Assert.Equal(9999999999999999999999999999m, (decimal)LoudListener.LastEvent.Payload[3]);

                    actid = Guid.NewGuid();
                    Assert.Throws<EventSourceException>(() => log.LogTransferNoOpcode(actid));
                }
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        static partial void Test_WriteEvent_ArgsBasicTypes_Etw_Validate_DateTime(EventSourceTest log)
        {
            DateTime now = DateTime.Now;
            log.EventDateTime(now);
            Assert.Equal(24, LoudListener.LastEvent.EventId);
            Assert.Equal(1, LoudListener.LastEvent.Payload.Count);
            Assert.Equal((DateTime)LoudListener.LastEvent.Payload[0], now);
        }

        static partial void Test_WriteEvent_ArgsCornerCases_TestEtw(EventSourceTest log)
        {
            Guid guid = Guid.NewGuid();

            log.EventWithManyTypeArgs("Hello", 0, 0, 0, 'a', 0, 0, 0, 0, (float) 10.0, (double) 11.0, guid);
            Assert.Equal(25, LoudListener.LastEvent.EventId);
            Assert.Equal(12, LoudListener.LastEvent.Payload.Count);
            Assert.Equal("Hello", (string) LoudListener.LastEvent.Payload[0]);
            Assert.Equal(0, (long) LoudListener.LastEvent.Payload[1]);
            Assert.Equal((uint) 0, (uint) LoudListener.LastEvent.Payload[2]);
            Assert.Equal((ulong) 0, (ulong) LoudListener.LastEvent.Payload[3]);
            Assert.Equal('a', (char) LoudListener.LastEvent.Payload[4]);
            Assert.Equal((byte) 0, (byte) LoudListener.LastEvent.Payload[5]);
            Assert.Equal((sbyte) 0, (sbyte) LoudListener.LastEvent.Payload[6]);
            Assert.Equal((short) 0, (short) LoudListener.LastEvent.Payload[7]);
            Assert.Equal((ushort) 0, (ushort) LoudListener.LastEvent.Payload[8]);
            Assert.Equal((float) 10.0, (float) LoudListener.LastEvent.Payload[9]);
            Assert.Equal((double) 11.0, (double) LoudListener.LastEvent.Payload[10]);
            Assert.Equal(guid, (Guid) LoudListener.LastEvent.Payload[11]);

            log.EventWithWeirdArgs(IntPtr.Zero, true, MyLongEnum.LongVal1 /*, 9999999999999999999999999999m*/);
            Assert.Equal(30, LoudListener.LastEvent.EventId);
            Assert.Equal(3 /*4*/, LoudListener.LastEvent.Payload.Count);
            Assert.Equal(IntPtr.Zero, (IntPtr) LoudListener.LastEvent.Payload[0]);
            Assert.Equal(true, (bool) LoudListener.LastEvent.Payload[1]);
            Assert.Equal(MyLongEnum.LongVal1, (MyLongEnum) LoudListener.LastEvent.Payload[2]);
            // Assert.Equal(9999999999999999999999999999m, (decimal)LoudListener.LastEvent.Payload[3]);
        }
    }
}
