// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderSetParent
    {
        public static IEnumerable<object[]> SetParent_TestData()
        {
            yield return new object[] { TypeAttributes.NotPublic, typeof(string), typeof(string) };
            yield return new object[] { TypeAttributes.NotPublic, typeof(EmptyNonGenericClass), typeof(EmptyNonGenericClass) };
            yield return new object[] { TypeAttributes.NotPublic, typeof(EmptyGenericClass<>), typeof(EmptyGenericClass<>) };
            yield return new object[] { TypeAttributes.NotPublic, typeof(int?), typeof(int?) };
            yield return new object[] { TypeAttributes.NotPublic, typeof(object), typeof(object) };
            yield return new object[] { TypeAttributes.NotPublic, null, typeof(object) };
            yield return new object[] { TypeAttributes.Interface | TypeAttributes.Abstract, null, null };
        }

        [Theory]
        [MemberData(nameof(SetParent_TestData))]
        public void SetParent(TypeAttributes attributes, Type parent, Type expected)
        {
            TypeBuilder type = Helpers.DynamicType(attributes);
            type.SetParent(parent);
            Assert.Equal(expected, type.BaseType);
        }

        [Fact]
        public void SetParent_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            type.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => type.SetParent(typeof(string)));
        }

        [Fact]
        public void SetParent_InterfaceType_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            Assert.Throws<ArgumentException>(null, () => type.SetParent(typeof(EmptyNonGenericInterface1)));
        }

        [Fact]
        public void SetParent_ByRefType_ThrowsArgumentExceptionOnCreation()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.SetParent(typeof(int).MakeByRefType());

            Assert.Throws<ArgumentException>(null, () => type.CreateTypeInfo());
        }

        [Fact]
        public void ParentNotCreated_ThrowsNotSupportedExceptionOnCreation()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type = module.DefineType("Daughter", TypeAttributes.Public);
            TypeBuilder parentType = module.DefineType("Parent", TypeAttributes.Public);

            type.SetParent(parentType.AsType());
            Assert.Throws<NotSupportedException>(() => type.CreateTypeInfo());
        }

        [Theory]
        [InlineData(typeof(void))]
        [InlineData(typeof(EmptyNonGenericStruct))]
        [InlineData(typeof(EmptyGenericStruct<>))]
        [InlineData(typeof(EmptyGenericStruct<int>))]
        [InlineData(typeof(SealedClass))]
        public void ParentNotInheritable_ThrowsTypeLoadExceptionOnCreation(Type parentType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);

            type.SetParent(parentType);
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
            Assert.Throws<NotSupportedException>(() => type.CreateTypeInfo());
        }

        [Fact]
        public void ParentOpenGenericClass_ThrowsBadImageFormatExceptionOnCreation()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);

            type.SetParent(typeof(EmptyGenericClass<>));
            Assert.Throws<BadImageFormatException>(() => type.CreateTypeInfo());
        }
    }
}
