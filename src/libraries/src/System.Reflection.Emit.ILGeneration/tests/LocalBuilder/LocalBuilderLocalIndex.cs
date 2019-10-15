// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class LocalBuilderLocalIndex
    {
        [Fact]
        public void LocalIndex_FirstLocal_ReturnsZero()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            LocalBuilder localBuilder = ilGenerator.DeclareLocal(typeof(string));
            Assert.Equal(0, localBuilder.LocalIndex);
        }

        [Fact]
        public void LocalIndex_MultipleLocals_ReturnsIndex()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            for (int i = 0; i < 1000; i++)
            {
                LocalBuilder localBuilder = ilGenerator.DeclareLocal(typeof(char));
                Assert.Equal(i, localBuilder.LocalIndex);
            }
        }
    }
}
