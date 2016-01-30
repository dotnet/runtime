// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using Xunit;
using System.Collections.Generic;

namespace System.Reflection.Emit.Tests
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class ABAttribute1 : Attribute
    {
        private bool _s;

        public ABAttribute1(bool s)
        {
            _s = s;
        }
    }

    public class AssemblyBuilderSetCustomAttribute1
    {
        [Fact]
        public void TestSetCustomAttribute()
        {
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("PT1"), AssemblyBuilderAccess.Run);
            ConstructorInfo c = typeof(ABAttribute1).GetConstructor(new Type[] { typeof(bool) });
            asmBuilder.SetCustomAttribute(c, new byte[] { 01, 00, 01 });
            IEnumerable<Attribute> attributes = asmBuilder.GetCustomAttributes();
            Assert.Equal("System.Reflection.Emit.Tests.ABAttribute1", attributes.First().ToString());
        }

        [Fact]
        public void TestThrowsExceptionOnNullConstructorInfo()
        {
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NT1"), AssemblyBuilderAccess.Run);
            Assert.Throws<ArgumentNullException>(() => { asmBuilder.SetCustomAttribute(null, new byte[] { }); });
        }

        [Fact]
        public void TestThrowsExceptionOnNullByteArray()
        {
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NT2"), AssemblyBuilderAccess.Run);
            ConstructorInfo dummyCtor = typeof(DateTime).GetConstructor(new Type[] { });
            Assert.Throws<ArgumentNullException>(() => { asmBuilder.SetCustomAttribute(dummyCtor, null); });
        }
    }
}
