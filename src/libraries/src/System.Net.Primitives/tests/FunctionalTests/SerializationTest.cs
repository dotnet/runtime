// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class SerializationTest
    {
        public static IEnumerable<object[]> SerializeDeserialize_Roundtrip_MemberData()
        {
            yield return new object[] { IPAddress.Parse("127.0.0.1") };
            yield return new object[] { new IPEndPoint(IPAddress.Loopback, 12345) };
            yield return new object[] { new Cookie("somekey", "somevalue") };
            yield return new object[] { new CookieCollection() { new Cookie("somekey", "somevalue") } };
        }

        [Theory]
        [MemberData(nameof(SerializeDeserialize_Roundtrip_MemberData))]
        public static void SerializeDeserialize_Roundtrip(object obj)
        {
            Assert.Equal(obj, BinaryFormatterHelpers.Clone(obj));
        }
    }
}
