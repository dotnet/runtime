// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualBasic.CompilerServices;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class BooleanTypeTests
    {
        [Fact]
        public void NullData()
        {
            // Null is valid input for Boolean.FromObject
            Assert.Throws<InvalidCastException>(() => BooleanType.FromString(null));
        }

        [Theory]
        [MemberData(nameof(InvalidStringData))]
        public void InvalidCastString(string value)
        {
            Assert.Throws<InvalidCastException>(() => BooleanType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(InvalidStringData))]
        [MemberData(nameof(InvalidObjectData))]
        public void InvalidCastObject(object value)
        {
            Assert.Throws<InvalidCastException>(() => BooleanType.FromObject(value));
        }

        public static TheoryData<string> InvalidStringData => new TheoryData<string>()
        {
            { "" },
            { "23&" },
            { "abc" },
        };

        public static TheoryData<object> InvalidObjectData => new TheoryData<object>()
        {
            { DateTime.Now },
            { 'c' },
            { Guid.Empty }
        };

        [Theory]
        [MemberData(nameof(BoolStringData))]
        public void FromString(bool expected, string value)
        {
            Assert.Equal(expected, BooleanType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(BoolStringData))]
        [MemberData(nameof(BoolObjectData))]
        public void FromObject(bool expected, object value)
        {
            Assert.Equal(expected, BooleanType.FromObject(value));
        }

        public static TheoryData<bool, string> BoolStringData => new TheoryData<bool, string>()
        {
            { false, "0"},
            { false, "False"},
            { true, "True"},
            { true, "1"},
            { true, "1.2"},
            { true, "2"},
            { true, "-1"},
            { false, "&H00" },
            { false, "&O00" },
            { true, "&H01" },
            { true, "&O01" },
            { true, "9999999999999999999999999999999999999" }
        };

        public static TheoryData<bool, object> BoolObjectData => new TheoryData<bool, object>()
        {
            { false, 0 },
            { false, null },
            { false, false },
            { true, true },
            { true, 1 },
            { true, 1.2 },
            { true, 2 },
            { true, -1 },
            { false, (byte)0 },
            { true, (byte)1 },
            { false, (short)0 },
            { true, (short)1 },
            { false, (double)0 },
            { true, (double)1 },
            { false, (decimal)0 },
            { true, (decimal)1 }
        };
    }
}
