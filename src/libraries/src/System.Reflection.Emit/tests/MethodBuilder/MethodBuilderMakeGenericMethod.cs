// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        private TypeBuilder TestTypeBuilder
        {
            get
            {
                if (null == _testTypeBuilder)
                {
                    AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
                    AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                        assemblyName, TestAssemblyBuilderAccess);

                    ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestDynamicModuleName);


                    _testTypeBuilder = moduleBuilder.DefineType(TestDynamicTypeName, TestTypeAttributes);
                }

                return _testTypeBuilder;
            }
        }

        private TypeBuilder _testTypeBuilder;

        [Fact]
        public void PosTest1()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type returnType = typeof(void);
            Type[] desiredType = new Type[] {
                typeof(string)
            };

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);
            Type[] typeParameters =
                builder.DefineGenericParameters(new string[] { "T" }).Select(a => a.AsType()).ToArray();
            builder.SetParameters(typeParameters);
            builder.SetReturnType(returnType);
            MethodInfo methodInfo = builder.MakeGenericMethod(desiredType);
            VerifyMethodInfo(methodInfo, builder, returnType);
        }

        [Fact]
        public void PosTest2()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type returnType = typeof(int);
            Type[] desiredType = new Type[] {
                typeof(string),
                typeof(int)
            };

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);
            Type[] typeParameters =
                builder.DefineGenericParameters(new string[] { "T", "U" }).Select(a => a.AsType()).ToArray();
            builder.SetParameters(typeParameters);
            builder.SetReturnType(returnType);
            MethodInfo methodInfo = builder.MakeGenericMethod(desiredType);
            VerifyMethodInfo(methodInfo, builder, returnType);
        }

        [Fact]
        public void NegTest1()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type returnType = typeof(void);

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);
            Type[] typeParameters =
                builder.DefineGenericParameters(new string[] { "T" }).Select(a => a.AsType()).ToArray();
            builder.SetParameters(typeParameters);
            builder.SetReturnType(returnType);

            MethodInfo methodInfo = builder.MakeGenericMethod(null);
        }

        [Fact]
        public void NegTest2()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type returnType = typeof(void);

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);
            Type[] typeParameters =
                builder.DefineGenericParameters(new string[] { "T" }).Select(a => a.AsType()).ToArray();
            builder.SetParameters(typeParameters);
            builder.SetReturnType(returnType);

            MethodInfo methodInfo = builder.MakeGenericMethod(new Type[] { });
        }

        [Fact]
        public void NegTest3()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type returnType = typeof(void);

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
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
