// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public class EventInfoTests
    {
        public static IEnumerable<object[]> Events_TestData()
        {
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.PublicEvent) };
            yield return new object[] { typeof(BaseClass), "PrivateEvent" };
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.PublicStaticEvent) };
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.PublicVirtualEvent) };
            yield return new object[] { typeof(SubClass), nameof(BaseClass.PublicEvent) };
            yield return new object[] { typeof(SubClass), nameof(SubClass.EventPublicNew) };
        }

        [Theory]
        [MemberData(nameof(Events_TestData))]
        public void IsMulticast_ReturnsTrue(Type type, string name)
        {
            EventInfo eventInfo = GetEventInfo(type, name);
            Assert.True(eventInfo.IsMulticast);
        }

        [Theory]
        [InlineData(typeof(BaseClass), nameof(BaseClass.PublicEvent), true)]
        [InlineData(typeof(BaseClass), "PrivateEvent", false)]
        public void GetAddMethod(Type type, string name, bool isVisible)
        {
            EventInfo eventInfo = GetEventInfo(type, name);
            Assert.Equal(isVisible, eventInfo.GetAddMethod() != null);
            Assert.Equal(isVisible, eventInfo.GetAddMethod(false) != null);
            Assert.NotNull(eventInfo.GetAddMethod(true));

            MethodInfo addMethod = eventInfo.AddMethod;
            Assert.Equal(addMethod, eventInfo.GetAddMethod(true));
            if (addMethod != null)
            {
                Assert.Equal("add_" + name, addMethod.Name);
                Assert.Equal(1, addMethod.GetParameters().Length);
                Assert.Equal(typeof(void), addMethod.ReturnParameter.ParameterType);
            }
        }

        [Theory]
        [InlineData(typeof(BaseClass), nameof(BaseClass.PublicEvent), true)]
        [InlineData(typeof(BaseClass), "PrivateEvent", false)]
        public void GetRemoveMethod(Type type, string name, bool isVisible)
        {
            EventInfo eventInfo = GetEventInfo(type, name);
            Assert.Equal(isVisible, eventInfo.GetRemoveMethod() != null);
            Assert.Equal(isVisible, eventInfo.GetRemoveMethod(false) != null);
            Assert.NotNull(eventInfo.GetRemoveMethod(true));

            MethodInfo removeMethod = eventInfo.RemoveMethod;
            Assert.Equal(removeMethod, eventInfo.GetRemoveMethod(true));
            if (removeMethod != null)
            {
                Assert.Equal("remove_" + name, removeMethod.Name);
                Assert.Equal(1, removeMethod.GetParameters().Length);
                Assert.Equal(typeof(void), removeMethod.ReturnParameter.ParameterType);
            }
        }

        [Theory]
        [MemberData(nameof(Events_TestData))]
        public void GetRaiseMethod(Type type, string name)
        {
            EventInfo eventInfo = GetEventInfo(type, name);
            Assert.Null(eventInfo.GetRaiseMethod(false));
            Assert.Null(eventInfo.GetRaiseMethod(true));

            Assert.Equal(eventInfo.GetRaiseMethod(true), eventInfo.RaiseMethod);
        }

        public static IEnumerable<object[]> AddRemove_TestData()
        {
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.PublicEvent), new BaseClass() };
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.PublicStaticEvent), null };
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.PublicVirtualEvent), new BaseClass() };
            yield return new object[] { typeof(SubClass), nameof(SubClass.EventPublicNew), new SubClass() };
            yield return new object[] { typeof(SubClass), nameof(SubClass.PublicEvent), new SubClass() };
        }

        [Theory]
        [MemberData(nameof(AddRemove_TestData))]
        public void AddRemove(Type type, string name, object target)
        {
            EventInfo eventInfo = GetEventInfo(type, name);
            EventHandler handler = new EventHandler(ObjectEventArgsHandler);
            eventInfo.AddEventHandler(target, handler);
            eventInfo.RemoveEventHandler(target, handler);
        }

        public static IEnumerable<object[]> AddEventHandler_Invalid_TestData()
        {
            // Target is null
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.PublicEvent), null, new EventHandler(ObjectEventArgsHandler), typeof(TargetException) };

            // Handler is incorrect
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.PublicEvent), "hello", new EventHandler(ObjectEventArgsHandler), typeof(TargetException) };
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.PublicEvent), new BaseClass(), new ObjectDelegate(ObjectHandler), typeof(ArgumentException) };
        }

        [Theory]
        [MemberData(nameof(AddEventHandler_Invalid_TestData))]
        public void AddEventHandler_Invalid(Type type, string name, object target, Delegate handler, Type exceptionType)
        {
            EventInfo eventInfo = GetEventInfo(type, name);
            Assert.Throws(exceptionType, () => eventInfo.AddEventHandler(target, handler));
        }

        public static IEnumerable<object[]> RemoveEventHandler_Invalid_TestData()
        {
            // Target is null
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.PublicEvent), new BaseClass(), new EventHandler(ObjectEventArgsHandler), null, new EventHandler(ObjectEventArgsHandler), typeof(TargetException) };

            // Target is incorrect
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.PublicEvent), new BaseClass(), new EventHandler(ObjectEventArgsHandler), "hello", new EventHandler(ObjectEventArgsHandler), typeof(TargetException) };

            // Handler is incorrect
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.PublicEvent), new BaseClass(), new EventHandler(ObjectEventArgsHandler), new BaseClass(), new ObjectDelegate(ObjectHandler), typeof(ArgumentException) };
        }

        [Theory]
        [MemberData(nameof(RemoveEventHandler_Invalid_TestData))]
        public void RemovEventHandler_Invalid(Type type, string name, object addTarget, Delegate addHandler, object removeTarget, Delegate removeHandler, Type exceptionType)
        {
            EventInfo eventInfo = GetEventInfo(type, name);
            eventInfo.AddEventHandler(addTarget, addHandler);

            Assert.Throws(exceptionType, () => eventInfo.RemoveEventHandler(removeTarget, removeHandler));
        }

        [Theory]
        [InlineData(typeof(BaseClass), nameof(BaseClass.PublicEvent), typeof(BaseClass), nameof(BaseClass.PublicEvent), true)]
        [InlineData(typeof(BaseClass), nameof(BaseClass.PublicEvent), typeof(SubClass), nameof(SubClass.PublicEvent), false)]
        [InlineData(typeof(BaseClass), nameof(BaseClass.PublicEvent), typeof(BaseClass), nameof(BaseClass.PublicStaticEvent), false)]
        public void EqualsTest(Type type1, string name1, Type type2, string name2, bool expected)
        {
            EventInfo eventInfo1 = GetEventInfo(type1, name1);
            EventInfo eventInfo2 = GetEventInfo(type2, name2);
            Assert.Equal(expected, eventInfo1.Equals(eventInfo2));
            if (expected)
            {
                Assert.Equal(eventInfo1.GetHashCode(), eventInfo2.GetHashCode());
            }
        }

        [Theory]
        [MemberData(nameof(Events_TestData))]
        public void EventHandlerType(Type type, string name)
        {
            EventInfo eventInfo = GetEventInfo(type, name);
            Assert.Equal(typeof(EventHandler), eventInfo.EventHandlerType);
        }

        [Theory]
        [MemberData(nameof(Events_TestData))]
        public void Attributes(Type type, string name)
        {
            EventInfo eventInfo = GetEventInfo(type, name);
            Assert.Equal(EventAttributes.None, eventInfo.Attributes);
        }

        [Theory]
        [MemberData(nameof(Events_TestData))]
        public void IsSpecialName_NormalEvent_ReturnsFalse(Type type, string name)
        {
            EventInfo eventInfo = GetEventInfo(type, name);
            Assert.False(eventInfo.IsSpecialName);
        }

        private static void ObjectEventArgsHandler(object o, EventArgs e) { }
        private static void ObjectHandler(object o) { }
        public delegate void ObjectDelegate(object o);

        private static EventInfo GetEventInfo(Type declaringType, string eventName)
        {
            TypeInfo type = declaringType.GetTypeInfo();
            return type.DeclaredEvents.First(declaredEvent => declaredEvent.Name == eventName);
        }

#pragma warning disable 0067
        protected class BaseClass
        {
            private event EventHandler PrivateEvent;
            public event EventHandler PublicEvent;
            public static event EventHandler PublicStaticEvent;
            public virtual event EventHandler PublicVirtualEvent;
        }

        protected class SubClass : BaseClass
        {
            public new event EventHandler PublicEvent;
            public event EventHandler EventPublicNew;
        }
#pragma warning restore 0067
    }
}
