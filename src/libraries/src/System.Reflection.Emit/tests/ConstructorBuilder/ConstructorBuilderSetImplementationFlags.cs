// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            int methodImplementationValue = (int)methodImplementationFlags;

            FieldInfo[] fields = typeof(MethodImplAttributes).GetFields(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name == "Runtime")
                {
                    int fieldValue = (int)fields[i].GetValue(null);
                    Assert.Equal(fieldValue, (fieldValue & methodImplementationValue));
                }
            }
        }

        [Fact]
        public void MethodImplementationFlags_NotSet()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(string) });

            MethodImplAttributes methodImplementationFlags = constructor.MethodImplementationFlags;
            int methodImplementationValue = (int)methodImplementationFlags;

            FieldInfo[] fields = typeof(MethodImplAttributes).GetFields(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name == "Runtime")
                {
                    int fieldValue = (int)fields[i].GetValue(null);
                    Assert.NotEqual(fieldValue, (fieldValue & methodImplementationValue));
                }
            }
        }

        [Fact]
        public void SetImplementationFlags_TypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);

            ILGenerator ilGenerator = constructor.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_1);

            Type createdType = type.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => constructor.SetImplementationFlags(MethodImplAttributes.Runtime));
        }
    }
}
