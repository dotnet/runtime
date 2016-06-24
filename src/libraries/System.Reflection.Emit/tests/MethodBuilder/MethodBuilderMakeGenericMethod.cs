// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderMakeGenericMethod
    {
        public static IEnumerable<object[]> MakeGenericMethod_TestData()
        {
            yield return new object[] { new string[] { "T" }, new Type[] { typeof(string) }, typeof(void) };
            yield return new object[] { new string[] { "T", "U" }, new Type[] { typeof(string), typeof(int) }, typeof(int) };
        }

        [Theory]
        [MemberData(nameof(MakeGenericMethod_TestData))]
        public void MakeGenericMethod(string[] names, Type[] typeArguments, Type returnType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);

            MethodBuilder builder = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static);
            Type[] typeParameters = builder.DefineGenericParameters(names).Select(a => a.AsType()).ToArray();
            builder.SetParameters(typeParameters);
            builder.SetReturnType(returnType);

            MethodInfo methodInfo = builder.MakeGenericMethod(typeArguments);
            VerifyMethodInfo(methodInfo, builder, returnType);
        }

        [Fact]
        public void TestNotThrowsExceptionOnNull()
        {
            Type returnType = typeof(void);
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder builder = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static);
            Type[] typeParameters = builder.DefineGenericParameters(new string[] { "T" }).Select(a => a.AsType()).ToArray();
            builder.SetParameters(typeParameters);
            builder.SetReturnType(returnType);

            MethodInfo methodInfo = builder.MakeGenericMethod(null);
        }

        [Fact]
        public void TestNotThrowsExceptionOnEmptyArray1()
        {
            Type returnType = typeof(void);

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder builder = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static);
            Type[] typeParameters = builder.DefineGenericParameters(new string[] { "T" }).Select(a => a.AsType()).ToArray();
            builder.SetParameters(typeParameters);
            builder.SetReturnType(returnType);

            MethodInfo methodInfo = builder.MakeGenericMethod(new Type[0]);
        }

        [Fact]
        public void TestNotThrowsExceptionOnEmptyArray2()
        {
            Type returnType = typeof(void);

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder builder = type.DefineMethod("Test", MethodAttributes.Public | MethodAttributes.Static);
            Type[] typeParameters = builder.DefineGenericParameters(new string[] { "T" }).Select(a => a.AsType()).ToArray();
            builder.SetParameters(typeParameters);
            builder.SetReturnType(returnType);

            MethodInfo methodInfo = builder.MakeGenericMethod();
        }

        private void VerifyMethodInfo(MethodInfo methodInfo, MethodBuilder builder, Type returnType)
        {
            if (methodInfo == null)
            {
                Assert.Null(builder);
            }
            else
            {
                Assert.Equal(methodInfo.Name, builder.Name);
                Assert.Equal(methodInfo.ReturnType, returnType);
            }
        }
    }
}
