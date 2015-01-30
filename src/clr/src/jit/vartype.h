//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*****************************************************************************/
#ifndef _VARTYPE_H_
#define _VARTYPE_H_
/*****************************************************************************/
#include "error.h"

enum    var_types_classification
{
    VTF_ANY = 0x0000,
    VTF_INT = 0x0001,
    VTF_UNS = 0x0002,   // type is unsigned
    VTF_FLT = 0x0004,
    VTF_GCR = 0x0008,   // type is GC ref
    VTF_BYR = 0x0010,   // type is Byref
    VTF_I   = 0x0020,   // is machine sized 
};

DECLARE_TYPED_ENUM(var_types,BYTE)
{
    #define DEF_TP(tn,nm,jitType,verType,sz,sze,asze,st,al,tf,howUsed) TYP_##tn,
    #include "typelist.h"
    #undef  DEF_TP

    TYP_COUNT,

    TYP_lastIntrins = TYP_DOUBLE
}
END_DECLARE_TYPED_ENUM(var_types,BYTE)

/*****************************************************************************
 * C-style pointers are implemented as TYP_INT or TYP_LONG depending on the
 * platform
 */

#ifdef _WIN64
#define TYP_I_IMPL          TYP_LONG
#define TYP_U_IMPL          TYP_ULONG
#define TYPE_REF_IIM        TYPE_REF_LNG
#else
#define TYP_I_IMPL          TYP_INT
#define TYP_U_IMPL          TYP_UINT
#define TYPE_REF_IIM        TYPE_REF_INT
#ifdef _PREFAST_
// We silence this in the 32-bit build because for portability, we like to have asserts like this:
// assert(op2->gtType == TYP_INT || op2->gtType == TYP_I_IMPL);
// This is obviously redundant for 32-bit builds, but we don't want to have ifdefs and different
// asserts just for 64-bit builds, so for now just silence the assert
#pragma warning(disable: 6287) // warning 6287: the left and right sub-expressions are identical
#endif //_PREFAST_
#endif


/*****************************************************************************/

const extern  BYTE  varTypeClassification[TYP_COUNT];

// make any class with a TypeGet member also have a function TypeGet() that does the same thing
template<class T>
inline var_types TypeGet(T * t) { return t->TypeGet(); }

// make a TypeGet function which is the identity function for var_types
// the point of this and the preceding template is now you can make template functions 
// that work on var_types as well as any object that exposes a TypeGet method.
// such as all of these varTypeIs* functions
inline var_types TypeGet(var_types v) { return v; }

#ifdef FEATURE_SIMD
template <class T>
inline  bool        varTypeIsSIMD(T vt)
{
    if (TypeGet(vt) == TYP_SIMD12)
    {
        return true;
    }

    if (TypeGet(vt) == TYP_SIMD16)
    {
        return true;
    }

#ifdef FEATURE_AVX_SUPPORT
    if (TypeGet(vt) == TYP_SIMD32)
    {
        return true;
    }
#endif // FEATURE_AVX_SUPPORT
    return false;
}
#else // FEATURE_SIMD

// Always return false if FEATURE_SIMD is not enabled
template <class T>
inline  bool        varTypeIsSIMD(T vt)
{
    return false;
}  
#endif // !FEATURE_SIMD

template <class T>
inline  bool        varTypeIsIntegral(T vt)
{
    return  ((varTypeClassification[TypeGet(vt)] & (VTF_INT        )) != 0);
}

template <class T>
inline  bool        varTypeIsIntegralOrI(T vt)
{
    return  ((varTypeClassification[TypeGet(vt)] & (VTF_INT|VTF_I  )) != 0);
}

template <class T>
inline  bool        varTypeIsUnsigned  (T vt)
{
    return  ((varTypeClassification[TypeGet(vt)] & (VTF_UNS        )) != 0);
}

