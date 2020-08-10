// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.TypeSystem;

namespace Internal.JitInterface
{
    public static class CORINFO
    {
        // CORINFO_MAXINDIRECTIONS is the maximum number of
        // indirections used by runtime lookups.
        // This accounts for up to 2 indirections to get at a dictionary followed by a possible spill slot
        public const uint MAXINDIRECTIONS = 4;
        public const ushort USEHELPER = 0xffff;
        public const ushort CORINFO_NO_SIZE_CHECK = 0xffff;
    }

    public struct CORINFO_METHOD_STRUCT_
    {
        internal static unsafe CORINFO_METHOD_STRUCT_* Construct(int i)
        {
            return (CORINFO_METHOD_STRUCT_*)((i + 1) << 4);
        }

        internal static unsafe int GetValue(CORINFO_METHOD_STRUCT_* val)
        {
            return ((int)val - 1) >> 4;
        }
    }

    public struct CORINFO_FIELD_STRUCT_
    {
        internal static unsafe CORINFO_FIELD_STRUCT_* Construct(int i)
        {
            return (CORINFO_FIELD_STRUCT_*)((i + 1) << 4);
        }
        internal static unsafe int GetValue(CORINFO_FIELD_STRUCT_* val)
        {
            return ((int)val - 1) >> 4;
        }
    }

    public struct CORINFO_CLASS_STRUCT_
    {
        internal static unsafe CORINFO_CLASS_STRUCT_* Construct(int i)
        {
            return (CORINFO_CLASS_STRUCT_*)((i + 1) << 4);
        }

        internal static unsafe int GetValue(CORINFO_CLASS_STRUCT_* val)
        {
            return ((int)val - 1) >> 4;
        }
    }

    public struct CORINFO_ARG_LIST_STRUCT_
    {
    }

    public struct CORINFO_MODULE_STRUCT_
    {
        internal static unsafe CORINFO_MODULE_STRUCT_* Construct(int i)
        {
            return (CORINFO_MODULE_STRUCT_*)((i + 1) << 4);
        }
        internal static unsafe int GetValue(CORINFO_MODULE_STRUCT_* val)
        {
            return ((int)val - 1) >> 4;
        }
    }

    public struct CORINFO_ASSEMBLY_STRUCT_
    {
    }

    public struct CORINFO_CONTEXT_STRUCT
    {
    }

    public struct CORINFO_GENERIC_STRUCT_
    {
    }

    public struct CORINFO_JUST_MY_CODE_HANDLE_
    {
    }

    public struct CORINFO_VarArgInfo
    {
    }
    
    public struct PatchpointInfo
    {
    }

    public enum _EXCEPTION_POINTERS
    { }

    public unsafe struct CORINFO_SIG_INST
    {
        public uint classInstCount;
        public CORINFO_CLASS_STRUCT_** classInst; // (representative, not exact) instantiation for class type variables in signature
        public uint methInstCount;
        public CORINFO_CLASS_STRUCT_** methInst; // (representative, not exact) instantiation for method type variables in signature
    }

    public enum mdToken : uint
    { }

    public enum HRESULT {
        E_NOTIMPL = -2147467263
    }

    public unsafe struct CORINFO_SIG_INFO
    {
        public CorInfoCallConv callConv;
        public CORINFO_CLASS_STRUCT_* retTypeClass;   // if the return type is a value class, this is its handle (enums are normalized)
        public CORINFO_CLASS_STRUCT_* retTypeSigClass;// returns the value class as it is in the sig (enums are not converted to primitives)
        public byte _retType;
        public CorInfoSigInfoFlags flags;    // used by IL stubs code
        public ushort numArgs;
        public CORINFO_SIG_INST sigInst;  // information about how type variables are being instantiated in generic code
        public CORINFO_ARG_LIST_STRUCT_* args;
        public byte* pSig;
        public uint cbSig;
        public CORINFO_MODULE_STRUCT_* scope;          // passed to getArgClass
        public mdToken token;

        public CorInfoType retType { get { return (CorInfoType)_retType; } set { _retType = (byte)value; } }
        private CorInfoCallConv getCallConv() { return (CorInfoCallConv)((callConv & CorInfoCallConv.CORINFO_CALLCONV_MASK)); }
        private bool hasThis() { return ((callConv & CorInfoCallConv.CORINFO_CALLCONV_HASTHIS) != 0); }
        private bool hasExplicitThis() { return ((callConv & CorInfoCallConv.CORINFO_CALLCONV_EXPLICITTHIS) != 0); }
        private uint totalILArgs() { return (uint)(numArgs + (hasThis() ? 1 : 0)); }
        private bool isVarArg() { return ((getCallConv() == CorInfoCallConv.CORINFO_CALLCONV_VARARG) || (getCallConv() == CorInfoCallConv.CORINFO_CALLCONV_NATIVEVARARG)); }
        internal bool hasTypeArg() { return ((callConv & CorInfoCallConv.CORINFO_CALLCONV_PARAMTYPE) != 0); }
    };

    //----------------------------------------------------------------------------
    // Looking up handles and addresses.
    //
    // When the JIT requests a handle, the EE may direct the JIT that it must
    // access the handle in a variety of ways.  These are packed as
    //    CORINFO_CONST_LOOKUP
    // or CORINFO_LOOKUP (contains either a CORINFO_CONST_LOOKUP or a CORINFO_RUNTIME_LOOKUP)
    //
    // Constant Lookups v. Runtime Lookups (i.e. when will Runtime Lookups be generated?)
    // -----------------------------------------------------------------------------------
    //
    // CORINFO_LOOKUP_KIND is part of the result type of embedGenericHandle,
    // getVirtualCallInfo and any other functions that may require a
    // runtime lookup when compiling shared generic code.
    //
    // CORINFO_LOOKUP_KIND indicates whether a particular token in the instruction stream can be:
    // (a) Mapped to a handle (type, field or method) at compile-time (!needsRuntimeLookup)
    // (b) Must be looked up at run-time, and if so which runtime lookup technique should be used (see below)
    //
    // If the JIT or EE does not support code sharing for generic code, then
    // all CORINFO_LOOKUP results will be "constant lookups", i.e.
    // the needsRuntimeLookup of CORINFO_LOOKUP.lookupKind.needsRuntimeLookup
    // will be false.
    //
    // Constant Lookups
    // ----------------
    //
    // Constant Lookups are either:
    //     IAT_VALUE: immediate (relocatable) values,
    //     IAT_PVALUE: immediate values access via an indirection through an immediate (relocatable) address
    //     IAT_RELPVALUE: immediate values access via a relative indirection through an immediate offset
    //     IAT_PPVALUE: immediate values access via a double indirection through an immediate (relocatable) address
    //
    // Runtime Lookups
    // ---------------
    //
    // CORINFO_LOOKUP_KIND is part of the result type of embedGenericHandle,
    // getVirtualCallInfo and any other functions that may require a
    // runtime lookup when compiling shared generic code.
    //
    // CORINFO_LOOKUP_KIND indicates whether a particular token in the instruction stream can be:
    // (a) Mapped to a handle (type, field or method) at compile-time (!needsRuntimeLookup)
    // (b) Must be looked up at run-time using the class dictionary
    //     stored in the vtable of the this pointer (needsRuntimeLookup && THISOBJ)
    // (c) Must be looked up at run-time using the method dictionary
    //     stored in the method descriptor parameter passed to a generic
    //     method (needsRuntimeLookup && METHODPARAM)
    // (d) Must be looked up at run-time using the class dictionary stored
    //     in the vtable parameter passed to a method in a generic
    //     struct (needsRuntimeLookup && CLASSPARAM)

    public unsafe struct CORINFO_CONST_LOOKUP
    {
        // If the handle is obtained at compile-time, then this handle is the "exact" handle (class, method, or field)
        // Otherwise, it's a representative...
        // If accessType is
        //     IAT_VALUE   --> "handle" stores the real handle or "addr " stores the computed address
        //     IAT_PVALUE  --> "addr" stores a pointer to a location which will hold the real handle
        //     IAT_RELPVALUE --> "addr" stores a relative pointer to a location which will hold the real handle
        //     IAT_PPVALUE --> "addr" stores a double indirection to a location which will hold the real handle

        public InfoAccessType accessType;

        // _value represent the union of handle and addr
        private IntPtr _value;
        public CORINFO_GENERIC_STRUCT_* handle { get { return (CORINFO_GENERIC_STRUCT_*)_value; } set { _value = (IntPtr)value; } }
        public void* addr { get { return (void*)_value; } set { _value = (IntPtr)value; } }
    };

