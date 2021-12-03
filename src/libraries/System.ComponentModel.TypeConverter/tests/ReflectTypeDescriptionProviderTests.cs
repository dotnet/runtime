// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ReflectTypeDescriptionProviderTests
    {
        [Fact]
        public void GetAttributes_Skips_ComVisibleAttribute()
        {
            ReflectTypeDescriptionProvider provider = new ReflectTypeDescriptionProvider();
            AttributeCollection attributeCollection = provider.GetAttributes(typeof(TestClass1));
            Assert.NotEmpty(attributeCollection);
            Assert.Equal(3, attributeCollection.Count);
            Assert.DoesNotContain(attributeCollection.Attributes, attr => attr.GetType() == typeof(ComVisibleAttribute));
        }

        [Fact]
        public void GetExtenderProviders_ReturnEmpty_WhenSiteServiceNull()
        {
            ReflectTypeDescriptionProvider provider = new ReflectTypeDescriptionProvider();
            using TestComponent testClass2 = new TestComponent();
            testClass2.Site = new TestSite1();
            testClass2.Disposed += (object obj, EventArgs args) => { };
            IExtenderProvider[] extenderProviders = provider.GetExtenderProviders(testClass2);
            Assert.Empty(extenderProviders);
        }

        [Fact]
        public void GetExtenderProviders_ReturnResult_WhenSiteServiceNotNull()
        {
            ReflectTypeDescriptionProvider provider = new ReflectTypeDescriptionProvider();
            using TestComponent testClass2 = new TestComponent();
            testClass2.Site = new TestSite2();
            testClass2.Disposed += (object obj, EventArgs args) => { };
            IExtenderProvider[] extenderProviders = provider.GetExtenderProviders(testClass2);
            IExtenderProvider result = Assert.Single(extenderProviders);
            Assert.IsType<TestExtenderProvider>(result);
        }

        [ComVisible(true), Guid("4a223ebb-fe95-4649-94e5-2e5cc8f5f4e9"), InterfaceType(1)]
        internal interface TestInterface
        {
            void Action();
        }

        [Description]
        internal class TestClass1 : TestInterface
        {
            public void Action() { }
        }

        internal class TestComponent : IComponent
        {
            public ISite? Site { get; set; }

            public event EventHandler? Disposed;

            public void Dispose()
            {
                if (Disposed != null)
                {
                    Disposed(this, new EventArgs());
                }
            }
        }

        internal class TestSite1 : ISite
        {
            private TestComponent _testComponent = new TestComponent();
            private TestContainer _testContainer = new TestContainer();

            public IComponent Component => _testComponent;

            public IContainer? Container => _testContainer;

            public bool DesignMode => true;

            public string? Name { get; set; }

            public object? GetService(Type serviceType) => null;
        }

        internal class TestSite2 : ISite
        {
            private TestComponent _testComponent = new TestComponent();
            private TestContainer _testContainer = new TestContainer();
            private TestExtenderListService _testExtenderListService = new TestExtenderListService();

            public IComponent Component => _testComponent;

            public IContainer? Container => _testContainer;

            public bool DesignMode => true;

            public string? Name { get; set; }

            public object? GetService(Type serviceType) => _testExtenderListService;
        }

        internal class TestContainer : IContainer
        {
            private List<IComponent> _components = new List<IComponent>();

            public ComponentCollection Components => new ComponentCollection(_components.ToArray());

            public void Add(IComponent? component) => _components.Add(component);
            public void Add(IComponent? component, string? name) => _components.Add(component);
            public void Dispose() => _components.Clear();
            public void Remove(IComponent? component) => _components.Remove(component);
        }

        internal class TestExtenderProvider : IExtenderProvider
        {
            public bool CanExtend(object extendee) => true;
        }

        internal class TestExtenderListService : IExtenderListService
        {
            private TestExtenderProvider _testExtenderProvider = new TestExtenderProvider();

            public IExtenderProvider[] GetExtenderProviders() => new[] { _testExtenderProvider };
        }
    }
}
