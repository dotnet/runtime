// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public enum MemberAttributes
    {
        Abstract = 0x0001,
        Final = 0x0002,
        Static = 0x0003,
        Override = 0x0004,
        Const = 0x0005,
        New = 0x0010,
        Overloaded = 0x0100,
        Assembly = 0x1000,
        FamilyAndAssembly = 0x2000,
        Family = 0x3000,
        FamilyOrAssembly = 0x4000,
        Private = 0x5000,
        Public = 0x6000,
        AccessMask = 0xF000,
        ScopeMask = 0x000F,
        VTableMask = 0x00F0
    }
}
