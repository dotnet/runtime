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
            yield return new object[] { new Cookie("somekey", "somevalue") };
            yield return new object[] { new CookieCollection() { new Cookie("somekey", "somevalue") } };
        }

        [Theory]
        [MemberData(nameof(SerializeDeserialize_Roundtrip_MemberData))]
        public static void SerializeDeserialize_Roundtrip_EqualObjects(object obj)
        {
            Assert.Equal(obj, BinaryFormatterHelpers.Clone(obj));
        }

        [Fact]
        public static void SerializeDeserialize_CookieContainerRoundtrip_EqualValues()
        {
            CookieContainer cookies1 = new CookieContainer();
            CookieContainer cookies2 = BinaryFormatterHelpers.Clone(cookies1);

            Assert.Equal(cookies1.Capacity, cookies2.Capacity);
            Assert.Equal(cookies1.Count, cookies2.Count);
            Assert.Equal(cookies1.MaxCookieSize, cookies2.MaxCookieSize);
            Assert.Equal(cookies1.PerDomainCapacity, cookies2.PerDomainCapacity);
        }
    }
}
