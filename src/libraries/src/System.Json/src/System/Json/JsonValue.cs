// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

using JsonPair = System.Collections.Generic.KeyValuePair<string, System.Json.JsonValue>;

namespace System.Json
{
    public abstract class JsonValue : IEnumerable
    {
        public static JsonValue Load(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return Load(new StreamReader(stream, true));
        }

        public static JsonValue Load(TextReader textReader)
        {
            if (textReader == null)
            {
                throw new ArgumentNullException(nameof(textReader));
            }

            return ToJsonValue(new JavaScriptReader(textReader).Read());
        }

        private static IEnumerable<KeyValuePair<string, JsonValue>> ToJsonPairEnumerable(IEnumerable<KeyValuePair<string, object>> kvpc)
        {
            foreach (KeyValuePair<string, object> kvp in kvpc)
            {
                yield return new KeyValuePair<string, JsonValue>(kvp.Key, ToJsonValue(kvp.Value));
            }
        }

        private static IEnumerable<JsonValue> ToJsonValueEnumerable(IEnumerable<object> arr)
        {
            foreach (object obj in arr)
            {
                yield return ToJsonValue(obj);
            }
        }

        private static JsonValue ToJsonValue(object ret)
        {
            if (ret == null)
            {
                return null;
            }

            var kvpc = ret as IEnumerable<KeyValuePair<string, object>>;
            if (kvpc != null)
            {
                return new JsonObject(ToJsonPairEnumerable(kvpc));
            }

            var arr = ret as IEnumerable<object>;
            if (arr != null)
            {
                return new JsonArray(ToJsonValueEnumerable(arr));
            }

            if (ret is bool) return new JsonPrimitive((bool)ret);
            if (ret is decimal) return new JsonPrimitive((decimal)ret);
            if (ret is double) return new JsonPrimitive((double)ret);
            if (ret is int) return new JsonPrimitive((int)ret);
            if (ret is long) return new JsonPrimitive((long)ret);
            if (ret is string) return new JsonPrimitive((string)ret);

            Debug.Assert(ret is ulong);
            return new JsonPrimitive((ulong)ret);
        }

        public static JsonValue Parse(string jsonString)
        {
            if (jsonString == null)
            {
                throw new ArgumentNullException(nameof(jsonString));
            }

            return Load(new StringReader(jsonString));
        }

        public virtual int Count
        {
            get { throw new InvalidOperationException(); }
        }

        public abstract JsonType JsonType { get; }

        public virtual JsonValue this[int index]
        {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException(); }
        }

