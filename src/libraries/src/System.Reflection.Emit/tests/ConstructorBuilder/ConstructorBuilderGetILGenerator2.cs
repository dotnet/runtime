// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ConstructorBuilderGetILGenerator2
    {
        private const string AssemblyName = "ConstructorBuilderGetILGenerator2";
        private const string DefaultModuleName = "DynamicModule";
        private const string DefaultTypeName = "DynamicType";
        private const AssemblyBuilderAccess DefaultAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const CallingConventions DefaultCallingConvention = CallingConventions.Standard;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        private ModuleBuilder TestModuleBuilder
        {
            get
            {
                AssemblyName name = new AssemblyName(AssemblyName);
                AssemblyBuilder assembly =
                    AssemblyBuilder.DefineDynamicAssembly(name, DefaultAssemblyBuilderAccess);
                _testModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(assembly, "Module1");


                return _testModuleBuilder;
            }
        }

        private ModuleBuilder _testModuleBuilder;

        [Fact]
        public void TestIlGeneratorOnNonDefaultConstructor()
        {
            int i = 0;
            int randValue = 0;
            randValue = _generator.GetInt16();
            MethodAttributes[] attributes = new MethodAttributes[] {
                MethodAttributes.Assembly,
                MethodAttributes.CheckAccessOnOverride,
                MethodAttributes.FamANDAssem,
                MethodAttributes.Family,
                MethodAttributes.FamORAssem,
                MethodAttributes.Final,
                MethodAttributes.HasSecurity,
                MethodAttributes.HideBySig,
                MethodAttributes.MemberAccessMask,
                MethodAttributes.NewSlot,
                MethodAttributes.Private,
                MethodAttributes.PrivateScope,
                MethodAttributes.Public,
                MethodAttributes.RequireSecObject,
                MethodAttributes.ReuseSlot,
                MethodAttributes.RTSpecialName,
                MethodAttributes.SpecialName,
                MethodAttributes.Static,
                MethodAttributes.UnmanagedExport,
                MethodAttributes.Virtual,
                MethodAttributes.VtableLayoutMask
            };

            for (; i < attributes.Length; ++i)
            {
                ILGenerator generator =
                    CreateConstructorBuilder("PosTest1_Type" + i, attributes[i]).GetILGenerator(randValue);
                Assert.NotNull(generator);
            }
        }

        [Fact]
        public void TestThrowsExceptionWithNoMethodyBody()
        {
            int randValue = 0;
            randValue = _generator.GetInt16();
            Assert.Throws<InvalidOperationException>(() =>
            {
                ILGenerator generator = CreateConstructorBuilder("NegTest1_Type1", MethodAttributes.PinvokeImpl).GetILGenerator(randValue);
            });
        }

        [Fact]
        public void TestThrowsExceptionOnDefaultConstructor()
        {
            int randValue = 0;
            randValue = _generator.GetInt16();
            TypeBuilder type = TestModuleBuilder.DefineType("NegTest2_Type1");

            Assert.Throws<InvalidOperationException>(() =>
            {
                ConstructorBuilder constructor = type.DefineDefaultConstructor(MethodAttributes.Public);
                constructor.GetILGenerator(randValue);
            });
        }

        private ConstructorBuilder CreateConstructorBuilder(string typeName, MethodAttributes attribute)
        {
            TypeBuilder type = TestModuleBuilder.DefineType(typeName);

            return type.DefineConstructor(
                attribute,
                DefaultCallingConvention,
                new Type[] { });
        }
    }
}
