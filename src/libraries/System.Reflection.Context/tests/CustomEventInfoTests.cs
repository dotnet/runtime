// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    // Test type with an event for coverage
    internal class TypeWithEvent
    {
        public event EventHandler TestEvent;
        public event EventHandler<EventArgs> GenericEvent;

        public void RaiseEvent()
        {
            TestEvent?.Invoke(this, EventArgs.Empty);
            GenericEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public class CustomEventInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly EventInfo _customEvent;
        private readonly TypeInfo _customTypeInfo;

        public CustomEventInfoTests()
        {
            TypeInfo typeInfo = typeof(TypeWithEvent).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
            _customEvent = _customTypeInfo.GetEvent("TestEvent");
        }

        [Fact]
        public void Attributes_ReturnsValue()
        {
            EventAttributes attrs = _customEvent.Attributes;
            Assert.Equal(EventAttributes.None, attrs);
        }

        [Fact]
        public void DeclaringType_ReturnsCustomType()
        {
            Assert.Equal(ProjectionConstants.CustomType, _customEvent.DeclaringType.GetType().FullName);
        }

        [Fact]
        public void EventHandlerType_ReturnsProjectedType()
        {
            Type handlerType = _customEvent.EventHandlerType;
            Assert.NotNull(handlerType);
            Assert.Equal(ProjectionConstants.CustomType, handlerType.GetType().FullName);
        }

        [Fact]
        public void IsMulticast_ReturnsTrue()
        {
            Assert.True(_customEvent.IsMulticast);
        }

        [Fact]
        public void MetadataToken_ReturnsPositiveValue()
        {
            int token = _customEvent.MetadataToken;
            Assert.InRange(token, 1, int.MaxValue);
        }

        [Fact]
        public void Module_ReturnsCustomModule()
        {
            Assert.Equal(ProjectionConstants.CustomModule, _customEvent.Module.GetType().FullName);
        }

        [Fact]
        public void Name_ReturnsValue()
        {
            Assert.Equal("TestEvent", _customEvent.Name);
        }

        [Fact]
        public void ReflectedType_ReturnsCustomType()
        {
            Assert.Equal(ProjectionConstants.CustomType, _customEvent.ReflectedType.GetType().FullName);
        }

        [Fact]
        public void GetAddMethod_ReturnsProjectedMethod()
        {
            MethodInfo addMethod = _customEvent.GetAddMethod(false);
            Assert.NotNull(addMethod);
            Assert.Contains("add_TestEvent", addMethod.Name);
        }

        [Fact]
        public void GetRemoveMethod_ReturnsProjectedMethod()
        {
            MethodInfo removeMethod = _customEvent.GetRemoveMethod(false);
            Assert.NotNull(removeMethod);
            Assert.Contains("remove_TestEvent", removeMethod.Name);
        }

        [Fact]
        public void GetRaiseMethod_ReturnsNull()
        {
            // C# events don't have raise methods by default
            MethodInfo raiseMethod = _customEvent.GetRaiseMethod(false);
            Assert.Null(raiseMethod);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15191", TestRuntimes.Mono)]
        public void GetOtherMethods_ReturnsEmpty()
        {
            MethodInfo[] otherMethods = _customEvent.GetOtherMethods(false);
            Assert.Empty(otherMethods);
        }

        [Fact]
        public void AddEventHandler_Works()
        {
            var target = new TypeWithEvent();
            bool called = false;
            EventHandler handler = (s, e) => called = true;

            _customEvent.AddEventHandler(target, handler);
            target.RaiseEvent();

            Assert.True(called);
        }

        [Fact]
        public void RemoveEventHandler_Works()
        {
            var target = new TypeWithEvent();
            bool called = false;
            EventHandler handler = (s, e) => called = true;

            _customEvent.AddEventHandler(target, handler);
            _customEvent.RemoveEventHandler(target, handler);
            target.RaiseEvent();

            Assert.False(called);
        }

        [Fact]
        public void GetCustomAttributes_WithType_ReturnsEmptyForUnattributedEvent()
        {
            object[] attributes = _customEvent.GetCustomAttributes(typeof(Attribute), true);
            Assert.Empty(attributes);
        }

        [Fact]
        public void GetCustomAttributes_NoType_ReturnsEmptyForUnattributedEvent()
        {
            object[] attributes = _customEvent.GetCustomAttributes(false);
            Assert.Empty(attributes);
        }

        [Fact]
        public void GetCustomAttributesData_ReturnsEmptyForUnattributedEvent()
        {
            IList<CustomAttributeData> data = _customEvent.GetCustomAttributesData();
            Assert.Empty(data);
        }

        [Fact]
        public void IsDefined_ReturnsFalseForUnattributedEvent()
        {
            bool isDefined = _customEvent.IsDefined(typeof(Attribute), true);
            Assert.False(isDefined);
        }

        [Fact]
        public void ToString_ReturnsValue()
        {
            string str = _customEvent.ToString();
            Assert.Contains("TestEvent", str);
        }

        [Fact]
        public void Equals_SameEvent_ReturnsTrue()
        {
            EventInfo sameEvent = _customTypeInfo.GetEvent("TestEvent");
            Assert.True(_customEvent.Equals(sameEvent));
        }

        [Fact]
        public void GetHashCode_IsIdempotent()
        {
            int hashCode1 = _customEvent.GetHashCode();
            int hashCode2 = _customEvent.GetHashCode();
            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public void GetEvents_ReturnsTwoEvents()
        {
            EventInfo[] events = _customTypeInfo.GetEvents(BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(2, events.Length);
            Assert.Contains(events, e => e.Name == "TestEvent");
            Assert.Contains(events, e => e.Name == "GenericEvent");
        }
    }
}
