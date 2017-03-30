// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.ComponentModel.Tests
{
    public static partial class Win32ExceptionTestType
    {
        public static IEnumerable<object[]> SerializeDeserialize_MemberData()
        {
            yield return new object[] { new Win32Exception() };
            yield return new object[] { new Win32Exception(42) };
            yield return new object[] { new Win32Exception(-42) };
            yield return new object[] { new Win32Exception("some message") };
            yield return new object[] { new Win32Exception(42, "some message") };
            yield return new object[] { new Win32Exception("some message", new InvalidOperationException()) };
        }

        [Theory]
        [MemberData(nameof(SerializeDeserialize_MemberData))]
        public static void SerializeDeserialize(Win32Exception exception)
        {
            BinaryFormatterHelpers.AssertRoundtrips(exception, e => e.NativeErrorCode, e => e.ErrorCode);
        }

        [Fact]
        public static void GetObjectData_InvalidArgs_Throws()
        {
            var e = new Win32Exception();
            Assert.Throws<ArgumentNullException>("info", () => e.GetObjectData(null, default(StreamingContext)));
        }
    }
}
