// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          _typeInfo                                         XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************
 This header file is named _typeInfo.h to be distinguished from typeinfo.h
 in the NT SDK
******************************************************************************/

/*****************************************************************************/
#ifndef _TYPEINFO_H_
#define _TYPEINFO_H_
/*****************************************************************************/

enum ti_types
{
#define DEF_TI(ti, nm) ti,
#include "titypes.h"
#undef DEF_TI
    TI_ONLY_ENUM = TI_METHOD, // Enum values with greater value are completely described by the enumeration.
};

#if defined(TARGET_64BIT)
#define TI_I_IMPL TI_LONG
#else
#define TI_I_IMPL TI_INT
#endif

#ifdef DEBUG
#if VERBOSE_VERIFY
#define TI_DUMP_PADDING "                                          "
#ifdef _MSC_VER
namespace
{
#endif // _MSC_VER
const char* g_ti_type_names_map[] = {
#define DEF_TI(ti, nm) nm,
#include "titypes.h"
#undef DEF_TI
};
#ifdef _MSC_VER
}
#endif // _MSC_VER
#endif // VERBOSE_VERIFY
#endif // DEBUG

#ifdef _MSC_VER
namespace
{
#endif //  _MSC_VER
const ti_types g_jit_types_map[] = {
#define DEF_TP(tn, nm, jitType, verType, sz, sze, asze, st, al, tf, howUsed) verType,
#include "typelist.h"
#undef DEF_TP
};
#ifdef _MSC_VER
}
#endif // _MSC_VER

#ifdef DEBUG
#if VERBOSE_VERIFY
inline const char* tiType2Str(ti_types type)
{
    return g_ti_type_names_map[type];
}
#endif // VERBOSE_VERIFY
#endif // DEBUG

// typeInfo does not care about distinction between signed/unsigned
// This routine converts all unsigned types to signed ones
inline ti_types varType2tiType(var_types type)
{
    assert(g_jit_types_map[TYP_BYTE] == TI_BYTE);
    assert(g_jit_types_map[TYP_INT] == TI_INT);
    assert(g_jit_types_map[TYP_UINT] == TI_INT);
    assert(g_jit_types_map[TYP_FLOAT] == TI_FLOAT);
    assert(g_jit_types_map[TYP_BYREF] == TI_ERROR);
    assert(g_jit_types_map[type] != TI_ERROR);
    return g_jit_types_map[type];
}

#ifdef _MSC_VER
namespace
{
#endif // _MSC_VER
const ti_types g_ti_types_map[CORINFO_TYPE_COUNT] = {
    // see the definition of enum CorInfoType in file inc/corinfo.h
    TI_ERROR,  // CORINFO_TYPE_UNDEF           = 0x0,
    TI_ERROR,  // CORINFO_TYPE_VOID            = 0x1,
    TI_BYTE,   // CORINFO_TYPE_BOOL            = 0x2,
    TI_SHORT,  // CORINFO_TYPE_CHAR            = 0x3,
    TI_BYTE,   // CORINFO_TYPE_BYTE            = 0x4,
    TI_BYTE,   // CORINFO_TYPE_UBYTE           = 0x5,
    TI_SHORT,  // CORINFO_TYPE_SHORT           = 0x6,
    TI_SHORT,  // CORINFO_TYPE_USHORT          = 0x7,
    TI_INT,    // CORINFO_TYPE_INT             = 0x8,
    TI_INT,    // CORINFO_TYPE_UINT            = 0x9,
    TI_LONG,   // CORINFO_TYPE_LONG            = 0xa,
    TI_LONG,   // CORINFO_TYPE_ULONG           = 0xb,
    TI_I_IMPL, // CORINFO_TYPE_NATIVEINT       = 0xc,
    TI_I_IMPL, // CORINFO_TYPE_NATIVEUINT      = 0xd,
    TI_FLOAT,  // CORINFO_TYPE_FLOAT           = 0xe,
    TI_DOUBLE, // CORINFO_TYPE_DOUBLE          = 0xf,
    TI_REF,    // CORINFO_TYPE_STRING          = 0x10,
    TI_ERROR,  // CORINFO_TYPE_PTR             = 0x11,
    TI_ERROR,  // CORINFO_TYPE_BYREF           = 0x12,
    TI_STRUCT, // CORINFO_TYPE_VALUECLASS      = 0x13,
    TI_REF,    // CORINFO_TYPE_CLASS           = 0x14,
    TI_STRUCT, // CORINFO_TYPE_REFANY          = 0x15,
    TI_REF,    // CORINFO_TYPE_VAR             = 0x16,
};
#ifdef _MSC_VER
}
#endif // _MSC_VER

