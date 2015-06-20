// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderSetImplementationFlags
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const TypeAttributes TestTypeAttributes = TypeAttributes.Abstract;
        private const MethodAttributes TestMethodAttributes = MethodAttributes.Public | MethodAttributes.Static;
        private const int MinStringLength = 1;
        private const int MaxStringLength = 128;
        private const int ByteArraySize = 128;

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
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.CodeTypeMask;

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void PosTest2()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.ForwardRef;

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void PosTest3()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.IL;

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void PosTest4()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.InternalCall;

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void PosTest5()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.Managed;

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void PosTest6()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.ManagedMask;

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }


        [Fact]
        public void PosTest8()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.Native;

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void PosTest9()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.NoInlining;

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void PosTest10()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.OPTIL;

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void PosTest11()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.PreserveSig;

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void PosTest12()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.Runtime;

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void PosTest13()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.Synchronized;

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void PosTest14()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.Unmanaged;

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void NegTest1()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.Unmanaged;

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);

            Type type = TestTypeBuilder.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => { builder.SetImplementationFlags(desiredFlags); });
        }
    }
}
