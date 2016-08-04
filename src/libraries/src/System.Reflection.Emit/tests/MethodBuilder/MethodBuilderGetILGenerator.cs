// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderGetILGenerator
    {
        [Theory]
        [InlineData(20)]
        [InlineData(-10)]
        public void GetILGenerator_Int(int size)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[0]);

            ILGenerator ilGenerator = method.GetILGenerator(size);
            int expectedReturn = 5;
            ilGenerator.Emit(OpCodes.Ldc_I4, expectedReturn);
            ilGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo().AsType();
            MethodInfo createedMethod = createdType.GetMethod("TestMethod");
            Assert.Equal(expectedReturn, createedMethod.Invoke(null, null));
        }

        [Theory]
        [InlineData(TypeAttributes.Public, MethodAttributes.Public | MethodAttributes.PinvokeImpl)]
        [InlineData(TypeAttributes.Abstract, MethodAttributes.PinvokeImpl)]
        [InlineData(TypeAttributes.Abstract, MethodAttributes.Abstract | MethodAttributes.PinvokeImpl)]
        public void GetILGenerator_NoMethodBody_ThrowsInvalidOperationException(TypeAttributes typeAttributes, MethodAttributes methodAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(typeAttributes);
            MethodBuilder method = type.DefineMethod("TestMethod", methodAttributes);

            Assert.Throws<InvalidOperationException>(() => method.GetILGenerator());
            Assert.Throws<InvalidOperationException>(() => method.GetILGenerator(10));
        }

        [Theory]
        [InlineData(MethodAttributes.Abstract)]
        [InlineData(MethodAttributes.Assembly)]
        [InlineData(MethodAttributes.CheckAccessOnOverride)]
        [InlineData(MethodAttributes.FamANDAssem)]
        [InlineData(MethodAttributes.Family)]
        [InlineData(MethodAttributes.FamORAssem)]
        [InlineData(MethodAttributes.Final)]
        [InlineData(MethodAttributes.HasSecurity)]
        [InlineData(MethodAttributes.HideBySig)]
        [InlineData(MethodAttributes.MemberAccessMask)]
        [InlineData(MethodAttributes.NewSlot)]
        [InlineData(MethodAttributes.Private)]
        [InlineData(MethodAttributes.PrivateScope)]
        [InlineData(MethodAttributes.Public)]
        [InlineData(MethodAttributes.RequireSecObject)]
        [InlineData(MethodAttributes.ReuseSlot)]
        [InlineData(MethodAttributes.RTSpecialName)]
        [InlineData(MethodAttributes.SpecialName)]
        [InlineData(MethodAttributes.Static)]
        [InlineData(MethodAttributes.UnmanagedExport)]
        [InlineData(MethodAttributes.Virtual)]
        [InlineData(MethodAttributes.VtableLayoutMask)]
        [InlineData(MethodAttributes.Assembly | MethodAttributes.CheckAccessOnOverride |
                MethodAttributes.FamORAssem | MethodAttributes.Final |
                MethodAttributes.HasSecurity | MethodAttributes.HideBySig | MethodAttributes.MemberAccessMask |
                MethodAttributes.NewSlot | MethodAttributes.Private |
                MethodAttributes.PrivateScope | MethodAttributes.RequireSecObject |
                MethodAttributes.RTSpecialName | MethodAttributes.SpecialName |
                MethodAttributes.Static | MethodAttributes.UnmanagedExport)]
        public void GetILGenerator_DifferentAttributes(MethodAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod(attributes.ToString(), attributes);
            Assert.NotNull(method.GetILGenerator());
        }
    }
}
