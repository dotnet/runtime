// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar
{
    /// <summary>
    /// Specifies the supported formats that tar entries can use.
    /// </summary>
    public enum TarEntryFormat
    {
        /// <summary>
        /// Tar entry format undetermined.
        /// </summary>
        Unknown,
        /// <summary>
        /// 1979 Version 7 AT&amp;T Unix tar entry format.
        /// </summary>
        V7,
        /// <summary>
        /// POSIX IEEE 1003.1-1988 Unix Standard tar entry format.
        /// </summary>
        Ustar,
        /// <summary>
        /// POSIX IEEE 1003.1-2001 ("POSIX.1") Pax Interchange tar entry format.
        /// </summary>
        Pax,
        /// <summary>
        /// GNU tar entry format (gnu).
        /// </summary>
        Gnu,
    }
}
