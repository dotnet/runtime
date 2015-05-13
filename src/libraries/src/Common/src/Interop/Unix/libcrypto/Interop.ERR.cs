// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        [DllImport(Libraries.LibCrypto)]
        private static extern void ERR_load_crypto_strings();

        [DllImport(Libraries.LibCrypto)]
        private static extern uint ERR_get_error();

        [DllImport(Libraries.LibCrypto, CharSet = CharSet.Ansi)]
        private static extern void ERR_error_string_n(uint e, StringBuilder buf, int len);

        internal static string GetOpenSslErrorString()
        {
            uint error = ERR_get_error();
            StringBuilder buf = new StringBuilder(1024);
            
            ERR_error_string_n(error, buf, buf.Capacity);
            return buf.ToString();
        }
    }
}
