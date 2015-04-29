// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests
{
    // System.Reflection.EventInfo.IsSpecialNameProperty
    public class EventInfoIsSpecialName
    {
        public delegate void Delegate1();

        // Positive Test 1:the event is public and not contain_
        [Fact]
        public void PosTest1()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event1");
            Assert.False(eventinfo.IsSpecialName);
        }

        // Positive Test 2:the event is public but contain_
        [Fact]
        public void PosTest2()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("_Event2");
            Assert.False(eventinfo.IsSpecialName);
        }

        // Positive Test 3:the event is private and not contain_
        [Fact]
        public void PosTest3()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event3", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.False(eventinfo.IsSpecialName);
        }

        // Positive Test 4:the event is private but contain_
        [Fact]
        public void PosTest4()
        {
            Type tpA = typeof(TestClass);
            EventInfo eventinfo = tpA.GetEvent("Event_4", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.False(eventinfo.IsSpecialName);
        }

        public class TestClass
        {
            public event Delegate1 Event1;
            public event Delegate1 _Event2;
            private event Delegate1 Event3;
            private event Delegate1 Event_4;
            public void method()
            {
                if (Event1 != null)
                {
                    Event1();
                }
                if (_Event2 != null)
                {
                    _Event2();
                }
                if (Event3 != null)
                {
                    Event3();
                }
                if (Event_4 != null)
                {
                    Event_4();
                }
            }
        }
    }
}
