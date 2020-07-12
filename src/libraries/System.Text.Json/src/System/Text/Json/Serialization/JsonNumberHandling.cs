// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Determines how <see cref="JsonSerializer"/> handles numbers when serializing and deserializing.
    /// </summary>
    [Flags]
    public enum JsonNumberHandling
    {
        /// <summary>
        /// Numbers will only be read from <see cref="JsonTokenType.Number"/> tokens and will only be written as JSON numbers (without quotes).
        /// </summary>
        Strict = 0,
        /// <summary>
        /// Numbers can be read from <see cref="JsonTokenType.String"/> tokens.
        /// Does not prevent numbers from being read from <see cref="JsonTokenType.Number"/> token.
        /// </summary>
        AllowReadingFromString = 1,
        /// <summary>
        /// Numbers will be written as JSON strings (with quotes), not as JSON numbers.
        /// </summary>
        WriteAsString = 2,
        /// <summary>
        /// Floating-point constants represented as <see cref="JsonTokenType.String"/>
        /// tokens such as "NaN", "Infinity", "-Infinity", can be read when reading,
        /// and such CLR values such as <see cref="float.NaN"/>, <see cref="double.PositiveInfinity"/>,
        /// <see cref="float.NegativeInfinity"/> will be written as their corresponding JSON string representations.
        /// </summary>
        AllowNamedFloatingPointLiterals = 4
    }
}
