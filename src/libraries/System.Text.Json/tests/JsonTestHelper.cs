// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json.Tests;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Sdk;

namespace System.Text.Json
{
    internal static class JsonTestHelper
    {
        public static string NewtonsoftReturnStringHelper(TextReader reader)
        {
            var sb = new StringBuilder();
            var json = new JsonTextReader(reader);
            while (json.Read())
            {
                if (json.Value != null)
                {
                    // Use InvariantCulture to make sure numbers retain the decimal point '.'
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", json.Value);
                }
            }
            return sb.ToString();
        }

        public static string WriteDepthObject(int depth, bool writeComment = false)
        {
            var sb = new StringBuilder();
            var textWriter = new StringWriter();
            var json = new JsonTextWriter(textWriter);
            json.WriteStartObject();
            for (int i = 0; i < depth; i++)
            {
                json.WritePropertyName("message" + i);
                json.WriteStartObject();
            }
            if (writeComment)
                json.WriteComment("Random comment string");
            json.WritePropertyName("message" + depth);
            json.WriteValue("Hello, World!");
            for (int i = 0; i < depth; i++)
            {
                json.WriteEndObject();
            }
            json.WriteEndObject();
            json.Flush();

            return textWriter.ToString();
        }

        public static string WriteDepthArray(int depth, bool writeComment = false)
        {
            var sb = new StringBuilder();
            var textWriter = new StringWriter();
            var json = new JsonTextWriter(textWriter);
            json.WriteStartArray();
            for (int i = 0; i < depth; i++)
            {
                json.WriteStartArray();
            }
            if (writeComment)
                json.WriteComment("Random comment string");
            json.WriteValue("Hello, World!");
            for (int i = 0; i < depth; i++)
            {
                json.WriteEndArray();
            }
            json.WriteEndArray();
            json.Flush();

            return textWriter.ToString();
        }

        public static string WriteDepthObjectWithArray(int depth, bool writeComment = false)
        {
            var sb = new StringBuilder();
            var textWriter = new StringWriter();
            var json = new JsonTextWriter(textWriter);
            json.WriteStartObject();
            for (int i = 0; i < depth; i++)
            {
                json.WritePropertyName("message" + i);
                json.WriteStartObject();
            }
            if (writeComment)
                json.WriteComment("Random comment string");
            json.WritePropertyName("message" + depth);
            json.WriteStartArray();
            json.WriteValue("string1");
            json.WriteValue("string2");
            json.WriteEndArray();
            json.WritePropertyName("address");
            json.WriteStartObject();
            json.WritePropertyName("street");
            json.WriteValue("1 Microsoft Way");
            json.WritePropertyName("city");
            json.WriteValue("Redmond");
            json.WritePropertyName("zip");
            json.WriteValue(98052);
            json.WriteEndObject();
            for (int i = 0; i < depth; i++)
            {
                json.WriteEndObject();
            }
            json.WriteEndObject();
            json.Flush();

            return textWriter.ToString();
        }

        public static byte[] ReturnBytesHelper(byte[] data, out int length, JsonCommentHandling commentHandling = JsonCommentHandling.Disallow, int maxDepth = 64)
        {
            var state = new JsonReaderState(new JsonReaderOptions { CommentHandling = commentHandling, MaxDepth = maxDepth });
            var reader = new Utf8JsonReader(data, true, state);
            return ReaderLoop(data.Length, out length, ref reader);
        }

        public static byte[] SequenceReturnBytesHelper(byte[] data, out int length, JsonCommentHandling commentHandling = JsonCommentHandling.Disallow, int maxDepth = 64)
        {
            ReadOnlySequence<byte> sequence = CreateSegments(data);
            var state = new JsonReaderState(new JsonReaderOptions { CommentHandling = commentHandling, MaxDepth = maxDepth });
            var reader = new Utf8JsonReader(sequence, true, state);
            return ReaderLoop(data.Length, out length, ref reader);
        }

        public static ReadOnlySequence<byte> CreateSegments(byte[] data)
        {
            ReadOnlyMemory<byte> dataMemory = data;

            var firstSegment = new BufferSegment<byte>(dataMemory.Slice(0, data.Length / 2));
            ReadOnlyMemory<byte> secondMem = dataMemory.Slice(data.Length / 2);
            BufferSegment<byte> secondSegment = firstSegment.Append(secondMem);

            return new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, secondMem.Length);
        }

