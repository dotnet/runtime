// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public sealed class ReferenceHandlerTestsDynamic_String : ReferenceHandlerTests
    {
        public ReferenceHandlerTestsDynamic_String() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public sealed class ReferenceHandlerTestsDynamic_AsyncStream : ReferenceHandlerTests
    {
        public ReferenceHandlerTestsDynamic_AsyncStream() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/66727")]
    public sealed class ReferenceHandlerTestsDynamic_AsyncStreamWithSmallBuffer : ReferenceHandlerTests
    {
        public ReferenceHandlerTestsDynamic_AsyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.AsyncStreamSerializerWithSmallBuffer) { }
    }

    public sealed class ReferenceHandlerTestsDynamic_IgnoreCycles_String : ReferenceHandlerTests_IgnoreCycles
    {
        public ReferenceHandlerTestsDynamic_IgnoreCycles_String() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public sealed class ReferenceHandlerTestsDynamic_IgnoreCycles_AsyncStream : ReferenceHandlerTests_IgnoreCycles
    {
        public ReferenceHandlerTestsDynamic_IgnoreCycles_AsyncStream() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }
}
