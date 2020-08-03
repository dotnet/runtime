// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Xunit;

namespace System.Diagnostics.Tests
{
    public static class CounterCreationDataTests
    {
        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterCreationData_CreateCounterCreationData_SimpleSimpleHelpRawBase()
        {
            CounterCreationData ccd = new CounterCreationData("Simple", "Simple Help", PerformanceCounterType.RawBase);

            Assert.Equal("Simple", ccd.CounterName);
            Assert.Equal("Simple Help", ccd.CounterHelp);
            Assert.Equal(PerformanceCounterType.RawBase, ccd.CounterType);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterCreationData_SetCounterType_Invalud()
        {
            CounterCreationData ccd = new CounterCreationData("Simple", "Simple Help", PerformanceCounterType.RawBase);
            Assert.Throws<InvalidEnumArgumentException>(() => ccd.CounterType = (PerformanceCounterType)int.MaxValue);
        }
    }
}
