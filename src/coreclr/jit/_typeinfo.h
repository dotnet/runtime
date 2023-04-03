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

#ifdef _MSC_VER
namespace
{
#endif //  _MSC_VER
const ti_types g_jit_types_map[] = {
#define DEF_TP(tn, nm, jitType, verType, sz, sze, asze, st, al, regTyp, regFld, tf) verType,
#include "typelist.h"
#undef DEF_TP
};
#ifdef _MSC_VER
}
#endif // _MSC_VER

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
 * L = unused
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
 */

#define TI_FLAG_DATA_BITS 6
#define TI_FLAG_DATA_MASK ((1 << TI_FLAG_DATA_BITS) - 1)

// Flag indicating this item is a byref <something>
#define TI_FLAG_BYREF 0x00000080
#define TI_ALL_BYREF_FLAGS (TI_FLAG_BYREF)

/*****************************************************************************
 * A typeInfo can be one of several types:
 * - A primitive type (I4,I8,R4,R8,I)
 * - A type (ref, array, value type) (m_cls describes the type)
 * - An array (m_cls describes the array type)
 * - A byref (byref flag set, otherwise the same as the above),
 * - A Function Pointer (m_methodPointerInfo)
 */

class typeInfo
{

private:
    union {
        struct
        {
            ti_types type : TI_FLAG_DATA_BITS;
            unsigned : 1;       // unused
            unsigned byref : 1; // used
            unsigned : 1;       // unused
            unsigned : 1;       // unused
            unsigned : 1;       // unused
            unsigned : 1;       // unused
            unsigned : 1;       // unused
            unsigned : 1;       // unused
            unsigned : 1;       // unused
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

    typeInfo(ti_types tiType, CORINFO_CLASS_HANDLE cls)
    {
        assert(tiType == TI_STRUCT || tiType == TI_REF);
        assert(cls != nullptr && !isInvalidHandle(cls));
        m_flags = tiType;
        m_cls   = cls;
    }

    typeInfo(methodPointerInfo* methodPointerInfo)
    {
        assert(methodPointerInfo != nullptr);
        assert(methodPointerInfo->m_token.hMethod != nullptr);
        assert(!isInvalidHandle(methodPointerInfo->m_token.hMethod));
        m_flags             = TI_METHOD;
        m_methodPointerInfo = methodPointerInfo;
    }

public:
    /////////////////////////////////////////////////////////////////////////
    // Operations
    /////////////////////////////////////////////////////////////////////////

    typeInfo& MakeByRef()
    {
        assert(!IsByRef());
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
        return (m_flags & (TI_FLAG_DATA_MASK | TI_FLAG_BYREF)) == DWORD(type);
    }

    // Returns whether this is a by-ref
    bool IsByRef() const
    {
        return (m_flags & TI_FLAG_BYREF);
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

private:
    // used to make functions that return typeinfo efficient.
    typeInfo(DWORD flags, CORINFO_CLASS_HANDLE cls)
    {
        m_cls   = cls;
        m_flags = flags;
    }

    friend typeInfo ByRef(const typeInfo& ti);
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
/*****************************************************************************/
#endif // _TYPEINFO_H_
/*****************************************************************************/
