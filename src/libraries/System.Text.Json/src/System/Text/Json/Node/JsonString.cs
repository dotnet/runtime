// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    /// <summary>
    ///   Represents a text JSON value.
    /// </summary>
    public sealed class JsonString : JsonNode, IEquatable<JsonString>
    {
        private string _value;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonString"/> class representing the empty value.
        /// </summary>
        public JsonString() => Value = string.Empty;

        /// <summary>
        ///  Initializes a new instance of the <see cref="JsonString"/> class representing a specified value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON string.</param>
        /// <exception cref="ArgumentNullException">
        ///   Provided value is null.
        /// </exception>
        public JsonString(string value) => Value = value;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonString"/> class representing a specified value.
        /// </summary>
        /// <param name="value">The value to represent as a JSON string.</param>
        public JsonString(ReadOnlySpan<char> value) => Value = value.ToString();

        /// <summary>
        ///   Gets or sets the text value represented by the instance.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///   Provided value is null.
        /// </exception>
        public string Value
        {
            get => _value;
            set => _value = value ?? throw new ArgumentNullException();
        }

        /// <summary>
        ///   Returns the text value represented by the instance.
        /// </summary>
        /// <returns>The value represented by this instance.</returns>
        public override string ToString() => _value;

        /// <summary>
        ///   Converts a <see cref="string"/> to a <see cref="JsonString"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator JsonString(string value) => new JsonString(value);

        /// <summary>
        ///   Compares <paramref name="obj"/> to the value of this instance.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns>
        ///   <see langword="true"/> if the text value of this instance matches <paramref name="obj"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public override bool Equals(object obj) => obj is JsonString jsonString && Equals(jsonString);

        /// <summary>
        ///   Calculates a hash code of this instance.
        /// </summary>
        /// <returns>A hash code for this instance.</returns>
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>
        ///   Compares other JSON string to the value of this instance.
        /// </summary>
        /// <param name="other">The JSON string to compare against.</param>
        /// <returns>
        ///   <see langword="true"/> if the text value of this instance matches <paramref name="other"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public bool Equals(JsonString other) => !(other is null) && Value == other.Value;

        /// <summary>
        ///   Compares values of two JSON strings.
        /// </summary>
        /// <param name="left">The JSON string to compare.</param>
        /// <param name="right">The JSON string to compare.</param>
        /// <returns>
        ///   <see langword="true"/> if values of instances match,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator ==(JsonString left, JsonString right)
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
        ///   Compares values of two JSON strings.
        /// </summary>
        /// <param name="left">The JSON string to compare.</param>
        /// <param name="right">The JSON string to compare.</param>
        /// <returns>
        ///   <see langword="true"/> if values of instances do not match,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator !=(JsonString left, JsonString right) => !(left == right);
    }
}
