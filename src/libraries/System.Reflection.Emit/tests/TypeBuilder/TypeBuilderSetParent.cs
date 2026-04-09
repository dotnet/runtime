// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderSetParent
    {
        public static IEnumerable<object[]> SetParent_TestData()
        {
            yield return new object[] { TypeAttributes.NotPublic, typeof(EmptyNonGenericClass), typeof(EmptyNonGenericClass) };
            yield return new object[] { TypeAttributes.NotPublic, typeof(object), typeof(object) };
            yield return new object[] { TypeAttributes.NotPublic, null, typeof(object) };
            yield return new object[] { TypeAttributes.Abstract, typeof(EmptyGenericClass<int>), typeof(EmptyGenericClass<int>) };
            yield return new object[] { TypeAttributes.Interface | TypeAttributes.Abstract, null, null };
        }

        [Theory]
        [MemberData(nameof(SetParent_TestData))]
        public void SetParent(TypeAttributes attributes, Type parent, Type expected)
        {
            TypeBuilder type = Helpers.DynamicType(attributes);

            type.SetParent(parent);
            Assert.Equal(expected, type.BaseType);

            TypeInfo createdType = type.CreateTypeInfo();
            Assert.Equal(expected, createdType.BaseType);
        }

        [Fact]
        public void SetParent_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            type.CreateType();
            Assert.Throws<InvalidOperationException>(() => type.SetParent(typeof(string)));
        }

        [Fact]
        public void SetParent_ThisIsInterface_ThrowsTypeLoadExceptionOnLoad()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Interface | TypeAttributes.Abstract);
            type.SetParent(typeof(EmptyNonGenericClass));
            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        [Theory]
        [InlineData(TypeAttributes.NotPublic)]
        [InlineData(TypeAttributes.Interface | TypeAttributes.Abstract)]
        public void SetParent_InterfaceType_ThrowsArgumentException(TypeAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(attributes);
            AssertExtensions.Throws<ArgumentException>(null, () => type.SetParent(typeof(EmptyNonGenericInterface1)));
        }

        [Fact]
        public void SetParent_ByRefType_ThrowsNotSupportedExceptionOnCreation()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);

            type.SetParent(typeof(int).MakeByRefType());
            Assert.Equal(typeof(int).MakeByRefType(), type.BaseType);

            Assert.Throws<NotSupportedException>(() => type.CreateTypeInfo());
        }

        [Fact]
        public void SetParent_GenericParameter_ThrowsNotSupportedExceptionOnCreation()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            GenericTypeParameterBuilder genericType = type.DefineGenericParameters("T")[0];

            type.SetParent(genericType.AsType());
            Assert.Equal(genericType.AsType(), type.BaseType);

            Assert.Throws<NotSupportedException>(() => type.CreateTypeInfo());
        }

        [Fact]
        public void ParentNotCreated_ThrowsNotSupportedExceptionOnCreation()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type = module.DefineType("Daughter", TypeAttributes.Public);
            TypeBuilder parentType = module.DefineType("Parent", TypeAttributes.Public);

            type.SetParent(parentType.AsType());
            Assert.Equal(parentType.AsType(), type.BaseType);

            Assert.Throws<NotSupportedException>(() => type.CreateTypeInfo());
        }

        [Theory]
        [InlineData(typeof(void))]
        [InlineData(typeof(EmptyNonGenericStruct))]
        [InlineData(typeof(EmptyEnum))]
        [InlineData(typeof(EmptyGenericStruct<>))]
        [InlineData(typeof(EmptyGenericStruct<int>))]
        [InlineData(typeof(SealedClass))]
        [InlineData(typeof(int?))]
        public void ParentNotInheritable_ThrowsTypeLoadExceptionOnCreation(Type parentType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);

            type.SetParent(parentType);
            Assert.Equal(parentType, type.BaseType);

            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(StaticClass))]
        [InlineData(typeof(int*))]
        [InlineData(typeof(EmptyNonGenericClass[]))]
        public void ParentHasNoDefaultConstructor_ThrowsNotSupportedExceptionOnCreation(Type parentType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);

            type.SetParent(parentType);
            Assert.Equal(parentType, type.BaseType);

            Assert.Throws<NotSupportedException>(() => type.CreateTypeInfo());
        }

        [Fact]
        public void ParentOpenGenericClass_ThrowsBadImageFormatExceptionOnCreation()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);

            type.SetParent(typeof(EmptyGenericClass<>));
            Assert.Equal(typeof(EmptyGenericClass<>), type.BaseType);

            Assert.Throws<BadImageFormatException>(() => type.CreateTypeInfo());
        }
    }
}