    public enum CORINFO_RUNTIME_LOOKUP_KIND
    {
        CORINFO_LOOKUP_THISOBJ,
        CORINFO_LOOKUP_METHODPARAM,
        CORINFO_LOOKUP_CLASSPARAM,
        CORINFO_LOOKUP_NOT_SUPPORTED, // Returned for attempts to inline dictionary lookups
    }

    public unsafe struct CORINFO_LOOKUP_KIND
    {
        private byte _needsRuntimeLookup;
        public bool needsRuntimeLookup { get { return _needsRuntimeLookup != 0; } set { _needsRuntimeLookup = value ? (byte)1 : (byte)0; } }
        public CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind;

        // The 'runtimeLookupFlags' and 'runtimeLookupArgs' fields
        // are just for internal VM / ZAP communication, not to be used by the JIT.
        public ushort runtimeLookupFlags;
        public void* runtimeLookupArgs;
    }

    // CORINFO_RUNTIME_LOOKUP indicates the details of the runtime lookup
    // operation to be performed.
    //

    public unsafe struct CORINFO_RUNTIME_LOOKUP
    {
        // This is signature you must pass back to the runtime lookup helper
        public void* signature;

        // Here is the helper you must call. It is one of CORINFO_HELP_RUNTIMEHANDLE_* helpers.
        public CorInfoHelpFunc helper;

        // Number of indirections to get there
        // CORINFO_USEHELPER = don't know how to get it, so use helper function at run-time instead
        // 0 = use the this pointer itself (e.g. token is C<!0> inside code in sealed class C)
        //     or method desc itself (e.g. token is method void M::mymeth<!!0>() inside code in M::mymeth)
        // Otherwise, follow each byte-offset stored in the "offsets[]" array (may be negative)
        public ushort indirections;

        // If set, test for null and branch to helper if null
        public byte _testForNull;
        public bool testForNull { get { return _testForNull != 0; } set { _testForNull = value ? (byte)1 : (byte)0; } }

        // If set, test the lowest bit and dereference if set (see code:FixupPointer)
        public byte _testForFixup;
        public bool testForFixup { get { return _testForFixup != 0; } set { _testForFixup = value ? (byte)1 : (byte)0; } }

        public ushort sizeOffset;
        public IntPtr offset0;
        public IntPtr offset1;
        public IntPtr offset2;
        public IntPtr offset3;

        public byte _indirectFirstOffset;
        public bool indirectFirstOffset { get { return _indirectFirstOffset != 0; } set { _indirectFirstOffset = value ? (byte)1 : (byte)0; } }

        public byte _indirectSecondOffset;
        public bool indirectSecondOffset { get { return _indirectSecondOffset != 0; } set { _indirectSecondOffset = value ? (byte)1 : (byte)0; } }

    }

    // Result of calling embedGenericHandle
    public unsafe struct CORINFO_LOOKUP
    {
        public CORINFO_LOOKUP_KIND lookupKind;

        // If kind.needsRuntimeLookup then this indicates how to do the lookup
        public CORINFO_RUNTIME_LOOKUP runtimeLookup;

        // If the handle is obtained at compile-time, then this handle is the "exact" handle (class, method, or field)
        // Otherwise, it's a representative...  If accessType is
        //     IAT_VALUE --> "handle" stores the real handle or "addr " stores the computed address
        //     IAT_PVALUE --> "addr" stores a pointer to a location which will hold the real handle
        //     IAT_RELPVALUE --> "addr" stores a relative pointer to a location which will hold the real handle
        //     IAT_PPVALUE --> "addr" stores a double indirection to a location which will hold the real handle
        public ref CORINFO_CONST_LOOKUP constLookup
        {
            get
            {
                // constLookup is union with runtimeLookup
                Debug.Assert(sizeof(CORINFO_RUNTIME_LOOKUP) >= sizeof(CORINFO_CONST_LOOKUP));
                fixed (CORINFO_RUNTIME_LOOKUP * p = &runtimeLookup)
                    return ref *(CORINFO_CONST_LOOKUP *)p;
            }
        }
    }

    public unsafe struct CORINFO_RESOLVED_TOKEN
    {
        //
        // [In] arguments of resolveToken
        //
        public CORINFO_CONTEXT_STRUCT* tokenContext;       //Context for resolution of generic arguments
        public CORINFO_MODULE_STRUCT_* tokenScope;
        public mdToken token;              //The source token
        public CorInfoTokenKind tokenType;

        //
        // [Out] arguments of resolveToken.
        // - Type handle is always non-NULL.
        // - At most one of method and field handles is non-NULL (according to the token type).
        // - Method handle is an instantiating stub only for generic methods. Type handle
        //   is required to provide the full context for methods in generic types.
        //
        public CORINFO_CLASS_STRUCT_* hClass;
        public CORINFO_METHOD_STRUCT_* hMethod;
        public CORINFO_FIELD_STRUCT_* hField;

        //
        // [Out] TypeSpec and MethodSpec signatures for generics. NULL otherwise.
        //
        public byte* pTypeSpec;
        public uint cbTypeSpec;
        public byte* pMethodSpec;
        public uint cbMethodSpec;
    }


    // Flags computed by a runtime compiler
    public enum CorInfoMethodRuntimeFlags
    {
        CORINFO_FLG_BAD_INLINEE = 0x00000001, // The method is not suitable for inlining
        // unused = 0x00000002,
        // unused = 0x00000004,
        CORINFO_FLG_SWITCHED_TO_MIN_OPT = 0x00000008, // The JIT decided to switch to MinOpt for this method, when it was not requested
        CORINFO_FLG_SWITCHED_TO_OPTIMIZED = 0x00000010, // The JIT decided to switch to tier 1 for this method, when a different tier was requested
    };

    // The enumeration is returned in 'getSig'

    public enum CorInfoCallConv
    {
        // These correspond to CorCallingConvention

        CORINFO_CALLCONV_DEFAULT = 0x0,
        CORINFO_CALLCONV_C = 0x1,
        CORINFO_CALLCONV_STDCALL = 0x2,
        CORINFO_CALLCONV_THISCALL = 0x3,
        CORINFO_CALLCONV_FASTCALL = 0x4,
        CORINFO_CALLCONV_VARARG = 0x5,
        CORINFO_CALLCONV_FIELD = 0x6,
        CORINFO_CALLCONV_LOCAL_SIG = 0x7,
        CORINFO_CALLCONV_PROPERTY = 0x8,
        CORINFO_CALLCONV_UNMANAGED = 0x9,
        CORINFO_CALLCONV_NATIVEVARARG = 0xb,    // used ONLY for IL stub PInvoke vararg calls

        CORINFO_CALLCONV_MASK = 0x0f,     // Calling convention is bottom 4 bits
        CORINFO_CALLCONV_GENERIC = 0x10,
        CORINFO_CALLCONV_HASTHIS = 0x20,
        CORINFO_CALLCONV_EXPLICITTHIS = 0x40,
        CORINFO_CALLCONV_PARAMTYPE = 0x80,     // Passed last. Same as CORINFO_GENERICS_CTXT_FROM_PARAMTYPEARG
    }

    public enum CorInfoUnmanagedCallConv
    {
        // These correspond to CorUnmanagedCallingConvention

        CORINFO_UNMANAGED_CALLCONV_UNKNOWN,
        CORINFO_UNMANAGED_CALLCONV_C,
        CORINFO_UNMANAGED_CALLCONV_STDCALL,
        CORINFO_UNMANAGED_CALLCONV_THISCALL,
        CORINFO_UNMANAGED_CALLCONV_FASTCALL
    }

    public enum CORINFO_CALLINFO_FLAGS
    {
        CORINFO_CALLINFO_NONE = 0x0000,
        CORINFO_CALLINFO_ALLOWINSTPARAM = 0x0001,   // Can the compiler generate code to pass an instantiation parameters? Simple compilers should not use this flag
        CORINFO_CALLINFO_CALLVIRT = 0x0002,   // Is it a virtual call?
        CORINFO_CALLINFO_KINDONLY = 0x0004,   // This is set to only query the kind of call to perform, without getting any other information
        CORINFO_CALLINFO_VERIFICATION = 0x0008,   // Gets extra verification information.
        CORINFO_CALLINFO_SECURITYCHECKS = 0x0010,   // Perform security checks.
        CORINFO_CALLINFO_LDFTN = 0x0020,   // Resolving target of LDFTN
        CORINFO_CALLINFO_ATYPICAL_CALLSITE = 0x0040, // Atypical callsite that cannot be disassembled by delay loading helper
    }

