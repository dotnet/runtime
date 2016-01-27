// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    // System.Reflection.EventInfo.GetRaiseMethod()
    public class EventInfoGetRaiseMethod1
    {
        public delegate void Delegate1();

        // Positive Test 1:The Event is public
        [Fact]
        public void PosTest1()
        {
            TestClass tc = new TestClass();
            Type tpA = tc.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event1");
            MethodInfo methodinfo = eventinfo.GetRaiseMethod();
            Assert.Null(methodinfo);
        }

        // Positive Test 2:The Event is private
        [Fact]
        public void PosTest2()
        {
            TestClass tc = new TestClass();
            Type tpA = tc.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event2", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo methodinfo = eventinfo.GetRaiseMethod();
            Assert.Null(methodinfo);
        }

        // Positive Test 3:The Event is protected
        [Fact]
        public void PosTest3()
        {
            TestClass tc = new TestClass();
            Type tpA = tc.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event3", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo methodinfo = eventinfo.GetRaiseMethod();
            Assert.Null(methodinfo);
        }

        public class TestClass
        {
            public event Delegate1 Event1;
            private event Delegate1 Event2;
            protected event Delegate1 Event3;
            public void method()
            {
                if (Event1 != null)
                {
                    Event1 += new Delegate1(method1);
                    Event1();
                }
                if (Event2 != null)
                {
                    Event2 += new Delegate1(method1);
                    Event2();
                }
                if (Event3 != null)
                {
                    Event3 += new Delegate1(method1);
                    Event3();
                }
            }
            public void method1() { }
        }
    }
}
