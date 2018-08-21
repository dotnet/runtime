// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static partial class FileSystem
    {
        public static void Encrypt(string path)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_FileEncryption);
        }

        public static void Decrypt(string path)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_FileEncryption);
        }
    }
}