    // Bit-twiddling of contexts assumes word-alignment of method handles and type handles
    // If this ever changes, some other encoding will be needed
    public enum CorInfoContextFlags
    {
        CORINFO_CONTEXTFLAGS_METHOD = 0x00, // CORINFO_CONTEXT_HANDLE is really a CORINFO_METHOD_HANDLE
        CORINFO_CONTEXTFLAGS_CLASS = 0x01, // CORINFO_CONTEXT_HANDLE is really a CORINFO_CLASS_HANDLE
        CORINFO_CONTEXTFLAGS_MASK = 0x01
    };

    public enum CorInfoSigInfoFlags : byte
    {
        CORINFO_SIGFLAG_IS_LOCAL_SIG = 0x01,
        CORINFO_SIGFLAG_IL_STUB = 0x02,
        CORINFO_SIGFLAG_SUPPRESS_GC_TRANSITION = 0x04,
        CORINFO_SIGFLAG_FAT_CALL = 0x08,
    };

    // These are returned from getMethodOptions
    public enum CorInfoOptions
    {
        CORINFO_OPT_INIT_LOCALS = 0x00000010, // zero initialize all variables

        CORINFO_GENERICS_CTXT_FROM_THIS = 0x00000020, // is this shared generic code that access the generic context from the this pointer?  If so, then if the method has SEH then the 'this' pointer must always be reported and kept alive.
        CORINFO_GENERICS_CTXT_FROM_METHODDESC = 0x00000040, // is this shared generic code that access the generic context from the ParamTypeArg(that is a MethodDesc)?  If so, then if the method has SEH then the 'ParamTypeArg' must always be reported and kept alive. Same as CORINFO_CALLCONV_PARAMTYPE
        CORINFO_GENERICS_CTXT_FROM_METHODTABLE = 0x00000080, // is this shared generic code that access the generic context from the ParamTypeArg(that is a MethodTable)?  If so, then if the method has SEH then the 'ParamTypeArg' must always be reported and kept alive. Same as CORINFO_CALLCONV_PARAMTYPE
        CORINFO_GENERICS_CTXT_MASK = (CORINFO_GENERICS_CTXT_FROM_THIS |
                                                   CORINFO_GENERICS_CTXT_FROM_METHODDESC |
                                                   CORINFO_GENERICS_CTXT_FROM_METHODTABLE),
        CORINFO_GENERICS_CTXT_KEEP_ALIVE = 0x00000100, // Keep the generics context alive throughout the method even if there is no explicit use, and report its location to the CLR
    }

    public enum CorInfoIntrinsics
    {
        CORINFO_INTRINSIC_GetChar,              // fetch character out of string
        CORINFO_INTRINSIC_Array_GetDimLength,   // Get number of elements in a given dimension of an array
        CORINFO_INTRINSIC_Array_Get,            // Get the value of an element in an array
        CORINFO_INTRINSIC_Array_Address,        // Get the address of an element in an array
        CORINFO_INTRINSIC_Array_Set,            // Set the value of an element in an array
        CORINFO_INTRINSIC_StringGetChar,        // fetch character out of string
        CORINFO_INTRINSIC_StringLength,         // get the length
        CORINFO_INTRINSIC_InitializeArray,      // initialize an array from static data
        CORINFO_INTRINSIC_GetTypeFromHandle,
        CORINFO_INTRINSIC_RTH_GetValueInternal,
        CORINFO_INTRINSIC_TypeEQ,
        CORINFO_INTRINSIC_TypeNEQ,
        CORINFO_INTRINSIC_Object_GetType,
        CORINFO_INTRINSIC_StubHelpers_GetStubContext,
        CORINFO_INTRINSIC_StubHelpers_GetStubContextAddr,
        CORINFO_INTRINSIC_StubHelpers_GetNDirectTarget,
        CORINFO_INTRINSIC_StubHelpers_NextCallReturnAddress,
        CORINFO_INTRINSIC_InterlockedAdd32,
        CORINFO_INTRINSIC_InterlockedAdd64,
        CORINFO_INTRINSIC_InterlockedXAdd32,
        CORINFO_INTRINSIC_InterlockedXAdd64,
        CORINFO_INTRINSIC_InterlockedXchg32,
        CORINFO_INTRINSIC_InterlockedXchg64,
        CORINFO_INTRINSIC_InterlockedCmpXchg32,
        CORINFO_INTRINSIC_InterlockedCmpXchg64,
        CORINFO_INTRINSIC_MemoryBarrier,
        CORINFO_INTRINSIC_MemoryBarrierLoad,
        CORINFO_INTRINSIC_GetCurrentManagedThread,
        CORINFO_INTRINSIC_GetManagedThreadId,
        CORINFO_INTRINSIC_ByReference_Ctor,
        CORINFO_INTRINSIC_ByReference_Value,
        CORINFO_INTRINSIC_Span_GetItem,
        CORINFO_INTRINSIC_ReadOnlySpan_GetItem,
        CORINFO_INTRINSIC_GetRawHandle,

        CORINFO_INTRINSIC_Count,
        CORINFO_INTRINSIC_Illegal = -1,         // Not a true intrinsic,
    }

    // Can a value be accessed directly from JITed code.
    public enum InfoAccessType
    {
        IAT_VALUE,      // The info value is directly available
        IAT_PVALUE,     // The value needs to be accessed via an       indirection
        IAT_PPVALUE,    // The value needs to be accessed via a double indirection
        IAT_RELPVALUE   // The value needs to be accessed via a relative indirection
    }

    public enum CorInfoGCType
    {
        TYPE_GC_NONE,   // no embedded objectrefs
        TYPE_GC_REF,    // Is an object ref
        TYPE_GC_BYREF,  // Is an interior pointer - promote it but don't scan it
        TYPE_GC_OTHER   // requires type-specific treatment
    }

    public enum CorInfoClassId
    {
        CLASSID_SYSTEM_OBJECT,
        CLASSID_TYPED_BYREF,
        CLASSID_TYPE_HANDLE,
        CLASSID_FIELD_HANDLE,
        CLASSID_METHOD_HANDLE,
        CLASSID_STRING,
        CLASSID_ARGUMENT_HANDLE,
        CLASSID_RUNTIME_TYPE,
    }
    public enum CorInfoInline
    {
        INLINE_PASS = 0,    // Inlining OK

        // failures are negative
        INLINE_FAIL = -1,   // Inlining not OK for this case only
        INLINE_NEVER = -2,   // This method should never be inlined, regardless of context
    }

    public enum CorInfoInlineTypeCheck
    {
        CORINFO_INLINE_TYPECHECK_NONE = 0x00000000, // It's not okay to compare type's vtable with a native type handle
        CORINFO_INLINE_TYPECHECK_PASS = 0x00000001, // It's okay to compare type's vtable with a native type handle
        CORINFO_INLINE_TYPECHECK_USE_HELPER = 0x00000002, // Use a specialized helper to compare type's vtable with native type handle
    }

    public enum CorInfoInlineTypeCheckSource
    {
        CORINFO_INLINE_TYPECHECK_SOURCE_VTABLE = 0x00000000, // Type handle comes from the vtable
        CORINFO_INLINE_TYPECHECK_SOURCE_TOKEN  = 0x00000001, // Type handle comes from an ldtoken
    }

    public enum CorInfoInlineRestrictions
    {
        INLINE_RESPECT_BOUNDARY = 0x00000001, // You can inline if there are no calls from the method being inlined
        INLINE_NO_CALLEE_LDSTR = 0x00000002, // You can inline only if you guarantee that if inlinee does an ldstr
        // inlinee's module will never see that string (by any means).
        // This is due to how we implement the NoStringInterningAttribute
        // (by reusing the fixup table).
        INLINE_SAME_THIS = 0x00000004, // You can inline only if the callee is on the same this reference as caller
    }

    // If you add more values here, keep it in sync with TailCallTypeMap in ..\vm\ClrEtwAll.man
    // and the string enum in CEEInfo::reportTailCallDecision in ..\vm\JITInterface.cpp
    public enum CorInfoTailCall
    {
        TAILCALL_OPTIMIZED = 0,    // Optimized tail call (epilog + jmp)
        TAILCALL_RECURSIVE = 1,    // Optimized into a loop (only when a method tail calls itself)
        TAILCALL_HELPER = 2,    // Helper assisted tail call (call to JIT_TailCall)

