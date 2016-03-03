// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0067

namespace System.Reflection.Tests
{
    public class EventInfo_Members
    {
        // Verify AddEventHandler and RemoveEventHandler for EventInfo
        [Fact]
        public void IsMulticast()
        {
            Assert.True(typeof(MyClass).GetTypeInfo().GetEvent("publicEvent").IsMulticast);
            Assert.True(typeof(MyClass).GetTypeInfo().GetEvent("privateEvent", BindingFlags.NonPublic | BindingFlags.Instance).IsMulticast);
        }

        // Verify AddEventHandler and RemoveEventHandler for EventInfo
        [Fact]
        public void GetAddMethod()
        {
            EventInfo eventInfo = typeof(MyClass).GetTypeInfo().GetEvent("publicEvent");
            MethodInfo mi = eventInfo.GetAddMethod();
            Assert.Equal("add_publicEvent", mi.Name);
            Assert.Equal(1, mi.GetParameters().Length);
            Assert.Equal(typeof(CallMe), mi.GetParameters()[0].ParameterType);

            eventInfo = typeof(MyClass).GetTypeInfo().GetEvent("privateEvent", BindingFlags.NonPublic | BindingFlags.Instance);
            mi = eventInfo.GetAddMethod();
            Assert.Null(mi);

            mi = eventInfo.GetAddMethod(false);
            Assert.Null(mi);

            mi = eventInfo.GetAddMethod(true);
            Assert.Equal("add_privateEvent", mi.Name);
            Assert.Equal(1, mi.GetParameters().Length);
            Assert.Equal(typeof(CallMe), mi.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void GetRemoveMethod()
        {
            EventInfo eventInfo = typeof(MyClass).GetTypeInfo().GetEvent("publicEvent");
            MethodInfo mi = eventInfo.GetRemoveMethod();
            Assert.Equal("remove_publicEvent", mi.Name);
            Assert.Equal(1, mi.GetParameters().Length);
            Assert.Equal(typeof(CallMe), mi.GetParameters()[0].ParameterType);

            eventInfo = typeof(MyClass).GetTypeInfo().GetEvent("privateEvent", BindingFlags.NonPublic | BindingFlags.Instance);
            mi = eventInfo.GetRemoveMethod();
            Assert.Null(mi);

            mi = eventInfo.GetRemoveMethod(false);
            Assert.Null(mi);

            mi = eventInfo.GetRemoveMethod(true);
            Assert.Equal("remove_privateEvent", mi.Name);
            Assert.Equal(1, mi.GetParameters().Length);
            Assert.Equal(typeof(CallMe), mi.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void GetRaiseMethod()
        {
            // There is no god way to support this in c#

            // There is no god way to support this in c#
            EventInfo eventInfo = typeof(MyClass).GetTypeInfo().GetEvent("publicEvent");

            MethodInfo mi = eventInfo.GetRaiseMethod();
            Assert.Null(mi);

            mi = eventInfo.GetRaiseMethod(false);
            Assert.Null(mi);

            mi = eventInfo.GetRaiseMethod(true);
            Assert.Null(mi);


            eventInfo = typeof(MyClass).GetTypeInfo().GetEvent("privateEvent", BindingFlags.NonPublic | BindingFlags.Instance);

            mi = eventInfo.GetRaiseMethod();
            Assert.Null(mi);

            mi = eventInfo.GetRaiseMethod(false);
            Assert.Null(mi);

            mi = eventInfo.GetRaiseMethod(true);
            Assert.Null(mi);
        }


        public delegate void CallMe(string x);

        public class MyClass
        {
            public event CallMe publicEvent;
            private event CallMe privateEvent;
        }
    }
}
