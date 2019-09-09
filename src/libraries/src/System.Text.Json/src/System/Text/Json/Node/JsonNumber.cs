// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Text.Json
{
    /// <summary>
    ///   Represents a mutable numeric JSON value.
    /// </summary>
    public sealed class JsonNumber : JsonNode, IEquatable<JsonNumber>
    {
        private string _value;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonNumber"/> class representing the value 0.
        /// </summary>
        public JsonNumber() => _value = "0";

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonNumber"/> class representing a specified value.
        /// </summary>
        /// <param name="value">The string representation of a numeric value in a legal JSON number format.</param>
        /// <exception cref="ArgumentNullException">
        ///   Provided value is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   Provided value is not in a legal JSON number format.
        /// </exception>
        /// <remarks>Provided value is stored in the same format as passed.</remarks>
        public JsonNumber(string value) => SetFormattedValue(value);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonNumber"/> class from a <see cref="byte"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        public JsonNumber(byte value) => SetByte(value);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonNumber"/> class from an <see cref="short"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        public JsonNumber(short value) => SetInt16(value);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonNumber"/> class from an <see cref="int"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        public JsonNumber(int value) => SetInt32(value);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonNumber"/> class from an <see cref="long"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        public JsonNumber(long value) => SetInt64(value);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonNumber"/> class from a <see cref="float"/> value.
        /// </summary>
        /// <param name="value">A value to represent as a JSON number.</param>
        /// <exception cref="ArgumentException">
        ///   Provided value is not in a legal JSON number format.
        /// </exception>
        public JsonNumber(float value) => SetSingle(value);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonNumber"/> class from a <see cref="double"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        /// <exception cref="ArgumentException">
        ///   Provided value is not in a legal JSON number format.
        /// </exception>
        public JsonNumber(double value) => SetDouble(value);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonNumber"/> class from a <see cref="sbyte"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        [CLSCompliant(false)]
        public JsonNumber(sbyte value) => SetSByte(value);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonNumber"/> class from a <see cref="ushort"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        [CLSCompliant(false)]
        public JsonNumber(ushort value) => SetUInt16(value);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonNumber"/> class from a <see cref="uint"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        [CLSCompliant(false)]
        public JsonNumber(uint value) => SetUInt32(value);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonNumber"/> class from a <see cref="ulong"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        [CLSCompliant(false)]
        public JsonNumber(ulong value) => SetUInt64(value);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonNumber"/> class from a <see cref="decimal"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        public JsonNumber(decimal value) => SetDecimal(value);

        /// <summary>
        ///   Converts the numeric value of this instance to its equivalent string representation.
        /// </summary>
        /// <returns>The string representation of the value of this instance.</returns>
        /// <remarks>
        ///   Returns exactly the same value as it was set using  <see cref="SetFormattedValue(string)"/>.
        /// </remarks>
        public override string ToString() => _value;

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="byte"/> equivalent.
        /// </summary>
        /// <returns>A <see cref="byte"/> equivalent to the number stored by this instance.</returns>
        /// <exception cref="OverflowException">
        ///   <see cref="JsonNumber"/> represents a number less than <see cref="byte.MinValue"/> or greater than <see cref="byte.MaxValue"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   <see cref="JsonNumber"/> represents a number in a format not compliant with <see cref="byte"/>.
        /// </exception>
        public byte GetByte() => byte.Parse(_value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="short"/> equivalent.
        /// </summary>
        /// <returns>A <see cref="short"/> equivalent to the number stored by this instance.</returns>
        /// <exception cref="OverflowException">
        ///   <see cref="JsonNumber"/> represents a number less than <see cref="short.MinValue"/> or greater than <see cref="short.MaxValue"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   <see cref="JsonNumber"/> represents a number in a format not compliant with <see cref="short"/>.
        /// </exception>
        public short GetInt16() => short.Parse(_value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="int"/> equivalent.
        /// </summary>
        /// <returns>A <see cref="int"/> equivalent to the number stored by this instance.</returns>
        /// <exception cref="OverflowException">
        ///   <see cref="JsonNumber"/> represents a number less than <see cref="int.MinValue"/> or greater than <see cref="int.MaxValue"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   <see cref="JsonNumber"/> represents a number in a format not compliant with <see cref="int"/>.
        /// </exception>
        public int GetInt32() => int.Parse(_value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="long"/> equivalent.
        /// </summary>
        /// <returns>A <see cref="long"/> equivalent to the number stored by this instance.</returns>
        /// <exception cref="OverflowException">
        ///   <see cref="JsonNumber"/> represents a number less than <see cref="long.MinValue"/> or greater than <see cref="long.MaxValue"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   <see cref="JsonNumber"/> represents a number in a format not compliant with <see cref="long"/>.
        /// </exception>
        public long GetInt64() => long.Parse(_value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="float"/> equivalent.
        /// </summary>
        /// <returns>A <see cref="float"/> equivalent to the number stored by this instance.</returns>
        /// <exception cref="OverflowException">
        ///   <see cref="JsonNumber"/> represents a number less than <see cref="float.MinValue"/> or greater than <see cref="float.MaxValue"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   <see cref="JsonNumber"/> represents a number in a format not compliant with <see cref="float"/>.
        /// </exception>
        /// <remarks>
        ///   On .NET Core this method does not return <see langword="false"/> for values larger than
        ///   <see cref="float.MaxValue"/> (or smaller than <see cref="float.MinValue"/>),
        ///   instead <see langword="true"/> is returned and <see cref="float.PositiveInfinity"/> (or
        ///   <see cref="float.NegativeInfinity"/>) is emitted.
        /// </remarks>
        /// <remarks>
        ///   Allows scientific mode.
        /// </remarks>
        public float GetSingle() => float.Parse(_value, NumberStyles.Float, CultureInfo.InvariantCulture);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="double"/> equivalent.
        /// </summary>
        /// <returns>A <see cref="double"/> equivalent to the number stored by this instance.</returns>
        /// <exception cref="OverflowException">
        ///   <see cref="JsonNumber"/> represents a number less than <see cref="double.MinValue"/> or greater than <see cref="double.MaxValue"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   <see cref="JsonNumber"/> represents a number in a format not compliant with <see cref="double"/>.
        /// </exception>
        /// <remarks>
        ///   On .NET Core this method does not return <see langword="false"/> for values larger than
        ///   <see cref="double.MaxValue"/> (or smaller than <see cref="double.MinValue"/>),
        ///   instead <see langword="true"/> is returned and <see cref="double.PositiveInfinity"/> (or
        ///   <see cref="double.NegativeInfinity"/>) is emitted.
        /// </remarks>
        /// <remarks>
        ///   Allows scientific mode.
        /// </remarks>
        public double GetDouble() => double.Parse(_value, NumberStyles.Float, CultureInfo.InvariantCulture);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="sbyte"/> equivalent.
        /// </summary>
        /// <returns>A <see cref="sbyte"/> equivalent to the number stored by this instance.</returns>
        /// <exception cref="OverflowException">
        ///   <see cref="JsonNumber"/> represents a number less than <see cref="sbyte.MinValue"/> or greater than <see cref="sbyte.MaxValue"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   <see cref="JsonNumber"/> represents a number in a format not compliant with <see cref="sbyte"/>.
        /// </exception>
        [CLSCompliant(false)]
        public sbyte GetSByte() => sbyte.Parse(_value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="ushort"/> equivalent.
        /// </summary>
        /// <returns>A <see cref="ushort"/> equivalent to the number stored by this instance.</returns>
        /// <exception cref="OverflowException">
        ///   <see cref="JsonNumber"/> represents a number less than <see cref="ushort.MinValue"/> or greater than <see cref="ushort.MaxValue"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   <see cref="JsonNumber"/> represents a number in a format not compliant with <see cref="ushort"/>.
        /// </exception>
        [CLSCompliant(false)]
        public ushort GetUInt16() => ushort.Parse(_value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="uint"/> equivalent.
        /// </summary>
        /// <returns>A <see cref="uint"/> equivalent to the number stored by this instance.</returns>
        /// <exception cref="OverflowException">
        ///   <see cref="JsonNumber"/> represents a number less than <see cref="uint.MinValue"/> or greater than <see cref="uint.MaxValue"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   <see cref="JsonNumber"/> represents a number in a format not compliant with <see cref="uint"/>.
        /// </exception>
        [CLSCompliant(false)]
        public uint GetUInt32() => uint.Parse(_value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="ulong"/> equivalent.
        /// </summary>
        /// <returns>A <see cref="ulong"/> equivalent to the number stored by this instance.</returns>
        /// <exception cref="OverflowException">
        ///   <see cref="JsonNumber"/> represents a number less than <see cref="ulong.MinValue"/> or greater than <see cref="ulong.MaxValue"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   <see cref="JsonNumber"/> represents a number in a format not compliant with <see cref="ulong"/>.
        /// </exception>
        [CLSCompliant(false)]
        public ulong GetUInt64() => ulong.Parse(_value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="decimal"/> equivalent.
        /// </summary>
        /// <returns>A <see cref="decimal"/> equivalent to the number stored by this instance.</returns>
        /// <exception cref="OverflowException">
        ///   <see cref="JsonNumber"/> represents a number less than <see cref="decimal.MinValue"/> or greater than <see cref="decimal.MaxValue"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   <see cref="JsonNumber"/> represents a number in a format not compliant with <see cref="decimal"/>.
        /// </exception>
        public decimal GetDecimal() => decimal.Parse(_value, NumberStyles.Float, CultureInfo.InvariantCulture);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="byte"/> equivalent.
        ///   A return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="value">
        ///   When this method returns, contains the <see cref="byte"/> value equivalent of the number contained in this instance,
        ///   if the conversion succeeded, or zero if the conversion failed.
        /// </param>
        /// <returns>
        ///  <see langword="true"/> if instance was converted successfully;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        public bool TryGetByte(out byte value) => byte.TryParse(_value, out value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="short"/> equivalent.
        ///   A return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="value">
        ///   When this method returns, contains the <see cref="short"/> value equivalent of the number contained in this instance,
        ///   if the conversion succeeded, or zero if the conversion failed.
        /// </param>
        /// <returns>
        ///  <see langword="true"/> if instance was converted successfully;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        public bool TryGetInt16(out short value) => short.TryParse(_value, out value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="int"/> equivalent.
        ///   A return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="value">
        ///   When this method returns, contains the <see cref="int"/> value equivalent of the number contained in this instance,
        ///   if the conversion succeeded, or zero if the conversion failed.
        /// </param>
        /// <returns>
        ///  <see langword="true"/> if instance was converted successfully;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        public bool TryGetInt32(out int value) => int.TryParse(_value, out value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="long"/> equivalent.
        ///   A return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="value">
        ///   When this method returns, contains the <see cref="long"/> value equivalent of the number contained in this instance,
        ///   if the conversion succeeded, or zero if the conversion failed.
        /// </param>
        /// <returns>
        ///  <see langword="true"/> if instance was converted successfully;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        public bool TryGetInt64(out long value) => long.TryParse(_value, out value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="float"/> equivalent.
        ///   A return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="value">
        ///   When this method returns, contains the <see cref="float"/> value equivalent of the number contained in this instance,
        ///   if the conversion succeeded, or zero if the conversion failed.
        /// </param>
        /// <returns>
        ///  <see langword="true"/> if instance was converted successfully;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        /// <remarks>
        ///   On .NET Core this method does not return <see langword="false"/> for values larger than
        ///   <see cref="float.MaxValue"/> (or smaller than <see cref="float.MinValue"/>),
        ///   instead <see langword="true"/> is returned and <see cref="float.PositiveInfinity"/> (or
        ///   <see cref="float.NegativeInfinity"/>) is emitted.
        /// </remarks>
        public bool TryGetSingle(out float value) => float.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="double"/> equivalent.
        ///   A return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="value">
        ///   When this method returns, contains the <see cref="double"/> value equivalent of the number contained in this instance,
        ///   if the conversion succeeded, or zero if the conversion failed.
        /// </param>
        /// <returns>
        ///  <see langword="true"/> if instance was converted successfully;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        /// <remarks>
        ///   On .NET Core this method does not return <see langword="false"/> for values larger than
        ///   <see cref="float.MaxValue"/> (or smaller than <see cref="float.MinValue"/>),
        ///   instead <see langword="true"/> is returned and <see cref="float.PositiveInfinity"/> (or
        ///   <see cref="float.NegativeInfinity"/>) is emitted.
        /// </remarks>
        public bool TryGetDouble(out double value) => double.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="sbyte"/> equivalent.
        ///   A return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="value">
        ///   When this method returns, contains the <see cref="sbyte"/> value equivalent of the number contained in this instance,
        ///   if the conversion succeeded, or zero if the conversion failed.
        /// </param>
        /// <returns>
        ///  <see langword="true"/> if instance was converted successfully;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        [CLSCompliant(false)]
        public bool TryGetSByte(out sbyte value) => sbyte.TryParse(_value, out value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="ushort"/> equivalent.
        ///   A return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="value">
        ///   When this method returns, contains the <see cref="ushort"/> value equivalent of the number contained in this instance,
        ///   if the conversion succeeded, or zero if the conversion failed.
        /// </param>
        /// <returns>
        ///  <see langword="true"/> if instance was converted successfully;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        [CLSCompliant(false)]
        public bool TryGetUInt16(out ushort value) => ushort.TryParse(_value, out value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="uint"/> equivalent.
        ///   A return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="value">
        ///   When this method returns, contains the <see cref="uint"/> value equivalent of the number contained in this instance,
        ///   if the conversion succeeded, or zero if the conversion failed.
        /// </param>
        /// <returns>
        ///  <see langword="true"/> if instance was converted successfully;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        [CLSCompliant(false)]
        public bool TryGetUInt32(out uint value) => uint.TryParse(_value, out value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="ulong"/> equivalent.
        ///   A return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="value">
        ///   When this method returns, contains the <see cref="ulong"/> value equivalent of the number contained in this instance,
        ///   if the conversion succeeded, or zero if the conversion failed.
        /// </param>
        /// <returns>
        ///  <see langword="true"/> if instance was converted successfully;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        [CLSCompliant(false)]
        public bool TryGetUInt64(out ulong value) => ulong.TryParse(_value, out value);

        /// <summary>
        ///   Converts the numeric value of this instance to its <see cref="decimal"/> equivalent.
        ///   A return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="value">
        ///   When this method returns, contains the <see cref="decimal"/> value equivalent of the number contained in this instance,
        ///   if the conversion succeeded, or zero if the conversion failed.
        /// </param>
        /// <returns>
        ///  <see langword="true"/> if instance was converted successfully;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        public bool TryGetDecimal(out decimal value) => decimal.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        /// <summary>
        ///   Changes the numeric value of this instance to represent a specified value.
        /// </summary>
        /// <param name="value">The string representation of a numeric value in a legal JSON number format.</param>
        /// <exception cref="ArgumentNullException">
        ///   Provided value is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   Provided value is not in a legal JSON number format.
        /// </exception>
        /// <remarks>Provided value is stored in the same format as passed.</remarks>
        public void SetFormattedValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                throw new ArgumentException(SR.EmptyStringToInitializeNumber, nameof(value));
            }

            JsonWriterHelper.ValidateNumber(Encoding.UTF8.GetBytes(value).AsSpan());
            _value = value;
        }

        /// <summary>
        ///   Changes the numeric value of this instance to represent a specified <see cref="byte"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        public void SetByte(byte value) => _value = value.ToString();

        /// <summary>
        ///   Changes the numeric value of this instance to represent a specified <see cref="short"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        public void SetInt16(short value) => _value = value.ToString();

        /// <summary>
        ///   Changes the numeric value of this instance to represent a specified <see cref="int"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        public void SetInt32(int value) => _value = value.ToString();

        /// <summary>
        ///   Changes the numeric value of this instance to represent a specified <see cref="long"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        public void SetInt64(long value) => _value = value.ToString();

        /// <summary>
        ///   Changes the numeric value of this instance to represent a specified <see cref="float"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        /// <exception cref="ArgumentException">
        ///   Provided value is not in a legal JSON number format.
        /// </exception>
        public void SetSingle(float value)
        {
            JsonWriterHelper.ValidateSingle(value);
            _value = value.ToString(CultureInfo.InvariantCulture);
        }
        /// <summary>
        ///   Changes the numeric value of this instance to represent a specified <see cref="double"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        /// <exception cref="ArgumentException">
        ///   Provided value is not in a legal JSON number format.
        /// </exception>
        public void SetDouble(double value)
        {
            JsonWriterHelper.ValidateDouble(value);
            _value = value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///   Changes the numeric value of this instance to represent a specified <see cref="sbyte"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        [CLSCompliant(false)]
        public void SetSByte(sbyte value) => _value = value.ToString();

        /// <summary>
        ///   Changes the numeric value of this instance to represent a specified <see cref="ushort"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        [CLSCompliant(false)]
        public void SetUInt16(ushort value) => _value = value.ToString();

        /// <summary>
        ///   Changes the numeric value of this instance to represent a specified <see cref="uint"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        [CLSCompliant(false)]
        public void SetUInt32(uint value) => _value = value.ToString();

        /// <summary>
        ///   Changes the numeric value of this instance to represent a specified <see cref="ulong"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        [CLSCompliant(false)]
        public void SetUInt64(ulong value) => _value = value.ToString();

        /// <summary>
        ///   Changes the numeric value of this instance to represent a specified <see cref="decimal"/> value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON number.</param>
        public void SetDecimal(decimal value) => _value = value.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        ///   Compares <paramref name="obj"/> to the value of this instance.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns>
        ///   <see langword="true"/> if the value of this instance matches <paramref name="obj"/> exactly (is equal and has the same format),
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public override bool Equals(object obj) => obj is JsonNumber jsonNumber && Equals(jsonNumber);

        /// <summary>
        ///   Calculates a hash code of this instance.
        /// </summary>
        /// <returns>A hash code for this instance.</returns>
        public override int GetHashCode() => _value.GetHashCode();

        /// <summary>
        ///   Compares other JSON number to the value of this instance.
        /// </summary>
        /// <param name="other">The JSON number to compare against.</param>
        /// <returns>
        ///   <see langword="true"/> if the value of this instance matches <paramref name="other"/> exactly (is equal and has the same format),
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public bool Equals(JsonNumber other) => !(other is null) && _value == other._value;

        /// <summary>
        ///   Compares values of two JSON numbers.
        /// </summary>
        /// <param name="left">The JSON number to compare.</param>
        /// <param name="right">The JSON number to compare.</param>
        /// <returns>
        ///   <see langword="true"/> if values of instances match exactly (are equal and have the same format),
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator ==(JsonNumber left, JsonNumber right)
        {
            // Test "right" first to allow branch elimination when inlined for null checks (== null)
            // so it can become a simple test
            if (right is null)
            {
                // return true/false not the test result https://github.com/dotnet/coreclr/issues/914
                return (left is null) ? true : false;
            }

            return right.Equals(left);
        }

        /// <summary>
        ///   Compares values of two JSON numbers.
        /// </summary>
        /// <param name="left">The JSON number to compare.</param>
        /// <param name="right">The JSON number to compare.</param>
        /// <returns>
        ///   <see langword="true"/> if values of instances do not match exactly (are not equal or have different format),
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator !=(JsonNumber left, JsonNumber right) => !(left == right);

        /// <summary>
        ///   Creates a new JSON number that is a copy of the current instance.
        /// </summary>
        /// <returns>A new JSON number that is a copy of this instance.</returns>
        public override JsonNode Clone() => new JsonNumber(_value);

        /// <summary>
        ///   Returns <see cref="JsonValueKind.Number"/>
        /// </summary>
        public override JsonValueKind ValueKind { get => JsonValueKind.Number; }
    }
}