        // failures are negative
        TAILCALL_FAIL = -1,   // Couldn't do a tail call
    }

    public enum CorInfoCanSkipVerificationResult
    {
        CORINFO_VERIFICATION_CANNOT_SKIP = 0,    // Cannot skip verification during jit time.
        CORINFO_VERIFICATION_CAN_SKIP = 1,    // Can skip verification during jit time.
        CORINFO_VERIFICATION_RUNTIME_CHECK = 2,    // Cannot skip verification during jit time,
        //     but need to insert a callout to the VM to ask during runtime
        //     whether to raise a verification or not (if the method is unverifiable).
        CORINFO_VERIFICATION_DONT_JIT = 3,    // Cannot skip verification during jit time,
        //     but do not jit the method if is is unverifiable.
    }

    public enum CorInfoInitClassResult
    {
        CORINFO_INITCLASS_NOT_REQUIRED = 0x00, // No class initialization required, but the class is not actually initialized yet
        // (e.g. we are guaranteed to run the static constructor in method prolog)
        CORINFO_INITCLASS_INITIALIZED = 0x01, // Class initialized
        CORINFO_INITCLASS_USE_HELPER = 0x02, // The JIT must insert class initialization helper call.
        CORINFO_INITCLASS_DONT_INLINE = 0x04, // The JIT should not inline the method requesting the class initialization. The class
        // initialization requires helper class now, but will not require initialization
        // if the method is compiled standalone. Or the method cannot be inlined due to some
        // requirement around class initialization such as shared generics.
    }

    public enum CORINFO_ACCESS_FLAGS
    {
        CORINFO_ACCESS_ANY = 0x0000, // Normal access
        CORINFO_ACCESS_THIS = 0x0001, // Accessed via the this reference
        // CORINFO_ACCESS_UNUSED = 0x0002,

        CORINFO_ACCESS_NONNULL = 0x0004, // Instance is guaranteed non-null

        CORINFO_ACCESS_LDFTN = 0x0010, // Accessed via ldftn

        // Field access flags
        CORINFO_ACCESS_GET = 0x0100, // Field get (ldfld)
        CORINFO_ACCESS_SET = 0x0200, // Field set (stfld)
        CORINFO_ACCESS_ADDRESS = 0x0400, // Field address (ldflda)
        CORINFO_ACCESS_INIT_ARRAY = 0x0800, // Field use for InitializeArray
        CORINFO_ACCESS_ATYPICAL_CALLSITE = 0x4000, // Atypical callsite that cannot be disassembled by delay loading helper
        CORINFO_ACCESS_INLINECHECK = 0x8000, // Return fieldFlags and fieldAccessor only. Used by JIT64 during inlining.
    }


    // these are the attribute flags for fields and methods (getMethodAttribs)
    [Flags]
    public enum CorInfoFlag : uint
    {
        //  CORINFO_FLG_UNUSED                = 0x00000001,
        //  CORINFO_FLG_UNUSED                = 0x00000002,
        CORINFO_FLG_PROTECTED = 0x00000004,
        CORINFO_FLG_STATIC = 0x00000008,
        CORINFO_FLG_FINAL = 0x00000010,
        CORINFO_FLG_SYNCH = 0x00000020,
        CORINFO_FLG_VIRTUAL = 0x00000040,
        //  CORINFO_FLG_UNUSED                = 0x00000080,
        CORINFO_FLG_NATIVE = 0x00000100,
        CORINFO_FLG_INTRINSIC_TYPE = 0x00000200, // This type is marked by [Intrinsic]
        CORINFO_FLG_ABSTRACT = 0x00000400,

        CORINFO_FLG_EnC = 0x00000800, // member was added by Edit'n'Continue

        // These are internal flags that can only be on methods
        CORINFO_FLG_FORCEINLINE = 0x00010000, // The method should be inlined if possible.
        CORINFO_FLG_SHAREDINST = 0x00020000, // the code for this method is shared between different generic instantiations (also set on classes/types)
        CORINFO_FLG_DELEGATE_INVOKE = 0x00040000, // "Delegate
        CORINFO_FLG_PINVOKE = 0x00080000, // Is a P/Invoke call
        // CORINFO_FLG_UNUSED = 0x00100000,
        CORINFO_FLG_NOGCCHECK = 0x00200000, // This method is FCALL that has no GC check.  Don't put alone in loops
        CORINFO_FLG_INTRINSIC = 0x00400000, // This method MAY have an intrinsic ID
        CORINFO_FLG_CONSTRUCTOR = 0x00800000, // This method is an instance or type initializer
        CORINFO_FLG_AGGRESSIVE_OPT = 0x01000000, // The method may contain hot code and should be aggressively optimized if possible
        CORINFO_FLG_DISABLE_TIER0_FOR_LOOPS = 0x02000000, // Indicates that tier 0 JIT should not be used for a method that contains a loop
        // CORINFO_FLG_UNUSED = 0x04000000,
        CORINFO_FLG_DONT_INLINE = 0x10000000, // The method should not be inlined
        CORINFO_FLG_DONT_INLINE_CALLER = 0x20000000, // The method should not be inlined, nor should its callers. It cannot be tail called.
        CORINFO_FLG_JIT_INTRINSIC = 0x40000000, // Method is a potential jit intrinsic; verify identity by name check

        // These are internal flags that can only be on Classes
        CORINFO_FLG_VALUECLASS = 0x00010000, // is the class a value class
        //  This flag is define din the Methods section, but is also valid on classes.
        //  CORINFO_FLG_SHAREDINST            = 0x00020000, // This class is satisfies TypeHandle::IsCanonicalSubtype
        CORINFO_FLG_VAROBJSIZE = 0x00040000, // the object size varies depending of constructor args
        CORINFO_FLG_ARRAY = 0x00080000, // class is an array class (initialized differently)
        CORINFO_FLG_OVERLAPPING_FIELDS = 0x00100000, // struct or class has fields that overlap (aka union)
        CORINFO_FLG_INTERFACE = 0x00200000, // it is an interface
        CORINFO_FLG_DONT_PROMOTE = 0x00400000, // don't try to promote fieds of types outside of AOT compilation version bubble
        CORINFO_FLG_CUSTOMLAYOUT = 0x00800000, // does this struct have custom layout?
        CORINFO_FLG_CONTAINS_GC_PTR = 0x01000000, // does the class contain a gc ptr ?
        CORINFO_FLG_DELEGATE = 0x02000000, // is this a subclass of delegate or multicast delegate ?
        // CORINFO_FLG_UNUSED = 0x04000000,
        CORINFO_FLG_CONTAINS_STACK_PTR = 0x08000000, // This class has a stack pointer inside it
        CORINFO_FLG_VARIANCE = 0x10000000, // MethodTable::HasVariance (sealed does *not* mean uncast-able)
        CORINFO_FLG_BEFOREFIELDINIT = 0x20000000, // Additional flexibility for when to run .cctor (see code:#ClassConstructionFlags)
        CORINFO_FLG_GENERIC_TYPE_VARIABLE = 0x40000000, // This is really a handle for a variable type
        CORINFO_FLG_UNSAFE_VALUECLASS = 0x80000000, // Unsafe (C++'s /GS) value type
    }


    //----------------------------------------------------------------------------
    // Exception handling

    // These are the flags set on an CORINFO_EH_CLAUSE
    public enum CORINFO_EH_CLAUSE_FLAGS
    {
        CORINFO_EH_CLAUSE_NONE = 0,
        CORINFO_EH_CLAUSE_FILTER = 0x0001, // If this bit is on, then this EH entry is for a filter
        CORINFO_EH_CLAUSE_FINALLY = 0x0002, // This clause is a finally clause
        CORINFO_EH_CLAUSE_FAULT = 0x0004, // This clause is a fault clause
        CORINFO_EH_CLAUSE_DUPLICATED = 0x0008, // Duplicated clause. This clause was duplicated to a funclet which was pulled out of line
        CORINFO_EH_CLAUSE_SAMETRY = 0x0010, // This clause covers same try block as the previous one. (Used by CoreRT ABI.)
    };

    public struct CORINFO_EH_CLAUSE
    {
        public CORINFO_EH_CLAUSE_FLAGS Flags;
        public uint TryOffset;
        public uint TryLength;
        public uint HandlerOffset;
        public uint HandlerLength;
        public uint ClassTokenOrOffset;
        /*        union
                {
                    DWORD                   ClassToken;       // use for type-based exception handlers
                    DWORD                   FilterOffset;     // use for filter-based exception handlers (COR_ILEXCEPTION_FILTER is set)
                };*/
    }

