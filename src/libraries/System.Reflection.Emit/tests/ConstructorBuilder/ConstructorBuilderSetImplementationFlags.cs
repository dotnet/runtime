// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ConstructorBuilderSetImplementationFlags
    {
        [Fact]
        public void MethodImplementationFlags_SetToCustomValue()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(string) });

            constructor.SetImplementationFlags(MethodImplAttributes.Runtime);
            MethodImplAttributes methodImplementationFlags = constructor.MethodImplementationFlags;
            Assert.Equal(MethodImplAttributes.Runtime, constructor.MethodImplementationFlags);
        }

        [Fact]
        public void MethodImplementationFlags_NotSet()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(string) });

            Assert.Equal(MethodImplAttributes.IL, constructor.MethodImplementationFlags);
        }

        [Fact]
        public void SetImplementationFlags_TypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            constructor.GetILGenerator().Emit(OpCodes.Ret);

            Type createdType = type.CreateType();
            Assert.Throws<InvalidOperationException>(() => constructor.SetImplementationFlags(MethodImplAttributes.Runtime));
        }
    }
}
