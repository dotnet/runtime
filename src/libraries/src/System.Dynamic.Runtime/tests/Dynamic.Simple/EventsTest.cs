// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace SampleDynamicTests
{
    public delegate int AddDelegate(int i);

    public class EventsTest
    {
        public event AddDelegate E;

        public void DoEvent(int arg)
        {
            int result = this.E(arg);
            Assert.Equal(arg + 1, result);
        }

        public static int InvokedMethod(int i)
        {
            return i + 1;
        }

        [Fact]
        public static void EventsTest_RunTest()
        {
            dynamic d = new EventsTest();
            d.E += new AddDelegate(EventsTest.InvokedMethod);
            d.DoEvent(6);
        }
    }
}
