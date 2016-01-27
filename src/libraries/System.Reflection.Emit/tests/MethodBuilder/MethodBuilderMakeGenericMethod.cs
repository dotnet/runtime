// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderMakeGenericMethod
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const TypeAttributes TestTypeAttributes = TypeAttributes.Abstract;
        private const MethodAttributes TestMethodAttributes = MethodAttributes.Public | MethodAttributes.Static;
        private const int MinStringLength = 1;
        private const int MaxStringLength = 128;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        private TypeBuilder GetTestTypeBuilder()
        {
            AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName, TestAssemblyBuilderAccess);

            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestDynamicModuleName);
            return moduleBuilder.DefineType(TestDynamicTypeName, TestTypeAttributes);
        }

        [Fact]
        public void TestWithSingleGenericParameter()
        {
            string methodName = null;

            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type returnType = typeof(void);
            Type[] desiredType = new Type[] {
                typeof(string)
            };

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);
            Type[] typeParameters =
                builder.DefineGenericParameters(new string[] { "T" }).Select(a => a.AsType()).ToArray();
            builder.SetParameters(typeParameters);
            builder.SetReturnType(returnType);
            MethodInfo methodInfo = builder.MakeGenericMethod(desiredType);
            VerifyMethodInfo(methodInfo, builder, returnType);
        }

        [Fact]
        public void TestWithMultipleGenericParameters()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type returnType = typeof(int);
            Type[] desiredType = new Type[] {
                typeof(string),
                typeof(int)
            };

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);
            Type[] typeParameters =
                builder.DefineGenericParameters(new string[] { "T", "U" }).Select(a => a.AsType()).ToArray();
            builder.SetParameters(typeParameters);
            builder.SetReturnType(returnType);
            MethodInfo methodInfo = builder.MakeGenericMethod(desiredType);
            VerifyMethodInfo(methodInfo, builder, returnType);
        }

        [Fact]
        public void TestNotThrowsExceptionOnNull()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type returnType = typeof(void);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);
            Type[] typeParameters =
                builder.DefineGenericParameters(new string[] { "T" }).Select(a => a.AsType()).ToArray();
            builder.SetParameters(typeParameters);
            builder.SetReturnType(returnType);

            MethodInfo methodInfo = builder.MakeGenericMethod(null);
        }

        [Fact]
        public void TestNotThrowsExceptionOnEmptyArray1()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type returnType = typeof(void);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);
            Type[] typeParameters =
                builder.DefineGenericParameters(new string[] { "T" }).Select(a => a.AsType()).ToArray();
            builder.SetParameters(typeParameters);
            builder.SetReturnType(returnType);

            MethodInfo methodInfo = builder.MakeGenericMethod(new Type[] { });
        }

        [Fact]
        public void TestNotThrowsExceptionOnEmptyArray2()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type returnType = typeof(void);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);
            Type[] typeParameters =
                builder.DefineGenericParameters(new string[] { "T" }).Select(a => a.AsType()).ToArray();
            builder.SetParameters(typeParameters);
            builder.SetReturnType(returnType);

            MethodInfo methodInfo = builder.MakeGenericMethod();
        }

        private void VerifyMethodInfo(MethodInfo methodInfo, MethodBuilder builder, Type returnType)
        {
            if (methodInfo == null)
                Assert.Null(builder);

            if (methodInfo != null)
            {
                Assert.NotNull(builder);
                Assert.Equal(methodInfo.Name, builder.Name);
                Assert.True(methodInfo.ReturnType.Equals(returnType));
            }
        }
    }
}
