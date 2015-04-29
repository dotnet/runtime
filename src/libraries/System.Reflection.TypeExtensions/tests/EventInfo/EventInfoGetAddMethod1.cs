// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests
{
    // System.Reflection.EventInfo.GetAddMethod()
    public class EventInfoGetAddMethod1
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
            MethodInfo methodinfo = eventinfo.GetAddMethod();
            Assert.Equal("Void add_Event1(Delegate1)", methodinfo.ToString());
        }

        // Positive Test 2:The Event is private
        [Fact]
        public void PosTest2()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event3", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo methodinfo = eventinfo.GetAddMethod();
            Assert.Null(methodinfo);
        }

        // Positive Test 3:The Event is protected
        [Fact]
        public void PosTest3()
        {
            TestClass1 tc1 = new TestClass1();
            tc1.Event2 += new Delegate2(tc1.method2);
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event2", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo methodinfo = eventinfo.GetAddMethod();
            Assert.Null(methodinfo);
        }

        // Positive Test 4:The Event is protected internal
        [Fact]
        public void PosTest4()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event4", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo methodinfo = eventinfo.GetAddMethod();
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
