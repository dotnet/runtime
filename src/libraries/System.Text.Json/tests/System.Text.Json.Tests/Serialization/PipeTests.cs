// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class PipeTests
    {
        public sealed class PipeTests_Async : PipeTests
        {
            public PipeTests_Async() : base(JsonSerializerWrapper.AsyncPipeSerializer) { }
        }

        public sealed class PipeTests_AsyncWithSmallBuffer : PipeTests
        {
            public PipeTests_AsyncWithSmallBuffer() : base(JsonSerializerWrapper.AsyncPipeSerializerWithSmallBuffer) { }
        }

        /// <summary>
        /// The System Under Test for the test suite.
        /// </summary>
        private PipeJsonSerializerWrapper Serializer { get; }

        public PipeTests(PipeJsonSerializerWrapper serializer)
        {
            Serializer = serializer;
        }
    }
}
