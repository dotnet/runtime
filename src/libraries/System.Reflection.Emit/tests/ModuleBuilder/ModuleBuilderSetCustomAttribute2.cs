// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;
using System.Linq;

namespace System.Reflection.Emit.Tests
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class MBMyAttribute2 : Attribute
    {
        public int i;

        public MBMyAttribute2(int i)
        {
            this.i = i;
        }
    }

    public class ModuleBuilderSetCustomAttribute2
    {
        [Fact]
        public void TestSetCustomAttribute()
        {
            AssemblyName TestAssemblyName = new AssemblyName();
            TestAssemblyName.Name = "TestAssembly";
            AssemblyBuilder TestAssembly = AssemblyBuilder.DefineDynamicAssembly(TestAssemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder TestModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(TestAssembly, "Module1");
            ConstructorInfo infoConstructor = typeof(MBMyAttribute2).GetConstructor(new Type[] { typeof(int) });
            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(infoConstructor, new object[] { 5 });
            TestModuleBuilder.SetCustomAttribute(attributeBuilder);
            object[] attributes = TestModuleBuilder.GetCustomAttributes().Select(a => (object)a).ToArray();
            Assert.Equal(1, attributes.Length);
            Assert.True(attributes[0] is MBMyAttribute2);
            Assert.Equal(5, ((MBMyAttribute2)attributes[0]).i);
        }

        [Fact]
        public void TestThrowsExceptionOnNullBuilder()
        {
            AssemblyName TestAssemblyName = new AssemblyName();
            TestAssemblyName.Name = "TestAssembly";
            AssemblyBuilder TestAssembly = AssemblyBuilder.DefineDynamicAssembly(TestAssemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder TestModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(TestAssembly, "Module1");
            CustomAttributeBuilder attributeBuilder = null;
            Assert.Throws<ArgumentNullException>(() => { TestModuleBuilder.SetCustomAttribute(attributeBuilder); });
        }
    }
}
