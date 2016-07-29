// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Type testType = type.CreateTypeInfo().AsType();

            Assert.Equal(createdInterface, testType.GetTypeInfo().ImplementedInterfaces.Where(i => i.Name == createdInterface.Name).FirstOrDefault());
        }

        [Fact]
        public void AddInterfaceImplementation_GeneralClass()
        {
            TypeBuilder interfaceBuilder = Helpers.DynamicType(TypeAttributes.Abstract | TypeAttributes.Interface | TypeAttributes.Public);
            interfaceBuilder.DefineMethod("TestMethod",
                                                            MethodAttributes.Abstract |
                                                            MethodAttributes.Virtual |
                                                            MethodAttributes.Public,
                                                            typeof(int),
                                                            new Type[]
                                                            { typeof(int), typeof(int) });

            Type createdInterface = interfaceBuilder.CreateTypeInfo().AsType();

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.AddInterfaceImplementation(createdInterface);
            MethodBuilder methodBuilder = type.DefineMethod(createdInterface.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(int),
                new Type[] { typeof(int), typeof(int) });

            ILGenerator ilGenerator = methodBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            MethodInfo createdMethod = createdInterface.GetMethod("TestMethod");
            type.DefineMethodOverride(methodBuilder, createdMethod);
            Type testType = type.CreateTypeInfo().AsType();

            Assert.Equal(createdInterface, testType.GetTypeInfo().ImplementedInterfaces.Where(i => i.Name == createdInterface.Name).FirstOrDefault());
        }

        [Fact]
        public void AddInterfaceImplementation_NullInterfaceType_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Throws<ArgumentNullException>("interfaceType", () => type.AddInterfaceImplementation(null));
        }

        [Fact]
        public void AddInterfaceImplementation_TypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder testTypeBuilder = Helpers.DynamicType(TypeAttributes.Public);
            testTypeBuilder.CreateTypeInfo().AsType();

            TypeBuilder interfaceBuilder = Helpers.DynamicType(TypeAttributes.Abstract | TypeAttributes.Interface | TypeAttributes.Public);
            interfaceBuilder.DefineMethod("TestMethod",
                MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.Public,
                typeof(void),
                new Type[] { typeof(int), typeof(int) });
            Type createdInterface = interfaceBuilder.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => testTypeBuilder.AddInterfaceImplementation(createdInterface));
        }
    }
}
