// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderAddInterfaceImplementation
    {
        [Theory]
        [InlineData(TypeAttributes.Abstract | TypeAttributes.Class | TypeAttributes.Public)]
        [InlineData(TypeAttributes.Abstract | TypeAttributes.Interface | TypeAttributes.Public)]
        public void AddInterfaceImplementation(TypeAttributes typeAttributes)
        {
            TypeBuilder interfaceBuilder = Helpers.DynamicType(TypeAttributes.Abstract | TypeAttributes.Interface | TypeAttributes.Public);
            interfaceBuilder.DefineMethod("TestMethod",
                MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.Public,
                typeof(int),
                new Type[] { typeof(int), typeof(int) });
            Type createdInterface = interfaceBuilder.CreateTypeInfo().AsType();

            TypeBuilder type = Helpers.DynamicType(typeAttributes);
            type.AddInterfaceImplementation(createdInterface);

            Type createdType = type.CreateTypeInfo().AsType();
            Assert.Equal(createdInterface, createdType.GetTypeInfo().ImplementedInterfaces.Single(i => i.Name == createdInterface.Name));
        }

        [Fact]
        public void AddInterfaceImplementation_GeneralClass()
        {
            TypeBuilder interfaceBuilder = Helpers.DynamicType(TypeAttributes.Abstract | TypeAttributes.Interface | TypeAttributes.Public);
            interfaceBuilder.DefineMethod("TestMethod",
                MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.Public,
                typeof(int),
                new Type[] { typeof(int), typeof(int) });

            Type createdInterface = interfaceBuilder.CreateTypeInfo().AsType();

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.AddInterfaceImplementation(createdInterface);
            MethodBuilder methodBuilder = type.DefineMethod(createdInterface.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(int),
                new Type[] { typeof(int), typeof(int) });
            methodBuilder.GetILGenerator().Emit(OpCodes.Ret);

            MethodInfo createdMethod = createdInterface.GetMethod("TestMethod");
            type.DefineMethodOverride(methodBuilder, createdMethod);

            Type createdType = type.CreateTypeInfo().AsType();
            Assert.Equal(createdInterface, createdType.GetTypeInfo().ImplementedInterfaces.Single(i => i.Name == createdInterface.Name));
        }

        [Fact]
        public void AddInterfaceImplementation_NullInterfaceType_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentNullException>("interfaceType", () => type.AddInterfaceImplementation(null));
        }

        [Fact]
        public void AddInterfaceImplementation_ByRefInterfaceType_ThrowsArgumentExceptioN()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentException>(null, () => type.AddInterfaceImplementation(typeof(int).MakeByRefType()));
        }

        [Fact]
        public void AddInterfaceImplementation_TypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.CreateTypeInfo().AsType();
            
            Assert.Throws<InvalidOperationException>(() => type.AddInterfaceImplementation(typeof(EmptyNonGenericInterface1)));
        }

        public static IEnumerable<object[]> NonInterfaceTypes_TestData()
        {
            yield return new object[] { typeof(EmptyNonGenericClass) };
            yield return new object[] { typeof(EmptyNonGenericStruct) };
            yield return new object[] { typeof(EmptyGenericClass<int>) };
            yield return new object[] { typeof(EmptyGenericStruct<>) };
            yield return new object[] { typeof(EmptyGenericStruct<int>) };
            yield return new object[] { typeof(EmptyEnum) };
            yield return new object[] { typeof(int).MakePointerType() };
            yield return new object[] { typeof(int[]) };
            yield return new object[] { typeof(GenericInterface<>).GetGenericArguments()[0] };
            yield return new object[] { typeof(GenericInterface<int>).GetGenericArguments()[0] };
        }

        [Theory]
        [MemberData(nameof(NonInterfaceTypes_TestData))]
        public void AddInterfaceImplementation_TypeNotInterface_ThrowsTypeLoadExceptionOnCreation(Type interfaceType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.AddInterfaceImplementation(interfaceType);

            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        [Theory]
        [InlineData(typeof(EmptyGenericInterface<>))]
        [InlineData(typeof(EmptyGenericClass<>))]
        public void AddInterfaceImplementation_OpenGenericType_ThrowsBadImageFormatExceptionOnCreation(Type interfaceType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.AddInterfaceImplementation(interfaceType);

            Assert.Throws<BadImageFormatException>(() => type.CreateTypeInfo());
        }

        [Fact]
        public void AddInterfaceImplementation_TypeDoesntImplementInterface_ThrowsTypeLoadExceptionOnCreation()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.AddInterfaceImplementation(typeof(NonGenericInterface));

            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        [Fact]
        public void AddInterfaceImplementation_InterfaceNotCreated_ThrowsTypeLoadExceptionOnCreation()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.AddInterfaceImplementation(Helpers.DynamicType(TypeAttributes.Interface | TypeAttributes.Abstract).AsType());

            Assert.Throws<TypeLoadException>(() => type.CreateTypeInfo());
        }

        public interface NonGenericInterface
        {
            void Method();
        }

        public interface GenericInterface<T>
        {
            T Method();
        }
    }
}
