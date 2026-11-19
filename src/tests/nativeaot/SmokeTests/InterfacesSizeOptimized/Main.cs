// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// The number of interfaces of a MethodTable is negative if its interface map has relative pointers, which is only
// the case on 64-bit targets. It is after the flags, the base size, the related type and the number of vtable slots.
unsafe
{
    if (sizeof(nint) == 8)
    {
        ushort numInterfaces = *(ushort*)((byte*)new Probe().GetType().TypeHandle.Value + 2 * sizeof(int) + sizeof(nint) + sizeof(ushort));
        if ((short)numInterfaces >= 0)
        {
            Console.WriteLine("The interface map doesn't have relative pointers");
            return 1;
        }
    }
}

return Interfaces.Run();

class Probe : IDisposable
{
    public void Dispose() { }
}
