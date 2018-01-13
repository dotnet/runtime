// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Http;
using Xunit;

namespace System.Net.Http.Tests
{
    public class DigestAuthenticationTests
    {
        private static readonly List<string> s_keyListWithCountTwo = new List<string> { "key1", "key2" };
        private static readonly List<string> s_valueListWithCountTwo = new List<string> { "value1", "value2" };
        private static readonly List<string> s_emptyStringList = new List<string>();

        [Theory]
        [MemberData(nameof(DigestResponse_Challenge_TestData))]
        public void DigestResponse_Parse_Succeeds(string challenge, List<string> keys, List<string> values)
        {
            AuthenticationHelper.DigestResponse digestResponse = new AuthenticationHelper.DigestResponse(challenge);
            Assert.Equal(keys.Count, digestResponse.Parameters.Count);
            Assert.Equal(values.Count, digestResponse.Parameters.Count);
            Assert.Equal(keys, digestResponse.Parameters.Keys);
            Assert.Equal(values, digestResponse.Parameters.Values);
        }

        public static IEnumerable<object[]> DigestResponse_Challenge_TestData()
        {
            yield return new object[] { "key1=value1,key2=value2", s_keyListWithCountTwo, s_valueListWithCountTwo };
            yield return new object[] { "\tkey1===value1,key2 \t===\tvalue2", s_keyListWithCountTwo, s_valueListWithCountTwo };
            yield return new object[] { "    key1 = value1, key2 =    value2,", s_keyListWithCountTwo, s_valueListWithCountTwo };
            yield return new object[] { "key1 === value1,key2=, value2", s_keyListWithCountTwo, new List<string> { "value1", string.Empty } };
            yield return new object[] { "key1,==value1,,,    key2=\"value2\", key3 m", new List<string> { "key1,", "key2" }, s_valueListWithCountTwo };
            yield return new object[] { "key1= \"value1   \",key2  =  \"v alu#e2\"   ,", s_keyListWithCountTwo, new List<string> { "value1   ", "v alu#e2"} };
            yield return new object[] { "key1   ", s_emptyStringList, s_emptyStringList };
            yield return new object[] { "=====", s_emptyStringList, s_emptyStringList };
            yield return new object[] { ",,", s_emptyStringList, s_emptyStringList };
            yield return new object[] { "=,=", s_emptyStringList, s_emptyStringList };
            yield return new object[] { "=value1,key2=,", s_emptyStringList, s_emptyStringList };
            yield return new object[] { "key1\tm= value1", s_emptyStringList, s_emptyStringList };
        }
    }
}
