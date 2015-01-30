// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security {
    using System.Runtime.Remoting;
    using System;
    using System.Security.Policy;
[System.Runtime.InteropServices.ComVisible(true)]
    public interface IEvidenceFactory
    {
#if FEATURE_CAS_POLICY        
        Evidence Evidence
        {
            get;
        }
#endif // FEATURE_CAS_POLICY
    }

}
