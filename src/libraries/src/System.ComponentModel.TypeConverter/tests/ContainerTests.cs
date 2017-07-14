// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information. 

//
// System.ComponentModel.Container test cases
//
// Authors:
// 	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//  Ivan N. Zlatev (contact i-nZ.net)

// Copyright (c) 2006 Novell, Inc. (http://www.novell.com)
// Copyright (c) 2006 Ivan N. Zlatev
//

using System.ComponentModel.Design;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Tests
{
    internal class TestService
    {
    }

    internal class TestContainer : Container
    {
        ServiceContainer _services = new ServiceContainer();
        bool allowDuplicateNames;

        public TestContainer()
        {
            _services.AddService(typeof(TestService), new TestService());
        }

        public bool AllowDuplicateNames
        {
            get { return allowDuplicateNames; }
            set { allowDuplicateNames = value; }
        }

        protected override object GetService(Type serviceType)
        {
            return _services.GetService(serviceType);
        }

        public new void RemoveWithoutUnsiting(IComponent component)
        {
            base.RemoveWithoutUnsiting(component);
        }

        public void InvokeValidateName(IComponent component, string name)
        {
            ValidateName(component, name);
        }

        protected override void ValidateName(IComponent component, string name)
        {
            if (AllowDuplicateNames)
                return;
            base.ValidateName(component, name);
        }

        public bool Contains(IComponent component)
        {
            bool found = false;

            foreach (IComponent c in Components)
            {
                if (component.Equals(c))
                {
                    found = true;
                    break;
                }
            }
            return found;
        }

        public new void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    internal class TestComponent : Component
    {
        public override ISite Site
        {
            get
            {
                return base.Site;
            }
            set
            {
                base.Site = value;
                if (value != null)
                {
                    Assert.NotNull(value.GetService(typeof(ISite)));
                    Assert.NotNull(value.GetService(typeof(TestService)));
                }
            }
        }

        public bool IsDisposed
        {
            get { return disposed; }
        }

        public bool ThrowOnDispose
        {
            get { return throwOnDispose; }
            set { throwOnDispose = value; }
        }

        protected override void Dispose(bool disposing)
        {
            if (ThrowOnDispose)
                throw new InvalidOperationException();

            base.Dispose(disposing);
            disposed = true;
        }

        private bool disposed;
        private bool throwOnDispose;
    }


    public class ContainerTest
    {
        private TestContainer _container;

        public ContainerTest()
        {
            _container = new TestContainer();
        }

        [Fact] // Add (IComponent)
        public void Add1()
        {
            TestContainer containerA = new TestContainer();
            TestContainer containerB = new TestContainer();

            ISite siteA;
            ISite siteB;

            TestComponent compA = new TestComponent();
            Assert.Null(compA.Site);
            TestComponent compB = new TestComponent();
            Assert.Null(compB.Site);
            Assert.Equal(0, containerA.Components.Count);
            Assert.Equal(0, containerB.Components.Count);

            containerA.Add(compA);
            siteA = compA.Site;
            Assert.NotNull(siteA);
            Assert.Same(compA, siteA.Component);
            Assert.Same(containerA, siteA.Container);
            Assert.False(siteA.DesignMode);
            Assert.Null(siteA.Name);
            containerA.Add(compB);
            siteB = compB.Site;
            Assert.NotNull(siteB);
            Assert.Same(compB, siteB.Component);
            Assert.Same(containerA, siteB.Container);
            Assert.False(siteB.DesignMode);
            Assert.Null(siteB.Name);

            Assert.False(object.ReferenceEquals(siteA, siteB));
            Assert.Equal(2, containerA.Components.Count);
            Assert.Equal(0, containerB.Components.Count);
            Assert.Same(compA, containerA.Components[0]);
            Assert.Same(compB, containerA.Components[1]);

            // check effect of adding component that is already member of
            // another container
            containerB.Add(compA);
            Assert.False(object.ReferenceEquals(siteA, compA.Site));
            siteA = compA.Site;
            Assert.NotNull(siteA);
            Assert.Same(compA, siteA.Component);
            Assert.Same(containerB, siteA.Container);
            Assert.False(siteA.DesignMode);
            Assert.Null(siteA.Name);

            Assert.Equal(1, containerA.Components.Count);
            Assert.Equal(1, containerB.Components.Count);
            Assert.Same(compB, containerA.Components[0]);
            Assert.Same(compA, containerB.Components[0]);

            // check effect of add component twice to same container
            containerB.Add(compA);
            Assert.Same(siteA, compA.Site);

            Assert.Equal(1, containerA.Components.Count);
            Assert.Equal(1, containerB.Components.Count);
            Assert.Same(compB, containerA.Components[0]);
            Assert.Same(compA, containerB.Components[0]);
        }

        [Fact]
        public void Add1_Component_Null()
        {
            _container.Add((IComponent)null);
            Assert.Equal(0, _container.Components.Count);
        }

        [Fact] // Add (IComponent, String)
        public void Add2()
        {
            TestContainer containerA = new TestContainer();
            TestContainer containerB = new TestContainer();

            ISite siteA;
            ISite siteB;

            TestComponent compA = new TestComponent();
            Assert.Null(compA.Site);
            TestComponent compB = new TestComponent();
            Assert.Null(compB.Site);
            Assert.Equal(0, containerA.Components.Count);
            Assert.Equal(0, containerB.Components.Count);

            containerA.Add(compA, "A");
            siteA = compA.Site;
            Assert.NotNull(siteA);
            Assert.Same(compA, siteA.Component);
            Assert.Same(containerA, siteA.Container);
            Assert.False(siteA.DesignMode);
            Assert.Equal("A", siteA.Name);
            containerA.Add(compB, "B");
            siteB = compB.Site;
            Assert.NotNull(siteB);
            Assert.Same(compB, siteB.Component);
            Assert.Same(containerA, siteB.Container);
            Assert.False(siteB.DesignMode);
            Assert.Equal("B", siteB.Name);

            Assert.False(object.ReferenceEquals(siteA, siteB));
            Assert.Equal(2, containerA.Components.Count);
            Assert.Equal(0, containerB.Components.Count);
            Assert.Same(compA, containerA.Components[0]);
            Assert.Same(compB, containerA.Components[1]);

            // check effect of adding component that is already member of
            // another container
            containerB.Add(compA, "A2");
            Assert.False(object.ReferenceEquals(siteA, compA.Site));
            siteA = compA.Site;
            Assert.NotNull(siteA);
            Assert.Same(compA, siteA.Component);
            Assert.Same(containerB, siteA.Container);
            Assert.False(siteA.DesignMode);
            Assert.Equal("A2", siteA.Name);

            Assert.Equal(1, containerA.Components.Count);
            Assert.Equal(1, containerB.Components.Count);
            Assert.Same(compB, containerA.Components[0]);
            Assert.Same(compA, containerB.Components[0]);

            // check effect of add component twice to same container
            containerB.Add(compA, "A2");
            Assert.Same(siteA, compA.Site);
            Assert.Equal("A2", siteA.Name);

            Assert.Equal(1, containerA.Components.Count);
            Assert.Equal(1, containerB.Components.Count);
            Assert.Same(compB, containerA.Components[0]);
            Assert.Same(compA, containerB.Components[0]);

            // add again with different name
            containerB.Add(compA, "A3");
            Assert.Same(siteA, compA.Site);
            Assert.Equal("A2", siteA.Name);

            Assert.Equal(1, containerA.Components.Count);
            Assert.Equal(1, containerB.Components.Count);
            Assert.Same(compB, containerA.Components[0]);
            Assert.Same(compA, containerB.Components[0]);

            // check effect of add component twice to same container
            containerB.Add(compA, "A2");
            Assert.Same(siteA, compA.Site);
            Assert.Equal("A2", siteA.Name);
        }

        [Fact]
        public void Add_ExceedsSizeOfBuffer_Success()
        {
            var container = new Container();
            var components = new Component[] { new Component(), new Component(), new Component(), new Component(), new Component() };
            
            for (int i = 0; i < components.Length; i++)
            {
                container.Add(components[i]);
                Assert.Same(components[i], container.Components[i]);
            }
        }

        [Fact]
        public void Add2_Component_Null()
        {
            _container.Add((IComponent)null, "A");
            Assert.Equal(0, _container.Components.Count);
            _container.Add(new TestComponent(), "A");
            Assert.Equal(1, _container.Components.Count);
            _container.Add((IComponent)null, "A");
            Assert.Equal(1, _container.Components.Count);
        }

        [Fact]
        public void Add2_Name_Duplicate()
        {
            TestContainer container = new TestContainer();
            TestComponent c1 = new TestComponent();
            container.Add(c1, "dup");

            // new component, same case
            TestComponent c2 = new TestComponent();
            ArgumentException ex;

            ex = AssertExtensions.Throws<ArgumentException>(null, () => container.Add(c2, "dup"));
            Assert.Equal(typeof(ArgumentException), ex.GetType());
            Assert.Null(ex.InnerException);
            if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
            {
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("'dup'") != -1);
                Assert.Null(ex.ParamName);
            }
            Assert.Equal(1, container.Components.Count);

            // new component, different case
            TestComponent c3 = new TestComponent();
            ex = AssertExtensions.Throws<ArgumentException>(null, () => container.Add(c3, "duP"));
            // Duplicate component name 'duP'.  Component names must be
            // unique and case-insensitive
            Assert.Equal(typeof(ArgumentException), ex.GetType());
            Assert.Null(ex.InnerException);
            if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
            {
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("'duP'") != -1);
                Assert.Null(ex.ParamName);
            }
            Assert.Equal(1, container.Components.Count);

            // existing component, same case
            TestComponent c4 = new TestComponent();
            container.Add(c4, "C4");
            Assert.Equal(2, container.Components.Count);
            container.Add(c4, "dup");
            Assert.Equal(2, container.Components.Count);
            Assert.Equal("C4", c4.Site.Name);

            // component of other container, same case
            TestContainer container2 = new TestContainer();
            TestComponent c5 = new TestComponent();
            container2.Add(c5, "C5");
            ex = AssertExtensions.Throws<ArgumentException>(null, () => container.Add(c5, "dup"));
            // Duplicate component name 'dup'.  Component names must be
            // unique and case-insensitive
            Assert.Equal(typeof(ArgumentException), ex.GetType());
            Assert.Null(ex.InnerException);
            if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
            {
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("'dup'") != -1);
                Assert.Null(ex.ParamName);
            }
            Assert.Equal(2, container.Components.Count);
            Assert.Equal(1, container2.Components.Count);
            Assert.Same(c5, container2.Components[0]);

            container.AllowDuplicateNames = true;
            TestComponent c6 = new TestComponent();
            container.Add(c6, "dup");
            Assert.Equal(3, container.Components.Count);
            Assert.NotNull(c1.Site);
            Assert.Equal("dup", c1.Site.Name);
            Assert.NotNull(c6.Site);
            Assert.Equal("dup", c6.Site.Name);
            Assert.False(object.ReferenceEquals(c1.Site, c6.Site));
        }

        [Fact]
        public void Add_SetSiteName_ReturnsExpected()
        {
            var component = new Component();
            var container = new Container();

            container.Add(component, "Name1");
            Assert.Equal("Name1", component.Site.Name);

            component.Site.Name = "OtherName";
            Assert.Equal("OtherName", component.Site.Name);

            // Setting to the same value is a nop.
            component.Site.Name = "OtherName";
            Assert.Equal("OtherName", component.Site.Name);
        }

        [Fact]
        public void Add_SetSiteNameDuplicate_ThrowsArgumentException()
        {
            var component1 = new Component();
            var component2 = new Component();
            var container = new Container();

            container.Add(component1, "Name1");
            container.Add(component2, "Name2");

            Assert.Throws<ArgumentException>(null, () => component1.Site.Name = "Name2");
        }

        [Fact]
        public void Add_DuplicateNameWithInheritedReadOnly_AddsSuccessfully()
        {
            var component1 = new Component();
            var component2 = new Component();
            TypeDescriptor.AddAttributes(component1, new InheritanceAttribute(InheritanceLevel.InheritedReadOnly));

            var container = new Container();
            container.Add(component1, "Name");
            container.Add(component2, "Name");

            Assert.Equal(new IComponent[] { component1, component2 }, container.Components.Cast<IComponent>());
        }

        [Fact]
        public void AddRemove()
        {
            TestComponent component = new TestComponent();

            _container.Add(component);
            Assert.NotNull(component.Site);
            Assert.True(_container.Contains(component));

            _container.Remove(component);
            Assert.Null(component.Site);
            Assert.False(_container.Contains(component));
        }

        [Fact] // Dispose ()
        public void Dispose1()
        {
            TestComponent compA;
            TestComponent compB;

            compA = new TestComponent();
            _container.Add(compA);
            compB = new TestComponent();
            _container.Add(compB);

            _container.Dispose();

            Assert.Equal(0, _container.Components.Count);
            Assert.True(compA.IsDisposed);
            Assert.Null(compA.Site);
            Assert.True(compB.IsDisposed);
            Assert.Null(compB.Site);

            _container = new TestContainer();
            compA = new TestComponent();
            compA.ThrowOnDispose = true;
            _container.Add(compA);
            compB = new TestComponent();
            _container.Add(compB);

            Assert.Throws<InvalidOperationException>(() => _container.Dispose());
            // assert that component is not removed from components until after
            // Dispose of component has succeeded
            Assert.Equal(0, _container.Components.Count);
            Assert.False(compA.IsDisposed);
            Assert.Null(compA.Site);
            Assert.True(compB.IsDisposed);
            Assert.Null(compB.Site);

            compA.ThrowOnDispose = false;

            _container = new TestContainer();
            compA = new TestComponent();
            _container.Add(compA);
            compB = new TestComponent();
            compB.ThrowOnDispose = true;
            _container.Add(compB);

            Assert.Throws<InvalidOperationException>(() => _container.Dispose());
            Assert.Equal(1, _container.Components.Count);
            Assert.Same(compA, _container.Components[0]);
            Assert.False(compA.IsDisposed);
            Assert.NotNull(compA.Site);
            Assert.False(compB.IsDisposed);
            Assert.Null(compB.Site);
            compB.ThrowOnDispose = false;
        }

        [Fact] // Dispose (Boolean)
        public void Dispose2()
        {
            TestComponent compA;
            TestComponent compB;

            compA = new TestComponent();
            _container.Add(compA);
            compB = new TestComponent();
            _container.Add(compB);

            _container.Dispose(false);

            Assert.Equal(2, _container.Components.Count);
            Assert.False(compA.IsDisposed);
            Assert.NotNull(compA.Site);
            Assert.False(compB.IsDisposed);
            Assert.NotNull(compB.Site);

            _container.Dispose(true);

            Assert.Equal(0, _container.Components.Count);
            Assert.True(compA.IsDisposed);
            Assert.Null(compA.Site);
            Assert.True(compB.IsDisposed);
            Assert.Null(compB.Site);

            compA = new TestComponent();
            _container.Add(compA);
            compB = new TestComponent();
            _container.Add(compB);

            Assert.Equal(2, _container.Components.Count);
            Assert.False(compA.IsDisposed);
            Assert.NotNull(compA.Site);
            Assert.False(compB.IsDisposed);
            Assert.NotNull(compB.Site);

            _container.Dispose(true);

            Assert.Equal(0, _container.Components.Count);
            Assert.True(compA.IsDisposed);
            Assert.Null(compA.Site);
            Assert.True(compB.IsDisposed);
            Assert.Null(compB.Site);
        }

        [Fact]
        public void Dispose_Recursive()
        {
            MyComponent comp = new MyComponent();
            Container container = comp.CreateContainer();
            comp.Dispose();
            Assert.Equal(0, container.Components.Count);
        }

        [Fact]
        public void GetService()
        {
            object service;

            GetServiceContainer container = new GetServiceContainer();
            container.Add(new MyComponent());
            service = container.GetService(typeof(MyComponent));
            Assert.Null(service);
            service = container.GetService(typeof(Component));
            Assert.Null(service);
            service = container.GetService(typeof(IContainer));
            Assert.Same(container, service);
            service = container.GetService((Type)null);
            Assert.Null(service);
        }

        [Fact]
        public void Remove()
        {
            TestComponent compA;
            TestComponent compB;
            ISite siteA;
            ISite siteB;

            compA = new TestComponent();
            _container.Add(compA);
            siteA = compA.Site;
            compB = new TestComponent();
            _container.Add(compB);
            siteB = compB.Site;
            _container.Remove(compB);
            Assert.Same(siteA, compA.Site);
            Assert.Null(compB.Site);
            Assert.Equal(1, _container.Components.Count);
            Assert.Same(compA, _container.Components[0]);

            // remove component with no site
            compB = new TestComponent();
            _container.Remove(compB);
            Assert.Same(siteA, compA.Site);
            Assert.Null(compB.Site);
            Assert.Equal(1, _container.Components.Count);
            Assert.Same(compA, _container.Components[0]);

            // remove component associated with other container
            TestContainer container2 = new TestContainer();
            compB = new TestComponent();
            container2.Add(compB);
            siteB = compB.Site;
            _container.Remove(compB);
            Assert.Same(siteA, compA.Site);
            Assert.Same(siteB, compB.Site);
            Assert.Equal(1, _container.Components.Count);
            Assert.Same(compA, _container.Components[0]);
            Assert.Equal(1, container2.Components.Count);
            Assert.Same(compB, container2.Components[0]);
        }

        [Fact]
        public void Remove_Component_Null()
        {
            _container.Add(new TestComponent());
            _container.Remove((IComponent)null);
            Assert.Equal(1, _container.Components.Count);
        }

        [Fact]
        public void RemoveWithoutUnsiting()
        {
            TestComponent compA;
            TestComponent compB;
            ISite siteA;
            ISite siteB;

            compA = new TestComponent();
            _container.Add(compA);
            siteA = compA.Site;
            compB = new TestComponent();
            _container.Add(compB);
            siteB = compB.Site;
            _container.RemoveWithoutUnsiting(compB);
            Assert.Same(siteA, compA.Site);
            Assert.Same(siteB, compB.Site);
            Assert.Equal(1, _container.Components.Count);
            Assert.Same(compA, _container.Components[0]);

            // remove component with no site
            compB = new TestComponent();
            _container.RemoveWithoutUnsiting(compB);
            Assert.Same(siteA, compA.Site);
            Assert.Null(compB.Site);
            Assert.Equal(1, _container.Components.Count);
            Assert.Same(compA, _container.Components[0]);

            // remove component associated with other container
            TestContainer container2 = new TestContainer();
            compB = new TestComponent();
            container2.Add(compB);
            siteB = compB.Site;
            _container.RemoveWithoutUnsiting(compB);
            Assert.Same(siteA, compA.Site);
            Assert.Same(siteB, compB.Site);
            Assert.Equal(1, _container.Components.Count);
            Assert.Same(compA, _container.Components[0]);
            Assert.Equal(1, container2.Components.Count);
            Assert.Same(compB, container2.Components[0]);
        }

        [Fact]
        public void RemoveWithoutUnsiting_Component_Null()
        {
            ISite site;
            TestComponent component;

            component = new TestComponent();
            _container.Add(component);
            site = component.Site;
            _container.RemoveWithoutUnsiting((IComponent)null);
            Assert.Same(site, component.Site);
            Assert.Equal(1, _container.Components.Count);
            Assert.Same(component, _container.Components[0]);
        }

        [Fact]
        public void Remove_NoSuchComponentWithoutUnsiting_Nop()
        {
            var component1 = new Component();
            var component2 = new Component();
            var container = new SitingContainer();

            container.Add(component1);
            container.Add(component2);

            container.DoRemoveWithoutUnsitting(component1);
            Assert.Equal(1, container.Components.Count);

            container.DoRemoveWithoutUnsitting(component1);
            Assert.Equal(1, container.Components.Count);

            container.DoRemoveWithoutUnsitting(component2);
            Assert.Equal(0, container.Components.Count);
        }

        private class SitingContainer : Container
        {
            public void DoRemoveWithoutUnsitting(IComponent component) => RemoveWithoutUnsiting(component);
        }

        [Fact]
        public void ValidateName_Component_Null()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => _container.InvokeValidateName((IComponent)null, "A"));
            Assert.Equal(typeof(ArgumentNullException), ex.GetType());
            Assert.Null(ex.InnerException);
            if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
            {
                Assert.NotNull(ex.Message);
                Assert.Equal("component", ex.ParamName);
            }
        }

        [Fact]
        public void ValidateName_Name_Null()
        {
            TestComponent compA = new TestComponent();
            _container.Add(compA, (string)null);
            TestComponent compB = new TestComponent();
            _container.InvokeValidateName(compB, (string)null);
        }

        [Fact]
        public void ValidateName_Name_Duplicate()
        {
            TestComponent compA = new TestComponent();
            _container.Add(compA, "dup");

            // same component, same case
            _container.InvokeValidateName(compA, "dup");

            // existing component, same case
            TestComponent compB = new TestComponent();
            _container.Add(compB, "B");

            ArgumentException ex;
            ex = AssertExtensions.Throws<ArgumentException>(null, () => _container.InvokeValidateName(compB, "dup"));
            // Duplicate component name 'duP'.  Component names must be
            // unique and case-insensitive
            Assert.Equal(typeof(ArgumentException), ex.GetType());
            Assert.Null(ex.InnerException);
            if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
            {
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("'dup'") != -1);
                Assert.Null(ex.ParamName);
            }
            Assert.Equal(2, _container.Components.Count);
            _container.InvokeValidateName(compB, "whatever");

            // new component, different case
            TestComponent compC = new TestComponent();
            ex = AssertExtensions.Throws<ArgumentException>(null, () => _container.InvokeValidateName(compC, "dup"));
            // Duplicate component name 'duP'.  Component names must be
            // unique and case-insensitive
            Assert.Equal(typeof(ArgumentException), ex.GetType());
            Assert.Null(ex.InnerException);
            if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
            {
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("'dup'") != -1);
                Assert.Null(ex.ParamName);
            }
            Assert.Equal(2, _container.Components.Count);
            _container.InvokeValidateName(compC, "whatever");

            // component of other container, different case
            TestContainer container2 = new TestContainer();
            TestComponent compD = new TestComponent();
            container2.Add(compD, "B");
            ex = AssertExtensions.Throws<ArgumentException>(null, () => _container.InvokeValidateName(compD, "dup"));
            // Duplicate component name 'duP'.  Component names must be
            // unique and case-insensitive
            Assert.Equal(typeof(ArgumentException), ex.GetType());
            Assert.Null(ex.InnerException);
            if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
            {
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("'dup'") != -1);
                Assert.Null(ex.ParamName);
            }
            Assert.Equal(2, _container.Components.Count);
            _container.InvokeValidateName(compD, "whatever");
            Assert.Equal(1, container2.Components.Count);
            Assert.Same(compD, container2.Components[0]);
        }

        [Fact]
        public void Components_GetWithDefaultFilterService_ReturnsAllComponents()
        {
            var component1 = new SubComponent();
            var component2 = new Component();
            var component3 = new SubComponent();

            var container = new FilterContainer { FilterService = new DefaultFilterService() };
            container.Add(component1);
            container.Add(component2);
            container.Add(component3);

            Assert.Equal(new IComponent[] { component1, component2, component3 }, container.Components.Cast<IComponent>());
        }

        [Fact]
        public void Components_GetWithCustomFilterService_ReturnsFilteredComponents()
        {
            var component1 = new SubComponent();
            var component2 = new Component();
            var component3 = new SubComponent();

            // This filter only includes SubComponents.
            var container = new FilterContainer { FilterService = new CustomContainerFilterService() };
            container.Add(component1);
            container.Add(component2);
            container.Add(component3);

            Assert.Equal(new IComponent[] { component1, component3 }, container.Components.Cast<IComponent>());
        }

        [Fact]
        public void Components_GetWithCustomFilterServiceAfterChangingComponents_ReturnsUpdatedComponents()
        {
            var component1 = new SubComponent();
            var component2 = new Component();
            var component3 = new SubComponent();

            // This filter only includes SubComponents.
            var container = new FilterContainer { FilterService = new CustomContainerFilterService() };
            container.Add(component1);
            container.Add(component2);
            container.Add(component3);

            Assert.Equal(new IComponent[] { component1, component3 }, container.Components.Cast<IComponent>());

            container.Remove(component1);
            Assert.Equal(new IComponent[] { component3 }, container.Components.Cast<IComponent>());
        }

        [Fact]
        public void Components_GetWithNullFilterService_ReturnsUnfiltered()
        {
            var component1 = new SubComponent();
            var component2 = new Component();
            var component3 = new SubComponent();

            // This filter only includes SubComponents.
            var container = new FilterContainer { FilterService = new NullContainerFilterService() };
            container.Add(component1);
            container.Add(component2);
            container.Add(component3);

            Assert.Equal(new IComponent[] { component1, component2, component3 }, container.Components.Cast<IComponent>());
        }

        private class FilterContainer : Container
        {
            public ContainerFilterService FilterService { get; set; }

            protected override object GetService(Type service)
            {
                if (service == typeof(ContainerFilterService))
                {
                    return FilterService;
                }

                return base.GetService(service);
            }
        }

        private class SubComponent : Component { }

        private class DefaultFilterService : ContainerFilterService { }

        private class CustomContainerFilterService : ContainerFilterService
        {
            public override ComponentCollection FilterComponents(ComponentCollection components)
            {
                SubComponent[] newComponents = components.OfType<SubComponent>().ToArray();
                return new ComponentCollection(newComponents);
            }
        }

        private class NullContainerFilterService : ContainerFilterService
        {
            public override ComponentCollection FilterComponents(ComponentCollection components)
            {
                return null;
            }
        }

        private class MyComponent : Component
        {
            private Container container;

            protected override void Dispose(bool disposing)
            {
                if (container != null)
                    container.Dispose();
                base.Dispose(disposing);
            }

            public Container CreateContainer()
            {
                if (container != null)
                    throw new InvalidOperationException();
                container = new Container();
                container.Add(new MyComponent());
                container.Add(this);
                return container;
            }
        }

        private class MyContainer : IContainer
        {
            private ComponentCollection components = new ComponentCollection(
                new Component[0]);

            public ComponentCollection Components
            {
                get { return components; }
            }

            public void Add(IComponent component)
            {
            }

            public void Add(IComponent component, string name)
            {
            }

            public void Remove(IComponent component)
            {
            }

            public void Dispose()
            {
            }
        }

        public class GetServiceContainer : Container
        {
            public new object GetService(Type service)
            {
                return base.GetService(service);
            }
        }
    }
}
