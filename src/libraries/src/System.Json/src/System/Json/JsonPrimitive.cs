// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;
using System.Text;

namespace System.Json
{
    public class JsonPrimitive : JsonValue
    {
        object value;

        public JsonPrimitive(bool value)
        {
            this.value = value;
        }

        public JsonPrimitive(byte value)
        {
            this.value = value;
        }

        public JsonPrimitive(char value)
        {
            this.value = value;
        }

        public JsonPrimitive(decimal value)
        {
            this.value = value;
        }

        public JsonPrimitive(double value)
        {
            this.value = value;
        }

        public JsonPrimitive(float value)
        {
            this.value = value;
        }

        public JsonPrimitive(int value)
        {
            this.value = value;
        }

        public JsonPrimitive(long value)
        {
            this.value = value;
        }

        [CLSCompliant(false)]
        public JsonPrimitive(sbyte value)
        {
            this.value = value;
        }

        public JsonPrimitive(short value)
        {
            this.value = value;
        }

        public JsonPrimitive(string value)
        {
            this.value = value;
        }

        public JsonPrimitive(DateTime value)
        {
            this.value = value;
        }

        [CLSCompliant(false)]
        public JsonPrimitive(uint value)
        {
            this.value = value;
        }

        [CLSCompliant(false)]
        public JsonPrimitive(ulong value)
        {
            this.value = value;
        }

        [CLSCompliant(false)]
        public JsonPrimitive(ushort value)
        {
            this.value = value;
        }

        public JsonPrimitive(DateTimeOffset value)
        {
            this.value = value;
        }

        public JsonPrimitive(Guid value)
        {
            this.value = value;
        }

        public JsonPrimitive(TimeSpan value)
        {
            this.value = value;
        }

        public JsonPrimitive(Uri value)
        {
            this.value = value;
        }

        internal object Value
        {
            get { return value; }
        }

        public override JsonType JsonType
        {
            get
            {
                return
                    value == null || value.GetType() == typeof(char) || value.GetType() == typeof(string) || value.GetType() == typeof(DateTime) || value.GetType() == typeof(object) ? JsonType.String : // DateTimeOffset || Guid || TimeSpan || Uri
                    value.GetType() == typeof(bool) ? JsonType.Boolean :
                    JsonType.Number;
            }
        }

        private static readonly byte[] s_trueBytes = Encoding.UTF8.GetBytes("true");
        private static readonly byte[] s_falseBytes = Encoding.UTF8.GetBytes("false");

        public override void Save(Stream stream)
        {
            switch (JsonType)
            {
                case JsonType.Boolean:
                    if ((bool)value)
                        stream.Write(s_trueBytes, 0, 4);
                    else
                        stream.Write(s_falseBytes, 0, 5);
                    break;
                case JsonType.String:
                    stream.WriteByte((byte)'\"');
                    byte[] bytes = Encoding.UTF8.GetBytes(EscapeString(value.ToString()));
                    stream.Write(bytes, 0, bytes.Length);
                    stream.WriteByte((byte)'\"');
                    break;
                default:
                    bytes = Encoding.UTF8.GetBytes(GetFormattedString());
                    stream.Write(bytes, 0, bytes.Length);
                    break;
            }
        }

        internal string GetFormattedString()
        {
            switch (JsonType)
            {
                case JsonType.String:
                    if (value is string || value == null)
                        return (string)value;
                    if (value is char)
                        return value.ToString();
                    throw new NotImplementedException("GetFormattedString from value type " + value.GetType());
                case JsonType.Number:
                    string s;
                    if (value is float || value is double)
                        // Use "round-trip" format
                        s = ((IFormattable)value).ToString("R", NumberFormatInfo.InvariantInfo);
                    else
                        s = ((IFormattable)value).ToString("G", NumberFormatInfo.InvariantInfo);
                    if (s == "NaN" || s == "Infinity" || s == "-Infinity")
                        return "\"" + s + "\"";
                    else
                        return s;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