// Convert the type returned from the VM to a ti_type.

inline ti_types JITtype2tiType(CorInfoType type)
{
    // spot check to make certain enumerations have not changed

    assert(g_ti_types_map[CORINFO_TYPE_CLASS] == TI_REF);
    assert(g_ti_types_map[CORINFO_TYPE_BYREF] == TI_ERROR);
    assert(g_ti_types_map[CORINFO_TYPE_DOUBLE] == TI_DOUBLE);
    assert(g_ti_types_map[CORINFO_TYPE_VALUECLASS] == TI_STRUCT);
    assert(g_ti_types_map[CORINFO_TYPE_STRING] == TI_REF);

    type = CorInfoType(type & CORINFO_TYPE_MASK); // strip off modifiers

    assert(type < CORINFO_TYPE_COUNT);

    assert(g_ti_types_map[type] != TI_ERROR || type == CORINFO_TYPE_VOID);
    return g_ti_types_map[type];
};

/*****************************************************************************
* Captures information about a method pointer
*
* m_token is the CORINFO_RESOLVED_TOKEN from the IL, potentially with a more
*         precise method handle from getCallInfo
* m_tokenConstraint is the constraint if this was a constrained ldftn.
*
*/
class methodPointerInfo
{
public:
    CORINFO_RESOLVED_TOKEN m_token;
    mdToken                m_tokenConstraint;
};

/*****************************************************************************
 * Declares the typeInfo class, which represents the type of an entity on the
 * stack, in a local variable or an argument.
 *
 * Flags: LLLLLLLLLLLLLLLLffffffffffTTTTTT
 *
 * L = local var # or instance field #
 * x = unused
 * f = flags
 * T = type
 *
 * The lower bits are used to store the type component, and may be one of:
 *
 * TI_* (primitive)   - see tyelist.h for enumeration (BYTE, SHORT, INT..)
 * TI_REF             - OBJREF / ARRAY use m_cls for the type
 *                       (including arrays and null objref)
 * TI_STRUCT          - VALUE type, use m_cls for the actual type
 *
 * NOTE carefully that BYREF info is not stored here.  You will never see a
 * TI_BYREF in this component.  For example, the type component
 * of a "byref TI_INT" is TI_FLAG_BYREF | TI_INT.
 *
 * NOTE carefully that Generic Type Variable info is
 * only stored here in part.  Values of type "T" (e.g "!0" in ILASM syntax),
 * i.e. some generic variable type, appear only when verifying generic
 * code.  They come in two flavours: unboxed and boxed.  Unboxed
 * is the norm, e.g. a local, field or argument of type T.  Boxed
 * values arise from an IL instruction such as "box !0".
 * The EE provides type handles for each different type
 * variable and the EE's "canCast" operation decides casting
 * for boxed type variable. Thus:
 *
 *    (TI_REF, <type-variable-type-handle>) == boxed type variable
 *
 *    (TI_REF, <type-variable-type-handle>)
 *          + TI_FLAG_GENERIC_TYPE_VAR      == unboxed type variable
 *
 * Using TI_REF for these may seem odd but using TI_STRUCT means the
 * code-generation parts of the importer get confused when they
 * can't work out the size, GC-ness etc. of the "struct".  So using TI_REF
 * just tricks these backend parts into generating pseudo-trees for
 * the generic code we're verifying.  These trees then get thrown away
 * anyway as we do verification of generic code in import-only mode.
 *
 */

#define TI_FLAG_DATA_BITS 6
#define TI_FLAG_DATA_MASK ((1 << TI_FLAG_DATA_BITS) - 1)

// Flag indicating this item is uninitialized
// Note that if UNINIT and BYREF are both set,
// it means byref (uninit x) - i.e. we are pointing to an uninit <something>

#define TI_FLAG_UNINIT_OBJREF 0x00000040

// Flag indicating this item is a byref <something>

#define TI_FLAG_BYREF 0x00000080

// This item is a byref generated using the readonly. prefix
// to a ldelema or Address function on an array type.  The
// runtime type check is ignored in these cases, but the
// resulting byref can only be used in order to perform a
// constraint call.

#define TI_FLAG_BYREF_READONLY 0x00000100

// This item is the MSIL 'I' type which is pointer-sized
// (different size depending on platform) but which on ALL platforms
// is implicitly convertible with a 32-bit int but not with a 64-bit one.

