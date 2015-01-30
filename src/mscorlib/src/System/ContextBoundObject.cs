// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
** Purpose: Defines the root type for all context bound types
**          
**
===========================================================*/
namespace System {   
    
    using System;
    using System.Security.Permissions;
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
#if FEATURE_REMOTING
    public abstract class ContextBoundObject : MarshalByRefObject {
#else // FEATURE_REMOTING
    public abstract class ContextBoundObject {
#endif // FEATURE_REMOTING
    }
}
