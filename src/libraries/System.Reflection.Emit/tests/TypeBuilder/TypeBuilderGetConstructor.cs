// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderGetConstructor
    {
        [Fact]
        public void GetConstructor_DeclaringTypeOfConstructorNotGenericTypeDefinition_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.DefineGenericParameters("T");

            ConstructorBuilder ctor = type.DefineDefaultConstructor(MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            AssertExtensions.Throws<ArgumentException>("type", () => TypeBuilder.GetConstructor(type.AsType(), ctor));
        }

        [Fact]
        public void GetConstructor()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.DefineGenericParameters("T");

            ConstructorBuilder ctor = type.DefineDefaultConstructor(MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            Type genericIntType = type.MakeGenericType(typeof(int));
            ConstructorInfo constructor = TypeBuilder.GetConstructor(genericIntType, ctor);
            Assert.False(constructor.IsGenericMethodDefinition);
        }

        [Fact]
        public void GetConstructor_TypeNotTypeBuilder_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => TypeBuilder.GetConstructor(typeof(int), typeof(int).GetConstructor(new Type[0])));
        }

        [Fact]
        public void GetConstructor_DeclaringTypeOfConstructorNotGenericTypeDefinitionOfType_ThrowsArgumentException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type1 = module.DefineType("TestType1", TypeAttributes.Class | TypeAttributes.Public);
            type1.DefineGenericParameters("T");

            TypeBuilder type2 = module.DefineType("TestType2", TypeAttributes.Class | TypeAttributes.Public);
            type2.DefineGenericParameters("T");

            ConstructorBuilder ctor1 = type1.DefineDefaultConstructor(MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            ConstructorBuilder ctor2 = type2.DefineDefaultConstructor(MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            Type genericInt = type1.MakeGenericType(typeof(int));
            AssertExtensions.Throws<ArgumentException>("type", () => TypeBuilder.GetConstructor(genericInt, ctor2));
        }

        [Fact]
        public void GetConstructor_TypeNotGeneric_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);

            ConstructorBuilder ctor = type.DefineDefaultConstructor(MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            AssertExtensions.Throws<ArgumentException>("constructor", () => TypeBuilder.GetConstructor(type.AsType(), ctor));
        }
    }
}