// Note:  this flag is currently used only in 64-bit systems to annotate
// native int types.  In 32 bits, since you can transparently coalesce int32
// and native-int and both are the same size, JIT32 had no need to model
// native-ints as a separate entity.  For 64-bit though, since they have
// different size, it's important to discern between a long and a native int
// since conversions between them are not verifiable.
#define TI_FLAG_NATIVE_INT 0x00000200

// This item contains the 'this' pointer (used for tracking)

#define TI_FLAG_THIS_PTR 0x00001000

// This item is a byref to something which has a permanent home
// (e.g. a static field, or instance field of an object in GC heap, as
// opposed to the stack or a local variable).  TI_FLAG_BYREF must also be
// set. This information is useful for tail calls and return byrefs.
//
// Instructions that generate a permanent home byref:
//
//  ldelema
//  ldflda of a ref object or another permanent home byref
//  array element address Get() helper
//  call or calli to a method that returns a byref and is verifiable or SkipVerify
//  dup
//  unbox

#define TI_FLAG_BYREF_PERMANENT_HOME 0x00002000

// This is for use when verifying generic code.
// This indicates that the type handle is really an unboxed
// generic type variable (e.g. the result of loading an argument
// of type T in a class List<T>).  Without this flag
// the same type handle indicates a boxed generic value,
// e.g. the result of a "box T" instruction.
#define TI_FLAG_GENERIC_TYPE_VAR 0x00004000

// Number of bits local var # is shifted

#define TI_FLAG_LOCAL_VAR_SHIFT 16
#define TI_FLAG_LOCAL_VAR_MASK 0xFFFF0000

// Field info uses the same space as the local info

#define TI_FLAG_FIELD_SHIFT TI_FLAG_LOCAL_VAR_SHIFT
#define TI_FLAG_FIELD_MASK TI_FLAG_LOCAL_VAR_MASK

#define TI_ALL_BYREF_FLAGS (TI_FLAG_BYREF | TI_FLAG_BYREF_READONLY | TI_FLAG_BYREF_PERMANENT_HOME)

/*****************************************************************************
 * A typeInfo can be one of several types:
 * - A primitive type (I4,I8,R4,R8,I)
 * - A type (ref, array, value type) (m_cls describes the type)
 * - An array (m_cls describes the array type)
 * - A byref (byref flag set, otherwise the same as the above),
 * - A Function Pointer (m_methodPointerInfo)
 * - A byref local variable (byref and byref local flags set), can be
 *   uninitialized
 *
 * The reason that there can be 2 types of byrefs (general byrefs, and byref
 * locals) is that byref locals initially point to uninitialized items.
 * Therefore these byrefs must be tracked specially.
 */

class typeInfo
{

private:
    union {
        struct
        {
            ti_types type : TI_FLAG_DATA_BITS;
            unsigned uninitobj : 1;        // used
            unsigned byref : 1;            // used
            unsigned byref_readonly : 1;   // used
            unsigned nativeInt : 1;        // used
            unsigned : 1;                  // unused
            unsigned : 1;                  // unused
            unsigned thisPtr : 1;          // used
            unsigned thisPermHome : 1;     // used
            unsigned generic_type_var : 1; // used
        } m_bits;

        DWORD m_flags;
    };

    union {
        CORINFO_CLASS_HANDLE m_cls;
        // Valid only for type TI_METHOD
        methodPointerInfo* m_methodPointerInfo;
    };

    template <typename T>
    static bool isInvalidHandle(const T handle)
    {
        static_assert(std::is_same<T, CORINFO_CLASS_HANDLE>::value || std::is_same<T, CORINFO_METHOD_HANDLE>::value,
                      "");
#ifdef HOST_64BIT
        return handle == reinterpret_cast<T>(0xcccccccccccccccc);
#else
        return handle == reinterpret_cast<T>(0xcccccccc);
#endif
    }

public:
    typeInfo() : m_flags(TI_ERROR)
    {
        m_cls = NO_CLASS_HANDLE;
    }

    typeInfo(ti_types tiType)
    {
        assert((tiType >= TI_BYTE) && (tiType <= TI_NULL));

        m_flags = (DWORD)tiType;
        m_cls   = NO_CLASS_HANDLE;
    }

    typeInfo(var_types varType)
    {
        m_flags = (DWORD)varType2tiType(varType);
        m_cls   = NO_CLASS_HANDLE;
    }

