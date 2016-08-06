// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

#pragma warning disable 0067

namespace System.Reflection.Tests
{
    public delegate void VoidDelegate();
    public delegate void IntDelegate(int i);

    public class EventInfoTests
    {
        public static IEnumerable<object[]> AddEventHandler_TestData()
        {
            EI_Class tc1 = new EI_Class();
            yield return new object[] { typeof(EI_Class).GetEvent("PublicEvent"), tc1, new VoidDelegate(tc1.PublicVoidMethod1), 1 };
            yield return new object[] { typeof(EI_Class).GetEvent("PublicStaticEvent"), null, new VoidDelegate(tc1.ProtectedInternalVoidMethod), 2 };
            yield return new object[] { typeof(EI_Class).GetEvent("PublicStaticEvent"), tc1, new VoidDelegate(tc1.PublicVoidMethod2), 3 };
        }

        [Theory]
        [MemberData(nameof(AddEventHandler_TestData))]
        public void AddEventHandler_RemoveEventHandler(EventInfo eventInfo, EI_Class target, Delegate handler, int expectedStaticVariable)
        {
            // Add and make sure we bound the event.
            eventInfo.AddEventHandler(target, handler);
            (target ?? new EI_Class()).InvokeAllEvents();
            Assert.Equal(expectedStaticVariable, EI_Class.StaticVariable);
            EI_Class.StaticVariable = 0; // Reset

            // Remove and make sure we unbound the event.
            eventInfo.RemoveEventHandler(target, handler);
            (target ?? new EI_Class()).InvokeAllEvents();
            Assert.Equal(0, EI_Class.StaticVariable);
        }

        public static IEnumerable<object[]> AddEventHandler_Invalid_TestData()
        {
            // Null target for instance method
            EI_Class tc1 = new EI_Class();
            yield return new object[] { typeof(EI_Class).GetEvent("PublicEvent"), null, new VoidDelegate(tc1.ProtectedInternalVoidMethod), typeof(TargetException) };

            // Event not declared on target
            yield return new object[] { typeof(EI_Class).GetEvent("PublicEvent"), new DummyClass(), new VoidDelegate(tc1.ProtectedInternalVoidMethod), typeof(TargetException) };

            // Event does not have a public add accessor
            yield return new object[] { typeof(EI_Class).GetEvent("PrivateEvent", BindingFlags.NonPublic | BindingFlags.Instance), new DummyClass(), new VoidDelegate(tc1.ProtectedInternalVoidMethod), typeof(InvalidOperationException) };
        }
        
        [Theory]
        [MemberData(nameof(AddEventHandler_Invalid_TestData))]
        public void AddEventHandler_Invalid(EventInfo eventInfo, object target, Delegate handler, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => eventInfo.AddEventHandler(target, handler));
            Assert.Throws(exceptionType, () => eventInfo.RemoveEventHandler(target, handler));
        }

        [Theory]
        [InlineData("PublicEvent")]
        [InlineData("Public_Event")]
        [InlineData("Private_Event")]
        [InlineData("PrivateEvent")]
        [InlineData("ProtectedEvent")]
        [InlineData("ProtectedInternalEvent")]
        [InlineData("ProtectedInternalEvent")]
        public void Attributes_IsSpecialName(string name)
        {
            EventInfo eventInfo = Helpers.GetEvent(typeof(EI_Class), name);
            Assert.Equal(EventAttributes.None, eventInfo.Attributes);
            Assert.False(eventInfo.IsSpecialName);
        }

        [Theory]
        [InlineData("PublicEvent", typeof(VoidDelegate))]
        [InlineData("PrivateEvent", typeof(VoidDelegate))]
        [InlineData("ProtectedEvent", typeof(VoidDelegate))]
        [InlineData("ProtectedInternalEvent", typeof(VoidDelegate))]
        public void EventHandlerType(string name, Type expected)
        {
            EventInfo eventInfo = Helpers.GetEvent(typeof(EI_Class), name);
            Assert.Equal(expected, eventInfo.EventHandlerType);
        }

