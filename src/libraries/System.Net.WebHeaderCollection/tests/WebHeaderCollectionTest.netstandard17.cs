// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Tests;

using Xunit;

namespace System.Net.WebHeaderCollectionTests
{
    public partial class WebHeaderCollectionTest
    {
        public static IEnumerable<object[]> SerializeDeserialize_Roundtrip_MemberData()
        {
            for (int i = 0; i < 10; i++)
            {
                var wc = new WebHeaderCollection();
                for (int j = 0; j < i; j++)
                {
                    wc[$"header{j}"] = $"value{j}";
                }
                yield return new object[] { wc };
            }
        }

        [Theory]
        [MemberData(nameof(SerializeDeserialize_Roundtrip_MemberData))]
        public void SerializeDeserialize_Roundtrip(WebHeaderCollection c)
        {
            Assert.Equal(c, BinaryFormatterHelpers.Clone(c));
        }
    }
}
