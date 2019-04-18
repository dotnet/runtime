// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Reflection
{
    [Flags]
    public enum MethodAttributes
    {
        // NOTE: This Enum matchs the CorMethodAttr defined in CorHdr.h

        // member access mask - Use this mask to retrieve accessibility information.
        MemberAccessMask = 0x0007,
        PrivateScope = 0x0000,     // Member not referenceable.
        Private = 0x0001,     // Accessible only by the parent type.  
        FamANDAssem = 0x0002,     // Accessible by sub-types only in this Assembly.
        Assembly = 0x0003,     // Accessibly by anyone in the Assembly.
        Family = 0x0004,     // Accessible only by type and sub-types.    
        FamORAssem = 0x0005,     // Accessibly by sub-types anywhere, plus anyone in assembly.
        Public = 0x0006,     // Accessibly by anyone who has visibility to this scope.    
                             // end member access mask

        // method contract attributes.
        Static = 0x0010,     // Defined on type, else per instance.
        Final = 0x0020,     // Method may not be overridden.
        Virtual = 0x0040,     // Method virtual.
        HideBySig = 0x0080,     // Method hides by name+sig, else just by name.
        CheckAccessOnOverride = 0x0200,

        // vtable layout mask - Use this mask to retrieve vtable attributes.
        VtableLayoutMask = 0x0100,
        ReuseSlot = 0x0000,     // The default.
        NewSlot = 0x0100,     // Method always gets a new slot in the vtable.
                              // end vtable layout mask

        // method implementation attributes.
        Abstract = 0x0400,     // Method does not provide an implementation.
        SpecialName = 0x0800,     // Method is special.  Name describes how.

        // interop attributes
        PinvokeImpl = 0x2000,     // Implementation is forwarded through pinvoke.
        UnmanagedExport = 0x0008,     // Managed method exported via thunk to unmanaged code.
        RTSpecialName = 0x1000,     // Runtime should check name encoding.

        HasSecurity = 0x4000,     // Method has security associate with it.
        RequireSecObject = 0x8000,     // Method calls another method containing security code.

        ReservedMask = 0xd000,
    }
}
