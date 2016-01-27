// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    // System.Reflection.EventInfo.EventHandlerType
    public class EventInfoEventHandlerType
    {
        public delegate void Delegate();

        // Positive Test 1: The event is public
        [Fact]
        public void PosTest1()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event1");
            Type eventinfoType = eventinfo.EventHandlerType;
            Assert.Equal(typeof(Delegate), eventinfoType);
        }

        // Positive Test 2: The event is private
        [Fact]
        public void PosTest2()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event2", BindingFlags.NonPublic | BindingFlags.Instance);
            Type eventinfoType = eventinfo.EventHandlerType;
            Assert.Equal(typeof(Delegate), eventinfoType);
        }

        // Positive Test 3: The event is protected
        [Fact]
        public void PosTest3()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event3", BindingFlags.NonPublic | BindingFlags.Instance);
            Type eventinfoType = eventinfo.EventHandlerType;
            Assert.Equal(typeof(Delegate), eventinfoType);
        }

        // Positive Test 4: The event is internal
        [Fact]
        public void PosTest4()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event4", BindingFlags.NonPublic | BindingFlags.Instance);
            Type eventinfoType = eventinfo.EventHandlerType;
            Assert.Equal(typeof(Delegate), eventinfoType);
        }

#pragma warning disable 0067
        public class TestClass
        {
            public event Delegate Event1;
            private event Delegate Event2;
            protected event Delegate Event3;
            internal event Delegate Event4;
            public void method01()
            {
                Event1 += new Delegate(method1);
            }
            public void method02()
            {
                Event2 += new Delegate(method1);
            }
            public void method03()
            {
                Event3 += new Delegate(method1);
            }
            public void method04()
            {
                Event4 += new Delegate(method1);
            }
            public void method1() { }
        }
#pragma warning restore 0067
    }
}
