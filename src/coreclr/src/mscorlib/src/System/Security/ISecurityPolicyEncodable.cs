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
    using System.Security.Policy;
    
    
[System.Runtime.InteropServices.ComVisible(true)]
    public interface ISecurityPolicyEncodable
    {
#if FEATURE_CAS_POLICY
        SecurityElement ToXml( PolicyLevel level );
    
        void FromXml( SecurityElement e, PolicyLevel level );
#endif // FEATURE_CAS_POLICY
    }

}
