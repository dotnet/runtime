// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Determines how <see cref="JsonSerializer"/> handles numbers when serializing and deserializing.
    /// </summary>
    [Flags]
#if BUILDING_SOURCE_GENERATOR
    internal
#else
    public
#endif
     enum JsonNumberHandling
    {
        /// <summary>
        /// Numbers will only be read from <see cref="JsonTokenType.Number"/> tokens and will only be written as JSON numbers (without quotes).
        /// </summary>
        Strict = 0x0,

        /// <summary>
        /// Numbers can be read from <see cref="JsonTokenType.String"/> tokens.
        /// Does not prevent numbers from being read from <see cref="JsonTokenType.Number"/> token.
        /// Strings that have escaped characters will be unescaped before reading.
        /// Leading or trailing trivia within the string token, including whitespace, is not allowed.
        /// </summary>
        AllowReadingFromString = 0x1,

        /// <summary>
        /// Numbers will be written as JSON strings (with quotes), not as JSON numbers.
        /// </summary>
        WriteAsString = 0x2,

        /// <summary>
        /// The "NaN", "Infinity", and "-Infinity" <see cref="JsonTokenType.String"/> tokens can be read as
        /// floating-point constants, and the <see cref="float"/> and <see cref="double"/> values for these
        /// constants (such as <see cref="float.PositiveInfinity"/> and <see cref="double.NaN"/>)
        /// will be written as their corresponding JSON string representations.
        /// Strings that have escaped characters will be unescaped before reading.
        /// Leading or trailing trivia within the string token, including whitespace, is not allowed.
        /// </summary>
        AllowNamedFloatingPointLiterals = 0x4
    }
}