    public struct BlockCounts  // Also defined here: code:CORBBTPROF_BLOCK_DATA
    {
        public uint ILOffset;
        public uint ExecutionCount;
    }

    // The enumeration is returned in 'getSig','getType', getArgType methods
    public enum CorInfoType
    {
        CORINFO_TYPE_UNDEF = 0x0,
        CORINFO_TYPE_VOID = 0x1,
        CORINFO_TYPE_BOOL = 0x2,
        CORINFO_TYPE_CHAR = 0x3,
        CORINFO_TYPE_BYTE = 0x4,
        CORINFO_TYPE_UBYTE = 0x5,
        CORINFO_TYPE_SHORT = 0x6,
        CORINFO_TYPE_USHORT = 0x7,
        CORINFO_TYPE_INT = 0x8,
        CORINFO_TYPE_UINT = 0x9,
        CORINFO_TYPE_LONG = 0xa,
        CORINFO_TYPE_ULONG = 0xb,
        CORINFO_TYPE_NATIVEINT = 0xc,
        CORINFO_TYPE_NATIVEUINT = 0xd,
        CORINFO_TYPE_FLOAT = 0xe,
        CORINFO_TYPE_DOUBLE = 0xf,
        CORINFO_TYPE_STRING = 0x10,         // Not used, should remove
        CORINFO_TYPE_PTR = 0x11,
        CORINFO_TYPE_BYREF = 0x12,
        CORINFO_TYPE_VALUECLASS = 0x13,
        CORINFO_TYPE_CLASS = 0x14,
        CORINFO_TYPE_REFANY = 0x15,

        // CORINFO_TYPE_VAR is for a generic type variable.
        // Generic type variables only appear when the JIT is doing
        // verification (not NOT compilation) of generic code
        // for the EE, in which case we're running
        // the JIT in "import only" mode.

        CORINFO_TYPE_VAR = 0x16,
        CORINFO_TYPE_COUNT,                         // number of jit types
    }

    public enum CorInfoIsAccessAllowedResult
    {
        CORINFO_ACCESS_ALLOWED = 0,           // Call allowed
        CORINFO_ACCESS_ILLEGAL = 1,           // Call not allowed
    }

    //----------------------------------------------------------------------------
    // Embedding type, method and field handles (for "ldtoken" or to pass back to helpers)

    // Result of calling embedGenericHandle
    public unsafe struct CORINFO_GENERICHANDLE_RESULT
    {
        public CORINFO_LOOKUP lookup;

        // compileTimeHandle is guaranteed to be either NULL or a handle that is usable during compile time.
        // It must not be embedded in the code because it might not be valid at run-time.
        public CORINFO_GENERIC_STRUCT_* compileTimeHandle;

        // Type of the result
        public CorInfoGenericHandleType handleType;
    }

    public enum CorInfoGenericHandleType
    {
        CORINFO_HANDLETYPE_UNKNOWN,
        CORINFO_HANDLETYPE_CLASS,
        CORINFO_HANDLETYPE_METHOD,
        CORINFO_HANDLETYPE_FIELD
    }

    // Enum used for HFA type recognition.
    // Supported across architectures, so that it can be used in altjits and cross-compilation.
    public enum CorInfoHFAElemType
    {
        CORINFO_HFA_ELEM_NONE,
        CORINFO_HFA_ELEM_FLOAT,
        CORINFO_HFA_ELEM_DOUBLE,
        CORINFO_HFA_ELEM_VECTOR64,
        CORINFO_HFA_ELEM_VECTOR128,
    }

    /* data to optimize delegate construction */
    public unsafe struct DelegateCtorArgs
    {
        public void* pMethod;
        public void* pArg3;
        public void* pArg4;
        public void* pArg5;
    }

    /*****************************************************************************/
    // These are flags passed to ICorJitInfo::allocMem
    // to guide the memory allocation for the code, readonly data, and read-write data
    public enum CorJitAllocMemFlag
    {
        CORJIT_ALLOCMEM_DEFAULT_CODE_ALIGN = 0x00000000, // The code will be use the normal alignment
        CORJIT_ALLOCMEM_FLG_16BYTE_ALIGN = 0x00000001, // The code will be 16-byte aligned
        CORJIT_ALLOCMEM_FLG_RODATA_16BYTE_ALIGN = 0x00000002, // The read-only data will be 16-byte aligned
        CORJIT_ALLOCMEM_FLG_32BYTE_ALIGN   = 0x00000004, // The code will be 32-byte aligned
        CORJIT_ALLOCMEM_FLG_RODATA_32BYTE_ALIGN = 0x00000008, // The read-only data will be 32-byte aligned
    }

    public enum CorJitFuncKind
    {
        CORJIT_FUNC_ROOT,          // The main/root function (always id==0)
        CORJIT_FUNC_HANDLER,       // a funclet associated with an EH handler (finally, fault, catch, filter handler)
        CORJIT_FUNC_FILTER         // a funclet associated with an EH filter
    }

    public unsafe struct CORINFO_METHOD_INFO
    {
        public CORINFO_METHOD_STRUCT_* ftn;
        public CORINFO_MODULE_STRUCT_* scope;
        public byte* ILCode;
        public uint ILCodeSize;
        public uint maxStack;
        public uint EHcount;
        public CorInfoOptions options;
        public CorInfoRegionKind regionKind;
        public CORINFO_SIG_INFO args;
        public CORINFO_SIG_INFO locals;
    }
    //
    // what type of code region we are in
    //
    public enum CorInfoRegionKind
    {
        CORINFO_REGION_NONE,
        CORINFO_REGION_HOT,
        CORINFO_REGION_COLD,
        CORINFO_REGION_JIT,
    }

    public enum CorInfoTypeWithMod
    {
        CORINFO_TYPE_MASK = 0x3F,        // lower 6 bits are type mask
        CORINFO_TYPE_MOD_PINNED = 0x40,        // can be applied to CLASS, or BYREF to indiate pinned
    };

    public struct CORINFO_HELPER_ARG
    {
        public IntPtr argHandle;
        public CorInfoAccessAllowedHelperArgType argType;
    }

    public enum CorInfoAccessAllowedHelperArgType
    {
        CORINFO_HELPER_ARG_TYPE_Invalid = 0,
        CORINFO_HELPER_ARG_TYPE_Field = 1,
        CORINFO_HELPER_ARG_TYPE_Method = 2,
        CORINFO_HELPER_ARG_TYPE_Class = 3,
        CORINFO_HELPER_ARG_TYPE_Module = 4,
        CORINFO_HELPER_ARG_TYPE_Const = 5,
    }

    public struct CORINFO_HELPER_DESC
    {
        public CorInfoHelpFunc helperNum;
        public uint numArgs;
        public CORINFO_HELPER_ARG args0;
        public CORINFO_HELPER_ARG args1;
        public CORINFO_HELPER_ARG args2;
        public CORINFO_HELPER_ARG args3;
    }


    public enum CORINFO_OS
    {
        CORINFO_WINNT,
        CORINFO_UNIX,
    }

    public enum CORINFO_RUNTIME_ABI
    {
        CORINFO_DESKTOP_ABI = 0x100,
        CORINFO_CORECLR_ABI = 0x200,
        CORINFO_CORERT_ABI = 0x300,
    }

    // For some highly optimized paths, the JIT must generate code that directly
    // manipulates internal EE data structures. The getEEInfo() helper returns
    // this structure containing the needed offsets and values.
    public struct CORINFO_EE_INFO
    {
        // Information about the InlinedCallFrame structure layout
        public struct InlinedCallFrameInfo
        {
            // Size of the Frame structure
            public uint size;

            public uint offsetOfGSCookie;
            public uint offsetOfFrameVptr;
            public uint offsetOfFrameLink;
            public uint offsetOfCallSiteSP;
            public uint offsetOfCalleeSavedFP;
            public uint offsetOfCallTarget;
            public uint offsetOfReturnAddress;
            public uint offsetOfSPAfterProlog;
        }
        public InlinedCallFrameInfo inlinedCallFrameInfo;

        // Offsets into the Thread structure
        public uint offsetOfThreadFrame;            // offset of the current Frame
        public uint offsetOfGCState;                // offset of the preemptive/cooperative state of the Thread

        // Delegate offsets
        public uint offsetOfDelegateInstance;
        public uint offsetOfDelegateFirstTarget;

