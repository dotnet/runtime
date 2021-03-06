// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    public abstract class SerializerTests
    {
        protected JsonSerializerWrapperForString JsonSerializerWrapperForString { get; }

        protected JsonSerializerWrapperForStream JsonSerializerWrapperForStream { get; }

        protected SerializerTests(JsonSerializerWrapperForString stringSerializerWrapper, JsonSerializerWrapperForStream? streamSerializerWrapper = null)
            => (JsonSerializerWrapperForString, JsonSerializerWrapperForStream) = (stringSerializerWrapper, streamSerializerWrapper);
    }
}
