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

        private TypeBuilder GetTestTypeBuilder()
        {
            AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName, TestAssemblyBuilderAccess);

            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestDynamicModuleName);
            return moduleBuilder.DefineType(TestDynamicTypeName, TestTypeAttributes);
        }

        [Fact]
        public void TestWithCodeTypeMask()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.CodeTypeMask;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void TestWithForwardRef()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.ForwardRef;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void TestWithIL()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.IL;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void TestWithInternalCall()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.InternalCall;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void TestWithManaged()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.Managed;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void TestWithManagedMask()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.ManagedMask;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }


        [Fact]
        public void TestWithNative()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.Native;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void TestWithNoInlining()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.NoInlining;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void TestWithOPTIL()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.OPTIL;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void TestWithPreserveSig()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.PreserveSig;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void TestWithRuntime()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.Runtime;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void TestWithSynchronized()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.Synchronized;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void TestWithUnmanaged()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.Unmanaged;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetImplementationFlags(desiredFlags);

            MethodImplAttributes actualFlags = builder.MethodImplementationFlags;
            Assert.Equal(desiredFlags, actualFlags);
        }

        [Fact]
        public void TestThrowsExceptionOnTypeCreated()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodImplAttributes desiredFlags = MethodImplAttributes.Unmanaged;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);

            Type type = typeBuilder.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => { builder.SetImplementationFlags(desiredFlags); });
        }
    }
}
