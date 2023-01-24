// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    // This Enum matches the CorFieldAttr defined in CorHdr.h
    public enum PEFileKinds
    {
        Dll = 0x0001,
        ConsoleApplication = 0x0002,
        WindowApplication = 0x0003,
    }
}
