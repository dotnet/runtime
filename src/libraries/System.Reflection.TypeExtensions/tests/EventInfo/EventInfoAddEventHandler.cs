// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests
{
    public delegate void TestForEvent1();

    // System.Reflection.EventInfo.AddEventHandler
    public class EventInfoAddEventHandler
    {
        // Positive Test 1: add Event handler to the not static event
        [Fact]
        public void PosTest1()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event1");
            eventinfo.AddEventHandler(tc1, new TestForEvent1(tc1.method1));
            tc1.method();
            Assert.Equal(1, TestClass1.StaticVariable1);
        }

        // Positive Test 2:add to Event handler to the static event and the target is null
        [Fact]
        public void PosTest2()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event2");
            eventinfo.AddEventHandler(null, new TestForEvent1(tc1.method2));
            tc1.method();
            Assert.Equal(-1, TestClass1.StaticVariable2);
        }

        // Positive Test 3:add to Event handler to the static event and the target is not null
        [Fact]
        public void PosTest3()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event2");
            eventinfo.AddEventHandler(tc1, new TestForEvent1(tc1.method3));
            tc1.method();
            Assert.Equal(1, TestClass1.StaticVariable3);
        }

        // Negative Test 1:add to Event handler to the not static event and the target is null
        [Fact]
        public void NegTest1()
        {
            try
            {
                TestClass1 tc1 = new TestClass1();
                Type tpA = tc1.GetType();
                EventInfo eventinfo = tpA.GetEvent("Event1");
                eventinfo.AddEventHandler(null, new TestForEvent1(tc1.method2));
                Assert.True(false);
            }
            catch (Exception e)
            {
                if (e.GetType().ToString() != "System.Reflection.TargetException")
                {
                    Assert.True(false);
                }
            }
        }

        // Negative Test 2:The EventInfo is not declared on the target
        [Fact]
        public void NegTest2()
        {
            try
            {
                TestClass1 tc1 = new TestClass1();
                Type tpA = tc1.GetType();
                EventInfo eventinfo = tpA.GetEvent("Event1");
                TestClass2 tc2 = new TestClass2();
                eventinfo.AddEventHandler(tc2, new TestForEvent1(tc1.method2));
                Assert.True(false);
            }
            catch (Exception e)
            {
                if (!e.GetType().FullName.Equals("System.Reflection.TargetException"))
                {
                    Assert.True(false);
                }
            }
        }

        // Negative Test 3:The event does not have a public add accessor
        [Fact]
        public void NegTest3()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event3", BindingFlags.NonPublic | BindingFlags.Instance);
            TestClass2 tc2 = new TestClass2();
            Assert.Throws<InvalidOperationException>(() =>
           {
               eventinfo.AddEventHandler(tc2, new TestForEvent1(tc1.method2));
           });
        }
    }

    public class TestClass1
    {
        public static int StaticVariable1 = 0; // Incremented by method1
        public static int StaticVariable2 = 0; // Decremented by method2
        public static int StaticVariable3 = 0; // Incremented by method3

        public readonly int m_ConstVariable = 0;
        public event TestForEvent1 Event1;
        public static event TestForEvent1 Event2;
        private event TestForEvent1 Event3;

        public void method()
        {
            if (Event1 != null)
            {
                Event1();
            }
            if (Event2 != null)
            {
                Event2();
            }
            if (Event3 != null)
            {
                Event3();
            }
        }
        public void method1()
        {
            StaticVariable1++;
        }
        protected internal void method2()
        {
            StaticVariable2--;
        }
        public void method3()
        {
            StaticVariable3++;
        }
    }
    public class TestClass2
    {
    }
}