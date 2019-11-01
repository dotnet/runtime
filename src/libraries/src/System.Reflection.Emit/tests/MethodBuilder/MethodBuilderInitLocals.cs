// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderInitLocals
    {
        [Fact]
        public void InitLocals_Get_ReturnsTrue()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);
            Assert.True(method.InitLocals);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InitLocals_Set(bool newInitLocals)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);

            method.InitLocals = newInitLocals;
            Assert.Equal(newInitLocals, method.InitLocals);
        }
    }
}
