// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef INTERPRETER_H_DEFINED
#define INTERPRETER_H_DEFINED 1

#include "corjit.h"
#include "corinfo.h"
#include "codeman.h"
#include "jitinterface.h"
#include "stack.h"
#include "crst.h"
#include "callhelpers.h"
#include "codeversion.h"
#include "clr_std/type_traits"

typedef SSIZE_T NativeInt;
typedef SIZE_T NativeUInt;
typedef SIZE_T NativePtr;

// Determines whether we interpret IL stubs.  (We might disable this selectively for
// some architectures, perhaps.)
#define INTERP_ILSTUBS 1

// If this is set, we keep track of extra information about IL instructions executed per-method.
#define INTERP_PROFILE 0

// If this is set, we track the distribution of IL instructions.
#define INTERP_ILINSTR_PROFILE 0

#define INTERP_ILCYCLE_PROFILE 0
#if INTERP_ILCYCLE_PROFILE
#if !INTERP_ILINSTR_PROFILE
#error INTERP_ILCYCLE_PROFILE may only be set if INTERP_ILINSTR_PROFILE is also set.
#endif
#endif

#if defined(_DEBUG) || INTERP_ILINSTR_PROFILE
// I define "INTERP_TRACING", rather than just using _DEBUG, so that I can easily make a build
// in which tracing is enabled in retail.
#define INTERP_TRACING 1
#else
#define INTERP_TRACING 0
#endif // defined(_DEBUG) || defined(INTERP_ILINSTR_PROFILE)

#if INTERP_TRACING
#define INTERPLOG(...) if (s_TraceInterpreterVerboseFlag.val(CLRConfig::INTERNAL_TraceInterpreterVerbose)) { fprintf(GetLogFile(), __VA_ARGS__); }
#else
#define INTERPLOG(...)
#endif

#if INTERP_TRACING
#define InterpTracingArg(x) ,x
#else
#define InterpTracingArg(x)
#endif

#define FEATURE_INTERPRETER_DEADSIMPLE_OPT 0

#define NYI_INTERP(msg) _ASSERTE_MSG(false, msg)
// I wanted to define NYI_INTERP as the following in retail:
//   #define NYI_INTERP(msg) _ASSERTE_ALL_BUILDS(false)
// but doing so gave a very odd unreachable code error.


// To allow keeping a pointer (index) to the vararg cookie argument to implement arglist.
// Use sentinel value of NO_VA_ARGNUM.
#define NO_VA_ARGNUM UINT_MAX

// First, a set of utility routines on CorInfoTypes.

// Returns "true" iff "cit" is "stack-normal": all integer types with byte size less than 4
// are folded to CORINFO_TYPE_INT; all remaining unsigned types are folded to their signed counterparts.
bool IsStackNormalType(CorInfoType cit);

// Returns the stack-normal CorInfoType that contains "cit".
CorInfoType CorInfoTypeStackNormalize(CorInfoType cit);

// Returns the (byte) size of "cit".  Requires that "cit" is not a CORINFO_TYPE_VALUECLASS.
size_t CorInfoTypeSize(CorInfoType cit);

// Returns true iff "cit" is an unsigned integral type.
bool CorInfoTypeIsUnsigned(CorInfoType cit);

// Returns true iff "cit" is an integral type.
bool CorInfoTypeIsIntegral(CorInfoType cit);

// Returns true iff "cet" is an unsigned integral type.
bool CorElemTypeIsUnsigned(CorElementType cet);

// Returns true iff "cit" is a floating-point type.
bool CorInfoTypeIsFloatingPoint(CorInfoType cit);

// Returns true iff "cihet" is a floating-point type (float or double).
// TODO: Handle Vector64, Vector128?
bool CorInfoTypeIsFloatingPoint(CorInfoHFAElemType cihet);

// Returns true iff "cit" is a pointer type (mgd/unmgd pointer, or native int).
bool CorInfoTypeIsPointer(CorInfoType cit);

// Requires that "cit" is stack-normal; returns its (byte) size.
inline size_t CorInfoTypeStackNormalSize(CorInfoType cit)
{
    _ASSERTE(IsStackNormalType(cit));
    return CorInfoTypeSize(cit);
}

inline unsigned getClassSize(CORINFO_CLASS_HANDLE clsHnd)
{
    TypeHandle VMClsHnd(clsHnd);
    return VMClsHnd.GetSize();
}

// The values of this enumeration are in one-to-one correspondence with CorInfoType --
// just shifted so that they're the value stored in an interpreter type for non-value-class
// CorinfoTypes.
enum CorInfoTypeShifted
{
    CORINFO_TYPE_SHIFTED_UNDEF      = unsigned(CORINFO_TYPE_UNDEF)      << 2,    //0x0 << 2 = 0x0
    CORINFO_TYPE_SHIFTED_VOID       = unsigned(CORINFO_TYPE_VOID)       << 2,    //0x1 << 2 = 0x4
    CORINFO_TYPE_SHIFTED_BOOL       = unsigned(CORINFO_TYPE_BOOL)       << 2,    //0x2 << 2 = 0x8
    CORINFO_TYPE_SHIFTED_CHAR       = unsigned(CORINFO_TYPE_CHAR)       << 2,    //0x3 << 2 = 0xC
    CORINFO_TYPE_SHIFTED_BYTE       = unsigned(CORINFO_TYPE_BYTE)       << 2,    //0x4 << 2 = 0x10
    CORINFO_TYPE_SHIFTED_UBYTE      = unsigned(CORINFO_TYPE_UBYTE)      << 2,    //0x5 << 2 = 0x14
    CORINFO_TYPE_SHIFTED_SHORT      = unsigned(CORINFO_TYPE_SHORT)      << 2,    //0x6 << 2 = 0x18
    CORINFO_TYPE_SHIFTED_USHORT     = unsigned(CORINFO_TYPE_USHORT)     << 2,    //0x7 << 2 = 0x1C
    CORINFO_TYPE_SHIFTED_INT        = unsigned(CORINFO_TYPE_INT)        << 2,    //0x8 << 2 = 0x20
    CORINFO_TYPE_SHIFTED_UINT       = unsigned(CORINFO_TYPE_UINT)       << 2,    //0x9 << 2 = 0x24
    CORINFO_TYPE_SHIFTED_LONG       = unsigned(CORINFO_TYPE_LONG)       << 2,    //0xa << 2 = 0x28
    CORINFO_TYPE_SHIFTED_ULONG      = unsigned(CORINFO_TYPE_ULONG)      << 2,    //0xb << 2 = 0x2C
    CORINFO_TYPE_SHIFTED_NATIVEINT  = unsigned(CORINFO_TYPE_NATIVEINT)  << 2,    //0xc << 2 = 0x30
    CORINFO_TYPE_SHIFTED_NATIVEUINT = unsigned(CORINFO_TYPE_NATIVEUINT) << 2,    //0xd << 2 = 0x34
    CORINFO_TYPE_SHIFTED_FLOAT      = unsigned(CORINFO_TYPE_FLOAT)      << 2,    //0xe << 2 = 0x38
    CORINFO_TYPE_SHIFTED_DOUBLE     = unsigned(CORINFO_TYPE_DOUBLE)     << 2,    //0xf << 2 = 0x3C
    CORINFO_TYPE_SHIFTED_STRING     = unsigned(CORINFO_TYPE_STRING)     << 2,    //0x10 << 2 = 0x40
    CORINFO_TYPE_SHIFTED_PTR        = unsigned(CORINFO_TYPE_PTR)        << 2,    //0x11 << 2 = 0x44
    CORINFO_TYPE_SHIFTED_BYREF      = unsigned(CORINFO_TYPE_BYREF)      << 2,    //0x12 << 2 = 0x48
    CORINFO_TYPE_SHIFTED_VALUECLASS = unsigned(CORINFO_TYPE_VALUECLASS) << 2,    //0x13 << 2 = 0x4C
    CORINFO_TYPE_SHIFTED_CLASS      = unsigned(CORINFO_TYPE_CLASS)      << 2,    //0x14 << 2 = 0x50
    CORINFO_TYPE_SHIFTED_REFANY     = unsigned(CORINFO_TYPE_REFANY)     << 2,    //0x15 << 2 = 0x54
    CORINFO_TYPE_SHIFTED_VAR        = unsigned(CORINFO_TYPE_VAR)        << 2,    //0x16 << 2 = 0x58
};

class InterpreterType
{
    // We use this typedef, but the InterpreterType is actually encoded.  We assume that the two
    // low-order bits of a "real" CORINFO_CLASS_HANDLE are zero, then use them as follows:
    //    0x0 ==> if "ci" is a non-struct CORINFO_TYPE_* value, m_tp contents are (ci << 2).
    //    0x1, 0x3 ==> is a CORINFO_CLASS_HANDLE "sh" for a struct type, or'd with 0x1 and possibly 0x2.
    //       0x2 is added to indicate that an instance does not fit in a INT64 stack slot on the platform, and
    //         should be referenced via a level of indirection.
    //    0x2 (exactly) indicates that it is a "native struct type".
    //
    CORINFO_CLASS_HANDLE m_tp;

public:
    // Default ==> undefined.
    InterpreterType()
        : m_tp(reinterpret_cast<CORINFO_CLASS_HANDLE>((static_cast<intptr_t>(CORINFO_TYPE_UNDEF) << 2)))
    {}

    // Requires that "cit" is not CORINFO_TYPE_VALUECLASS.
    InterpreterType(CorInfoType cit)
        : m_tp(reinterpret_cast<CORINFO_CLASS_HANDLE>((static_cast<intptr_t>(cit) << 2)))
    {
        _ASSERTE(cit != CORINFO_TYPE_VALUECLASS);
    }

    // Requires that "cet" is not ELEMENT_TYPE_VALUETYPE.
    InterpreterType(CorElementType cet)
        : m_tp(reinterpret_cast<CORINFO_CLASS_HANDLE>((static_cast<intptr_t>(CEEInfo::asCorInfoType(cet)) << 2)))
    {
        _ASSERTE(cet != ELEMENT_TYPE_VALUETYPE);
    }

