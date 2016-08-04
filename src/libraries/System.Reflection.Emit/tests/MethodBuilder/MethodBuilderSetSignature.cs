// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderSetSignature
    {
        [Fact]
        public void SetSignature_GenericMethod_SingleGenericParameter()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);
            
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters("T");
            GenericTypeParameterBuilder returnType = typeParameters[0];

            method.SetSignature(returnType.AsType(), null, null, null, null, null);
            VerifyMethodSignature(type, method, returnType.AsType());
        }

        [Fact]
        public void SetSignature_GenericMethod_MultipleGenericParameters()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);
            
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters("T", "U");
            GenericTypeParameterBuilder desiredReturnType = typeParameters[1];

            method.SetSignature(desiredReturnType.AsType(), null, null, null, null, null);
            VerifyMethodSignature(type, method, desiredReturnType.AsType());
        }

        [Fact]
        public void SetSignature_GenericMethod_ReturnType_RequiredCustomModifiers()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);
            
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters("T");
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];

            method.SetSignature(desiredReturnType.AsType(), null, null, null, null, null);
            VerifyMethodSignature(type, method, desiredReturnType.AsType());
        }

        [Fact]
        public void SetSignature_GenericMethod_ReturnType_RequiredModifier_OptionalCustomModifiers()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);
            
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters("T");
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];

            method.SetSignature(desiredReturnType.AsType(), null, null, null, null, null);
            VerifyMethodSignature(type, method, desiredReturnType.AsType());
        }

        [Fact]
        public void SetSignature_GenericMethod_ReturnType_OptionalCustomModifiers()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);
            
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters("T");
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];

            method.SetSignature(desiredReturnType.AsType(), null, null, null, null, null);
            VerifyMethodSignature(type, method, desiredReturnType.AsType());
        }

        [Fact]
        public void SetSignature_GenericMethod_ParameterTypes()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);
            
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters("T");
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];
            Type[] desiredParamType = new Type[] { typeof(int) };

            method.SetSignature(desiredReturnType.AsType(), null, null, desiredParamType, null, null);
            VerifyMethodSignature(type, method, desiredReturnType.AsType());
        }

        [Fact]
        public void SetSignature_GenericMethod_MultipleParameters()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);
            
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters("T", "U");
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];
            Type[] desiredParamType = new Type[] { typeof(int), typeParameters[1].AsType() };

            method.SetSignature(desiredReturnType.AsType(), null, null, desiredParamType, null, null);
            VerifyMethodSignature(type, method, desiredReturnType.AsType());
        }

        [Fact]
        public void SetSignature_GenericMethod_ParameterType_RequiredCustomModifiers()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);

            string[] typeParamNames = new string[] { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                method.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];
            Type[] desiredParamType = new Type[] { typeof(int) };
            Type[][] parameterTypeRequiredCustomModifiers = new Type[desiredParamType.Length][];
            for (int i = 0; i < desiredParamType.Length; ++i)
            {
                parameterTypeRequiredCustomModifiers[i] = null;
            }

            method.SetSignature(desiredReturnType.AsType(), null, null, desiredParamType, parameterTypeRequiredCustomModifiers, null);

            VerifyMethodSignature(type, method, desiredReturnType.AsType());
        }

        [Fact]
        public void SetSignature_GenericMethod_ParameterType_RequiredCustomModifer_OptionalCustomModifiers()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);
            
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters("T");
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];
            Type[] desiredParamType = new Type[] { typeof(int) };
            Type[][] parameterTypeRequiredCustomModifiers = new Type[desiredParamType.Length][];
            Type[][] parameterTypeOptionalCustomModifiers = new Type[desiredParamType.Length][];
            for (int i = 0; i < desiredParamType.Length; ++i)
            {
                parameterTypeRequiredCustomModifiers[i] = null;
                parameterTypeOptionalCustomModifiers[i] = null;
            }

            method.SetSignature(desiredReturnType.AsType(), null, null, desiredParamType, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
            VerifyMethodSignature(type, method, desiredReturnType.AsType());
        }

        [Fact]
        public void SetSignature_GenericMethod_ParameterType_OptionalCustomModifiers()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);

            GenericTypeParameterBuilder[] typeParameters =
                method.DefineGenericParameters("T");
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];
            Type[] desiredParamType = new Type[] { typeof(int) };
            Type[][] parameterTypeOptionalCustomModifiers = new Type[desiredParamType.Length][];
            for (int i = 0; i < desiredParamType.Length; ++i)
            {
                parameterTypeOptionalCustomModifiers[i] = null;
            }

            method.SetSignature(desiredReturnType.AsType(), null, null, desiredParamType, null, parameterTypeOptionalCustomModifiers);
            VerifyMethodSignature(type, method, desiredReturnType.AsType());
        }

        [Fact]
        public void SetSignature_NonGenericMethod()
        {
            Type[] parameterTypes = new Type[] { typeof(string), typeof(object) };
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, typeof(void), parameterTypes);
            string[] parameterNames = new string[parameterTypes.Length];
            for (int i = 0; i < parameterNames.Length; ++i)
            {
                parameterNames[i] = "P" + i.ToString();
                method.DefineParameter(i + 1, ParameterAttributes.In, parameterNames[i]);
            }

            Type desiredReturnType = typeof(void);
            Type[] desiredParamType = new Type[] { typeof(int) };
            Type[][] parameterTypeRequiredCustomModifiers = new Type[desiredParamType.Length][];
            Type[][] parameterTypeOptionalCustomModifiers = new Type[desiredParamType.Length][];
            for (int i = 0; i < desiredParamType.Length; ++i)
            {
                parameterTypeRequiredCustomModifiers[i] = null;
                parameterTypeOptionalCustomModifiers[i] = null;
            }

            method.SetSignature(desiredReturnType, null, null, desiredParamType, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
        }

        [Fact]
        public void SetSignature_AllParametersNull()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);

            method.SetSignature(null, null, null, null, null, null);
            VerifyMethodSignature(type, method, null);
        }

        [Fact]
        public void SetSignature_NullReturnType_CustomModifiersSetToWrongTypes()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);
            
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters("T");
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];

            method.SetSignature(null, null, null, null, null, null);
            VerifyMethodSignature(type, method, null);
        }

        [Fact]
        public void SetSignature_NullOnReturnType_CustomModifiersSetCorrectly()
        {
            int arraySize = 10;
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);
            
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters("T");
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];
            Type[] desiredParamType = null;
            Type[][] parameterTypeRequiredCustomModifiers = new Type[arraySize][];
            Type[][] parameterTypeOptionalCustomModifiers = new Type[arraySize][];
            for (int i = 0; i < arraySize; ++i)
            {
                parameterTypeRequiredCustomModifiers[i] = null;
                parameterTypeOptionalCustomModifiers[i] = null;
            }

            method.SetSignature(desiredReturnType.AsType(), null, null, desiredParamType, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
        }

        [Fact]
        public void SetSignature_NullReturnType_RequiredCustomModifiers_OptionalCustomModifiers()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);
            
            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters("T");
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];
            Type[] desiredParamType = new Type[] { typeof(void) };
            Type[][] parameterTypeRequiredCustomModifiers = new Type[desiredParamType.Length][];
            Type[][] parameterTypeOptionalCustomModifiers = new Type[desiredParamType.Length][];
            for (int i = 0; i < desiredParamType.Length; ++i)
            {
                parameterTypeRequiredCustomModifiers[i] = null;
                parameterTypeOptionalCustomModifiers[i] = null;
            }

            method.SetSignature(desiredReturnType.AsType(), null, null, desiredParamType, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
        }

        private void VerifyMethodSignature(TypeBuilder type, MethodBuilder method, Type desiredReturnType)
        {
            Type ret = type.CreateTypeInfo().AsType();
            MethodInfo methodInfo = method.GetBaseDefinition();
            Type actualReturnType = methodInfo.ReturnType;

            if (desiredReturnType == null)
            {
                Assert.Null(actualReturnType);
            }
            else
            {
                Assert.NotNull(actualReturnType);
                Assert.Equal(desiredReturnType.Name, actualReturnType.Name);
                Assert.True(actualReturnType.Equals(desiredReturnType));
            }
        }
    }
}
