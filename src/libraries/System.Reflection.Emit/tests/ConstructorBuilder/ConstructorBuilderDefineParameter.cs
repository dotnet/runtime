// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ConstructorBuilderDefineParameter
    {
        private static readonly ParameterAttributes[] s_supportedAttributes = new ParameterAttributes[]
        {
            ParameterAttributes.None,
            ParameterAttributes.HasDefault,
            ParameterAttributes.HasFieldMarshal,
            ParameterAttributes.In,
            ParameterAttributes.None,
            ParameterAttributes.Optional,
            ParameterAttributes.Out,
            ParameterAttributes.Retval
        };

        [Fact]
        public void DefineParameter_MultipleParameters()
        {
            Type[] parameterTypes = new Type[] { typeof(int), typeof(string) };

            for (int i = 1; i < s_supportedAttributes.Length; i++)
            {
                TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
                ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);

                constructor.DefineParameter(1, s_supportedAttributes[i - 1], "parameter1" + i);
                constructor.DefineParameter(2, s_supportedAttributes[i], "parameter2" + i);

                ILGenerator ilGenerator = constructor.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));
                ilGenerator.Emit(OpCodes.Ret);

                type.CreateTypeInfo().AsType();

                ParameterInfo[] definedParams = constructor.GetParameters();
                Assert.Equal(2, definedParams.Length);
            }
        }

        [Fact]
        public void DefineParameter_SingleParameter()
        {
            Type[] parameterTypes = new Type[] { typeof(ConstructorBuilderDefineParameter) };

            for (int i = 0; i < s_supportedAttributes.Length; i++)
            {
                TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
                ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);

                constructor.DefineParameter(1, s_supportedAttributes[i], "parameter1" + i);

                ILGenerator ilGenerator = constructor.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));
                ilGenerator.Emit(OpCodes.Ret);

                type.CreateTypeInfo().AsType();

                ParameterInfo[] definedParams = constructor.GetParameters();
                Assert.Equal(1, definedParams.Length);
            }
        }

        [Fact]
        public void DefineParameter_SingleParameter_NullParameterName()
        {
            Type[] parameterTypes = new Type[] { typeof(ConstructorBuilderDefineParameter) };

            for (int i = 0; i < s_supportedAttributes.Length; i++)
            {
                TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
                ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);

                constructor.DefineParameter(1, s_supportedAttributes[i], null);

                ILGenerator ilGenerator = constructor.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));
                ilGenerator.Emit(OpCodes.Ret);

                type.CreateTypeInfo().AsType();

                ParameterInfo[] definedParams = constructor.GetParameters();
                Assert.Equal(1, definedParams.Length);
            }
        }

        [Fact]
        public void DefineParameter_MultipleParameters_NullParameterNames()
        {
            Type[] parameterTypes = new Type[] { typeof(int), typeof(string) };

            for (int i = 1; i < s_supportedAttributes.Length; ++i)
            {
                TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
                ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);

                constructor.DefineParameter(1, s_supportedAttributes[i - 1], null);
                constructor.DefineParameter(2, s_supportedAttributes[i], null);

                ILGenerator ilGenerator = constructor.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));
                ilGenerator.Emit(OpCodes.Ret);

                type.CreateTypeInfo().AsType();

                ParameterInfo[] definedParams = constructor.GetParameters();
                Assert.Equal(2, definedParams.Length);
            }
        }

        [Fact]
        public void DefineParameter_ZeroSequence_NoParameters()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Any, new Type[0]);
            Assert.NotNull(constructor.DefineParameter(0, ParameterAttributes.None, "p"));
        }

        [Fact]
        public void DefineParameter_ZeroSequence_SingleParameter()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Any, new Type[0]);
            Assert.NotNull(constructor.DefineParameter(0, ParameterAttributes.None, "p"));
        }

        [Fact]
        public void DefineParameter_NoParameters_InvalidSequence_ThrowsArgumentOutOfRangeException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);

            Assert.Throws<ArgumentOutOfRangeException>(() => constructor.DefineParameter(-1, ParameterAttributes.None, "p"));
            Assert.Throws<ArgumentOutOfRangeException>(() => constructor.DefineParameter(1, ParameterAttributes.None, "p"));
        }

        [Fact]
        public void DefineParameter_SingleParameter_InvalidSequence_ThrowsArgumentOutOfRangeException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(int) });

            Assert.Throws<ArgumentOutOfRangeException>(() => constructor.DefineParameter(2, ParameterAttributes.None, "p"));
        }

        [Fact]
        public void DefineParameter_TypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(int) });

            constructor.GetILGenerator().Emit(OpCodes.Ret);
            type.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => constructor.DefineParameter(1, ParameterAttributes.None, "p"));
        }
    }
}