    InterpreterType(CEEInfo* comp, CORINFO_CLASS_HANDLE sh)
    {
        GCX_PREEMP();

        // TODO: might wish to make a different constructor, for the cases where this is possible...
        TypeHandle typHnd(sh);
        if (typHnd.IsNativeValueType())
        {
            intptr_t shAsInt = reinterpret_cast<intptr_t>(sh);
            _ASSERTE((shAsInt & 0x1) == 0); // The 0x2 bit might already be set by the VM! This is ok, because it's only set for native value types. This is a bit slimey...
            m_tp = reinterpret_cast<CORINFO_CLASS_HANDLE>(shAsInt | 0x2);
        }
        else
        {
            CorInfoType cit = comp->getTypeForPrimitiveValueClass(sh);
            if (cit != CORINFO_TYPE_UNDEF)
            {
                m_tp = reinterpret_cast<CORINFO_CLASS_HANDLE>(static_cast<intptr_t>(cit) << 2);
            }
            else
            {
                _ASSERTE((comp->getClassAttribs(sh) & CORINFO_FLG_VALUECLASS) != 0);
                intptr_t shAsInt = reinterpret_cast<intptr_t>(sh);
                _ASSERTE((shAsInt & 0x3) == 0);
                intptr_t bits = 0x1;                            // All value classes (structs) get 0x1 set.
                if (getClassSize(sh) > sizeof(INT64))
                {
                    bits |= 0x2;                                // "Large" structs get 0x2 set, also.
                }
                m_tp = reinterpret_cast<CORINFO_CLASS_HANDLE>(shAsInt | bits);
            }
        }
    }

    bool operator==(const InterpreterType& it2) const { return m_tp == it2.m_tp; }
    bool operator!=(const InterpreterType& it2) const { return m_tp != it2.m_tp; }

    CorInfoType ToCorInfoType() const
    {
        LIMITED_METHOD_CONTRACT;

        intptr_t iTypeAsInt = reinterpret_cast<intptr_t>(m_tp);
        if ((iTypeAsInt & 0x3) == 0x0)
        {
            return static_cast<CorInfoType>(iTypeAsInt >> 2);
        }
        // Is a class or struct (or refany?).
        else
        {
            return CORINFO_TYPE_VALUECLASS;
        }
    }

    CorInfoType ToCorInfoTypeNotStruct() const
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE_MSG((reinterpret_cast<intptr_t>(m_tp) & 0x3) == 0x0, "precondition: not a struct type.");

        intptr_t iTypeAsInt = reinterpret_cast<intptr_t>(m_tp);
        return static_cast<CorInfoType>(iTypeAsInt >> 2);
    }

    CorInfoTypeShifted ToCorInfoTypeShifted() const
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE_MSG((reinterpret_cast<intptr_t>(m_tp) & 0x3) == 0x0, "precondition: not a struct type.");

        return static_cast<CorInfoTypeShifted>(reinterpret_cast<size_t>(m_tp));
    }

    CORINFO_CLASS_HANDLE ToClassHandle() const
    {
        LIMITED_METHOD_CONTRACT;

        intptr_t asInt = reinterpret_cast<intptr_t>(m_tp);
        _ASSERTE((asInt & 0x3) != 0);
        return reinterpret_cast<CORINFO_CLASS_HANDLE>(asInt & (~0x3));
    }

    size_t AsRaw() const    // Just hand out the raw bits. Be careful using this! Use something else if you can!
    {
        LIMITED_METHOD_CONTRACT;

        return reinterpret_cast<size_t>(m_tp);
    }

    // Returns the stack-normalized type for "this".
    InterpreterType StackNormalize() const;

    // Returns the (byte) size of "this".  Requires "ceeInfo" for the struct case.
    __forceinline size_t Size(CEEInfo* ceeInfo) const
    {
        LIMITED_METHOD_CONTRACT;

        intptr_t asInt = reinterpret_cast<intptr_t>(m_tp);
        intptr_t asIntBits = (asInt & 0x3);
        if (asIntBits == 0)
        {
            return CorInfoTypeSize(ToCorInfoType());
        }
        else if (asIntBits == 0x2)
        {
            // Here we're breaking abstraction, and taking advantage of the fact that 0x2
            // is the low-bit encoding of "native struct type" both for InterpreterType and for
            // TypeHandle.
            TypeHandle typHnd(m_tp);
            _ASSERTE(typHnd.IsNativeValueType());
            return typHnd.AsNativeValueType()->GetNativeSize();
        }
        else
        {
            return getClassSize(ToClassHandle());
        }
    }

    __forceinline size_t SizeNotStruct() const
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE_MSG((reinterpret_cast<intptr_t>(m_tp) & 0x3) == 0, "Precondition: is not a struct type!");
        return CorInfoTypeSize(ToCorInfoTypeNotStruct());
    }

    // Requires that "it" is stack-normal; returns its (byte) size.
    size_t StackNormalSize() const
    {
        CorInfoType cit = ToCorInfoType();
        _ASSERTE(IsStackNormalType(cit)); // Precondition.
        return CorInfoTypeStackNormalSize(cit);
    }

    // Is it a struct? (But don't include "native struct type").
    bool IsStruct() const
    {
        intptr_t asInt = reinterpret_cast<intptr_t>(m_tp);
        return (asInt & 0x1) == 0x1 || (asInt == CORINFO_TYPE_SHIFTED_REFANY);
    }

    // Returns "true" iff represents a large (> INT64 size) struct.
    bool IsLargeStruct(CEEInfo* ceeInfo) const
    {
        intptr_t asInt = reinterpret_cast<intptr_t>(m_tp);
#if defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)
        if (asInt == CORINFO_TYPE_SHIFTED_REFANY)
        {
            return true;
        }
#endif
        return (asInt & 0x3) == 0x3
            || ((asInt & 0x3) == 0x2 && Size(ceeInfo) > sizeof(INT64));
    }

#ifdef _DEBUG
    bool MatchesWork(const InterpreterType it2, CEEInfo* info) const;

    bool Matches(const InterpreterType it2, CEEInfo* info) const
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        } CONTRACTL_END;

        return MatchesWork(it2, info) || it2.MatchesWork(*this, info);
    }
#endif // _DEBUG
};

#ifndef DACCESS_COMPILE
// This class does whatever "global" (applicable to all executions after the first, as opposed to caching
// within a single execution) we do.  It is parameterized over the "Key" type (which is required to be an integral
// type, to allow binary search), and the "Val" type of things cached.
template<typename Key, typename Val>
class InterpreterCache
{
public:
    InterpreterCache();

    // Returns "false" if "k" is already present, otherwise "true".  Requires that "v" == current mapping
    // if "k" is already present.
    bool AddItem(Key k, Val v);
    bool GetItem(Key k, Val& v);

private:
    struct KeyValPair
    {
        Key m_key;
        Val m_val;
    };

    // This is kept ordered by m_iloffset, to enable binary search.
    KeyValPair* m_pairs;
    unsigned short m_allocSize;
    unsigned short m_count;

    static const unsigned InitSize = 8;

    void EnsureCanInsert();

#ifdef _DEBUG
    static void AddAllocBytes(unsigned bytes);
#endif // _DEBUG
};

#ifdef _DEBUG
enum CachedItemKind
{
    CIK_Undefined,
    CIK_CallSite,
    CIK_StaticField,
    CIK_InstanceField,
    CIK_ClassHandle,
};
#endif // _DEBUG

struct StaticFieldCacheEntry
{
    void* m_srcPtr;
    UINT m_sz;
    InterpreterType m_it;

    StaticFieldCacheEntry(void* srcPtr, UINT sz, InterpreterType it) : m_srcPtr(srcPtr), m_sz(sz), m_it(it) {}

#ifdef _DEBUG
    bool operator==(const StaticFieldCacheEntry& entry) const
    {
        return m_srcPtr == entry.m_srcPtr && m_sz == entry.m_sz && m_it == entry.m_it;
    }
#endif // _DEBUG
};

// "small" part of CORINFO_SIG_INFO, sufficient for the interpreter to call the method so decribed
struct CORINFO_SIG_INFO_SMALL
{
    CORINFO_CLASS_HANDLE    retTypeClass;   // if the return type is a value class, this is its handle (enums are normalized)
    unsigned                numArgs : 16;
    CorInfoCallConv         callConv: 8;
    CorInfoType             retType : 8;

    CorInfoCallConv     getCallConv()       { return CorInfoCallConv((callConv & CORINFO_CALLCONV_MASK)); }
    bool                hasThis()           { return ((callConv & CORINFO_CALLCONV_HASTHIS) != 0); }
    bool                hasExplicitThis()   { return ((callConv & CORINFO_CALLCONV_EXPLICITTHIS) != 0); }
    bool                hasImplicitThis()   { return ((callConv & (CORINFO_CALLCONV_HASTHIS | CORINFO_CALLCONV_EXPLICITTHIS)) == CORINFO_CALLCONV_HASTHIS); }
    unsigned            totalILArgs()       { return (numArgs + (hasImplicitThis() ? 1 : 0)); }
    bool                isVarArg()          { return ((getCallConv() == CORINFO_CALLCONV_VARARG) || (getCallConv() == CORINFO_CALLCONV_NATIVEVARARG)); }
    bool                hasTypeArg()        { return ((callConv & CORINFO_CALLCONV_PARAMTYPE) != 0); }

#ifdef _DEBUG
    bool operator==(const CORINFO_SIG_INFO_SMALL& csis) const
    {
        return retTypeClass == csis.retTypeClass
            && numArgs == csis.numArgs
            && callConv == csis.callConv
            && retType == csis.retType;
    }
#endif // _DEBUG
};

struct CallSiteCacheData
{
    MethodDesc* m_pMD;

    CORINFO_SIG_INFO_SMALL             m_sigInfo;

    CallSiteCacheData(MethodDesc* pMD, const CORINFO_SIG_INFO_SMALL& sigInfo)
        : m_pMD(pMD), m_sigInfo(sigInfo)
    {}

#ifdef _DEBUG
    bool operator==(const CallSiteCacheData& cscd) const
    {
        return m_pMD == cscd.m_pMD
            && m_sigInfo == cscd.m_sigInfo;
    }
#endif // _DEBUG
};

struct CachedItem
{
#ifdef _DEBUG
    CachedItemKind m_tag;
#endif // _DEBUG
    union
    {
        // m_tag == CIK_CallSite
        CallSiteCacheData*                  m_callSiteInfo;
        // m_tag == CIK_StaticField
        StaticFieldCacheEntry*              m_staticFieldAddr;
        // m_tag == CIK_InstanceField
        FieldDesc*                          m_instanceField;
        // m_tag == CIT_ClassHandle
        CORINFO_CLASS_HANDLE                m_clsHnd;
    } m_value;

    CachedItem()
#ifdef _DEBUG
        : m_tag(CIK_Undefined)
#endif
    {}

#ifdef _DEBUG
    bool operator==(const CachedItem& ci)
    {
        if (m_tag != ci.m_tag) return false;
        switch (m_tag)
        {
        case CIK_CallSite:
            return *m_value.m_callSiteInfo == *ci.m_value.m_callSiteInfo;
        case CIK_StaticField:
            return *m_value.m_staticFieldAddr == *ci.m_value.m_staticFieldAddr;
        case CIK_InstanceField:
            return m_value.m_instanceField == ci.m_value.m_instanceField;
        case CIK_ClassHandle:
            return m_value.m_clsHnd == ci.m_value.m_clsHnd;
        default:
            return true;
        }
    }
#endif

