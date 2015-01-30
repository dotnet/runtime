// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Globalization {
    using System.Text;
    using System.Runtime.Remoting;
    using System;
    using System.Security;

    [Serializable]
    internal class CodePageDataItem
    {   
        [SecurityCritical]
        unsafe internal CodePageDataItem() {
            // TODO: Implement this fully.
        }

        unsafe public String WebName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                // TODO: Implement this fully.
                return "utf-8";
            }
        }
    
        public virtual int UIFamilyCodePage {
            get {
                // TODO: Implement this fully.
                return 1200;
            }
        }
    
        unsafe public String HeaderName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                // TODO: Implement this fully.
                return "utf-8";
            }
        }
    
        unsafe public String BodyName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                // TODO: Implement this fully.
                return "utf-8";
            }
        }    

        unsafe public uint Flags {
            get {
                // TODO: Implement this fully.
                return 771;
            }
        }

        // PAL ends here
    }
}
