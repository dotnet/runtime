// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest2
    {
        private const string DynamicAssemblyName = "TestDynamicAssembly";
        private const string DynamicModuleName = "TestDynamicModule";
        private const string DynamicTypeName = "TestDynamicType";
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
        public void TestWithPublicSingleParameterIntReturnTypeMethod()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            Type[] paramTypes = new Type[]
            {
                typeof(int)
            };

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             new Type[0]);
            myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                                         MethodAttributes.Public,
                                                         CallingConventions.HasThis,
                                                         typeof(int),
                                                         paramTypes);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldarg_1);
            methodILGenerator.Emit(OpCodes.Ret);

            myPropertyBuilder.AddOtherMethod(myMethodBuilder);
            Type myType = myTypeBuilder.CreateTypeInfo().AsType();
            PropertyInfo myProperty = myType.GetProperty(DynamicPropertyName,
                                                         BindingFlags.Public |
                                                         BindingFlags.NonPublic |
                                                         BindingFlags.Instance);
            VerifyAddMethodsResult(myProperty, myMethodBuilder);
        }

        [Fact]
        public void TestWithPrivateSingleParameterIntReturnTypeMethod()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            Type[] paramTypes = new Type[]
            {
                typeof(int)
            };

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             new Type[0]);
            myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                                         MethodAttributes.Private,
                                                         CallingConventions.HasThis,
                                                         typeof(int),
                                                         paramTypes);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldarg_1);
            methodILGenerator.Emit(OpCodes.Ret);

            myPropertyBuilder.AddOtherMethod(myMethodBuilder);
            Type myType = myTypeBuilder.CreateTypeInfo().AsType();
            PropertyInfo myProperty = myType.GetProperty(DynamicPropertyName,
                                                         BindingFlags.Public |
                                                         BindingFlags.NonPublic |
                                                         BindingFlags.Instance);
            VerifyAddMethodsResult(myProperty, myMethodBuilder);
        }

        [Fact]
        public void TestWithPublicStaticSingleParameterIntReturnTypeMethod()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            Type[] paramTypes = new Type[]
            {
                typeof(int)
            };

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             new Type[0]);
            myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                                         MethodAttributes.Static |
                                                         MethodAttributes.Public,
                                                         typeof(int),
                                                         paramTypes);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ret);

            myPropertyBuilder.AddOtherMethod(myMethodBuilder);
            Type myType = myTypeBuilder.CreateTypeInfo().AsType();
            PropertyInfo myProperty = myType.GetProperty(DynamicPropertyName,
                                                         BindingFlags.Public |
                                                         BindingFlags.NonPublic |
                                                         BindingFlags.Instance |
                                                         BindingFlags.Static);
            VerifyAddMethodsResult(myProperty, myMethodBuilder);
        }

        private void VerifyAddMethodsResult(PropertyInfo myProperty, MethodBuilder expectedMethod)
        {
            MethodInfo[] actualMethods = myProperty.GetAccessors(true);
            Assert.Equal(1, actualMethods.Length);
            Assert.Equal(expectedMethod.Name, actualMethods[0].Name);
        }

        [Fact]
        public void TestThrowsExceptionOnNullMethodBuilder()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             new Type[0]);
            myMethodBuilder = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                myPropertyBuilder.AddOtherMethod(myMethodBuilder);
                Type myType = myTypeBuilder.CreateTypeInfo().AsType();
            });
        }

        [Fact]
        public void TestThrowsExceptionOnCreateTypeCalled()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            Type[] paramTypes = new Type[]
            {
                typeof(int)
            };

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             new Type[0]);
            myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                             MethodAttributes.Public,
                                             CallingConventions.HasThis,
                                             typeof(int),
                                             paramTypes);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldarg_1);
            methodILGenerator.Emit(OpCodes.Ret);
            Type myType = myTypeBuilder.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => { myPropertyBuilder.AddOtherMethod(myMethodBuilder); });
        }
    }
}

