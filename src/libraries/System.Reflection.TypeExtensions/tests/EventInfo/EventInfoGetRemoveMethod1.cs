// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests
{
    // System.Reflection.EventInfo.GetRemoveMethod()
    public class EventInfoGetRemoveMethod1
    {
        public delegate void Delegate1();

        // Positive Test 1:The Event is public
        [Fact]
        public void PosTest1()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event1");
            MethodInfo methodinfo = eventinfo.GetRemoveMethod();
            Assert.Equal("Void remove_Event1(Delegate1)", methodinfo.ToString());
        }

        // Positive Test 2:The Event is private
        [Fact]
        public void PosTest2()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event2", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo methodinfo = eventinfo.GetRemoveMethod();
            Assert.Null(methodinfo);
        }

        // Positive Test 3:The Event is protected
        [Fact]
        public void PosTest3()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event3", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo methodinfo = eventinfo.GetRemoveMethod();
            Assert.Null(methodinfo);
        }

        // Positive Test 4:The Event defined in private class is public
        [Fact]
        public void PosTest4()
        {
            MyClass tc1 = new MyClass();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo methodinfo = eventinfo.GetRemoveMethod();
            Assert.Equal("Void remove_Event(Delegate1)", methodinfo.ToString());
        }

        // Positive Test 5:The Event is internal
        [Fact]
        public void PosTest5()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event4", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo methodinfo = eventinfo.GetRemoveMethod();
            Assert.Null(methodinfo);
        }

        private class MyClass
        {
            public event Delegate1 Event;
            public void method()
            {
                if (Event != null)
                {
                    Event += new Delegate1(method1);
                    Event -= new Delegate1(method1);
                    Event();
                }
            }
            public void method1() { }
        }

        public class TestClass1
        {
            public event Delegate1 Event1;
            private event Delegate1 Event2;
            protected event Delegate1 Event3;
            internal event Delegate1 Event4;
            public void method()
            {
                if (Event1 != null)
                {
                    Event1 += new Delegate1(method1);
                    Event1 -= new Delegate1(method1);
                    Event1();
                }
                if (Event2 != null)
                {
                    Event2 += new Delegate1(method1);
                    Event2 -= new Delegate1(method1);
                    Event2();
                }
                if (Event3 != null)
                {
                    Event3 += new Delegate1(method1);
                    Event3 -= new Delegate1(method1);
                    Event3();
                }
                if (Event4 != null)
                {
                    Event4 += new Delegate1(method1);
                    Event4 -= new Delegate1(method1);
                    Event4();
                }
            }
            public void method1()
            {
            }
        }
    }
}
