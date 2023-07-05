// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public sealed partial class CollectionTestsDynamic_String : CollectionTests
    {
        public CollectionTestsDynamic_String() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public sealed partial class CollectionTestsDynamic_AsyncStream : CollectionTests
    {
        public CollectionTestsDynamic_AsyncStream() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }

    public sealed partial class CollectionTestsDynamic_AsyncStreamWithSmallBuffer : CollectionTests
    {
        public CollectionTestsDynamic_AsyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.AsyncStreamSerializerWithSmallBuffer) { }
    }

    public sealed partial class CollectionTestsDynamic_SyncStream : CollectionTests
    {
        public CollectionTestsDynamic_SyncStream() : base(JsonSerializerWrapper.SyncStreamSerializer) { }
    }
}
