// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class AsyncEnumerableTests_AsyncStream : AsyncEnumerableTests
    {
        public AsyncEnumerableTests_AsyncStream() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }

        [Fact]
        public void DeserializeAsyncEnumerable_NullArgument_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("utf8Json", () => JsonSerializer.DeserializeAsyncEnumerable<int>(utf8Json: (Stream?)null));
            AssertExtensions.Throws<ArgumentNullException>("utf8Json", () => JsonSerializer.DeserializeAsyncEnumerable<int>(utf8Json: (Stream?)null, jsonTypeInfo: ResolveJsonTypeInfo<int>()));
            AssertExtensions.Throws<ArgumentNullException>("jsonTypeInfo", () => JsonSerializer.DeserializeAsyncEnumerable<int>(utf8Json: new MemoryStream(), jsonTypeInfo: null));
        }
    }

    public class AsyncEnumerableTests_AsyncPipeSerializer : AsyncEnumerableTests
    {
        public AsyncEnumerableTests_AsyncPipeSerializer() : base(JsonSerializerWrapper.AsyncPipeSerializer) { }

        [Fact]
        public void DeserializeAsyncEnumerable_NullArgument_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("utf8Json", () => JsonSerializer.DeserializeAsyncEnumerable<int>(utf8Json: (PipeReader?)null));
            AssertExtensions.Throws<ArgumentNullException>("utf8Json", () => JsonSerializer.DeserializeAsyncEnumerable<int>(utf8Json: (PipeReader?)null, jsonTypeInfo: ResolveJsonTypeInfo<int>()));
            AssertExtensions.Throws<ArgumentNullException>("jsonTypeInfo", () => JsonSerializer.DeserializeAsyncEnumerable<int>(utf8Json: new Pipe().Reader, jsonTypeInfo: null));
        }
    }
}
