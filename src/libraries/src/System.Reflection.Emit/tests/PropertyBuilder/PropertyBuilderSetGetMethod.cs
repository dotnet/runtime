// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest14
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
            MethodAttributes getMethodAtt = MethodAttributes.Private |
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
            myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                                         getMethodAtt,
                                                         typeof(int),
                                                         null);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldfld, myFieldBuilder);
            methodILGenerator.Emit(OpCodes.Ret);

            myPropertyBuilder.SetGetMethod(myMethodBuilder);
            MethodInfo actualMethod = myPropertyBuilder.GetGetMethod(true);
            Assert.Equal(myMethodBuilder.Name, actualMethod.Name);
        }

        [Fact]
        public void TestForProtectedGetAccessor()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            MethodAttributes getMethodAtt = MethodAttributes.Family |
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
            myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                                         getMethodAtt,
                                                         typeof(int),
                                                         null);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldfld, myFieldBuilder);
            methodILGenerator.Emit(OpCodes.Ret);

            myPropertyBuilder.SetGetMethod(myMethodBuilder);
            Type myType = myTypeBuilder.CreateTypeInfo().AsType();
            MethodInfo actualMethod = myPropertyBuilder.GetGetMethod(true);
            Assert.Equal(myMethodBuilder.Name, actualMethod.Name);
        }

        [Fact]
        public void TestForInternalGetAccessor()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            MethodAttributes getMethodAtt = MethodAttributes.FamORAssem |
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
            myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                                         getMethodAtt,
                                                         typeof(int),
                                                         null);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldfld, myFieldBuilder);
            methodILGenerator.Emit(OpCodes.Ret);

            myPropertyBuilder.SetGetMethod(myMethodBuilder);
            Type myType = myTypeBuilder.CreateTypeInfo().AsType();
            MethodInfo actualMethod = myPropertyBuilder.GetGetMethod(true);
            Assert.Equal(myMethodBuilder.Name, actualMethod.Name);
        }

        [Fact]
        public void TestForPublicStaticGetAccessor()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            bool nonPublic = false;
            MethodAttributes getMethodAtt = MethodAttributes.Static |
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
            myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                                         getMethodAtt,
                                                         typeof(int),
                                                         null);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldfld, myFieldBuilder);
            methodILGenerator.Emit(OpCodes.Ret);

            myPropertyBuilder.SetGetMethod(myMethodBuilder);
            Type myType = myTypeBuilder.CreateTypeInfo().AsType();
            MethodInfo actualMethod = myPropertyBuilder.GetGetMethod(nonPublic);
            Assert.Equal(myMethodBuilder.Name, actualMethod.Name);
        }

        [Fact]
        public void TestForPublicInstanceGetAccessor()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            MethodAttributes getMethodAtt = MethodAttributes.Public |
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
            myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                                         getMethodAtt,
                                                         typeof(int),
                                                         null);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldfld, myFieldBuilder);
            methodILGenerator.Emit(OpCodes.Ret);

            myPropertyBuilder.SetGetMethod(myMethodBuilder);
            Type myType = myTypeBuilder.CreateTypeInfo().AsType();
            MethodInfo actualMethod = myPropertyBuilder.GetGetMethod(false);
            Assert.Equal(myMethodBuilder.Name, actualMethod.Name);
        }

        [Fact]
        public void TestThrowsExceptionForNullBuilder()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder = null;

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             null);
            Assert.Throws<ArgumentNullException>(() => { myPropertyBuilder.SetGetMethod(myMethodBuilder); });
        }

        [Fact]
        public void TestThrowsExceptionForCreateTypeCalled()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            MethodAttributes getMethodAtt = MethodAttributes.Public |
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
            myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                                         getMethodAtt,
                                                         typeof(int),
                                                         null);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldfld, myFieldBuilder);
            methodILGenerator.Emit(OpCodes.Ret);
            myTypeBuilder.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => { myPropertyBuilder.SetGetMethod(myMethodBuilder); });
        }
    }
}
