// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// PropertyAttributes is an enum which defines the attributes that may be associated
// with a property.  The values here are defined in Corhdr.h.

namespace System.Reflection
{
    // This Enum matchs the CorPropertyAttr defined in CorHdr.h
    [Flags]
    public enum PropertyAttributes
    {
        None = 0x0000,
        SpecialName = 0x0200,     // property is special.  Name describes how.

        RTSpecialName = 0x0400,     // Runtime(metadata internal APIs) should check name encoding.
        HasDefault = 0x1000,     // Property has default 

        Reserved2 = 0x2000,
        Reserved3 = 0x4000,
        Reserved4 = 0x8000,
        ReservedMask = 0xf400,
    }
}
