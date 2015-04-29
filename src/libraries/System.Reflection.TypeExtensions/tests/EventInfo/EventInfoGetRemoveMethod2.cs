// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests
{
    // System.Reflection.EventInfo.GetRemoveMethod(System Boolean)
    public class EventInfoGetRemoveMethod2
    {
        public delegate void Delegate1();

        // Positive Test 1:The Event is public and the param Nonpublic is true
        [Fact]
        public void PosTest1()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event1");
            MethodInfo methodinfo = eventinfo.GetRemoveMethod(true);
            Assert.Equal("Void remove_Event1(Delegate1)", methodinfo.ToString());
        }

        // Positive Test 2:The Event is public and the param Nonpublic is false
        [Fact]
        public void PosTest2()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event1");
            MethodInfo methodinfo = eventinfo.GetRemoveMethod(false);
            Assert.Equal("Void remove_Event1(Delegate1)", methodinfo.ToString());
        }

        // Positive Test 3:The Event is private and the param Nonpublic is true
        [Fact]
        public void PosTest3()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event2", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo methodinfo = eventinfo.GetRemoveMethod(true);
            Assert.Equal("Void remove_Event2(Delegate1)", methodinfo.ToString());
        }

        // Positive Test 4:The Event is private and the param Nonpublic is false
        [Fact]
        public void PosTest4()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event2", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo methodinfo = eventinfo.GetRemoveMethod(false);
            Assert.Null(methodinfo);
        }

        // Positive Test 5:The Event is protected and the param Nonpublic is true
        [Fact]
        public void PosTest5()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event3", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo methodinfo = eventinfo.GetRemoveMethod(true);
            Assert.Equal("Void remove_Event3(Delegate1)", methodinfo.ToString());
        }

        // Positive Test 6:The Event is protected and the param Nonpublic is false
        [Fact]
        public void PosTest6()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event3", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo methodinfo = eventinfo.GetRemoveMethod(false);
            Assert.Null(methodinfo);
        }

        public class TestClass1
        {
            public event Delegate1 Event1;
            private event Delegate1 Event2;
            protected event Delegate1 Event3;
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
            }
            public void method1()
            {
            }
        }
    }
}