        // Wrapper delegate offsets
        public uint offsetOfWrapperDelegateIndirectCell;

        // Reverse PInvoke offsets
        public uint sizeOfReversePInvokeFrame;

        // OS Page size
        public UIntPtr osPageSize;

        // Null object offset
        public UIntPtr maxUncheckedOffsetForNullObject;

        // Target ABI. Combined with target architecture and OS to determine
        // GC, EH, and unwind styles.
        public CORINFO_RUNTIME_ABI targetAbi;

        public CORINFO_OS osType;
    }

    // Flags passed from JIT to runtime.
    public enum CORINFO_GET_TAILCALL_HELPERS_FLAGS
    {
        // The callsite is a callvirt instruction.
        CORINFO_TAILCALL_IS_CALLVIRT = 0x00000001,
    }

    // Flags passed from runtime to JIT.
    public enum CORINFO_TAILCALL_HELPERS_FLAGS
    {
        // The StoreArgs stub needs to be passed the target function pointer as the
        // first argument.
        CORINFO_TAILCALL_STORE_TARGET = 0x00000001,
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CORINFO_TAILCALL_HELPERS
    {
        CORINFO_TAILCALL_HELPERS_FLAGS flags;
        CORINFO_METHOD_STRUCT_*        hStoreArgs;
        CORINFO_METHOD_STRUCT_*        hCallTarget;
        CORINFO_METHOD_STRUCT_*        hDispatcher;
    };

    public enum CORINFO_THIS_TRANSFORM
    {
        CORINFO_NO_THIS_TRANSFORM,
        CORINFO_BOX_THIS,
        CORINFO_DEREF_THIS
    };

    //----------------------------------------------------------------------------
    // getCallInfo and CORINFO_CALL_INFO: The EE instructs the JIT about how to make a call
    //
    // callKind
    // --------
    //
    // CORINFO_CALL :
    //   Indicates that the JIT can use getFunctionEntryPoint to make a call,
    //   i.e. there is nothing abnormal about the call.  The JITs know what to do if they get this.
    //   Except in the case of constraint calls (see below), [targetMethodHandle] will hold
    //   the CORINFO_METHOD_HANDLE that a call to findMethod would
    //   have returned.
    //   This flag may be combined with nullInstanceCheck=TRUE for uses of callvirt on methods that can
    //   be resolved at compile-time (non-virtual, final or sealed).
    //
    // CORINFO_CALL_CODE_POINTER (shared generic code only) :
    //   Indicates that the JIT should do an indirect call to the entrypoint given by address, which may be specified
    //   as a runtime lookup by CORINFO_CALL_INFO::codePointerLookup.
    //   [targetMethodHandle] will not hold a valid value.
    //   This flag may be combined with nullInstanceCheck=TRUE for uses of callvirt on methods whose target method can
    //   be resolved at compile-time but whose instantiation can be resolved only through runtime lookup.
    //
    // CORINFO_VIRTUALCALL_STUB (interface calls) :
    //   Indicates that the EE supports "stub dispatch" and request the JIT to make a
    //   "stub dispatch" call (an indirect call through CORINFO_CALL_INFO::stubLookup,
    //   similar to CORINFO_CALL_CODE_POINTER).
    //   "Stub dispatch" is a specialized calling sequence (that may require use of NOPs)
    //   which allow the runtime to determine the call-site after the call has been dispatched.
    //   If the call is too complex for the JIT (e.g. because
    //   fetching the dispatch stub requires a runtime lookup, i.e. lookupKind.needsRuntimeLookup
    //   is set) then the JIT is allowed to implement the call as if it were CORINFO_VIRTUALCALL_LDVIRTFTN
    //   [targetMethodHandle] will hold the CORINFO_METHOD_HANDLE that a call to findMethod would
    //   have returned.
    //   This flag is always accompanied by nullInstanceCheck=TRUE.
    //
    // CORINFO_VIRTUALCALL_LDVIRTFTN (virtual generic methods) :
    //   Indicates that the EE provides no way to implement the call directly and
    //   that the JIT should use a LDVIRTFTN sequence (as implemented by CORINFO_HELP_VIRTUAL_FUNC_PTR)
    //   followed by an indirect call.
    //   [targetMethodHandle] will hold the CORINFO_METHOD_HANDLE that a call to findMethod would
    //   have returned.
    //   This flag is always accompanied by nullInstanceCheck=TRUE though typically the null check will
    //   be implicit in the access through the instance pointer.
    //
    //  CORINFO_VIRTUALCALL_VTABLE (regular virtual methods) :
    //   Indicates that the EE supports vtable dispatch and that the JIT should use getVTableOffset etc.
    //   to implement the call.
    //   [targetMethodHandle] will hold the CORINFO_METHOD_HANDLE that a call to findMethod would
    //   have returned.
    //   This flag is always accompanied by nullInstanceCheck=TRUE though typically the null check will
    //   be implicit in the access through the instance pointer.
    //
    // thisTransform and constraint calls
    // ----------------------------------
    //
    // For everything besides "constrained." calls "thisTransform" is set to
    // CORINFO_NO_THIS_TRANSFORM.
    //
    // For "constrained." calls the EE attempts to resolve the call at compile
    // time to a more specific method, or (shared generic code only) to a runtime lookup
    // for a code pointer for the more specific method.
    //
    // In order to permit this, the "this" pointer supplied for a "constrained." call
    // is a byref to an arbitrary type (see the IL spec). The "thisTransform" field
    // will indicate how the JIT must transform the "this" pointer in order
    // to be able to call the resolved method:
    //
    //  CORINFO_NO_THIS_TRANSFORM --> Leave it as a byref to an unboxed value type
    //  CORINFO_BOX_THIS          --> Box it to produce an object
    //  CORINFO_DEREF_THIS        --> Deref the byref to get an object reference
    //
    // In addition, the "kind" field will be set as follows for constraint calls:

    //    CORINFO_CALL              --> the call was resolved at compile time, and
    //                                  can be compiled like a normal call.
    //    CORINFO_CALL_CODE_POINTER --> the call was resolved, but the target address will be
    //                                  computed at runtime.  Only returned for shared generic code.
    //    CORINFO_VIRTUALCALL_STUB,
    //    CORINFO_VIRTUALCALL_LDVIRTFTN,
    //    CORINFO_VIRTUALCALL_VTABLE   --> usual values indicating that a virtual call must be made

    public enum CORINFO_CALL_KIND
    {
        CORINFO_CALL,
        CORINFO_CALL_CODE_POINTER,
        CORINFO_VIRTUALCALL_STUB,
        CORINFO_VIRTUALCALL_LDVIRTFTN,
        CORINFO_VIRTUALCALL_VTABLE
    };

    public enum CORINFO_VIRTUALCALL_NO_CHUNK : uint
    {
        Value = 0xFFFFFFFF,
    }

    public unsafe struct CORINFO_CALL_INFO
    {
        public CORINFO_METHOD_STRUCT_* hMethod;            //target method handle
        public uint methodFlags;        //flags for the target method

        public uint classFlags;         //flags for CORINFO_RESOLVED_TOKEN::hClass

        public CORINFO_SIG_INFO sig;

        //Verification information
        public uint verMethodFlags;     // flags for CORINFO_RESOLVED_TOKEN::hMethod
        public CORINFO_SIG_INFO verSig;
        //All of the regular method data is the same... hMethod might not be the same as CORINFO_RESOLVED_TOKEN::hMethod


        //If set to:
        //  - CORINFO_ACCESS_ALLOWED - The access is allowed.
        //  - CORINFO_ACCESS_ILLEGAL - This access cannot be allowed (i.e. it is public calling private).  The
        //      JIT may either insert the callsiteCalloutHelper into the code (as per a verification error) or
        //      call throwExceptionFromHelper on the callsiteCalloutHelper.  In this case callsiteCalloutHelper
        //      is guaranteed not to return.
        public CorInfoIsAccessAllowedResult accessAllowed;
        public CORINFO_HELPER_DESC callsiteCalloutHelper;

        // See above section on constraintCalls to understand when these are set to unusual values.
        public CORINFO_THIS_TRANSFORM thisTransform;

        public CORINFO_CALL_KIND kind;

        public uint _nullInstanceCheck;
        public bool nullInstanceCheck { get { return _nullInstanceCheck != 0; } set { _nullInstanceCheck = value ? (byte)1 : (byte)0; } }

        // Context for inlining and hidden arg
        public CORINFO_CONTEXT_STRUCT* contextHandle;

