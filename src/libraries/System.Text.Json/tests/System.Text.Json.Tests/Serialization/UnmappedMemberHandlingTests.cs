// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    public sealed partial class UnmappedMemberHandlingTests_String : UnmappedMemberHandlingTests
    {
        public UnmappedMemberHandlingTests_String() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public sealed partial class UnmappedMemberHandlingTests_AsyncStream : UnmappedMemberHandlingTests
    {
        public UnmappedMemberHandlingTests_AsyncStream() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }

    public sealed partial class UnmappedMemberHandlingTests_Pipe : UnmappedMemberHandlingTests
    {
        public UnmappedMemberHandlingTests_Pipe() : base(JsonSerializerWrapper.AsyncPipeSerializer) { }
    }
}
