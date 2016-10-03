// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public static partial class IntPtrTests
    {
        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void Equals_NetCoreApp11(IntPtr ptr, object obj, bool expected)
        {
            if (!(obj is IntPtr))
            {
                return;
            }

            IEquatable<IntPtr> iEquatable = ptr;
            Assert.Equal(expected, iEquatable.Equals((IntPtr)obj));
        }
    }
}
