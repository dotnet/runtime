// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace R2RDump
{
    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/corcompile.h">src/inc/corcompile.h</a> CorCompileImportType
    /// </summary>
    public enum CorCompileImportType
    {
        CORCOMPILE_IMPORT_TYPE_UNKNOWN = 0,
        CORCOMPILE_IMPORT_TYPE_EXTERNAL_METHOD = 1,
        CORCOMPILE_IMPORT_TYPE_STUB_DISPATCH = 2,
        CORCOMPILE_IMPORT_TYPE_STRING_HANDLE = 3,
        CORCOMPILE_IMPORT_TYPE_TYPE_HANDLE = 4,
        CORCOMPILE_IMPORT_TYPE_METHOD_HANDLE = 5,
        CORCOMPILE_IMPORT_TYPE_VIRTUAL_METHOD = 6,
    };

    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/corcompile.h">src/inc/corcompile.h</a> CorCompileImportFlags
    /// </summary>
    public enum CorCompileImportFlags
    {
        CORCOMPILE_IMPORT_FLAGS_UNKNOWN = 0x0000,
        CORCOMPILE_IMPORT_FLAGS_EAGER = 0x0001,   // Section at module load time.
        CORCOMPILE_IMPORT_FLAGS_CODE = 0x0002,   // Section contains code.
        CORCOMPILE_IMPORT_FLAGS_PCODE = 0x0004,   // Section contains pointers to code.
    };

    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/corcompile.h">src/inc/corcompile.h</a> CorCompileImportFlags
    /// </summary>
    public enum CORCOMPILE_FIXUP_BLOB_KIND
    {
        ENCODE_MODULE_OVERRIDE = 0x80,     /* When the high bit is set, override of the module immediately follows */
    }

    /// <summary>
    /// Constants for method and field encoding
    /// </summary>
    [Flags]
    public enum ReadyToRunMethodSigFlags : byte
    {
        READYTORUN_METHOD_SIG_None = 0x00,
        READYTORUN_METHOD_SIG_UnboxingStub = 0x01,
        READYTORUN_METHOD_SIG_InstantiatingStub = 0x02,
        READYTORUN_METHOD_SIG_MethodInstantiation = 0x04,
        READYTORUN_METHOD_SIG_SlotInsteadOfToken = 0x08,
        READYTORUN_METHOD_SIG_MemberRefToken = 0x10,
        READYTORUN_METHOD_SIG_Constrained = 0x20,
        READYTORUN_METHOD_SIG_OwnerType = 0x40,
    }

    [Flags]
    public enum ReadyToRunFieldSigFlags : byte
    {
        READYTORUN_FIELD_SIG_IndexInsteadOfToken = 0x08,
        READYTORUN_FIELD_SIG_MemberRefToken = 0x10,
        READYTORUN_FIELD_SIG_OwnerType = 0x40,
    }

    [Flags]
    public enum ReadyToRunTypeLayoutFlags : byte
    {
        READYTORUN_LAYOUT_HFA = 0x01,
        READYTORUN_LAYOUT_Alignment = 0x02,
        READYTORUN_LAYOUT_Alignment_Native = 0x04,
        READYTORUN_LAYOUT_GCLayout = 0x08,
        READYTORUN_LAYOUT_GCLayout_Empty = 0x10,
    }

    public enum ReadyToRunFixupKind
    {
        READYTORUN_FIXUP_ThisObjDictionaryLookup = 0x07,
        READYTORUN_FIXUP_TypeDictionaryLookup = 0x08,
        READYTORUN_FIXUP_MethodDictionaryLookup = 0x09,

        READYTORUN_FIXUP_TypeHandle = 0x10,
        READYTORUN_FIXUP_MethodHandle = 0x11,
        READYTORUN_FIXUP_FieldHandle = 0x12,

        READYTORUN_FIXUP_MethodEntry = 0x13, /* For calling a method entry point */
        READYTORUN_FIXUP_MethodEntry_DefToken = 0x14, /* Smaller version of MethodEntry - method is def token */
        READYTORUN_FIXUP_MethodEntry_RefToken = 0x15, /* Smaller version of MethodEntry - method is ref token */

        READYTORUN_FIXUP_VirtualEntry = 0x16, /* For invoking a virtual method */
        READYTORUN_FIXUP_VirtualEntry_DefToken = 0x17, /* Smaller version of VirtualEntry - method is def token */
        READYTORUN_FIXUP_VirtualEntry_RefToken = 0x18, /* Smaller version of VirtualEntry - method is ref token */
        READYTORUN_FIXUP_VirtualEntry_Slot = 0x19, /* Smaller version of VirtualEntry - type & slot */

        READYTORUN_FIXUP_Helper = 0x1A, /* Helper */
        READYTORUN_FIXUP_StringHandle = 0x1B, /* String handle */

        READYTORUN_FIXUP_NewObject = 0x1C, /* Dynamically created new helper */
        READYTORUN_FIXUP_NewArray = 0x1D,

        READYTORUN_FIXUP_IsInstanceOf = 0x1E, /* Dynamically created casting helper */
        READYTORUN_FIXUP_ChkCast = 0x1F,

        READYTORUN_FIXUP_FieldAddress = 0x20, /* For accessing a cross-module static fields */
        READYTORUN_FIXUP_CctorTrigger = 0x21, /* Static constructor trigger */

        READYTORUN_FIXUP_StaticBaseNonGC = 0x22, /* Dynamically created static base helpers */
        READYTORUN_FIXUP_StaticBaseGC = 0x23,
        READYTORUN_FIXUP_ThreadStaticBaseNonGC = 0x24,
        READYTORUN_FIXUP_ThreadStaticBaseGC = 0x25,

        READYTORUN_FIXUP_FieldBaseOffset = 0x26, /* Field base offset */
        READYTORUN_FIXUP_FieldOffset = 0x27, /* Field offset */

        READYTORUN_FIXUP_TypeDictionary = 0x28,
        READYTORUN_FIXUP_MethodDictionary = 0x29,

        READYTORUN_FIXUP_Check_TypeLayout = 0x2A, /* size, alignment, HFA, reference map */
        READYTORUN_FIXUP_Check_FieldOffset = 0x2B,

        READYTORUN_FIXUP_DelegateCtor = 0x2C, /* optimized delegate ctor */
        READYTORUN_FIXUP_DeclaringTypeHandle = 0x2D,

        READYTORUN_FIXUP_IndirectPInvokeTarget = 0x2E, /* Target of an inlined pinvoke */
    }

    //
    // Intrinsics and helpers
    //

    [Flags]
    public enum ReadyToRunHelper
    {
        READYTORUN_HELPER_Invalid = 0x00,

        // Not a real helper - handle to current module passed to delay load helpers.
        READYTORUN_HELPER_Module = 0x01,
        READYTORUN_HELPER_GSCookie = 0x02,

        //
        // Delay load helpers
        //

        // All delay load helpers use custom calling convention:
        // - scratch register - address of indirection cell. 0 = address is inferred from callsite.
        // - stack - section index, module handle
        READYTORUN_HELPER_DelayLoad_MethodCall = 0x08,

        READYTORUN_HELPER_DelayLoad_Helper = 0x10,
        READYTORUN_HELPER_DelayLoad_Helper_Obj = 0x11,
        READYTORUN_HELPER_DelayLoad_Helper_ObjObj = 0x12,

        // JIT helpers

        // Exception handling helpers
        READYTORUN_HELPER_Throw = 0x20,
        READYTORUN_HELPER_Rethrow = 0x21,
        READYTORUN_HELPER_Overflow = 0x22,
        READYTORUN_HELPER_RngChkFail = 0x23,
        READYTORUN_HELPER_FailFast = 0x24,
        READYTORUN_HELPER_ThrowNullRef = 0x25,
        READYTORUN_HELPER_ThrowDivZero = 0x26,

        // Write barriers
        READYTORUN_HELPER_WriteBarrier = 0x30,
        READYTORUN_HELPER_CheckedWriteBarrier = 0x31,
        READYTORUN_HELPER_ByRefWriteBarrier = 0x32,

        // Array helpers
        READYTORUN_HELPER_Stelem_Ref = 0x38,
        READYTORUN_HELPER_Ldelema_Ref = 0x39,

        READYTORUN_HELPER_MemSet = 0x40,
        READYTORUN_HELPER_MemCpy = 0x41,

        // PInvoke helpers
        READYTORUN_HELPER_PInvokeBegin = 0x42,
        READYTORUN_HELPER_PInvokeEnd = 0x43,

        // Get string handle lazily
        READYTORUN_HELPER_GetString = 0x50,

        // Used by /Tuning for Profile optimizations
        READYTORUN_HELPER_LogMethodEnter = 0x51,

        // Reflection helpers
        READYTORUN_HELPER_GetRuntimeTypeHandle = 0x54,
        READYTORUN_HELPER_GetRuntimeMethodHandle = 0x55,
        READYTORUN_HELPER_GetRuntimeFieldHandle = 0x56,

        READYTORUN_HELPER_Box = 0x58,
        READYTORUN_HELPER_Box_Nullable = 0x59,
        READYTORUN_HELPER_Unbox = 0x5A,
        READYTORUN_HELPER_Unbox_Nullable = 0x5B,
        READYTORUN_HELPER_NewMultiDimArr = 0x5C,
        READYTORUN_HELPER_NewMultiDimArr_NonVarArg = 0x5D,

        // Helpers used with generic handle lookup cases
        READYTORUN_HELPER_NewObject = 0x60,
        READYTORUN_HELPER_NewArray = 0x61,
        READYTORUN_HELPER_CheckCastAny = 0x62,
        READYTORUN_HELPER_CheckInstanceAny = 0x63,
        READYTORUN_HELPER_GenericGcStaticBase = 0x64,
        READYTORUN_HELPER_GenericNonGcStaticBase = 0x65,
        READYTORUN_HELPER_GenericGcTlsBase = 0x66,
        READYTORUN_HELPER_GenericNonGcTlsBase = 0x67,
        READYTORUN_HELPER_VirtualFuncPtr = 0x68,

        // Long mul/div/shift ops
        READYTORUN_HELPER_LMul = 0xC0,
        READYTORUN_HELPER_LMulOfv = 0xC1,
        READYTORUN_HELPER_ULMulOvf = 0xC2,
        READYTORUN_HELPER_LDiv = 0xC3,
        READYTORUN_HELPER_LMod = 0xC4,
        READYTORUN_HELPER_ULDiv = 0xC5,
        READYTORUN_HELPER_ULMod = 0xC6,
        READYTORUN_HELPER_LLsh = 0xC7,
        READYTORUN_HELPER_LRsh = 0xC8,
        READYTORUN_HELPER_LRsz = 0xC9,
        READYTORUN_HELPER_Lng2Dbl = 0xCA,
        READYTORUN_HELPER_ULng2Dbl = 0xCB,

        // 32-bit division helpers
        READYTORUN_HELPER_Div = 0xCC,
        READYTORUN_HELPER_Mod = 0xCD,
        READYTORUN_HELPER_UDiv = 0xCE,
        READYTORUN_HELPER_UMod = 0xCF,

        // Floating point conversions
        READYTORUN_HELPER_Dbl2Int = 0xD0,
        READYTORUN_HELPER_Dbl2IntOvf = 0xD1,
        READYTORUN_HELPER_Dbl2Lng = 0xD2,
        READYTORUN_HELPER_Dbl2LngOvf = 0xD3,
        READYTORUN_HELPER_Dbl2UInt = 0xD4,
        READYTORUN_HELPER_Dbl2UIntOvf = 0xD5,
        READYTORUN_HELPER_Dbl2ULng = 0xD6,
        READYTORUN_HELPER_Dbl2ULngOvf = 0xD7,

        // Floating point ops
        READYTORUN_HELPER_FltRem = 0xE0,
        READYTORUN_HELPER_DblRem = 0xE1,
        READYTORUN_HELPER_FltRound = 0xE2,
        READYTORUN_HELPER_DblRound = 0xE3,

        // Personality rountines
        READYTORUN_HELPER_PersonalityRoutine = 0xF0,
        READYTORUN_HELPER_PersonalityRoutineFilterFunclet = 0xF1,

        // Synchronized methods
        READYTORUN_HELPER_MonitorEnter = 0xF8,
        READYTORUN_HELPER_MonitorExit = 0xF9,

        //
        // Deprecated/legacy
        //

        // JIT32 x86-specific write barriers
        READYTORUN_HELPER_WriteBarrier_EAX = 0x100,
        READYTORUN_HELPER_WriteBarrier_EBX = 0x101,
        READYTORUN_HELPER_WriteBarrier_ECX = 0x102,
        READYTORUN_HELPER_WriteBarrier_ESI = 0x103,
        READYTORUN_HELPER_WriteBarrier_EDI = 0x104,
        READYTORUN_HELPER_WriteBarrier_EBP = 0x105,
        READYTORUN_HELPER_CheckedWriteBarrier_EAX = 0x106,
        READYTORUN_HELPER_CheckedWriteBarrier_EBX = 0x107,
        READYTORUN_HELPER_CheckedWriteBarrier_ECX = 0x108,
        READYTORUN_HELPER_CheckedWriteBarrier_ESI = 0x109,
        READYTORUN_HELPER_CheckedWriteBarrier_EDI = 0x10A,
        READYTORUN_HELPER_CheckedWriteBarrier_EBP = 0x10B,

        // JIT32 x86-specific exception handling
        READYTORUN_HELPER_EndCatch = 0x110,

        // A flag to indicate that a helper call uses VSD
        READYTORUN_HELPER_FLAG_VSD = 0x10000000,
    }

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

        // ZapSig encoding for an array MethodTable to allow it to remain such after decoding
        // (rather than being transformed into the TypeHandle representing that array)
        //
        // The element is always followed by ELEMENT_TYPE_SZARRAY or ELEMENT_TYPE_ARRAY
        ELEMENT_TYPE_NATIVE_ARRAY_TEMPLATE_ZAPSIG = 0x3c,

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

        ELEMENT_TYPE_CANON_ZAPSIG = 0x3e,     // zapsig encoding for [mscorlib]System.__Canon
        ELEMENT_TYPE_MODULE_ZAPSIG = 0x3f,     // zapsig encoding for external module id#

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
