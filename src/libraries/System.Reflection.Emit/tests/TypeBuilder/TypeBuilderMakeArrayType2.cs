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
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        [Fact]
        public void TestWithInstanceType()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType");
            Type arrayType = testTyBuilder.MakeArrayType(1);
            Assert.Equal(typeof(Array), arrayType.GetTypeInfo().BaseType);
            Assert.Equal("testType[*]", arrayType.Name);
        }

        [Fact]
        public void TestWithMultiDimensionInstanceType()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType");
            Type arrayType = testTyBuilder.MakeArrayType(3);
            Assert.Equal(typeof(Array), arrayType.GetTypeInfo().BaseType);
            Assert.Equal("testType[,,]", arrayType.Name);
        }

        [Fact]
        public void TestWithAbstractType()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType", TypeAttributes.Public | TypeAttributes.Abstract);
            Type arrayType = testTyBuilder.MakeArrayType(1);
            Assert.Equal(typeof(Array), arrayType.GetTypeInfo().BaseType);
            Assert.Equal("testType[*]", arrayType.Name);
        }

        [Fact]
        public void TestWithMultiDimensionAndAbstractType()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType", TypeAttributes.Public | TypeAttributes.Abstract);
            Type arrayType = testTyBuilder.MakeArrayType(3);
            Assert.Equal(typeof(Array), arrayType.GetTypeInfo().BaseType);
            Assert.Equal("testType[,,]", arrayType.Name);
        }

        [Fact]
        public void TestThrowsExceptionForZeroRank()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType", TypeAttributes.Public | TypeAttributes.Abstract);
            Assert.Throws<IndexOutOfRangeException>(() => { Type arrayType = testTyBuilder.MakeArrayType(0); });
        }

        [Fact]
        public void TestThrowsExcetpionForNegativeRank()
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
                return minValue + _generator.GetInt32() % (maxValue - minValue);
            }
            return minValue;
        }
    }
}
