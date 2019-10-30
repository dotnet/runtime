// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.PortableExecutable
{
    /// <summary>
    /// COR20Flags
    /// </summary>
    [Flags]
    public enum CorFlags
    {
        ILOnly = 0x00000001,
        Requires32Bit = 0x00000002,
        ILLibrary = 0x00000004,
        StrongNameSigned = 0x00000008,
        NativeEntryPoint = 0x00000010,
        TrackDebugData = 0x00010000,
        Prefers32Bit = 0x00020000,
    }
}
