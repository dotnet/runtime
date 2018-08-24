// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public partial class DefaultValueAttributeTests
    {
        [Fact]
        public static void Ctor_netcoreapp11()
        {
            Assert.Equal((sbyte)42, new DefaultValueAttribute((sbyte)42).Value);
            Assert.Equal((ushort)42, new DefaultValueAttribute((ushort)42).Value);
            Assert.Equal((uint)42, new DefaultValueAttribute((uint)42).Value);
            Assert.Equal((ulong)42, new DefaultValueAttribute((ulong)42).Value);
        }
    }
}
