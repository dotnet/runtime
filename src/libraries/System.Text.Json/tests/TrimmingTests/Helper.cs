// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    internal static class TestHelper
    {
        /// <summary>
        /// Used when comparing JSON payloads with more than two properties.
        /// We cannot check for string equality since property ordering depends
        /// on reflection ordering which is not guaranteed.
        /// </summary>
        public static bool JsonEqual(string expected, string actual)
        {
            using JsonDocument expectedDom = JsonDocument.Parse(expected);
            using JsonDocument actualDom = JsonDocument.Parse(actual);
            return JsonEqual(expectedDom.RootElement, actualDom.RootElement);
        }

        private static bool JsonEqual(JsonElement expected, JsonElement actual)
        {
            JsonValueKind valueKind = expected.ValueKind;
            if (valueKind != actual.ValueKind)
            {
                return false;
            }

            switch (valueKind)
            {
                case JsonValueKind.Object:
                    var propertyNames = new HashSet<string>();

                    foreach (JsonProperty property in expected.EnumerateObject())
                    {
                        propertyNames.Add(property.Name);
                    }

                    foreach (JsonProperty property in actual.EnumerateObject())
                    {
                        propertyNames.Add(property.Name);
                    }

                    foreach (string name in propertyNames)
                    {
                        if (!JsonEqual(expected.GetProperty(name), actual.GetProperty(name)))
                        {
                            return false;
                        }
                    }

                    return true;
                case JsonValueKind.Array:
                    JsonElement.ArrayEnumerator expectedEnumerator = actual.EnumerateArray();
                    JsonElement.ArrayEnumerator actualEnumerator = expected.EnumerateArray();

                    while (expectedEnumerator.MoveNext())
                    {
                        if (!actualEnumerator.MoveNext())
                        {
                            return false;
                        }

                        if (!JsonEqual(expectedEnumerator.Current, actualEnumerator.Current))
                        {
                            return false;
                        }
                    }

                    return !actualEnumerator.MoveNext();
                case JsonValueKind.String:
                    return expected.GetString() == actual.GetString();
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    return expected.GetRawText() == actual.GetRawText();
                default:
                    throw new NotSupportedException($"Unexpected JsonValueKind: JsonValueKind.{valueKind}.");
            }
        }

        public static bool AssertCollectionAndSerialize<T>(object obj, string json)
        {
            return obj is T && JsonSerializer.Serialize(obj) == json;
        }
    }

    public class MyClass
    {
        public int X { get; set; }
        [JsonInclude]
        public int Y;
    }

    internal struct MyStruct
    {
        public int X { get; }
        [JsonInclude]
        public int Y;

        [JsonConstructor]
        public MyStruct(int x, int y) => (X, Y) = (x, y);
    }

    internal class MyClassWithParameterizedCtor
    {
        public int X { get; set; }
        [JsonInclude]
        public int Y;

        public MyClassWithParameterizedCtor(int x, int y) => (X, Y) = (x, y);
    }

    internal class MyBigClass
    {
        public string A { get; }
        [JsonInclude]
        public string B;
        public string C { get; }
        [JsonInclude]
        public int One;
        public int Two { get; }
        [JsonInclude]
        public int Three;

        public MyBigClass(string a, string b, string c, int one, int two, int three)
        {
            A = a;
            B = b;
            C = c;
            One = one;
            Two = two;
            Three = three;
        }
    }
}
