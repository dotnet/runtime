// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Extensions.DependencyModel
{
    internal static class JsonTextReaderExtensions
    {
        internal static bool TryReadStringProperty(this JsonTextReader reader, out string name, out string value)
        {
            name = null;
            value = null;
            if (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                name = (string)reader.Value;
                
                if (reader.Read())
                {
                    if (reader.TokenType == JsonToken.String)
                    {
                        value = (string)reader.Value;
                    }
                    else
                    {
                        reader.Skip();
                    }
                }

                return true;
            }

            return false;
        }

        internal static void ReadStartObject(this JsonTextReader reader)
        {
            reader.Read();
            CheckStartObject(reader);
        }

        internal static void CheckStartObject(this JsonTextReader reader)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                throw CreateUnexpectedException(reader, "{");
            }
        }

        internal static void CheckEndObject(this JsonTextReader reader)
        {
            if (reader.TokenType != JsonToken.EndObject)
            {
                throw CreateUnexpectedException(reader, "}");
            }
        }

        internal static string[] ReadStringArray(this JsonTextReader reader)
        {
            reader.Read();
            if (reader.TokenType != JsonToken.StartArray)
            {
                throw CreateUnexpectedException(reader,"[");
            }

            var items = new List<string>();

            while (reader.Read() && reader.TokenType == JsonToken.String)
            {
                items.Add((string)reader.Value);
            }

            if (reader.TokenType != JsonToken.EndArray)
            {
                throw CreateUnexpectedException(reader, "]");
            }

            return items.ToArray();
        }

        internal static Exception CreateUnexpectedException(JsonTextReader reader, string expected)
        {
            return new FormatException($"Unexpected character encountered, excepted '{expected}' " +
                                       $"at line {reader.LineNumber} position {reader.LinePosition} path {reader.Path}");
        }
    }
}
