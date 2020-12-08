// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderGetGenericArguments
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void GetGenericArguments_NonGenericMethod_ReturnsEmptyArray()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("Name", MethodAttributes.Public);
            Assert.Equal(Type.EmptyTypes, method.GetGenericArguments());
        }

        [Fact]
        public void GetGenericArguments_GenericMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("Name", MethodAttributes.Public);
            GenericTypeParameterBuilder[] genericParams = method.DefineGenericParameters("T", "U");

            VerifyGenericArguments(method, genericParams);
        }

        [Fact]
        public void GetGenericArguments_GenericMethod_GenericParameters()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("Name", MethodAttributes.Public);
            GenericTypeParameterBuilder[] genericParams = method.DefineGenericParameters("T", "U");
            method.SetReturnType(genericParams[0].AsType());

            VerifyGenericArguments(method, genericParams);
        }

        [Fact]
        public void GetGenericArguments_SingleParameters()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("Name", MethodAttributes.Public, typeof(void), new Type[] { typeof(int) });
            GenericTypeParameterBuilder[] genericParams = method.DefineGenericParameters("T");
            method.DefineParameter(1, ParameterAttributes.HasDefault, "Parameter");
            VerifyGenericArguments(method, genericParams);
        }

        private static void VerifyGenericArguments(MethodBuilder method, GenericTypeParameterBuilder[] expected)
        {
            Type[] genericArguments = method.GetGenericArguments();
            Assert.Equal(expected.Select(p => p.AsType()), genericArguments);
        }
    }
}
