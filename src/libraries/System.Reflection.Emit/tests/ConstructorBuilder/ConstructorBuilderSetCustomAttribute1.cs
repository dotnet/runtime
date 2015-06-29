// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;

using Xunit;

namespace System.Reflection.Emit.Tests
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class CBMyAttribute1 : Attribute
    {
        public int i;

        public CBMyAttribute1(int i)
        {
            this.i = i;
        }
    }

    public class ConstructorBuilderSetCustomAttribute1
    {
        [Fact]
        public void PosTest1()
        {
            AssemblyName TestAssemblyName = new AssemblyName("TestAssembly");
            AssemblyBuilder TestAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(TestAssemblyName, AssemblyBuilderAccess.Run);

            ModuleBuilder TestModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(TestAssemblyBuilder, "Module1");

            TypeBuilder TestTypeBuilder = TestModuleBuilder.DefineType("TestTypeBuilder", TypeAttributes.Public);
            ConstructorBuilder TestConstructorBuilder =
                TestTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(int) });
            Type myType = typeof(CBMyAttribute1);
            ConstructorInfo myConstructorInfo = myType.GetConstructor(new Type[] { typeof(int) });
            TestConstructorBuilder.SetCustomAttribute(myConstructorInfo, new byte[] { 01, 00, 05, 00, 00, 00 });
        }

        [Fact]
        public void NegTest1()
        {
            AssemblyName TestAssemblyName = new AssemblyName("TestAssembly");
            AssemblyBuilder TestAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(TestAssemblyName, AssemblyBuilderAccess.Run);

            ModuleBuilder TestModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(TestAssemblyBuilder, "Module1");

            TypeBuilder TestTypeBuilder = TestModuleBuilder.DefineType("TestTypeBuilder", TypeAttributes.Public);
            ConstructorBuilder TestConstructorBuilder =
                TestTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(int) });
            Assert.Throws<ArgumentNullException>(() => { TestConstructorBuilder.SetCustomAttribute(null, new byte[] { 01, 00, 05, 00, 00, 00 }); });
        }
    }
}
