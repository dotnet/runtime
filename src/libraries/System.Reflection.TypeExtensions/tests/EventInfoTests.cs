// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            yield return new object[] { TypeExtensions.GetEvent(typeof(EI_Class), "PublicEvent"), tc1, new VoidDelegate(tc1.PublicVoidMethod1), 1 };
            yield return new object[] { TypeExtensions.GetEvent(typeof(EI_Class), "PublicStaticEvent"), null, new VoidDelegate(tc1.ProtectedInternalVoidMethod), 2 };
            yield return new object[] { TypeExtensions.GetEvent(typeof(EI_Class), "PublicStaticEvent"), tc1, new VoidDelegate(tc1.PublicVoidMethod2), 3 };
        }

        [Theory]
        [MemberData(nameof(AddEventHandler_TestData))]
        public void AddEventHandler_RemoveEventHandler(EventInfo eventInfo, EI_Class target, Delegate handler, int expectedStaticVariable)
        {
            // Add and make sure we bound the event.
            eventInfo.AddEventHandler(target, handler);
            target?.InvokeAllEvents();
            EI_Class.InvokeStaticEvent();
            Assert.Equal(expectedStaticVariable, EI_Class.AddEventHandler_RemoveEventHandler_Test_TrackingVariable);
            EI_Class.AddEventHandler_RemoveEventHandler_Test_TrackingVariable = 0; // Reset

            // Remove and make sure we unbound the event.
            eventInfo.RemoveEventHandler(target, handler);
            target?.InvokeAllEvents();
            EI_Class.InvokeStaticEvent();
            Assert.Equal(0, EI_Class.AddEventHandler_RemoveEventHandler_Test_TrackingVariable);
        }

        public static IEnumerable<object[]> AddEventHandler_Invalid_TestData()
        {
            // Null target for instance method
            EI_Class tc1 = new EI_Class();
            yield return new object[] { TypeExtensions.GetEvent(typeof(EI_Class), "PublicEvent"), null, new VoidDelegate(tc1.ProtectedInternalVoidMethod), typeof(TargetException) };

            // Event not declared on target
            yield return new object[] { TypeExtensions.GetEvent(typeof(EI_Class), "PublicEvent"), new DummyClass(), new VoidDelegate(tc1.ProtectedInternalVoidMethod), typeof(TargetException) };

            // Event does not have a public add accessor
            yield return new object[] { TypeExtensions.GetEvent(typeof(EI_Class), "PrivateEvent", BindingFlags.NonPublic | BindingFlags.Instance), new DummyClass(), new VoidDelegate(tc1.ProtectedInternalVoidMethod), typeof(InvalidOperationException) };
        }

        [Theory]
        [MemberData(nameof(AddEventHandler_Invalid_TestData))]
        public void AddEventHandler_Invalid(EventInfo eventInfo, object target, Delegate handler, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => eventInfo.AddEventHandler(target, handler));
            Assert.Throws(exceptionType, () => eventInfo.RemoveEventHandler(target, handler));
        }

        [Theory]
        [InlineData(nameof(EI_Class.PublicEvent))]
        [InlineData(nameof(EI_Class.Public_Event))]
        [InlineData("PrivateEvent")]
        [InlineData("ProtectedEvent")]
        [InlineData(nameof(EI_Class.ProtectedInternalEvent))]
        public void Attributes_IsSpecialName(string name)
        {
            EventInfo eventInfo = TypeExtensions.GetEvent(typeof(EI_Class), name, Helpers.AllFlags);
            Assert.Equal(EventAttributes.None, eventInfo.Attributes);
            Assert.False(eventInfo.IsSpecialName);
        }

        [Theory]
        [InlineData(nameof(EI_Class.PublicEvent), typeof(VoidDelegate))]
        [InlineData("PrivateEvent", typeof(VoidDelegate))]
        [InlineData("ProtectedEvent", typeof(VoidDelegate))]
        [InlineData(nameof(EI_Class.ProtectedInternalEvent), typeof(VoidDelegate))]
        public void EventHandlerType(string name, Type expected)
        {
            EventInfo eventInfo = TypeExtensions.GetEvent(typeof(EI_Class), name, Helpers.AllFlags);
            Assert.Equal(expected, eventInfo.EventHandlerType);
        }

        [Theory]
        [InlineData(nameof(EI_Class.PublicEvent), "Void add_PublicEvent(System.Reflection.Tests.VoidDelegate)", false)]
        [InlineData("PrivateEvent", "Void add_PrivateEvent(System.Reflection.Tests.VoidDelegate)", true)]
        [InlineData("ProtectedEvent", "Void add_ProtectedEvent(System.Reflection.Tests.VoidDelegate)", true)]
        [InlineData(nameof(EI_Class.ProtectedInternalEvent), "Void add_ProtectedInternalEvent(System.Reflection.Tests.VoidDelegate)", true)]
        public void GetAddMethod(string name, string expectedToString, bool nonPublic)
        {
            EventInfo eventInfo = TypeExtensions.GetEvent(typeof(EI_Class), name, Helpers.AllFlags);
            MethodInfo method = eventInfo.GetAddMethod();
            Assert.Equal(nonPublic, method == null);
            if (method != null)
            {
                Assert.Equal(expectedToString, method.ToString());
            }

            method = eventInfo.GetAddMethod(false);
            Assert.Equal(nonPublic, method == null);
            if (method != null)
            {
                Assert.Equal(expectedToString, method.ToString());
            }

            method = eventInfo.GetAddMethod(true);
            Assert.NotNull(method);
            Assert.Equal(expectedToString, method.ToString());
        }

        [Theory]
        [InlineData(nameof(EI_Class.PublicEvent))]
        [InlineData("PrivateEvent")]
        [InlineData("ProtectedEvent")]
        [InlineData(nameof(EI_Class.ProtectedInternalEvent))]
        public void GetRaiseMethod(string name)
        {
            EventInfo eventInfo = TypeExtensions.GetEvent(typeof(EI_Class), name, Helpers.AllFlags);
            Assert.Null(eventInfo.GetRaiseMethod());
            Assert.Null(eventInfo.GetRaiseMethod(false));
            Assert.Null(eventInfo.GetRaiseMethod(true));
        }

        [Theory]
        [InlineData(nameof(EI_Class.PublicEvent), "Void remove_PublicEvent(System.Reflection.Tests.VoidDelegate)", false)]
        [InlineData("PrivateEvent", "Void remove_PrivateEvent(System.Reflection.Tests.VoidDelegate)", true)]
        [InlineData("ProtectedEvent", "Void remove_ProtectedEvent(System.Reflection.Tests.VoidDelegate)", true)]
        [InlineData(nameof(EI_Class.ProtectedInternalEvent), "Void remove_ProtectedInternalEvent(System.Reflection.Tests.VoidDelegate)", true)]
        public void GetRemoveMethod(string name, string expectedToString, bool nonPublic)
        {
            EventInfo eventInfo = TypeExtensions.GetEvent(typeof(EI_Class), name, Helpers.AllFlags);
            MethodInfo method = eventInfo.GetRemoveMethod();
            Assert.Equal(nonPublic, method == null);
            if (method != null)
            {
                Assert.Equal(expectedToString, method.ToString());
            }

            method = eventInfo.GetRemoveMethod(false);
            Assert.Equal(nonPublic, method == null);
            if (method != null)
            {
                Assert.Equal(expectedToString, method.ToString());
            }

            method = eventInfo.GetRemoveMethod(true);
            Assert.NotNull(method);
            Assert.Equal(expectedToString, method.ToString());
        }

        [Theory]
        [InlineData("PublicEvent")]
        [InlineData("ProtectedEvent")]
        [InlineData("PrivateEvent")]
        [InlineData("InternalEvent")]
        public void DeclaringType_Module(string name)
        {
            EventInfo eventInfo = TypeExtensions.GetEvent(typeof(EI_Class), name, Helpers.AllFlags);
            Assert.Equal(typeof(EI_Class), eventInfo.DeclaringType);
            Assert.Equal(typeof(EI_Class).GetTypeInfo().Module, eventInfo.Module);
        }

        [Theory]
        [InlineData("PublicEvent")]
        [InlineData("ProtectedEvent")]
        [InlineData("PrivateEvent")]
        [InlineData("InternalEvent")]
        public void Name(string name)
        {
            EventInfo eventInfo = TypeExtensions.GetEvent(typeof(EI_Class), name, Helpers.AllFlags);
            Assert.Equal(name, eventInfo.Name);
        }
    }

    public class EI_Class
    {
        public static int AddEventHandler_RemoveEventHandler_Test_TrackingVariable = 0;

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
            PrivateEvent?.Invoke();
            InternalEvent?.Invoke();
            ProtectedEvent?.Invoke();
            ProtectedInternalEvent?.Invoke();
        }

        public static void InvokeStaticEvent()
        {
            PublicStaticEvent?.Invoke();
        }

        public void PublicVoidMethod1() => AddEventHandler_RemoveEventHandler_Test_TrackingVariable += 1;
        protected internal void ProtectedInternalVoidMethod() => AddEventHandler_RemoveEventHandler_Test_TrackingVariable += 2;
        public void PublicVoidMethod2() => AddEventHandler_RemoveEventHandler_Test_TrackingVariable += 3;
    }

    public class DummyClass { }
}
