// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar
{
    /// <summary>
    /// Specifies the supported Tar formats.
    /// </summary>
    public enum TarFormat
    {
        /// <summary>
        /// Tar format undetermined.
        /// </summary>
        Unknown,
        /// <summary>
        /// 1979 Version 7 AT&amp;T Unix Tar Command Format (v7).
        /// </summary>
        V7,
        /// <summary>
        /// POSIX IEEE 1003.1-1988 Unix Standard Tar Format (ustar).
        /// </summary>
        Ustar,
        /// <summary>
        /// POSIX IEEE 1003.1-2001 ("POSIX.1") Pax Interchange Tar Format (pax).
        /// </summary>
        Pax,
        /// <summary>
        /// GNU Tar Format (gnu).
        /// </summary>
        Gnu,
    }
}
