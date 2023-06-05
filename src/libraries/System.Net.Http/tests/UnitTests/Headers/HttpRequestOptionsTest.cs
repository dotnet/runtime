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
            const string ExpectedKey = "WebAssemblyEnableStreamingResponse";
            const bool ExpectedValue = true;
            const string UnexpectedKey = "hello";

            var requestOptions = new HttpRequestOptions();
            requestOptions.Set(new HttpRequestOptionsKey<bool>(ExpectedKey), ExpectedValue);

            IReadOnlyDictionary<string, object?> readOnlyDictionary = requestOptions;
            IDictionary<string, object?> dictionary = requestOptions;

            Assert.Equal(1, readOnlyDictionary.Count);
            Assert.Equal(1, dictionary.Count);

            Assert.True(readOnlyDictionary.ContainsKey(ExpectedKey));
            Assert.True(dictionary.ContainsKey(ExpectedKey));
            Assert.False(readOnlyDictionary.ContainsKey(UnexpectedKey));
            Assert.False(dictionary.ContainsKey(UnexpectedKey));

            Assert.True(readOnlyDictionary.TryGetValue(ExpectedKey, out object? getValueFromReadOnlyDictionary));
            Assert.True(dictionary.TryGetValue(ExpectedKey, out object? getValueFromDictionary));
            Assert.Equal(ExpectedValue, getValueFromReadOnlyDictionary);
            Assert.Equal(ExpectedValue, getValueFromDictionary);

            Assert.Equal(ExpectedValue, readOnlyDictionary[ExpectedKey]);
            Assert.Equal(ExpectedValue, dictionary[ExpectedKey]);
            Assert.Throws<KeyNotFoundException>(() => readOnlyDictionary[UnexpectedKey]);
            Assert.Throws<KeyNotFoundException>(() => dictionary[UnexpectedKey]);

            Assert.Collection(readOnlyDictionary.Keys, item => Assert.Equal(ExpectedKey, item));
            Assert.Collection(dictionary.Keys, item => Assert.Equal(ExpectedKey, item));

            Assert.Collection(readOnlyDictionary.Values, item => Assert.Equal(ExpectedValue, item));
            Assert.Collection(dictionary.Values, item => Assert.Equal(ExpectedValue, item));
        }
    }
}
