// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    public sealed class UnionTests_String : UnionTests
    {
        public UnionTests_String() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public sealed class UnionTests_AsyncStreamWithSmallBuffer : UnionTests
    {
        public UnionTests_AsyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.AsyncStreamSerializerWithSmallBuffer) { }
    }

    public sealed class UnionTests_SyncStreamWithSmallBuffer : UnionTests
    {
        public UnionTests_SyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.SyncStreamSerializerWithSmallBuffer) { }
    }
}
