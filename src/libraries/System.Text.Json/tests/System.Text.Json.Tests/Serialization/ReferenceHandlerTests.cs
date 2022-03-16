// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    public sealed partial class ReferenceHandlerTestsDynamic : ReferenceHandlerTests
    {
        public ReferenceHandlerTestsDynamic() : base(JsonSerializerWrapperForString.StringSerializer, JsonSerializerWrapperForStream.AsyncStreamSerializer) { }
    }

    public sealed partial class ReferenceHandlerTests_IgnoreCycles_Dynamic : ReferenceHandlerTests_IgnoreCycles
    {
        public ReferenceHandlerTests_IgnoreCycles_Dynamic() : base(JsonSerializerWrapperForString.StringSerializer, JsonSerializerWrapperForStream.AsyncStreamSerializer) { }
    }
}