    static typeInfo nativeInt()
    {
        typeInfo result = typeInfo(TI_I_IMPL);
#ifdef TARGET_64BIT
        result.m_flags |= TI_FLAG_NATIVE_INT;
#endif
        return result;
    }

    typeInfo(ti_types tiType, CORINFO_CLASS_HANDLE cls, bool typeVar = false)
    {
        assert(tiType == TI_STRUCT || tiType == TI_REF);
        assert(cls != nullptr && !isInvalidHandle(cls));
        m_flags = tiType;
        if (typeVar)
        {
            m_flags |= TI_FLAG_GENERIC_TYPE_VAR;
        }
        m_cls = cls;
    }

    typeInfo(methodPointerInfo* methodPointerInfo)
    {
        assert(methodPointerInfo != nullptr);
        assert(methodPointerInfo->m_token.hMethod != nullptr);
        assert(!isInvalidHandle(methodPointerInfo->m_token.hMethod));
        m_flags             = TI_METHOD;
        m_methodPointerInfo = methodPointerInfo;
    }

#ifdef DEBUG
#if VERBOSE_VERIFY
    void Dump() const;
#endif // VERBOSE_VERIFY
#endif // DEBUG

public:
    // Note that we specifically ignore the permanent byref here. The rationale is that
    // the type system doesn't know about this (it's jit only), ie, signatures don't specify if
    // a byref is safe, so they are fully equivalent for the jit, except for the RET instruction,
    // instructions that load safe byrefs and the stack merging logic, which need to know about
    // the bit
    static bool AreEquivalent(const typeInfo& li, const typeInfo& ti)
    {
        DWORD allFlags = TI_FLAG_DATA_MASK | TI_FLAG_BYREF | TI_FLAG_BYREF_READONLY | TI_FLAG_GENERIC_TYPE_VAR |
                         TI_FLAG_UNINIT_OBJREF;
#ifdef TARGET_64BIT
        allFlags |= TI_FLAG_NATIVE_INT;
#endif // TARGET_64BIT

        if ((li.m_flags & allFlags) != (ti.m_flags & allFlags))
        {
            return false;
        }

        unsigned type = li.m_flags & TI_FLAG_DATA_MASK;
        assert(TI_ERROR <
               TI_ONLY_ENUM); // TI_ERROR looks like it needs more than enum.  This optimises the success case a bit
        if (type > TI_ONLY_ENUM)
        {
            return true;
        }
        if (type == TI_ERROR)
        {
            return false; // TI_ERROR != TI_ERROR
        }
        assert(li.m_cls != NO_CLASS_HANDLE && ti.m_cls != NO_CLASS_HANDLE);
        return li.m_cls == ti.m_cls;
    }

#ifdef DEBUG
    // On 64-bit systems, nodes whose "proper" type is "native int" get labeled TYP_LONG.
    // In the verification type system, we always transform "native int" to "TI_LONG" with the
    // native int flag set.
    // Ideally, we would keep track of which nodes labeled "TYP_LONG" are really "native int", but
    // attempts to do that have proved too difficult.  So in situations where we try to compare the
    // verification type system and the node type system, we use this method, which allows the specific
    // mismatch where "verTi" is TI_LONG with the native int flag and "nodeTi" is TI_LONG without the
    // native int flag set.
    static bool AreEquivalentModuloNativeInt(const typeInfo& verTi, const typeInfo& nodeTi)
    {
        if (AreEquivalent(verTi, nodeTi))
        {
            return true;
        }
#ifdef TARGET_64BIT
        return (nodeTi.IsType(TI_I_IMPL) && tiCompatibleWith(nullptr, verTi, typeInfo::nativeInt(), true)) ||
               (verTi.IsType(TI_I_IMPL) && tiCompatibleWith(nullptr, typeInfo::nativeInt(), nodeTi, true));
#else  // TARGET_64BIT
        return false;
#endif // !TARGET_64BIT
    }
#endif // DEBUG

    static bool tiMergeToCommonParent(COMP_HANDLE CompHnd, typeInfo* pDest, const typeInfo* pSrc, bool* changed);
    static bool tiCompatibleWith(COMP_HANDLE     CompHnd,
                                 const typeInfo& child,
                                 const typeInfo& parent,
                                 bool            normalisedForStack);

    static bool tiMergeCompatibleWith(COMP_HANDLE     CompHnd,
                                      const typeInfo& child,
                                      const typeInfo& parent,
                                      bool            normalisedForStack);

    /////////////////////////////////////////////////////////////////////////
    // Operations
    /////////////////////////////////////////////////////////////////////////