    CachedItem(CallSiteCacheData* callSiteInfo)
#ifdef _DEBUG
        : m_tag(CIK_CallSite)
#endif
    {
        m_value.m_callSiteInfo = callSiteInfo;
    }

    CachedItem(StaticFieldCacheEntry* staticFieldAddr)
#ifdef _DEBUG
        : m_tag(CIK_StaticField)
#endif
    {
        m_value.m_staticFieldAddr = staticFieldAddr;
    }

    CachedItem(FieldDesc* instanceField)
#ifdef _DEBUG
        : m_tag(CIK_InstanceField)
#endif
    {
        m_value.m_instanceField = instanceField;
    }

    CachedItem(CORINFO_CLASS_HANDLE m_clsHnd)
#ifdef _DEBUG
        : m_tag(CIK_ClassHandle)
#endif
    {
        m_value.m_clsHnd = m_clsHnd;
    }
};


const char* eeGetMethodFullName(CEEInfo* info, CORINFO_METHOD_HANDLE hnd, const char** clsName = NULL);

// The per-InterpMethodInfo cache may map generic instantiation information to the
// cache for the current instantitation; when we find the right one the first time we copy it
// into here, so we only have to do the instantiation->cache lookup once.
typedef InterpreterCache<unsigned, CachedItem> ILOffsetToItemCache;
typedef InterpreterCache<size_t, ILOffsetToItemCache*> GenericContextToInnerCache;

#endif // DACCESS_COMPILE

// This is the information that the interpreter stub provides to the
// interpreter about the method being interpreted.
struct InterpreterMethodInfo
{
#if INTERP_PROFILE || defined(_DEBUG)
    const char*                 m_clsName;
    const char*                 m_methName;
#endif

    // Stub num for the current method under interpretation.
    int                         m_stubNum;

    // The method this info is relevant to.
    CORINFO_METHOD_HANDLE       m_method;

    // The module containing the method.
    CORINFO_MODULE_HANDLE       m_module;

    // Code pointer, size, and max stack usage.
    BYTE*                       m_ILCode;
    BYTE*                       m_ILCodeEnd;        // One byte past the last byte of IL. IL Code Size = m_ILCodeEnd - m_ILCode.

    // The CLR transforms delegate constructors, and may add up to this many
    // extra arguments.  This amount will be added to the IL's reported MaxStack to
    // get the "maxStack" value below, so we can use a uniform calling convention for
    // "DoCall".
    unsigned                    m_maxStack;

    unsigned                    m_ehClauseCount;

    // Used to implement arglist, an index into the ilArgs array where the argument pointed to is VA sig cookie.
    unsigned                    m_varArgHandleArgNum;

    // The number of arguments.
    unsigned short              m_numArgs;

    // The number of local variables.
    unsigned short              m_numLocals;

    enum Flags
    {
        // Is the first argument a "this" pointer?
        Flag_hasThisArg,
        // If "m_hasThisArg" is true, indicates whether the type of this is an object pointer
        // or a byref.
        Flag_thisArgIsObjPtr,
        // Is there a return buffer argument?
        Flag_hasRetBuffArg,
        // Is the method a var arg method
        Flag_isVarArg,
        // Is the last argument a generic type context?
        Flag_hasGenericsContextArg,
        // Does the type have generic args?
        Flag_typeHasGenericArgs,
        // Does the method have generic args?
        Flag_methHasGenericArgs,
        // Is the method a "dead simple" getter (one that just reads a field?)
        Flag_methIsDeadSimpleGetter,
        // We recognize two forms of dead simple getters, one for "opt" and one for "dbg".  If it is
        // dead simple, is it dbg or opt?
        Flag_methIsDeadSimpleGetterIsDbgForm,
        Flag_Count,
    };

    typedef UINT16 FlagGroup;

    // The bitmask for a set of InterpreterMethodInfo::Flags.
    FlagGroup                       m_flags;

    template<int Flg>
    FlagGroup GetFlagBit() {
        // This works as long as FlagGroup is "int" type.
        static_assert(sizeof(FlagGroup) * 8 >= Flag_Count, "error: bitset not large enough");
        return (1 << Flg);
    }

    // Get and set the value of a flag.
    template<int Flg>
    bool GetFlag() { return (m_flags & GetFlagBit<Flg>()) != 0; }
    template<int Flg>
    void SetFlag(bool b)
    {
        if (b) m_flags |= GetFlagBit<Flg>();
        else   m_flags &= (~GetFlagBit<Flg>());
    }

    // This structure describes a local: its type and its offset.
    struct LocalDesc
    {
        InterpreterType m_typeStackNormal;
        InterpreterType m_type;
        unsigned m_offset;
    };

    // This structure describes an argument.  Much like a LocalDesc, but
    // "m_nativeOffset" contains the offset if the argument was passed using the system's native calling convention
    // (e.g., the calling convention for a JIT -> Interpreter call) whereas "m_directOffset" describes arguments passed
    // via a direct Interpreter -> Interpreter call.
    struct ArgDesc
    {
        InterpreterType m_typeStackNormal;
        InterpreterType m_type;
        short           m_nativeOffset;
        short           m_directOffset;
    };


    // This is an array of size at least "m_numArgs", such that entry "i" describes the "i'th"
    // arg in the "m_ilArgs" array passed to the interpreter: that is, the ArgDesc contains the type, stack-normal type,
    // and offset in the "m_ilArgs" array of that argument.  In addition, has extra entries if "m_hasGenericsContextArg"
    // and/or "m_hasRetBuffArg" are true, giving the offset of those arguments -- the offsets of those arguments
    // are in that order in the array.  (The corresponding types should be NativeInt.)
    ArgDesc*                    m_argDescs;

    // This is an array of size "m_numLocals", such that entry "i" describes the "i'th"
    // local : that is, the LocalDesc contains the type, stack-normal type, and, if the type
    // is a large struct type, the offset in the local variable large-struct memory array.
    LocalDesc*                  m_localDescs;

    // A bit map, with 1 bit per local, indicating whether it contains a pinning reference.
    char*                       m_localIsPinningRefBits;

    unsigned                    m_largeStructLocalSize;
    unsigned                    LocalMemSize()
    {
        return m_largeStructLocalSize + m_numLocals * sizeof(INT64);
    }

    // I will probably need more information about the return value, but for now...
    CorInfoType                 m_returnType;

    // The number of times this method has been interpreted.
    unsigned int               m_invocations;

#if INTERP_PROFILE
    UINT64                      m_totIlInstructionsExeced;
    unsigned                    m_maxIlInstructionsExeced;

    void RecordExecInstrs(unsigned instrs)
    {
        m_totIlInstructionsExeced += instrs;
        if (instrs > m_maxIlInstructionsExeced)
        {
            m_maxIlInstructionsExeced = instrs;
        }
    }
#endif

// #ifndef DACCESS_COMPILE
    // Caching information.  Currently the only thing we cache is saved formats of MethodDescCallSites
    // at call instructions.
    // We use a "void*", because the actual type depends on the whether the method has
    // a dynamic generics context.  If so, this is a cache from the generic parameter to an
    // ILoffset->item cache; if not, it's a the ILoffset->item cache directly.
    void* m_methodCache;
// #endif // DACCESS_COMPILE

    InterpreterMethodInfo(CEEInfo* comp, CORINFO_METHOD_INFO* methInfo);

    void InitArgInfo(CEEInfo* comp, CORINFO_METHOD_INFO* methInfo, short* argOffsets_);

    void AllocPinningBitsIfNeeded();

    void SetPinningBit(unsigned locNum);
    bool GetPinningBit(unsigned locNum);

    CORINFO_CONTEXT_HANDLE GetPreciseGenericsContext(Object* thisArg, void* genericsCtxtArg);

#ifndef DACCESS_COMPILE
    // Gets the proper cache for a call to a method with the current InterpreterMethodInfo, with the given
    // "thisArg" and "genericsCtxtArg".  If "alloc" is true, will allocate the cache if necessary.
    ILOffsetToItemCache* GetCacheForCall(Object* thisArg, void* genericsCtxtArg, bool alloc = false);
#endif // DACCESS_COMPILE

    ~InterpreterMethodInfo();
};


// Expose some protected methods of CEEInfo.
class InterpreterCEEInfo: public CEEInfo
{
    CEEJitInfo m_jitInfo;
public:
    InterpreterCEEInfo(CORINFO_METHOD_HANDLE meth): CEEInfo((MethodDesc*)meth), m_jitInfo((MethodDesc*)meth, NULL, NULL, CORJIT_FLAGS::CORJIT_FLAG_SPEED_OPT) { }
};

extern INT64 F_CALL_CONV InterpretMethod(InterpreterMethodInfo* methInfo, BYTE* ilArgs, void* stubContext);
extern float F_CALL_CONV InterpretMethodFloat(InterpreterMethodInfo* methInfo, BYTE* ilArgs, void* stubContext);
extern double F_CALL_CONV InterpretMethodDouble(InterpreterMethodInfo* methInfo, BYTE* ilArgs, void* stubContext);

class Interpreter
{
    friend INT64 F_CALL_CONV InterpretMethod(InterpreterMethodInfo* methInfo, BYTE* ilArgs, void* stubContext);
    friend float F_CALL_CONV InterpretMethodFloat(InterpreterMethodInfo* methInfo, BYTE* ilArgs, void* stubContext);
    friend double F_CALL_CONV InterpretMethodDouble(InterpreterMethodInfo* methInfo, BYTE* ilArgs, void* stubContext);

    // This will be inlined into the bodies of the methods above
    static inline ARG_SLOT InterpretMethodBody(InterpreterMethodInfo* interpMethInfo, bool directCall, BYTE* ilArgs, void* stubContext);

    // The local frame size of the method being interpreted.
    static size_t GetFrameSize(InterpreterMethodInfo* interpMethInfo);

    // JIT the method if we've passed the threshold, or if "force" is true.
    static void JitMethodIfAppropriate(InterpreterMethodInfo* interpMethInfo, bool force = false);

