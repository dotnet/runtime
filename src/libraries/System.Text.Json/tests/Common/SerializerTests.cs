// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    /// <summary>
    /// Base class abstracting the serialization System Under Test.
    /// </summary>
    public abstract class SerializerTests
    {
        /// <summary>
        /// The serialization System Under Test to be targeted by deriving test suites.
        /// </summary>
        protected JsonSerializerWrapper Serializer { get; }

        /// <summary>
        /// For Systems Under Test that support streaming, exposes the relevant API surface.
        /// </summary>
        protected StreamingJsonSerializerWrapper? StreamingSerializer { get; }

        protected SerializerTests(JsonSerializerWrapper serializerUnderTest)
        {
            Serializer = serializerUnderTest;
            StreamingSerializer = serializerUnderTest as StreamingJsonSerializerWrapper;
        }
    }
}
