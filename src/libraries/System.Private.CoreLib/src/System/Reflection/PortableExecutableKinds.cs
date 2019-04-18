// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Reflection
{
    [Flags]
    public enum PortableExecutableKinds
    {
        NotAPortableExecutableImage = 0x0,
        ILOnly = 0x1,
        Required32Bit = 0x2,
        PE32Plus = 0x4,
        Unmanaged32Bit = 0x8,
        Preferred32Bit = 0x10,
    }
}