    void SetIsThisPtr()
    {
        m_flags |= TI_FLAG_THIS_PTR;
        assert(m_bits.thisPtr);
    }

    void ClearThisPtr()
    {
        m_flags &= ~(TI_FLAG_THIS_PTR);
    }

    void SetIsPermanentHomeByRef()
    {
        assert(IsByRef());
        m_flags |= TI_FLAG_BYREF_PERMANENT_HOME;
    }

    void SetIsReadonlyByRef()
    {
        assert(IsByRef());
        m_flags |= TI_FLAG_BYREF_READONLY;
    }

    // Set that this item is uninitialized.
    void SetUninitialisedObjRef()
    {
        assert((IsObjRef() && IsThisPtr()));
        // For now, this is used only  to track uninit this ptrs in ctors

        m_flags |= TI_FLAG_UNINIT_OBJREF;
        assert(m_bits.uninitobj);
    }

    // Set that this item is initialised.
    void SetInitialisedObjRef()
    {
        assert((IsObjRef() && IsThisPtr()));
        // For now, this is used only  to track uninit this ptrs in ctors

        m_flags &= ~TI_FLAG_UNINIT_OBJREF;
    }

    typeInfo& DereferenceByRef()
    {
        if (!IsByRef())
        {
            m_flags = TI_ERROR;
            INDEBUG(m_cls = NO_CLASS_HANDLE);
        }
        m_flags &= ~(TI_FLAG_THIS_PTR | TI_ALL_BYREF_FLAGS);
        return *this;
    }

    typeInfo& MakeByRef()
    {
        assert(!IsByRef());
        m_flags &= ~(TI_FLAG_THIS_PTR);
        m_flags |= TI_FLAG_BYREF;
        return *this;
    }

    // I1,I2 --> I4
    // FLOAT --> DOUBLE
    // objref, arrays, byrefs, value classes are unchanged
    //
    typeInfo& NormaliseForStack()
    {
        switch (GetType())
        {
            case TI_BYTE:
            case TI_SHORT:
                m_flags = TI_INT;
                break;

            case TI_FLOAT:
                m_flags = TI_DOUBLE;
                break;
            default:
                break;
        }
        return (*this);
    }

    /////////////////////////////////////////////////////////////////////////
    // Getters
    /////////////////////////////////////////////////////////////////////////

    CORINFO_CLASS_HANDLE GetClassHandle() const
    {
        return m_cls;
    }

    CORINFO_CLASS_HANDLE GetClassHandleForValueClass() const
    {
        assert(IsType(TI_STRUCT));
        assert(m_cls != NO_CLASS_HANDLE);
        return m_cls;
    }

    CORINFO_CLASS_HANDLE GetClassHandleForObjRef() const
    {
        assert(IsType(TI_REF));
        assert(m_cls != NO_CLASS_HANDLE);
        return m_cls;
    }

    CORINFO_METHOD_HANDLE GetMethod() const
    {
        assert(GetType() == TI_METHOD);
        return m_methodPointerInfo->m_token.hMethod;
    }

    methodPointerInfo* GetMethodPointerInfo() const
    {
        assert(GetType() == TI_METHOD);
        return m_methodPointerInfo;
    }

    // Get this item's type
    // If primitive, returns the primitive type (TI_*)
    // If not primitive, returns:
    //  - TI_ERROR if a byref anything
    //  - TI_REF if a class or array or null or a generic type variable
    //  - TI_STRUCT if a value class
    ti_types GetType() const
    {
        if (m_flags & TI_FLAG_BYREF)
        {
            return TI_ERROR;
        }

        // objref/array/null (objref), value class, ptr, primitive
        return (ti_types)(m_flags & TI_FLAG_DATA_MASK);
    }

    bool IsType(ti_types type) const
    {
        assert(type != TI_ERROR);
        return (m_flags & (TI_FLAG_DATA_MASK | TI_FLAG_BYREF | TI_FLAG_BYREF_READONLY | TI_FLAG_BYREF_PERMANENT_HOME |
                           TI_FLAG_GENERIC_TYPE_VAR)) == DWORD(type);
    }

    // Returns whether this is an objref
    bool IsObjRef() const
    {
        return IsType(TI_REF) || IsType(TI_NULL);
    }

    // Returns whether this is a by-ref
    bool IsByRef() const
    {
        return (m_flags & TI_FLAG_BYREF);
    }

    // Returns whether this is the this pointer
    bool IsThisPtr() const
    {
        return (m_flags & TI_FLAG_THIS_PTR);
    }

