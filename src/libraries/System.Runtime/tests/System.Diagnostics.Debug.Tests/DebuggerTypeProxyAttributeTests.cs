// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.Tests
{
    public class DebuggerTypeProxyAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("TypeName")]
        public void Ctor_TypeName(string typeName)
        {
            var attribute = new DebuggerTypeProxyAttribute(typeName);
            Assert.Equal(typeName, attribute.ProxyTypeName);
            Assert.Null(attribute.Target);
            Assert.Null(attribute.TargetTypeName);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(DebuggerTypeProxyAttributeTests))]
        public void Ctor_Type(Type type)
        {
            var attribute = new DebuggerTypeProxyAttribute(type);
            Assert.Equal(type.AssemblyQualifiedName, attribute.ProxyTypeName);
            Assert.Null(attribute.Target);
            Assert.Null(attribute.TargetTypeName);
        }

        [Fact]
        public void Ctor_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => new DebuggerTypeProxyAttribute((Type)null));
        }

        [Theory]
        [InlineData(typeof(int))]
        public void Target_Set_GetReturnsExpected(Type target)
        {
            var attribute = new DebuggerTypeProxyAttribute("TypeName") { Target = target };
            Assert.Equal(target, attribute.Target);
            Assert.Equal(target.AssemblyQualifiedName, attribute.TargetTypeName);
        }

        [Fact]
        public void Target_SetNull_ThrowsArgumentNullException()
        {
            var attribute = new DebuggerTypeProxyAttribute("TypeName");
            AssertExtensions.Throws<ArgumentNullException>("value", () => attribute.Target = null);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("TargetTypeName")]
        public void TargetTypeName_Set_GetReturnsExpected(string targetTypeName)
        {
            var attribute = new DebuggerTypeProxyAttribute("TypeName") { TargetTypeName = targetTypeName };
            Assert.Equal(targetTypeName, attribute.TargetTypeName);
        }
    }
}
