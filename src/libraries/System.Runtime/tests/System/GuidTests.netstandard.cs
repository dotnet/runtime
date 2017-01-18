// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public static partial class GuidTests
    {
        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToString_Provider(Guid guid, string format, string expected)
        {
            // Format provider parameter is ignored
            Assert.Equal(expected, guid.ToString(format, CultureInfo.CurrentCulture));
        }

        [Theory]
        [MemberData(nameof(CompareTo_TestData))]
        public static void CompareTo_Object(Guid guid, object obj, int expected)
        {
            Assert.Equal(expected, Math.Sign(guid.CompareTo(obj)));
        }
    }
}
