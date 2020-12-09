// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#ifndef _VARTYPE_H_
#define _VARTYPE_H_
/*****************************************************************************/
#include "error.h"

enum var_types_classification
{
    VTF_ANY = 0x0000,
    VTF_INT = 0x0001,
    VTF_UNS = 0x0002, // type is unsigned
    VTF_FLT = 0x0004,
    VTF_GCR = 0x0008, // type is GC ref
    VTF_BYR = 0x0010, // type is Byref
    VTF_I   = 0x0020, // is machine sized
    VTF_S   = 0x0040, // is a struct type
};

enum var_types : BYTE
{
#define DEF_TP(tn, nm, jitType, verType, sz, sze, asze, st, al, tf, howUsed) TYP_##tn,
#include "typelist.h"
#undef DEF_TP
    TYP_COUNT
};

/*****************************************************************************
 * C-style pointers are implemented as TYP_INT or TYP_LONG depending on the
 * platform
 */

#ifdef TARGET_64BIT
#define TYP_I_IMPL TYP_LONG
#define TYP_U_IMPL TYP_ULONG
#define TYPE_REF_IIM TYPE_REF_LNG
#else
#define TYP_I_IMPL TYP_INT
#define TYP_U_IMPL TYP_UINT
#define TYPE_REF_IIM TYPE_REF_INT
#ifdef _PREFAST_
// We silence this in the 32-bit build because for portability, we like to have asserts like this:
// assert(op2->gtType == TYP_INT || op2->gtType == TYP_I_IMPL);
// This is obviously redundant for 32-bit builds, but we don't want to have ifdefs and different
// asserts just for 64-bit builds, so for now just silence the assert
#pragma warning(disable : 6287) // warning 6287: the left and right sub-expressions are identical
#endif                          //_PREFAST_
#endif

/*****************************************************************************/

const extern BYTE varTypeClassification[TYP_COUNT];

// make any class with a TypeGet member also have a function TypeGet() that does the same thing
template <class T>
inline var_types TypeGet(T* t)
{
    return t->TypeGet();
}

// make a TypeGet function which is the identity function for var_types
// the point of this and the preceding template is now you can make template functions
// that work on var_types as well as any object that exposes a TypeGet method.
// such as all of these varTypeIs* functions
inline var_types TypeGet(var_types v)
{
    return v;
}

#ifdef FEATURE_SIMD
template <class T>
inline bool varTypeIsSIMD(T vt)
{
    switch (TypeGet(vt))
    {
        case TYP_SIMD8:
        case TYP_SIMD12:
        case TYP_SIMD16:
        case TYP_SIMD32:
            return true;
        default:
            return false;
    }
}
#else  // FEATURE_SIMD

// Always return false if FEATURE_SIMD is not enabled
template <class T>
inline bool varTypeIsSIMD(T vt)
{
    return false;
}
#endif // !FEATURE_SIMD

template <class T>
inline bool varTypeIsIntegral(T vt)
{
    return ((varTypeClassification[TypeGet(vt)] & (VTF_INT)) != 0);
}

template <class T>
inline bool varTypeIsIntegralOrI(T vt)
{
    return ((varTypeClassification[TypeGet(vt)] & (VTF_INT | VTF_I)) != 0);
}

template <class T>
inline bool varTypeIsUnsigned(T vt)
{
    return ((varTypeClassification[TypeGet(vt)] & (VTF_UNS)) != 0);
}

template <class T>
inline bool varTypeIsSigned(T vt)
{
    return varTypeIsIntegralOrI(vt) && !varTypeIsUnsigned(vt);
}

// If "vt" is an unsigned integral type, returns the corresponding signed integral type, otherwise
// return "vt".
inline var_types varTypeUnsignedToSigned(var_types vt)
{
    if (varTypeIsUnsigned(vt))
    {
        switch (vt)
        {
            case TYP_BOOL:
            case TYP_UBYTE:
                return TYP_BYTE;
            case TYP_USHORT:
                return TYP_SHORT;
            case TYP_UINT:
                return TYP_INT;
            case TYP_ULONG:
                return TYP_LONG;
            default:
                unreached();
        }
    }
    else
    {
        return vt;
    }
}

// If "vt" is a signed integral type, returns the corresponding unsigned integral type, otherwise
// return "vt".
inline var_types varTypeSignedToUnsigned(var_types vt)
{
    if (varTypeIsSigned(vt))
    {
        switch (vt)
        {
            case TYP_BYTE:
                return TYP_UBYTE;
            case TYP_SHORT:
                return TYP_USHORT;
            case TYP_INT:
                return TYP_UINT;
            case TYP_LONG:
                return TYP_ULONG;
            default:
                unreached();
        }
    }
    else
    {
        return vt;
    }
}

template <class T>
inline bool varTypeIsFloating(T vt)
{
    return ((varTypeClassification[TypeGet(vt)] & (VTF_FLT)) != 0);
}

template <class T>
inline bool varTypeIsArithmetic(T vt)
{
    return ((varTypeClassification[TypeGet(vt)] & (VTF_INT | VTF_FLT)) != 0);
}