    friend class InterpreterFrame;

public:
    // Return an interpreter stub for the given method.  That is, a stub that transforms the arguments from the native
    // calling convention to the interpreter convention, and provides the method descriptor, then calls the interpreter.
    // If "jmpCall" setting is true, then "ppInterpreterMethodInfo" must be provided and the GenerateInterpreterStub
    // will NOT generate a stub. Instead it will provide a MethodInfo that is initialized correctly after computing
    // arg descs.
    static CorJitResult GenerateInterpreterStub(CEEInfo* comp,
                                                CORINFO_METHOD_INFO* info,
                                                /*OUT*/ BYTE **nativeEntry,
                                                /*OUT*/ ULONG *nativeSizeOfCode,
                                                InterpreterMethodInfo** ppInterpMethodInfo = NULL,
                                                bool jmpCall = false);

    // If "addr" is the start address of an interpreter stub, return the corresponding MethodDesc*,
    // else "NULL".
    static class MethodDesc* InterpretationStubToMethodInfo(PCODE addr);

    // A value to indicate that the cache has not been initialized (to distinguish it from NULL --
    // we've looked and it doesn't yet have a cache.)
#define UninitExecCache reinterpret_cast<ILOffsetToItemCache*>(0x1)

    // The "frameMemory" should be a pointer to a locally-allocated memory block
    // whose size is sufficient to hold the m_localVarMemory, the operand stack, and the
    // operand type stack.
    Interpreter(InterpreterMethodInfo* methInfo_, bool directCall_, BYTE* ilArgs_, void* stubContext_, BYTE* frameMemory)
        : m_methInfo(methInfo_),
          m_interpCeeInfo(methInfo_->m_method),
          m_ILCodePtr(methInfo_->m_ILCode),
          m_directCall(directCall_),
          m_ilArgs(ilArgs_),
          m_stubContext(stubContext_),
          m_orOfPushedInterpreterTypes(0),
          m_largeStructOperandStack(NULL),
          m_largeStructOperandStackHt(0),
          m_largeStructOperandStackAllocSize(0),
          m_curStackHt(0),
          m_leaveInfoStack(),
          m_filterNextScan(0),
          m_filterHandlerOffset(0),
          m_filterExcILOffset(0),
          m_inFlightException(NULL),
          m_thisArg(NULL),
#ifdef USE_CHECKED_OBJECTREFS
          m_retBufArg(NULL),  // Initialize to NULL so we can safely declare protected.
#endif // USE_CHECKED_OBJECTREFS
          m_genericsCtxtArg(NULL),
          m_securityObject((Object*)NULL),
          m_args(NULL),
          m_argsSize(0),
          m_callThisArg(NULL),
          m_structRetValITPtr(NULL),
#ifndef DACCESS_COMPILE
          // Means "uninitialized"
          m_thisExecCache(UninitExecCache),
#endif
          m_constrainedFlag(false),
          m_readonlyFlag(false),
          m_locAllocData(NULL),
          m_preciseGenericsContext(NULL),
          m_functionPointerStack(NULL)
    {
        // We must zero the locals.
        memset(frameMemory, 0, methInfo_->LocalMemSize() + sizeof(GSCookie));

        // m_localVarMemory is below the fixed size slots, above the large struct slots.
        m_localVarMemory = frameMemory + methInfo_->m_largeStructLocalSize + sizeof(GSCookie);
        m_gsCookieAddr = (GSCookie*) (m_localVarMemory - sizeof(GSCookie));

        // Having zeroed, for large struct locals, we must initialize the fixed-size local slot to point to the
        // corresponding large-struct local slot.
        for (unsigned i = 0; i < methInfo_->m_numLocals; i++)
        {
            if (methInfo_->m_localDescs[i].m_type.IsLargeStruct(&m_interpCeeInfo))
            {
                void* structPtr = ArgSlotEndiannessFixup(reinterpret_cast<ARG_SLOT*>(FixedSizeLocalSlot(i)), sizeof(void**));
                *reinterpret_cast<void**>(structPtr) = LargeStructLocalSlot(i);
            }
        }
        frameMemory += methInfo_->LocalMemSize();
        frameMemory += sizeof(GSCookie);

#define COMBINE_OPSTACK_VAL_TYPE 0

#if COMBINE_OPSTACK_VAL_TYPE
        m_operandStackX = reinterpret_cast<OpStackValAndType*>(frameMemory);
        frameMemory += (methInfo_->m_maxStack * sizeof(OpStackValAndType));
#else
        m_operandStack = reinterpret_cast<INT64*>(frameMemory);
        frameMemory += (methInfo_->m_maxStack * sizeof(INT64));
        m_operandStackTypes = reinterpret_cast<InterpreterType*>(frameMemory);
#endif

        // If we have a "this" arg, save it in case we need it later.  (So we can
        // reliably get it even if the IL updates arg 0...)
        if (m_methInfo->GetFlag<InterpreterMethodInfo::Flag_hasThisArg>())
        {
            m_thisArg = *reinterpret_cast<Object**>(GetArgAddr(0));
        }

        unsigned extraArgInd = methInfo_->m_numArgs - 1;
        // We do these in the *reverse* of the order they appear in the array, so that we can conditionally process
        // the ones that are used.
        if (m_methInfo->GetFlag<InterpreterMethodInfo::Flag_hasGenericsContextArg>())
        {
            m_genericsCtxtArg = *reinterpret_cast<Object**>(GetArgAddr(extraArgInd));
            extraArgInd--;
        }
        if (m_methInfo->GetFlag<InterpreterMethodInfo::Flag_isVarArg>())
        {
            extraArgInd--;
        }
        if (m_methInfo->GetFlag<InterpreterMethodInfo::Flag_hasRetBuffArg>())
        {
            m_retBufArg = *reinterpret_cast<void**>(GetArgAddr(extraArgInd));
            extraArgInd--;
        }
    }

    ~Interpreter()
    {
        if (m_largeStructOperandStack != NULL)
        {
            delete[] m_largeStructOperandStack;
        }

        if (m_locAllocData != NULL)
        {
            delete m_locAllocData;
        }

        if (m_functionPointerStack != NULL)
        {
            delete[] m_functionPointerStack;
        }
    }

    // Called during EE startup to initialize locks and other generic resources.
    static void Initialize();

    // Called during stub generation to initialize compiler-specific resources.
    static void InitializeCompilerStatics(CEEInfo* info);

    // Called during EE shutdown to destroy locks and release generic resources.
    static void Terminate();

    // Returns true iff "stackPtr" can only be in younger frames than "this".  (On a downwards-
    // growing stack, it is less than the smallest local address of "this".)
    bool IsInCalleesFrames(void* stackPtr);

    MethodDesc* GetMethodDesc() { return reinterpret_cast<MethodDesc*>(m_methInfo->m_method); }

#if INTERP_ILSTUBS
    void*      GetStubContext() { return m_stubContext; }
#endif

    OBJECTREF* GetAddressOfSecurityObject() { return &m_securityObject; }

    void*      GetParamTypeArg() { return m_genericsCtxtArg; }

private:
    // Architecture-dependent helpers.
    inline static unsigned short NumberOfIntegerRegArgs();

    // Wrapper for ExecuteMethod to do a O(1) alloca when performing a jmpCall and normal calls. If doJmpCall is true, this method also resolves the call token into pResolvedToken.
    static
    ARG_SLOT ExecuteMethodWrapper(struct InterpreterMethodInfo* interpMethInfo, bool directCall, BYTE* ilArgs, void* stubContext, bool* pDoJmpCall, CORINFO_RESOLVED_TOKEN* pResolvedCallToken);

    // Execute the current method, and set *retVal to the return value, if any.
    void ExecuteMethod(ARG_SLOT* retVal, bool* pDoJmpCall, unsigned* pJumpCallToken);

    // Fetches the monitor for static methods by asking cee info. Returns the monitor
    // object.
    AwareLock* GetMonitorForStaticMethod();

    // Synchronized methods have to call monitor enter and exit at the entry and exits of the
    // method.
    void DoMonitorEnterWork();
    void DoMonitorExitWork();

    // Determines if the current exception is handled by the current method.  If so,
    // returns true and sets the interpreter state to start executing in the appropriate handler.
    bool MethodHandlesException(OBJECTREF orThrowable);

    // Assumes that "ilCode" is the first instruction in a method, whose code is of size "codeSize".
    // Returns "false" if this method has no loops; if it returns "true", it might have a loop.
    static bool MethodMayHaveLoop(BYTE* ilCode, unsigned codeSize);

    // Do anything that needs to be done on a backwards branch (e.g., GC poll).
    // Assumes that "offset" is the delta between the current code pointer and the post-branch pointer;
    // obviously, it will be negative.
    void BackwardsBranchActions(int offset);

    // Expects "interp0" to be the address of the interpreter object being scanned.
    static void GCScanRoots(promote_func* pf, ScanContext* sc, void* interp0);

    // The above calls this instance method.
    void GCScanRoots(promote_func* pf, ScanContext* sc);

    // Scan the root at "loc", whose type is "it", using "pf" and "sc".
    void GCScanRootAtLoc(Object** loc, InterpreterType it, promote_func* pf, ScanContext* sc,
                         bool pinningRef = false);

    // Scan the root at "loc", whose type is the value class "valueCls", using "pf" and "sc".
    void GCScanValueClassRootAtLoc(Object** loc, CORINFO_CLASS_HANDLE valueClsHnd, promote_func* pf, ScanContext* sc);

    // Asserts that "addr" is the start of the interpretation stub for "md".  Records this in a table,
    // to satisfy later calls to "InterpretationStubToMethodInfo."
    static void RecordInterpreterStubForMethodDesc(CORINFO_METHOD_HANDLE md, void* addr);

    struct ArgState
    {
        unsigned short numRegArgs;
        unsigned short numFPRegArgSlots;
        unsigned       fpArgsUsed;   // Bit per single-precision fp arg accounted for.
        short          callerArgStackSlots;
        short*         argOffsets;
        enum ArgRegStatus
        {
            ARS_IntReg,
            ARS_FloatReg,
            ARS_NotReg
        };
        ArgRegStatus*  argIsReg;

        ArgState(unsigned totalArgs) :
            numRegArgs(0),
            numFPRegArgSlots(0), fpArgsUsed(0),
            callerArgStackSlots(0),
            argOffsets(new short[totalArgs]),
            argIsReg(new ArgRegStatus[totalArgs])
        {
            for (unsigned i = 0; i < totalArgs; i++)
            {
                argIsReg[i] = ARS_NotReg;
                argOffsets[i] = 0;
            }
        }

#if defined(HOST_ARM)
        static const int MaxNumFPRegArgSlots = 16;
#elif defined(HOST_ARM64)
        static const int MaxNumFPRegArgSlots = 8;
#elif defined(HOST_AMD64)
#if defined(UNIX_AMD64_ABI)
        static const int MaxNumFPRegArgSlots = 8;
#else
        static const int MaxNumFPRegArgSlots = 4;
#endif
#elif defined(HOST_LOONGARCH64)
        static const int MaxNumFPRegArgSlots = 8;
#elif defined(HOST_RISCV64)
        static const int MaxNumFPRegArgSlots = 8;
#endif