        public virtual JsonValue this[string key]
        {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException(); }
        }

        public virtual bool ContainsKey(string key)
        {
            throw new InvalidOperationException();
        }

        public virtual void Save(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Save(new StreamWriter(stream));
        }

        public virtual void Save(TextWriter textWriter)
        {
            if (textWriter == null)
            {
                throw new ArgumentNullException(nameof(textWriter));
            }

            SaveInternal(textWriter);
        }

        private void SaveInternal(TextWriter w)
        {
            switch (JsonType)
            {
                case JsonType.Object:
                    w.Write('{');
                    bool following = false;
                    foreach (JsonPair pair in ((JsonObject)this))
                    {
                        if (following)
                        {
                            w.Write(", ");
                        }

                        w.Write('\"');
                        w.Write(EscapeString(pair.Key));
                        w.Write("\": ");
                        if (pair.Value == null)
                        {
                            w.Write("null");
                        }
                        else
                        {
                            pair.Value.SaveInternal(w);
                        }

                        following = true;
                    }
                    w.Write('}');
                    break;

                case JsonType.Array:
                    w.Write('[');
                    following = false;
                    foreach (JsonValue v in ((JsonArray)this))
                    {
                        if (following)
                        {
                            w.Write(", ");
                        }

                        if (v != null)
                        {
                            v.SaveInternal(w);
                        }
                        else
                        {
                            w.Write("null");
                        }

                        following = true;
                    }
                    w.Write(']');
                    break;

                case JsonType.Boolean:
                    w.Write(this ? "true" : "false");
                    break;

                case JsonType.String:
                    w.Write('"');
                    w.Write(EscapeString(((JsonPrimitive)this).GetFormattedString()));
                    w.Write('"');
                    break;

                default:
                    w.Write(((JsonPrimitive)this).GetFormattedString());
                    break;
            }
        }

        public override string ToString()
        {
            var sw = new StringWriter();
            Save(sw);
            return sw.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new InvalidOperationException();
        }

        // Characters which have to be escaped:
        // - Required by JSON Spec: Control characters, '"' and '\\'
        // - Broken surrogates to make sure the JSON string is valid Unicode
        //   (and can be encoded as UTF8)
        // - JSON does not require U+2028 and U+2029 to be escaped, but
        //   JavaScript does require this:
        //   http://stackoverflow.com/questions/2965293/javascript-parse-error-on-u2028-unicode-character/9168133#9168133
        // - '/' also does not have to be escaped, but escaping it when
        //   preceeded by a '<' avoids problems with JSON in HTML <script> tags
        private static bool NeedEscape(string src, int i)
        {
            char c = src[i];
            return c < 32 || c == '"' || c == '\\'
                // Broken lead surrogate
                || (c >= '\uD800' && c <= '\uDBFF' &&
                    (i == src.Length - 1 || src[i + 1] < '\uDC00' || src[i + 1] > '\uDFFF'))
                // Broken tail surrogate
                || (c >= '\uDC00' && c <= '\uDFFF' &&
                    (i == 0 || src[i - 1] < '\uD800' || src[i - 1] > '\uDBFF'))
                // To produce valid JavaScript
                || c == '\u2028' || c == '\u2029'
                // Escape "</" for <script> tags
                || (c == '/' && i > 0 && src[i - 1] == '<');
        }

        internal static string EscapeString(string src)
        {
            if (src != null)
            {
                for (int i = 0; i < src.Length; i++)
                {
                    if (NeedEscape(src, i))
                    {
                        var sb = new StringBuilder();
                        if (i > 0)
                        {
                            sb.Append(src, 0, i);
                        }
                        return DoEscapeString(sb, src, i);
                    }
                }
            }

            return src;
        }

        private static string DoEscapeString(StringBuilder sb, string src, int cur)
        {
            int start = cur;
            for (int i = cur; i < src.Length; i++)
                if (NeedEscape(src, i))
                {
                    sb.Append(src, start, i - start);
                    switch (src[i])
                    {
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        case '\"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '/': sb.Append("\\/"); break;
                        default:
                            sb.Append("\\u");
                            sb.Append(((int)src[i]).ToString("x04"));
                            break;
                    }
                    start = i + 1;
                }
            sb.Append(src, start, src.Length - start);
            return sb.ToString();
        }

        // CLI -> JsonValue

        public static implicit operator JsonValue(bool value) => new JsonPrimitive(value);

        public static implicit operator JsonValue(byte value) => new JsonPrimitive(value);

        public static implicit operator JsonValue(char value) => new JsonPrimitive(value);

        public static implicit operator JsonValue(decimal value) => new JsonPrimitive(value);

        public static implicit operator JsonValue(double value) => new JsonPrimitive(value);

        public static implicit operator JsonValue(float value) => new JsonPrimitive(value);

        public static implicit operator JsonValue(int value) => new JsonPrimitive(value);

        public static implicit operator JsonValue(long value) => new JsonPrimitive(value);

        [CLSCompliant(false)]
        public static implicit operator JsonValue(sbyte value) => new JsonPrimitive(value);

        public static implicit operator JsonValue(short value) => new JsonPrimitive(value);

        public static implicit operator JsonValue(string value) => new JsonPrimitive(value);

        [CLSCompliant(false)]
        public static implicit operator JsonValue(uint value) => new JsonPrimitive(value);

        [CLSCompliant(false)]
        public static implicit operator JsonValue(ulong value) => new JsonPrimitive(value);

        [CLSCompliant(false)]
        public static implicit operator JsonValue(ushort value) => new JsonPrimitive(value);

        public static implicit operator JsonValue(DateTime value) => new JsonPrimitive(value);

        public static implicit operator JsonValue(DateTimeOffset value) => new JsonPrimitive(value);

        public static implicit operator JsonValue(Guid value) => new JsonPrimitive(value);

        public static implicit operator JsonValue(TimeSpan value) => new JsonPrimitive(value);

        public static implicit operator JsonValue(Uri value) => new JsonPrimitive(value);

        // JsonValue -> CLI

        public static implicit operator bool (JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return Convert.ToBoolean(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
        }

        public static implicit operator byte (JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return Convert.ToByte(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
        }

        public static implicit operator char (JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return Convert.ToChar(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
        }

        public static implicit operator decimal (JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return Convert.ToDecimal(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
        }

        public static implicit operator double (JsonValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return Convert.ToDouble(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
        }

        public static implicit operator float (JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return Convert.ToSingle(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
        }

        public static implicit operator int (JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return Convert.ToInt32(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
        }

        public static implicit operator long (JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return Convert.ToInt64(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
        }

        [CLSCompliant(false)]
        public static implicit operator sbyte (JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return Convert.ToSByte(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
        }

        public static implicit operator short (JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return Convert.ToInt16(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
        }

        public static implicit operator string (JsonValue value)
        {
            return value != null ?
                (string)((JsonPrimitive)value).Value :
                null;
        }

        [CLSCompliant(false)]
        public static implicit operator uint (JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return Convert.ToUInt32(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
        }

        [CLSCompliant(false)]
        public static implicit operator ulong (JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return Convert.ToUInt64(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
        }

        [CLSCompliant(false)]
        public static implicit operator ushort (JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return Convert.ToUInt16(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
        }

        public static implicit operator DateTime(JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            
            return (DateTime)((JsonPrimitive)value).Value;
        }

        public static implicit operator DateTimeOffset(JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return (DateTimeOffset)((JsonPrimitive)value).Value;
        }

        public static implicit operator TimeSpan(JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return (TimeSpan)((JsonPrimitive)value).Value;
        }

        public static implicit operator Guid(JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return (Guid)((JsonPrimitive)value).Value;
        }

        public static implicit operator Uri(JsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return (Uri)((JsonPrimitive)value).Value;
        }
    }
}
