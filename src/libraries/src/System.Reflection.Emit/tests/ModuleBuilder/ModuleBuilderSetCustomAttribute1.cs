// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;
using System.Linq;

namespace System.Reflection.Emit.Tests
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class MBMyAttribute1 : Attribute
    {
        public int i;

        public MBMyAttribute1(int i)
        {
            this.i = i;
        }
    }

    public class ModuleBuilderSetCustomAttribute1
    {
        [Fact]
        public void TestSetCustomAttribute()
        {
            AssemblyName TestAssemblyName = new AssemblyName();
            TestAssemblyName.Name = "TestAssembly";
            AssemblyBuilder TestAssembly = AssemblyBuilder.DefineDynamicAssembly(TestAssemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder TestModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(TestAssembly, "Module1");
            ConstructorInfo infoConstructor = typeof(MBMyAttribute1).GetConstructor(new Type[] { typeof(int) });
            TestModuleBuilder.SetCustomAttribute(infoConstructor, new byte[] { 01, 00, 05, 00, 00, 00 });
            object[] attributes = TestModuleBuilder.GetCustomAttributes().Select(a => (object)a).ToArray();
            Assert.Equal(1, attributes.Length);
            Assert.True(attributes[0] is MBMyAttribute1);
            Assert.Equal(5, ((MBMyAttribute1)attributes[0]).i);
        }

        [Fact]
        public void TestThrowsExceptionOnNullConstructorInfo()
        {
            AssemblyName TestAssemblyName = new AssemblyName();
            TestAssemblyName.Name = "TestAssembly";
            AssemblyBuilder TestAssembly = AssemblyBuilder.DefineDynamicAssembly(TestAssemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder TestModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(TestAssembly, "Module1");
            Assert.Throws<ArgumentNullException>(() => { TestModuleBuilder.SetCustomAttribute(null, new byte[] { 01, 00, 05, 00, 00, 00 }); });
        }
    }
}
