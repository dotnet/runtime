// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.Tests
{
    public class DebuggerDisplayAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Value")]
        public void Ctor_Value(string value)
        {
            var attribute = new DebuggerDisplayAttribute(value);
            Assert.Equal(string.Empty, attribute.Name);
            Assert.Equal(value ?? string.Empty, attribute.Value);
            Assert.Equal(string.Empty, attribute.Type);
            Assert.Null(attribute.Target);
            Assert.Null(attribute.TargetTypeName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Name")]
        public void Name_Set_GetReturnsExpected(string name)
        {
            var attribute = new DebuggerDisplayAttribute("Value") { Name = name };
            Assert.Equal(name, attribute.Name);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Type")]
        public void Type_Set_GetReturnsExpected(string type)
        {
            var attribute = new DebuggerDisplayAttribute("Value") { Type = type };
            Assert.Equal(type, attribute.Type);
        }

        [Theory]
        [InlineData(typeof(int))]
        public void Target_Set_GetReturnsExpected(Type target)
        {
            var attribute = new DebuggerDisplayAttribute("Value") { Target = target };
            Assert.Equal(target, attribute.Target);
            Assert.Equal(target.AssemblyQualifiedName, attribute.TargetTypeName);
        }

        [Fact]
        public void Target_SetNull_ThrowsArgumentNullException()
        {
            var attribute = new DebuggerDisplayAttribute("Value");
            AssertExtensions.Throws<ArgumentNullException>("value", () => attribute.Target = null);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("TargetTypeName")]
        public void TargetTypeName_Set_GetReturnsExpected(string targetTypeName)
        {
            var attribute = new DebuggerDisplayAttribute("Value") { TargetTypeName = targetTypeName };
            Assert.Equal(targetTypeName, attribute.TargetTypeName);
        }
    }
}
