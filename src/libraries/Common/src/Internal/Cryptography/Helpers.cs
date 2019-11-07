// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Internal.Cryptography
{
    internal static partial class Helpers
    {
        [return: NotNullIfNotNull("src")]
        public static byte[]? CloneByteArray(this byte[]? src)
        {
            if (src == null)
            {
                return null;
            }

            return (byte[])(src.Clone());
        }
    }
}
