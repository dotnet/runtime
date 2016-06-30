// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderReturnParameter
    {
        [Fact]
        public void ReturnParameter_NoSetReturnParameter_ReturnsVoid()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);

            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            type.CreateTypeInfo().AsType();
            Assert.Equal(typeof(void), method.ReturnParameter.ParameterType);
        }

        [Theory]
        [InlineData(typeof(void))]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        public void ReturnParameter(Type returnType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod( "TestMethod", MethodAttributes.Public, returnType, new Type[] { typeof(int) });
            ParameterBuilder paramBuilder = method.DefineParameter(1, ParameterAttributes.HasDefault, "TestParam");

            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            type.CreateTypeInfo().AsType();
            Assert.Equal(returnType, method.ReturnParameter.ParameterType);
        }

        [Fact]
        public void ReturnParameter_TypeNotCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);

            Assert.Throws<InvalidOperationException>(() => method.ReturnParameter);
        }

        [Fact]
        public void ReturnParameter_NoBody_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);

            type.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => method.ReturnParameter);
        }
    }
}
