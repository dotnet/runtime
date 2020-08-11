// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MyNamespace;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests.CodeGen
{
    public class SlowPathTests
    {
        /// <summary>
        /// When using a Stream, verify that the JsonTypeInfo falls back to standard converters
        /// instead of calling the generated Serialize and Deserialize methods.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public static async Task Stream()
        {
            const int Count = 100;

            var objs = new List<IndexViewModel>(Count);
            for (int i = 0; i < Count; i++)
            {
                objs.Add(IndexViewModelPocoTests.Create());
            }

            byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(objs);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = 1
            };

            JsonSerializerContext context = new JsonSerializerContext(options);

            JsonTypeInfo<List<IndexViewModel>> typeInfo = KnownCollectionTypeInfos<IndexViewModel>.
                GetList(JsonContext.Default.IndexViewModel, context);

            using (MemoryStream stream = new MemoryStream(utf8))
            {
                List<IndexViewModel> newObjs = await JsonSerializer.DeserializeAsync<List<IndexViewModel>>(
                    stream,
                    jsonTypeInfo: typeInfo);

                Assert.Equal(Count, newObjs.Count);
                for (int i = 0; i < objs.Count; i++)
                {
                    IndexViewModelPocoTests.Verify(objs[i], newObjs[i]);
                }
            }

            // todo: add SerializeAsync method
            //using (MemoryStream stream = new MemoryStream(utf8))
            //{
            //    await JsonSerializer.SerializeAsync(stream, typeInfo, newObjs);
            //    stream.Seek(0, SeekOrigin.Begin);
            //    var newUtf8 = new byte[stream.Length];
            //    int bytesRead = stream.Read(newUtf8, 0, newUtf8.Length);
            //    Assert.Equal(utf8, newUtf8);
            //}
        }
    }
}
