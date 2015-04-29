// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests
{
    // System.Reflection.EventInfo.Attributes
    public class EventInfoAttributesProperty
    {
        public delegate void Delegate1();
        public delegate void Delegate2();

        // Positive Test 1: The eventinfo corresponding public event
        [Fact]
        public void PosTest1()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event1");
            EventAttributes eventattribute = eventinfo.Attributes;
            Assert.Equal(EventAttributes.None, eventattribute);
        }

        // Positive Test 2: The eventinfo corresponding private event
        [Fact]
        public void PosTest2()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event2", BindingFlags.NonPublic | BindingFlags.Instance);
            EventAttributes eventattribute = eventinfo.Attributes;
            Assert.Equal(EventAttributes.None, eventattribute);
        }

        // Positive Test 3: The eventinfo corresponding protected event
        [Fact]
        public void PosTest3()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event3", BindingFlags.NonPublic | BindingFlags.Instance);
            EventAttributes eventattribute = eventinfo.Attributes;
            Assert.Equal(EventAttributes.None, eventattribute);
        }

        // Positive Test 4: The eventinfo corresponding internal event
        [Fact]
        public void PosTest4()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event4", BindingFlags.NonPublic | BindingFlags.Instance);
            EventAttributes eventattribute = eventinfo.Attributes;
            Assert.Equal(EventAttributes.None, eventattribute);
        }

        public class TestClass
        {
            public event Delegate1 Event1
            {
                add { Event1 += new Delegate1(new TestClass().method1); }
                remove { Event1 -= new Delegate1(new TestClass().method1); }
            }
            private event Delegate1 Event2
            {
                add { Event2 += new Delegate1(new TestClass().method1); }
                remove { Event2 -= new Delegate1(new TestClass().method1); }
            }
            protected event Delegate1 Event3
            {
                add { Event3 += new Delegate1(new TestClass().method1); }
                remove { Event3 -= new Delegate1(new TestClass().method1); }
            }
            internal event Delegate1 Event4
            {
                add { Event4 += new Delegate1(new TestClass().method1); }
                remove { Event4 -= new Delegate1(new TestClass().method1); }
            }
            public void method1() { }
        }
    }
}