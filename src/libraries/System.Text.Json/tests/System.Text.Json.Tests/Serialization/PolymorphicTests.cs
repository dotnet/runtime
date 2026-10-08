// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    public sealed class PolymorphicTests_Span : PolymorphicTests
    {
        public PolymorphicTests_Span() : base(JsonSerializerWrapper.SpanSerializer) { }
    }

    public sealed class PolymorphicTests_String : PolymorphicTests
    {
        public PolymorphicTests_String() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public sealed class PolymorphicTests_AsyncStream : PolymorphicTests
    {
        public PolymorphicTests_AsyncStream() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }

    public sealed class PolymorphicTests_AsyncStreamWithSmallBuffer : PolymorphicTests
    {
        public PolymorphicTests_AsyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.AsyncStreamSerializerWithSmallBuffer) { }
    }

    public sealed class PolymorphicTests_SyncStream : PolymorphicTests
    {
        public PolymorphicTests_SyncStream() : base(JsonSerializerWrapper.SyncStreamSerializer) { }
    }

    public sealed class PolymorphicTests_Writer : PolymorphicTests
    {
        public PolymorphicTests_Writer() : base(JsonSerializerWrapper.ReaderWriterSerializer) { }
    }

    public sealed class PolymorphicTests_Document : PolymorphicTests
    {
        public PolymorphicTests_Document() : base(JsonSerializerWrapper.DocumentSerializer) { }
    }

    public sealed class PolymorphicTests_Element : PolymorphicTests
    {
        public PolymorphicTests_Element() : base(JsonSerializerWrapper.ElementSerializer) { }
    }

    public sealed class PolymorphicTests_Node : PolymorphicTests
    {
        public PolymorphicTests_Node() : base(JsonSerializerWrapper.NodeSerializer) { }
    }

    public sealed class PolymorphicTests_Pipe : PolymorphicTests
    {
        public PolymorphicTests_Pipe() : base(JsonSerializerWrapper.AsyncPipeSerializer) { }
    }

    public sealed class PolymorphicTests_PipeWithSmallBuffer : PolymorphicTests
    {
        public PolymorphicTests_PipeWithSmallBuffer() : base(JsonSerializerWrapper.AsyncPipeSerializerWithSmallBuffer) { }
    }
}
