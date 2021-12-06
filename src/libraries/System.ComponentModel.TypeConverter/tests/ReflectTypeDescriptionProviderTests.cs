// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ReflectTypeDescriptionProviderTests
    {
        [Fact]
        public void GetAttributes_Skips_ComVisibleAttribute_And_GuidAttribute_And_InterfaceTypeAttribute()
        {
            object provider = CreateReflectTypeDescriptionProviderInstance();
            AttributeCollection attributeCollection = provider.GetType().GetMethod("GetAttributes", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(provider, new[] { typeof(TestClass1) }) as AttributeCollection;
            Assert.NotEmpty(attributeCollection);
            IEnumerable<Attribute> attributes = attributeCollection.Cast<Attribute>();
            Attribute attribute =  Assert.Single(attributes);
            Assert.IsType<DescriptionAttribute>(attribute);
            Assert.DoesNotContain(attributeCollection.Cast<Attribute>(), attr => attr.GetType() == typeof(ComVisibleAttribute));
            Assert.DoesNotContain(attributeCollection.Cast<Attribute>(), attr => attr.GetType() == typeof(GuidAttribute));
            Assert.DoesNotContain(attributeCollection.Cast<Attribute>(), attr => attr.GetType() == typeof(InterfaceTypeAttribute));
        }

        [Fact]
        public void GetExtenderProviders_ReturnResultFromContainerComponents_WhenComponentSiteServiceNull()
        {
            object provider = CreateReflectTypeDescriptionProviderInstance();
            using TestComponent testComponent = new TestComponent();
            testComponent.Site = new TestSiteWithoutService();
            testComponent.Disposed += (object obj, EventArgs args) => { };
            IExtenderProvider[] extenderProviders = provider.GetType().GetMethod("GetExtenderProviders", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(provider, new[] { testComponent }) as IExtenderProvider[];
            IExtenderProvider result = Assert.Single(extenderProviders);
            Assert.IsType<ComponentExtendedProvider>(result);
        }

        [Fact]
        public void GetExtenderProviders_ReturnResultFromComponentSiteService_WhenComponentSiteServiceNotNull()
        {
            object provider = CreateReflectTypeDescriptionProviderInstance();
            using TestComponent testComponent = new TestComponent();
            testComponent.Site = new TestSiteWithService();
            testComponent.Disposed += (object obj, EventArgs args) => { };
            IExtenderProvider[] extenderProviders = provider.GetType().GetMethod("GetExtenderProviders", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(provider, new[] { testComponent }) as IExtenderProvider[];
            IExtenderProvider result = Assert.Single(extenderProviders);
            Assert.IsType<TestExtenderProvider>(result);
        }

        private static object CreateReflectTypeDescriptionProviderInstance()
        {
            Assembly srcAsssemby = Assembly.Load(Assembly.GetExecutingAssembly().GetReferencedAssemblies().FirstOrDefault(a => a.FullName.StartsWith("System.ComponentModel.TypeConverter")));
            Type reflectTypeDescriptionProviderType = srcAsssemby.GetType("System.ComponentModel.ReflectTypeDescriptionProvider");
            return reflectTypeDescriptionProviderType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, new Type[0]).Invoke(new object[0]);
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

        internal class ComponentExtendedProvider : IComponent, IExtenderProvider
        {
            public ISite? Site { get; set; }

            public event EventHandler? Disposed;

            public bool CanExtend(object extendee) => true;

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
