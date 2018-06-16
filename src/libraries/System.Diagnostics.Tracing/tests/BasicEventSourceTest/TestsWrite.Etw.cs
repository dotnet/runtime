// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Xunit;

namespace BasicEventSourceTests
{
    public partial class TestsWrite
    {
        // Specifies whether the process is elevated or not.
        private static readonly Lazy<bool> s_isElevated = new Lazy<bool>(AdminHelpers.IsProcessElevated);
        private static bool IsProcessElevated => s_isElevated.Value;
        private static bool IsProcessElevatedAndNotWindowsNanoServer =>
            IsProcessElevated && PlatformDetection.IsNotWindowsNanoServer; // ActiveIssue: https://github.com/dotnet/corefx/issues/29754

        /// <summary>
        /// Tests the EventSource.Write[T] method (can only use the self-describing mechanism).  
        /// Tests the ETW code path
        /// </summary>
        [ConditionalFact(nameof(IsProcessElevatedAndNotWindowsNanoServer))]
        public void Test_Write_T_ETW()
        {
            using (var listener = new EtwListener())
            {
                Test_Write_T(listener);
            }
        }

        [ActiveIssue("dotnet/corefx #18806", TargetFrameworkMonikers.NetFramework)]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/27106")]
        [ConditionalFact(nameof(IsProcessElevated))]
        public void Test_Write_T_In_Manifest_Serialization_WithEtwListener()
        {
            using (var eventListener = new EventListenerListener())
            using (var etwListener = new EtwListener())
            {
                var listenerGenerators = new List<Func<Listener>>
                {
                    () => eventListener,
                    () => etwListener
                };

                Test_Write_T_In_Manifest_Serialization_Impl(listenerGenerators);
            }
        }

        static partial void Test_Write_T_AddEtwTests(Listener listener, List<SubTest> tests, EventSource logger)
        {
            if (listener is EtwListener)
            {
                tests.Add(new SubTest("Write/Basic/WriteOfTWithEmbeddedNullString",
                    delegate
                    {
                        string nullString = null;
                        logger.Write("EmbeddedNullStringEvent", new { a = "Hello" + '\0' + "World!", b = nullString });
                    },
                    delegate (Event evt)
                    {
                        Assert.Equal(logger.Name, evt.ProviderName);
                        Assert.Equal("EmbeddedNullStringEvent", evt.EventName);
                        Assert.Equal(evt.PayloadValue(0, "a"), "Hello");
                        Assert.Equal(evt.PayloadValue(1, "b"), "");
                    }));
            }
        }
    }
}