    bool IsUnboxedGenericTypeVar() const
    {
        return !IsByRef() && (m_flags & TI_FLAG_GENERIC_TYPE_VAR);
    }

    bool IsReadonlyByRef() const
    {
        return IsByRef() && (m_flags & TI_FLAG_BYREF_READONLY);
    }

    bool IsPermanentHomeByRef() const
    {
        return IsByRef() && (m_flags & TI_FLAG_BYREF_PERMANENT_HOME);
    }

    // Returns whether this is a method desc
    bool IsMethod() const
    {
        return GetType() == TI_METHOD;
    }

    bool IsStruct() const
    {
        return IsType(TI_STRUCT);
    }

    // A byref value class is NOT a value class
    bool IsValueClass() const
    {
        return (IsStruct() || IsPrimitiveType());
    }

    // Does not return true for primitives. Will return true for value types that behave
    // as primitives
    bool IsValueClassWithClsHnd() const
    {
        if ((GetType() == TI_STRUCT) ||
            (m_cls && GetType() != TI_REF && GetType() != TI_METHOD &&
             GetType() != TI_ERROR)) // necessary because if byref bit is set, we return TI_ERROR)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // Returns whether this is an integer or real number
    // NOTE: Use NormaliseToPrimitiveType() if you think you may have a
    // System.Int32 etc., because those types are not considered number
    // types by this function.
    bool IsNumberType() const
    {
        ti_types Type = GetType();

        // I1, I2, Boolean, character etc. cannot exist plainly -
        // everything is at least an I4

        return (Type == TI_INT || Type == TI_LONG || Type == TI_DOUBLE);
    }

    // Returns whether this is an integer
    // NOTE: Use NormaliseToPrimitiveType() if you think you may have a
    // System.Int32 etc., because those types are not considered number
    // types by this function.
    bool IsIntegerType() const
    {
        ti_types Type = GetType();

        // I1, I2, Boolean, character etc. cannot exist plainly -
        // everything is at least an I4

        return (Type == TI_INT || Type == TI_LONG);
    }

    // Returns true whether this is an integer or a native int.
    bool IsIntOrNativeIntType() const
    {
#ifdef TARGET_64BIT
        return (GetType() == TI_INT) || AreEquivalent(*this, nativeInt());
#else
        return IsType(TI_INT);
#endif
    }

    bool IsNativeIntType() const
    {
        return AreEquivalent(*this, nativeInt());
    }

    // Returns whether this is a primitive type (not a byref, objref,
    // array, null, value class, invalid value)
    // May Need to normalise first (m/r/I4 --> I4)
    bool IsPrimitiveType() const
    {
        DWORD Type = GetType();

        // boolean, char, u1,u2 never appear on the operand stack
        return (Type == TI_BYTE || Type == TI_SHORT || Type == TI_INT || Type == TI_LONG || Type == TI_FLOAT ||
                Type == TI_DOUBLE);
    }

    // Returns whether this is the null objref
    bool IsNullObjRef() const
    {
        return (IsType(TI_NULL));
    }

    // must be for a local which is an object type (i.e. has a slot >= 0)
    // for primitive locals, use the liveness bitmap instead
    // Note that this works if the error is 'Byref'
    bool IsDead() const
    {
        return (m_flags & (TI_FLAG_DATA_MASK)) == TI_ERROR;
    }

    bool IsUninitialisedObjRef() const
    {
        return (m_flags & TI_FLAG_UNINIT_OBJREF);
    }

private:
    // used to make functions that return typeinfo efficient.
    typeInfo(DWORD flags, CORINFO_CLASS_HANDLE cls)
    {
        m_cls   = cls;
        m_flags = flags;
    }

    friend typeInfo ByRef(const typeInfo& ti);
    friend typeInfo DereferenceByRef(const typeInfo& ti);
    friend typeInfo NormaliseForStack(const typeInfo& ti);
};

inline typeInfo NormaliseForStack(const typeInfo& ti)
{
    return typeInfo(ti).NormaliseForStack();
}

// given ti make a byref to that type.
inline typeInfo ByRef(const typeInfo& ti)
{
    return typeInfo(ti).MakeByRef();
}

// given ti which is a byref, return the type it points at
inline typeInfo DereferenceByRef(const typeInfo& ti)
{
    return typeInfo(ti).DereferenceByRef();
}
/*****************************************************************************/
#endif // _TYPEINFO_H_
/*****************************************************************************/