        public uint _exactContextNeedsRuntimeLookup; // Set if contextHandle is approx handle. Runtime lookup is required to get the exact handle.
        public bool exactContextNeedsRuntimeLookup { get { return _exactContextNeedsRuntimeLookup != 0; } set { _exactContextNeedsRuntimeLookup = value ? (byte)1 : (byte)0; } }

        // If kind.CORINFO_VIRTUALCALL_STUB then stubLookup will be set.
        // If kind.CORINFO_CALL_CODE_POINTER then entryPointLookup will be set.
        public CORINFO_LOOKUP codePointerOrStubLookup;

        // Used by Ready-to-Run
        public CORINFO_CONST_LOOKUP instParamLookup;

        public uint _wrapperDelegateInvoke;
        public bool wrapperDelegateInvoke { get { return _wrapperDelegateInvoke != 0; } set { _wrapperDelegateInvoke = value ? (byte)1 : (byte)0; } }
    }


    //----------------------------------------------------------------------------
    // getFieldInfo and CORINFO_FIELD_INFO: The EE instructs the JIT about how to access a field

    public enum CORINFO_FIELD_ACCESSOR
    {
        CORINFO_FIELD_INSTANCE,                 // regular instance field at given offset from this-ptr
        CORINFO_FIELD_INSTANCE_WITH_BASE,       // instance field with base offset (used by Ready-to-Run)
        CORINFO_FIELD_INSTANCE_HELPER,          // instance field accessed using helper (arguments are this, FieldDesc * and the value)
        CORINFO_FIELD_INSTANCE_ADDR_HELPER,     // instance field accessed using address-of helper (arguments are this and FieldDesc *)

        CORINFO_FIELD_STATIC_ADDRESS,           // field at given address
        CORINFO_FIELD_STATIC_RVA_ADDRESS,       // RVA field at given address
        CORINFO_FIELD_STATIC_SHARED_STATIC_HELPER, // static field accessed using the "shared static" helper (arguments are ModuleID + ClassID)
        CORINFO_FIELD_STATIC_GENERICS_STATIC_HELPER, // static field access using the "generic static" helper (argument is MethodTable *)
        CORINFO_FIELD_STATIC_ADDR_HELPER,       // static field accessed using address-of helper (argument is FieldDesc *)
        CORINFO_FIELD_STATIC_TLS,               // unmanaged TLS access
        CORINFO_FIELD_STATIC_READYTORUN_HELPER, // static field access using a runtime lookup helper

        CORINFO_FIELD_INTRINSIC_ZERO,           // intrinsic zero (IntPtr.Zero, UIntPtr.Zero)
        CORINFO_FIELD_INTRINSIC_EMPTY_STRING,   // intrinsic emptry string (String.Empty)
        CORINFO_FIELD_INTRINSIC_ISLITTLEENDIAN, // intrinsic BitConverter.IsLittleEndian
    }

    // Set of flags returned in CORINFO_FIELD_INFO::fieldFlags
    public enum CORINFO_FIELD_FLAGS
    {
        CORINFO_FLG_FIELD_STATIC = 0x00000001,
        CORINFO_FLG_FIELD_UNMANAGED = 0x00000002, // RVA field
        CORINFO_FLG_FIELD_FINAL = 0x00000004,
        CORINFO_FLG_FIELD_STATIC_IN_HEAP = 0x00000008, // See code:#StaticFields. This static field is in the GC heap as a boxed object
        CORINFO_FLG_FIELD_SAFESTATIC_BYREF_RETURN = 0x00000010, // Field can be returned safely (has GC heap lifetime)
        CORINFO_FLG_FIELD_INITCLASS = 0x00000020, // initClass has to be called before accessing the field
        CORINFO_FLG_FIELD_PROTECTED = 0x00000040,
    }

    public unsafe struct CORINFO_FIELD_INFO
    {
        public CORINFO_FIELD_ACCESSOR fieldAccessor;
        public CORINFO_FIELD_FLAGS fieldFlags;

        // Helper to use if the field access requires it
        public CorInfoHelpFunc helper;

        // Field offset if there is one
        public uint offset;

        public CorInfoType fieldType;
        public CORINFO_CLASS_STRUCT_* structType; //possibly null

        //See CORINFO_CALL_INFO.accessAllowed
        public CorInfoIsAccessAllowedResult accessAllowed;
        public CORINFO_HELPER_DESC accessCalloutHelper;

        // Used by Ready-to-Run
        public CORINFO_CONST_LOOKUP fieldLookup;
    };

    // System V struct passing
    // The Classification types are described in the ABI spec at https://software.intel.com/sites/default/files/article/402129/mpx-linux64-abi.pdf
    public enum SystemVClassificationType : byte
    {
        SystemVClassificationTypeUnknown            = 0,
        SystemVClassificationTypeStruct             = 1,
        SystemVClassificationTypeNoClass            = 2,
        SystemVClassificationTypeMemory             = 3,
        SystemVClassificationTypeInteger            = 4,
        SystemVClassificationTypeIntegerReference   = 5,
        SystemVClassificationTypeIntegerByRef       = 6,
        SystemVClassificationTypeSSE                = 7,
        // SystemVClassificationTypeSSEUp           = Unused, // Not supported by the CLR.
        // SystemVClassificationTypeX87             = Unused, // Not supported by the CLR.
        // SystemVClassificationTypeX87Up           = Unused, // Not supported by the CLR.
        // SystemVClassificationTypeComplexX87      = Unused, // Not supported by the CLR.
    };

    public struct SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR
    {
        public const int CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS = 2;
        public const int CLR_SYSTEMV_MAX_STRUCT_BYTES_TO_PASS_IN_REGISTERS = 16;

        public const int SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES = 8; // Size of an eightbyte in bytes.
        public const int SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT = 16; // Maximum number of fields in struct passed in registers

        public byte _passedInRegisters;
        // Whether the struct is passable/passed (this includes struct returning) in registers.
        public bool passedInRegisters { get { return _passedInRegisters != 0; } set { _passedInRegisters = value ? (byte)1 : (byte)0; } }

        // Number of eightbytes for this struct.
        public byte eightByteCount;

        // The eightbytes type classification.
        public SystemVClassificationType eightByteClassifications0;
        public SystemVClassificationType eightByteClassifications1;

        // The size of the eightbytes (an eightbyte could include padding. This represents the no padding size of the eightbyte).
        public byte eightByteSizes0;
        public byte eightByteSizes1;

        // The start offset of the eightbytes (in bytes).
        public byte eightByteOffsets0;
        public byte eightByteOffsets1;
    };

    // DEBUGGER DATA
    public enum MappingTypes
    {
        NO_MAPPING = -1, // -- The IL offset corresponds to no source code (such as EH step blocks).
        PROLOG = -2,     // -- The IL offset indicates a prolog
        EPILOG = -3      // -- The IL offset indicates an epilog
    }

    public enum BoundaryTypes
    {
        NO_BOUNDARIES = 0x00,     // No implicit boundaries
        STACK_EMPTY_BOUNDARIES = 0x01,     // Boundary whenever the IL evaluation stack is empty
        NOP_BOUNDARIES = 0x02,     // Before every CEE_NOP instruction
        CALL_SITE_BOUNDARIES = 0x04,     // Before every CEE_CALL, CEE_CALLVIRT, etc instruction

        // Set of boundaries that debugger should always reasonably ask the JIT for.
        DEFAULT_BOUNDARIES = STACK_EMPTY_BOUNDARIES | NOP_BOUNDARIES | CALL_SITE_BOUNDARIES
    }

    // Note that SourceTypes can be OR'd together - it's possible that
    // a sequence point will also be a stack_empty point, and/or a call site.
    // The debugger will check to see if a boundary offset's source field &
    // SEQUENCE_POINT is true to determine if the boundary is a sequence point.
    [Flags]
    public enum SourceTypes
    {
        SOURCE_TYPE_INVALID = 0x00, // To indicate that nothing else applies
        SEQUENCE_POINT = 0x01, // The debugger asked for it.
        STACK_EMPTY = 0x02, // The stack is empty here
        CALL_SITE = 0x04, // This is a call site.
        NATIVE_END_OFFSET_UNKNOWN = 0x08, // Indicates a epilog endpoint
        CALL_INSTRUCTION = 0x10  // The actual instruction of a call.
    };