template <class T>
inline unsigned varTypeGCtype(T vt)
{
    return (unsigned)(varTypeClassification[TypeGet(vt)] & (VTF_GCR | VTF_BYR));
}

template <class T>
inline bool varTypeIsGC(T vt)
{
    return (varTypeGCtype(vt) != 0);
}

template <class T>
inline bool varTypeIsI(T vt)
{
    return ((varTypeClassification[TypeGet(vt)] & VTF_I) != 0);
}

template <class T>
inline bool varTypeIsEnregisterable(T vt)
{
    return (TypeGet(vt) != TYP_STRUCT);
}

template <class T>
inline bool varTypeIsByte(T vt)
{
    return (TypeGet(vt) >= TYP_BOOL) && (TypeGet(vt) <= TYP_UBYTE);
}

template <class T>
inline bool varTypeIsShort(T vt)
{
    return (TypeGet(vt) == TYP_SHORT) || (TypeGet(vt) == TYP_USHORT);
}

template <class T>
inline bool varTypeIsSmall(T vt)
{
    return (TypeGet(vt) >= TYP_BOOL) && (TypeGet(vt) <= TYP_USHORT);
}

template <class T>
inline bool varTypeIsSmallInt(T vt)
{
    return (TypeGet(vt) >= TYP_BYTE) && (TypeGet(vt) <= TYP_USHORT);
}

template <class T>
inline bool varTypeIsIntOrI(T vt)
{
    return ((TypeGet(vt) == TYP_INT)
#ifdef TARGET_64BIT
            || (TypeGet(vt) == TYP_I_IMPL)
#endif // TARGET_64BIT
                );
}

template <class T>
inline bool genActualTypeIsIntOrI(T vt)
{
    return ((TypeGet(vt) >= TYP_BOOL) && (TypeGet(vt) <= TYP_U_IMPL));
}

template <class T>
inline bool varTypeIsLong(T vt)
{
    return (TypeGet(vt) >= TYP_LONG) && (TypeGet(vt) <= TYP_ULONG);
}

template <class T>
inline bool varTypeIsMultiReg(T vt)
{
#ifdef TARGET_64BIT
    return false;
#else
    return (TypeGet(vt) == TYP_LONG);
#endif
}

template <class T>
inline bool varTypeIsSingleReg(T vt)
{
    return !varTypeIsMultiReg(vt);
}

template <class T>
inline bool varTypeIsComposite(T vt)
{
    return (!varTypeIsArithmetic(TypeGet(vt)) && TypeGet(vt) != TYP_VOID);
}

// Is this type promotable?
// In general only structs are promotable.
// However, a SIMD type, e.g. TYP_SIMD may be handled as either a struct, OR a
// fully-promoted register type.
// On 32-bit systems longs are split into an upper and lower half, and they are
// handled as if they are structs with two integer fields.

template <class T>
inline bool varTypeIsPromotable(T vt)
{
    return (varTypeIsStruct(vt) || (TypeGet(vt) == TYP_BLK)
#if !defined(TARGET_64BIT)
            || varTypeIsLong(vt)
#endif // !defined(TARGET_64BIT)
                );
}

template <class T>
inline bool varTypeIsStruct(T vt)
{
    return ((varTypeClassification[TypeGet(vt)] & VTF_S) != 0);
}

template <class T>
inline bool varTypeUsesFloatReg(T vt)
{
    // Note that not all targets support SIMD, but if they don't, varTypeIsSIMD will
    // always return false.
    return varTypeIsFloating(vt) || varTypeIsSIMD(vt);
}

template <class T>
inline bool varTypeUsesFloatArgReg(T vt)
{
#ifdef TARGET_ARM64
    // Arm64 passes SIMD types in floating point registers.
    return varTypeUsesFloatReg(vt);
#else
    // Other targets pass them as regular structs - by reference or by value.
    return varTypeIsFloating(vt);
#endif
}

//------------------------------------------------------------------------
// varTypeIsValidHfaType: Determine if the type is a valid HFA type
//
// Arguments:
//    vt - the type of interest
//
// Return Value:
//    Returns true iff the type is a valid HFA type.
//
// Notes:
//    This should only be called with the return value from GetHfaType().
//    The only valid values are TYP_UNDEF, for which this returns false,
//    TYP_FLOAT, TYP_DOUBLE, or (ARM64-only) TYP_SIMD*.
//
template <class T>
inline bool varTypeIsValidHfaType(T vt)
{
#ifdef FEATURE_HFA
    bool isValid = (TypeGet(vt) != TYP_UNDEF);
    if (isValid)
    {
#ifdef TARGET_ARM64
        assert(varTypeUsesFloatReg(vt));
#else  // !TARGET_ARM64
        assert(varTypeIsFloating(vt));
#endif // !TARGET_ARM64
    }
    return isValid;
#else  // !FEATURE_HFA
    return false;
#endif // !FEATURE_HFA
}

/*****************************************************************************/
#endif // _VARTYPE_H_
/*****************************************************************************/
