// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class LocalBuilderLocalType
    {
        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        [InlineData(typeof(LocalTypeClass))]
        public void LocalType(Type localType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            LocalBuilder localBuilder = ilGenerator.DeclareLocal(localType);
            Assert.Equal(localType, localBuilder.LocalType);
        }
    }

    public class LocalTypeClass { }
}
