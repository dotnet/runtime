// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    public sealed partial class NullableAnnotationsTests_String : NullableAnnotationsTests
    {
        public NullableAnnotationsTests_String() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public sealed partial class NullableAnnotationsTests_AsyncStreamWithSmallBuffer : NullableAnnotationsTests
    {
        public NullableAnnotationsTests_AsyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.AsyncStreamSerializerWithSmallBuffer) { }
    }
}
