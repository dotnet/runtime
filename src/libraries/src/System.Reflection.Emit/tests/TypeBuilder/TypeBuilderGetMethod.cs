// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using TestLibrary;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderGetMethod
    {
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

            MethodBuilder genMethod = myType.DefineMethod("GM",
                MethodAttributes.Public | MethodAttributes.Static);
            string[] methodParamNames = { "U" };
            GenericTypeParameterBuilder[] methodParams =
                genMethod.DefineGenericParameters(methodParamNames);

            genMethod.SetSignature(null, null, null,
                new Type[] { methodParams[0].AsType() }, null, null);

            MethodInfo SampleOfIntGM = TypeBuilder.GetMethod(myType.AsType(),
                genMethod);

            Assert.True(SampleOfIntGM.IsGenericMethodDefinition);
            Assert.Equal("U", SampleOfIntGM.GetGenericArguments()[0].ToString());
        }

        [Fact]
        public void PosTest2()
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

            MethodBuilder genMethod = myType.DefineMethod("GM",
                MethodAttributes.Public | MethodAttributes.Static);
            string[] methodParamNames = { "U" };
            GenericTypeParameterBuilder[] methodParams =
                genMethod.DefineGenericParameters(methodParamNames);

            genMethod.SetSignature(null, null, null,
                new Type[] { methodParams[0].AsType() }, null, null);

            Type SampleOfInt =
                myType.MakeGenericType(typeof(int));
            MethodInfo SampleOfIntGM = TypeBuilder.GetMethod(SampleOfInt,
                genMethod);

            Assert.True(SampleOfIntGM.IsGenericMethodDefinition);
            Assert.Equal("U", SampleOfIntGM.GetGenericArguments()[0].ToString());
        }

        [Fact]
        public void NegTest1()
        {
            Assert.Throws<ArgumentException>(() => { TypeBuilder.GetMethod(typeof(int), typeof(int).GetMethod("Parse", new Type[] { typeof(string) })); });
        }

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

            MethodBuilder genMethod = myType.DefineMethod("GM",
                MethodAttributes.Public | MethodAttributes.Static);
            string[] methodParamNames = { "U" };
            GenericTypeParameterBuilder[] methodParams =
                genMethod.DefineGenericParameters(methodParamNames);

            genMethod.SetSignature(null, null, null,
                new Type[] { methodParams[0].AsType() }, null, null);

            Type SampleOfInt =
                myType.MakeGenericType(typeof(int));
            MethodInfo genMethod2 = genMethod.MakeGenericMethod(typeof(int));
            Assert.Throws<ArgumentException>(() =>
            {
                MethodInfo SampleOfIntGM = TypeBuilder.GetMethod(SampleOfInt, genMethod2);
            });
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

            MethodBuilder genMethod = myType.DefineMethod("GM",
                MethodAttributes.Public | MethodAttributes.Static);
            string[] methodParamNames = { "U" };
            GenericTypeParameterBuilder[] methodParams =
                genMethod.DefineGenericParameters(methodParamNames);
            genMethod.SetSignature(null, null, null,
                new Type[] { methodParams[0].AsType() }, null, null);

            MethodBuilder genMethod2 = myType2.DefineMethod("GM", MethodAttributes.Public | MethodAttributes.Static);
            GenericTypeParameterBuilder[] methodParams2 =
                genMethod2.DefineGenericParameters(methodParamNames);
            genMethod2.SetSignature(null, null, null,
                new Type[] { methodParams[0].AsType() }, null, null);

            Type SampleOfInt =
                myType.MakeGenericType(typeof(int));

            Assert.Throws<ArgumentException>(() =>
            {
                MethodInfo SampleOfIntGM = TypeBuilder.GetMethod(SampleOfInt, genMethod2);
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

            MethodBuilder genMethod = myType.DefineMethod("GM",
                MethodAttributes.Public | MethodAttributes.Static);
            string[] methodParamNames = { "U" };
            GenericTypeParameterBuilder[] methodParams =
                genMethod.DefineGenericParameters(methodParamNames);

            genMethod.SetSignature(null, null, null,
                new Type[] { methodParams[0].AsType() }, null, null);

            Assert.Throws<ArgumentException>(() =>
            {
                MethodInfo SampleOfIntGM = TypeBuilder.GetMethod(myType.AsType(), genMethod);
            });
        }
    }
}