        ~ArgState()
        {
            delete[] argOffsets;
            delete[] argIsReg;
        }

        void AddArg(unsigned canonIndex, short numSlots = 1, bool noReg = false, bool twoSlotAlign = false);

        // By this call, argument "canonIndex" is declared to be a floating point argument, taking the given #
        // of slots.  Important that this be called in argument order.
        void AddFPArg(unsigned canonIndex, unsigned short numSlots, bool doubleAlign);

#if defined(HOST_AMD64)
        // We have a special function for AMD64 because both integer/float registers overlap. However, all
        // callers are expected to call AddArg/AddFPArg directly.
        void AddArgAmd64(unsigned canonIndex, unsigned short numSlots, bool isFloatingType);
#endif
    };

    typedef MapSHash<void*, CORINFO_METHOD_HANDLE> AddrToMDMap;
    static AddrToMDMap* s_addrToMDMap;
    static AddrToMDMap* GetAddrToMdMap();

    // In debug, we map to a pair, containing the Thread that inserted it, so we can assert that any given thread only
    // inserts one stub for a CORINFO_METHOD_HANDLE.
    struct MethInfo
    {
        InterpreterMethodInfo* m_info;
#ifdef _DEBUG
        Thread* m_thread;
#endif // _DEBUG
    };
    typedef MapSHash<CORINFO_METHOD_HANDLE, MethInfo> MethodHandleToInterpMethInfoPtrMap;
    static MethodHandleToInterpMethInfoPtrMap* s_methodHandleToInterpMethInfoPtrMap;
    static MethodHandleToInterpMethInfoPtrMap* GetMethodHandleToInterpMethInfoPtrMap();

    static InterpreterMethodInfo* RecordInterpreterMethodInfoForMethodHandle(CORINFO_METHOD_HANDLE md, InterpreterMethodInfo* methInfo);
    static InterpreterMethodInfo* MethodHandleToInterpreterMethInfoPtr(CORINFO_METHOD_HANDLE md);

public:
    static unsigned s_interpreterStubNum;
private:
    unsigned CurOffset()
    {
        _ASSERTE(m_methInfo->m_ILCode <= m_ILCodePtr &&
                                         m_ILCodePtr < m_methInfo->m_ILCodeEnd);
        unsigned res = static_cast<unsigned>(m_ILCodePtr - m_methInfo->m_ILCode);
        return res;
    }

    // We've computed a branch target. Is the target in range? If not, throw an InvalidProgramException.
    // Otherwise, execute the branch by changing m_ILCodePtr.
    void ExecuteBranch(BYTE* ilTargetPtr)
    {
        if (m_methInfo->m_ILCode <= ilTargetPtr &&
                                    ilTargetPtr < m_methInfo->m_ILCodeEnd)
        {
            m_ILCodePtr = ilTargetPtr;
        }
        else
        {
            COMPlusThrow(kInvalidProgramException);
        }
    }

    // Private fields:
    //
    InterpreterMethodInfo* m_methInfo;
    InterpreterCEEInfo m_interpCeeInfo;

    BYTE*  m_ILCodePtr;

    bool   m_directCall;
    BYTE*  m_ilArgs;

    __forceinline InterpreterType GetArgType(unsigned argNum)
    {
        return m_methInfo->m_argDescs[argNum].m_type;
    }

    __forceinline InterpreterType GetArgTypeNormal(unsigned argNum)
    {
        return m_methInfo->m_argDescs[argNum].m_typeStackNormal;
    }

    __forceinline BYTE* GetArgAddr(unsigned argNum)
    {
        if (!m_directCall)
        {
#if defined(HOST_AMD64) && !defined(UNIX_AMD64_ABI)
            // In AMD64, a reference to the struct is passed if its size exceeds the word size.
            // Dereference the arg to get to the ref of the struct.
            if (GetArgType(argNum).IsLargeStruct(&m_interpCeeInfo))
            {
                return *reinterpret_cast<BYTE**>(&m_ilArgs[m_methInfo->m_argDescs[argNum].m_nativeOffset]);
            }
#endif
            return &m_ilArgs[m_methInfo->m_argDescs[argNum].m_nativeOffset];
        }
        else
        {
            if (GetArgType(argNum).IsLargeStruct(&m_interpCeeInfo))
            {
                return *reinterpret_cast<BYTE**>(&m_ilArgs[m_methInfo->m_argDescs[argNum].m_directOffset]);
            }
            else
            {
                return &m_ilArgs[m_methInfo->m_argDescs[argNum].m_directOffset];
            }
        }
    }

    __forceinline MethodTable* GetMethodTableFromClsHnd(CORINFO_CLASS_HANDLE hnd)
    {
        TypeHandle th(hnd);
        return th.GetMethodTable();
    }

#ifdef FEATURE_HFA
    __forceinline BYTE* GetHFARetBuffAddr(unsigned sz)
    {
        // Round up to a double boundary:
        sz = ((sz + sizeof(double) - 1) / sizeof(double)) * sizeof(double);
        // We rely on the interpreter stub to have pushed "sz" bytes on its stack frame,
        // below m_ilArgs;
        return m_ilArgs - sz;
    }
#endif // FEATURE_HFA

    void*  m_stubContext;


    // Address of the GSCookie value in the current method's frame.
    GSCookie* m_gsCookieAddr;

    BYTE* GetFrameBase()
    {
        return (m_localVarMemory - sizeof(GSCookie) - m_methInfo->m_largeStructLocalSize);
    }
    // m_localVarMemory points to the boundary between the fixed-size slots for the locals
    // (positive offsets), and the full-sized slots for large struct locals (negative offsets).
    BYTE*  m_localVarMemory;
    INT64* FixedSizeLocalSlot(unsigned locNum)
    {
        return reinterpret_cast<INT64*>(m_localVarMemory) + locNum;
    }

    BYTE* LargeStructLocalSlot(unsigned locNum)
    {
        BYTE* base = GetFrameBase();
        BYTE* addr = base + m_methInfo->m_localDescs[locNum].m_offset;
        _ASSERTE(IsInLargeStructLocalArea(addr));
        return addr;
    }

    bool IsInLargeStructLocalArea(void* addr)
    {
        void* base = GetFrameBase();
        return (base <= addr) && (addr < (static_cast<void*>(m_localVarMemory - sizeof(GSCookie))));
    }

    bool IsInLocalArea(void* addr)
    {
        void* base = GetFrameBase();
        return (base <= addr) && (addr < static_cast<void*>(reinterpret_cast<INT64*>(m_localVarMemory) + m_methInfo->m_numLocals));
    }

    // Ensures that the operand stack contains no pointers to large struct local slots (by
    // copying the values out to locations allocated on the large struct stack.
    void OpStackNormalize();

    // The defining property of this word is: if the bottom two bits are not 0x3, then the current operand stack contains no pointers
    // to large-struct slots for locals.  Operationally, we achieve this by taking "OR" of the interpreter types of local variables that have been loaded onto the
    // operand stack -- if any have been large structs, they will have 0x3 as the low order bits of their interpreter type, and this will be
    // "sticky."  We may sometimes determine that no large struct local pointers are currently on the stack, and reset this word to zero.
    size_t m_orOfPushedInterpreterTypes;

#if COMBINE_OPSTACK_VAL_TYPE
    struct OpStackValAndType
    {
        INT64 m_val;
        InterpreterType m_type;
        INT32 m_pad;
    };

    OpStackValAndType* m_operandStackX;
#else
    INT64* m_operandStack;
#endif

    template<typename T>
    __forceinline T OpStackGet(unsigned ind)
    {
        return *OpStackGetAddr<T>(ind);
    }

    template<typename T>
    __forceinline void OpStackSet(unsigned ind, T val)
    {
        *OpStackGetAddr<T>(ind) = val;
    }

#if COMBINE_OPSTACK_VAL_TYPE
    template<typename T>
    __forceinline T* OpStackGetAddr(unsigned ind)
    {
        return reinterpret_cast<T*>(ArgSlotEndiannessFixup(reinterpret_cast<ARG_SLOT*>(&m_operandStackX[ind].m_val), sizeof(T)));
    }

    __forceinline void* OpStackGetAddr(unsigned ind, size_t sz)
    {
        return ArgSlotEndiannessFixup(reinterpret_cast<ARG_SLOT*>(&m_operandStackX[ind].m_val), sz);
    }
#else
    template<typename T>
    __forceinline T* OpStackGetAddr(unsigned ind)
    {
        return reinterpret_cast<T*>(ArgSlotEndiannessFixup(reinterpret_cast<ARG_SLOT*>(&m_operandStack[ind]), sizeof(T)));
    }

    __forceinline void* OpStackGetAddr(unsigned ind, size_t sz)
    {
        return ArgSlotEndiannessFixup(reinterpret_cast<ARG_SLOT*>(&m_operandStack[ind]), sz);
    }
#endif

    __forceinline INT64 GetSmallStructValue(void* src, size_t sz)
    {
        _ASSERTE(sz <= sizeof(INT64));

        INT64 ret = 0;
        memcpy(ArgSlotEndiannessFixup(reinterpret_cast<ARG_SLOT*>(&ret), sz), src, sz);
        return ret;
    }

    BYTE*  m_largeStructOperandStack;
    size_t m_largeStructOperandStackHt;
    size_t m_largeStructOperandStackAllocSize;

    // Allocate "sz" bytes on the large struct operand stack, and return a pointer to where
    // the structure should be copied.
    void* LargeStructOperandStackPush(size_t sz);

    // Deallocate "sz" bytes from the large struct operand stack, unless the corresponding
    // operand stack value "fromAddr" is a pointer to a local variable.
    void LargeStructOperandStackPop(size_t sz, void* fromAddr);

    // Ensures that we can push a struct of size "sz" on the large struct operand stack.
    void LargeStructOperandStackEnsureCanPush(size_t sz);

#ifdef _DEBUG
    // Returns "true" iff the sum of sizes of large structures on the operand stack
    // equals "m_largeStructOperandStackHt", which should be an invariant.
    bool LargeStructStackHeightIsValid();
#endif // _DEBUG

    // Returns "true" iff the "cit" is 'considered' a valid pointer type for the
    // architecture. For ex: nativeint/byref and for amd64 longs with loose rules.
    bool IsValidPointerType(CorInfoType cit);

#if !COMBINE_OPSTACK_VAL_TYPE
    InterpreterType* m_operandStackTypes;
#endif

#if COMBINE_OPSTACK_VAL_TYPE
#if USE_MACRO_FOR_OPSTACKACCESS
#define OpStackTypeGet(ind) m_operandStackX[ind].m_type
#define OpStackTypeSet(ind, it) m_operandStackX[ind].m_type = it
#else
    __forceinline InterpreterType OpStackTypeGet(unsigned ind)
    {
        return m_operandStackX[ind].m_type;
    }

