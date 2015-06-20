// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderAttributes
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const TypeAttributes TestTypeAttributes = TypeAttributes.Abstract;
        private const int MinStringLength = 1;
        private const int MaxStringLength = 128;

        private TypeBuilder TestTypeBuilder
        {
            get
            {
                if (null == _testTypeBuilder)
                {
                    AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
                    AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                        assemblyName, TestAssemblyBuilderAccess);

                    ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestDynamicModuleName);
                    _testTypeBuilder = moduleBuilder.DefineType(TestDynamicTypeName, TestTypeAttributes);
                }

                return _testTypeBuilder;
            }
        }

        private TypeBuilder _testTypeBuilder;

        [Fact]
        public void PosTest1()
        {
            PosTestHelp(MethodAttributes.Abstract);
            PosTestHelp(MethodAttributes.Assembly);
            PosTestHelp(MethodAttributes.CheckAccessOnOverride);
            PosTestHelp(MethodAttributes.FamANDAssem);
            PosTestHelp(MethodAttributes.Family);
            PosTestHelp(MethodAttributes.FamORAssem);
            PosTestHelp(MethodAttributes.Final);
            PosTestHelp(MethodAttributes.HasSecurity);
            PosTestHelp(MethodAttributes.HideBySig);
            PosTestHelp(MethodAttributes.MemberAccessMask);
            PosTestHelp(MethodAttributes.NewSlot);
            PosTestHelp(MethodAttributes.PinvokeImpl);
            PosTestHelp(MethodAttributes.Private);
            PosTestHelp(MethodAttributes.PrivateScope);
            PosTestHelp(MethodAttributes.Public);
            PosTestHelp(MethodAttributes.RequireSecObject);
            PosTestHelp(MethodAttributes.ReuseSlot);
            PosTestHelp(MethodAttributes.RTSpecialName);
            PosTestHelp(MethodAttributes.SpecialName);
            PosTestHelp(MethodAttributes.Static);
            PosTestHelp(MethodAttributes.UnmanagedExport);
            PosTestHelp(MethodAttributes.Virtual);
            PosTestHelp(MethodAttributes.VtableLayoutMask);
        }

        [Fact]
        public void PosTest2()
        {
            PosTestHelp(
                MethodAttributes.Abstract |
                MethodAttributes.Public |
                MethodAttributes.NewSlot |
                MethodAttributes.Virtual);
            PosTestHelp(
                MethodAttributes.Final |
                MethodAttributes.Private |
                MethodAttributes.SpecialName |
                MethodAttributes.Static);
        }

        private void PosTestHelp(MethodAttributes desiredAttribute)
        {
            string methodName = null;
            MethodAttributes actualAttribute = (MethodAttributes)(-1);
            methodName = TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                desiredAttribute);
            actualAttribute = builder.Attributes;

            Assert.Equal(desiredAttribute, actualAttribute);
        }
    }
}
