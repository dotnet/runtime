// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public class DictionaryEntry_Tests
    {
        public static IEnumerable<object?[]> ToString_Inputs()
        {
            // Mainline scenarios
            yield return new object?[] { "key", "value", "[key, value]" };
            yield return new object?[] { 1, 2, "[1, 2]" };

            // Types, nulls, and empty strings get standard ToString behavior
            yield return new object?[] { typeof(object), (object?)null, "[System.Object, ]" };
            yield return new object?[] { (object?)null, (object?)null, "[, ]" };
            yield return new object?[] { "", "", "[, ]" };

            // There's no escaping; keys and values are emitted as-is
            yield return new object?[] { "[key, value", "]]", "[[key, value, ]]]" };
            yield return new object?[] { new DictionaryEntry("key", "key"), new DictionaryEntry("value", "value"), "[[key, key], [value, value]]" };

            // There's no truncation; keys and values are emitted as-is
            yield return new object?[] { new String('K', 512), new String('V', 1024), $"[{new String('K', 512)}, {new String('V', 1024)}]" };
        }

        [Theory]
        [MemberData(nameof(ToString_Inputs))]
        public void ToString_FormatsKeyValue(object key, object? value, string expected)
        {
            var entry = new DictionaryEntry(key, value);
            string result = entry.ToString();

            Assert.Equal(expected, result);
        }
    }
}
