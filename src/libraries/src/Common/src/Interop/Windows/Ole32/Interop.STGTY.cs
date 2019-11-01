// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class Ole32
    {
        /// <summary>
        /// Type of the storage element. Used with <see cref="STATSTG"/>.
        /// <see href="https://docs.microsoft.com/en-us/windows/desktop/api/objidl/ne-objidl-tagstgty"/>
        /// </summary>
        internal enum STGTY : uint
        {
            STGTY_STORAGE = 1,
            STGTY_STREAM = 2,
            STGTY_LOCKBYTES = 3,
            STGTY_PROPERTY = 4
        }
    }
}
