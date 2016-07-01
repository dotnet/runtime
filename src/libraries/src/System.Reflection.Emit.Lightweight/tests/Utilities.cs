// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class Helpers
    {
        public static ModuleBuilder GetModuleBuilder(AssemblyBuilder asmBuild, string moduleName)
        {
            return asmBuild.DefineDynamicModule(moduleName);
        }

        public static void VerifyMethod(DynamicMethod method, string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Module module)
        {
            Assert.Equal(name, method.Name);
            Assert.Equal(attributes, method.Attributes);
            Assert.Equal(callingConvention, method.CallingConvention);

            Assert.Equal(returnType ?? typeof(void), method.ReturnType);
            Assert.Null(method.ReturnParameter);

            ParameterInfo[] parameters = method.GetParameters();
            if (parameterTypes == null)
            {
                Assert.Empty(parameters);
            }
            else
            {
                Assert.Equal(parameterTypes.Length, parameters.Length);
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    ParameterInfo parameter = parameters[i];
                    Assert.Equal(parameterTypes[i], parameter.ParameterType);
                    Assert.Equal(i, parameter.Position);
                }
            }

            Assert.Same(module, method.Module);
            Assert.Null(method.DeclaringType);

            Assert.True(method.InitLocals);
        }

        public static void EmitMethodBody(ILGenerator ilGenerator, FieldInfo field)
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, field);

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, field);

            ilGenerator.Emit(OpCodes.Ret);
        }
    }

    public class TestClass { }
    public class GenericClass<T> { }
    public class GenericClass2<T, U> { }
    public interface TestInterface { }

    public class IDClass
    {
        private int _id;

        public IDClass(int id) { _id = id; }
        public IDClass() : this(0) { }

        public int ID => _id;
    }

    public delegate int IntDelegate(int id);
}
