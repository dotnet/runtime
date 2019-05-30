// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json
{
    /// <summary>
    ///   Represents a specific JSON value within a <see cref="JsonDocument"/>.
    /// </summary>
    public readonly partial struct JsonElement
    {
        private readonly JsonDocument _parent;
        private readonly int _idx;

        internal JsonElement(JsonDocument parent, int idx)
        {
            // parent is usually not null, but the Current property
            // on the enumerators (when initialized as `default`) can
            // get here with a null.
            Debug.Assert(idx >= 0);

            _parent = parent;
            _idx = idx;
        }

        private JsonTokenType TokenType => _parent?.GetJsonTokenType(_idx) ?? JsonTokenType.None;

        /// <summary>
        ///   The <see cref="JsonValueType"/> that the value is.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public JsonValueType Type => TokenType.ToValueType();

        /// <summary>
        ///   Get the value at a specified index when the current value is a
        ///   <see cref="JsonValueType.Array"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Array"/>.
        /// </exception>
        /// <exception cref="IndexOutOfRangeException">
        ///   <paramref name="index"/> is not in the range [0, <see cref="GetArrayLength"/>()).
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public JsonElement this[int index]
        {
            get
            {
                CheckValidInstance();

                return _parent.GetArrayIndexElement(_idx, index);
            }
        }

        /// <summary>
        ///   Get the number of values contained within the current array value.
        /// </summary>
        /// <returns>The number of values contained within the current array value.</returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Array"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public int GetArrayLength()
        {
            CheckValidInstance();

            return _parent.GetArrayLength(_idx);
        }

        /// <summary>
        ///   Gets a <see cref="JsonElement"/> representing the value of a required property identified
        ///   by <paramref name="propertyName"/>.
        /// </summary>
        /// <remarks>
        ///   Property name matching is performed as an ordinal, case-sensitive, comparison.
        ///
        ///   If a property is defined multiple times for the same object, the last such definition is
        ///   what is matched.
        /// </remarks>
        /// <param name="propertyName">Name of the property whose value to return.</param>
        /// <returns>
        ///   A <see cref="JsonElement"/> representing the value of the requested property.
        /// </returns>
        /// <seealso cref="EnumerateObject"/>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Object"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        ///   No property was found with the requested name.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public JsonElement GetProperty(string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            if (TryGetProperty(propertyName, out JsonElement property))
            {
                return property;
            }

            throw new KeyNotFoundException();
        }

        /// <summary>
        ///   Gets a <see cref="JsonElement"/> representing the value of a required property identified
        ///   by <paramref name="propertyName"/>.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Property name matching is performed as an ordinal, case-sensitive, comparison.
        ///   </para>
        ///
        ///   <para>
        ///     If a property is defined multiple times for the same object, the last such definition is
        ///     what is matched.
        ///   </para>
        /// </remarks>
        /// <param name="propertyName">Name of the property whose value to return.</param>
        /// <returns>
        ///   A <see cref="JsonElement"/> representing the value of the requested property.
        /// </returns>
        /// <seealso cref="EnumerateObject"/>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Object"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        ///   No property was found with the requested name.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public JsonElement GetProperty(ReadOnlySpan<char> propertyName)
        {
            if (TryGetProperty(propertyName, out JsonElement property))
            {
                return property;
            }

            throw new KeyNotFoundException();
        }

        /// <summary>
        ///   Gets a <see cref="JsonElement"/> representing the value of a required property identified
        ///   by <paramref name="utf8PropertyName"/>.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Property name matching is performed as an ordinal, case-sensitive, comparison.
        ///   </para>
        ///
        ///   <para>
        ///     If a property is defined multiple times for the same object, the last such definition is
        ///     what is matched.
        ///   </para>
        /// </remarks>
        /// <param name="utf8PropertyName">
        ///   The UTF-8 (with no Byte-Order-Mark (BOM)) representation of the name of the property to return.
        /// </param>
        /// <returns>
        ///   A <see cref="JsonElement"/> representing the value of the requested property.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Object"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        ///   No property was found with the requested name.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        /// <seealso cref="EnumerateObject"/>
        public JsonElement GetProperty(ReadOnlySpan<byte> utf8PropertyName)
        {
            if (TryGetProperty(utf8PropertyName, out JsonElement property))
            {
                return property;
            }

            throw new KeyNotFoundException();
        }

        /// <summary>
        ///   Looks for a property named <paramref name="propertyName"/> in the current object, returning
        ///   whether or not such a property existed. When the property exists <paramref name="value"/>
        ///   is assigned to the value of that property.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Property name matching is performed as an ordinal, case-sensitive, comparison.
        ///   </para>
        ///
        ///   <para>
        ///     If a property is defined multiple times for the same object, the last such definition is
        ///     what is matched.
        ///   </para>
        /// </remarks>
        /// <param name="propertyName">Name of the property to find.</param>
        /// <param name="value">Receives the value of the located property.</param>
        /// <returns>
        ///   <see langword="true"/> if the property was found, <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Object"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        /// <seealso cref="EnumerateObject"/>
        public bool TryGetProperty(string propertyName, out JsonElement value)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            return TryGetProperty(propertyName.AsSpan(), out value);
        }

        /// <summary>
        ///   Looks for a property named <paramref name="propertyName"/> in the current object, returning
        ///   whether or not such a property existed. When the property exists <paramref name="value"/>
        ///   is assigned to the value of that property.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Property name matching is performed as an ordinal, case-sensitive, comparison.
        ///   </para>
        ///
        ///   <para>
        ///     If a property is defined multiple times for the same object, the last such definition is
        ///     what is matched.
        ///   </para>
        /// </remarks>
        /// <param name="propertyName">Name of the property to find.</param>
        /// <param name="value">Receives the value of the located property.</param>
        /// <returns>
        ///   <see langword="true"/> if the property was found, <see langword="false"/> otherwise.
        /// </returns>
        /// <seealso cref="EnumerateObject"/>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Object"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public bool TryGetProperty(ReadOnlySpan<char> propertyName, out JsonElement value)
        {
            CheckValidInstance();

            return _parent.TryGetNamedPropertyValue(_idx, propertyName, out value);
        }

        /// <summary>
        ///   Looks for a property named <paramref name="utf8PropertyName"/> in the current object, returning
        ///   whether or not such a property existed. When the property exists <paramref name="value"/>
        ///   is assigned to the value of that property.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Property name matching is performed as an ordinal, case-sensitive, comparison.
        ///   </para>
        ///
        ///   <para>
        ///     If a property is defined multiple times for the same object, the last such definition is
        ///     what is matched.
        ///   </para>
        /// </remarks>
        /// <param name="utf8PropertyName">
        ///   The UTF-8 (with no Byte-Order-Mark (BOM)) representation of the name of the property to return.
        /// </param>
        /// <param name="value">Receives the value of the located property.</param>
        /// <returns>
        ///   <see langword="true"/> if the property was found, <see langword="false"/> otherwise.
        /// </returns>
        /// <seealso cref="EnumerateObject"/>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Object"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public bool TryGetProperty(ReadOnlySpan<byte> utf8PropertyName, out JsonElement value)
        {
            CheckValidInstance();

            return _parent.TryGetNamedPropertyValue(_idx, utf8PropertyName, out value);
        }

        /// <summary>
        ///   Gets the value of the element as a <see cref="bool"/>.
        /// </summary>
        /// <remarks>
        ///   This method does not parse the contents of a JSON string value.
        /// </remarks>
        /// <returns>The value of the element as a <see cref="bool"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is neither <see cref="JsonValueType.True"/> or
        ///   <see cref="JsonValueType.False"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public bool GetBoolean()
        {
            // CheckValidInstance is redundant.  Asking for the type will
            // return None, which then throws the same exception in the return statement.

            JsonTokenType type = TokenType;

            return
                type == JsonTokenType.True ? true :
                type == JsonTokenType.False ? false :
                throw ThrowHelper.GetJsonElementWrongTypeException(nameof(Boolean), type);
        }

        /// <summary>
        ///   Gets the value of the element as a <see cref="string"/>.
        /// </summary>
        /// <remarks>
        ///   This method does not create a string representation of values other than JSON strings.
        /// </remarks>
        /// <returns>The value of the element as a <see cref="string"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is neither <see cref="JsonValueType.String"/> nor <see cref="JsonValueType.Null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        /// <seealso cref="ToString"/>
        public string GetString()
        {
            CheckValidInstance();

            return _parent.GetString(_idx, JsonTokenType.String);
        }

        /// <summary>
        ///   Attempts to represent the current JSON number as an <see cref="int"/>.
        /// </summary>
        /// <param name="value">Receives the value.</param>
        /// <remarks>
        ///   This method does not parse the contents of a JSON string value.
        /// </remarks>
        /// <returns>
        ///   <see langword="true"/> if the number can be represented as an <see cref="int"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Number"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public bool TryGetInt32(out int value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        /// <summary>
        ///   Gets the current JSON number as an <see cref="int"/>.
        /// </summary>
        /// <returns>The current JSON number as an <see cref="int"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Number"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   The value cannot be represented as an <see cref="int"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public int GetInt32()
        {
            if (TryGetInt32(out int value))
            {
                return value;
            }

            throw new FormatException();
        }

        /// <summary>
        ///   Attempts to represent the current JSON number as a <see cref="uint"/>.
        /// </summary>
        /// <param name="value">Receives the value.</param>
        /// <remarks>
        ///   This method does not parse the contents of a JSON string value.
        /// </remarks>
        /// <returns>
        ///   <see langword="true"/> if the number can be represented as a <see cref="uint"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Number"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        [CLSCompliant(false)]
        public bool TryGetUInt32(out uint value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        /// <summary>
        ///   Gets the current JSON number as a <see cref="uint"/>.
        /// </summary>
        /// <returns>The current JSON number as a <see cref="uint"/>.</returns>
        /// <remarks>
        ///   This method does not parse the contents of a JSON string value.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Number"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   The value cannot be represented as a <see cref="uint"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        [CLSCompliant(false)]
        public uint GetUInt32()
        {
            if (TryGetUInt32(out uint value))
            {
                return value;
            }

            throw new FormatException();
        }

        /// <summary>
        ///   Attempts to represent the current JSON number as a <see cref="long"/>.
        /// </summary>
        /// <param name="value">Receives the value.</param>
        /// <remarks>
        ///   This method does not parse the contents of a JSON string value.
        /// </remarks>
        /// <returns>
        ///   <see langword="true"/> if the number can be represented as a <see cref="long"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Number"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public bool TryGetInt64(out long value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        /// <summary>
        ///   Gets the current JSON number as a <see cref="long"/>.
        /// </summary>
        /// <returns>The current JSON number as a <see cref="long"/>.</returns>
        /// <remarks>
        ///   This method does not parse the contents of a JSON string value.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Number"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   The value cannot be represented as a <see cref="long"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public long GetInt64()
        {
            if (TryGetInt64(out long value))
            {
                return value;
            }

            throw new FormatException();
        }

        /// <summary>
        ///   Attempts to represent the current JSON number as a <see cref="ulong"/>.
        /// </summary>
        /// <param name="value">Receives the value.</param>
        /// <remarks>
        ///   This method does not parse the contents of a JSON string value.
        /// </remarks>
        /// <returns>
        ///   <see langword="true"/> if the number can be represented as a <see cref="ulong"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Number"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        [CLSCompliant(false)]
        public bool TryGetUInt64(out ulong value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        /// <summary>
        ///   Gets the current JSON number as a <see cref="ulong"/>.
        /// </summary>
        /// <returns>The current JSON number as a <see cref="ulong"/>.</returns>
        /// <remarks>
        ///   This method does not parse the contents of a JSON string value.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Number"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   The value cannot be represented as a <see cref="ulong"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        [CLSCompliant(false)]
        public ulong GetUInt64()
        {
            if (TryGetUInt64(out ulong value))
            {
                return value;
            }

            throw new FormatException();
        }

        /// <summary>
        ///   Attempts to represent the current JSON number as a <see cref="double"/>.
        /// </summary>
        /// <param name="value">Receives the value.</param>
        /// <remarks>
        ///   <para>
        ///     This method does not parse the contents of a JSON string value.
        ///   </para>
        ///
        ///   <para>
        ///     On .NET Core this method does not return <see langword="false"/> for values larger than
        ///     <see cref="double.MaxValue"/> (or smaller than <see cref="double.MinValue"/>),
        ///     instead <see langword="true"/> is returned and <see cref="double.PositiveInfinity"/> (or
        ///     <see cref="double.NegativeInfinity"/>) is emitted.
        ///   </para>
        /// </remarks>
        /// <returns>
        ///   <see langword="true"/> if the number can be represented as a <see cref="double"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Number"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public bool TryGetDouble(out double value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        /// <summary>
        ///   Gets the current JSON number as a <see cref="double"/>.
        /// </summary>
        /// <returns>The current JSON number as a <see cref="double"/>.</returns>
        /// <remarks>
        ///   <para>
        ///     This method does not parse the contents of a JSON string value.
        ///   </para>
        ///
        ///   <para>
        ///     On .NET Core this method returns <see cref="double.PositiveInfinity"/> (or
        ///     <see cref="double.NegativeInfinity"/>) for values larger than
        ///     <see cref="double.MaxValue"/> (or smaller than <see cref="double.MinValue"/>).
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Number"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   The value cannot be represented as a <see cref="double"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public double GetDouble()
        {
            if (TryGetDouble(out double value))
            {
                return value;
            }

            throw new FormatException();
        }

        /// <summary>
        ///   Attempts to represent the current JSON number as a <see cref="float"/>.
        /// </summary>
        /// <param name="value">Receives the value.</param>
        /// <remarks>
        ///   <para>
        ///     This method does not parse the contents of a JSON string value.
        ///   </para>
        ///
        ///   <para>
        ///     On .NET Core this method does not return <see langword="false"/> for values larger than
        ///     <see cref="float.MaxValue"/> (or smaller than <see cref="float.MinValue"/>),
        ///     instead <see langword="true"/> is returned and <see cref="float.PositiveInfinity"/> (or
        ///     <see cref="float.NegativeInfinity"/>) is emitted.
        ///   </para>
        /// </remarks>
        /// <returns>
        ///   <see langword="true"/> if the number can be represented as a <see cref="float"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Number"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public bool TryGetSingle(out float value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        /// <summary>
        ///   Gets the current JSON number as a <see cref="float"/>.
        /// </summary>
        /// <returns>The current JSON number as a <see cref="float"/>.</returns>
        /// <remarks>
        ///   <para>
        ///     This method does not parse the contents of a JSON string value.
        ///   </para>
        ///
        ///   <para>
        ///     On .NET Core this method returns <see cref="float.PositiveInfinity"/> (or
        ///     <see cref="float.NegativeInfinity"/>) for values larger than
        ///     <see cref="float.MaxValue"/> (or smaller than <see cref="float.MinValue"/>).
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Number"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   The value cannot be represented as a <see cref="float"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public float GetSingle()
        {
            if (TryGetSingle(out float value))
            {
                return value;
            }

            throw new FormatException();
        }

        /// <summary>
        ///   Attempts to represent the current JSON number as a <see cref="decimal"/>.
        /// </summary>
        /// <param name="value">Receives the value.</param>
        /// <remarks>
        ///   This method does not parse the contents of a JSON string value.
        /// </remarks>
        /// <returns>
        ///   <see langword="true"/> if the number can be represented as a <see cref="decimal"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Number"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        /// <seealso cref="GetRawText"/>
        public bool TryGetDecimal(out decimal value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        /// <summary>
        ///   Gets the current JSON number as a <see cref="decimal"/>.
        /// </summary>
        /// <returns>The current JSON number as a <see cref="decimal"/>.</returns>
        /// <remarks>
        ///   This method does not parse the contents of a JSON string value.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Number"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   The value cannot be represented as a <see cref="decimal"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        /// <seealso cref="GetRawText"/>
        public decimal GetDecimal()
        {
            if (TryGetDecimal(out decimal value))
            {
                return value;
            }

            throw new FormatException();
        }

        /// <summary>
        ///   Attempts to represent the current JSON string as a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="value">Receives the value.</param>
        /// <remarks>
        ///   This method does not create a DateTime representation of values other than JSON strings.
        /// </remarks>
        /// <returns>
        ///   <see langword="true"/> if the string can be represented as a <see cref="DateTime"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.String"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public bool TryGetDateTime(out DateTime value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        /// <summary>
        ///   Gets the value of the element as a <see cref="DateTime"/>.
        /// </summary>
        /// <remarks>
        ///   This method does not create a DateTime representation of values other than JSON strings.
        /// </remarks>
        /// <returns>The value of the element as a <see cref="DateTime"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.String"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   The value cannot be represented as a <see cref="DateTime"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        /// <seealso cref="ToString"/>
        public DateTime GetDateTime()
        {
            if (TryGetDateTime(out DateTime value))
            {
                return value;
            }

            throw new FormatException();
        }

        /// <summary>
        ///   Attempts to represent the current JSON string as a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="value">Receives the value.</param>
        /// <remarks>
        ///   This method does not create a DateTimeOffset representation of values other than JSON strings.
        /// </remarks>
        /// <returns>
        ///   <see langword="true"/> if the string can be represented as a <see cref="DateTimeOffset"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.String"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public bool TryGetDateTimeOffset(out DateTimeOffset value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        /// <summary>
        ///   Gets the value of the element as a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <remarks>
        ///   This method does not create a DateTimeOffset representation of values other than JSON strings.
        /// </remarks>
        /// <returns>The value of the element as a <see cref="DateTimeOffset"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.String"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   The value cannot be represented as a <see cref="DateTimeOffset"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        /// <seealso cref="ToString"/>
        public DateTimeOffset GetDateTimeOffset()
        {
            if (TryGetDateTimeOffset(out DateTimeOffset value))
            {
                return value;
            }

            throw new FormatException();
        }

        /// <summary>
        ///   Attempts to represent the current JSON string as a <see cref="Guid"/>.
        /// </summary>
        /// <param name="value">Receives the value.</param>
        /// <remarks>
        ///   This method does not create a Guid representation of values other than JSON strings.
        /// </remarks>
        /// <returns>
        ///   <see langword="true"/> if the string can be represented as a <see cref="Guid"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.String"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public bool TryGetGuid(out Guid value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        /// <summary>
        ///   Gets the value of the element as a <see cref="Guid"/>.
        /// </summary>
        /// <remarks>
        ///   This method does not create a Guid representation of values other than JSON strings.
        /// </remarks>
        /// <returns>The value of the element as a <see cref="Guid"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.String"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   The value cannot be represented as a <see cref="Guid"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        /// <seealso cref="ToString"/>
        public Guid GetGuid()
        {
            if (TryGetGuid(out Guid value))
            {
                return value;
            }

            throw new FormatException();
        }

        internal string GetPropertyName()
        {
            CheckValidInstance();

            return _parent.GetNameOfPropertyValue(_idx);
        }

        /// <summary>
        ///   Gets the original input data backing this value, returning it as a <see cref="string"/>.
        /// </summary>
        /// <returns>
        ///  The original input data backing this value, returning it as a <see cref="string"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public string GetRawText()
        {
            CheckValidInstance();

            return _parent.GetRawValueAsString(_idx);
        }

        internal string GetPropertyRawText()
        {
            CheckValidInstance();

            return _parent.GetPropertyRawValueAsString(_idx);
        }

        /// <summary>
        ///   Write the element into the provided writer as a named object property.
        /// </summary>
        /// <param name="propertyName">The name for this value within the JSON object.</param>
        /// <param name="writer">The writer.</param>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is <see cref="JsonValueType.Undefined"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public void WriteAsProperty(string propertyName, Utf8JsonWriter writer)
            => WriteAsProperty(propertyName.AsSpan(), writer);

        /// <summary>
        ///   Write the element into the provided writer as a named object property.
        /// </summary>
        /// <param name="propertyName">The name for this value within the JSON object.</param>
        /// <param name="writer">The writer.</param>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is <see cref="JsonValueType.Undefined"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public void WriteAsProperty(ReadOnlySpan<char> propertyName, Utf8JsonWriter writer)
        {
            CheckValidInstance();

            _parent.WriteElementTo(_idx, writer, propertyName);
        }

        /// <summary>
        ///   Write the element into the provided writer as a named object property.
        /// </summary>
        /// <param name="utf8PropertyName">
        ///   The name for this value within the JSON object, as UTF-8 text.
        /// </param>
        /// <param name="writer">The writer.</param>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is <see cref="JsonValueType.Undefined"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public void WriteAsProperty(ReadOnlySpan<byte> utf8PropertyName, Utf8JsonWriter writer)
        {
            CheckValidInstance();

            _parent.WriteElementTo(_idx, writer, utf8PropertyName);
        }

        /// <summary>
        ///   Write the element into the provided writer as a value.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is <see cref="JsonValueType.Undefined"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public void WriteAsValue(Utf8JsonWriter writer)
        {
            CheckValidInstance();

            _parent.WriteElementTo(_idx, writer);
        }

        /// <summary>
        ///   Get an enumerator to enumerate the values in the JSON array represented by this JsonElement.
        /// </summary>
        /// <returns>
        ///   An enumerator to enumerate the values in the JSON array represented by this JsonElement.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Array"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public ArrayEnumerator EnumerateArray()
        {
            CheckValidInstance();

            JsonTokenType tokenType = TokenType;

            if (tokenType != JsonTokenType.StartArray)
            {
                throw ThrowHelper.GetJsonElementWrongTypeException(JsonTokenType.StartArray, tokenType);
            }

            return new ArrayEnumerator(this);
        }

        /// <summary>
        ///   Get an enumerator to enumerate the properties in the JSON object represented by this JsonElement.
        /// </summary>
        /// <returns>
        ///   An enumerator to enumerate the properties in the JSON object represented by this JsonElement.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonValueType.Object"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public ObjectEnumerator EnumerateObject()
        {
            CheckValidInstance();

            JsonTokenType tokenType = TokenType;

            if (tokenType != JsonTokenType.StartObject)
            {
                throw ThrowHelper.GetJsonElementWrongTypeException(JsonTokenType.StartObject, tokenType);
            }

            return new ObjectEnumerator(this);
        }

        /// <summary>
        ///   Gets a string representation for the current value appropriate to the value type.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     For <see cref="JsonValueType.Null"/>, <see cref="string.Empty"/> is returned.
        ///   </para>
        ///
        ///   <para>
        ///     For <see cref="JsonValueType.True"/>, <see cref="bool.TrueString"/> is returned.
        ///   </para>
        ///
        ///   <para>
        ///     For <see cref="JsonValueType.False"/>, <see cref="bool.FalseString"/> is returned.
        ///   </para>
        /// 
        ///   <para>
        ///     For <see cref="JsonValueType.String"/>, the value of <see cref="GetString"/>() is returned.
        ///   </para>
        ///
        ///   <para>
        ///     For other types, the value of <see cref="GetRawText"/>() is returned.
        ///   </para>
        /// </remarks>
        /// <returns>
        ///   A string representation for the current value appropriate to the value type.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>
        public override string ToString()
        {
            switch (TokenType)
            {
                case JsonTokenType.None:
                case JsonTokenType.Null:
                    return string.Empty;
                case JsonTokenType.True:
                    return bool.TrueString;
                case JsonTokenType.False:
                    return bool.FalseString;
                case JsonTokenType.Number:
                case JsonTokenType.StartArray:
                case JsonTokenType.StartObject:
                {
                    // null parent should have hit the None case
                    Debug.Assert(_parent != null);
                    return _parent.GetRawValueAsString(_idx);
                }
                case JsonTokenType.String:
                    return GetString();
                case JsonTokenType.Comment:
                case JsonTokenType.EndArray:
                case JsonTokenType.EndObject:
                default:
                    Debug.Fail($"No handler for {nameof(JsonTokenType)}.{TokenType}");
                    return string.Empty;
            }
        }

        /// <summary>
        ///   Get a JsonElement which can be safely stored beyond the lifetime of the
        ///   original <see cref="JsonDocument"/>.
        /// </summary>
        /// <returns>
        ///   A JsonElement which can be safely stored beyond the lifetime of the
        ///   original <see cref="JsonDocument"/>.
        /// </returns>
        /// <remarks>
        ///   If this JsonElement is itself the output of a previous call to Clone, or
        ///   a value contained within another JsonElement which was the output of a previous
        ///   call to Clone, this method results in no additional memory allocation.
        /// </remarks>
        public JsonElement Clone()
        {
            CheckValidInstance();

            if (!_parent.IsDisposable)
            {
                return this;
            }

            return _parent.CloneElement(_idx);
        }

        private void CheckValidInstance()
        {
            if (_parent == null)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
