// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
