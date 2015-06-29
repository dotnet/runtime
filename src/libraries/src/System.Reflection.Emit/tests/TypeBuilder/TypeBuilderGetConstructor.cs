// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using TestLibrary;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderGetConstructor
    {
        [Fact]
        public void NegTest2()
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderGetMethodTest");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                 myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");


            TypeBuilder myType = myModule.DefineType("Sample",
                TypeAttributes.Class | TypeAttributes.Public);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParams =
                myType.DefineGenericParameters(typeParamNames);

            ConstructorBuilder ctor = myType.DefineDefaultConstructor(
                 MethodAttributes.PrivateScope | MethodAttributes.Public |
                 MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                 MethodAttributes.RTSpecialName);

            Assert.Throws<ArgumentException>(() =>
            {
                ConstructorInfo ci = TypeBuilder.GetConstructor(myType.AsType(), ctor);
            });
        }

        [Fact]
        public void PosTest1()
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderGetMethodTest");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                 myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");


            TypeBuilder myType = myModule.DefineType("Sample",
                TypeAttributes.Class | TypeAttributes.Public);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParams =
                myType.DefineGenericParameters(typeParamNames);


            ConstructorBuilder ctor = myType.DefineDefaultConstructor(
                 MethodAttributes.PrivateScope | MethodAttributes.Public |
                 MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                 MethodAttributes.RTSpecialName);

            Type SampleOfInt =
                myType.MakeGenericType(typeof(int));
            ConstructorInfo ci = TypeBuilder.GetConstructor(SampleOfInt,
                ctor);

            Assert.False(ci.IsGenericMethodDefinition);
        }

        [Fact]
        public void NegTest1()
        {
            Assert.Throws<ArgumentException>(() => { TypeBuilder.GetConstructor(typeof(int), typeof(int).GetConstructor(new Type[0])); });
        }

        [Fact]
        public void NegTest3()
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderGetMethodTest");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                 myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");


            TypeBuilder myType = myModule.DefineType("Sample",
                TypeAttributes.Class | TypeAttributes.Public);
            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParams =
                myType.DefineGenericParameters(typeParamNames);

            TypeBuilder myType2 = myModule.DefineType("Sample2",
                TypeAttributes.Class | TypeAttributes.Public);
            GenericTypeParameterBuilder[] typeParams2 =
                myType2.DefineGenericParameters(typeParamNames);

            ConstructorBuilder ctor = myType.DefineDefaultConstructor(
                 MethodAttributes.PrivateScope | MethodAttributes.Public |
                 MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                 MethodAttributes.RTSpecialName);

            ConstructorBuilder ctor2 = myType2.DefineDefaultConstructor(
                 MethodAttributes.PrivateScope | MethodAttributes.Public |
                 MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                 MethodAttributes.RTSpecialName);

            Type SampleOfInt =
                myType.MakeGenericType(typeof(int));

            Assert.Throws<ArgumentException>(() =>
            {
                ConstructorInfo ci = TypeBuilder.GetConstructor(SampleOfInt, ctor2);
            });
        }

        [Fact]
        public void NegTest4()
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderGetMethodTest");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                 myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");

            TypeBuilder myType = myModule.DefineType("Sample",
                TypeAttributes.Class | TypeAttributes.Public);

            ConstructorBuilder ctor = myType.DefineDefaultConstructor(
                 MethodAttributes.PrivateScope | MethodAttributes.Public |
                 MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                 MethodAttributes.RTSpecialName);

            Assert.Throws<ArgumentException>(() =>
            {
                ConstructorInfo ci = TypeBuilder.GetConstructor(myType.AsType(), ctor);
            });
        }
    }
}
