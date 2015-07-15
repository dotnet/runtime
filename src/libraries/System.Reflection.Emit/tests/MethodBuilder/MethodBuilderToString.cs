// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderToString1
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const TypeAttributes TestTypeAttributes = TypeAttributes.Abstract;
        private const MethodAttributes TestMethodAttributes = MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual;
        private const int MinStringLength = 1;
        private const int MaxStringLength = 128;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();
        private readonly byte[] _defaultILArray = new byte[]  {
            0x00,
            0x72,
            0x01,
            0x00,
            0x00,
            0x70,
            0x28,
            0x04,
            0x00,
            0x00,
            0x0a,
            0x00,
            0x2a
        };

        private TypeBuilder GetTestTypeBuilder()
        {
            AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                            assemblyName, TestAssemblyBuilderAccess);
            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, "Module1");
            return moduleBuilder.DefineType(TestDynamicTypeName, TestTypeAttributes);
        }

        [Fact]
        public void TestWithAllFieldsSet()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            ILGenerator ilgen = builder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);
            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];

            builder.SetSignature(desiredReturnType.AsType(), null, null, null, null, null);
            string actualString = builder.ToString();
            string desiredString = GetDesiredMethodToString(builder);

            Assert.NotNull(actualString);
            Assert.Contains(desiredString, actualString);
        }

        [Fact]
        public void TestWithNameAndAttributeSet()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName, MethodAttributes.Public);

            string actualString = builder.ToString();
            string desiredString = GetDesiredMethodToString(builder);

            Assert.NotNull(actualString);
            Assert.Contains(desiredString, actualString);
        }

        [Fact]
        public void TestWithNameAttributeAndSignatureSet()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetSignature(typeof(void), null, null, null, null, null);
            string actualString = builder.ToString();
            string desiredString = GetDesiredMethodToString(builder);

            Assert.NotNull(actualString);
            Assert.Contains(desiredString, actualString);
        }

        private string GetDesiredMethodToString(MethodBuilder builder)
        {
            // Avoid use string.Format or StringBuilder
            return "Name: " + builder.Name + " " + Environment.NewLine +
                "Attributes: " + ((int)builder.Attributes).ToString() + Environment.NewLine +
                "Method Signature: ";
        }
    }
}
