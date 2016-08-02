// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class LabelEquals
    {
        [Fact]
        public void Equals_SameInstance_ReturnsTrue()
        {
            Label label = new Label();
            Assert.True(label.Equals(label));
            Assert.True(label.Equals((object)label));
        }

        [Fact]
        public void Equals_DifferentLabel_ReturnsFalse()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public);
            ILGenerator ilGenerator = method.GetILGenerator();
            Label label1 = ilGenerator.DefineLabel();
            Label label2 = ilGenerator.DefineLabel();

            Assert.False(label1.Equals(label2));
            Assert.False(label1.Equals((object)label2));
        }

        [Theory]
        [InlineData(1)]
        [InlineData("label")]
        [InlineData("  ")]
        [InlineData(null)]
        public void Equals_ObjectNotLabel_ReturnsFalse(object obj)
        {
            Label label = new Label();
            Assert.False(label.Equals(obj));
        }

        [Fact]
        public void Equals_EqualityOperators()
        {
            Label lb1 = new Label();
            Label lb2 = new Label();

            Assert.True(lb1 == lb2);
            Assert.False(lb1 != lb2);
        }
    }
}
