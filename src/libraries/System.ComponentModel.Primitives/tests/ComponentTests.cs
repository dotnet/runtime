// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ComponentTests
    {
        [Fact]
        public void CanRaiseEvents_Get_ReturnsTrueByDefault()
        {
            var component = new SubComponent();
            Assert.True(component.GetCanRaiseEvents());
        }

        [Fact]
        public void Events_Get_ReturnsSameInstance()
        {
            var component = new SubComponent();
            Assert.Same(component.GetEvents(), component.GetEvents());
        }

        [Fact]
        public void Site_Set_GetReturnsExpected()
        {
            var component = new SubComponent();
            Assert.Null(component.Site);

            var site = new MockSite();
            component.Site = site;
            Assert.Same(site, component.Site);
        }

        [Fact]
        public void Container_Set_GetReturnsExpected()
        {
            var component = new SubComponent();
            Assert.Null(component.Container);

            var site = new MockSite { Container = new Container() };
            component.Site = site;
            Assert.Same(site.Container, component.Container);
        }

        [Fact]
        public void DesignMode_Set_GetReturnsExpected()
        {
            var component = new SubComponent();
            Assert.False(component.GetDesignMode());

            var site = new MockSite { DesignMode = true };
            component.Site = site;
            Assert.True(component.GetDesignMode());
        }

        [Fact]
        public void GetServiceType_CustomSite_ReturnsExpected()
        {
            var component = new SubComponent();
            Assert.Null(component.GetServiceInternal(typeof(int)));

            var site = new MockSite { ServiceType = typeof(bool) };
            component.Site = site;
            Assert.Equal(typeof(bool), component.GetServiceInternal(typeof(int)));
        }

        [Fact]
        public void Dispose_NullSite_Success()
        {
            var component = new SubComponent();
            component.Dispose();
            component.Dispose();
        }

        [Fact]
        public void Dispose_NullSiteContainer_Success()
        {
            var component = new SubComponent()
            {
                Site = new MockSite { Container = null }
            };
            component.Dispose();
            component.Dispose();
        }

        [Fact]
        public void Dispose_NonNullSiteContainer_RemovesComponentFromContainer()
        {
            var component = new SubComponent()
            {
                Site = new MockSite { Container = new Container() }
            };

            component.Dispose();
            Assert.Null(component.Site);

            component.Dispose();
        }

        [Fact]
        public void Dispose_HasDisposedEvent_InvokesEvent()
        {
            var component = new SubComponent();
            component.Disposed += Component_Disposed;

            component.Dispose();
            Assert.True(InvokedDisposed);

            InvokedDisposed = false;
            component.Dispose();
            Assert.True(InvokedDisposed);

            component.Disposed -= Component_Disposed;
            InvokedDisposed = false;
            component.Dispose();
            Assert.False(InvokedDisposed);
        }

        [Fact]
        public void Dispose_HasDisposedWithoutEvents_DoesNotnvokesEvent()
        {
            var component = new SubComponent();
            component.Disposed += Component_Disposed;
            component.CanRaiseEventsInternal = false;

            component.Dispose();
            Assert.False(InvokedDisposed);
        }

        [Fact]
        public void Dispose_NotDisposing_DoesNotInvokeEvent()
        {
            var component = new SubComponent();
            component.Disposed += Component_Disposed;

            component.DisposeInternal(false);
            Assert.False(InvokedDisposed);

            component.DisposeInternal(true);
            Assert.True(InvokedDisposed);
        }

        [Fact]
        public void Finalize_Invoke_DoesNotCallDisposedEvent()
        {
            var component = new SubComponent();
            component.Disposed += Component_Disposed;

            MethodInfo method = typeof(Component).GetMethod("Finalize", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method.Invoke(component, null);
            Assert.False(InvokedDisposed);
        }

        [Fact]
        public void ToString_HasSite_ReturnsExpected()
        {
            var component = new SubComponent();
            Assert.Equal("System.ComponentModel.Tests.ComponentTests+SubComponent", component.ToString());

            component.Site = new MockSite { Name = "name" };
            Assert.Equal("name [System.ComponentModel.Tests.ComponentTests+SubComponent]", component.ToString());
        }

        private bool InvokedDisposed { get; set; }
        private void Component_Disposed(object sender, EventArgs e) => InvokedDisposed = true;

        public class SubComponent : Component
        {
            public bool GetCanRaiseEvents() => CanRaiseEvents;

            public bool? CanRaiseEventsInternal { get; set; }
            protected override bool CanRaiseEvents => CanRaiseEventsInternal ?? base.CanRaiseEvents;

            public bool GetDesignMode() => DesignMode;
            public EventHandlerList GetEvents() => Events;

            public object GetServiceInternal(Type serviceType) => GetService(serviceType);

            public void DisposeInternal(bool disposing) => Dispose(disposing);
        }
    }
}
