// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