    __forceinline void OpStackTypeSet(unsigned ind, InterpreterType it)
    {
        _ASSERTE(IsStackNormalType(it.ToCorInfoType()));
        m_operandStackX[ind].m_type = it;
    }
#endif
#else
    __forceinline InterpreterType OpStackTypeGet(unsigned ind)
    {
        return m_operandStackTypes[ind];
    }

    __forceinline void OpStackTypeSet(unsigned ind, InterpreterType it)
    {
        _ASSERTE(IsStackNormalType(it.ToCorInfoType()));
        m_operandStackTypes[ind] = it;
    }
#endif
    unsigned m_curStackHt;

    // These are used in searching for finally clauses when we 'leave' a try block:

    struct LeaveInfo
    {
        unsigned m_offset;      // The offset of "leave" instructions in try blocks whose finally blocks are being executed.
        BYTE*    m_target;       // The location the 'leave' was jumping to -- where execution should resume after all finally's have been executed.
        unsigned m_nextEHIndex;      // The index in the EH table at which the search for the next finally for "lastLeaveOffset" should resume.

        LeaveInfo(unsigned offset = 0, BYTE* target = NULL) : m_offset(offset), m_target(target), m_nextEHIndex(0) {}
    };
    // This is a stack of the currently in-force "leaves."  (Multiple leave's can be being processed when a try-finally occurs
    // within a finally).
    Stack<LeaveInfo> m_leaveInfoStack;

    // Used to track the next filter to scan in case the current
    // filter doesn't handle the exception.
    unsigned m_filterNextScan;

    // Used to record the handler offset for the current filter so it can be used during endfilter.
    unsigned m_filterHandlerOffset;

    // The actual offset at which the exception occurred for a filter that might possibly handle it.
    unsigned m_filterExcILOffset;

    // This is the exception to rethrow upon exiting the last finally.
    Object* m_inFlightException; // This must be scanned by GC.

    // Storing "this" and "typeCtxt" args if necessary.
    Object* m_thisArg;   // This must be scanned by GC.
    void*   m_retBufArg; // This must be scanned by GC:
                         // if the caller is JITted, o.f = Foo(), for o.f a value type, retBuf may be ref o.f.
    void*   m_genericsCtxtArg;

    // Acquired variable for synchronized methods.
    unsigned char   m_monAcquired;

    // Holds the security object, for frames that require it.
    OBJECTREF m_securityObject;

    ARG_SLOT* m_args;
    InterpreterType* m_argTypes;
    unsigned  m_argsSize;

    void* m_callThisArg;

    // If "m_structRetValITPtr" is non-NULL, then "*m_structRetValITPtr" represents a struct type, and
    // "m_structRetValTempSpace" is a pointer to a value of that struct type, which must be scanned during GC.
    InterpreterType* m_structRetValITPtr;
    void* m_structRetValTempSpace;

#ifdef DACCESS_COMPILE
    void* m_thisExecCache;
#else  // DACCESS_COMPILE

     // The proper cache for the current method execution (or else UninitExecCache).
    ILOffsetToItemCache* m_thisExecCache;

    // Retrieve the ILoffset->Item cache for the generic instantiation (if any) of the
    // currently-executing method.  If "alloc" is true, allocate one if its not there.
    ILOffsetToItemCache* GetThisExecCache(bool alloc)
    {
        if (m_thisExecCache == UninitExecCache ||
            (m_thisExecCache == NULL && alloc))
        {
            m_thisExecCache = m_methInfo->GetCacheForCall(m_thisArg, m_genericsCtxtArg, alloc);
        }
        _ASSERTE(!alloc || m_thisExecCache != NULL);
        return m_thisExecCache;
    }

    // Cache that a call at "iloffset" has the given CallSiteCacheData "callInfo".
    void CacheCallInfo(unsigned iloffset, CallSiteCacheData* callInfo);

    // If there's a cached CORINFO_CALL_INFO for the call at the given IL offset, return it, else NULL.
    CallSiteCacheData* GetCachedCallInfo(unsigned iloffset);

    void CacheInstanceField(unsigned iloffset, FieldDesc* fld);
    FieldDesc* GetCachedInstanceField(unsigned iloffset);

    void CacheStaticField(unsigned iloffset, StaticFieldCacheEntry* pEntry);
    StaticFieldCacheEntry* GetCachedStaticField(unsigned iloffset);

    void CacheClassHandle(unsigned ilOffset, CORINFO_CLASS_HANDLE clsHnd);
    CORINFO_CLASS_HANDLE GetCachedClassHandle(unsigned iloffset);
#endif // DACCESS_COMPILE

#if INTERP_ILCYCLE_PROFILE
    // Cycles we want to delete from the current instructions cycle count; e.g.,
    // cycles spent in a callee.
    unsigned __int64 m_exemptCycles;
    unsigned __int64 m_startCycles;
    unsigned short   m_instr;

    void UpdateCycleCount();
#endif // INTERP_ILCYCLE_PROFILE

#ifdef _DEBUG

    // These collectively record all the interpreter method infos we've created.
    static InterpreterMethodInfo** s_interpMethInfos;
    static unsigned s_interpMethInfosAllocSize;
    static unsigned s_interpMethInfosCount;

    static void AddInterpMethInfo(InterpreterMethodInfo* methInfo);

    // Print any end-of-run summary information we've collected, and want
    // printed.

    // Both methods below require that "mi0" and "mi1" are actually "InterpreterMethodInfo*"s.

    // Returns -1, 0, or 1, depending on whether "mi0->m_invocations" is less than,
    // equal, or greater than "mi1->m_invocations.".
    static int _cdecl CompareMethInfosByInvocations(const void* mi0, const void* mi1);
#if INTERP_PROFILE
    // Returns 1, 0, or -1, depending on whether "mi0->m_totIlInstructionsExeced" is less than,
    // equal, or greater than "mi1->m_totIlInstructionsExeced.".  (Note that this enables a descending sort.)
    static int _cdecl CompareMethInfosByILInstrs(const void* mi0, const void* mi1);
#endif // INTERP_PROFILE
#endif // _DEBUG

    private:
    static ConfigDWORD s_PrintPostMortemFlag;

    public:
    static void PrintPostMortemData();

#if INTERP_TRACING
    private:
    // Returns a string name of the il operation at "ILCodePtr".
    static const char* ILOp(BYTE* ilCodePtr);
    static const char* ILOp1Byte(unsigned short ilInstrVal);
    static const char* ILOp2Byte(unsigned short ilInstrVal);

    // Prints a representation of the operand stack.
    void PrintOStack();

    // Prints a representation of the arguments.
    void PrintArgs();

    // Prints a representation of the locals.
    void PrintLocals();

    // Helper functions for the above:
    // Print the value at ostack position "index".
    void PrintOStackValue(unsigned index);

    // Print the value of the argument number "argNum".
    void PrintArgValue(unsigned argNum);

    // Requires that "valAddr" point to a location containing a value of type
    // "cit", and prints that value.
    void PrintValue(InterpreterType cit, BYTE* valAddr);

    public:
    static inline FILE* GetLogFile();
    private:
    static FILE*        s_InterpreterLogFile;
    static ConfigDWORD  s_DumpInterpreterStubsFlag;
    static ConfigDWORD  s_TraceInterpreterEntriesFlag;
    static ConfigDWORD  s_TraceInterpreterILFlag;
    static ConfigDWORD  s_TraceInterpreterOstackFlag;
    static ConfigDWORD  s_TraceInterpreterVerboseFlag;
    static ConfigDWORD  s_TraceInterpreterJITTransitionFlag;
    static ConfigDWORD  s_InterpreterStubMin;
    static ConfigDWORD  s_InterpreterStubMax;

    // The total number of method invocations.
    static LONG s_totalInvocations;
    // The total number of calls made by interpreted code.
    static LONG s_totalInterpCalls;
    static LONG s_totalInterpCallsToGetters;
    static LONG s_totalInterpCallsToDeadSimpleGetters;
    static LONG s_totalInterpCallsToDeadSimpleGettersShortCircuited;
    static LONG s_totalInterpCallsToSetters;
    static LONG s_totalInterpCallsToIntrinsics;
    static LONG s_totalInterpCallsToIntrinsicsUnhandled;

    enum ResolveTokenKind {
        RTK_Undefined,
        RTK_Constrained,
        RTK_NewObj,
        RTK_NewArr,
        RTK_LdToken,
        RTK_LdFtn,
        RTK_LdVirtFtn,
        RTK_SFldAddr,
        RTK_LdElem,
        RTK_Call,
        RTK_LdObj,
        RTK_StObj,
        RTK_CpObj,
        RTK_InitObj,
        RTK_IsInst,
        RTK_CastClass,
        RTK_MkRefAny,
        RTK_RefAnyVal,
        RTK_Sizeof,
        RTK_StElem,
        RTK_Box,
        RTK_Unbox,
        RTK_UnboxAny,
        RTK_LdFld,
        RTK_LdFldA,
        RTK_StFld,
        RTK_FindClass,
        RTK_CheckHandlesException,
        RTK_Count
    };
    static const char* s_tokenResolutionKindNames[RTK_Count];

    static LONG s_tokenResolutionOpportunities[RTK_Count];
    static LONG s_tokenResolutionCalls[RTK_Count];
#endif // INTERP_TRACING

#if INTERP_ILINSTR_PROFILE
    static unsigned short         s_ILInstrCategories[512];

    static int                   s_ILInstrExecs[256];
    static int                   s_ILInstrExecsByCategory[512];
#if INTERP_ILCYCLE_PROFILE
    static unsigned __int64       s_ILInstrCyclesByCategory[512];
#endif // INTERP_ILCYCLE_PROFILE

    static const unsigned         CountIlInstr2Byte = 0x22;
    static int                   s_ILInstr2ByteExecs[CountIlInstr2Byte];

#if INTERP_ILCYCLE_PROFILE
    static unsigned __int64       s_ILInstrCycles[512];
    // XXX
    static unsigned __int64              s_callCycles;
    static unsigned                      s_calls;
#endif // INTERP_ILCYCLE_PROFILE
#endif // INTERP_ILINSTR_PROFILE