    public struct OffsetMapping
    {
        public uint nativeOffset;
        public uint ilOffset;
        public SourceTypes source; // The debugger needs this so that
        // we don't put Edit and Continue breakpoints where
        // the stack isn't empty.  We can put regular breakpoints
        // there, though, so we need a way to discriminate
        // between offsets.
    };

    public enum ILNum
    {
        VARARGS_HND_ILNUM   = -1, // Value for the CORINFO_VARARGS_HANDLE varNumber
        RETBUF_ILNUM        = -2, // Pointer to the return-buffer
        TYPECTXT_ILNUM      = -3, // ParamTypeArg for CORINFO_GENERICS_CTXT_FROM_PARAMTYPEARG

        UNKNOWN_ILNUM       = -4, // Unknown variable

        MAX_ILNUM           = -4  // Sentinal value. This should be set to the largest magnitude value in the enum
                                  // so that the compression routines know the enum's range.
    };

    public struct ILVarInfo
    {
        public uint startOffset;
        public uint endOffset;
        public uint varNumber;
    };

    // This enum is used for JIT to tell EE where this token comes from.
    // E.g. Depending on different opcodes, we might allow/disallow certain types of tokens or
    // return different types of handles (e.g. boxed vs. regular entrypoints)
    public enum CorInfoTokenKind
    {
        CORINFO_TOKENKIND_Class = 0x01,
        CORINFO_TOKENKIND_Method = 0x02,
        CORINFO_TOKENKIND_Field = 0x04,
        CORINFO_TOKENKIND_Mask = 0x07,

        // token comes from CEE_LDTOKEN
        CORINFO_TOKENKIND_Ldtoken = 0x10 | CORINFO_TOKENKIND_Class | CORINFO_TOKENKIND_Method | CORINFO_TOKENKIND_Field,

        // token comes from CEE_CASTCLASS or CEE_ISINST
        CORINFO_TOKENKIND_Casting = 0x20 | CORINFO_TOKENKIND_Class,

        // token comes from CEE_NEWARR
        CORINFO_TOKENKIND_Newarr = 0x40 | CORINFO_TOKENKIND_Class,

        // token comes from CEE_BOX
        CORINFO_TOKENKIND_Box = 0x80 | CORINFO_TOKENKIND_Class,

        // token comes from CEE_CONSTRAINED
        CORINFO_TOKENKIND_Constrained = 0x100 | CORINFO_TOKENKIND_Class,

        // token comes from CEE_NEWOBJ
        CORINFO_TOKENKIND_NewObj = 0x200 | CORINFO_TOKENKIND_Method,

        // token comes from CEE_LDVIRTFTN
        CORINFO_TOKENKIND_Ldvirtftn = 0x400 | CORINFO_TOKENKIND_Method,
    };

    // These are error codes returned by CompileMethod
    public enum CorJitResult
    {
        // Note that I dont use FACILITY_NULL for the facility number,
        // we may want to get a 'real' facility number
        CORJIT_OK = 0 /*NO_ERROR*/,
        CORJIT_BADCODE = unchecked((int)0x80000001)/*MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, 1)*/,
        CORJIT_OUTOFMEM = unchecked((int)0x80000002)/*MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, 2)*/,
        CORJIT_INTERNALERROR = unchecked((int)0x80000003)/*MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, 3)*/,
        CORJIT_SKIPPED = unchecked((int)0x80000004)/*MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, 4)*/,
        CORJIT_RECOVERABLEERROR = unchecked((int)0x80000005)/*MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, 5)*/
    };

    public enum TypeCompareState
    {
        MustNot = -1, // types are not equal
        May = 0,      // types may be equal (must test at runtime)
        Must = 1,     // type are equal
    }

    public enum CorJitFlag : uint
    {
        CORJIT_FLAG_CALL_GETJITFLAGS = 0xffffffff, // Indicates that the JIT should retrieve flags in the form of a
                                                   // pointer to a CORJIT_FLAGS value via ICorJitInfo::getJitFlags().
        CORJIT_FLAG_SPEED_OPT = 0,
        CORJIT_FLAG_SIZE_OPT = 1,
        CORJIT_FLAG_DEBUG_CODE = 2, // generate "debuggable" code (no code-mangling optimizations)
        CORJIT_FLAG_DEBUG_EnC = 3, // We are in Edit-n-Continue mode
        CORJIT_FLAG_DEBUG_INFO = 4, // generate line and local-var info
        CORJIT_FLAG_MIN_OPT = 5, // disable all jit optimizations (not necesarily debuggable code)
        CORJIT_FLAG_GCPOLL_CALLS = 6, // Emit calls to JIT_POLLGC for thread suspension.
        CORJIT_FLAG_MCJIT_BACKGROUND = 7, // Calling from multicore JIT background thread, do not call JitComplete
        CORJIT_FLAG_UNUSED1 = 8,
        CORJIT_FLAG_UNUSED2 = 9,
        CORJIT_FLAG_UNUSED3 = 10,
        CORJIT_FLAG_UNUSED4 = 11,
        CORJIT_FLAG_UNUSED5 = 12,
        CORJIT_FLAG_UNUSED6 = 13,
        CORJIT_FLAG_FEATURE_SIMD = 17,
        CORJIT_FLAG_MAKEFINALCODE = 18, // Use the final code generator, i.e., not the interpreter.
        CORJIT_FLAG_READYTORUN = 19, // Use version-resilient code generation
        CORJIT_FLAG_PROF_ENTERLEAVE = 20, // Instrument prologues/epilogues
        CORJIT_FLAG_PROF_REJIT_NOPS = 21, // Insert NOPs to ensure code is re-jitable
        CORJIT_FLAG_PROF_NO_PINVOKE_INLINE = 22, // Disables PInvoke inlining
        CORJIT_FLAG_SKIP_VERIFICATION = 23, // (lazy) skip verification - determined without doing a full resolve. See comment below
        CORJIT_FLAG_PREJIT = 24, // jit or prejit is the execution engine.
        CORJIT_FLAG_RELOC = 25, // Generate relocatable code
        CORJIT_FLAG_IMPORT_ONLY = 26, // Only import the function
        CORJIT_FLAG_IL_STUB = 27, // method is an IL stub
        CORJIT_FLAG_PROCSPLIT = 28, // JIT should separate code into hot and cold sections
        CORJIT_FLAG_BBINSTR = 29, // Collect basic block profile information
        CORJIT_FLAG_BBOPT = 30, // Optimize method based on profile information
        CORJIT_FLAG_FRAMED = 31, // All methods have an EBP frame
        CORJIT_FLAG_ALIGN_LOOPS = 32, // add NOPs before loops to align them at 16 byte boundaries
        CORJIT_FLAG_PUBLISH_SECRET_PARAM = 33, // JIT must place stub secret param into local 0.  (used by IL stubs)
        CORJIT_FLAG_GCPOLL_INLINE = 34, // JIT must inline calls to GCPoll when possible
        CORJIT_FLAG_SAMPLING_JIT_BACKGROUND = 35, // JIT is being invoked as a result of stack sampling for hot methods in the background
        CORJIT_FLAG_USE_PINVOKE_HELPERS = 36, // The JIT should use the PINVOKE_{BEGIN,END} helpers instead of emitting inline transitions
        CORJIT_FLAG_REVERSE_PINVOKE = 37, // The JIT should insert REVERSE_PINVOKE_{ENTER,EXIT} helpers into method prolog/epilog
        // CORJIT_FLAG_UNUSED = 38,
        CORJIT_FLAG_TIER0 = 39, // This is the initial tier for tiered compilation which should generate code as quickly as possible
        CORJIT_FLAG_TIER1 = 40, // This is the final tier (for now) for tiered compilation which should generate high quality code
        CORJIT_FLAG_RELATIVE_CODE_RELOCS = 41, // JIT should generate PC-relative address computations instead of EE relocation records
        CORJIT_FLAG_NO_INLINING = 42, // JIT should not inline any called method into this method
    }

    public struct CORJIT_FLAGS
    {
        private UInt64 _corJitFlags;
        public InstructionSetFlags InstructionSetFlags;

        public void Reset()
        {
            _corJitFlags = 0;
            InstructionSetFlags = default(InstructionSetFlags);
        }

        public void Set(CorJitFlag flag)
        {
            _corJitFlags |= 1UL << (int)flag;
        }

        public void Clear(CorJitFlag flag)
        {
            _corJitFlags &= ~(1UL << (int)flag);
        }

        public bool IsSet(CorJitFlag flag)
        {
            return (_corJitFlags & (1UL << (int)flag)) != 0;
        }
    }
}
