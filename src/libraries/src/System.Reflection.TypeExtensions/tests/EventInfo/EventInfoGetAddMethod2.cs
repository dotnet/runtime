// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;

namespace System.Reflection.Tests
{
    // System.Reflection.EventInfo.GetAddMethod(System.Boolean)
    public class EventInfoGetAddMethod2
    {
        public delegate void Delegate1(int i);
        public delegate void Delegate2();

        // Positive Test 1:The Event is public
        [Fact]
        public void PosTest1()
        {
            TestClass1 tc1 = new TestClass1();
            tc1.Event1 += new Delegate1(tc1.method1);
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event1");
            MethodInfo methodinfo = eventinfo.GetAddMethod(true);
            Assert.Equal("Void add_Event1(Delegate1)", methodinfo.ToString());
        }

        // Positive Test 2:The Event is private and the param of Nonpublic is true
        [Fact]
        public void PosTest2()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event3", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo methodinfo = eventinfo.GetAddMethod(true);
            Assert.Equal("Void add_Event3(Delegate1)", methodinfo.ToString());
        }

        // Positive Test 3:The Event is private and the param of Nonpublic is false
        [Fact]
        public void PosTest3()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event3", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo methodinfo = eventinfo.GetAddMethod(false);
            Assert.Null(methodinfo);
        }

        // Positive Test 4:The Event is protected and the param of Nonpublic is true
        [Fact]
        public void PosTest4()
        {
            TestClass1 tc1 = new TestClass1();
            tc1.Event2 += new Delegate2(tc1.method2);
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event2", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo methodinfo = eventinfo.GetAddMethod(true);
            Assert.Equal("Void add_Event2(Delegate2)", methodinfo.ToString());
        }

        // Positive Test 5:The Event is protected and the param of Nonpublic is false
        [Fact]
        public void PosTest5()
        {
            TestClass1 tc1 = new TestClass1();
            tc1.Event2 += new Delegate2(tc1.method2);
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event2", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo methodinfo = eventinfo.GetAddMethod(false);
            Assert.Null(methodinfo);
        }

        // Positive Test 6:The Event is protected internal and the param of Nonpublic is true
        [Fact]
        public void PosTest6()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event4", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo methodinfo = eventinfo.GetAddMethod(true);
            Assert.Equal("Void add_Event4(Delegate2)", methodinfo.ToString());
        }

        // Positive Test 7:The Event is protected internal and the param of Nonpublic is false
        [Fact]
        public void PosTest7()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event4", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo methodinfo = eventinfo.GetAddMethod(false);
            Assert.Null(methodinfo);
        }

        public class TestClass1
        {
            public event Delegate1 Event1;
            protected internal event Delegate2 Event2;
            private event Delegate1 Event3;
            protected event Delegate2 Event4;
            public void method()
            {
                if (Event1 != null)
                {
                    Event1(0);
                }
                if (Event2 != null)
                {
                    Event2();
                }
                if (Event3 != null)
                {
                    Event3(0);
                }
                if (Event4 != null)
                {
                    Event4();
                }
            }
            public void method1(int i)
            {
                Debug.WriteLine(i);
            }
            public void method2()
            {
            }
        }
    }
}
