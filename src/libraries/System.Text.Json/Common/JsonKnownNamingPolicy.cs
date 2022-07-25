// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// The <see cref="Json.JsonNamingPolicy"/> to be used at run time.
    /// </summary>
#if BUILDING_SOURCE_GENERATOR
    internal
#else
    public
#endif
    enum JsonKnownNamingPolicy
    {
        /// <summary>
        /// Specifies that JSON property names should not be converted.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Specifies that the built-in <see cref="Json.JsonNamingPolicy.CamelCase"/> be used to convert JSON property names.
        /// </summary>
        CamelCase = 1,

        /// <summary>
        /// Specifies that the built-in <see cref="Json.JsonNamingPolicy.SnakeLowerCase"/> be used to convert JSON property names.
        /// </summary>
        SnakeLowerCase = 2,

        /// <summary>
        /// Specifies that the built-in <see cref="Json.JsonNamingPolicy.SnakeUpperCase"/> be used to convert JSON property names.
        /// </summary>
        SnakeUpperCase = 3,

        /// <summary>
        /// Specifies that the built-in <see cref="Json.JsonNamingPolicy.KebabLowerCase"/> be used to convert JSON property names.
        /// </summary>
        KebabLowerCase = 4,

        /// <summary>
        /// Specifies that the built-in <see cref="Json.JsonNamingPolicy.KebabUpperCase"/> be used to convert JSON property names.
        /// </summary>
        KebabUpperCase = 5
    }
}
