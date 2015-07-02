// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest15
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
        public void TestForPrivateSetAccessor()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            MethodAttributes setMethodAtt = MethodAttributes.Private |
                                            MethodAttributes.SpecialName |
                                            MethodAttributes.HideBySig;

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder myFieldBuilder = myTypeBuilder.DefineField(DynamicFieldName,
                                                                    typeof(int),
                                                                    FieldAttributes.Private);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             null);
            myMethodBuilder = ImplementMethod(
                                        myTypeBuilder,
                                        DynamicMethodName,
                                        setMethodAtt,
                                        null,
                                        new Type[] { typeof(int) },
                                        myFieldBuilder);
            myPropertyBuilder.SetSetMethod(myMethodBuilder);
            MethodInfo actualMethod = myPropertyBuilder.GetSetMethod(true);
            Assert.Equal(myMethodBuilder.Name, actualMethod.Name);
        }

        [Fact]
        public void TestForProtectedSetAccessor()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            MethodAttributes setMethodAtt = MethodAttributes.Family |
                                            MethodAttributes.SpecialName |
                                            MethodAttributes.HideBySig;

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder myFieldBuilder = myTypeBuilder.DefineField(DynamicFieldName,
                                                                    typeof(int),
                                                                    FieldAttributes.Private);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             null);
            myMethodBuilder = ImplementMethod(
                                        myTypeBuilder,
                                        DynamicMethodName,
                                        setMethodAtt,
                                        null,
                                        new Type[] { typeof(int) },
                                        myFieldBuilder);
            myPropertyBuilder.SetSetMethod(myMethodBuilder);
            MethodInfo actualMethod = myPropertyBuilder.GetSetMethod(true);
            Assert.Equal(myMethodBuilder.Name, actualMethod.Name);
        }

        [Fact]
        public void TestForInternalSetAccessor()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            MethodAttributes setMethodAtt = MethodAttributes.FamORAssem |
                                            MethodAttributes.SpecialName |
                                            MethodAttributes.HideBySig;

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder myFieldBuilder = myTypeBuilder.DefineField(DynamicFieldName,
                                                                    typeof(int),
                                                                    FieldAttributes.Private);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             null);
            myMethodBuilder = ImplementMethod(
                                        myTypeBuilder,
                                        DynamicMethodName,
                                        setMethodAtt,
                                        null,
                                        new Type[] { typeof(int) },
                                        myFieldBuilder);
            myPropertyBuilder.SetSetMethod(myMethodBuilder);
            MethodInfo actualMethod = myPropertyBuilder.GetSetMethod(true);
            Assert.Equal(myMethodBuilder.Name, actualMethod.Name);
        }

        [Fact]
        public void TestForPublicStaticSetAccessor()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            MethodAttributes setMethodAtt = MethodAttributes.Static |
                                            MethodAttributes.Public |
                                            MethodAttributes.SpecialName |
                                            MethodAttributes.HideBySig;

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder myFieldBuilder = myTypeBuilder.DefineField(DynamicFieldName,
                                                                    typeof(int),
                                                                    FieldAttributes.Private);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             null);
            myMethodBuilder = ImplementMethod(
                                        myTypeBuilder,
                                        DynamicMethodName,
                                        setMethodAtt,
                                        null,
                                        new Type[] { typeof(int) },
                                        myFieldBuilder);
            myPropertyBuilder.SetSetMethod(myMethodBuilder);
            MethodInfo actualMethod = myPropertyBuilder.GetSetMethod(true);
            Assert.Equal(myMethodBuilder.Name, actualMethod.Name);
        }

        [Fact]
        public void TestForPublicInstanceSetAccessor()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            MethodAttributes setMethodAtt = MethodAttributes.Public |
                                            MethodAttributes.SpecialName |
                                            MethodAttributes.HideBySig;

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder myFieldBuilder = myTypeBuilder.DefineField(DynamicFieldName,
                                                                    typeof(int),
                                                                    FieldAttributes.Private);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             null);
            myMethodBuilder = ImplementMethod(
                                        myTypeBuilder,
                                        DynamicMethodName,
                                        setMethodAtt,
                                        null,
                                        new Type[] { typeof(int) },
                                        myFieldBuilder);
            myPropertyBuilder.SetSetMethod(myMethodBuilder);
            MethodInfo actualMethod = myPropertyBuilder.GetSetMethod(true);
            Assert.Equal(myMethodBuilder.Name, actualMethod.Name);
        }

        [Fact]
        public void TestThrowsExceptionForNullMethodBuilder()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder = null;

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             null);
            Assert.Throws<ArgumentNullException>(() => { myPropertyBuilder.SetSetMethod(myMethodBuilder); });
        }

        [Fact]
        public void TestThrowsExceptionForCreateTypeCalled()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            MethodAttributes setMethodAtt = MethodAttributes.Public |
                                            MethodAttributes.SpecialName |
                                            MethodAttributes.HideBySig;

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            FieldBuilder myFieldBuilder = myTypeBuilder.DefineField(DynamicFieldName,
                                                                    typeof(int),
                                                                    FieldAttributes.Private);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             null);
            myMethodBuilder = ImplementMethod(
                                        myTypeBuilder,
                                        DynamicMethodName,
                                        setMethodAtt,
                                        null,
                                        new Type[] { typeof(int) },
                                        myFieldBuilder);
            myTypeBuilder.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => { myPropertyBuilder.SetSetMethod(myMethodBuilder); });
        }

        private MethodBuilder ImplementMethod(
            TypeBuilder myTypeBuilder,
            string methodName,
            MethodAttributes methodAttr,
            Type returnType,
            Type[] paramTypes,
            FieldBuilder myFieldBuilder)
        {
            MethodBuilder myMethodBuilder = myTypeBuilder.DefineMethod(
                                                methodName,
                                                methodAttr,
                                                returnType,
                                                paramTypes);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();
            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldarg_1);
            methodILGenerator.Emit(OpCodes.Stfld, myFieldBuilder);
            methodILGenerator.Emit(OpCodes.Ret);

            return myMethodBuilder;
        }
    }
}