        [Theory]
        [InlineData("PublicEvent", "Void add_PublicEvent(System.Reflection.Tests.VoidDelegate)", "Void add_PublicEvent(System.Reflection.Tests.VoidDelegate)")]
        [InlineData("PrivateEvent", null, "Void add_PrivateEvent(System.Reflection.Tests.VoidDelegate)")]
        [InlineData("ProtectedEvent", null, "Void add_ProtectedEvent(System.Reflection.Tests.VoidDelegate)")]
        [InlineData("ProtectedInternalEvent", null, "Void add_ProtectedInternalEvent(System.Reflection.Tests.VoidDelegate)")]
        public void GetAddMethod(string name, string publicToString, string nonPublicToString)
        {
            EventInfo eventInfo = Helpers.GetEvent(typeof(EI_Class), name);
            MethodInfo method = eventInfo.GetAddMethod();
            Assert.Equal(publicToString == null, method == null);
            Assert.Equal(publicToString, method?.ToString());

            method = eventInfo.GetAddMethod(false);
            Assert.Equal(publicToString == null, method == null);
            Assert.Equal(publicToString, method?.ToString());

            method = eventInfo.GetAddMethod(true);
            Assert.NotNull(method);
            Assert.Equal(nonPublicToString, method.ToString());
        }

        [Theory]
        [InlineData("PublicEvent")]
        [InlineData("PrivateEvent")]
        [InlineData("ProtectedEvent")]
        [InlineData("ProtectedInternalEvent")]
        public void GetRaiseMethod(string name)
        {
            EventInfo eventInfo = Helpers.GetEvent(typeof(EI_Class), name);
            Assert.Null(eventInfo.GetRaiseMethod());
            Assert.Null(eventInfo.GetRaiseMethod(false));
            Assert.Null(eventInfo.GetRaiseMethod(true));
        }

        [Theory]
        [InlineData("PublicEvent", "Void remove_PublicEvent(System.Reflection.Tests.VoidDelegate)", "Void remove_PublicEvent(System.Reflection.Tests.VoidDelegate)")]
        [InlineData("PrivateEvent", null, "Void remove_PrivateEvent(System.Reflection.Tests.VoidDelegate)")]
        [InlineData("ProtectedEvent", null, "Void remove_ProtectedEvent(System.Reflection.Tests.VoidDelegate)")]
        [InlineData("ProtectedInternalEvent", null, "Void remove_ProtectedInternalEvent(System.Reflection.Tests.VoidDelegate)")]
        public void GetRemoveMethod(string name, string publicToString, string nonPublicToString)
        {
            EventInfo eventInfo = Helpers.GetEvent(typeof(EI_Class), name);
            MethodInfo method = eventInfo.GetRemoveMethod();
            Assert.Equal(publicToString == null, method == null);
            Assert.Equal(publicToString, method?.ToString());

            method = eventInfo.GetRemoveMethod(false);
            Assert.Equal(publicToString == null, method == null);
            Assert.Equal(publicToString, method?.ToString());

            method = eventInfo.GetRemoveMethod(true);
            Assert.NotNull(method);
            Assert.Equal(nonPublicToString, method.ToString());
        }

        [Theory]
        [InlineData("PublicEvent")]
        [InlineData("ProtectedEvent")]
        [InlineData("PrivateEvent")]
        [InlineData("InternalEvent")]
        public void GetType_FullName(string name)
        {
            EventInfo eventInfo = Helpers.GetEvent(typeof(EI_Class), name);
            Assert.Equal("System.Reflection.RuntimeEventInfo", eventInfo.GetType().FullName);
        }
    }

    public class EI_Class
    {
        public static int StaticVariable = 0;
        
        public event VoidDelegate PublicEvent;
        public event VoidDelegate Public_Event;
        public static event VoidDelegate PublicStaticEvent;
        private event VoidDelegate PrivateEvent;
        private event VoidDelegate Private_Event;
        internal event VoidDelegate InternalEvent;
        protected event VoidDelegate ProtectedEvent;
        protected internal event VoidDelegate ProtectedInternalEvent;

        public void InvokeAllEvents()
        {
            PublicEvent?.Invoke();
            PublicStaticEvent?.Invoke();
            PrivateEvent?.Invoke();
            InternalEvent?.Invoke();
            ProtectedEvent?.Invoke();
            ProtectedInternalEvent?.Invoke();
        }

        public void PublicVoidMethod1() => StaticVariable += 1;
        protected internal void ProtectedInternalVoidMethod() => StaticVariable += 2;
        public void PublicVoidMethod2() => StaticVariable += 3;
    }

    public class DummyClass { }
}
