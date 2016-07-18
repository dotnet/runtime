// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderDeclaringType
    {
        [Fact]
        public void DeclaringType_TypeNotCreated()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);
            Assert.True(method.DeclaringType.Equals(type));
        }

        [Fact]
        public void DeclaringType_TypeCreated()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            Type resultType = type.CreateTypeInfo().AsType();
            Assert.True(method.DeclaringType.Equals(resultType));
        }
    }
}
