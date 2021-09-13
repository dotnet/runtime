// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyModel
{
    internal static class Utf8JsonReaderExtensions
    {
        public static bool IsTokenTypeProperty(this ref Utf8JsonReader reader)
            => reader.TokenType == JsonTokenType.PropertyName;

        public static bool TryReadStringProperty(this ref Utf8JsonReader reader, out string? name, out string? value)
        {
            name = null;
            value = null;
            if (reader.Read() && reader.IsTokenTypeProperty())
            {
                name = reader.GetString();

                if (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        value = reader.GetString();
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

        public static void ReadStartObject(this ref Utf8JsonReader reader)
        {
            reader.Read();
            reader.CheckStartObject();
        }

        public static void CheckStartObject(this ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw CreateUnexpectedException(ref reader, "{");
            }
        }

        public static void CheckEndObject(this ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw CreateUnexpectedException(ref reader, "}");
            }
        }

        public static string?[] ReadStringArray(this ref Utf8JsonReader reader)
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw CreateUnexpectedException(ref reader, "[");
            }

            var items = new List<string?>();

            while (reader.Read() && reader.TokenType == JsonTokenType.String)
            {
                items.Add(reader.GetString());
            }

            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw CreateUnexpectedException(ref reader, "]");
            }

            return items.ToArray();
        }

        public static string? ReadAsString(this ref Utf8JsonReader reader)
        {
            Debug.Assert(reader.IsTokenTypeProperty());
            reader.Read();
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonTokenType.String)
            {
                throw CreateUnexpectedException(ref reader, "a JSON string token");
            }
            Debug.Assert(!reader.IsTokenTypeProperty());
            return reader.GetString();
        }

        public static bool? ReadAsNullableBoolean(this ref Utf8JsonReader reader)
        {
            Debug.Assert(reader.IsTokenTypeProperty());
            reader.Read();
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonTokenType.True && reader.TokenType != JsonTokenType.False)
            {
                throw CreateUnexpectedException(ref reader, "a JSON true or false literal token");
            }
            return reader.GetBoolean();
        }

        public static bool ReadAsBoolean(this ref Utf8JsonReader reader, bool defaultValue)
        {
            Debug.Assert(reader.IsTokenTypeProperty());
            reader.Read();
            if (reader.TokenType == JsonTokenType.Null)
            {
                return defaultValue;
            }
            if (reader.TokenType != JsonTokenType.True && reader.TokenType != JsonTokenType.False)
            {
                throw CreateUnexpectedException(ref reader, "a JSON true or false literal token");
            }
            return reader.GetBoolean();
        }

        private static Exception CreateUnexpectedException(ref Utf8JsonReader reader, string expected)
        {
            // Replace with public API once https://github.com/dotnet/runtime/issues/28482 is fixed
            object boxedState = reader.CurrentState;
            long lineNumber = (long)(typeof(JsonReaderState).GetField("_lineNumber", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(boxedState) ?? -1);
            long bytePositionInLine = (long)(typeof(JsonReaderState).GetField("_bytePositionInLine", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(boxedState) ?? -1);

            return new FormatException($"Unexpected character encountered, excepted '{expected}' " +
                                       $"at line {lineNumber} position {bytePositionInLine}");
        }
    }
}
