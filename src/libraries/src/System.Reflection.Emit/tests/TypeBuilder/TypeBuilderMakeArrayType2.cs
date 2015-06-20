// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderMakeArrayType2
    {
        [Fact]
        public void PosTest1()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType");
            Type arrayType = testTyBuilder.MakeArrayType(1);
            Assert.False(arrayType.GetTypeInfo().BaseType != typeof(Array) || arrayType.Name != "testType[*]");
        }

        [Fact]
        public void PosTest2()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType");
            Type arrayType = testTyBuilder.MakeArrayType(3);
            Assert.False(arrayType.GetTypeInfo().BaseType != typeof(Array) || arrayType.Name != "testType[,,]");
        }

        [Fact]
        public void PosTest3()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType", TypeAttributes.Public | TypeAttributes.Abstract);
            Type arrayType = testTyBuilder.MakeArrayType(1);
            Assert.False(arrayType.GetTypeInfo().BaseType != typeof(Array) || arrayType.Name != "testType[*]");
        }

        [Fact]
        public void PosTest4()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType", TypeAttributes.Public | TypeAttributes.Abstract);
            Type arrayType = testTyBuilder.MakeArrayType(3);
            Assert.False(arrayType.GetTypeInfo().BaseType != typeof(Array) || arrayType.Name != "testType[,,]");
        }

        [Fact]
        public void NegTest1()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType", TypeAttributes.Public | TypeAttributes.Abstract);
            Assert.Throws<IndexOutOfRangeException>(() => { Type arrayType = testTyBuilder.MakeArrayType(0); });
        }

        [Fact]
        public void NegTest2()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType");
            Assert.Throws<IndexOutOfRangeException>(() => { Type arrayType = testTyBuilder.MakeArrayType(GetInt32(0, int.MaxValue) * (-1)); });
        }

        private ModuleBuilder CreateModuleBuilder()
        {
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = "myAssembly.dll";
            AssemblyBuilder myAssemBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssemBuilder, "Module1");
            return myModBuilder;
        }

        private int GetInt32(int minValue, int maxValue)
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + TestLibrary.Generator.GetInt32() % (maxValue - minValue);
            }
            return minValue;
        }
    }
}
