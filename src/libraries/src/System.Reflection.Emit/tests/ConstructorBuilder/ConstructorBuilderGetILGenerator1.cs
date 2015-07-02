// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ConstructorBuilderGetILGenerator1
    {
        private const string AssemblyName = "ConstructorBuilderGetILGenerator1";
        private const string DefaultModuleName = "DynamicModule";
        private const string DefaultTypeName = "DynamicType";
        private const AssemblyBuilderAccess DefaultAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const CallingConventions DefaultCallingConvention = CallingConventions.Standard;

        private ModuleBuilder TestModuleBuilder
        {
            get
            {
                AssemblyName name = new AssemblyName(AssemblyName);
                AssemblyBuilder assembly =
                    AssemblyBuilder.DefineDynamicAssembly(name, DefaultAssemblyBuilderAccess);
                return TestLibrary.Utilities.GetModuleBuilder(assembly, "Module1");
            }
        }

        [Fact]
        public void TestILGeneratorOnNonDefaultConstructor()
        {
            int i = 0;
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
                    CreateConstructorBuilder("PosTest1_Type" + i, attributes[i]).GetILGenerator();
                Assert.NotNull(generator);
            }
        }

        [Fact]
        public void TestThrowsExceptionWithNoMethodyBody()
        {
            // InvalidOperationException should be thrown when The constructor has MethodAttributes or MethodImplAttributes flags indicating that it should not have a method body.
            Assert.Throws<InvalidOperationException>(() =>
            {
                ILGenerator generator = CreateConstructorBuilder("NegTest1_Type1", MethodAttributes.PinvokeImpl).GetILGenerator();
            });
        }

        [Fact]
        public void TestThrowsExceptionOnDefaultConstructor()
        {
            TypeBuilder type = TestModuleBuilder.DefineType("NegTest2_Type1");

            ConstructorBuilder constructor = type.DefineDefaultConstructor(MethodAttributes.Public);
            Assert.Throws<InvalidOperationException>(() => { constructor.GetILGenerator(); });
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
