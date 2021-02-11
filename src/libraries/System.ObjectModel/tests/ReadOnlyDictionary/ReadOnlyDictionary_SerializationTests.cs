// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    public partial class ReadOnlyDictionary_Serialization
    {
        public static IEnumerable<object[]> SerializeDeserialize_Roundtrips_MemberData()
        {
            yield return new object[] { new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()) };
            yield return new object[] { new ReadOnlyDictionary<string, string>(new Dictionary<string, string>() { { "a", "b" } }) };
            yield return new object[] { new ReadOnlyDictionary<string, string>(new Dictionary<string, string>() { { "a", "b" }, { "c", "d" } }) };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        [MemberData(nameof(SerializeDeserialize_Roundtrips_MemberData))]
        public void SerializeDeserialize_Roundtrips(ReadOnlyDictionary<string, string> d)
        {
            ReadOnlyDictionary<string, string> clone = BinaryFormatterHelpers.Clone(d);
            Assert.NotSame(d, clone);
            Assert.Equal(d, clone);
        }
    }
}
