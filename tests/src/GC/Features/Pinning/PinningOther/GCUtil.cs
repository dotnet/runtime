// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Security;

public class GCUtil
{
    [SecuritySafeCritical]
    public static GCHandle Alloc(Object obj, GCHandleType gcht)
    {
        return GCHandle.Alloc(obj, gcht);
    }

    [SecuritySafeCritical]
    public static IntPtr AddrOfPinnedObject(GCHandle gh)
    {
        return gh.AddrOfPinnedObject();
    }

    [SecuritySafeCritical]
    public static void Free(ref GCHandle gh)
    {
        gh.Free();
    }


    [SecuritySafeCritical]
    public static Object GetTarget(GCHandle gh)
    {
        return gh.Target;
    }
}
