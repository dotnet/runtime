// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class CallingConventionTests
    {
        [Fact]
        public void Values()
        {
            Assert.Equal(1, (int)CallingConvention.Winapi);
            Assert.Equal(2, (int)CallingConvention.Cdecl);
            Assert.Equal(3, (int)CallingConvention.StdCall);
            Assert.Equal(4, (int)CallingConvention.ThisCall);
            Assert.Equal(5, (int)CallingConvention.FastCall);
        }
    }
}