// If "vt" is an unsigned integral type, returns the corresponding signed integral type, otherwise
// return "vt".
inline var_types    varTypeUnsignedToSigned(var_types vt)
{
    if (varTypeIsUnsigned(vt))
    {
        switch (vt)
        {
        case TYP_BOOL:
        case TYP_UBYTE: return TYP_BYTE;
        case TYP_USHORT: 
        case TYP_CHAR: return TYP_SHORT;
        case TYP_UINT: return TYP_INT;
        case TYP_ULONG: return TYP_LONG;
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
inline  bool        varTypeIsFloating  (T vt)
{
    return  ((varTypeClassification[TypeGet(vt)] & (VTF_FLT        )) != 0);
}

template <class T>
inline  bool        varTypeIsArithmetic(T vt)
{
    return  ((varTypeClassification[TypeGet(vt)] & (VTF_INT|VTF_FLT)) != 0);
}

template <class T>
inline unsigned      varTypeGCtype     (T vt)
{
    return  (unsigned)(varTypeClassification[TypeGet(vt)] & (VTF_GCR|VTF_BYR));
}

template <class T>
inline bool         varTypeIsGC        (T vt)
{
    return  (varTypeGCtype(vt) != 0);
}

template <class T>
inline bool         varTypeIsI         (T vt)
{
    return          ((varTypeClassification[TypeGet(vt)] & VTF_I) != 0); 
}

template <class T>
inline bool         varTypeCanReg      (T vt)
{
    return          ((varTypeClassification[TypeGet(vt)] & (VTF_INT|VTF_I|VTF_FLT)) != 0);
}

template <class T>
inline bool         varTypeIsByte      (T vt)
{
    return          (TypeGet(vt) >= TYP_BOOL) && (TypeGet(vt) <= TYP_UBYTE);
}

template <class T>
inline bool         varTypeIsShort     (T vt)
{
    return          (TypeGet(vt) >= TYP_CHAR) && (TypeGet(vt) <= TYP_USHORT);
}

template <class T>
inline bool         varTypeIsSmall     (T vt)
{
    return          (TypeGet(vt) >= TYP_BOOL) && (TypeGet(vt) <= TYP_USHORT);
}

template <class T>
inline bool         varTypeIsSmallInt  (T vt)
{
    return          (TypeGet(vt) >= TYP_BYTE) && (TypeGet(vt) <= TYP_USHORT);
}

template <class T>
inline bool         varTypeIsIntOrI   (T vt)
{
    return          ((TypeGet(vt) == TYP_INT)
#ifdef _TARGET_64BIT_
                    || (TypeGet(vt) == TYP_I_IMPL)
#endif // _TARGET_64BIT_
                    );
}

template <class T>
inline bool         genActualTypeIsIntOrI   (T vt)
{
    return          ((TypeGet(vt) >= TYP_BOOL) && (TypeGet(vt) <= TYP_U_IMPL));
}

template <class T>
inline bool         varTypeIsLong     (T vt)
{
    return          (TypeGet(vt) >= TYP_LONG) && (TypeGet(vt) <= TYP_ULONG);
}

template <class T>
inline bool         varTypeIsMultiReg     (T vt)
{
#ifdef _TARGET_64BIT_
    return false;
#else
    return          (TypeGet(vt) == TYP_LONG);
#endif
}

template <class T>
inline bool         varTypeIsSingleReg     (T vt)
{
    return !varTypeIsMultiReg(vt);
}

template <class T>
inline  bool        varTypeIsComposite(T vt)
{
    return  (!varTypeIsArithmetic(TypeGet(vt)) && TypeGet(vt) != TYP_VOID);
}

// Is this type promotable?
// In general only structs are promotable.
// However, a SIMD type, e.g. TYP_SIMD may be handled as either a struct, OR a
// fully-promoted register type.
// On 32-bit systems longs are split into an upper and lower half, and they are
// handled as if they are structs with two integer fields.

template <class T>
inline  bool        varTypeIsPromotable(T vt)
{
    return (TypeGet(vt) == TYP_STRUCT ||
           (TypeGet(vt) == TYP_BLK)   ||
#if !defined(_TARGET_64BIT_)
           varTypeIsLong(vt)          ||
#endif // !defined(_TARGET_64BIT_)
           varTypeIsSIMD(vt));
}

/*****************************************************************************/
#endif // _VARTYPE_H_
/*****************************************************************************/
