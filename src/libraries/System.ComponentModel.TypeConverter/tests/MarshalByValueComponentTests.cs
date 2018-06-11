// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class MarshalByValueComponentTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var component = new SubMarshalByValueComponent();
            Assert.NotNull(component.EventsEntryPoint);
            Assert.Same(component.EventsEntryPoint, component.EventsEntryPoint);
            Assert.Null(component.Container);
            Assert.False(component.DesignMode);
            Assert.Null(component.Site);
            Assert.Null(component.GetService(null));
        }

        [Fact]
        public void Site_Set_GetReturnsExpected()
        {
            var site = new Site();
            var component = new SubMarshalByValueComponent() { Site = site };
            Assert.Same(site, component.Site);

            Assert.Same(site.Container, component.Container);
            Assert.Equal(site.DesignMode, component.DesignMode);
            Assert.Equal("service", component.GetService(typeof(int)));
        }

        [Fact]
        public void Disposed_AddRemoveEventHandler_Success()
        {
            bool calledDisposedHandler = false;
            void Handler(object sender, EventArgs e) => calledDisposedHandler = true;

            var component = new MarshalByValueComponent();
            component.Disposed += Handler;
            component.Disposed -= Handler;

            component.Dispose();
            Assert.False(calledDisposedHandler);
        }

        [Fact]
        public void Dispose_NoSiteNoEventHandler_Nop()
        {
            var component = new SubMarshalByValueComponent();
            component.Dispose();

            // With events.
            Assert.NotNull(component.EventsEntryPoint);
            component.Dispose();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Dispose_DisposingNoSiteNoEventHandler_Nop(bool disposing)
        {
            var component = new SubMarshalByValueComponent();
            component.DisposeEntryPoint(disposing);

            // With events.
            Assert.NotNull(component.EventsEntryPoint);
            component.DisposeEntryPoint(disposing);
        }

        [Fact]
        public void Dispose_NoSiteEventHandler_InvokesEventHandler()
        {
            bool calledDisposed = false;
            var component = new SubMarshalByValueComponent();
            component.Disposed += (sender, e) =>
            {
                Assert.Same(component, sender);
                Assert.Same(EventArgs.Empty, e);
                calledDisposed = true;
            };

            Assert.False(calledDisposed);
            component.Dispose();
            Assert.True(calledDisposed);

            // Call multiple times.
            calledDisposed = false;
            component.Dispose();
            Assert.True(calledDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Dispose_DisposingNoSiteEventHandler_InvokesEventHandler(bool disposing)
        {
            bool calledDisposed = false;
            var component = new SubMarshalByValueComponent();
            component.Disposed += (sender, e) =>
            {
                Assert.Same(component, sender);
                Assert.Same(EventArgs.Empty, e);
                calledDisposed = true;
            };

            Assert.False(calledDisposed);
            component.DisposeEntryPoint(disposing);
            Assert.Equal(disposing, calledDisposed);

            // Call multiple times.
            calledDisposed = false;
            component.DisposeEntryPoint(disposing);
            Assert.Equal(disposing, calledDisposed);
        }

        [Fact]
        public void Dispose_SiteEventHandler_InvokesEventHandlerAndRemovesSiteFromComponent()
        {
            bool calledDisposed = false;
            var site = new Site();
            var component = new MarshalByValueComponent() { Site = site };
            component.Disposed += (sender, e) =>
            {
                Assert.Same(component, sender);
                Assert.Same(EventArgs.Empty, e);
                calledDisposed = true;
            };

            Assert.False(calledDisposed);
            site.Container.Add(component);
            component.Dispose();
            Assert.True(calledDisposed);
            Assert.Empty(site.Container.Components);

            // Call multiple times.
            component.Site = site;
            site.Container = null;
            calledDisposed = false;
            component.Dispose();
            Assert.True(calledDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Dispose_DisposingSiteEventHandler_InvokesEventHandlerAndRemovesSiteFromComponent(bool disposing)
        {
            bool calledDisposed = false;
            var site = new Site();
            var component = new SubMarshalByValueComponent() { Site = site };
            component.Disposed += (sender, e) =>
            {
                Assert.Same(component, sender);
                Assert.Same(EventArgs.Empty, e);
                calledDisposed = true;
            };

            Assert.False(calledDisposed);
            site.Container.Add(component);
            component.DisposeEntryPoint(disposing);
            Assert.Equal(disposing, calledDisposed);
            Assert.Empty(site.Container.Components);

            // Call multiple times.
            component.Site = site;
            site.Container = null;
            calledDisposed = false;
            component.DisposeEntryPoint(disposing);
            Assert.Equal(disposing, calledDisposed);
        }

        [Fact]
        public void ToString_WithSite_ReturnsExpected()
        {
            var component = new MarshalByValueComponent() { Site = new Site() };
            Assert.Equal("name [System.ComponentModel.MarshalByValueComponent]", component.ToString());
        }

        [Fact]
        public void ToString_NoSite_ReturnsExpected()
        {
            var component = new MarshalByValueComponent();
            Assert.Equal("System.ComponentModel.MarshalByValueComponent", component.ToString());
        }

        public class SubMarshalByValueComponent : MarshalByValueComponent
        {
            public SubMarshalByValueComponent() : base() { }

            public EventHandlerList EventsEntryPoint => Events;

            public void DisposeEntryPoint(bool disposing) => Dispose(disposing);
        }

        public class Site : ISite
        {
            public IComponent Component { get; set; } = new Component();

            public IContainer Container { get; set; } = new Container();

            public bool DesignMode { get; set; } = true;

            public string Name { get; set; } = "name";

            public object GetService(Type serviceType)
            {
                Assert.Equal(typeof(int), serviceType);
                return "service";
            }
        }
    }
}