        public static ReadOnlySequence<byte> GetSequence(byte[] _dataUtf8, int segmentSize)
        {
            int numberOfSegments = _dataUtf8.Length / segmentSize + 1;
            byte[][] buffers = new byte[numberOfSegments][];

            for (int j = 0; j < numberOfSegments - 1; j++)
            {
                buffers[j] = new byte[segmentSize];
                Array.Copy(_dataUtf8, j * segmentSize, buffers[j], 0, segmentSize);
            }

            int remaining = _dataUtf8.Length % segmentSize;
            buffers[numberOfSegments - 1] = new byte[remaining];
            Array.Copy(_dataUtf8, _dataUtf8.Length - remaining, buffers[numberOfSegments - 1], 0, remaining);

            return BufferFactory.Create(buffers);
        }

        public static List<ReadOnlySequence<byte>> GetSequences(ReadOnlyMemory<byte> dataMemory)
        {
            var sequences = new List<ReadOnlySequence<byte>>
            {
                new ReadOnlySequence<byte>(dataMemory)
            };

            for (int i = 0; i < dataMemory.Length; i++)
            {
                var firstSegment = new BufferSegment<byte>(dataMemory.Slice(0, i));
                ReadOnlyMemory<byte> secondMem = dataMemory.Slice(i);
                BufferSegment<byte> secondSegment = firstSegment.Append(secondMem);
                var sequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, secondMem.Length);
                sequences.Add(sequence);
            }
            return sequences;
        }

        internal static ReadOnlySequence<byte> SegmentInto(ReadOnlyMemory<byte> data, int segmentCount)
        {
            if (segmentCount < 2)
                throw new ArgumentOutOfRangeException(nameof(segmentCount));

            int perSegment = data.Length / segmentCount;
            BufferSegment<byte> first;

            if (perSegment == 0 && data.Length > 0)
            {
                first = new BufferSegment<byte>(data.Slice(0, 1));
                data = data.Slice(1);
            }
            else
            {
                first = new BufferSegment<byte>(data.Slice(0, perSegment));
                data = data.Slice(perSegment);
            }

            BufferSegment<byte> last = first;
            segmentCount--;

            while (segmentCount > 1)
            {
                perSegment = data.Length / segmentCount;
                last = last.Append(data.Slice(0, perSegment));
                data = data.Slice(perSegment);
                segmentCount--;
            }

            last = last.Append(data);
            return new ReadOnlySequence<byte>(first, 0, last, data.Length);
        }

        public static object ReturnObjectHelper(byte[] data, JsonCommentHandling commentHandling = JsonCommentHandling.Disallow)
        {
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
            var reader = new Utf8JsonReader(data, true, state);
            return ReaderLoop(ref reader);
        }

        public static string ObjectToString(object jsonValues)
        {
            string s = "";
            if (jsonValues is List<object> jsonList)
                s = ListToString(jsonList);
            else if (jsonValues is Dictionary<string, object> jsonDictionary)
                s = DictionaryToString(jsonDictionary);
            return s;
        }

        public static string DictionaryToString(Dictionary<string, object> dictionary)
        {
            var builder = new StringBuilder();
            foreach (KeyValuePair<string, object> entry in dictionary)
            {
                if (entry.Value is Dictionary<string, object> nestedDictionary)
                    builder.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", entry.Key).Append(DictionaryToString(nestedDictionary));
                else if (entry.Value is List<object> nestedList)
                    builder.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", entry.Key).Append(ListToString(nestedList));
                else
                    builder.AppendFormat(CultureInfo.InvariantCulture, "{0}, {1}, ", entry.Key, entry.Value);
            }
            return builder.ToString();
        }

        public static string ListToString(List<object> list)
        {
            var builder = new StringBuilder();
            foreach (object entry in list)
            {
                if (entry is Dictionary<string, object> nestedDictionary)
                    builder.Append(DictionaryToString(nestedDictionary));
                else if (entry is List<object> nestedList)
                    builder.Append(ListToString(nestedList));
                else
                    builder.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", entry);
            }
            return builder.ToString();
        }

        private static JsonTokenType MapTokenType(JsonToken token)
        {
            switch (token)
            {
                case JsonToken.None:
                    return JsonTokenType.None;
                case JsonToken.StartObject:
                    return JsonTokenType.StartObject;
                case JsonToken.StartArray:
                    return JsonTokenType.StartArray;
                case JsonToken.PropertyName:
                    return JsonTokenType.PropertyName;
                case JsonToken.Comment:
                    return JsonTokenType.Comment;
                case JsonToken.Integer:
                case JsonToken.Float:
                    return JsonTokenType.Number;
                case JsonToken.String:
                    return JsonTokenType.String;
                case JsonToken.Boolean:
                    return JsonTokenType.True;
                case JsonToken.Null:
                    return JsonTokenType.Null;
                case JsonToken.EndObject:
                    return JsonTokenType.EndObject;
                case JsonToken.EndArray:
                    return JsonTokenType.EndArray;
                case JsonToken.StartConstructor:
                case JsonToken.EndConstructor:
                case JsonToken.Date:
                case JsonToken.Bytes:
                case JsonToken.Undefined:
                case JsonToken.Raw:
                default:
                    throw new InvalidOperationException();
            }
        }

        public static string InsertCommentsEverywhere(string jsonString)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                var newtonsoft = new JsonTextReader(new StringReader(jsonString));
                writer.WriteComment("comment");
                while (newtonsoft.Read())
                {
                    writer.WriteToken(newtonsoft, writeChildren: false);
                    writer.WriteComment("comment");
                }
                writer.WriteComment("comment");
            }
            return sb.ToString();
        }

        public static List<JsonTokenType> GetTokenTypes(string jsonString)
        {
            var newtonsoft = new JsonTextReader(new StringReader(jsonString));
            int totalReads = 0;
            while (newtonsoft.Read())
            {
                totalReads++;
            }

            var expectedTokenTypes = new List<JsonTokenType>();

            for (int i = 0; i < totalReads; i++)
            {
                newtonsoft = new JsonTextReader(new StringReader(jsonString));
                for (int j = 0; j < i; j++)
                {
                    Assert.True(newtonsoft.Read());
                }
                newtonsoft.Skip();
                expectedTokenTypes.Add(MapTokenType(newtonsoft.TokenType));
            }

            return expectedTokenTypes;
        }

        public static byte[] ReaderLoop(int inpuDataLength, out int length, ref Utf8JsonReader json)
        {
            byte[] outputArray = new byte[inpuDataLength];
            Span<byte> destination = outputArray;

            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;
                ReadOnlySpan<byte> valueSpan = json.HasValueSequence ? json.ValueSequence.ToArray() : json.ValueSpan;
                if (json.HasValueSequence)
                {
                    Assert.True(json.ValueSpan == default);
                    if ((tokenType != JsonTokenType.String && tokenType != JsonTokenType.PropertyName) || json.GetString().Length != 0)
                    {
                        // Empty strings could still make this true, i.e. ""
                        Assert.False(json.ValueSequence.IsEmpty);
                    }
                }
                else
                {
                    Assert.True(json.ValueSequence.IsEmpty);
                    if ((tokenType != JsonTokenType.String && tokenType != JsonTokenType.PropertyName) || json.GetString().Length != 0)
                    {
                        // Empty strings could still make this true, i.e. ""
                        Assert.False(json.ValueSpan == default);
                    }
                }
                switch (tokenType)
                {
                    case JsonTokenType.PropertyName:
                        valueSpan.CopyTo(destination);
                        destination[valueSpan.Length] = (byte)',';
                        destination[valueSpan.Length + 1] = (byte)' ';
                        destination = destination.Slice(valueSpan.Length + 2);
                        break;
                    case JsonTokenType.Number:
                    case JsonTokenType.String:
                    case JsonTokenType.Comment:
                        valueSpan.CopyTo(destination);
                        destination[valueSpan.Length] = (byte)',';
                        destination[valueSpan.Length + 1] = (byte)' ';
                        destination = destination.Slice(valueSpan.Length + 2);
                        break;
                    case JsonTokenType.True:
                        // Special casing True/False so that the casing matches with Json.NET
                        destination[0] = (byte)'T';
                        destination[1] = (byte)'r';
                        destination[2] = (byte)'u';
                        destination[3] = (byte)'e';
                        destination[valueSpan.Length] = (byte)',';
                        destination[valueSpan.Length + 1] = (byte)' ';
                        destination = destination.Slice(valueSpan.Length + 2);
                        break;
                    case JsonTokenType.False:
                        destination[0] = (byte)'F';
                        destination[1] = (byte)'a';
                        destination[2] = (byte)'l';
                        destination[3] = (byte)'s';
                        destination[4] = (byte)'e';
                        destination[valueSpan.Length] = (byte)',';
                        destination[valueSpan.Length + 1] = (byte)' ';
                        destination = destination.Slice(valueSpan.Length + 2);
                        break;
                    case JsonTokenType.Null:
                        // Special casing Null so that it matches what JSON.NET does
                        break;
                    case JsonTokenType.StartObject:
                        Assert.True(json.ValueSpan.SequenceEqual(new byte[] { (byte)'{' }));
                        Assert.True(json.ValueSequence.IsEmpty);
                        break;
                    case JsonTokenType.EndObject:
                        Assert.True(json.ValueSpan.SequenceEqual(new byte[] { (byte)'}' }));
                        Assert.True(json.ValueSequence.IsEmpty);
                        break;
                    case JsonTokenType.StartArray:
                        Assert.True(json.ValueSpan.SequenceEqual(new byte[] { (byte)'[' }));
                        Assert.True(json.ValueSequence.IsEmpty);
                        break;
                    case JsonTokenType.EndArray:
                        Assert.True(json.ValueSpan.SequenceEqual(new byte[] { (byte)']' }));
                        Assert.True(json.ValueSequence.IsEmpty);
                        break;
                    default:
                        break;
                }
            }
            length = outputArray.Length - destination.Length;
            return outputArray;
        }

        public static object ReaderLoop(ref Utf8JsonReader json)
        {
            object root = null;

            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;
                ReadOnlySpan<byte> valueSpan = json.ValueSpan;
                switch (tokenType)
                {
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                        root = valueSpan[0] == 't';
                        break;
                    case JsonTokenType.Number:
                        json.TryGetDouble(out double valueDouble);
                        root = valueDouble;
                        break;
                    case JsonTokenType.String:
                        string valueString = json.GetString();
                        root = valueString;
                        break;
                    case JsonTokenType.Null:
                        break;
                    case JsonTokenType.StartObject:
                        Assert.True(valueSpan.SequenceEqual(new byte[] { (byte)'{' }));
                        root = ReaderDictionaryLoop(ref json);
                        break;
                    case JsonTokenType.StartArray:
                        Assert.True(valueSpan.SequenceEqual(new byte[] { (byte)'[' }));
                        root = ReaderListLoop(ref json);
                        break;
                    case JsonTokenType.EndObject:
                        Assert.True(valueSpan.SequenceEqual(new byte[] { (byte)'}' }));
                        break;
                    case JsonTokenType.EndArray:
                        Assert.True(valueSpan.SequenceEqual(new byte[] { (byte)']' }));
                        break;
                    case JsonTokenType.None:
                    case JsonTokenType.Comment:
                    default:
                        break;
                }
            }
            return root;
        }

        public static Dictionary<string, object> ReaderDictionaryLoop(ref Utf8JsonReader json)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            string key = "";
            object value = null;

            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;
                ReadOnlySpan<byte> valueSpan = json.ValueSpan;
                switch (tokenType)
                {
                    case JsonTokenType.PropertyName:
                        key = json.GetString();
                        dictionary.Add(key, null);
                        break;
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                        value = valueSpan[0] == 't';
                        if (dictionary.TryGetValue(key, out _))
                        {
                            dictionary[key] = value;
                        }
                        else
                        {
                            dictionary.Add(key, value);
                        }
                        break;
                    case JsonTokenType.Number:
                        json.TryGetDouble(out double valueDouble);
                        if (dictionary.TryGetValue(key, out _))
                        {
                            dictionary[key] = valueDouble;
                        }
                        else
                        {
                            dictionary.Add(key, valueDouble);
                        }
                        break;
                    case JsonTokenType.String:
                        string valueString = json.GetString();
                        if (dictionary.TryGetValue(key, out _))
                        {
                            dictionary[key] = valueString;
                        }
                        else
                        {
                            dictionary.Add(key, valueString);
                        }
                        break;
                    case JsonTokenType.Null:
                        value = null;
                        if (dictionary.TryGetValue(key, out _))
                        {
                            dictionary[key] = value;
                        }
                        else
                        {
                            dictionary.Add(key, value);
                        }
                        break;
                    case JsonTokenType.StartObject:
                        Assert.True(valueSpan.SequenceEqual(new byte[] { (byte)'{' }));
                        value = ReaderDictionaryLoop(ref json);
                        if (dictionary.TryGetValue(key, out _))
                        {
                            dictionary[key] = value;
                        }
                        else
                        {
                            dictionary.Add(key, value);
                        }
                        break;
                    case JsonTokenType.StartArray:
                        Assert.True(valueSpan.SequenceEqual(new byte[] { (byte)'[' }));
                        value = ReaderListLoop(ref json);
                        if (dictionary.TryGetValue(key, out _))
                        {
                            dictionary[key] = value;
                        }
                        else
                        {
                            dictionary.Add(key, value);
                        }
                        break;
                    case JsonTokenType.EndObject:
                        Assert.True(valueSpan.SequenceEqual(new byte[] { (byte)'}' }));
                        return dictionary;
                    case JsonTokenType.None:
                    case JsonTokenType.Comment:
                    default:
                        break;
                }
            }
            return dictionary;
        }

        public static List<object> ReaderListLoop(ref Utf8JsonReader json)
        {
            List<object> arrayList = new List<object>();

            object value = null;

            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;
                ReadOnlySpan<byte> valueSpan = json.ValueSpan;
                switch (tokenType)
                {
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                        value = valueSpan[0] == 't';
                        arrayList.Add(value);
                        break;
                    case JsonTokenType.Number:
                        json.TryGetDouble(out double doubleValue);
                        arrayList.Add(doubleValue);
                        break;
                    case JsonTokenType.String:
                        string valueString = json.GetString();
                        arrayList.Add(valueString);
                        break;
                    case JsonTokenType.Null:
                        value = null;
                        arrayList.Add(value);
                        break;
                    case JsonTokenType.StartObject:
                        Assert.True(valueSpan.SequenceEqual(new byte[] { (byte)'{' }));
                        value = ReaderDictionaryLoop(ref json);
                        arrayList.Add(value);
                        break;
                    case JsonTokenType.StartArray:
                        Assert.True(valueSpan.SequenceEqual(new byte[] { (byte)'[' }));
                        value = ReaderListLoop(ref json);
                        arrayList.Add(value);
                        break;
                    case JsonTokenType.EndArray:
                        Assert.True(valueSpan.SequenceEqual(new byte[] { (byte)']' }));
                        return arrayList;
                    case JsonTokenType.None:
                    case JsonTokenType.Comment:
                    default:
                        break;
                }
            }
            return arrayList;
        }

        public static float NextFloat(Random random)
        {
            double mantissa = (random.NextDouble() * 2.0) - 1.0;
            double exponent = Math.Pow(2.0, random.Next(-126, 128));
            float value = (float)(mantissa * exponent);
            return value;
        }

        public static double NextDouble(Random random, double minValue, double maxValue)
        {
            double value = random.NextDouble() * (maxValue - minValue) + minValue;
            return value;
        }

        public static decimal NextDecimal(Random random, double minValue, double maxValue)
        {
            double value = random.NextDouble() * (maxValue - minValue) + minValue;
            return (decimal)value;
        }

        public static string GetCompactString(string jsonString)
        {
            using (JsonTextReader jsonReader = new JsonTextReader(new StringReader(jsonString)))
            {
                jsonReader.FloatParseHandling = FloatParseHandling.Decimal;
                JToken jtoken = JToken.ReadFrom(jsonReader);
                var stringWriter = new StringWriter();
                using (JsonTextWriter jsonWriter = new JsonTextWriter(stringWriter))
                {
                    jtoken.WriteTo(jsonWriter);
                    return stringWriter.ToString();
                }
            }
        }

        public delegate void AssertThrowsActionUtf8JsonReader(Utf8JsonReader json);

        // Cannot use standard Assert.Throws() when testing Utf8JsonReader - ref structs and closures don't get along.
        public static void AssertThrows<E>(Utf8JsonReader json, AssertThrowsActionUtf8JsonReader action) where E : Exception
        {
            Exception ex;

            try
            {
                action(json);
                ex = null;
            }
            catch (Exception e)
            {
                ex = e;
            }

            if (ex == null)
            {
                throw new ThrowsException(typeof(E));
            }

            if (!(ex is E))
            {
                throw new ThrowsException(typeof(E), ex);
            }
        }

        public delegate void AssertThrowsActionUtf8JsonWriter(ref Utf8JsonWriter writer);

        public static void AssertThrows<E>(
            ref Utf8JsonWriter writer,
            AssertThrowsActionUtf8JsonWriter action)
            where E : Exception
        {
            Exception ex;

            try
            {
                action(ref writer);
                ex = null;
            }
            catch (Exception e)
            {
                ex = e;
            }

            if (ex == null)
            {
                throw new ThrowsException(typeof(E));
            }

            if (ex.GetType() != typeof(E))
            {
                throw new ThrowsException(typeof(E), ex);
            }
        }

        private static readonly Regex s_stripWhitespace = new Regex(@"\s+", RegexOptions.Compiled);

        public static string StripWhitespace(this string value)
            => s_stripWhitespace.Replace(value, string.Empty);
    }
}
