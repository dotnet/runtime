// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// todo
    /// </summary>
    public partial class JsonSerializerContext
    {
        private JsonTypeInfo<bool>? _boolean;
        private static JsonTypeInfo<bool>? s_boolean;
        /// <summary>
        /// todo
        /// </summary>
        public JsonTypeInfo<bool> Boolean
        {
            get
            {
                if (_boolean == null)
                {
                    {
                        if (s_boolean == null)
                        {
                            s_boolean = new JsonValueInfo<bool>(new BooleanConverter(), _options);
                        }

                        _boolean = s_boolean;
                    }
                }

                return _boolean;
            }
        }

        private JsonTypeInfo<DateTimeOffset>? _dateTimeOffset;
        private static JsonTypeInfo<DateTimeOffset>? s_dateTimeOffset;
        /// <summary>
        /// todo
        /// </summary>
        public JsonTypeInfo<DateTimeOffset> DateTimeOffset
        {
            get
            {
                if (_dateTimeOffset == null)
                {
                    {
                        if (s_dateTimeOffset == null)
                        {
                            s_dateTimeOffset = new JsonValueInfo<DateTimeOffset>(new DateTimeOffsetConverter(), _options);
                        }

                        _dateTimeOffset = s_dateTimeOffset;
                    }
                }

                return _dateTimeOffset;
            }
        }

        private JsonTypeInfo<DateTime>? _dateTime;
        private static JsonTypeInfo<DateTime>? s_dateTime;
        /// <summary>
        /// todo
        /// </summary>
        public JsonTypeInfo<DateTime> DateTime
        {
            get
            {
                if (_dateTime == null)
                {
                    {
                        if (s_dateTime == null)
                        {
                            s_dateTime = new JsonValueInfo<DateTime>(new DateTimeConverter(), _options);
                        }

                        _dateTime = s_dateTime;
                    }
                }

                return _dateTime;
            }
        }

        private JsonTypeInfo<int>? _int32;
        private static JsonTypeInfo<int>? s_int32;
        /// <summary>
        /// todo
        /// </summary>
        public JsonTypeInfo<int> Int32
        {
            get
            {
                if (_int32 == null)
                {
                    {
                        if (s_int32 == null)
                        {
                            s_int32 = new JsonValueInfo<int>(new Int32Converter(), _options);
                        }

                        _int32 = s_int32;
                    }
                }

                return _int32;
            }
        }

        private JsonTypeInfo<long>? _int64;
        private static JsonTypeInfo<long>? s_int64;
        /// <summary>
        /// todo
        /// </summary>
        public JsonTypeInfo<long> Int64
        {
            get
            {
                if (_int64 == null)
                {
                    {
                        if (s_int64 == null)
                        {
                            s_int64 = new JsonValueInfo<long>(new Int64Converter(), _options);
                        }

                        _int64 = s_int64;
                    }
                }

                return _int64;
            }
        }

        private JsonTypeInfo<float>? _single;
        private static JsonTypeInfo<float>? s_single;
        /// <summary>
        /// todo
        /// </summary>
        public JsonTypeInfo<float> Single
        {
            get
            {
                if (_single == null)
                {
                    {
                        if (s_single == null)
                        {
                            s_single = new JsonValueInfo<float>(new SingleConverter(), _options);
                        }

                        _single = s_single;
                    }
                }

                return _single;
            }
        }

        private JsonTypeInfo<double>? _double;
        private static JsonTypeInfo<double>? s_double;
        /// <summary>
        /// todo
        /// </summary>
        public JsonTypeInfo<double> Double
        {
            get
            {
                if (_double == null)
                {
                    {
                        if (s_double == null)
                        {
                            s_double = new JsonValueInfo<double>(new DoubleConverter(), _options);
                        }

                        _double = s_double;
                    }
                }

                return _double;
            }
        }

        private JsonTypeInfo<char>? _char;
        private static JsonTypeInfo<char>? s_char;
        /// <summary>
        /// todo
        /// </summary>
        public JsonTypeInfo<char> Char
        {
            get
            {
                if (_char == null)
                {
                    {
                        if (s_char == null)
                        {
                            s_char = new JsonValueInfo<char>(new CharConverter(), _options);
                        }

                        _char = s_char;
                    }
                }

                return _char;
            }
        }

        private JsonTypeInfo<string>? _string;
        private static JsonTypeInfo<string>? s_string;
        /// <summary>
        /// todo
        /// </summary>
        public JsonTypeInfo<string> String
        {
            get
            {
                if (_string == null)
                {
                    {
                        if (s_string == null)
                        {
                            s_string = new JsonValueInfo<string>(new StringConverter(), _options);
                        }

                        _string = s_string;
                    }
                }

                return _string;
            }
        }
    }
}
