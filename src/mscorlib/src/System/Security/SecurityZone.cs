// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
//
//  Enumeration of the zones code can come from
//

namespace System.Security
{
    using System;
    using System.Runtime.InteropServices;

    // The quick cache code depends on the values in this enumeration. Any change to this enumeration should
    // be reflected in PolicyManager.GenerateQuickCache as well.
    [ComVisible(true)]
    [Serializable]
    public enum SecurityZone
    {
        MyComputer   = 0,
        Intranet     = 1,
        Trusted      = 2,
        Internet     = 3,
        Untrusted    = 4,
    
        NoZone       = -1,  // No Zone Information
    }
}
