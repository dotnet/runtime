// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderGetGenericArguments
    {
        [Fact]
        public void GetGenericArguments_NonGenericMethod_ReturnsNull()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod1", MethodAttributes.Public);

            VerifyGenericArguments(method, null);
        }

        [Fact]
        public void GetGenericArguments_GenericMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod2", MethodAttributes.Public);
            GenericTypeParameterBuilder[] genericParams = method.DefineGenericParameters("T", "U");

            VerifyGenericArguments(method, genericParams);
        }

        [Fact]
        public void GetGenericArguments_GenericMethod_GenericParameters()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod4", MethodAttributes.Public);
            GenericTypeParameterBuilder[] genericParams = method.DefineGenericParameters("T", "U");
            method.SetReturnType(genericParams[0].AsType());

            VerifyGenericArguments(method, genericParams);
        }
        
        [Fact]
        public void GetGenericArguments_SingleParameters()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod3", MethodAttributes.Public, typeof(void), new Type[] { typeof(int) });
            GenericTypeParameterBuilder[] genericParams = method.DefineGenericParameters("T");
            method.DefineParameter(1, ParameterAttributes.HasDefault, "TestParam");
            VerifyGenericArguments(method, genericParams);
        }

        private static void VerifyGenericArguments(MethodBuilder method, GenericTypeParameterBuilder[] expected)
        {
            Type[] genericArguments = method.GetGenericArguments();
            if (expected == null)
            {
                Assert.Null(genericArguments);
            }
            else
            {
                Assert.Equal(expected.Length, genericArguments.Length);
                for (int i = 0; i < genericArguments.Length; ++i)
                {
                    Assert.True(expected[i].Equals(genericArguments[i]));
                }
            }
        }
    }
}
