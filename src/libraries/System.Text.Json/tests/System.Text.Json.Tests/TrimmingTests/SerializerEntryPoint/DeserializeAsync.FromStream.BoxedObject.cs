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
    /// Tests that the serializer's JsonSerializer.DeserializeAsync(Stream utf8Json, Type returnType,
    /// JsonSerializerContext context, CancellationToken cancellationToken) overload is trimming safe.
    /// A collection and a POCO are used.
    /// </summary>
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            string json = "[1]";
            using (MemoryStream stream = new(Encoding.UTF8.GetBytes(json)))
            {
                int[] arr = (int[])await JsonSerializer.DeserializeAsync(stream, typeof(int[]), Context.Default);
                if (arr is not [1])
                {
                    return -1;
                }
            }

            json = """{"X":1,"Y":2}""";
            using (MemoryStream stream = new(Encoding.UTF8.GetBytes(json)))
            {
                var obj = (MyClassWithParameterizedCtor)await JsonSerializer.DeserializeAsync(stream, typeof(MyClassWithParameterizedCtor), Context.Default);
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
