// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;
using System.Text;

namespace System.Json
{
    public class JsonPrimitive : JsonValue
    {
        private static readonly byte[] s_trueBytes = Encoding.UTF8.GetBytes("true");
        private static readonly byte[] s_falseBytes = Encoding.UTF8.GetBytes("false");
        private readonly object _value;

        public JsonPrimitive(bool value)
        {
            _value = value;
        }

        public JsonPrimitive(byte value)
        {
            _value = value;
        }

        public JsonPrimitive(char value)
        {
            _value = value;
        }

        public JsonPrimitive(decimal value)
        {
            _value = value;
        }

        public JsonPrimitive(double value)
        {
            _value = value;
        }

        public JsonPrimitive(float value)
        {
            _value = value;
        }

        public JsonPrimitive(int value)
        {
            _value = value;
        }

        public JsonPrimitive(long value)
        {
            _value = value;
        }

        [CLSCompliant(false)]
        public JsonPrimitive(sbyte value)
        {
            _value = value;
        }

        public JsonPrimitive(short value)
        {
            _value = value;
        }

        public JsonPrimitive(string value)
        {
            _value = value;
        }

        public JsonPrimitive(DateTime value)
        {
            _value = value;
        }

        [CLSCompliant(false)]
        public JsonPrimitive(uint value)
        {
            _value = value;
        }

        [CLSCompliant(false)]
        public JsonPrimitive(ulong value)
        {
            _value = value;
        }

        [CLSCompliant(false)]
        public JsonPrimitive(ushort value)
        {
            _value = value;
        }

        public JsonPrimitive(DateTimeOffset value)
        {
            _value = value;
        }

        public JsonPrimitive(Guid value)
        {
            _value = value;
        }

        public JsonPrimitive(TimeSpan value)
        {
            _value = value;
        }

        public JsonPrimitive(Uri value)
        {
            _value = value;
        }

        internal object Value => _value;

        public override JsonType JsonType =>
            _value == null || _value.GetType() == typeof(char) || _value.GetType() == typeof(string) || _value.GetType() == typeof(DateTime) || _value.GetType() == typeof(object) ? JsonType.String : // DateTimeOffset || Guid || TimeSpan || Uri
            _value.GetType() == typeof(bool) ? JsonType.Boolean :
            JsonType.Number;

        public override void Save(Stream stream)
        {
            switch (JsonType)
            {
                case JsonType.Boolean:
                    byte[] boolBytes = (bool)_value ? s_trueBytes : s_falseBytes;
                    stream.Write(boolBytes, 0, boolBytes.Length);
                    break;

                case JsonType.String:
                    stream.WriteByte((byte)'\"');
                    byte[] bytes = Encoding.UTF8.GetBytes(EscapeString(_value.ToString()));
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
                    if (_value is string || _value == null)
                    {
                        return (string)_value;
                    }
                    if (_value is char)
                    {
                        return _value.ToString();
                    }
                    throw new NotImplementedException(SR.Format(SR.NotImplemented_GetFormattedString, _value.GetType()));

                case JsonType.Number:
                    string s = _value is float || _value is double ?
                        ((IFormattable)_value).ToString("R", CultureInfo.InvariantCulture) : // Use "round-trip" format
                        ((IFormattable)_value).ToString("G", CultureInfo.InvariantCulture);
                    return s == "NaN" || s == "Infinity" || s == "-Infinity" ?
                        "\"" + s + "\"" :
                        s;

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
