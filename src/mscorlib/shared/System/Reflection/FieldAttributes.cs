// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    // This Enum matchs the CorFieldAttr defined in CorHdr.h
    [Flags]
    public enum FieldAttributes
    {
        // member access mask - Use this mask to retrieve accessibility information.
        FieldAccessMask = 0x0007,
        PrivateScope = 0x0000,    // Member not referenceable.
        Private = 0x0001,    // Accessible only by the parent type.  
        FamANDAssem = 0x0002,    // Accessible by sub-types only in this Assembly.
        Assembly = 0x0003,    // Accessibly by anyone in the Assembly.
        Family = 0x0004,    // Accessible only by type and sub-types.    
        FamORAssem = 0x0005,    // Accessibly by sub-types anywhere, plus anyone in assembly.
        Public = 0x0006,    // Accessibly by anyone who has visibility to this scope.    
                            // end member access mask

        // field contract attributes.
        Static = 0x0010,        // Defined on type, else per instance.
        InitOnly = 0x0020,     // Field may only be initialized, not written to after init.
        Literal = 0x0040,        // Value is compile time constant.
        NotSerialized = 0x0080,        // Field does not have to be serialized when type is remoted.

        SpecialName = 0x0200,     // field is special.  Name describes how.

        // interop attributes
        PinvokeImpl = 0x2000,        // Implementation is forwarded through pinvoke.

        RTSpecialName = 0x0400,     // Runtime(metadata internal APIs) should check name encoding.
        HasFieldMarshal = 0x1000,     // Field has marshalling information.
        HasDefault = 0x8000,     // Field has default.
        HasFieldRVA = 0x0100,     // Field has RVA.

        ReservedMask = 0x9500,
    }
}
