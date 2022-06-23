// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    public class ConstructorTests_String : ConstructorTests
    {
        public ConstructorTests_String()
            : base(JsonSerializerWrapper.StringSerializer)
        { }
    }

    public class ConstructorTests_AsyncStream : ConstructorTests
    {
        public ConstructorTests_AsyncStream()
            : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }

    public class ConstructorTests_SyncStream : ConstructorTests
    {
        public ConstructorTests_SyncStream()
            : base(JsonSerializerWrapper.SyncStreamSerializer) { }
    }

    public class ConstructorTests_Span : ConstructorTests
    {
        public ConstructorTests_Span()
            : base(JsonSerializerWrapper.SpanSerializer) { }
    }
}
