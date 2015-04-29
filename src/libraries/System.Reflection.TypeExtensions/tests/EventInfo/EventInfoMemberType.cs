// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests
{
    // System.Reflection.EventInfo.MemberTypeProperty
    public class EventInfoMemberType
    {
        public delegate void Delegate1();

        // Positive Test 1:the event is public
        [Fact]
        public void PosTest1()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event1");
            Assert.Equal("System.Reflection.RuntimeEventInfo", eventinfo.GetType().FullName);
        }

        // Positive Test 2:the event is protected
        [Fact]
        public void PosTest2()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event2", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal("System.Reflection.RuntimeEventInfo", eventinfo.GetType().FullName);
        }

        // Positive Test 3:the event is private
        [Fact]
        public void PosTest3()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event3", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal("System.Reflection.RuntimeEventInfo", eventinfo.GetType().FullName);
        }

        // Positive Test 4:the event is internal
        [Fact]
        public void PosTest4()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event4", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal("System.Reflection.RuntimeEventInfo", eventinfo.GetType().FullName);
        }

        public class TestClass
        {
            public event Delegate1 Event1;
            protected event Delegate1 Event2;
            private event Delegate1 Event3;
            internal event Delegate1 Event4;
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
                if (Event4 != null)
                {
                    Event4();
                }
            }
        }
    }
}