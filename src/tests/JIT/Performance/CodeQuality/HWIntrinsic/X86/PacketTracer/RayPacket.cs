// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

internal class RayPacket256
{
    public VectorPacket256 Starts;
    public VectorPacket256 Dirs;

    public RayPacket256(VectorPacket256 starts, VectorPacket256 dirs)
    {
        Starts = starts;
        Dirs = dirs;
    }
}
