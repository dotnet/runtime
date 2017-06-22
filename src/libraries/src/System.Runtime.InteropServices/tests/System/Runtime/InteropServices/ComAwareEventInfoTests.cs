// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
#pragma warning disable 0618 // CompareEventInfo is marked as Obsolete.
    public partial class ComAwareEventInfoTests
    {
        [Fact]
        public void Ctor_Type_EventName()
        {
            EventInfo expectedEvent = typeof(NonComObject).GetEvent(nameof(NonComObject.Event));
            var attribute = new ComAwareEventInfo(typeof(NonComObject), nameof(NonComObject.Event));

            Assert.Equal(expectedEvent.Attributes, attribute.Attributes);
            Assert.Equal(expectedEvent.DeclaringType, attribute.DeclaringType);
            Assert.Equal(expectedEvent.Name, attribute.Name);
            Assert.Equal(expectedEvent.ReflectedType, attribute.ReflectedType);

            Assert.Equal(expectedEvent.GetAddMethod(), attribute.GetAddMethod());
            Assert.Equal(expectedEvent.GetRaiseMethod(), attribute.GetRaiseMethod());
            Assert.Equal(expectedEvent.GetRemoveMethod(), attribute.GetRemoveMethod());

            Assert.Equal(expectedEvent.GetCustomAttributes(typeof(ExcludeFromCodeCoverageAttribute), true), attribute.GetCustomAttributes(typeof(ExcludeFromCodeCoverageAttribute), true));
            Assert.Equal(expectedEvent.GetCustomAttributes(true), attribute.GetCustomAttributes(true));
            Assert.Equal(expectedEvent.IsDefined(typeof(ExcludeFromCodeCoverageAttribute)), attribute.IsDefined(typeof(ExcludeFromCodeCoverageAttribute)));
        }

        [Fact]
        public void Ctor_NullType_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => new ComAwareEventInfo(null, "EventName"));
        }

        [Fact]
        public void Ctor_NullEventName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>(null, () => new ComAwareEventInfo(typeof(NonComObject), null));
        }

        [Fact]
        public void Properties_NoSuchEvent_ThrowsNullReferenceException()
        {
            var attribute = new ComAwareEventInfo(typeof(NonComObject), string.Empty);
            Assert.Throws<NullReferenceException>(() => attribute.Attributes);
            Assert.Throws<NullReferenceException>(() => attribute.DeclaringType);
            Assert.Throws<NullReferenceException>(() => attribute.Name);
            Assert.Throws<NullReferenceException>(() => attribute.ReflectedType);
        }

        [Fact]
        public void Methods_NoSuchEvent_ThrowsNullReferenceException()
        {
            var attribute = new ComAwareEventInfo(typeof(NonComObject), string.Empty);
            Assert.Throws<NullReferenceException>(() => attribute.AddEventHandler(new object(), new EventHandler(EventHandler)));
            Assert.Throws<NullReferenceException>(() => attribute.RemoveEventHandler(new object(), new EventHandler(EventHandler)));
            Assert.Throws<NullReferenceException>(() => attribute.GetAddMethod(false));
            Assert.Throws<NullReferenceException>(() => attribute.GetRaiseMethod(false));
            Assert.Throws<NullReferenceException>(() => attribute.GetRemoveMethod(false));
            Assert.Throws<NullReferenceException>(() => attribute.GetCustomAttributes(typeof(ComVisibleAttribute), false));
            Assert.Throws<NullReferenceException>(() => attribute.GetCustomAttributes(false));
            Assert.Throws<NullReferenceException>(() => attribute.IsDefined(typeof(ComVisibleAttribute), false));
        }

        [Fact]
        public void AddEventHandler_NonCom_Success()
        {
            var attribute = new ComAwareEventInfo(typeof(NonComObject), nameof(NonComObject.Event));
            var target = new NonComObject();
            Delegate handler = new EventHandler(EventHandler);

            attribute.AddEventHandler(target, handler);
            target.Raise(1);
            Assert.True(CalledEventHandler);

            CalledEventHandler = false;
            attribute.RemoveEventHandler(target, handler);
            Assert.False(CalledEventHandler);
        }

        [Fact]
        public void AddEventHandler_NullTarget_ThrowsArgumentNullException()
        {
            var attribute = new ComAwareEventInfo(typeof(NonComObject), nameof(NonComObject.Event));
            AssertExtensions.Throws<ArgumentNullException>("o", () => attribute.AddEventHandler(null, new EventHandler(EventHandler)));
        }

        public bool CalledEventHandler { get; set; }

        public void EventHandler(object sender, EventArgs e)
        {
            Assert.False(CalledEventHandler);
            CalledEventHandler = true;

            Assert.Equal(1, sender);
            Assert.Null(e);
        }

        public class NonComObject
        {
            [ExcludeFromCodeCoverage]
            public event EventHandler Event;

            public void Raise(object sender) => Event.Invoke(1, null);
        }
    }
#pragma warning restore 0618
}
