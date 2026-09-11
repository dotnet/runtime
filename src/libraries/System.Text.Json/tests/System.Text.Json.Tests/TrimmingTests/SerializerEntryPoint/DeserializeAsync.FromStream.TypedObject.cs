// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.DeserializeAsync<T>(Stream utf8Json, JsonTypeInfo<T> jsonTypeInfo,
    /// CancellationToken cancellationToken) overload is trimming safe. A collection and a POCO are used.
    /// </summary>
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            string json = "[1]";
            using (MemoryStream stream = new(Encoding.UTF8.GetBytes(json)))
            {
                int[] arr = await JsonSerializer.DeserializeAsync(stream, Context.Default.Int32Array);
                if (arr is not [1])
                {
                    return -1;
                }
            }

            json = """{"X":1,"Y":2}""";
            using (MemoryStream stream = new(Encoding.UTF8.GetBytes(json)))
            {
                MyClassWithParameterizedCtor obj = await JsonSerializer.DeserializeAsync(stream, Context.Default.MyClassWithParameterizedCtor);
                if (obj is not { X: 1, Y: 2 })
                {
                    return -2;
                }
            }

            return 100;
        }
    }

    [JsonSerializable(typeof(int[]))]
    [JsonSerializable(typeof(MyClassWithParameterizedCtor))]
    internal partial class Context : JsonSerializerContext;
}
