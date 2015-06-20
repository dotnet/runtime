// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest4
    {
        private const string DynamicAssemblyName = "TestDynamicAssembly";
        private const string DynamicModuleName = "TestDynamicModule";
        private const string DynamicTypeName = "TestDynamicType";
        private const string DynamicFieldName = "TestDynamicFieldA";
        private const string DynamicPropertyName = "TestDynamicProperty";
        private const string DynamicMethodName = "DynamicMethodA";

        private TypeBuilder GetTypeBuilder(TypeAttributes typeAtt)
        {
            AssemblyName myAssemblyName = new AssemblyName();
            myAssemblyName.Name = DynamicAssemblyName;
            AssemblyBuilder myAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(myAssemblyName,
                                                                            AssemblyBuilderAccess.Run);

            ModuleBuilder myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssemblyBuilder,
                                                                                DynamicModuleName);

            return myModuleBuilder.DefineType(DynamicTypeName, typeAtt);
        }

        [Fact]
        public void PosTest1()
        {
            bool actualValue, expectedValue;
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            MethodAttributes getMethodAtt = MethodAttributes.Private |
                                            MethodAttributes.SpecialName |
                                            MethodAttributes.HideBySig;
            expectedValue = true;

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);

            FieldBuilder myFieldBuilder = myTypeBuilder.DefineField(
                                            DynamicFieldName,
                                            typeof(int),
                                            FieldAttributes.Private);

            myPropertyBuilder = myTypeBuilder.DefineProperty(
                                    DynamicPropertyName,
                                    PropertyAttributes.None,
                                    typeof(int),
                                    null);

            myMethodBuilder = ImplementMethod(
                                    myTypeBuilder,
                                    DynamicMethodName,
                                    getMethodAtt,
                                    typeof(int),
                                    new Type[0],
                                    myFieldBuilder);

            myPropertyBuilder.SetGetMethod(myMethodBuilder);
            actualValue = myPropertyBuilder.CanRead;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest2()
        {
            bool actualValue, expectedValue;
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            MethodAttributes setMethodAtt = MethodAttributes.Public |
                                            MethodAttributes.SpecialName |
                                            MethodAttributes.HideBySig;
            expectedValue = false;

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder myFieldBuilder = myTypeBuilder.DefineField(DynamicFieldName,
                                                                    typeof(int),
                                                                    FieldAttributes.Private);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             null);
            myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                                         setMethodAtt,
                                                         null,
                                                         new Type[] { typeof(int) });
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldarg_1);
            methodILGenerator.Emit(OpCodes.Stfld, myFieldBuilder);
            methodILGenerator.Emit(OpCodes.Ret);

            myPropertyBuilder.SetSetMethod(myMethodBuilder);
            actualValue = myPropertyBuilder.CanRead;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest3()
        {
            bool actualValue, expectedValue;
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            expectedValue = false;
            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder myFieldBuilder = myTypeBuilder.DefineField(DynamicFieldName,
                                                                    typeof(int),
                                                                    FieldAttributes.Private);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             null);
            actualValue = myPropertyBuilder.CanRead;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest4()
        {
            bool actualValue, expectedValue;
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            MethodAttributes getMethodAtt = MethodAttributes.Static |
                                            MethodAttributes.Public |
                                            MethodAttributes.SpecialName |
                                            MethodAttributes.HideBySig;
            expectedValue = true;
            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder myFieldBuilder = myTypeBuilder.DefineField(DynamicFieldName,
                                                                    typeof(int),
                                                                    FieldAttributes.Private);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             null);
            myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                                         getMethodAtt,
                                                         typeof(int),
                                                         null);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldfld, myFieldBuilder);
            methodILGenerator.Emit(OpCodes.Ret);

            myPropertyBuilder.SetGetMethod(myMethodBuilder);
            actualValue = myPropertyBuilder.CanRead;
            Assert.Equal(expectedValue, actualValue);
        }

        private MethodBuilder ImplementMethod(TypeBuilder myTypeBuilder, string methodName, MethodAttributes methodAttr, Type returnType, Type[] paramTypes, FieldBuilder myFieldBuilder)
        {
            MethodBuilder myMethodBuilder = myTypeBuilder.DefineMethod(
                                                methodName,
                                                methodAttr,
                                                returnType,
                                                paramTypes);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();
            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldfld, myFieldBuilder);
            methodILGenerator.Emit(OpCodes.Ret);

            return myMethodBuilder;
        }
    }
}
