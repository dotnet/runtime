// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// PropertyAttributes is an enum which defines the attributes that may be associated
// 
//    with a property.  The values here are defined in Corhdr.h.
//
//
namespace System.Reflection {
    
    using System;
    // This Enum matchs the CorPropertyAttr defined in CorHdr.h
[Serializable]
[Flags]  
[System.Runtime.InteropServices.ComVisible(true)]
    public enum PropertyAttributes
    {
        None            =   0x0000,
        SpecialName     =   0x0200,     // property is special.  Name describes how.

        // Reserved flags for Runtime use only.
        ReservedMask          =   0xf400,
        RTSpecialName         =   0x0400,     // Runtime(metadata internal APIs) should check name encoding.
        HasDefault            =   0x1000,     // Property has default 
        Reserved2             =   0x2000,     // reserved bit
        Reserved3             =   0x4000,     // reserved bit 
        Reserved4             =   0x8000      // reserved bit 
    }
}
