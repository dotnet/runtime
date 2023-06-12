// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    public sealed class JsonCreationHandlingTests_String : JsonCreationHandlingTests
    {
        public JsonCreationHandlingTests_String() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public sealed class JsonCreationHandlingTests_AsyncStream : JsonCreationHandlingTests
    {
        public JsonCreationHandlingTests_AsyncStream() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }

    public sealed class JsonCreationHandlingTests_AsyncStreamWithSmallBuffer : JsonCreationHandlingTests
    {
        public JsonCreationHandlingTests_AsyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.AsyncStreamSerializerWithSmallBuffer) { }
    }

    public sealed class JsonCreationHandlingTests_SyncStream : JsonCreationHandlingTests
    {
        public JsonCreationHandlingTests_SyncStream() : base(JsonSerializerWrapper.SyncStreamSerializer) { }
    }
}
