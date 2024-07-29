// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    /// <summary>
    /// Values from the "CallKind" byte dealing with calling conventions used by reflection.
    /// Calling conventions have since been extended into modopts for Unmanaged.
    /// </summary>
    internal enum SignatureCallingConvention : byte
    {
        Default = 0,
        Cdecl = 1,
        StdCall = 2,
        ThisCall = 3,
        FastCall = 4,
        Unmanaged = 9,
    }
}
