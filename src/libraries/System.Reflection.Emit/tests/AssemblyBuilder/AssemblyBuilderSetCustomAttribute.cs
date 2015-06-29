// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public void PosTest1()
        {
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("PT1"), AssemblyBuilderAccess.Run);
            ConstructorInfo c = typeof(ABAttribute1).GetConstructor(new Type[] { typeof(bool) });
            asmBuilder.SetCustomAttribute(c, new byte[] { 01, 00, 01 });
            IEnumerable<Attribute> attributes = asmBuilder.GetCustomAttributes();
            Assert.Equal("System.Reflection.Emit.Tests.ABAttribute1", attributes.First().ToString());
        }

        [Fact]
        public void NegTest1()
        {
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NT1"), AssemblyBuilderAccess.Run);
            Assert.Throws<ArgumentNullException>(() => { asmBuilder.SetCustomAttribute(null, new byte[] { }); });
        }

        [Fact]
        public void NegTest2()
        {
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NT2"), AssemblyBuilderAccess.Run);
            ConstructorInfo dummyCtor = typeof(DateTime).GetConstructor(new Type[] { });
            Assert.Throws<ArgumentNullException>(() => { asmBuilder.SetCustomAttribute(dummyCtor, null); });
        }
    }
}
