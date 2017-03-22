// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// EventAttributes are an enum defining the attributes associated with and Event.
// These are defined in CorHdr.h and are a combination of bits and enums.

namespace System.Reflection
{
    [Flags]
    public enum EventAttributes
    {
        None = 0x0000,

        // This Enum matchs the CorEventAttr defined in CorHdr.h
        SpecialName = 0x0200,     // event is special.  Name describes how.

        RTSpecialName = 0x0400,     // Runtime(metadata internal APIs) should check name encoding.

        ReservedMask = 0x0400,
    }
}
