// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Permissions {
    
    using System;
[System.Runtime.InteropServices.ComVisible(true)]
    public interface IUnrestrictedPermission
    {
        bool IsUnrestricted();
    }
}
