// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/58204", TestPlatforms.iOS | TestPlatforms.tvOS)]
    public sealed partial class CollectionTestsDynamic : CollectionTests
    {
        public CollectionTestsDynamic() : base(JsonSerializerWrapperForString.StringSerializer, JsonSerializerWrapperForStream.AsyncStreamSerializer) { }
    }
}
