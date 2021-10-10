// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests : SerializerTests
    {
        public CollectionTests(JsonSerializerWrapperForString stringSerializerWrapper, JsonSerializerWrapperForStream streamSerializerWrapper)
            : base(stringSerializerWrapper, streamSerializerWrapper) { }
    }
}
