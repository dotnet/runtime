// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderSetReturnType
    {
        [Fact]
        public void SetReturnType_SingleGenericParameter()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);

            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters("T");
            Type returnType = typeParameters[0].AsType();
            method.SetReturnType(returnType);

            type.CreateType();
            VerifyReturnType(method, returnType);
        }

        [Fact]
        public void SetReturnType_MultipleGenericParameters()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);

            string[] typeParamNames = new string[] { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters(typeParamNames);

            Type returnType = typeParameters[1].AsType();
            method.SetReturnType(returnType);

            type.CreateType();
            VerifyReturnType(method, returnType);
        }

        [Fact]
        public void SetReturnType_GenericAndNonGenericParameters()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);

            string[] typeParamNames = new string[] { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters(typeParamNames);

            Type returnType = typeof(void);
            method.SetReturnType(returnType);

            type.CreateType();
            VerifyReturnType(method, returnType);
        }

        [Fact]
        public void SetReturnType_NonGenericMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, typeof(int), new Type[] { typeof(int) });

            Type returnType = typeof(void);
            method.SetReturnType(returnType);

            type.CreateType();
            VerifyReturnType(method, returnType);
        }

        [Fact]
        public void SetReturnType_OverwriteGenericReturnTypeWithNonGenericType()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);

            string[] typeParamNames = new string[] { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters(typeParamNames);

            Type returnType = typeof(void);
            method.SetReturnType(typeParameters[0].AsType());
            method.SetReturnType(returnType);

            type.CreateType();
            VerifyReturnType(method, returnType);
        }

        [Fact]
        public void SetReturnType_OverwriteGenericReturnTypeWithGenericType()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);

            string[] typeParamNames = new string[] { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters(typeParamNames);

            Type returnType = typeParameters[1].AsType();
            method.SetReturnType(typeParameters[0].AsType());
            method.SetReturnType(returnType);

            type.CreateType();
            VerifyReturnType(method, returnType);
        }

        [Fact]
        public void SetReturnType_OverwriteNonGenericReturnTypeWithGenericType()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, typeof(int), new Type[] { typeof(int) });

            string[] typeParamNames = new string[] { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters(typeParamNames);

            Type returnType = typeParameters[0].AsType();
            method.SetReturnType(returnType);

            type.CreateType();
            VerifyReturnType(method, returnType);
        }

        [Fact]
        public void SetReturnType_TypeCreated_Works()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, typeof(int), new Type[] { typeof(int) });

            string[] typeParamNames = new string[] { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters(typeParamNames);

            Type returnType = typeParameters[0].AsType();
            type.CreateType();
            method.SetReturnType(returnType);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void SetReturnType_NullReturnType_ReturnsVoid()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);

            method.SetReturnType(null);
            type.CreateType();
            VerifyReturnType(method, typeof(void));
        }

        private void VerifyReturnType(MethodBuilder method, Type expected)
        {
            MethodInfo methodInfo = method.GetBaseDefinition();
            Type returnType = methodInfo.ReturnType;
            if (expected == null)
            {
                Assert.Null(returnType);
            }
            else
            {
                Assert.Equal(expected.Name, returnType.Name);
                Assert.True(returnType.Equals(expected));
            }
        }
    }
}
