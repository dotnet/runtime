// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ComponentResourceManagerTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var resourceManager = new ComponentResourceManager();
            Assert.Null(resourceManager.BaseName);
            Assert.False(resourceManager.IgnoreCase);
            Assert.NotNull(resourceManager.ResourceSetType);
        }

        [Theory]
        [InlineData(typeof(int))]
        public void Ctor_Type(Type type)
        {
            var resourceManager = new ComponentResourceManager(type);
            Assert.Equal("Int32", resourceManager.BaseName);
            Assert.False(resourceManager.IgnoreCase);
            Assert.NotNull(resourceManager.ResourceSetType);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ApplyResources_ValueExists_ReturnsExpected(bool ignoreCase)
        {
            var resourceManager = new ComponentResourceManager(typeof(global::Resources.TestResx))
            {
                IgnoreCase = ignoreCase
            };

            var value = new TestValue();
            resourceManager.ApplyResources(value, "Object");
            Assert.Equal("ObjectGetSetProperty", value.GetSetProperty);
            Assert.Null(value.GetOnlyProperty);
            Assert.Null(value.GetPrivateProperty());

            if (!PlatformDetection.IsFullFramework) // https://github.com/dotnet/corefx/issues/22444 needs to be ported to netfx
            {
                resourceManager.ApplyResources(value, "Default");
                Assert.Equal("DefaultGetSetProperty", value.GetSetProperty);
                Assert.Null(value.GetOnlyProperty);
                Assert.Null(value.GetPrivateProperty());
            }
        }

        private class TestValue
        {
            public string GetSetProperty { get; set; }
            public string GetOnlyProperty { get; }
            private string PrivateProperty { get; set; }

            public string GetPrivateProperty() => PrivateProperty;
        }

        [Fact]
        public void ApplyResources_AmibguousWithSameDeclaringType_ThrowsAmbiguousMatchException()
        {
            var resourceManager = new ComponentResourceManager(typeof(global::Resources.TestResx))
            {
                IgnoreCase = true
            };

            var value = new MulticasedClass();
            Assert.Throws<AmbiguousMatchException>(() => resourceManager.ApplyResources(value, "Object"));
        }

        private class MulticasedClass
        {
            public string GetSetProperty { get; set; }
            public string getsetproperty { get; set; }
        }

        public static IEnumerable<object[]> AmbiguousWithDifferentDeclaringType_TestData()
        {
            yield return new object[] { new MulticaseSubClass() };
            yield return new object[] { new MulticaseSubSubClass() };
        }

        [Theory]
        [MemberData(nameof(AmbiguousWithDifferentDeclaringType_TestData))]
        public void ApplyResources_AmibguousWithDifferentDeclaringTypeInValueType_UsesMostDeclaredProperty<T>(T value) where T : MulticaseSubClass
        {
            var resourceManager = new ComponentResourceManager(typeof(global::Resources.TestResx))
            {
                IgnoreCase = true
            };
            
            resourceManager.ApplyResources(value, "Object");

            Assert.Null(value.GetSetProperty);
            Assert.Equal("ObjectGetSetProperty", value.getsetproperty);

            if (!PlatformDetection.IsFullFramework) // https://github.com/dotnet/corefx/issues/22444 needs to be ported to netfx
            {
                resourceManager.ApplyResources(value, "Default");

                Assert.Null(value.GetSetProperty);
                Assert.Equal("DefaultGetSetProperty", value.getsetproperty);
            }
        }

        public class MulticaseBaseClass
        {
            public string GetSetProperty { get; set; }
        }

        public class MulticaseSubClass : MulticaseBaseClass
        {
            public string getsetproperty { get; set; }
        }

        public class MulticaseSubSubClass : MulticaseSubClass { }

        [Fact]
        public void ApplyResources_IComponentWithNullSite_Success()
        {
            var resourceManager = new ComponentResourceManager(typeof(global::Resources.TestResx))
            {
                IgnoreCase = true
            };

            var value = new TestComponent();
            resourceManager.ApplyResources(value, "Object");
            Assert.Equal("ObjectGetSetProperty", value.GetSetProperty);

            if (!PlatformDetection.IsFullFramework) // https://github.com/dotnet/corefx/issues/22444 needs to be ported to netfx
            {
                resourceManager.ApplyResources(value, "Default");
                Assert.Equal("DefaultGetSetProperty", value.GetSetProperty);
            }
        }

        [Fact]
        public void ApplyResources_IComponentWithNonDesignModeSite_Success()
        {
            var resourceManager = new ComponentResourceManager(typeof(global::Resources.TestResx))
            {
                IgnoreCase = true
            };

            var value = new TestComponent { Site = new TestSite { DesignMode = false } };
            resourceManager.ApplyResources(value, "Object");
            Assert.Equal("ObjectGetSetProperty", value.GetSetProperty);

            if (!PlatformDetection.IsFullFramework) // https://github.com/dotnet/corefx/issues/22444 needs to be ported to netfx
            {
                resourceManager.ApplyResources(value, "Default");
                Assert.Equal("DefaultGetSetProperty", value.GetSetProperty);
            }
        }

        [Fact]
        public void ApplyResources_IComponentWithDesignModeSite_Success()
        {
            var resourceManager = new ComponentResourceManager(typeof(global::Resources.TestResx))
            {
                IgnoreCase = true
            };

            var value = new TestComponent { Site = new TestSite { DesignMode = true } };
            resourceManager.ApplyResources(value, "Object");
            Assert.Equal("ObjectGetSetProperty", value.GetSetProperty);

            if (!PlatformDetection.IsFullFramework) // https://github.com/dotnet/corefx/issues/22444 needs to be ported to netfx
            {
                resourceManager.ApplyResources(value, "Default");
                Assert.Equal("DefaultGetSetProperty", value.GetSetProperty);
            }
        }

        private class TestSite : ISite
        {
            public bool DesignMode { get; set; }

            public IComponent Component => throw new NotImplementedException();
            public IContainer Container => throw new NotImplementedException();
            public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public object GetService(Type serviceType) => null;
        }

        private class TestComponent : IComponent
        {
            public ISite Site { get; set; }

#pragma warning disable 0067
            public event EventHandler Disposed;
#pragma warning restore 0067

            public void Dispose() { }

            public string GetSetProperty { get; set; }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ApplyResources_NoSuchValue_Nop(bool ignoreCase)
        {
            var resourceManager = new ComponentResourceManager(typeof(global::Resources.TestResx))
            {
                IgnoreCase = ignoreCase
            };

            resourceManager.ApplyResources("Value", "ObjectName");
            resourceManager.ApplyResources("Value", "ObjectName", CultureInfo.CurrentUICulture);
            resourceManager.ApplyResources("Value", "ObjectName", CultureInfo.InvariantCulture);
        }

        [Fact]
        public void ApplyResources_NullValue_ThrowsArgumentNullException()
        {
            var resourceManager = new ComponentResourceManager();
            AssertExtensions.Throws<ArgumentNullException>("value", () => resourceManager.ApplyResources(null, "objectName"));
            AssertExtensions.Throws<ArgumentNullException>("value", () => resourceManager.ApplyResources(null, "objectName", CultureInfo.CurrentCulture));
        }

        [Fact]
        public void ApplyResources_NullObjectName_ThrowsArgumentNullException()
        {
            var resourceManager = new ComponentResourceManager();
            AssertExtensions.Throws<ArgumentNullException>("objectName", () => resourceManager.ApplyResources("value", null));
            AssertExtensions.Throws<ArgumentNullException>("objectName", () => resourceManager.ApplyResources("value", null, CultureInfo.CurrentCulture));
        }
    }
}
