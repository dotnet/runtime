// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.CorConstants
{
    public enum CorElementType : byte
    {
        Invalid = 0,
        ELEMENT_TYPE_VOID = 1,
        ELEMENT_TYPE_BOOLEAN = 2,
        ELEMENT_TYPE_CHAR = 3,
        ELEMENT_TYPE_I1 = 4,
        ELEMENT_TYPE_U1 = 5,
        ELEMENT_TYPE_I2 = 6,
        ELEMENT_TYPE_U2 = 7,
        ELEMENT_TYPE_I4 = 8,
        ELEMENT_TYPE_U4 = 9,
        ELEMENT_TYPE_I8 = 10,
        ELEMENT_TYPE_U8 = 11,
        ELEMENT_TYPE_R4 = 12,
        ELEMENT_TYPE_R8 = 13,
        ELEMENT_TYPE_STRING = 14,
        ELEMENT_TYPE_PTR = 15,
        ELEMENT_TYPE_BYREF = 16,
        ELEMENT_TYPE_VALUETYPE = 17,
        ELEMENT_TYPE_CLASS = 18,
        ELEMENT_TYPE_VAR = 19,
        ELEMENT_TYPE_ARRAY = 20,
        ELEMENT_TYPE_GENERICINST = 21,
        ELEMENT_TYPE_TYPEDBYREF = 22,
        ELEMENT_TYPE_I = 24,
        ELEMENT_TYPE_U = 25,
        ELEMENT_TYPE_FNPTR = 27,
        ELEMENT_TYPE_OBJECT = 28,
        ELEMENT_TYPE_SZARRAY = 29,
        ELEMENT_TYPE_MVAR = 30,
        ELEMENT_TYPE_CMOD_REQD = 31,
        ELEMENT_TYPE_CMOD_OPT = 32,

        // ZapSig encoding for ELEMENT_TYPE_VAR and ELEMENT_TYPE_MVAR. It is always followed
        // by the RID of a GenericParam token, encoded as a compressed integer.
        ELEMENT_TYPE_VAR_ZAPSIG = 0x3b,

        // UNUSED = 0x3c,

        // ZapSig encoding for native value types in IL stubs. IL stub signatures may contain
        // ELEMENT_TYPE_INTERNAL followed by ParamTypeDesc with ELEMENT_TYPE_VALUETYPE element
        // type. It acts like a modifier to the underlying structure making it look like its
        // unmanaged view (size determined by unmanaged layout, blittable, no GC pointers).
        //
        // ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG is used when encoding such types to NGEN images.
        // The signature looks like this: ET_NATIVE_VALUETYPE_ZAPSIG ET_VALUETYPE <token>.
        // See code:ZapSig.GetSignatureForTypeHandle and code:SigPointer.GetTypeHandleThrowing
        // where the encoding/decoding takes place.
        ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG = 0x3d,

        ELEMENT_TYPE_CANON_ZAPSIG = 0x3e,       // zapsig encoding for System.__Canon
        ELEMENT_TYPE_MODULE_ZAPSIG = 0x3f,      // zapsig encoding for external module id#

        ELEMENT_TYPE_HANDLE = 64,
        ELEMENT_TYPE_SENTINEL = 65,
        ELEMENT_TYPE_PINNED = 69,
    }

    public enum CorTokenType
    {
        mdtModule = 0x00000000,
        mdtTypeRef = 0x01000000,
        mdtTypeDef = 0x02000000,
        mdtFieldDef = 0x04000000,
        mdtMethodDef = 0x06000000,
        mdtParamDef = 0x08000000,
        mdtInterfaceImpl = 0x09000000,
        mdtMemberRef = 0x0a000000,
        mdtCustomAttribute = 0x0c000000,
        mdtPermission = 0x0e000000,
        mdtSignature = 0x11000000,
        mdtEvent = 0x14000000,
        mdtProperty = 0x17000000,
        mdtMethodImpl = 0x19000000,
        mdtModuleRef = 0x1a000000,
        mdtTypeSpec = 0x1b000000,
        mdtAssembly = 0x20000000,
        mdtAssemblyRef = 0x23000000,
        mdtFile = 0x26000000,
        mdtExportedType = 0x27000000,
        mdtManifestResource = 0x28000000,
        mdtGenericParam = 0x2a000000,
        mdtMethodSpec = 0x2b000000,
        mdtGenericParamConstraint = 0x2c000000,

        mdtString = 0x70000000,
        mdtName = 0x71000000,
        mdtBaseType = 0x72000000,
    }
}
