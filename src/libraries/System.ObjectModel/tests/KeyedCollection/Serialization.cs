// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    public partial class KeyedCollection_Serialization
    {
        public static IEnumerable<object[]> SerializeDeserialize_Roundtrips_MemberData()
        {
            yield return new object[] { new TestCollection() };
            yield return new object[] { new TestCollection() { "hello" } };
            yield return new object[] { new TestCollection() { "hello", "world" } };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        [MemberData(nameof(SerializeDeserialize_Roundtrips_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50933", TestPlatforms.Android)]
        public void SerializeDeserialize_Roundtrips(TestCollection c)
        {
            TestCollection clone = BinaryFormatterHelpers.Clone(c);
            Assert.NotSame(c, clone);
            Assert.Equal(c, clone);
        }

        [Serializable]
        public sealed class TestCollection : KeyedCollection<string, string>
        {
            protected override string GetKeyForItem(string item) => item;
        }
    }
}
