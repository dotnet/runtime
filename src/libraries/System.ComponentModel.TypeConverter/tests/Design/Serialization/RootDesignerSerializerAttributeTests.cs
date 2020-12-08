// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Design.Serialization.Tests
{
#pragma warning disable 0618
    public class RootDesignerSerializerAttributeTests
    {
        [Theory]
        [InlineData(typeof(int), typeof(int), true)]
        [InlineData(typeof(DefaultSerializationProviderAttributeTests), typeof(DesignerSerializerAttribute), false)]
        public void Ctor_SerializerType_BaseSerializerType(Type serializerType, Type baseSerializerType, bool reloadable)
        {
            var attribute = new RootDesignerSerializerAttribute(serializerType, baseSerializerType, reloadable);
            Assert.Equal(serializerType.AssemblyQualifiedName, attribute.SerializerTypeName);
            Assert.Equal(baseSerializerType.AssemblyQualifiedName, attribute.SerializerBaseTypeName);
            Assert.Equal(reloadable, attribute.Reloadable);
        }

        [Fact]
        public void Ctor_NullSerializerType_ThrowsNullReferenceException()
        {
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("serializerType", () => new RootDesignerSerializerAttribute((Type)null, typeof(int), false));
        }

        [Fact]
        public void Ctor_NullBaseSerializerType_ThrowsNullReferenceException()
        {
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("baseSerializerType", () => new RootDesignerSerializerAttribute(typeof(int), (Type)null, false));
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("baseSerializerType", () => new RootDesignerSerializerAttribute("int", (Type)null, false));
        }

        [Theory]
        [InlineData(null, typeof(int), true)]
        [InlineData("SerializerTypeName", typeof(DesignerSerializerAttribute), false)]
        public void Ctor_SerializerTypeName_BaseSerializerType(string serializerTypeName, Type baseSerializerType, bool reloadable)
        {
            var attribute = new RootDesignerSerializerAttribute(serializerTypeName, baseSerializerType, reloadable);
            Assert.Equal(serializerTypeName, attribute.SerializerTypeName);
            Assert.Equal(baseSerializerType.AssemblyQualifiedName, attribute.SerializerBaseTypeName);
            Assert.Equal(reloadable, attribute.Reloadable);
        }

        [Theory]
        [InlineData(null, null, true)]
        [InlineData("SerializerTypeName", "BaseSerializerTypeName", false)]
        public void Ctor_SerializerTypeName_BaseSerializerTypeName(string serializerTypeName, string baseSerializerTypeName, bool reloadable)
        {
            var attribute = new RootDesignerSerializerAttribute(serializerTypeName, baseSerializerTypeName, reloadable);
            Assert.Equal(serializerTypeName, attribute.SerializerTypeName);
            Assert.Equal(baseSerializerTypeName, attribute.SerializerBaseTypeName);
            Assert.Equal(reloadable, attribute.Reloadable);
        }

        public static IEnumerable<object[]> TypeId_TestData()
        {
            yield return new object[] { "BaseSerializerTypeName", "System.ComponentModel.Design.Serialization.RootDesignerSerializerAttributeBaseSerializerTypeName" };
            yield return new object[] { "BaseSerializerTypeName,Other", "System.ComponentModel.Design.Serialization.RootDesignerSerializerAttributeBaseSerializerTypeName" };
            yield return new object[] { string.Empty, "System.ComponentModel.Design.Serialization.RootDesignerSerializerAttribute" };
            yield return new object[] { null, "System.ComponentModel.Design.Serialization.RootDesignerSerializerAttribute" };
        }

        [Theory]
        [MemberData(nameof(TypeId_TestData))]
        public void TypeId_Get_ReturnsExcepted(string serializerBaseTypeName, object expected)
        {
            var attribute = new RootDesignerSerializerAttribute("SerializerType", serializerBaseTypeName, reloadable: true);
            Assert.Equal(expected, attribute.TypeId);
            Assert.Same(attribute.TypeId, attribute.TypeId);
        }
    }
#pragma warning restore 0618
}
