// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public static partial class TimeSpanTests
    {
        [Theory]
        [MemberData(nameof(CompareTo_TestData))]
        public static void CompareTo_Object(TimeSpan timeSpan1, object obj, int expected)
        {
            Assert.Equal(expected, Math.Sign(timeSpan1.CompareTo(obj)));
        }
    }
}
