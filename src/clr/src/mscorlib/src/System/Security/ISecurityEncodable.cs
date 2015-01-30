// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// 
//
// All encodable security classes that support encoding need to
// implement this interface
//

namespace System.Security  {
    
    using System;
    using System.Security.Util;
    
    
[System.Runtime.InteropServices.ComVisible(true)]
    public interface ISecurityEncodable
    {
#if FEATURE_CAS_POLICY
        SecurityElement ToXml();
    
        void FromXml( SecurityElement e );
#endif // FEATURE_CAS_POLICY
    }

}


