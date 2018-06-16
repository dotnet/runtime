// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using SdtEventSources;
using Xunit;

namespace BasicEventSourceTests
{
    partial class TestsWriteEvent
    {
        // Specifies whether the process is elevated or not.
        private static readonly Lazy<bool> s_isElevated = new Lazy<bool>(AdminHelpers.IsProcessElevated);
        private static bool IsProcessElevated => s_isElevated.Value;
        private static bool IsProcessElevatedAndNotWindowsNanoServer =>
            IsProcessElevated && PlatformDetection.IsNotWindowsNanoServer; // ActiveIssue: https://github.com/dotnet/corefx/issues/29754

        /// <summary>
        /// Tests WriteEvent using the manifest based mechanism.   
        /// Tests the ETW path. 
        /// </summary>
        [ConditionalFact(nameof(IsProcessElevatedAndNotWindowsNanoServer))]
        public void Test_WriteEvent_Manifest_ETW()
        {
            using (var listener = new EtwListener())
            {
                Test_WriteEvent(listener, false, true);
            }
        }

        /// <summary>
        /// Tests WriteEvent using the self-describing mechanism.   
        /// Tests both the ETW and TraceListener paths. 
        /// </summary>
        [ConditionalFact(nameof(IsProcessElevatedAndNotWindowsNanoServer))]
        public void Test_WriteEvent_SelfDescribing_ETW()
        {
            using (var listener = new EtwListener())
            {
                Test_WriteEvent(listener, true, true);
            }
        }

        /// <summary>
        /// Tests sending complex data (class, arrays etc) from WriteEvent 
        /// Tests the EventListener case
        /// </summary>
        [ConditionalFact(nameof(IsProcessElevatedAndNotWindowsNanoServer))]
        public void Test_WriteEvent_ComplexData_SelfDescribing_ETW()
        {
            using (var listener = new EtwListener())
            {
                Test_WriteEvent_ComplexData_SelfDescribing(listener);
            }
        }

        /// <summary>
        /// Tests sending complex data (class, arrays etc) from WriteEvent 
        /// Uses Manifest format
        /// Tests the EventListener case
        /// </summary>
        [ConditionalFact(nameof(IsProcessElevatedAndNotWindowsNanoServer))]
        public void Test_WriteEvent_ByteArray_Manifest_ETW()
        {
            using (var listener = new EtwListener())
            {
                Test_WriteEvent_ByteArray(false, listener);
            }
        }

        /// <summary>
        /// Tests sending complex data (class, arrays etc) from WriteEvent 
        /// Uses Self-Describing format
        /// Tests the EventListener case 
        /// </summary>
        [ConditionalFact(nameof(IsProcessElevatedAndNotWindowsNanoServer))]
        public void Test_WriteEvent_ByteArray_SelfDescribing_ETW()
        {
            using (var listener = new EtwListener())
            {
                Test_WriteEvent_ByteArray(true, listener);
            }
        }

        static partial void Test_WriteEvent_AddEtwTests(List<SubTest> tests, EventSourceTest logger)
        {
            if (!IsProcessElevated)
            {
                return;
            }

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
                    Assert.True(evt.EventName.Contains("WeirdArgs"));

                    Assert.Equal("128", evt.PayloadValue(0, "iptr").ToString());
                    Assert.Equal(true, (bool)evt.PayloadValue(1, "b"));
                    Assert.Equal((long)SdtEventSources.MyLongEnum.LongVal1, ((IConvertible)evt.PayloadValue(2, "le")).ToInt64(null));
                }));
        }
    }
}
