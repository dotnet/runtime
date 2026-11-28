// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public sealed partial class NullableAnnotationsTests_String : NullableAnnotationsTests
    {
        public NullableAnnotationsTests_String() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public sealed partial class NullableAnnotationsTests_AsyncStreamWithSmallBuffer : NullableAnnotationsTests
    {
        public NullableAnnotationsTests_AsyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.AsyncStreamSerializerWithSmallBuffer) { }

        [Fact]
        public async Task ReadNullIntoNotNullableNonCtorProperty_ParameterizedCtor_PipeReader_ThrowsJsonException()
        {
            JsonSerializerOptions options = new(JsonSerializerDefaults.Strict);
            byte[] json = """{"CtorProperty":"a","Property":null}"""u8.ToArray();

            JsonException ex = await Assert.ThrowsAsync<JsonException>(async () =>
                await JsonSerializer.DeserializeAsync<NotNullableNonCtorMembersParameterizedCtorClass>(
                    PipeReader.Create(new MemoryStream(json)),
                    options));

            Assert.Contains(nameof(NotNullableNonCtorMembersParameterizedCtorClass.Property), ex.Message);
            Assert.Equal("$.Property", ex.Path);
        }
    }
}
