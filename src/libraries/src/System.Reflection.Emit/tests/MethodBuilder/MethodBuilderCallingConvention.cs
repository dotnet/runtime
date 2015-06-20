// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderCallingConvention
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
            PosTestStaticHelp(CallingConventions.Any);
            PosTestStaticHelp(CallingConventions.ExplicitThis);
            PosTestStaticHelp(CallingConventions.HasThis);
            PosTestStaticHelp(CallingConventions.Standard);
            PosTestStaticHelp(CallingConventions.VarArgs);
        }

        [Fact]
        public void PosTest2()
        {
            PosTestStaticHelp(CallingConventions.Any | CallingConventions.Standard);
            PosTestStaticHelp(CallingConventions.Any | CallingConventions.VarArgs);
            PosTestStaticHelp(CallingConventions.HasThis | CallingConventions.Standard);
            PosTestStaticHelp(CallingConventions.HasThis | CallingConventions.ExplicitThis);
        }

        [Fact]
        public void PosTest3()
        {
            PosTestInstanceHelp(CallingConventions.Any);
            PosTestInstanceHelp(CallingConventions.ExplicitThis);
            PosTestInstanceHelp(CallingConventions.HasThis);
            PosTestInstanceHelp(CallingConventions.Standard);
            PosTestInstanceHelp(CallingConventions.VarArgs);
        }

        [Fact]
        public void PosTest4()
        {
            PosTestInstanceHelp(CallingConventions.Any | CallingConventions.Standard);
            PosTestInstanceHelp(CallingConventions.Any | CallingConventions.VarArgs);
            PosTestInstanceHelp(CallingConventions.HasThis | CallingConventions.Standard);
            PosTestInstanceHelp(CallingConventions.HasThis | CallingConventions.ExplicitThis);
        }

        [Fact]
        public void NegTest1()
        {
            string methodName = null;
            CallingConventions actualCallingConventions = (CallingConventions)0;
            CallingConventions desiredCallingConventions = (CallingConventions)(-1);
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public,
                desiredCallingConventions);
            actualCallingConventions = builder.CallingConvention;
            // Instance method should have HasThis calling convention
            desiredCallingConventions |= desiredCallingConventions | CallingConventions.HasThis;

            Assert.Equal(desiredCallingConventions, actualCallingConventions);
        }

        [Fact]
        public void NegTest2()
        {
            string methodName = null;
            CallingConventions actualCallingConventions = (CallingConventions)0;
            CallingConventions desiredCallingConventions = (CallingConventions)(-1);

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public,
                desiredCallingConventions);
            actualCallingConventions = builder.CallingConvention;

            Assert.Equal(desiredCallingConventions, actualCallingConventions);
        }

        private void PosTestStaticHelp(CallingConventions desiredCallingConventions)
        {
            string methodName = null;
            CallingConventions actualCallingConventions = (CallingConventions)(-1);

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Static,
                desiredCallingConventions);
            actualCallingConventions = builder.CallingConvention;

            Assert.Equal(desiredCallingConventions, actualCallingConventions);
        }

        private void PosTestInstanceHelp(CallingConventions desiredCallingConventions)
        {
            string methodName = null;
            CallingConventions actualCallingConventions = (CallingConventions)(-1);

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public,
                desiredCallingConventions);
            actualCallingConventions = builder.CallingConvention;
            // Instance method should have HasThis calling convention
            desiredCallingConventions |= desiredCallingConventions | CallingConventions.HasThis;

            Assert.Equal(desiredCallingConventions, actualCallingConventions);
        }
    }
}
