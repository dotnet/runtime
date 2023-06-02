// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Net.Http.Tests
{
    public class HttpRequestOptionsTest
    {
        [Fact]
        public void HttpRequestOptionsIReadOnlyDictionaryMethods_Should_WorkSameAsIDictionary()
        {
            var requestOptions = new HttpRequestOptions();
            const string expectedKey = "WebAssemblyEnableStreamingResponse";
            const bool expectedValue = true;
            requestOptions.Set(new HttpRequestOptionsKey<bool>(expectedKey), expectedValue);

            IReadOnlyDictionary<string, object?> readOnlyDictionary = requestOptions;
            IDictionary<string, object?> dictionary = requestOptions;

            Assert.Equal(1, readOnlyDictionary.Count);
            Assert.Equal(1, dictionary.Count);

            Assert.True(readOnlyDictionary.ContainsKey(expectedKey));
            Assert.True(dictionary.ContainsKey(expectedKey));

            Assert.True(readOnlyDictionary.TryGetValue(expectedKey, out object? getValueFromReadOnlyDictionary));
            Assert.True(dictionary.TryGetValue(expectedKey, out object? getValueFromDictionary));
            Assert.NotNull(getValueFromReadOnlyDictionary);
            Assert.NotNull(getValueFromDictionary);
            Assert.Equal(expectedValue, getValueFromReadOnlyDictionary);
            Assert.Equal(expectedValue, getValueFromDictionary);

            Assert.Equal(expectedValue, readOnlyDictionary[expectedKey]);
            Assert.Equal(expectedValue, dictionary[expectedKey]);

            Assert.Collection(readOnlyDictionary.Keys, item => Assert.Equal(expectedKey, item));
            Assert.Collection(dictionary.Keys, item => Assert.Equal(expectedKey, item));

            Assert.Collection(readOnlyDictionary.Values, item => Assert.Equal(expectedValue, item));
            Assert.Collection(dictionary.Values, item => Assert.Equal(expectedValue, item));
        }
    }
}
