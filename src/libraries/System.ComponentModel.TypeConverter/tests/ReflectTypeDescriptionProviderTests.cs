// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ReflectTypeDescriptionProviderTests
    {
        [Fact]
        public void GetAttributes_Skips_ComVisibleAttribute_And_GuidAttribute_And_InterfaceTypeAttribute()
        {
            AttributeCollection attributeCollection = TypeDescriptor.GetAttributes(typeof(TestClass1));
            Assert.NotEmpty(attributeCollection);
            IEnumerable<Attribute> attributes = attributeCollection.Cast<Attribute>();
            Attribute attribute =  Assert.Single(attributes);
            Assert.IsType<DescriptionAttribute>(attribute);
            Assert.DoesNotContain(attributes, attr => attr.GetType() == typeof(ComVisibleAttribute));
            Assert.DoesNotContain(attributes, attr => attr.GetType() == typeof(GuidAttribute));
            Assert.DoesNotContain(attributes, attr => attr.GetType() == typeof(InterfaceTypeAttribute));
        }

        [Fact]
        public void GetExtenderProviders_ReturnResultFromContainerComponents_WhenComponentSiteServiceNull()
        {
            using ComponentExtendedProvider testComponent = new ComponentExtendedProvider();
            testComponent.Site = new TestSiteWithoutService();
            testComponent.Disposed += (object obj, EventArgs args) => { };
            PropertyDescriptorCollection propertyDescriptorCollection = TypeDescriptor.GetProperties(testComponent);
            PropertyDescriptor testPropDescriptor = propertyDescriptorCollection["TestProp"];
            Assert.NotNull(testPropDescriptor);
            ExtenderProvidedPropertyAttribute extenderProvidedPropertyAttribute = testPropDescriptor.Attributes[typeof(ExtenderProvidedPropertyAttribute)] as ExtenderProvidedPropertyAttribute;
            Assert.NotNull(extenderProvidedPropertyAttribute);
            Assert.IsType<ComponentExtendedProvider>(extenderProvidedPropertyAttribute.Provider);
        }

        [Fact]
        public void GetExtenderProviders_ReturnResultFromComponentSiteService_WhenComponentSiteServiceNotNull()
        {
            using TestComponent testComponent = new TestComponent();
            testComponent.Site = new TestSiteWithService();
            testComponent.Disposed += (object obj, EventArgs args) => { };
            PropertyDescriptorCollection propertyDescriptorCollection = TypeDescriptor.GetProperties(testComponent);
            PropertyDescriptor testPropDescriptor = propertyDescriptorCollection["TestProp"];
            Assert.NotNull(testPropDescriptor);
            ExtenderProvidedPropertyAttribute extenderProvidedPropertyAttribute = testPropDescriptor.Attributes[typeof(ExtenderProvidedPropertyAttribute)] as ExtenderProvidedPropertyAttribute;
            Assert.NotNull(extenderProvidedPropertyAttribute);
            Assert.IsType<TestExtenderProvider>(extenderProvidedPropertyAttribute.Provider);
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

        [ProvideProperty("TestProp", "System.Object")]
        internal class ComponentExtendedProvider : IComponent, IExtenderProvider
        {
            private int _testProp;

            public ISite? Site { get; set; }

            public event EventHandler? Disposed;

            public bool CanExtend(object extendee) => true;

            public int GetTestProp(object extendee) => _testProp;

            public void SetTestProp(object extendee, int value)
            {
                _testProp = value;
            }

            public void Dispose()
            {
                if (Disposed != null)
                {
                    Disposed(this, new EventArgs());
                }
            }
        }

        internal class TestSiteWithoutService : ISite
        {
            private TestComponent _testComponent = new TestComponent();
            private TestContainer _testContainer = new TestContainer();

            public IComponent Component => _testComponent;

            public IContainer? Container => _testContainer;

            public bool DesignMode => true;

            public string? Name { get; set; }

            public object? GetService(Type serviceType) => null;
        }

        internal class TestSiteWithService : ISite
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
            private List<IComponent> _components = new List<IComponent> { new ComponentExtendedProvider() };

            public ComponentCollection Components => new ComponentCollection(_components.ToArray());

            public void Add(IComponent? component) => _components.Add(component);
            public void Add(IComponent? component, string? name) => _components.Add(component);
            public void Dispose() => _components.Clear();
            public void Remove(IComponent? component) => _components.Remove(component);
        }

        [ProvideProperty("TestProp", "System.Object")]
        internal class TestExtenderProvider : IExtenderProvider
        {
            private int _testProp;

            public bool CanExtend(object extendee) => true;

            public int GetTestProp(object extendee) => _testProp;

            public void SetTestProp(object extendee, int value)
            {
                _testProp = value;
            }
        }

        internal class TestExtenderListService : IExtenderListService
        {
            private TestExtenderProvider _testExtenderProvider = new TestExtenderProvider();

            public IExtenderProvider[] GetExtenderProviders() => new[] { _testExtenderProvider };
        }
    }
}
