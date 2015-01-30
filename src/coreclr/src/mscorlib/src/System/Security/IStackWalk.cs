// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