    // Non-debug-only statics.
    static ConfigMethodSet s_InterpretMeths;
    static ConfigMethodSet s_InterpretMethsExclude;
    static ConfigDWORD s_InterpretMethHashMin;
    static ConfigDWORD s_InterpretMethHashMax;
    static ConfigDWORD s_InterpreterJITThreshold;
    static ConfigDWORD s_InterpreterDoLoopMethodsFlag;
    static bool        s_InterpreterDoLoopMethods;
    static ConfigDWORD s_InterpreterUseCachingFlag;
    static bool        s_InterpreterUseCaching;
    static ConfigDWORD s_InterpreterLooseRulesFlag;
    static bool        s_InterpreterLooseRules;
    static CrstExplicitInit   s_methodCacheLock;
    static CrstExplicitInit   s_interpStubToMDMapLock;

    // True iff a "constrained" prefix has preceded a call.
    bool                   m_constrainedFlag;
    // True iff a "volatile" prefixe precedes a memory reference.
    bool                   m_volatileFlag;
    // If there has been a "constrained" prefix, this is initialized
    // with the token of the constraint class.
    CORINFO_RESOLVED_TOKEN m_constrainedResolvedToken;
    // True iff a "readonly" prefix has preceded a ldelema.
    bool                   m_readonlyFlag;

    // Data structures related to localloc.
    class LocAllocData
    {
        typedef void* PVoid;

        unsigned m_locAllocSize;   // The currently allocated # elements in m_locAllocs
        unsigned m_locAllocCurIdx; // Number of elements of m_locAllocs in use; 0 <= m_locAllocCurIdx < m_locAllocSize
        void** m_locAllocs;        // Always non-null in a constructed LocAllocData.
        static const unsigned DefaultAllocs = 1;

        unsigned EnsureIdx()
        {
            if (m_locAllocCurIdx == m_locAllocSize)
            {
                unsigned newSize = m_locAllocSize * 2;
                void** newLocAllocs = new PVoid[newSize];
                for (unsigned j = 0; j < m_locAllocCurIdx; j++)
                {
                    newLocAllocs[j] = m_locAllocs[j];
                }
                m_locAllocSize = newSize;
                delete[] m_locAllocs;
                m_locAllocs = newLocAllocs;
            }
            return m_locAllocCurIdx++;  // Note that we're returning the value before post-increment.
        }

    public:
        LocAllocData() :
            m_locAllocSize(DefaultAllocs),
            m_locAllocCurIdx(0)
        {
            m_locAllocs = new PVoid[DefaultAllocs];
            memset(m_locAllocs, 0, DefaultAllocs * sizeof(void*));
        }

        void* Alloc(NativeUInt sz)
        {
            unsigned idx = EnsureIdx();
            void* res = new char[sz];
            // We only *have* to do this if initlocals is set, but no harm in always doing it.
            memset(res, 0, sz);
            m_locAllocs[idx] = res;
            return res;
        }

        ~LocAllocData()
        {
            if (m_locAllocs != NULL)
            {
                for (unsigned i = 0; i < m_locAllocCurIdx; i++)
                {
                    delete[] reinterpret_cast<char*>(m_locAllocs[i]);
                }
            }
            delete[] m_locAllocs;
        }
    };

    LocAllocData* m_locAllocData;

    LocAllocData* GetLocAllocData()
    {
        if (m_locAllocData == NULL)
        {
            m_locAllocData = new LocAllocData();
        }
        return m_locAllocData;
    }

    // Search the current method's exception table, starting at "leaveEHIndex", for the first finally clause
    // for a try block that covers "lastLeaveOffset".  If one is found, sets m_ILCodePtr to the start of that
    // finally clause, updates "leaveEHIndex" to be the next index after the found clause in the exception
    // table, and returns true.  Otherwise, if no applicable finally clause is found, returns false.
    bool SearchForCoveringFinally();

    void LdIcon(INT32 c);
    void LdLcon(INT64 c);
    void LdR4con(INT32 c);
    void LdR8con(INT64 c);

    void LdArg(int argNum);
    void LdArgA(int argNum);
    void StArg(int argNum);

    __forceinline void LdLoc(int locNum);
    void LdLocA(int locNum);
    __forceinline void StLoc(int locNum);

    // Requires that "*addr" contain a value of type "tp"; reads that value and
    // pushes it on the operand stack.
    __forceinline void LdFromMemAddr(void* addr, InterpreterType tp);

    // Requires that "addr" is the address of a local var or argument location.
    // Pops the value on the operand stack, assumed to be of the given "tp", and stores
    // in "*addr".
    __forceinline void StToLocalMemAddr(void* addr, InterpreterType tp);

    void LdNull();

    // This requires that the width of "T" is at least 4 bytes.
    template<typename T, CorInfoType cit>
    void LdInd();

    // This requires that the width of "T" is less than 4 bytes (and loads it as an INT32).
    template<typename T, bool isUnsigned>
    void LdIndShort();

    void LdIndFloat();

    // Use this for non-object-ref types, and StInd_Ref for object refs.
    template<typename T>
    void StInd();

    void StInd_Ref();

    // Load/store instance/static fields.

    // If non-NULL, we've determined the field to be loaded by other means (e.g., we've identified a
    // "dead simple" property getter).  In this case, use this FieldDesc*, otherwise, look up via token
    // or cache.
    void LdFld(FieldDesc* fld = NULL);

    void LdFldA();
    void LdSFld();
    void LdSFldA();
    void StFld();
    void StSFld();

    // Helper method used by the static field methods above.
    // Requires that the code stream be pointing to a LDSFLD, LDSFLDA, or STSFLD.
    // The "accessFlgs" variable should indicate which, by which of the CORINFO_ACCESS_GET,
    // CORINFO_ACCESS_GET, and CORINFO_ACCESS_ADDRESS bits are set.
    // Sets *pStaticFieldAddr, which must be a pointer to memory protected as a byref) to the address of the static field,
    // sets *pit to the InterpreterType of the field,
    // sets *pFldSize to the size of the field, and sets *pManagedMem to true iff the address is in managed memory (this is
    // false only if the static variable is an "RVA").  (Increments the m_ILCodePtr of 'this' by 5, the
    // assumed size of all the listed instructions.
    __forceinline void StaticFldAddr(CORINFO_ACCESS_FLAGS accessFlgs,
                                     /*out (byref)*/void** pStaticFieldAddr,
                                     /*out*/InterpreterType* pit, /*out*/UINT* pFldSize, /*out*/bool* pManagedMem);

    // We give out the address of this as the address for an "intrinsic static Zero".
    static INT64 IntrinsicStaticZero;

    // The version above does caching; this version always does the work.  Returns "true" iff the results
    // are cacheable.
    bool StaticFldAddrWork(CORINFO_ACCESS_FLAGS accessFlgs,
                           /*out (byref)*/void** pStaticFieldAddr,
                           /*out*/InterpreterType* pit, /*out*/UINT* pFldSize, /*out*/bool* pManagedMem);

    // Ensure that pMT has been initialized (including running it's .cctor).
    static void EnsureClassInit(MethodTable* pMT);

    // Load/store array elements, get length.  "T" should be the element
    // type of the array (as indicated by a LDELEM opcode with a type); "IsObjType" should
    // be true iff T is an object type, and "cit" should be the stack-normal CorInfoType
    // to push on the type stack.
    template<typename T, bool IsObjType, CorInfoType cit>
    void LdElemWithType();

    // Load the address of an array element.

    template<typename T, bool IsObjType>
    void StElemWithType();

    template<bool takeAddr>
    void LdElem();
    void StElem();

    void InitBlk();
    void CpBlk();

    void Box();
    void UnboxAny();
    void Unbox();

    // Requires that operand stack location "i" contain a byref to a value of the struct type
    // "valCls".  Boxes the referent of that byref, and substitutes the resulting object pointer
    // at opstack location "i."
    void BoxStructRefAt(unsigned ind, CORINFO_CLASS_HANDLE valCls);

    void Throw();
    void Rethrow();
    void EndFilter();

    void LdLen();

    // Perform a normal (non-constructor) call.  The "virtualCall" argument indicates whether the
    // call should be virtual.
    void DoCall(bool virtualCall);

    // Perform a call.  For normal (non-constructor) calls, all optional args should be
    // NULL (the default).  For constructors, "thisArg" should be a this pointer (that is not on the operand stack),
    // and "callInfoPtr" should be the callInfo describing the constructor.  There's a special case here: for "VAROBJSIZE" constructors
    // (which currently are defined for String), we want to explicitly pass NULL to the (pseudo) constructor.  So passing
    // the special value "0x1" as "thisArg" will cause NULL to be pushed.
    void DoCallWork(bool virtualCall, void* thisArg = NULL, CORINFO_RESOLVED_TOKEN* methTokPtr = NULL, CORINFO_CALL_INFO* callInfoPtr = NULL);

    // Do the call-indirect operation.
    void CallI();

    // Analyze the given method to see if it is a "dead simple" property getter:
    //   * if instance, ldarg.0, ldfld, ret.
    //   * if static, ldstfld ret.
    // More complicated forms in DBG.  Sets *offsetOfLd" to the offset of the ldfld or ldstfld instruction.
    static bool IsDeadSimpleGetter(CEEInfo* info, MethodDesc* pMD, size_t* offsetOfLd);
    static const unsigned ILOffsetOfLdFldInDeadSimpleInstanceGetterDbg = 2;
    static const unsigned ILOffsetOfLdFldInDeadSimpleInstanceGetterOpt = 1;
    static const unsigned ILOffsetOfLdSFldInDeadSimpleStaticGetter = 0;

    // Here we handle a few intrinsic calls directly.
    void DoStringLength();
    void DoStringGetChar();
    void DoGetTypeFromHandle();
    void DoSIMDHwAccelerated();
    void DoGetIsSupported();

    // Returns the proper generics context for use in resolving tokens ("precise" in the sense of including generic instantiation
    // information).
    CORINFO_CONTEXT_HANDLE m_preciseGenericsContext;

    CORINFO_CONTEXT_HANDLE GetPreciseGenericsContext()
    {
        if (m_preciseGenericsContext == NULL)
        {
            m_preciseGenericsContext = m_methInfo->GetPreciseGenericsContext(m_thisArg, m_genericsCtxtArg);
        }
        return m_preciseGenericsContext;
    }

    // Process the "CONSTRAINED" prefix, recording the constraint on the "this" parameter.
    void RecordConstrainedCall();

    // Emit a barrier if the m_volatile flag is set, and reset the flag.
    void BarrierIfVolatile()
    {
        if (m_volatileFlag)
        {
            MemoryBarrier(); m_volatileFlag = false;
        }
    }

    enum BinaryArithOpEnum
    {
        BA_Add, BA_Sub, BA_Mul, BA_Div, BA_Rem
    };
    template<int op>
    __forceinline void BinaryArithOp();

