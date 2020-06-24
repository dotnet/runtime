// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text
{
    public static class EncodingExtensions
    {
        /// <summary>
        /// Returns a value stating whether <paramref name="encoding"/> is UTF-7.
        /// </summary>
        /// <remarks>
        /// This method checks only for the code page 65000.
        /// </remarks>
        public static bool IsUTF7Encoding(this Encoding encoding)
        {
            return (encoding.CodePage == 65000);
        }
    }
}
