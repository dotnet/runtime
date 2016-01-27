// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{

[System.Runtime.InteropServices.ComVisible(true)]
    public interface IStackWalk
    {
        [DynamicSecurityMethodAttribute()]
        void Assert();
        
        [DynamicSecurityMethodAttribute()]
        void Demand();
        
        [DynamicSecurityMethodAttribute()]
        void Deny();
        
        [DynamicSecurityMethodAttribute()]
        void PermitOnly();
    }
}
