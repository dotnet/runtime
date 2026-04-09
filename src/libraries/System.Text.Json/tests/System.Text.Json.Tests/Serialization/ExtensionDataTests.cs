// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    public sealed partial class ExtensionDataTestsDynamic_String : ExtensionDataTests
    {
        public ExtensionDataTestsDynamic_String() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public sealed partial class ExtensionDataTestsDynamic_AsyncStream : ExtensionDataTests
    {
        public ExtensionDataTestsDynamic_AsyncStream() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }
}
