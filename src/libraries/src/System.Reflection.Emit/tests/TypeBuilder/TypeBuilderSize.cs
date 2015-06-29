// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderSize
    {
        [Fact]
        public void PosTest1()
        {
            TypeBuilder myBuilder = CreateTypeBuilderWithGenericFilledSize();
            Assert.Equal(100, myBuilder.Size);
        }

        [Fact]
        public void PosTest2()
        {
            TypeBuilder myBuilder = CreateTypeBuilderWithGenericIgnoreSize();
            Assert.Equal(0, myBuilder.Size);
        }

        [Fact]
        public void PosTest3()
        {
            TypeBuilder myBuilder = CreateTypeBuilderWithoutGenericFilledSize();
            Assert.Equal(100, myBuilder.Size);
        }

        [Fact]
        public void PosTest4()
        {
            TypeBuilder myBuilder = CreateTypeBuilderWithoutGenericFilledSize();
            Assert.Equal(100, myBuilder.Size);
        }

        [Fact]
        public void PosTest5()
        {
            TypeBuilder myBuilder = GetNestedTypeBuilder();
            Assert.Equal(100, myBuilder.Size);
        }

        private TypeBuilder CreateTypeBuilderWithGenericFilledSize()
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderGetFieldExample");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");

            TypeBuilder myType = myModule.DefineType("Sample",
                TypeAttributes.Class | TypeAttributes.Public, null, 100);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParams =
                myType.DefineGenericParameters(typeParamNames);

            ConstructorBuilder ctor = myType.DefineDefaultConstructor(
                MethodAttributes.PrivateScope | MethodAttributes.Public |
                MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName);

            FieldBuilder myField = myType.DefineField("Field",
                typeParams[0].AsType(),
                FieldAttributes.Public);

            MethodBuilder genMethod = myType.DefineMethod("GM",
                MethodAttributes.Public | MethodAttributes.Static);
            string[] methodParamNames = { "U" };
            GenericTypeParameterBuilder[] methodParams =
                genMethod.DefineGenericParameters(methodParamNames);

            genMethod.SetSignature(null, null, null,
                new Type[] { methodParams[0].AsType() }, null, null);

            ILGenerator ilg = genMethod.GetILGenerator();

            Type SampleOfU = myType.MakeGenericType(methodParams[0].AsType());

            ilg.DeclareLocal(SampleOfU);

            ConstructorInfo ctorOfU = TypeBuilder.GetConstructor(
                SampleOfU, ctor);
            ilg.Emit(OpCodes.Newobj, ctorOfU);

            ilg.Emit(OpCodes.Stloc_0);
            ilg.Emit(OpCodes.Ldloc_0);
            ilg.Emit(OpCodes.Ldarg_0);

            FieldInfo FieldOfU = TypeBuilder.GetField(
                SampleOfU, myField);

            ilg.Emit(OpCodes.Stfld, FieldOfU);

            ilg.Emit(OpCodes.Ldloc_0);
            ilg.Emit(OpCodes.Ldfld, FieldOfU);
            ilg.Emit(OpCodes.Box, methodParams[0].AsType());
            MethodInfo writeLineObj =
                typeof(Console).GetMethod("WriteLine",
                    new Type[] { typeof(object) });
            ilg.EmitCall(OpCodes.Call, writeLineObj, null);
            ilg.Emit(OpCodes.Ret);

            TypeBuilder dummy = myModule.DefineType("Dummy",
                TypeAttributes.Class | TypeAttributes.NotPublic);
            MethodBuilder entryPoint = dummy.DefineMethod("Main",
                MethodAttributes.Public | MethodAttributes.Static,
                null, null);
            ilg = entryPoint.GetILGenerator();

            Type SampleOfInt =
                myType.MakeGenericType(typeof(int));

            MethodInfo SampleOfIntGM = TypeBuilder.GetMethod(SampleOfInt,
                genMethod);

            MethodInfo GMOfString =
                SampleOfIntGM.MakeGenericMethod(typeof(string));

            ilg.Emit(OpCodes.Ldstr, "Hello, world!");
            ilg.EmitCall(OpCodes.Call, GMOfString, null);
            ilg.Emit(OpCodes.Ret);

            myType.CreateTypeInfo().AsType();

            return myType;
        }

        private TypeBuilder CreateTypeBuilderWithGenericIgnoreSize()
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderGetFieldExample");
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

            FieldBuilder myField = myType.DefineField("Field",
                typeParams[0].AsType(),
                FieldAttributes.Public);

            MethodBuilder genMethod = myType.DefineMethod("GM",
                MethodAttributes.Public | MethodAttributes.Static);
            string[] methodParamNames = { "U" };
            GenericTypeParameterBuilder[] methodParams =
                genMethod.DefineGenericParameters(methodParamNames);

            genMethod.SetSignature(null, null, null,
                new Type[] { methodParams[0].AsType() }, null, null);

            ILGenerator ilg = genMethod.GetILGenerator();

            Type SampleOfU = myType.MakeGenericType(methodParams[0].AsType());

            ilg.DeclareLocal(SampleOfU);

            ConstructorInfo ctorOfU = TypeBuilder.GetConstructor(
                SampleOfU, ctor);
            ilg.Emit(OpCodes.Newobj, ctorOfU);

            ilg.Emit(OpCodes.Stloc_0);
            ilg.Emit(OpCodes.Ldloc_0);
            ilg.Emit(OpCodes.Ldarg_0);

            FieldInfo FieldOfU = TypeBuilder.GetField(
                SampleOfU, myField);

            ilg.Emit(OpCodes.Stfld, FieldOfU);

            ilg.Emit(OpCodes.Ldloc_0);
            ilg.Emit(OpCodes.Ldfld, FieldOfU);
            ilg.Emit(OpCodes.Box, methodParams[0].AsType());
            MethodInfo writeLineObj =
                typeof(Console).GetMethod("WriteLine",
                    new Type[] { typeof(object) });
            ilg.EmitCall(OpCodes.Call, writeLineObj, null);
            ilg.Emit(OpCodes.Ret);

            TypeBuilder dummy = myModule.DefineType("Dummy",
                TypeAttributes.Class | TypeAttributes.NotPublic);
            MethodBuilder entryPoint = dummy.DefineMethod("Main",
                MethodAttributes.Public | MethodAttributes.Static,
                null, null);
            ilg = entryPoint.GetILGenerator();

            Type SampleOfInt =
                myType.MakeGenericType(typeof(int));

            MethodInfo SampleOfIntGM = TypeBuilder.GetMethod(SampleOfInt,
                genMethod);

            MethodInfo GMOfString =
                SampleOfIntGM.MakeGenericMethod(typeof(string));

            ilg.Emit(OpCodes.Ldstr, "Hello, world!");
            ilg.EmitCall(OpCodes.Call, GMOfString, null);
            ilg.Emit(OpCodes.Ret);

            myType.CreateTypeInfo().AsType();

            return myType;
        }

        private TypeBuilder CreateTypeBuilderWithoutGenericFilledSize()
        {
            AssemblyName myAsmName =
                new AssemblyName("TestTypeBuilder");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");

            TypeBuilder TestType = myModule.DefineType("Test", TypeAttributes.Class | TypeAttributes.Public, null, 100);
            MethodBuilder entry = TestType.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, null, null);
            ILGenerator ilg = entry.GetILGenerator();
            ilg.Emit(OpCodes.Ldstr, "Test string here.");
            MethodInfo writeObj = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) });
            ilg.EmitCall(OpCodes.Call, writeObj, null);
            ilg.Emit(OpCodes.Ret);

            TestType.CreateTypeInfo().AsType();
            return TestType;
        }

        private TypeBuilder CreateTypeBuilderWithoutGenericIgnoreSize()
        {
            AssemblyName myAsmName =
                new AssemblyName("TestTypeBuilder");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");

            TypeBuilder TestType = myModule.DefineType("Test", TypeAttributes.Class | TypeAttributes.Public);
            MethodBuilder entry = TestType.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, null, null);
            ILGenerator ilg = entry.GetILGenerator();
            ilg.Emit(OpCodes.Ldstr, "Test string here.");
            MethodInfo writeObj = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) });
            ilg.EmitCall(OpCodes.Call, writeObj, null);
            ilg.Emit(OpCodes.Ret);

            TestType.CreateTypeInfo().AsType();
            return TestType;
        }

        private TypeBuilder GetNestedTypeBuilder()
        {
            ModuleBuilder myModule = BuildModule();

            TypeBuilder myBuilder = myModule.DefineType("Test", TypeAttributes.Abstract | TypeAttributes.Public | TypeAttributes.Class, null, 200);
            TypeBuilder myNestedBuilder = myBuilder.DefineNestedType("Lily", TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.NestedPublic, null, 100);
            MethodBuilder mainBuilder = myBuilder.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, null, null);

            ILGenerator ilg = mainBuilder.GetILGenerator();
            MethodInfo writeLineObj = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) });
            ilg.Emit(OpCodes.Ldstr, "Test string here.");
            ilg.EmitCall(OpCodes.Call, writeLineObj, null);
            ilg.Emit(OpCodes.Ret);

            myBuilder.AsType();
            myNestedBuilder.AsType();

            return myNestedBuilder;
        }

        private ModuleBuilder BuildModule()
        {
            AssemblyName myAsmName =
                new AssemblyName("MyTypeBuilder");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");

            return myModule;
        }
    }
}
