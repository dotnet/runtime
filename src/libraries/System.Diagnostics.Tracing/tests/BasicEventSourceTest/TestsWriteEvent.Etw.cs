// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using SdtEventSources;
using Xunit;

namespace BasicEventSourceTests
{
    partial class TestsWriteEvent
    {
        // Specifies whether the process is elevated or not.
        private static bool IsProcessElevatedAndNotWindowsNanoServer =>
            PlatformDetection.IsPrivilegedProcess && PlatformDetection.IsNotWindowsNanoServer; // ActiveIssue: https://github.com/dotnet/runtime/issues/26197

        /// <summary>
        /// Tests WriteEvent using the manifest based mechanism.
        /// Tests the ETW path.
        /// </summary>
        [ConditionalFact(nameof(IsProcessElevatedAndNotWindowsNanoServer))]
        public void Test_WriteEvent_Manifest_ETW()
        {
            using (var listener = new EtwListener())
            {
                Test_WriteEvent(listener, false);
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
                Test_WriteEvent(listener, true);
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
                Test_WriteEvent_ByteArray(listener, false);
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
                Test_WriteEvent_ByteArray(listener, true);
            }
        }
    }
}
