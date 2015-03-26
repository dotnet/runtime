// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Globalization
{
    using System;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security;
    using System.Threading;
    using System.Diagnostics.Contracts;

    internal static class EncodingTable
    {
        // Return a list of all EncodingInfo objects describing all of our encodings
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static unsafe EncodingInfo[] GetEncodings()
        {
            // TODO: Implement this fully.
            return new EncodingInfo[] {
                new EncodingInfo(CodePageUtf7, "utf-7", "Unicode (UTF-7)"),
                new EncodingInfo(CodePageUtf8, "utf-8", "Unicode (UTF-8)"),
                new EncodingInfo(CodePageUtf16, "utf-16", "Unicode"),
                new EncodingInfo(CodePageUtf16BE, "utf-16BE", "Unicode (Big-Endian)"),
                new EncodingInfo(CodePageUtf32, "utf-32", "Unicode (UTF-32)"),
            };
        }
    
        /*=================================GetCodePageFromName==========================
        **Action: Given a encoding name, return the correct code page number for this encoding.
        **Returns: The code page for the encoding.
        **Arguments:
        **  name    the name of the encoding
        **Exceptions:
        **  ArgumentNullException if name is null.
        **  internalGetCodePageFromName will throw ArgumentException if name is not a valid encoding name.
        ============================================================================*/
        
        internal static int GetCodePageFromName(String name)
        {
            // TODO: Implement this fully.
            switch (name)
            {
                case "utf-7":
                    return CodePageUtf7; 

                case "utf-8":
                    return CodePageUtf8;

                case "utf-16":
                    return CodePageUtf16;

                case "utf-16BE":
                    return CodePageUtf16BE;

                case "utf-32":
                    return CodePageUtf32;

                default:
                    return CodePageUtf8;
            }
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        unsafe internal static CodePageDataItem GetCodePageDataItem(int codepage) {
            // TODO: Implement this fully.
            switch (codepage)
            {
                case CodePageUtf7:
                    return new CodePageDataItem("utf-7", CodePageUtf7, "utf-7", "utf-7", 771);

                case CodePageUtf8:
                    return new CodePageDataItem("utf-8", CodePageUtf8, "utf-8", "utf-8", 771);

                case CodePageUtf16:
                    return new CodePageDataItem("utf-16", CodePageUtf16, "utf-16", "utf-16", 771);

                case CodePageUtf16BE:
                    return new CodePageDataItem("utf-16BE", CodePageUtf16BE, "utf-16BE", "utf-16BE", 771);

                case CodePageUtf32:
                    return new CodePageDataItem("utf-32", CodePageUtf32, "utf-32", "utf-32", 771);

                default:
                    return new CodePageDataItem("utf-8", CodePageUtf8, "utf-8", "utf-8", 771);
            }
        }

        // PAL ends here.

        const int CodePageUtf7 = 65000;
        const int CodePageUtf8 = 65001;
        const int CodePageUtf16 = 1200;
        const int CodePageUtf16BE = 1201;
        const int CodePageUtf32 = 12000;
    }
}
