// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Security
{
    // SecureString attempts to provide a defense-in-depth solution.
    //
    // On Windows, this is done with several mechanisms:
    // 1. keeping the data in unmanaged memory so that copies of it aren't implicitly made by the GC moving it around
    // 2. zero'ing out that unmanaged memory so that the string is reliably removed from memory when done with it
    // 3. encrypting the data while it's not being used (it's unencrypted to manipulate and use it)
    //
    // On Unix, we do 1 and 2, but we don't do 3 as there's no CryptProtectData equivalent.

    public sealed partial class SecureString
    {
        private static int GetAlignedByteSize(int length)
        {
            return Math.Max(length, 1) * sizeof(char);
        }

        private void ProtectMemory()
        {
            _encrypted = true;
        }

        private void UnprotectMemory()
        {
            _encrypted = false;
        }
    }
}
