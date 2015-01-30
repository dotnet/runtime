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
            return new EncodingInfo[] { new EncodingInfo(CodePageUtf8, "utf8", "Unicode (UTF-8)") };
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
            return CodePageUtf8;
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        unsafe internal static CodePageDataItem GetCodePageDataItem(int codepage) {

            // TODO: Implement this fully.
            return new CodePageDataItem();
        }

        // PAL ends here.

        const int CodePageUtf8 = 65001;
    }
}
