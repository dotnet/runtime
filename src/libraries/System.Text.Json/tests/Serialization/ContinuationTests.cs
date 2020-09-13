// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class ContinuationTests
    {
        [Fact]
        public static async Task ContinationAtNullShouldWork()
        {
            var stream = new MemoryStream();
            {
                var obj = new TestClass
                {
                    S = new string('x', 31),
                    P = "",
                    C = new()
                    {
                        S = "",
                        P = null,
                        C = null,
                    }
                };
                await JsonSerializer.SerializeAsync(stream, obj);
            }

            stream.Position = 0;
            {
                // {"S":"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx","P":"","C":{"S":"","P":null,"C":null}}
                //                                                                ^ continuation after 'u' (64 bytes)
                var readOptions = new JsonSerializerOptions
                {
                    DefaultBufferSize = 64,
                    IgnoreNullValues = true
                };

                var obj = await JsonSerializer.DeserializeAsync<TestClass>(stream, readOptions);

                Assert.Equal(new string('x', 31), obj.S);
                Assert.Equal("", obj.P);
                Assert.NotNull(obj.C);
                Assert.Equal("", obj.C.S);
                Assert.Null(obj.C.P);
                Assert.Null(obj.C.C);
            }
        }

        private class TestClass
        {
            public string S { get; set; }
            public string P { get; set; }
            public TestClass C { get; set; }
        }
    }
}
