// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
