// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public  class MarshalByRefObjectTest : MarshalByRefObject
    {
        [Fact]
        public static void MarshalByRefObjectTests()
        {
            var obj = new MarshalByRefObjectTest();
            Assert.Throws<PlatformNotSupportedException>(() => obj.GetLifetimeService());
            Assert.Throws<PlatformNotSupportedException>(() => obj.InitializeLifetimeService());
            
            var clone = obj.MemberwiseClone(false);
            Assert.NotNull(clone);
            Assert.NotSame(clone, obj);
            
            var clone1 = obj.MemberwiseClone(false);
            Assert.NotNull(clone1);
            Assert.NotSame(clone1, obj);
            Assert.NotSame(clone1, clone);
        }
    }
}
