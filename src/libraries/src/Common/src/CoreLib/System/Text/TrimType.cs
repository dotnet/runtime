// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text
{
    /// <summary>
    /// Specifies which portions of the string should be trimmed in a trimming operation.
    /// </summary>
    [Flags]
    internal enum TrimType
    {
        /// <summary>
        /// Trim from the beginning of the string.
        /// </summary>
        Head = 1 << 0,

        /// <summary>
        /// Trim from the end of the string.
        /// </summary>
        Tail = 1 << 1,

        /// <summary>
        /// Trim from both the beginning and the end of the string.
        /// </summary>
        Both = Head | Tail
    }
}
