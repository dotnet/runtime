// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class EventDescriptorTests
    {
        [Fact]
        public void RaiseAddedEventHandler()
        {
            var component = new DescriptorTestComponent();
            var eventHandlerWasCalled = false;
            Action eventHandler = () => eventHandlerWasCalled = true;
            var eventInfo = typeof(DescriptorTestComponent).GetEvent(nameof(DescriptorTestComponent.ActionEvent));
            EventDescriptor eventDescriptor = TypeDescriptor.CreateEvent(component.GetType(), nameof(component.ActionEvent), typeof(Action));

            eventDescriptor.AddEventHandler(component, eventHandler);
            component.RaiseEvent();

            Assert.True(eventHandlerWasCalled);
        }

        [Fact]
        public void RemoveAddedEventHandler()
        {
            var component = new DescriptorTestComponent();
            Action eventHandler = () => Assert.Fail("EventDescriptor failed to remove an event handler");
            EventDescriptor eventDescriptor = TypeDescriptor.CreateEvent(component.GetType(), nameof(component.ActionEvent), typeof(Action));

            eventDescriptor.AddEventHandler(component, eventHandler);
            eventDescriptor.RemoveEventHandler(component, eventHandler);

            // component.Event should now have no handler => raising it should throw a NullReferenceException
            Assert.Throws<NullReferenceException>(() => component.RaiseEvent());
        }

        [Fact]
        public void CopyConstructorAddsAttribute()
        {
            var oldEventDescriptor = TypeDescriptor.CreateEvent(typeof(DescriptorTestComponent), nameof(DescriptorTestComponent.ActionEvent), typeof(Action));
            var expectedString = "EventDescriptorTests.CopyConstructorAddsAttribute";
            var newAttribute = new DescriptorTestAttribute(expectedString);

            EventDescriptor newEventDescriptor = TypeDescriptor.CreateEvent(typeof(DescriptorTestComponent), oldEventDescriptor, new[] { newAttribute });

            Assert.True(newEventDescriptor.Attributes.Contains(newAttribute));
        }

        [Fact]
        public void GetComponentType()
        {
            var componentType = typeof(DescriptorTestComponent);
            EventDescriptor eventDescriptor = TypeDescriptor.CreateEvent(componentType, nameof(DescriptorTestComponent.ActionEvent), typeof(Action));

            Assert.Equal(componentType, eventDescriptor.ComponentType);
        }

        [Fact]
        public void GetEventType()
        {
            var eventType = typeof(Action);
            var eventDescriptor = TypeDescriptor.CreateEvent(typeof(DescriptorTestComponent), nameof(DescriptorTestComponent.ActionEvent), eventType, null);

            Assert.Equal(eventType, eventDescriptor.EventType);
        }
    }
}
