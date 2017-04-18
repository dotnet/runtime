// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderSetParameters
    {
        public static IEnumerable<object[]> SetParameters_TestData()
        {
            yield return new object[] { new Type[0], new string[] { "T" } };
            yield return new object[] { new Type[0], new string[] { "T", "U" } };
            yield return new object[] { new Type[] { typeof(int) }, new string[] { "T" } };
        }

        [Theory]
        [MemberData(nameof(SetParameters_TestData))]
        public void SetParameters(Type[] parameterTypes, string[] typeParamNames)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public, typeof(void), parameterTypes);
            
            Type[] typeParameters = method.DefineGenericParameters(typeParamNames).Select(a => a.AsType()).ToArray();
            method.SetParameters(typeParameters);

            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo().AsType();
            MethodInfo createdMethod = createdType.GetMethod(method.Name);
            VerifyParameters(createdMethod.GetParameters(), typeParameters, typeParamNames);
        }

        [Fact]
        public void SetParameters_WorksAfterTypeCreated()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);
            Type[] typeParameters = method.DefineGenericParameters("T").Select(a => a.AsType()).ToArray();

            method.SetParameters(typeParameters);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            type.CreateTypeInfo().AsType();
            method.SetParameters(typeParameters);
        }

        [Fact]
        public void SetParameters_NullParameterTypes()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);

            method.SetParameters(null);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo().AsType();
            MethodInfo createdMethod = createdType.GetMethod(method.Name);
            ParameterInfo[] parameters = createdMethod.GetParameters();
            VerifyParameters(parameters, new Type[0], null);
        }

        [Fact]
        public void SetParameters_EmptyParameterTypes()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder builder = type.DefineMethod("TestMethod", MethodAttributes.Public);

            builder.SetParameters(new Type[0]);
            ILGenerator ilGenerator = builder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo().AsType();

            MethodInfo method = createdType.GetMethod(builder.Name);
            ParameterInfo[] parameters = builder.GetParameters();
            VerifyParameters(parameters, new Type[0], null);
        }

        [Fact]
        public void SetParameters_NoParameterTypes()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder builder = type.DefineMethod("TestMethod", MethodAttributes.Public);

            builder.SetParameters();
            ILGenerator ilGenerator = builder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo().AsType();

            MethodInfo method = createdType.GetMethod(builder.Name);
            ParameterInfo[] parameters = method.GetParameters();
            VerifyParameters(parameters, new Type[0], null);
        }

        [Fact]
        public void SetParameters_NullParameter_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder builder = type.DefineMethod("TestMethod", MethodAttributes.Public, typeof(void), new Type[] { typeof(int) });
            Type[] typeParameters = builder.DefineGenericParameters("T").Select(a => a.AsType()).ToArray();

            Type[] parameterTypes = new Type[typeParameters.Length + 1];
            for (int i = 0; i < typeParameters.Length; ++i)
            {
                parameterTypes[i] = typeParameters[i];
            }
            parameterTypes[typeParameters.Length] = null;

            builder.SetParameters(parameterTypes);
            ILGenerator ilGenerator = builder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            AssertExtensions.Throws<ArgumentNullException>("argument", () => type.CreateTypeInfo().AsType());
        }

        private void VerifyParameters(ParameterInfo[] parameters, Type[] parameterTypes, string[] parameterName)
        {
            if (parameterTypes == null)
            {
                Assert.Null(parameters);
            }
            else
            {
                Assert.NotNull(parameters);
                for (int i = 0; i < parameters.Length; ++i)
                {
                    ParameterInfo parameter = parameters[i];
                    if (parameter.Name != null)
                    {
                        Assert.Equal(parameterName[i], parameter.Name);
                    }
                    else
                    {
                        Assert.Equal(parameterName[i], parameter.ParameterType.Name);
                    }

                    Assert.Equal(i, parameter.Position);
                }
            }
        }
    }
}
