// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    public sealed class StreamTests_Async : StreamTests
    {
        public StreamTests_Async() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }

    public sealed class StreamTests_Sync : StreamTests
    {
        public StreamTests_Sync() : base(StreamingJsonSerializerWrapper.SyncStreamSerializer) { }
    }

    public abstract partial class StreamTests
    {
        /// <summary>
        /// The System Under Test for the test suite.
        /// </summary>
        private StreamingJsonSerializerWrapper Serializer { get; }

        public StreamTests(StreamingJsonSerializerWrapper serializer)
        {
            Serializer = serializer;
        }
    }
}
