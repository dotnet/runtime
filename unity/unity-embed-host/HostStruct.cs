using System;

namespace Unity.CoreCLRHelpers;

unsafe struct HostStruct
{
    public IntPtr version;

    public delegate* unmanaged<byte*, long, IntPtr> LoadFromMemory;
    public delegate* unmanaged<byte*, int, IntPtr> LoadFromPath;
}
