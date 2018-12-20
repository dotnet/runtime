// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class Ole32
    {
        /// <summary>
        /// Stat flags for <see cref="IStream.Stat(out STATSTG, STATFLAG)"/>.
        /// <see href="https://docs.microsoft.com/en-us/windows/desktop/api/wtypes/ne-wtypes-tagstatflag"/>
        /// </summary>
        internal enum STATFLAG : uint
        {
            /// <summary>
            /// Stat includes the name.
            /// </summary>
            STATFLAG_DEFAULT = 0,

            /// <summary>
            /// Stat doesn't include the name.
            /// </summary>
            STATFLAG_NONAME = 1
        }
    }
}
