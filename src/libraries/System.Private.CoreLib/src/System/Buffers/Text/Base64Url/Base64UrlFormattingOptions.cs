// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers.Text
{
    /// <summary>
    /// Specifies options for Base64Url encoding.
    /// </summary>
    [Flags]
    public enum Base64UrlFormattingOptions
    {
        /// <summary>
        /// Omits the padding characters from the encoded output.
        /// </summary>
        None = 0,

        // The value 1 is reserved so that a line break option, if ever added, can share the
        // value of Base64FormattingOptions.InsertLineBreaks.

        /// <summary>
        /// Appends '=' padding characters to the encoded output so that its length is a multiple of four.
        /// </summary>
        IncludePadding = 2,
    }
}
