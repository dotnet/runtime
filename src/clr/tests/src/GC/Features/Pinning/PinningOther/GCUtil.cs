// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