    // "IsIntType" must be true iff "T" is an integral type, and "cit" must correspond to
    // "T".  "TypeIsUnchanged" implies that the proper type is already on the operand type stack.
    template<int op, typename T, bool IsIntType, CorInfoType cit, bool TypeIsUnchanged>
    __forceinline void BinaryArithOpWork(T val1, T val2);

    // "op" is a BinaryArithOpEnum above; actually, must be one "BA_Add", "BA_Sub", "BA_Mul".
    template<int op, bool asUnsigned>
    void BinaryArithOvfOp();

    template<int op, typename T, CorInfoType cit, bool TypeIsUnchanged>
    void BinaryArithOvfOpWork(T val1, T val2);

    INT32 RemFunc(INT32 v1, INT32 v2) { return v1 % v2; }
    INT64 RemFunc(INT64 v1, INT64 v2) { return v1 % v2; }
    float RemFunc(float v1, float v2);
    double RemFunc(double v1, double v2);

    enum BinaryIntOpEnum
    {
        BIO_And, BIO_DivUn, BIO_Or, BIO_RemUn, BIO_Xor
    };
    template<int op>
    void BinaryIntOp();

    template<int op, typename T, CorInfoType cit, bool TypeIsUnchanged>
    void BinaryIntOpWork(T val1, T val2);

    template<int op>
    void ShiftOp();

    template<int op, typename T, typename UT>
    void ShiftOpWork(unsigned op1idx, CorInfoType cit2);

    void Neg();
    void Not();

    // "T" should be the type indicated by the opcode.
    // "TIsUnsigned" should be true if "T" is an unsigned type.
    // "TCanHoldPtr" should be true if the type can hold a pointer (true for NativeInt and Long).
    // "TIsShort" should be true if "T" is less wide than Int32.
    // "cit" should be the *stack-normal* type of the converted value; even if "TIsShort", "cit" should be CORINFO_TYPE_INT.
    template<typename T, bool TIsUnsigned, bool TCanHoldPtr, bool TIsShort, CorInfoType cit>
    void Conv();

    void ConvRUn();

    // This version is for conversion to integral types.
    template<typename T, INT64 TMin, UINT64 TMax, bool TCanHoldPtr, CorInfoType cit>
    void ConvOvf();

    // This version is for conversion to integral types.
    template<typename T, INT64 TMin, UINT64 TMax, bool TCanHoldPtr, CorInfoType cit>
    void ConvOvfUn();

    void LdObj();
    void LdObjValueClassWork(CORINFO_CLASS_HANDLE valueClsHnd, unsigned ind, void* src);
    void CpObj();
    void StObj();
    void InitObj();

    void LdStr();
    void NewObj();
    void NewArr();
    void IsInst();
    void CastClass();

    void MkRefany();
    void RefanyType();
    void RefanyVal();

    void CkFinite();

    void LdToken();
    void LdFtn();
    void LdVirtFtn();

    // The JIT/EE machinery for transforming delegate constructor calls requires the
    // CORINFO_METHOD_HANDLE of a method.  Usually, the method will be provided by a previous LDFTN/LDVIRTFTN.
    // In the JIT, we fold that previous instruction and the delegate constructor into a single tree, before morphing.
    // At this time, the loaded function is still in the form of a CORINFO_METHOD_HANDLE.  At morph time, delegate constructor is transformed,
    // looking into the argument trees to find this handle.  LDFTN's that are not removed this way are morphed to have actual native code addresses.
    // To support both of these needs, LDFTN will push the native code address of a method, as uses that actually need the value to invoke or store in
    // data structures require, but it will also ensure that this parallel stack is allocated, and set the corresponding index to hold the method handle.
    // When we call a delegate constructor, we find the method handle on this stack.
    CORINFO_METHOD_HANDLE* m_functionPointerStack;
    CORINFO_METHOD_HANDLE* GetFunctionPointerStack()
    {
        if (m_functionPointerStack == NULL)
        {
            m_functionPointerStack = new CORINFO_METHOD_HANDLE[m_methInfo->m_maxStack];
            for (unsigned i = 0; i < m_methInfo->m_maxStack; i++)
            {
                m_functionPointerStack[i] = NULL;
            }
        }
        return m_functionPointerStack;
    }

    void Sizeof();

    void LocAlloc();

#if INTERP_ILINSTR_PROFILE
    static void SetILInstrCategories();

    // This type is used in sorting il instructions in a profile.
    struct InstrExecRecord
    {
        unsigned short m_instr;
        bool           m_is2byte;
        unsigned m_execs;
#if INTERP_ILCYCLE_PROFILE
        unsigned __int64 m_cycles;
#endif // INTERP_ILCYCLE_PROFILE

        static int _cdecl Compare(const void* v0, const void* v1)
        {
            InstrExecRecord* iep0 = (InstrExecRecord*)v0;
            InstrExecRecord* iep1 = (InstrExecRecord*)v1;
#if INTERP_ILCYCLE_PROFILE
            if (iep0->m_cycles > iep1->m_cycles) return -1;
            else if (iep0->m_cycles == iep1->m_cycles) return 0;
            else return 1;
#else
            if (iep0->m_execs > iep1->m_execs) return -1;
            else if (iep0->m_execs == iep1->m_execs) return 0;
            else return 1;
#endif // INTERP_ILCYCLE_PROFILE
        }
    };
    // Prints the given array "recs", assumed to already be sorted.
    static void PrintILProfile(InstrExecRecord* recs, unsigned totInstrs
#if INTERP_ILCYCLE_PROFILE
                                 , unsigned __int64 totCycles
#endif // INTERP_ILCYCLE_PROFILE
                                 );
#endif // INTERP_ILINSTR_PROFILE

    static size_t GetTypedRefSize(CEEInfo* info);
    static CORINFO_CLASS_HANDLE GetTypedRefClsHnd(CEEInfo* info);
    static InterpreterType GetTypedRefIT(CEEInfo* info);

    OBJECTREF TypeHandleToTypeRef(TypeHandle* pth);

    CorInfoType GetTypeForPrimitiveValueClass(CORINFO_CLASS_HANDLE clsHnd);

    static bool s_initialized;
    static bool s_compilerStaticsInitialized;

    // This is the class handle for the struct type TypedRef (aka "Refany").
    static CORINFO_CLASS_HANDLE s_TypedRefClsHnd;
    // This is the InterpreterType for the struct type TypedRef (aka "Refany").
    static InterpreterType s_TypedRefIT;
    // And this is the size of that struct.
    static size_t s_TypedRefSize;

    // This returns the class corresponding to the token, of kind "tokKind", at "codePtr".  If this
    // includes any runtime lookup via a generics context parameter, does that.
    CORINFO_CLASS_HANDLE GetTypeFromToken(BYTE* codePtr, CorInfoTokenKind tokKind  InterpTracingArg(ResolveTokenKind rtk));


    // Calls m_interpCeeInfo.resolveToken
    inline void ResolveToken(CORINFO_RESOLVED_TOKEN* resTok, mdToken token, CorInfoTokenKind tokenType InterpTracingArg(ResolveTokenKind rtk));

    inline FieldDesc* FindField(unsigned metaTok InterpTracingArg(ResolveTokenKind rtk));
    inline CORINFO_CLASS_HANDLE FindClass(unsigned metaTok InterpTracingArg(ResolveTokenKind rtk));

    enum CompareOpEnum
    {
        CO_EQ, CO_GT, CO_GT_UN, CO_LT, CO_LT_UN
    };

    // It does not help making these next two inline functions (taking the
    // template arg as a "real" arg).
    template<int compOp>
    void CompareOp();

    // Requires that the m_curStackHt is at least op1Idx+2.
    // Returns the result (0 or 1) of the comparison "opStack[op1Idx] op opStack[op1Idx + 1]".
    template<int compOp>
    INT32 CompareOpRes(unsigned op1Idx);

    // Making this inline, by making its arguments real arguments,
    // and using __forceinline didn't result in material difference.
    template<bool val, int targetLen>
    void BrOnValue();

    // A worker function for BrOnValue.  Assumes that "shouldBranch" indicates whether
    // a branch should be taken, and that "targetLen" is the length of the branch offset (1 or 4).
    // Updates "m_ILCodePtr" to the branch target if "shouldBranch" is true, or else
    // he next instruction (+ 1 + targetLength).
    __forceinline void BrOnValueTakeBranch(bool shouldBranch, int targetLen);

    template<int compOp, bool reverse, int targetLen>
    void BrOnComparison();

    inline static
    INT8 getI1(const BYTE * ptr)
    { return *(INT8*)ptr; }

    inline static
    UINT16 getU2LittleEndian(const BYTE * ptr)
    { return VAL16(*(UNALIGNED UINT16*)ptr); }

    inline static
    UINT32 getU4LittleEndian(const BYTE * ptr)
    { return VAL32(*(UNALIGNED UINT32*)ptr); }

    inline static
    INT32 getI4LittleEndian(const BYTE * ptr)
    { return VAL32(*(UNALIGNED INT32*)ptr); }

    inline static
    INT64 getI8LittleEndian(const BYTE * ptr)
    { return VAL64(*(UNALIGNED INT64*)ptr); }

    void VerificationError(const char* msg);

    void ThrowDivideByZero();
    void ThrowSysArithException();
    void ThrowNullPointerException();
    void ThrowOverflowException();
    void ThrowArrayBoundsException();
    void ThrowInvalidCastException();
    void ThrowStackOverflow();
    void ThrowOnInvalidPointer(void* ptr);

#ifdef _DEBUG
    bool TOSIsPtr();
#endif

#if INTERP_TRACING
    // Code copied from eeinterface.cpp in "compiler".  Should be common...
    const char* eeGetMethodFullName(CORINFO_METHOD_HANDLE hnd);
#endif // INTERP_TRACING
};

#if defined(HOST_X86)
inline
unsigned short Interpreter::NumberOfIntegerRegArgs() { return 2; }
#elif  defined(HOST_AMD64)
unsigned short Interpreter::NumberOfIntegerRegArgs()
{
#if defined(UNIX_AMD64_ABI)
    return 6;
#else
    return 4;
#endif
}
#elif defined(HOST_ARM)
unsigned short Interpreter::NumberOfIntegerRegArgs() { return 4; }
#elif defined(HOST_ARM64)
unsigned short Interpreter::NumberOfIntegerRegArgs() { return 8; }
#elif defined(HOST_LOONGARCH64)
unsigned short Interpreter::NumberOfIntegerRegArgs() { return 8; }
#elif defined(HOST_RISCV64)
unsigned short Interpreter::NumberOfIntegerRegArgs() { return 8; }
#else
#error Unsupported architecture.
#endif

#endif  // INTERPRETER_H_DEFINED
