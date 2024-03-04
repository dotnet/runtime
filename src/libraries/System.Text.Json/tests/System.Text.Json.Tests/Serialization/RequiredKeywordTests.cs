// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class RequiredKeywordTests_Span : RequiredKeywordTests
    {
        public RequiredKeywordTests_Span() : base(JsonSerializerWrapper.SpanSerializer) { }
    }

    public class RequiredKeywordTests_String : RequiredKeywordTests
    {
        public RequiredKeywordTests_String() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public class RequiredKeywordTests_AsyncStream : RequiredKeywordTests
    {
        public RequiredKeywordTests_AsyncStream() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }

    public class RequiredKeywordTests_AsyncStreamWithSmallBuffer : RequiredKeywordTests
    {
        public RequiredKeywordTests_AsyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.AsyncStreamSerializerWithSmallBuffer) { }
    }

    public class RequiredKeywordTests_SyncStream : RequiredKeywordTests
    {
        public RequiredKeywordTests_SyncStream() : base(JsonSerializerWrapper.SyncStreamSerializer) { }
    }

    public class RequiredKeywordTests_Writer : RequiredKeywordTests
    {
        public RequiredKeywordTests_Writer() : base(JsonSerializerWrapper.ReaderWriterSerializer) { }
    }

    public class RequiredKeywordTests_Document : RequiredKeywordTests
    {
        public RequiredKeywordTests_Document() : base(JsonSerializerWrapper.DocumentSerializer) { }
    }

    public class RequiredKeywordTests_Element : RequiredKeywordTests
    {
        public RequiredKeywordTests_Element() : base(JsonSerializerWrapper.ElementSerializer) { }
    }

    public class RequiredKeywordTests_Node : RequiredKeywordTests
    {
        public RequiredKeywordTests_Node() : base(JsonSerializerWrapper.NodeSerializer) { }
    }

    public class RequiredKeywordTests_Pipe : RequiredKeywordTests
    {
        public RequiredKeywordTests_Pipe() : base(JsonSerializerWrapper.PipeSerializer) { }
    }
}
