// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderAddInterfaceImplementation
    {
        private const string DynamicAssemblyName = "TestDynamicAssembly";
        private const string DynamicModuleName = "TestDynamicModule";
        private const string DynamicTypeName = "TestDynamicType";

        private const string DynamicInterfaceName = "TestDynamicInterface";
        private const string DynamicMethodName = "TestDynamicMethodA";

        public TypeBuilder RetriveTestTypeBuilder(TypeAttributes typeAtt)
        {
            AssemblyName asmName = new AssemblyName();
            asmName.Name = DynamicAssemblyName;
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

            ModuleBuilder modBuilder = TestLibrary.Utilities.GetModuleBuilder(asmBuilder, "Module1");

            TypeBuilder typeBuilder = modBuilder.DefineType(DynamicTypeName, typeAtt);

            return typeBuilder;
        }

        public TypeBuilder RetriveTestInterfaceBuilder()
        {
            AssemblyName asmName = new AssemblyName();
            asmName.Name = DynamicAssemblyName;
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName,
                                                                                                    AssemblyBuilderAccess.Run);

            ModuleBuilder modBuilder = TestLibrary.Utilities.GetModuleBuilder(asmBuilder, "Module1");

            TypeBuilder typeBuilder = modBuilder.DefineType(DynamicInterfaceName,
                                                                                 TypeAttributes.Abstract |
                                                                                 TypeAttributes.Interface |
                                                                                 TypeAttributes.Public);

            return typeBuilder;
        }

        [Fact]
        public void PosTest1()
        {
            Type expectedType;
            Type actualType;
            TypeBuilder testTypeBuilder;
            TypeBuilder testInterfaceBuilder;

            testInterfaceBuilder = RetriveTestInterfaceBuilder();
            testInterfaceBuilder.DefineMethod(DynamicMethodName,
                                                            MethodAttributes.Abstract |
                                                            MethodAttributes.Virtual |
                                                            MethodAttributes.Public,
                                                            typeof(int),
                                                            new Type[] { typeof(int), typeof(int) });
            Type testInterface = testInterfaceBuilder.CreateTypeInfo().AsType();

            testTypeBuilder = RetriveTestTypeBuilder(TypeAttributes.Abstract |
                                                                             TypeAttributes.Class |
                                                                             TypeAttributes.Public);
            testTypeBuilder.AddInterfaceImplementation(testInterface);
            Type testType = testTypeBuilder.CreateTypeInfo().AsType();

            expectedType = testInterface;
            actualType = testType.GetTypeInfo().ImplementedInterfaces.Where(i => i.Name == testInterface.Name).FirstOrDefault();
            Assert.Equal(expectedType, actualType);
        }

        [Fact]
        public void PosTest2()
        {
            Type expectedType;
            Type actualType;
            TypeBuilder testTypeBuilder;
            TypeBuilder testInterfaceBuilder;

            testInterfaceBuilder = RetriveTestInterfaceBuilder();
            testInterfaceBuilder.DefineMethod(DynamicMethodName,
                                                            MethodAttributes.Abstract |
                                                            MethodAttributes.Virtual |
                                                            MethodAttributes.Public,
                                                            typeof(int),
                                                            new Type[]
                                                            { typeof(int), typeof(int) });

            Type testInterface = testInterfaceBuilder.CreateTypeInfo().AsType();

            testTypeBuilder = RetriveTestTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);

            testTypeBuilder.AddInterfaceImplementation(testInterface);
            MethodBuilder methodBuilder = testTypeBuilder.DefineMethod(testInterface.Name,
                                                                                                   MethodAttributes.Public |
                                                                                                   MethodAttributes.Virtual,
                                                                                                   typeof(int),
                                                                                                   new Type[]
                                                                                                   { typeof(int), typeof(int) });
            byte[] ILcodes = new byte[]
            {
                    0x02,   // 02h is the opcode for ldarg.0
                    0x03,   // 03h is the opcode for ldarg.1
                    0x58,   // 58h is the opcode for add
                    0x2A    // 2Ah is the opcode for ret
            };

            ILGenerator ilgen = methodBuilder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);
            MethodInfo methodInfo = testInterface.GetMethod(DynamicMethodName);
            testTypeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
            Type testType = testTypeBuilder.CreateTypeInfo().AsType();

            expectedType = testInterface;
            actualType = testType.GetTypeInfo().ImplementedInterfaces.Where(i => i.Name == testInterface.Name).FirstOrDefault();
            Assert.Equal(expectedType, actualType);
        }

        [Fact]
        public void PosTest3()
        {
            Type expectedType;
            Type actualType;
            TypeBuilder testTypeBuilder;
            TypeBuilder testInterfaceBuilder;

            testInterfaceBuilder = RetriveTestInterfaceBuilder();
            testInterfaceBuilder.DefineMethod(DynamicMethodName,
                                                            MethodAttributes.Abstract |
                                                            MethodAttributes.Virtual |
                                                            MethodAttributes.Public,
                                                            typeof(int),
                                                            new Type[] { typeof(int), typeof(int) });
            Type testInterface = testInterfaceBuilder.CreateTypeInfo().AsType();

            testTypeBuilder = RetriveTestTypeBuilder(TypeAttributes.Abstract |
                                                                             TypeAttributes.Interface |
                                                                             TypeAttributes.Public);
            testTypeBuilder.AddInterfaceImplementation(testInterface);
            Type testType = testTypeBuilder.CreateTypeInfo().AsType();

            expectedType = testInterface;
            actualType = testType.GetTypeInfo().ImplementedInterfaces.Where(i => i.Name == testInterface.Name).FirstOrDefault();
            Assert.Equal(expectedType, actualType);
        }

        [Fact]
        public void NegTest1()
        {
            TypeBuilder testTypeBuilder;

            testTypeBuilder = RetriveTestTypeBuilder(TypeAttributes.Public);
            Assert.Throws<ArgumentNullException>(() => { testTypeBuilder.AddInterfaceImplementation(null); });
        }

        [Fact]
        public void NegTest2()
        {
            TypeBuilder testTypeBuilder;
            TypeBuilder testInterfaceBuilder;

            testTypeBuilder = RetriveTestTypeBuilder(TypeAttributes.Public);
            testTypeBuilder.CreateTypeInfo().AsType();

            testInterfaceBuilder = RetriveTestInterfaceBuilder();
            testInterfaceBuilder.DefineMethod(DynamicMethodName,
                                                            MethodAttributes.Abstract |
                                                            MethodAttributes.Virtual |
                                                            MethodAttributes.Public,
                                                            typeof(void),
                                                            new Type[] { typeof(int), typeof(int) });
            Type testInterface = testInterfaceBuilder.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => { testTypeBuilder.AddInterfaceImplementation(testInterface); });
        }
    }
}
