// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class NestedContainerTests
    {
        [Fact]
        public void Ctor_IComponent()
        {
            var owner = new Component();
            var container = new NestedContainer(owner);
            Assert.Same(owner, container.Owner);
            Assert.Empty(container.Components);
        }

        [Fact]
        public void Ctor_NullOwner_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("owner", () => new NestedContainer(null));
        }

        [Fact]
        public void OwnerName_NoSite_ReturnsNull()
        {
            var owner = new Component();
            var container = new SubNestedContainer(owner);
            Assert.Null(container.OwnerNameEntryPoint);
        }

        [Fact]
        public void OwnerName_HasISite_ReturnsNull()
        {
            var owner = new Component() { Site = new Site() };
            var container = new SubNestedContainer(owner);
            Assert.Equal("SiteName", container.OwnerNameEntryPoint);
        }

        [Fact]
        public void OwnerName_HasINestedSite_ReturnsNull()
        {
            var owner = new Component() { Site = new NestedSite() };
            var container = new SubNestedContainer(owner);
            Assert.Equal("NestedSiteName", container.OwnerNameEntryPoint);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        public void CreateSite_NullOwnerSite_Success(string name)
        {
            var component = new Component();

            var owner = new Component();
            var container = new SubNestedContainer(owner);
            INestedSite site = Assert.IsAssignableFrom<INestedSite>(container.CreateSiteEntryPoint(component, name));
            Assert.Same(component, site.Component);
            Assert.Same(container, site.Container);
            Assert.False(site.DesignMode);
            Assert.Equal(name, site.Name);
            Assert.Equal(name, site.FullName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        public void Add_NonNullOwnerSite_Success(string name)
        {
            var component = new Component();

            var owner = new Component() { Site = new Site() { DesignMode = true } };
            var container = new SubNestedContainer(owner);
            container.Add(component, name);

            INestedSite site = Assert.IsAssignableFrom<INestedSite>(component.Site);
            Assert.Same(component, site.Component);
            Assert.Same(container, site.Container);
            Assert.True(site.DesignMode);
            Assert.Equal(name, site.Name);
            Assert.Equal(name == null ? null : "SiteName." + name, site.FullName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        [InlineData("newName")]
        public void CreateSite_SetSiteName_Success(string value)
        {
            var component = new Component();

            var owner = new Component();
            var container = new SubNestedContainer(owner);
            container.Add(component, "name");

            INestedSite site = Assert.IsAssignableFrom<INestedSite>(component.Site);
            site.Name = value;
            Assert.Equal(value, site.Name);
        }

        [Fact]
        public void CreateSite_GetSiteServiceType_ReturnsExpected()
        {
            var component = new Component();

            var owner = new Component();
            var container = new SubNestedContainer(owner);
            container.Add(component);

            INestedSite site = Assert.IsAssignableFrom<INestedSite>(component.Site);
            Assert.Same(site, site.GetService(typeof(ISite)));
            Assert.Same(container, site.GetService(typeof(INestedContainer)));
            Assert.Same(container, site.GetService(typeof(IContainer)));
            Assert.Null(site.GetService(typeof(INestedSite)));
            Assert.Null(site.GetService(typeof(int)));
            Assert.Null(site.GetService(null));
        }

        [Fact]
        public void CreateSite_NullComponent_ThrowsArgumentNullException()
        {
            var owner = new Component();
            var container = new SubNestedContainer(owner);
            AssertExtensions.Throws<ArgumentNullException>("component", () => container.CreateSiteEntryPoint(null, "name"));
        }

        [Fact]
        public void GetService_Invoke_ReturnsExpected()
        {
            var owner = new Component();
            var container = new SubNestedContainer(owner);

            Assert.Same(container, container.GetServiceEntryPoint(typeof(INestedContainer)));
            Assert.Same(container, container.GetServiceEntryPoint(typeof(IContainer)));
            Assert.Null(container.GetServiceEntryPoint(typeof(int)));
            Assert.Null(container.GetServiceEntryPoint(null));
        }

        [Fact]
        public void Dispose_Invoke_Success()
        {
            var owner = new Component();
            var container = new NestedContainer(owner);
            container.Dispose();
            container.Dispose();
        }

        [Fact]
        public void DisposeOwner_Invoke_Success()
        {
            var owner = new Component();
            var container = new NestedContainer(owner);
            owner.Dispose();
            owner.Dispose();
        }

        public class SubNestedContainer : NestedContainer
        {
            public SubNestedContainer(IComponent owner) : base(owner) { }

            public string OwnerNameEntryPoint => OwnerName;

            public ISite CreateSiteEntryPoint(IComponent component, string name) => CreateSite(component, name);

            public object GetServiceEntryPoint(Type service) => GetService(service);
        }

        public class Site : ISite
        {
            public IComponent Component { get; set; }

            public IContainer Container { get; set; }

            public bool DesignMode { get; set; }

            public string Name { get; set; } = "SiteName";

            public object GetService(Type serviceType) => 10;
        }

        public class NestedSite : Site, INestedSite
        {
            public string FullName => "NestedSiteName";
        }
    }
}
