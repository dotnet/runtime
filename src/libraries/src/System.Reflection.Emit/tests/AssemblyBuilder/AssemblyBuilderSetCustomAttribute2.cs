// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using Xunit;
using System.Collections.Generic;

namespace System.Reflection.Emit.Tests
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class ABAttribute2 : Attribute
    {
        public int i;

        public ABAttribute2(int i)
        {
            this.i = i;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ABClassAttribute : Attribute
    {
        public int i;

        public ABClassAttribute(int i)
        {
            this.i = i;
        }
    }

    public class AssemblyBuilderSetCustomAttribute2
    {
        [Fact]
        public void TestSetCustomAttribute()
        {
            AssemblyName TestAssemblyName = new AssemblyName();
            TestAssemblyName.Name = "TestAssembly";
            AssemblyBuilder TestAssembly = AssemblyBuilder.DefineDynamicAssembly(TestAssemblyName, AssemblyBuilderAccess.Run);
            ConstructorInfo infoConstructor = typeof(ABClassAttribute).GetConstructor(new Type[] { typeof(int) });
            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(infoConstructor, new object[] { 5 });
            TestAssembly.SetCustomAttribute(attributeBuilder);
            IEnumerable<Attribute> attributes = TestAssembly.GetCustomAttributes();
            Assert.Equal("System.Reflection.Emit.Tests.ABClassAttribute", attributes.First().ToString());
        }

        [Fact]
        public void TestThrowsExceptionOnNullCustomAttributeBuilder()
        {
            AssemblyName TestAssemblyName = new AssemblyName();
            TestAssemblyName.Name = "TestAssembly";
            AssemblyBuilder TestAssembly = AssemblyBuilder.DefineDynamicAssembly(TestAssemblyName, AssemblyBuilderAccess.Run);
            CustomAttributeBuilder attributeBuilder = null;
            Assert.Throws<ArgumentNullException>(() => { TestAssembly.SetCustomAttribute(attributeBuilder); });
        }
    }
}
