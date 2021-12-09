// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    public enum GCHandleType
    {
        Weak = 0,
        WeakTrackResurrection = 1,
        Normal = 2,
        Pinned = 3  // NOTE: not implemented here
    }
}
