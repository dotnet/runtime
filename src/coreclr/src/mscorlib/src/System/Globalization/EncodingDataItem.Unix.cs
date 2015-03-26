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
        // TODO: Implement this fully.
        private readonly string _webName;
        private readonly int _uiFamilyCodePage;
        private readonly string _headerName;
        private readonly string _bodyName;
        private readonly uint _flags;

        [SecurityCritical]
        unsafe internal CodePageDataItem(
            string webName, int uiFamilyCodePage, string headerName,
            string bodyName, uint flags) {
            // TODO: Implement this fully.
            _webName = webName;
            _uiFamilyCodePage = uiFamilyCodePage;
            _headerName = headerName;
            _bodyName = bodyName;
            _flags = flags;
        }

        unsafe public String WebName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                // TODO: Implement this fully.
                return _webName;
            }
        }
    
        public virtual int UIFamilyCodePage {
            get {
                // TODO: Implement this fully.
                return _uiFamilyCodePage;
            }
        }
    
        unsafe public String HeaderName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                // TODO: Implement this fully.
                return _headerName;
            }
        }
    
        unsafe public String BodyName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                // TODO: Implement this fully.
                return _bodyName;
            }
        }    

        unsafe public uint Flags {
            get {
                // TODO: Implement this fully.
                return _flags;
            }
        }

        // PAL ends here
    }
}
