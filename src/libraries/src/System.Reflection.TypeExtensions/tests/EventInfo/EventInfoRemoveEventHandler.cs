// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    // System.Reflection.EventInfo.RemoveEvenHandler(System object,System delegate)
    public class EventInfoRemoveEventHandler
    {
        public delegate void TestForEvent1();

        // Positive Test 1:First Add Event handler  to the not static event then Remove
        [Fact]
        public void PosTest1()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event1");
            eventinfo.AddEventHandler(tc1, new TestForEvent1(tc1.method1));
            tc1.method01();
            eventinfo.RemoveEventHandler(tc1, new TestForEvent1(tc1.method1));
            tc1.method01();
            Assert.Equal(1, TestClass1.m_StaticVariable1);
        }

        // Positive Test 2:First Add Event handler to the static event and the target is null then Remove
        [Fact]
        public void PosTest2()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event2");
            eventinfo.AddEventHandler(null, new TestForEvent1(tc1.method2));
            tc1.method02();
            eventinfo.RemoveEventHandler(null, new TestForEvent1(tc1.method2));
            tc1.method02();
            Assert.Equal(1, TestClass1.m_StaticVariable2);
        }

        // Positive Test 3:First Add Event handler to the static event and the target is not null then Remove
        [Fact]
        public void PosTest3()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event2");
            eventinfo.AddEventHandler(tc1, new TestForEvent1(tc1.method3));
            tc1.method02();
            eventinfo.RemoveEventHandler(tc1, new TestForEvent1(tc1.method3));
            tc1.method02();
            Assert.Equal(1, TestClass1.m_StaticVariable3);
        }

        // Negative Test 1:The event does not have a public add accessor
        [Fact]
        public void NegTest1()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event3", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Throws<InvalidOperationException>(() =>
           {
               eventinfo.RemoveEventHandler(tc1, new TestForEvent1(tc1.method3));
           });
        }

        // Negative Test 2:The EventInfo is not declared on the target
        [Fact]
        public void NegTest2()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event1");
            TestClass2 tc2 = new TestClass2();
            Exception e = Assert.ThrowsAny<Exception>(() => eventinfo.RemoveEventHandler(tc2, new TestForEvent1(tc1.method1)));
            Assert.Contains("TargetException", e.GetType().FullName);
        }

        // Negative Test 3:add to Event handler to the not static event and the target is null
        [Fact]
        public void NegTest3()
        {
            TestClass1 tc1 = new TestClass1();
            Type tpA = tc1.GetType();
            EventInfo eventinfo = tpA.GetEvent("Event1");
            Exception e = Assert.ThrowsAny<Exception>(() => eventinfo.RemoveEventHandler(null, new TestForEvent1(tc1.method1)));
            Assert.Contains("TargetException", e.GetType().FullName);
        }

        public class TestClass1
        {
            public static int m_StaticVariable1 = 0;
            public static int m_StaticVariable2 = 0;
            public static int m_StaticVariable3 = 0;
            public event TestForEvent1 Event1;
            public static event TestForEvent1 Event2;
            private event TestForEvent1 Event3;
            public void method01()
            {
                if (Event1 != null)
                {
                    Event1();
                }
            }
            public void method02()
            {
                if (Event2 != null)
                {
                    Event2();
                }
            }
            public void method03()
            {
                if (Event3 != null)
                {
                    Event3();
                }
            }
            public void method1()
            {
                m_StaticVariable1++;
            }
            public void method2()
            {
                m_StaticVariable2++;
            }
            public void method3()
            {
                m_StaticVariable3++;
            }
        }
        public class TestClass2
        {
        }
    }
}
