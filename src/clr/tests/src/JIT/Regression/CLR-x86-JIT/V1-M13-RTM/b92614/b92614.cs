// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
public struct CC
{
    static float Static3(short N)
    {
        return
            82 * (ulong)N * (float)(((ulong)N) ^ (82u * (ulong)N));
    }
    static int Main() { Static3(0); return 100; }
}
