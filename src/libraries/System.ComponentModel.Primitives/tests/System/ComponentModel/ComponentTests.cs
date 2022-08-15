// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ComponentTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var component = new SubComponent();
            Assert.True(component.CanRaiseEvents);
            Assert.Null(component.Container);
            Assert.False(component.DesignMode);
            Assert.Same(component.Events, component.Events);
            Assert.Null(component.Site);
        }

        public static IEnumerable<object[]> Container_Get_TestData()
        {
            yield return new object[] { new Container() };
            yield return new object[] { null };
        }

        [Theory]
        [MemberData(nameof(Container_Get_TestData))]
        public void Container_GetWithSite_ReturnsExpected(Container result)
        {
            var site = new MockSite
            {
                Container = result
            };
            var component = new Component
            {
                Site = site
            };
            Assert.Same(result, component.Container);
        }

        [Fact]
        public void DesignMode_GetWithCustomSite_ReturnsNull()
        {
            // DesignMode uses the private _site field instead of the Site property.
            var component = new DifferentSiteComponent();
            Assert.Null(component.Container);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DesignMode_GetWithSite_ReturnsExpected(bool result)
        {
            var site = new MockSite
            {
                DesignMode = result
            };
            var component = new SubComponent
            {
                Site = site
            };
            Assert.Equal(result, component.DesignMode);
        }

        [Fact]
        public void DesignMode_GetWithCustomSite_ReturnsFalse()
        {
            // DesignMode uses the private _site field instead of the Site property.
            var component = new DifferentSiteComponent();
            Assert.False(component.DesignMode);
        }

        public static IEnumerable<object[]> Site_Set_TestData()
        {
            yield return new object[] { new MockSite() };
            yield return new object[] { null };
        }

        [Theory]
        [MemberData(nameof(Site_Set_TestData))]
        public void Site_Set_GetReturnsExpected(ISite value)
        {
            var component = new Component
            {
                Site = value
            };
            Assert.Same(value, component.Site);

            // Set same.
            component.Site = value;
            Assert.Same(value, component.Site);
        }

        [Fact]
        public void Dispose_NullSite_Success()
        {
            var component = new SubComponent();
            component.Dispose();
            Assert.Null(component.Site);

            component.Dispose();
            Assert.Null(component.Site);
        }

        [Fact]
        public void Dispose_NullSiteContainer_Success()
        {
            var site = new MockSite
            {
                Container = null
            };
            var component = new SubComponent
            {
                Site = site
            };
            component.Dispose();
            Assert.Same(site, component.Site);

            component.Dispose();
            Assert.Same(site, component.Site);
        }

        [Fact]
        public void Dispose_NonNullSiteContainer_RemovesComponentFromContainer()
        {
            var site = new MockSite
            {
                Container = new Container()
            };
            var component = new SubComponent
            {
                Site = site
            };

            component.Dispose();
            Assert.Null(component.Site);

            component.Dispose();
            Assert.Null(component.Site);
        }

        [Fact]
        public void Dispose_InvokeWithDisposed_CallsHandler()
        {
            var component = new SubComponent();
            int callCount = 0;
            EventHandler handler = (sender, e) =>
            {
                Assert.Same(component, sender);
                Assert.Same(EventArgs.Empty, e);
                callCount++;
            };
            component.Disposed += handler;

            component.Dispose();
            Assert.Equal(1, callCount);

            component.Dispose();
            Assert.Equal(2, callCount);

            // Remove handler.
            component.Disposed -= handler;
            component.Dispose();
            Assert.Equal(2, callCount);
        }

        [Fact]
        public void Dispose_InvokeCannotRaiseEvents_DoesNotCallHandler()
        {
            var component = new NoEventsComponent();
            int callCount = 0;
            component.Disposed += (sender, e) => callCount++;

            component.Dispose();
            Assert.Equal(0, callCount);
        }

        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public void Dispose_InvokeBoolWithDisposed_CallsHandlerIfDisposing(bool disposing, int expectedCallCount)
        {
            var component = new SubComponent();
            int callCount = 0;
            EventHandler handler = (sender, e) =>
            {
                Assert.Same(component, sender);
                Assert.Same(EventArgs.Empty, e);
                callCount++;
            };
            component.Disposed += handler;

            component.Dispose(disposing);
            Assert.Equal(expectedCallCount, callCount);

            component.Dispose(disposing);
            Assert.Equal(expectedCallCount * 2, callCount);

            // Remove handler.
            component.Disposed -= handler;
            component.Dispose(disposing);
            Assert.Equal(expectedCallCount * 2, callCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Dispose_InvokeDisposingCannotRaiseEvents_DoesNotCallHandler(bool disposing)
        {
            var component = new NoEventsComponent();
            int callCount = 0;
            component.Disposed += (sender, e) => callCount++;

            component.Dispose(disposing);
            Assert.Equal(0, callCount);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsInvokingFinalizersSupported))]
        public void Finalize_Invoke_DoesNotCallDisposedEvent()
        {
            var component = new SubComponent();
            int callCount = 0;
            component.Disposed += (sender, e) => callCount++;

            MethodInfo method = typeof(Component).GetMethod("Finalize", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method.Invoke(component, null);
            Assert.Equal(0, callCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void GetService_InvokeWithoutSite_ReturnsNull(Type serviceType)
        {
            var component = new SubComponent();
            Assert.Null(component.GetService(serviceType));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, typeof(bool))]
        [InlineData(typeof(int), null)]
        [InlineData(typeof(int), typeof(bool))]
        public void GetService_InvokeWithSite_ReturnsExpected(Type serviceType, Type result)
        {
            var site = new MockSite
            {
                ServiceType = result
            };
            var component = new SubComponent
            {
                Site = site
            };
            Assert.Same(result, component.GetService(serviceType));
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { new Component(), "System.ComponentModel.Component" };
            yield return new object[] { new Component { Site = new MockSite { Name = "name" } }, "name [System.ComponentModel.Component]" };
            yield return new object[] { new Component { Site = new MockSite { Name = string.Empty } }, " [System.ComponentModel.Component]" };
            yield return new object[] { new Component { Site = new MockSite { Name = null } }, " [System.ComponentModel.Component]" };

            // ToString uses the private _site field instead of the Site property.
            yield return new object[] { new DifferentSiteComponent { Site = new MockSite { Name = "Name2" } }, "System.ComponentModel.Tests.ComponentTests+DifferentSiteComponent" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_HasSite_ReturnsExpected(Component component, string expected)
        {
            Assert.Equal(expected, component.ToString());
        }

        private class SubComponent : Component
        {
            public new bool CanRaiseEvents => base.CanRaiseEvents;

            public new bool DesignMode => base.DesignMode;
            public new EventHandlerList Events => base.Events;

            public new void Dispose(bool disposing) => base.Dispose(disposing);

            public new object GetService(Type serviceType) => base.GetService(serviceType);
        }

        private class NoEventsComponent : Component
        {
            protected override bool CanRaiseEvents => false;

            public new void Dispose(bool disposing) => base.Dispose(disposing);
        }

        private class DifferentSiteComponent : Component
        {
            public new bool DesignMode => base.DesignMode;

            public override ISite Site
            {
                get => new MockSite { Container = new Container(), DesignMode = true, Name = "Name1" };
                set { }
            }
        }
    }
}
