// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using TestLibrary;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderGetField
    {
        [Fact]
        public void TestThrowsExceptionForDeclaringTypeOfFieldNotGeneric()
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderGetFieldTest");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                 myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");

            TypeBuilder myType = myModule.DefineType("Sample",
                TypeAttributes.Class | TypeAttributes.Public);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParams =
                myType.DefineGenericParameters(typeParamNames);

            FieldBuilder myField = myType.DefineField("Field",
                typeParams[0].AsType(),
                FieldAttributes.Public);

            Assert.Throws<ArgumentException>(() =>
            {
                FieldInfo fi = TypeBuilder.GetField(myType.AsType(), myField);
            });
        }

        [Fact]
        public void TestGetField()
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderGetFieldTest");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                 myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");

            TypeBuilder myType = myModule.DefineType("Sample",
                TypeAttributes.Class | TypeAttributes.Public);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParams =
                myType.DefineGenericParameters(typeParamNames);

            FieldBuilder myField = myType.DefineField("Field",
                typeParams[0].AsType(),
                FieldAttributes.Public);

            Type SampleOfInt =
                myType.MakeGenericType(typeof(int));
            FieldInfo fi = TypeBuilder.GetField(SampleOfInt,
                myField);

            Assert.Equal("Field", fi.Name);
        }

        [Fact]
        public void TestThrowsExceptionForTypeIsNotTypeBuilder()
        {
            Assert.Throws<ArgumentException>(() => { TypeBuilder.GetField(typeof(int), typeof(int).GetField("MaxValue")); });
        }

        [Fact]
        public void TestThrowsExceptionForDeclaringTypeOfFieldNotGenericTypeDefinitionOfType()
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderGetFieldTest");
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

            FieldBuilder myField = myType.DefineField("Field",
                typeParams[0].AsType(),
                FieldAttributes.Public);

            FieldBuilder myField2 = myType2.DefineField("Field",
                typeParams[0].AsType(),
                FieldAttributes.Public);


            Type SampleOfInt =
                myType.MakeGenericType(typeof(int));

            Assert.Throws<ArgumentException>(() =>
            {
                FieldInfo fi = TypeBuilder.GetField(SampleOfInt, myField2);
            });
        }

        [Fact]
        public void TestThrowsExceptionForTypeNotGeneric()
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderGetFieldTest");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                 myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");



            TypeBuilder myType = myModule.DefineType("Sample",
                TypeAttributes.Class | TypeAttributes.Public);

            FieldBuilder myField = myType.DefineField("Field",
                typeof(int),
                FieldAttributes.Public);

            Assert.Throws<ArgumentException>(() =>
            {
                FieldInfo fi = TypeBuilder.GetField(myType.AsType(), myField);
            });
        }
    }
}
