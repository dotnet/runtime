// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class RuntimeBinderTests
    {
        [Fact]
        public void MultipleUseOfSameLocalInSameScope()
        {
            dynamic d0 = 23;
            dynamic d1 = 14;
            if (d0 == 23)
            {
                dynamic d2 = 19;
                d0 = d0 - d1 + d2;
                Assert.Equal(28, new string(' ', d0).Length);
            }
            dynamic dr = d0 * d1 + d0 + d0 + d0 / d1 - Math.Pow(d1, 2);
            Assert.Equal(254, dr);
        }
    }
}
