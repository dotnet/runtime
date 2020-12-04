// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    [Flags]
    internal enum EnumConverterOptions
    {
        /// <summary>
        /// Allow string values.
        /// </summary>
        AllowStrings = 0b0001,

        /// <summary>
        /// Allow number values.
        /// </summary>
        AllowNumbers = 0b0010
    }
}
