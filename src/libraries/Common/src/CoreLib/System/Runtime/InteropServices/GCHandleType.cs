// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    // These are the types of handles used by the EE.  
    // IMPORTANT: These must match the definitions in ObjectHandle.h in the EE. 
    // IMPORTANT: If new values are added to the enum the GCHandle.MaxHandleType
    //            constant must be updated.
    public enum GCHandleType
    {
        Weak = 0,
        WeakTrackResurrection = 1,
        Normal = 2,
        Pinned = 3
    }
}
