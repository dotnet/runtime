// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class ReflectionContextTests
    {
        [Fact]
        public static void ReflectionContextTestsGetTypeForObject()
        {
            MyRC rc = new MyRC();
            TypeInfo ti = rc.MapType(typeof(int).GetTypeInfo());

            Assembly asm = rc.MapAssembly(typeof(int).GetTypeInfo().Assembly);

            ti = rc.GetTypeForObject(rc);

            Assert.Equal(ti, typeof(MyRC).GetTypeInfo());
        }
    }

    public class MyRC : ReflectionContext
    {
        public override Assembly MapAssembly(Assembly asm)
        {
            return asm;
        }

        public override TypeInfo MapType(TypeInfo typeInfo)
        {
            return typeInfo;
        }
    }
}
