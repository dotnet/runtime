// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    // Examples of StringSplitOptions combinations:
    //
    // string str = "a,,b, c, , d ,e";
    //
    // string[] split = str.Split(',', StringSplitOptions.None);
    // split := [ "a", "", "b", " c", " ", " d ", "e" ]
    //
    // string[] split = str.Split(',', StringSplitOptions.RemoveEmptyEntries);
    // split := [ "a", "b", " c", " ", " d ", "e" ]
    //
    // string[] split = str.Split(',', StringSplitOptions.TrimEntries);
    // split := [ "a", "", "b", "c", "", "d", "e" ]
    //
    // string[] split = str.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    // split := [ "a", "b", "c", "d", "e" ]

    /// <summary>
    /// Specifies how the results should be transformed when splitting a string into substrings.
    /// </summary>
    [Flags]
    public enum StringSplitOptions
    {
        /// <summary>
        /// Do not transform the results. This is the default behavior.
        /// </summary>
        None = 0,

        /// <summary>
        /// Remove empty (zero-length) substrings from the result.
        /// </summary>
        /// <remarks>
        /// If <see cref="RemoveEmptyEntries"/> and <see cref="TrimEntries"/> are specified together,
        /// then substrings that consist only of whitespace characters are also removed from the result.
        /// </remarks>
        RemoveEmptyEntries = 1,

        /// <summary>
        /// Trim whitespace from each substring in the result.
        /// </summary>
        TrimEntries = 2
    }
}
