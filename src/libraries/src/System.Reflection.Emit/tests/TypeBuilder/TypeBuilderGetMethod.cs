// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderGetMethod
    {
        [Fact]
        public void GetMethod_GenericTypeMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.DefineGenericParameters("T");

            MethodBuilder genericMethod = type.DefineMethod("GM", MethodAttributes.Public | MethodAttributes.Static);
            GenericTypeParameterBuilder[] methodParams = genericMethod.DefineGenericParameters("U");
            genericMethod.SetSignature(null, null, null, new Type[] { methodParams[0].AsType() }, null, null);

            MethodInfo createdGenericMethod = TypeBuilder.GetMethod(type.AsType(), genericMethod);

            Assert.True(createdGenericMethod.IsGenericMethodDefinition);
            Assert.Equal("U", createdGenericMethod.GetGenericArguments()[0].ToString());
        }

        [Fact]
        public void GetMethod_ConstructedTypeMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.DefineGenericParameters("T");

            MethodBuilder genericMethod = type.DefineMethod("GM", MethodAttributes.Public | MethodAttributes.Static);
            GenericTypeParameterBuilder[] methodParams = genericMethod.DefineGenericParameters("U");
            genericMethod.SetSignature(null, null, null, new Type[] { methodParams[0].AsType() }, null, null);

            Type genericIntType = type.MakeGenericType(typeof(int));
            MethodInfo createdGenericMethod = TypeBuilder.GetMethod(genericIntType, genericMethod);

            Assert.True(createdGenericMethod.IsGenericMethodDefinition);
            Assert.Equal("U", createdGenericMethod.GetGenericArguments()[0].ToString());
        }

        [Fact]
        public void GetMethod_TypeNotTypeBuilder_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => TypeBuilder.GetMethod(typeof(int), typeof(int).GetMethod("Parse", new Type[] { typeof(string) })));
        }

        [Fact]
        public void GetMethod_MethodDefinitionNotInTypeGenericDefinition_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.DefineGenericParameters("T");

            MethodBuilder genericMethod = type.DefineMethod("GM", MethodAttributes.Public | MethodAttributes.Static);
            GenericTypeParameterBuilder[] methodParams = genericMethod.DefineGenericParameters("U");
            genericMethod.SetSignature(null, null, null, new Type[] { methodParams[0].AsType() }, null, null);

            Type genericIntType = type.MakeGenericType(typeof(int));
            MethodInfo createdGenericMethod = genericMethod.MakeGenericMethod(typeof(int));
            AssertExtensions.Throws<ArgumentException>("method", () => TypeBuilder.GetMethod(genericIntType, createdGenericMethod));
        }

        [Fact]
        public void GetMethod_MethodNotGenericTypeDefinition_ThrowsArgumentException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type1 = module.DefineType("Sample", TypeAttributes.Class | TypeAttributes.Public);
            GenericTypeParameterBuilder[] typeParams = type1.DefineGenericParameters("T");

            TypeBuilder myType2 = module.DefineType("Sample2", TypeAttributes.Class | TypeAttributes.Public);
            myType2.DefineGenericParameters("T");

            MethodBuilder genericMethod1 = type1.DefineMethod("GM", MethodAttributes.Public | MethodAttributes.Static);
            GenericTypeParameterBuilder[] methodParams1 = genericMethod1.DefineGenericParameters("U");
            genericMethod1.SetSignature(null, null, null, new Type[] { methodParams1[0].AsType() }, null, null);

            MethodBuilder genMethod2 = myType2.DefineMethod("GM", MethodAttributes.Public | MethodAttributes.Static);
            GenericTypeParameterBuilder[] methodParams2 = genMethod2.DefineGenericParameters("U");
            genMethod2.SetSignature(null, null, null, new Type[] { methodParams1[0].AsType() }, null, null);

            Type genericIntType = type1.MakeGenericType(typeof(int));
            AssertExtensions.Throws<ArgumentException>("type", () => TypeBuilder.GetMethod(genericIntType, genMethod2));
        }

        [Fact]
        public void GetMethod_TypeIsNotGeneric_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);

            MethodBuilder genericMethod = type.DefineMethod("GM", MethodAttributes.Public | MethodAttributes.Static);
            GenericTypeParameterBuilder[] methodParams = genericMethod.DefineGenericParameters("U");
            genericMethod.SetSignature(null, null, null, new Type[] { methodParams[0].AsType() }, null, null);

            AssertExtensions.Throws<ArgumentException>("method", () => TypeBuilder.GetMethod(type.AsType(), genericMethod));
        }

        [Fact]
        public void GetMethod_TypeNotCreated_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Throws<NotSupportedException>(() => type.AsType().GetMethod("Name"));
        }

        [Fact]
        public void GetMethods_TypeNotCreated_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Throws<NotSupportedException>(() => type.AsType().GetMethods());
        }
    }
}
