// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Text.Json
{
    internal static partial class JsonTestHelper
    {
        public static void AssertJsonEqual(string expected, string actual)
        {
            using JsonDocument expectedDom = JsonDocument.Parse(expected);
            using JsonDocument actualDom = JsonDocument.Parse(actual);
            AssertJsonEqual(expectedDom.RootElement, actualDom.RootElement);
        }

        private static void AssertJsonEqual(JsonElement expected, JsonElement actual)
        {
            JsonValueKind valueKind = expected.ValueKind;
            Assert.Equal(valueKind, actual.ValueKind);

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
                        AssertJsonEqual(expected.GetProperty(name), actual.GetProperty(name));
                    }
                    break;
                case JsonValueKind.Array:
                    JsonElement.ArrayEnumerator expectedEnumerator = actual.EnumerateArray();
                    JsonElement.ArrayEnumerator actualEnumerator = expected.EnumerateArray();

                    while (expectedEnumerator.MoveNext())
                    {
                        Assert.True(actualEnumerator.MoveNext());
                        AssertJsonEqual(expectedEnumerator.Current, actualEnumerator.Current);
                    }

                    Assert.False(actualEnumerator.MoveNext());
                    break;
                case JsonValueKind.String:
                    Assert.Equal(expected.GetString(), actual.GetString());
                    break;
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    Assert.Equal(expected.GetRawText(), actual.GetRawText());
                    break;
                default:
                    Debug.Fail($"Unexpected JsonValueKind: JsonValueKind.{valueKind}.");
                    break;
            }
        }
    }
}
