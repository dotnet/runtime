// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
