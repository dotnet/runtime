// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "jitinterface.h"
#include "codeman.h"
#include "method.hpp"
#include "class.h"
#include "object.h"
#include "field.h"
#include "stublink.h"
#include "virtualcallstub.h"
#include "corjit.h"
#include "eeconfig.h"
#include "excep.h"
#include "log.h"
#include "excep.h"
#include "float.h"      // for isnan
#include "dbginterface.h"
#include "dllimport.h"
#include "gcheaputilities.h"
#include "comdelegate.h"
#include "corprof.h"
#include "eeprofinterfaces.h"
#include "dynamicinterfacecastable.h"

#ifndef TARGET_UNIX
// Included for referencing __report_gsfailure
#include "process.h"
#endif // !TARGET_UNIX

#ifdef PROFILING_SUPPORTED
#include "proftoeeinterfaceimpl.h"
#endif
#include "ecall.h"
#include "generics.h"
#include "typestring.h"
#include "typedesc.h"
#include "genericdict.h"
#include "array.h"
#include "debuginfostore.h"
#include "safemath.h"
#include "threadstatics.h"

#ifdef HAVE_GCCOVER
#include "gccover.h"
#endif // HAVE_GCCOVER

#include "runtimehandles.h"
#include "castcache.h"
#include "onstackreplacement.h"
#include "pgo.h"
#include "pgo_formatprocessing.h"

#ifndef FEATURE_EH_FUNCLETS
#include "excep.h"
#endif

#include "exinfo.h"

//========================================================================
//
// This file contains implementation of all JIT helpers. The helpers are
// divided into following categories:
//
//      INTEGER ARITHMETIC HELPERS
//      FLOATING POINT HELPERS
//      INSTANCE FIELD HELPERS
//      STATIC FIELD HELPERS
//      SHARED STATIC FIELD HELPERS
//      CASTING HELPERS
//      ALLOCATION HELPERS
//      STRING HELPERS
//      ARRAY HELPERS
//      VALUETYPE/BYREF HELPERS
//      GENERICS HELPERS
//      EXCEPTION HELPERS
//      DEBUGGER/PROFILER HELPERS
//      GC HELPERS
//      INTEROP HELPERS
//
//========================================================================



//========================================================================
//
//      INTEGER ARITHMETIC HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>

//
// helper macro to multiply two 32-bit uints
//
#define Mul32x32To64(a, b)  ((UINT64)((UINT32)(a)) * (UINT64)((UINT32)(b)))

//
// helper macro to get high 32-bit of 64-bit int
//
#define Hi32Bits(a)         ((UINT32)((UINT64)(a) >> 32))

//
// helper macro to check whether 64-bit signed int fits into 32-bit signed (compiles into one 32-bit compare)
//
#define Is32BitSigned(a)    (Hi32Bits(a) == Hi32Bits((INT64)(INT32)(a)))

//
// helper function to shift the result by 32-bits
//
inline UINT64 ShiftToHi32Bits(UINT32 x)
{
    // The shift compiles into slow multiplication by 2^32! VSWhidbey 360736
    // return ((UINT64)x) << 32;

    ULARGE_INTEGER ret;
    ret.u.HighPart = x;
    ret.u.LowPart = 0;
    return ret.QuadPart;
}

#if !defined(TARGET_X86) || defined(TARGET_UNIX)
/*********************************************************************/
HCIMPL2_VV(INT64, JIT_LMul, INT64 val1, INT64 val2)
{
    FCALL_CONTRACT;

    UINT32 val1High = Hi32Bits(val1);
    UINT32 val2High = Hi32Bits(val2);

    if ((val1High == 0) && (val2High == 0))
        return Mul32x32To64(val1, val2);

    return (val1 * val2);
}
HCIMPLEND
#endif // !TARGET_X86 || TARGET_UNIX

/*********************************************************************/
HCIMPL2_VV(INT64, JIT_LMulOvf, INT64 val1, INT64 val2)
{
    FCALL_CONTRACT;

    // This short-cut does not actually help since the multiplication
    // of two 32-bit signed ints compiles into the call to a slow helper
    // if (Is32BitSigned(val1) && Is32BitSigned(val2))
    //     return (INT64)(INT32)val1 * (INT64)(INT32)val2;

    INDEBUG(INT64 expected = val1 * val2;)
    INT64 ret;

        // Remember the sign of the result
    INT32 sign = Hi32Bits(val1) ^ Hi32Bits(val2);

        // Convert to unsigned multiplication
    if (val1 < 0) val1 = -val1;
    if (val2 < 0) val2 = -val2;

        // Get the upper 32 bits of the numbers
    UINT32 val1High = Hi32Bits(val1);
    UINT32 val2High = Hi32Bits(val2);

    UINT64 valMid;

    if (val1High == 0) {
        // Compute the 'middle' bits of the long multiplication
        valMid = Mul32x32To64(val2High, val1);
    }
    else {
        if (val2High != 0)
            goto ThrowExcep;
        // Compute the 'middle' bits of the long multiplication
        valMid = Mul32x32To64(val1High, val2);
    }

        // See if any bits after bit 32 are set
    if (Hi32Bits(valMid) != 0)
        goto ThrowExcep;

    ret = Mul32x32To64(val1, val2) + ShiftToHi32Bits((UINT32)(valMid));

    // check for overflow
    if (Hi32Bits(ret) < (UINT32)valMid)
        goto ThrowExcep;

    if (sign >= 0) {
        // have we spilled into the sign bit?
        if (ret < 0)
            goto ThrowExcep;
    }
    else {
        ret = -ret;
        // have we spilled into the sign bit?
        if (ret > 0)
            goto ThrowExcep;
    }
    _ASSERTE(ret == expected);
    return ret;

ThrowExcep:
    FCThrow(kOverflowException);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(UINT64, JIT_ULMulOvf, UINT64 val1, UINT64 val2)
{
    FCALL_CONTRACT;

    INDEBUG(UINT64 expected = val1 * val2;)
    UINT64 ret;

        // Get the upper 32 bits of the numbers
    UINT32 val1High = Hi32Bits(val1);
    UINT32 val2High = Hi32Bits(val2);

    UINT64 valMid;

    if (val1High == 0) {
        if (val2High == 0)
            return Mul32x32To64(val1, val2);
        // Compute the 'middle' bits of the long multiplication
        valMid = Mul32x32To64(val2High, val1);
    }
    else {
        if (val2High != 0)
            goto ThrowExcep;
        // Compute the 'middle' bits of the long multiplication
        valMid = Mul32x32To64(val1High, val2);
    }

        // See if any bits after bit 32 are set
    if (Hi32Bits(valMid) != 0)
        goto ThrowExcep;

    ret = Mul32x32To64(val1, val2) + ShiftToHi32Bits((UINT32)(valMid));

    // check for overflow
    if (Hi32Bits(ret) < (UINT32)valMid)
        goto ThrowExcep;

    _ASSERTE(ret == expected);
    return ret;

ThrowExcep:
        FCThrow(kOverflowException);
    }
HCIMPLEND

/*********************************************************************/
HCIMPL2(INT32, JIT_Div, INT32 dividend, INT32 divisor)
{
    FCALL_CONTRACT;

    RuntimeExceptionKind ehKind;

    if (((UINT32) (divisor + 1)) <= 1)  // Unsigned test for divisor in [-1 .. 0]
    {
        if (divisor == 0)
        {
            ehKind = kDivideByZeroException;
            goto ThrowExcep;
        }
        else if (divisor == -1)
        {
            if (dividend == _I32_MIN)
            {
                ehKind = kOverflowException;
                goto ThrowExcep;
            }
            return -dividend;
        }
    }

    return(dividend / divisor);

ThrowExcep:
    FCThrow(ehKind);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2(INT32, JIT_Mod, INT32 dividend, INT32 divisor)
{
    FCALL_CONTRACT;

    RuntimeExceptionKind ehKind;

    if (((UINT32) (divisor + 1)) <= 1)  // Unsigned test for divisor in [-1 .. 0]
    {
        if (divisor == 0)
        {
            ehKind = kDivideByZeroException;
            goto ThrowExcep;
        }
        else if (divisor == -1)
        {
            if (dividend == _I32_MIN)
            {
                ehKind = kOverflowException;
                goto ThrowExcep;
            }
            return 0;
        }
    }

    return(dividend % divisor);

ThrowExcep:
    FCThrow(ehKind);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2(UINT32, JIT_UDiv, UINT32 dividend, UINT32 divisor)
{
    FCALL_CONTRACT;

    if (divisor == 0)
        FCThrow(kDivideByZeroException);

    return(dividend / divisor);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2(UINT32, JIT_UMod, UINT32 dividend, UINT32 divisor)
{
    FCALL_CONTRACT;

    if (divisor == 0)
        FCThrow(kDivideByZeroException);

    return(dividend % divisor);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(INT64, JIT_LDiv, INT64 dividend, INT64 divisor)
{
    FCALL_CONTRACT;

    RuntimeExceptionKind ehKind;

    if (Is32BitSigned(divisor))
    {
        if ((INT32)divisor == 0)
        {
            ehKind = kDivideByZeroException;
            goto ThrowExcep;
        }

        if ((INT32)divisor == -1)
        {
            if ((UINT64) dividend == UI64(0x8000000000000000))
            {
                ehKind = kOverflowException;
                goto ThrowExcep;
            }
            return -dividend;
        }

        // Check for -ive or +ive numbers in the range -2**31 to 2**31
        if (Is32BitSigned(dividend))
            return((INT32)dividend / (INT32)divisor);
    }

    // For all other combinations fallback to int64 div.
    return(dividend / divisor);

ThrowExcep:
    FCThrow(ehKind);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(INT64, JIT_LMod, INT64 dividend, INT64 divisor)
{
    FCALL_CONTRACT;

    RuntimeExceptionKind ehKind;

    if (Is32BitSigned(divisor))
    {
        if ((INT32)divisor == 0)
        {
            ehKind = kDivideByZeroException;
            goto ThrowExcep;
        }

        if ((INT32)divisor == -1)
        {
            // <TODO>TODO, we really should remove this as it lengthens the code path
            // and the spec really says that it should not throw an exception. </TODO>
            if ((UINT64) dividend == UI64(0x8000000000000000))
            {
                ehKind = kOverflowException;
                goto ThrowExcep;
            }
            return 0;
        }

        // Check for -ive or +ive numbers in the range -2**31 to 2**31
        if (Is32BitSigned(dividend))
            return((INT32)dividend % (INT32)divisor);
    }

    // For all other combinations fallback to int64 div.
    return(dividend % divisor);

ThrowExcep:
    FCThrow(ehKind);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(UINT64, JIT_ULDiv, UINT64 dividend, UINT64 divisor)
{
    FCALL_CONTRACT;

    if (Hi32Bits(divisor) == 0)
    {
        if ((UINT32)(divisor) == 0)
        FCThrow(kDivideByZeroException);

        if (Hi32Bits(dividend) == 0)
            return((UINT32)dividend / (UINT32)divisor);
    }

    return(dividend / divisor);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(UINT64, JIT_ULMod, UINT64 dividend, UINT64 divisor)
{
    FCALL_CONTRACT;

    if (Hi32Bits(divisor) == 0)
    {
        if ((UINT32)(divisor) == 0)
        FCThrow(kDivideByZeroException);

        if (Hi32Bits(dividend) == 0)
            return((UINT32)dividend % (UINT32)divisor);
    }

    return(dividend % divisor);
}
HCIMPLEND

#if !defined(HOST_64BIT) && !defined(TARGET_X86)
/*********************************************************************/
HCIMPL2_VV(UINT64, JIT_LLsh, UINT64 num, int shift)
{
    FCALL_CONTRACT;
    return num << (shift & 0x3F);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(INT64, JIT_LRsh, INT64 num, int shift)
{
    FCALL_CONTRACT;
    return num >> (shift & 0x3F);
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(UINT64, JIT_LRsz, UINT64 num, int shift)
{
    FCALL_CONTRACT;
    return num >> (shift & 0x3F);
}
HCIMPLEND
#endif // !HOST_64BIT && !TARGET_X86

#include <optdefault.h>


//========================================================================
//
//      FLOATING POINT HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>

/*********************************************************************/
//
HCIMPL1_V(double, JIT_ULng2Dbl, UINT64 val)
{
    FCALL_CONTRACT;

    double conv = (double) ((INT64) val);
    if (conv < 0)
        conv += (4294967296.0 * 4294967296.0);  // add 2^64
    _ASSERTE(conv >= 0);
    return(conv);
}
HCIMPLEND

/*********************************************************************/
// needed for ARM and RyuJIT-x86
HCIMPL1_V(double, JIT_Lng2Dbl, INT64 val)
{
    FCALL_CONTRACT;
    return double(val);
}
HCIMPLEND

//--------------------------------------------------------------------------
template <class ftype>
ftype modftype(ftype value, ftype *iptr);
template <> float modftype(float value, float *iptr) { return modff(value, iptr); }
template <> double modftype(double value, double *iptr) { return modf(value, iptr); }

// round to nearest, round to even if tied
template <class ftype>
ftype BankersRound(ftype value)
{
    if (value < 0.0) return -BankersRound <ftype> (-value);

    ftype integerPart;
    modftype( value, &integerPart );

    // if decimal part is exactly .5
    if ((value -(integerPart +0.5)) == 0.0)
    {
        // round to even
        if (fmod(ftype(integerPart), ftype(2.0)) == 0.0)
            return integerPart;

        // Else return the nearest even integer
        return (ftype)copysign(ceil(fabs(value+0.5)),
                         value);
    }

    // Otherwise round to closest
    return (ftype)copysign(floor(fabs(value)+0.5),
                     value);
}


/*********************************************************************/
// round double to nearest int (as double)
HCIMPL1_V(double, JIT_DoubleRound, double val)
{
    FCALL_CONTRACT;
    return BankersRound(val);
}
HCIMPLEND

/*********************************************************************/
// round float to nearest int (as float)
HCIMPL1_V(float, JIT_FloatRound, float val)
{
    FCALL_CONTRACT;
    return BankersRound(val);
}
HCIMPLEND

/*********************************************************************/
// Call fast Dbl2Lng conversion - used by functions below
FORCEINLINE INT64 FastDbl2Lng(double val)
{
#ifdef TARGET_X86
    FCALL_CONTRACT;
    return HCCALL1_V(JIT_Dbl2Lng, val);
#else
    FCALL_CONTRACT;
    return((__int64) val);
#endif
}

/*********************************************************************/
HCIMPL1_V(UINT32, JIT_Dbl2UIntOvf, double val)
{
    FCALL_CONTRACT;

        // Note that this expression also works properly for val = NaN case
    if (val > -1.0 && val < 4294967296.0)
        return((UINT32)FastDbl2Lng(val));

    FCThrow(kOverflowException);
}
HCIMPLEND

/*********************************************************************/
HCIMPL1_V(UINT64, JIT_Dbl2ULng, double val)
{
    FCALL_CONTRACT;

    const double two63  = 2147483648.0 * 4294967296.0;
    UINT64 ret;
    if (val < two63) {
        ret = FastDbl2Lng(val);
    }
    else {
        // subtract 0x8000000000000000, do the convert then add it back again
        ret = FastDbl2Lng(val - two63) + I64(0x8000000000000000);
    }
    return ret;
}
HCIMPLEND

/*********************************************************************/
HCIMPL1_V(UINT64, JIT_Dbl2ULngOvf, double val)
{
    FCALL_CONTRACT;

    const double two64  = 4294967296.0 * 4294967296.0;
        // Note that this expression also works properly for val = NaN case
    if (val > -1.0 && val < two64) {
        const double two63  = 2147483648.0 * 4294967296.0;
        UINT64 ret;
        if (val < two63) {
            ret = FastDbl2Lng(val);
        }
        else {
            // subtract 0x8000000000000000, do the convert then add it back again
            ret = FastDbl2Lng(val - two63) + I64(0x8000000000000000);
        }
#ifdef _DEBUG
        // since no overflow can occur, the value always has to be within 1
        double roundTripVal = HCCALL1_V(JIT_ULng2Dbl, ret);
        _ASSERTE(val - 1.0 <= roundTripVal && roundTripVal <= val + 1.0);
#endif // _DEBUG
        return ret;
    }

    FCThrow(kOverflowException);
}
HCIMPLEND


#if !defined(TARGET_X86) || defined(TARGET_UNIX)

HCIMPL1_V(INT64, JIT_Dbl2Lng, double val)
{
    FCALL_CONTRACT;

    return((INT64)val);
}
HCIMPLEND

HCIMPL1_V(int, JIT_Dbl2IntOvf, double val)
{
    FCALL_CONTRACT;

    const double two31 = 2147483648.0;

        // Note that this expression also works properly for val = NaN case
    if (val > -two31 - 1 && val < two31)
        return((INT32)val);

    FCThrow(kOverflowException);
}
HCIMPLEND

HCIMPL1_V(INT64, JIT_Dbl2LngOvf, double val)
{
    FCALL_CONTRACT;

    const double two63  = 2147483648.0 * 4294967296.0;

    // Note that this expression also works properly for val = NaN case
    // We need to compare with the very next double to two63. 0x402 is epsilon to get us there.
    if (val > -two63 - 0x402 && val < two63)
        return((INT64)val);

    FCThrow(kOverflowException);
}
HCIMPLEND

#ifndef TARGET_WINDOWS
namespace
{
    bool isnan(float val)
    {
        UINT32 bits = *reinterpret_cast<UINT32*>(&val);
        return (bits & 0x7FFFFFFFU) > 0x7F800000U;
    }
    bool isnan(double val)
    {
        UINT64 bits = *reinterpret_cast<UINT64*>(&val);
        return (bits & 0x7FFFFFFFFFFFFFFFULL) > 0x7FF0000000000000ULL;
    }
    bool isfinite(float val)
    {
        UINT32 bits = *reinterpret_cast<UINT32*>(&val);
        return (~bits & 0x7F800000U) != 0;
    }
    bool isfinite(double val)
    {
        UINT64 bits = *reinterpret_cast<UINT64*>(&val);
        return (~bits & 0x7FF0000000000000ULL) != 0;
    }
}
#endif

HCIMPL2_VV(float, JIT_FltRem, float dividend, float divisor)
{
    FCALL_CONTRACT;

    //
    // From the ECMA standard:
    //
    // If [divisor] is zero or [dividend] is infinity
    //   the result is NaN.
    // If [divisor] is infinity,
    //   the result is [dividend] (negated for -infinity***).
    //
    // ***"negated for -infinity" has been removed from the spec
    //

    if (divisor==0 || !isfinite(dividend))
    {
        UINT32 NaN = CLR_NAN_32;
        return *(float *)(&NaN);
    }
    else if (!isfinite(divisor) && !isnan(divisor))
    {
        return dividend;
    }
    // else...
#if 0
    // COMPILER BUG WITH FMODF() + /Oi, USE FMOD() INSTEAD
    return fmodf(dividend,divisor);
#else
    return (float)fmod((double)dividend,(double)divisor);
#endif
}
HCIMPLEND

HCIMPL2_VV(double, JIT_DblRem, double dividend, double divisor)
{
    FCALL_CONTRACT;

    //
    // From the ECMA standard:
    //
    // If [divisor] is zero or [dividend] is infinity
    //   the result is NaN.
    // If [divisor] is infinity,
    //   the result is [dividend] (negated for -infinity***).
    //
    // ***"negated for -infinity" has been removed from the spec
    //
    if (divisor==0 || !isfinite(dividend))
    {
        UINT64 NaN = CLR_NAN_64;
        return *(double *)(&NaN);
    }
    else if (!isfinite(divisor) && !isnan(divisor))
    {
        return dividend;
    }
    // else...
    return(fmod(dividend,divisor));
}
HCIMPLEND

#endif // !TARGET_X86 || TARGET_UNIX

#include <optdefault.h>


//========================================================================
//
//      INSTANCE FIELD HELPERS
//
//========================================================================

/*********************************************************************/
// Returns the address of the instance field in the object (This is an interior
// pointer and the caller has to use it appropriately) or a static field.
// obj can be either a reference or a byref
HCIMPL2(void*, JIT_GetFieldAddr_Framed, Object *obj, FieldDesc* pFD)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pFD));
    } CONTRACTL_END;

    void * fldAddr = NULL;
    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    HELPER_METHOD_FRAME_BEGIN_RET_1(objRef);

    if (!pFD->IsStatic() && objRef == NULL)
        COMPlusThrow(kNullReferenceException);

    fldAddr = pFD->GetAddress(OBJECTREFToObject(objRef));

    HELPER_METHOD_FRAME_END();

    return fldAddr;
}
HCIMPLEND

#include <optsmallperfcritical.h>
HCIMPL2(void*, JIT_GetFieldAddr, Object *obj, FieldDesc* pFD)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pFD));
    } CONTRACTL_END;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL2(JIT_GetFieldAddr_Framed, obj, pFD);
    }

    return pFD->GetAddressGuaranteedInHeap(obj);
}
HCIMPLEND
#include <optdefault.h>

#include <optsmallperfcritical.h>
HCIMPL1(void*, JIT_GetStaticFieldAddr, FieldDesc* pFD)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pFD));
    } CONTRACTL_END;

    // [TODO] Only handling EnC for now
    _ASSERTE(pFD->IsEnCNew());

    {
        ENDFORBIDGC();
        return HCCALL2(JIT_GetFieldAddr_Framed, NULL, pFD);
    }
}
HCIMPLEND
#include <optdefault.h>

/*********************************************************************/
#define HCallAssert(cache, target) // suppressed to avoid ambiguous cast errors caused by use of template
template <typename FIELDTYPE>
NOINLINE HCIMPL2(FIELDTYPE, JIT_GetField_Framed, Object *obj, FieldDesc *pFD)
#undef HCallAssert
{
    FCALL_CONTRACT;

    FIELDTYPE value = 0;

    // This is an instance field helper
    _ASSERTE(!pFD->IsStatic());

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    HELPER_METHOD_FRAME_BEGIN_RET_1(objRef);
    if (objRef == NULL)
        COMPlusThrow(kNullReferenceException);
    pFD->GetInstanceField(objRef, &value);
    HELPER_METHOD_POLL();
    HELPER_METHOD_FRAME_END();

    return value;
}
HCIMPLEND

/*********************************************************************/
#include <optsmallperfcritical.h>

HCIMPL2(INT8, JIT_GetField8, Object *obj, FieldDesc *pFD)
{
    FCALL_CONTRACT;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL2(JIT_GetField_Framed<INT8>, obj, pFD);
    }

    INT8 val = VolatileLoad<INT8>((INT8*)pFD->GetAddressGuaranteedInHeap(obj));
    FC_GC_POLL_RET();
    return val;
}
HCIMPLEND

HCIMPL2(INT16, JIT_GetField16, Object *obj, FieldDesc *pFD)
{
    FCALL_CONTRACT;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL2(JIT_GetField_Framed<INT16>, obj, pFD);
    }

    INT16 val = VolatileLoad<INT16>((INT16*)pFD->GetAddressGuaranteedInHeap(obj));
    FC_GC_POLL_RET();
    return val;
}
HCIMPLEND

HCIMPL2(INT32, JIT_GetField32, Object *obj, FieldDesc *pFD)
{
    FCALL_CONTRACT;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL2(JIT_GetField_Framed<INT32>, obj, pFD);
    }

    INT32 val = VolatileLoad<INT32>((INT32*)pFD->GetAddressGuaranteedInHeap(obj));
    FC_GC_POLL_RET();
    return val;
}
HCIMPLEND

HCIMPL2(INT64, JIT_GetField64, Object *obj, FieldDesc *pFD)
{
    FCALL_CONTRACT;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL2(JIT_GetField_Framed<INT64>, obj, pFD);
    }

    INT64 val = VolatileLoad<INT64>((INT64*)pFD->GetAddressGuaranteedInHeap(obj));
    FC_GC_POLL_RET();
    return val;
}
HCIMPLEND

HCIMPL2(FLOAT, JIT_GetFieldFloat, Object *obj, FieldDesc *pFD)
{
    FCALL_CONTRACT;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL2(JIT_GetField_Framed<FLOAT>, obj, pFD);
    }

    FLOAT val;
    (INT32&)val = VolatileLoad<INT32>((INT32*)pFD->GetAddressGuaranteedInHeap(obj));
    FC_GC_POLL_RET();
    return val;
}
HCIMPLEND

HCIMPL2(DOUBLE, JIT_GetFieldDouble, Object *obj, FieldDesc *pFD)
{
    FCALL_CONTRACT;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL2(JIT_GetField_Framed<DOUBLE>, obj, pFD);
    }

    DOUBLE val;
    (INT64&)val = VolatileLoad<INT64>((INT64*)pFD->GetAddressGuaranteedInHeap(obj));
    FC_GC_POLL_RET();
    return val;
}
HCIMPLEND

#include <optdefault.h>

/*********************************************************************/
#define HCallAssert(cache, target) // suppressed to avoid ambiguous cast errors caused by use of template
template <typename FIELDTYPE>
NOINLINE HCIMPL3(VOID, JIT_SetField_Framed, Object *obj, FieldDesc* pFD, FIELDTYPE val)
#undef HCallAssert
{
    FCALL_CONTRACT;

    // This is an instance field helper
    _ASSERTE(!pFD->IsStatic());

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    HELPER_METHOD_FRAME_BEGIN_1(objRef);
    if (objRef == NULL)
        COMPlusThrow(kNullReferenceException);
    pFD->SetInstanceField(objRef, &val);
    HELPER_METHOD_POLL();
    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
#include <optsmallperfcritical.h>

HCIMPL3(VOID, JIT_SetField8, Object *obj, FieldDesc *pFD, INT8 val)
{
    FCALL_CONTRACT;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL3(JIT_SetField_Framed<INT8>, obj, pFD, val);
    }

    VolatileStore<INT8>((INT8*)pFD->GetAddressGuaranteedInHeap(obj), val);
    FC_GC_POLL();
}
HCIMPLEND

HCIMPL3(VOID, JIT_SetField16, Object *obj, FieldDesc *pFD, INT16 val)
{
    FCALL_CONTRACT;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL3(JIT_SetField_Framed<INT16>, obj, pFD, val);
    }

    VolatileStore<INT16>((INT16*)pFD->GetAddressGuaranteedInHeap(obj), val);
    FC_GC_POLL();
}
HCIMPLEND

HCIMPL3(VOID, JIT_SetField32, Object *obj, FieldDesc *pFD, INT32 val)
{
    FCALL_CONTRACT;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL3(JIT_SetField_Framed<INT32>, obj, pFD, val);
    }

    VolatileStore<INT32>((INT32*)pFD->GetAddressGuaranteedInHeap(obj), val);
    FC_GC_POLL();
}
HCIMPLEND

HCIMPL3(VOID, JIT_SetField64, Object *obj, FieldDesc *pFD, INT64 val)
{
    FCALL_CONTRACT;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL3(JIT_SetField_Framed<INT64>, obj, pFD, val);
    }

    VolatileStore<INT64>((INT64*)pFD->GetAddressGuaranteedInHeap(obj), val);
    FC_GC_POLL();
}
HCIMPLEND

HCIMPL3(VOID, JIT_SetFieldFloat, Object *obj, FieldDesc *pFD, FLOAT val)
{
    FCALL_CONTRACT;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL3(JIT_SetField_Framed<FLOAT>, obj, pFD, val);
    }

    VolatileStore<INT32>((INT32*)pFD->GetAddressGuaranteedInHeap(obj), (INT32&)val);
    FC_GC_POLL();
}
HCIMPLEND

HCIMPL3(VOID, JIT_SetFieldDouble, Object *obj, FieldDesc *pFD, DOUBLE val)
{
    FCALL_CONTRACT;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL3(JIT_SetField_Framed<DOUBLE>, obj, pFD, val);
    }

    VolatileStore<INT64>((INT64*)pFD->GetAddressGuaranteedInHeap(obj), (INT64&)val);
    FC_GC_POLL();
}
HCIMPLEND

#include <optdefault.h>

/*********************************************************************/
HCIMPL2(Object*, JIT_GetFieldObj_Framed, Object *obj, FieldDesc *pFD)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(!pFD->IsStatic());
        PRECONDITION(!pFD->IsPrimitive() && !pFD->IsByValue());  // Assert that we are called only for objects
    } CONTRACTL_END;

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    OBJECTREF val = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_2(objRef, val);        // Set up a frame
    if (objRef == NULL)
        COMPlusThrow(kNullReferenceException);
    pFD->GetInstanceField(objRef, &val);
    HELPER_METHOD_POLL();
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(val);
}
HCIMPLEND

#include <optsmallperfcritical.h>
HCIMPL2(Object*, JIT_GetFieldObj, Object *obj, FieldDesc *pFD)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(!pFD->IsStatic());
        PRECONDITION(!pFD->IsPrimitive() && !pFD->IsByValue());  // Assert that we are called only for objects
    } CONTRACTL_END;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL2(JIT_GetFieldObj_Framed, obj, pFD);
    }

    void * address = pFD->GetAddressGuaranteedInHeap(obj);
    OBJECTREF val = ObjectToOBJECTREF(VolatileLoad((Object **)address));

    FC_GC_POLL_AND_RETURN_OBJREF(val);
}
HCIMPLEND
#include <optdefault.h>

/*********************************************************************/
HCIMPL3(VOID, JIT_SetFieldObj_Framed, Object *obj, FieldDesc *pFD, Object *value)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(!pFD->IsStatic());
        PRECONDITION(!pFD->IsPrimitive() && !pFD->IsByValue());  // Assert that we are called only for objects
    } CONTRACTL_END;

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    OBJECTREF val = ObjectToOBJECTREF(value);

    HELPER_METHOD_FRAME_BEGIN_2(objRef, val);
    if (objRef == NULL)
        COMPlusThrow(kNullReferenceException);
    pFD->SetInstanceField(objRef, &val);
    HELPER_METHOD_POLL();
    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

#include <optsmallperfcritical.h>
HCIMPL3(VOID, JIT_SetFieldObj, Object *obj, FieldDesc *pFD, Object *value)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(!pFD->IsStatic());
        PRECONDITION(!pFD->IsPrimitive() && !pFD->IsByValue());  // Assert that we are called only for objects
    } CONTRACTL_END;

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL3(JIT_SetFieldObj_Framed, obj, pFD, value);
    }

    void * address = pFD->GetAddressGuaranteedInHeap(obj);
    SetObjectReference((OBJECTREF*)address, ObjectToOBJECTREF(value));
    FC_GC_POLL();
}
HCIMPLEND
#include <optdefault.h>

/*********************************************************************/
HCIMPL4(VOID, JIT_GetFieldStruct_Framed, LPVOID retBuff, Object *obj, FieldDesc *pFD, MethodTable *pFieldMT)
{
    FCALL_CONTRACT;

    // This may be a  cross context field access. Setup a frame as we will
    // transition to managed code later

    // This is an instance field helper
    _ASSERTE(!pFD->IsStatic());

    // Assert that we are not called for objects or primitive types
    _ASSERTE(!pFD->IsPrimitive());

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    HELPER_METHOD_FRAME_BEGIN_1(objRef);        // Set up a frame

    if (objRef == NULL)
        COMPlusThrow(kNullReferenceException);

    // Try an unwrap operation in case that we are not being called
    // in the same context as the server.
    // If that is the case then GetObjectFromProxy will return
    // the server object.
    BOOL fRemoted = FALSE;


    if (!fRemoted)
    {
        void * pAddr = pFD->GetAddress(OBJECTREFToObject(objRef));
        CopyValueClass(retBuff, pAddr, pFieldMT);
    }

    HELPER_METHOD_FRAME_END();          // Tear down the frame
}
HCIMPLEND

#include <optsmallperfcritical.h>
HCIMPL4(VOID, JIT_GetFieldStruct, LPVOID retBuff, Object *obj, FieldDesc *pFD, MethodTable *pFieldMT)
{
    FCALL_CONTRACT;

    _ASSERTE(pFieldMT->IsValueType());

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL4(JIT_GetFieldStruct_Framed, retBuff, obj, pFD, pFieldMT);
    }

    void * pAddr = pFD->GetAddressGuaranteedInHeap(obj);
    CopyValueClass(retBuff, pAddr, pFieldMT);
}
HCIMPLEND
#include <optdefault.h>

/*********************************************************************/
HCIMPL4(VOID, JIT_SetFieldStruct_Framed, Object *obj, FieldDesc *pFD, MethodTable *pFieldMT, LPVOID valuePtr)
{
    FCALL_CONTRACT;

    // Assert that we are not called for objects or primitive types
    _ASSERTE(!pFD->IsPrimitive());

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    // This may be a  cross context field access. Setup a frame as we will
    // transition to managed code later

    HELPER_METHOD_FRAME_BEGIN_1(objRef);        // Set up a frame

    if (objRef == NULL)
        COMPlusThrow(kNullReferenceException);

    // Try an unwrap operation in case that we are not being called
    // in the same context as the server.
    // If that is the case then GetObjectFromProxy will return
    // the server object.
    BOOL fRemoted = FALSE;


    if (!fRemoted)
    {
        void * pAddr = pFD->GetAddress(OBJECTREFToObject(objRef));
        CopyValueClass(pAddr, valuePtr, pFieldMT);
    }

    HELPER_METHOD_FRAME_END();          // Tear down the frame
}
HCIMPLEND

#include <optsmallperfcritical.h>
HCIMPL4(VOID, JIT_SetFieldStruct, Object *obj, FieldDesc *pFD, MethodTable *pFieldMT, LPVOID valuePtr)
{
    FCALL_CONTRACT;

    _ASSERTE(pFieldMT->IsValueType());

    if (obj == NULL || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL4(JIT_SetFieldStruct_Framed, obj, pFD, pFieldMT, valuePtr);
    }

    void * pAddr = pFD->GetAddressGuaranteedInHeap(obj);
    CopyValueClass(pAddr, valuePtr, pFieldMT);
}
HCIMPLEND
#include <optdefault.h>



//========================================================================
//
//      STATIC FIELD HELPERS
//
//========================================================================



// Slow helper to tailcall from the fast one
NOINLINE HCIMPL1(void, JIT_InitClass_Framed, MethodTable* pMT)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    // We don't want to be calling JIT_InitClass at all for perf reasons
    // on the Global Class <Module> as the Class loading logic ensures that we
    // already have initialized the Global Class <Module>
    CONSISTENCY_CHECK(!pMT->IsGlobalClass());

    pMT->CheckRestore();
    pMT->CheckRunClassInitThrowing();

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND


/*************************************************************/
#include <optsmallperfcritical.h>
HCIMPL1(void, JIT_InitClass, CORINFO_CLASS_HANDLE typeHnd_)
{
    FCALL_CONTRACT;

    TypeHandle typeHnd(typeHnd_);
    MethodTable *pMT = typeHnd.AsMethodTable();

    if (pMT->IsClassInited())
        return;

    // Tailcall to the slow helper
    ENDFORBIDGC();
    HCCALL1(JIT_InitClass_Framed, pMT);
}
HCIMPLEND
#include <optdefault.h>

/*************************************************************/
HCIMPL2(void, JIT_InitInstantiatedClass, CORINFO_CLASS_HANDLE typeHnd_, CORINFO_METHOD_HANDLE methHnd_)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(methHnd_ != NULL);
    } CONTRACTL_END;

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();    // Set up a frame

    MethodTable * pMT = (MethodTable*) typeHnd_;
    MethodDesc *  pMD = (MethodDesc*)  methHnd_;

    MethodTable * pTemplateMT = pMD->GetMethodTable();
    if (pTemplateMT->IsSharedByGenericInstantiations())
    {
        pMT = ClassLoader::LoadGenericInstantiationThrowing(pTemplateMT->GetModule(),
                                                            pTemplateMT->GetCl(),
                                                            pMD->GetExactClassInstantiation(pMT)).AsMethodTable();
    }
    else
    {
        pMT = pTemplateMT;
    }

    pMT->CheckRestore();
    pMT->EnsureInstanceActive();
    pMT->CheckRunClassInitThrowing();
    HELPER_METHOD_FRAME_END();
}
HCIMPLEND


//========================================================================
//
//      SHARED STATIC FIELD HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>

HCIMPL1(void*, JIT_GetNonGCStaticBase_Portable, MethodTable* pMT)
{
    FCALL_CONTRACT;

    if (pMT->IsClassInited())
    {
        return pMT->GetDynamicStaticsInfo()->m_pNonGCStatics;
    }

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL1(JIT_GetNonGCStaticBase_Helper, pMT);
}
HCIMPLEND


HCIMPL1(void*, JIT_GetDynamicNonGCStaticBase_Portable, DynamicStaticsInfo* pStaticsInfo)
{
    FCALL_CONTRACT;

    if (pStaticsInfo->IsClassInited())
    {
        return pStaticsInfo->m_pNonGCStatics;
    }

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL1(JIT_GetNonGCStaticBase_Helper, pStaticsInfo->GetMethodTable());
}
HCIMPLEND
// No constructor version of JIT_GetSharedNonGCStaticBase.  Does not check if class has
// been initialized.
HCIMPL1(void*, JIT_GetNonGCStaticBaseNoCtor_Portable, MethodTable* pMT)
{
    FCALL_CONTRACT;

    return pMT->GetDynamicStaticsInfo()->m_pNonGCStatics;
}
HCIMPLEND

// No constructor version of JIT_GetSharedNonGCStaticBase.  Does not check if class has
// been initialized.
HCIMPL1(void*, JIT_GetDynamicNonGCStaticBaseNoCtor_Portable, DynamicStaticsInfo* pDynamicStaticsInfo)
{
    FCALL_CONTRACT;

    return pDynamicStaticsInfo->m_pNonGCStatics;
}
HCIMPLEND

HCIMPL1(void*, JIT_GetGCStaticBase_Portable, MethodTable* pMT)
{
    FCALL_CONTRACT;

    if (pMT->IsClassInited())
    {
        return pMT->GetDynamicStaticsInfo()->m_pGCStatics;
    }

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL1(JIT_GetGCStaticBase_Helper, pMT);
}
HCIMPLEND

HCIMPL1(void*, JIT_GetDynamicGCStaticBase_Portable, DynamicStaticsInfo* pStaticsInfo)
{
    FCALL_CONTRACT;

    if (pStaticsInfo->IsClassInited())
    {
        return pStaticsInfo->m_pGCStatics;
    }

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL1(JIT_GetGCStaticBase_Helper, pStaticsInfo->GetMethodTable());
}
HCIMPLEND

// No constructor version of JIT_GetSharedGCStaticBase.  Does not check if class has been
// initialized.
HCIMPL1(void*, JIT_GetGCStaticBaseNoCtor_Portable, MethodTable* pMT)
{
    FCALL_CONTRACT;

    return pMT->GetDynamicStaticsInfo()->m_pGCStatics;
}
HCIMPLEND

// No constructor version of JIT_GetSharedGCStaticBase.  Does not check if class has been
// initialized.
HCIMPL1(void*, JIT_GetDynamicGCStaticBaseNoCtor_Portable, DynamicStaticsInfo* pDynamicStaticsInfo)
{
    FCALL_CONTRACT;

    return pDynamicStaticsInfo->m_pGCStatics;
}
HCIMPLEND

#include <optdefault.h>


// The following two functions can be tail called from platform dependent versions of
// JIT_GetSharedGCStaticBase and JIT_GetShareNonGCStaticBase
HCIMPL1(void*, JIT_GetNonGCStaticBase_Helper, MethodTable* pMT)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    PREFIX_ASSUME(pMT != NULL);
    pMT->CheckRunClassInitThrowing();
    HELPER_METHOD_FRAME_END();

    return (void*)pMT->GetDynamicStaticsInfo()->m_pNonGCStatics;
}
HCIMPLEND

HCIMPL1(void*, JIT_GetGCStaticBase_Helper, MethodTable* pMT)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    PREFIX_ASSUME(pMT != NULL);
    pMT->CheckRunClassInitThrowing();
    HELPER_METHOD_FRAME_END();

    return (void*)pMT->GetDynamicStaticsInfo()->m_pGCStatics;
}
HCIMPLEND

//========================================================================
//
//      THREAD STATIC FIELD HELPERS
//
//========================================================================


// *** These framed helpers get called if allocation needs to occur or
//     if the class constructor needs to run

HCIMPL1(void*, JIT_GetNonGCThreadStaticBase_Helper, MethodTable * pMT)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMT));
    } CONTRACTL_END;

    void* base = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    // Check if the class constructor needs to be run
    pMT->CheckRunClassInitThrowing();

    // Lookup the non-GC statics base pointer
    base = (void*) pMT->GetNonGCThreadStaticsBasePointer();
    CONSISTENCY_CHECK(base != NULL);

    HELPER_METHOD_FRAME_END();

    return base;
}
HCIMPLEND

HCIMPL1(void*, JIT_GetGCThreadStaticBase_Helper, MethodTable * pMT)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMT));
    } CONTRACTL_END;

    void* base = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    // Check if the class constructor needs to be run
    pMT->CheckRunClassInitThrowing();

    // Lookup the GC statics base pointer
    base = (void*) pMT->GetGCThreadStaticsBasePointer();
    CONSISTENCY_CHECK(base != NULL);

    HELPER_METHOD_FRAME_END();

    return base;
}
HCIMPLEND

// *** This helper corresponds to both CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE and
//     CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR. Even though we always check
//     if the class constructor has been run, we have a separate helper ID for the "no ctor"
//     version because it allows the JIT to do some reordering that otherwise wouldn't be
//     possible.

#include <optsmallperfcritical.h>
HCIMPL1(void*, JIT_GetNonGCThreadStaticBase, MethodTable *pMT)
{
    FCALL_CONTRACT;

    void* pThreadStaticBase = GetThreadLocalStaticBaseIfExistsAndInitialized(pMT->GetThreadStaticsInfo()->NonGCTlsIndex);
    if (pThreadStaticBase != NULL)
    {
        return pThreadStaticBase;
    }

    ENDFORBIDGC();
    return HCCALL1(JIT_GetNonGCThreadStaticBase_Helper, pMT);
}
HCIMPLEND

HCIMPL1(void*, JIT_GetDynamicNonGCThreadStaticBase, ThreadStaticsInfo *pThreadStaticsInfo)
{
    FCALL_CONTRACT;

    void* pThreadStaticBase = GetThreadLocalStaticBaseIfExistsAndInitialized(pThreadStaticsInfo->NonGCTlsIndex);
    if (pThreadStaticBase != NULL)
    {
        return pThreadStaticBase;
    }

    ENDFORBIDGC();
    return HCCALL1(JIT_GetNonGCThreadStaticBase_Helper, pThreadStaticsInfo->m_genericStatics.m_DynamicStatics.GetMethodTable());
}
HCIMPLEND

// *** This helper corresponds CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED.
//      Even though we always check if the class constructor has been run, we have a separate
//      helper ID for the "no ctor" version because it allows the JIT to do some reordering that
//      otherwise wouldn't be possible.
HCIMPL1(void*, JIT_GetNonGCThreadStaticBaseOptimized, UINT32 staticBlockIndex)
{
    void* staticBlock = nullptr;

    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame
    TLSIndex tlsIndex(staticBlockIndex);
    // Check if the class constructor needs to be run
    MethodTable *pMT = LookupMethodTableForThreadStatic(tlsIndex);
    pMT->CheckRunClassInitThrowing();

    // Lookup the non-GC statics base pointer
    staticBlock = (void*) pMT->GetNonGCThreadStaticsBasePointer();
    HELPER_METHOD_FRAME_END();

    return staticBlock;
}
HCIMPLEND

#include <optdefault.h>

// *** This helper corresponds to both CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE and
//     CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR. Even though we always check
//     if the class constructor has been run, we have a separate helper ID for the "no ctor"
//     version because it allows the JIT to do some reordering that otherwise wouldn't be
//     possible.

#include <optsmallperfcritical.h>
HCIMPL1(void*, JIT_GetGCThreadStaticBase, MethodTable *pMT)
{
    FCALL_CONTRACT;

    void* pThreadStaticBase = GetThreadLocalStaticBaseIfExistsAndInitialized(pMT->GetThreadStaticsInfo()->GCTlsIndex);
    if (pThreadStaticBase != NULL)
    {
        return pThreadStaticBase;
    }

    ENDFORBIDGC();
    return HCCALL1(JIT_GetGCThreadStaticBase_Helper, pMT);
}
HCIMPLEND

HCIMPL1(void*, JIT_GetDynamicGCThreadStaticBase, ThreadStaticsInfo *pThreadStaticsInfo)
{
    FCALL_CONTRACT;

    void* pThreadStaticBase = GetThreadLocalStaticBaseIfExistsAndInitialized(pThreadStaticsInfo->GCTlsIndex);
    if (pThreadStaticBase != NULL)
    {
        return pThreadStaticBase;
    }

    ENDFORBIDGC();
    return HCCALL1(JIT_GetGCThreadStaticBase_Helper, pThreadStaticsInfo->m_genericStatics.m_DynamicStatics.GetMethodTable());
}
HCIMPLEND

#include <optdefault.h>

// *** This helper corresponds CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED.
//      Even though we always check if the class constructor has been run, we have a separate
//      helper ID for the "no ctor" version because it allows the JIT to do some reordering that
//      otherwise wouldn't be possible.
HCIMPL1(void*, JIT_GetGCThreadStaticBaseOptimized, UINT32 staticBlockIndex)
{
    void* staticBlock = nullptr;

    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    TLSIndex tlsIndex(staticBlockIndex);
    // Check if the class constructor needs to be run
    MethodTable *pMT = LookupMethodTableForThreadStatic(tlsIndex);
    pMT->CheckRunClassInitThrowing();

    // Lookup the non-GC statics base pointer
    staticBlock = (void*) pMT->GetGCThreadStaticsBasePointer();
    HELPER_METHOD_FRAME_END();

    return staticBlock;
}
HCIMPLEND

//========================================================================
//
//      STATIC FIELD DYNAMIC HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>
HCIMPL1_RAW(TADDR, JIT_StaticFieldAddress_Dynamic, StaticFieldAddressArgs * pArgs)
{
    FCALL_CONTRACT;

    TADDR base = HCCALL1(pArgs->staticBaseHelper, pArgs->arg0);
    return base + pArgs->offset;
}
HCIMPLEND_RAW
#include <optdefault.h>

#include <optsmallperfcritical.h>
HCIMPL1_RAW(TADDR, JIT_StaticFieldAddressUnbox_Dynamic, StaticFieldAddressArgs * pArgs)
{
    FCALL_CONTRACT;

    TADDR base = HCCALL1(pArgs->staticBaseHelper, pArgs->arg0);
    return *(TADDR *)(base + pArgs->offset) + Object::GetOffsetOfFirstField();
}
HCIMPLEND_RAW
#include <optdefault.h>

//========================================================================
//
//      CASTING HELPERS
//
//========================================================================

TypeHandle::CastResult STDCALL ObjIsInstanceOfCached(Object *pObject, TypeHandle toTypeHnd)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pObject));
    } CONTRACTL_END;

    MethodTable* pMT = pObject->GetMethodTable();
    return CastCache::TryGetFromCache(pMT, toTypeHnd);
}

BOOL ObjIsInstanceOfCore(Object *pObject, TypeHandle toTypeHnd, BOOL throwCastException)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pObject));
    } CONTRACTL_END;

    BOOL fCast = FALSE;
    MethodTable* pMT = pObject->GetMethodTable();

    OBJECTREF obj = ObjectToOBJECTREF(pObject);
    GCPROTECT_BEGIN(obj);

    // we check nullable case first because it is not cacheable.
    // object castability and type castability disagree on T --> Nullable<T>,
    // so we can't put this in the cache
    if (Nullable::IsNullableForType(toTypeHnd, pMT))
    {
        // allow an object of type T to be cast to Nullable<T> (they have the same representation)
        fCast = TRUE;
    }
    else if (toTypeHnd.IsTypeDesc())
    {
        CastCache::TryAddToCache(pMT, toTypeHnd, FALSE);
        fCast = FALSE;
    }
    else if (pMT->CanCastTo(toTypeHnd.AsMethodTable(), /* pVisited */ NULL))
    {
        fCast = TRUE;
    }
    else if (toTypeHnd.IsInterface())
    {
#ifdef FEATURE_COMINTEROP
        // If we are casting a COM object from interface then we need to do a check to see
        // if it implements the interface.
        if (pMT->IsComObjectType())
        {
            fCast = ComObject::SupportsInterface(obj, toTypeHnd.AsMethodTable());
        }
        else
#endif // FEATURE_COMINTEROP
#ifdef FEATURE_ICASTABLE
        // If type implements ICastable interface we give it a chance to tell us if it can be casted
        // to a given type.
        if (pMT->IsICastable())
        {
            // Make actual call to ICastableHelpers.IsInstanceOfInterface(obj, interfaceTypeObj, out exception)
            OBJECTREF exception = NULL;
            GCPROTECT_BEGIN(exception);

            PREPARE_NONVIRTUAL_CALLSITE(METHOD__ICASTABLEHELPERS__ISINSTANCEOF);

            OBJECTREF managedType = toTypeHnd.GetManagedClassObject(); //GC triggers

            DECLARE_ARGHOLDER_ARRAY(args, 3);
            args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(obj);
            args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(managedType);
            args[ARGNUM_2] = PTR_TO_ARGHOLDER(&exception);

            CALL_MANAGED_METHOD(fCast, BOOL, args);
            INDEBUG(managedType = NULL); // managedType isn't protected during the call

            if (!fCast && throwCastException && exception != NULL)
            {
                RealCOMPlusThrow(exception);
            }
            GCPROTECT_END(); //exception
        }
        else
#endif // FEATURE_ICASTABLE
        if (pMT->IsIDynamicInterfaceCastable())
        {
            fCast = DynamicInterfaceCastable::IsInstanceOf(&obj, toTypeHnd, throwCastException);
        }
    }

    if (!fCast && throwCastException)
    {
        COMPlusThrowInvalidCastException(&obj, toTypeHnd);
    }

    GCPROTECT_END(); // obj

    return(fCast);
}

BOOL ObjIsInstanceOf(Object* pObject, TypeHandle toTypeHnd, BOOL throwCastException)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pObject));
    } CONTRACTL_END;

    MethodTable* pMT = pObject->GetMethodTable();
    TypeHandle::CastResult result = CastCache::TryGetFromCache(pMT, toTypeHnd);

    if (result == TypeHandle::CanCast ||
        (result == TypeHandle::CannotCast && !throwCastException))
    {
        return (BOOL)result;
    }

    return ObjIsInstanceOfCore(pObject, toTypeHnd, throwCastException);
}

HCIMPL2(Object*, ChkCastAny_NoCacheLookup, CORINFO_CLASS_HANDLE type, Object* obj)
{
    FCALL_CONTRACT;

    // This case should be handled by frameless helper
    _ASSERTE(obj != NULL);

    OBJECTREF oref = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(oref);

    TypeHandle clsHnd(type);

    HELPER_METHOD_FRAME_BEGIN_RET_1(oref);
    if (!ObjIsInstanceOfCore(OBJECTREFToObject(oref), clsHnd, TRUE))
    {
        UNREACHABLE(); //ObjIsInstanceOf will throw if cast can't be done
    }
    HELPER_METHOD_POLL();
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(oref);
}
HCIMPLEND

HCIMPL2(Object*, IsInstanceOfAny_NoCacheLookup, CORINFO_CLASS_HANDLE type, Object* obj)
{
    FCALL_CONTRACT;

    // This case should be handled by frameless helper
    _ASSERTE(obj != NULL);

    OBJECTREF oref = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(oref);

    TypeHandle clsHnd(type);

    HELPER_METHOD_FRAME_BEGIN_RET_1(oref);
    if (!ObjIsInstanceOfCore(OBJECTREFToObject(oref), clsHnd))
        oref = NULL;
    HELPER_METHOD_POLL();
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(oref);
}
HCIMPLEND

//========================================================================
//
//      ALLOCATION HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>

//*************************************************************
// Allocation fast path for typical objects
//
HCIMPL1(Object*, JIT_NewS_MP_FastPortable, CORINFO_CLASS_HANDLE typeHnd_)
{
    FCALL_CONTRACT;

    do
    {
        _ASSERTE(GCHeapUtilities::UseThreadAllocationContexts());

        // This is typically the only call in the fast path. Making the call early seems to be better, as it allows the compiler
        // to use volatile registers for intermediate values. This reduces the number of push/pop instructions and eliminates
        // some reshuffling of intermediate values into nonvolatile registers around the call.
        Thread *thread = GetThread();

        TypeHandle typeHandle(typeHnd_);
        _ASSERTE(!typeHandle.IsTypeDesc()); // heap objects must have method tables
        MethodTable *methodTable = typeHandle.AsMethodTable();

        SIZE_T size = methodTable->GetBaseSize();
        _ASSERTE(size % DATA_ALIGNMENT == 0);

        gc_alloc_context *allocContext = thread->GetAllocContext();
        BYTE *allocPtr = allocContext->alloc_ptr;
        _ASSERTE(allocPtr <= allocContext->alloc_limit);
        if (size > static_cast<SIZE_T>(allocContext->alloc_limit - allocPtr))
        {
            break;
        }
        allocContext->alloc_ptr = allocPtr + size;

        _ASSERTE(allocPtr != nullptr);
        Object *object = reinterpret_cast<Object *>(allocPtr);
        _ASSERTE(object->HasEmptySyncBlockInfo());
        object->SetMethodTable(methodTable);

        return object;
    } while (false);

    // Tail call to the slow helper
    ENDFORBIDGC();
    return HCCALL1(JIT_New, typeHnd_);
}
HCIMPLEND

#include <optdefault.h>

/*************************************************************/
HCIMPL1(Object*, JIT_New, CORINFO_CLASS_HANDLE typeHnd_)
{
    FCALL_CONTRACT;

    OBJECTREF newobj = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    TypeHandle typeHnd(typeHnd_);

    _ASSERTE(!typeHnd.IsTypeDesc());  // heap objects must have method tables
    MethodTable *pMT = typeHnd.AsMethodTable();

#ifdef _DEBUG
    if (g_pConfig->FastGCStressLevel()) {
        GetThread()->DisableStressHeap();
    }
#endif // _DEBUG

    newobj = AllocateObject(pMT);

    HELPER_METHOD_FRAME_END();
    return(OBJECTREFToObject(newobj));
}
HCIMPLEND

/*************************************************************/
HCIMPL1(Object*, JIT_NewMaybeFrozen, CORINFO_CLASS_HANDLE typeHnd_)
{
    FCALL_CONTRACT;

    OBJECTREF newobj = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    TypeHandle typeHnd(typeHnd_);

    _ASSERTE(!typeHnd.IsTypeDesc());  // heap objects must have method tables
    MethodTable* pMT = typeHnd.AsMethodTable();

#ifdef _DEBUG
    if (g_pConfig->FastGCStressLevel()) {
        GetThread()->DisableStressHeap();
    }
#endif // _DEBUG

    newobj = TryAllocateFrozenObject(pMT);
    if (newobj == NULL)
    {
        // Fallback to normal heap allocation.
        newobj = AllocateObject(pMT);
    }

    HELPER_METHOD_FRAME_END();
    return(OBJECTREFToObject(newobj));
}
HCIMPLEND


//========================================================================
//
//      STRING HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>

//*************************************************************
// Allocation fast path for typical objects
//
HCIMPL1(StringObject*, AllocateString_MP_FastPortable, DWORD stringLength)
{
    FCALL_CONTRACT;

    do
    {
        _ASSERTE(GCHeapUtilities::UseThreadAllocationContexts());

        // Instead of doing elaborate overflow checks, we just limit the number of elements. This will avoid all overflow
        // problems, as well as making sure big string objects are correctly allocated in the big object heap.
        if (stringLength >= (LARGE_OBJECT_SIZE - 256) / sizeof(WCHAR))
        {
            break;
        }

        // This is typically the only call in the fast path. Making the call early seems to be better, as it allows the compiler
        // to use volatile registers for intermediate values. This reduces the number of push/pop instructions and eliminates
        // some reshuffling of intermediate values into nonvolatile registers around the call.
        Thread *thread = GetThread();

        SIZE_T totalSize = StringObject::GetSize(stringLength);

        // The method table's base size includes space for a terminating null character
        _ASSERTE(totalSize >= g_pStringClass->GetBaseSize());
        _ASSERTE((totalSize - g_pStringClass->GetBaseSize()) / sizeof(WCHAR) == stringLength);

        SIZE_T alignedTotalSize = ALIGN_UP(totalSize, DATA_ALIGNMENT);
        _ASSERTE(alignedTotalSize >= totalSize);
        totalSize = alignedTotalSize;

        gc_alloc_context *allocContext = thread->GetAllocContext();
        BYTE *allocPtr = allocContext->alloc_ptr;
        _ASSERTE(allocPtr <= allocContext->alloc_limit);
        if (totalSize > static_cast<SIZE_T>(allocContext->alloc_limit - allocPtr))
        {
            break;
        }
        allocContext->alloc_ptr = allocPtr + totalSize;

        _ASSERTE(allocPtr != nullptr);
        StringObject *stringObject = reinterpret_cast<StringObject *>(allocPtr);
        stringObject->SetMethodTable(g_pStringClass);
        stringObject->SetStringLength(stringLength);
        _ASSERTE(stringObject->GetBuffer()[stringLength] == W('\0'));

        return stringObject;
    } while (false);

    // Tail call to the slow helper
    ENDFORBIDGC();
    return HCCALL1(FramedAllocateString, stringLength);
}
HCIMPLEND

#include <optdefault.h>

/*********************************************************************/
/* We don't use HCIMPL macros because this is not a real helper call */
/* This function just needs mangled arguments like a helper call     */

HCIMPL1_RAW(StringObject*, UnframedAllocateString, DWORD stringLength)
{
    // This isn't _really_ an FCALL and therefore shouldn't have the
    // SO_TOLERANT part of the FCALL_CONTRACT b/c it is not entered
    // from managed code.
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    STRINGREF result;
    result = AllocateString(stringLength);

    return((StringObject*) OBJECTREFToObject(result));
}
HCIMPLEND_RAW

HCIMPL1(StringObject*, FramedAllocateString, DWORD stringLength)
{
    FCALL_CONTRACT;

    STRINGREF result = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    result = AllocateString(stringLength);

    HELPER_METHOD_FRAME_END();
    return((StringObject*) OBJECTREFToObject(result));
}
HCIMPLEND

/*********************************************************************/
OBJECTHANDLE ConstructStringLiteral(CORINFO_MODULE_HANDLE scopeHnd, mdToken metaTok, void** ppPinnedString)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    _ASSERTE(TypeFromToken(metaTok) == mdtString);

    Module* module = GetModule(scopeHnd);
    return module->ResolveStringRef(metaTok, ppPinnedString);
}

/*********************************************************************/
HCIMPL2(Object *, JIT_StrCns, unsigned rid, CORINFO_MODULE_HANDLE scopeHnd)
{
    FCALL_CONTRACT;

    OBJECTHANDLE hndStr = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    // Retrieve the handle to the COM+ string object.
    hndStr = ConstructStringLiteral(scopeHnd, RidToToken(rid, mdtString));
    HELPER_METHOD_FRAME_END();

    // Don't use ObjectFromHandle; this isn't a real handle
    return *(Object**)hndStr;
}
HCIMPLEND


//========================================================================
//
//      ARRAY HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>

//*************************************************************
// Array allocation fast path for arrays of value type elements
//
HCIMPL2(Object*, JIT_NewArr1VC_MP_FastPortable, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size)
{
    FCALL_CONTRACT;

    do
    {
        _ASSERTE(GCHeapUtilities::UseThreadAllocationContexts());

        // Do a conservative check here.  This is to avoid overflow while doing the calculations.  We don't
        // have to worry about "large" objects, since the allocation quantum is never big enough for
        // LARGE_OBJECT_SIZE.
        //
        // For Value Classes, this needs to be 2^16 - slack (2^32 / max component size),
        // The slack includes the size for the array header and round-up ; for alignment.  Use 256 for the
        // slack value out of laziness.
        SIZE_T componentCount = static_cast<SIZE_T>(size);
        if (componentCount >= static_cast<SIZE_T>(65535 - 256))
        {
            break;
        }

        // This is typically the only call in the fast path. Making the call early seems to be better, as it allows the compiler
        // to use volatile registers for intermediate values. This reduces the number of push/pop instructions and eliminates
        // some reshuffling of intermediate values into nonvolatile registers around the call.
        Thread *thread = GetThread();

        MethodTable *pArrayMT = (MethodTable *)arrayMT;

        _ASSERTE(pArrayMT->HasComponentSize());
        SIZE_T componentSize = pArrayMT->RawGetComponentSize();
        SIZE_T totalSize = componentCount * componentSize;
        _ASSERTE(totalSize / componentSize == componentCount);

        SIZE_T baseSize = pArrayMT->GetBaseSize();
        totalSize += baseSize;
        _ASSERTE(totalSize >= baseSize);

        SIZE_T alignedTotalSize = ALIGN_UP(totalSize, DATA_ALIGNMENT);
        _ASSERTE(alignedTotalSize >= totalSize);
        totalSize = alignedTotalSize;

        gc_alloc_context *allocContext = thread->GetAllocContext();
        BYTE *allocPtr = allocContext->alloc_ptr;
        _ASSERTE(allocPtr <= allocContext->alloc_limit);
        if (totalSize > static_cast<SIZE_T>(allocContext->alloc_limit - allocPtr))
        {
            break;
        }
        allocContext->alloc_ptr = allocPtr + totalSize;

        _ASSERTE(allocPtr != nullptr);
        ArrayBase *array = reinterpret_cast<ArrayBase *>(allocPtr);
        array->SetMethodTable(pArrayMT);
        _ASSERTE(static_cast<DWORD>(componentCount) == componentCount);
        array->m_NumComponents = static_cast<DWORD>(componentCount);

        return array;
    } while (false);

    // Tail call to the slow helper
    ENDFORBIDGC();
    return HCCALL2(JIT_NewArr1, arrayMT, size);
}
HCIMPLEND

//*************************************************************
// Array allocation fast path for arrays of object elements
//
HCIMPL2(Object*, JIT_NewArr1OBJ_MP_FastPortable, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size)
{
    FCALL_CONTRACT;

    do
    {
        _ASSERTE(GCHeapUtilities::UseThreadAllocationContexts());

        // Make sure that the total size cannot reach LARGE_OBJECT_SIZE, which also allows us to avoid overflow checks. The
        // "256" slack is to cover the array header size and round-up, using a constant value here out of laziness.
        SIZE_T componentCount = static_cast<SIZE_T>(size);
        if (componentCount >= static_cast<SIZE_T>((LARGE_OBJECT_SIZE - 256) / sizeof(void *)))
        {
            break;
        }

        // This is typically the only call in the fast path. Making the call early seems to be better, as it allows the compiler
        // to use volatile registers for intermediate values. This reduces the number of push/pop instructions and eliminates
        // some reshuffling of intermediate values into nonvolatile registers around the call.
        Thread *thread = GetThread();

        SIZE_T totalSize = componentCount * sizeof(void *);
        _ASSERTE(totalSize / sizeof(void *) == componentCount);

        MethodTable *pArrayMT = (MethodTable *)arrayMT;

        SIZE_T baseSize = pArrayMT->GetBaseSize();
        totalSize += baseSize;
        _ASSERTE(totalSize >= baseSize);

        _ASSERTE(ALIGN_UP(totalSize, DATA_ALIGNMENT) == totalSize);

        gc_alloc_context *allocContext = thread->GetAllocContext();
        BYTE *allocPtr = allocContext->alloc_ptr;
        _ASSERTE(allocPtr <= allocContext->alloc_limit);
        if (totalSize > static_cast<SIZE_T>(allocContext->alloc_limit - allocPtr))
        {
            break;
        }
        allocContext->alloc_ptr = allocPtr + totalSize;

        _ASSERTE(allocPtr != nullptr);
        ArrayBase *array = reinterpret_cast<ArrayBase *>(allocPtr);
        array->SetMethodTable(pArrayMT);
        _ASSERTE(static_cast<DWORD>(componentCount) == componentCount);
        array->m_NumComponents = static_cast<DWORD>(componentCount);

        return array;
    } while (false);

    // Tail call to the slow helper
    ENDFORBIDGC();
    return HCCALL2(JIT_NewArr1, arrayMT, size);
}
HCIMPLEND

#include <optdefault.h>

/*************************************************************/
HCIMPL2(Object*, JIT_NewArr1, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size)
{
    FCALL_CONTRACT;

    OBJECTREF newArray = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    MethodTable *pArrayMT = (MethodTable *)arrayMT;

    _ASSERTE(pArrayMT->IsFullyLoaded());
    _ASSERTE(pArrayMT->IsArray());
    _ASSERTE(!pArrayMT->IsMultiDimArray());

    if (size < 0)
        COMPlusThrow(kOverflowException);

#ifdef HOST_64BIT
    // Even though ECMA allows using a native int as the argument to newarr instruction
    // (therefore size is INT_PTR), ArrayBase::m_NumComponents is 32-bit, so even on 64-bit
    // platforms we can't create an array whose size exceeds 32 bits.
    if (size > INT_MAX)
        EX_THROW(EEMessageException, (kOverflowException, IDS_EE_ARRAY_DIMENSIONS_EXCEEDED));
#endif

#ifdef _DEBUG
    if (g_pConfig->FastGCStressLevel()) {
        GetThread()->DisableStressHeap();
    }
#endif // _DEBUG

    newArray = AllocateSzArray(pArrayMT, (INT32)size);
    HELPER_METHOD_FRAME_END();

    return(OBJECTREFToObject(newArray));
}
HCIMPLEND


/*************************************************************/
HCIMPL2(Object*, JIT_NewArr1MaybeFrozen, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size)
{
    FCALL_CONTRACT;

    OBJECTREF newArray = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    MethodTable* pArrayMT = (MethodTable*)arrayMT;

    _ASSERTE(pArrayMT->IsFullyLoaded());
    _ASSERTE(pArrayMT->IsArray());
    _ASSERTE(!pArrayMT->IsMultiDimArray());

    if (size < 0)
        COMPlusThrow(kOverflowException);

#ifdef HOST_64BIT
    // Even though ECMA allows using a native int as the argument to newarr instruction
    // (therefore size is INT_PTR), ArrayBase::m_NumComponents is 32-bit, so even on 64-bit
    // platforms we can't create an array whose size exceeds 32 bits.
    if (size > INT_MAX)
        EX_THROW(EEMessageException, (kOverflowException, IDS_EE_ARRAY_DIMENSIONS_EXCEEDED));
#endif

#ifdef _DEBUG
    if (g_pConfig->FastGCStressLevel()) {
        GetThread()->DisableStressHeap();
    }
#endif // _DEBUG

    newArray = TryAllocateFrozenSzArray(pArrayMT, (INT32)size);
    if (newArray == NULL)
    {
        // Fallback to default heap allocation
        newArray = AllocateSzArray(pArrayMT, (INT32)size);
    }
    _ASSERTE(newArray != NULL);

    HELPER_METHOD_FRAME_END();

    return(OBJECTREFToObject(newArray));
}
HCIMPLEND

/*************************************************************/
HCIMPL3(Object*, JIT_NewMDArr, CORINFO_CLASS_HANDLE classHnd, unsigned dwNumArgs, INT32 * pArgList)
{
    FCALL_CONTRACT;

    OBJECTREF    ret = 0;
    HELPER_METHOD_FRAME_BEGIN_RET_1(ret);    // Set up a frame

    TypeHandle typeHnd(classHnd);
    typeHnd.CheckRestore();
    _ASSERTE(typeHnd.GetMethodTable()->IsArray());

    ret = AllocateArrayEx(typeHnd, pArgList, dwNumArgs);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(ret);
}
HCIMPLEND

#include <optdefault.h>

//===========================================================================
// This routine is called if the Array store needs a frame constructed
// in order to do the array check.  It should only be called from
// the array store check helpers.

HCIMPL2(LPVOID, ArrayStoreCheck, Object** pElement, PtrArray** pArray)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_2(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, *pElement, *pArray);

    GCStress<cfg_any, EeconfigFastGcSPolicy>::MaybeTrigger();

    // call "Core" version directly since all the callers do the "NoGC" call first and that checks the cache
    if (!ObjIsInstanceOfCore(*pElement, (*pArray)->GetArrayElementTypeHandle()))
        COMPlusThrow(kArrayTypeMismatchException);

    HELPER_METHOD_FRAME_END();

    return (LPVOID)0; // Used to aid epilog walker
}
HCIMPLEND

//========================================================================
//
//      VALUETYPE/BYREF HELPERS
//
//========================================================================

/*************************************************************/
HCIMPL2(Object*, JIT_Box, CORINFO_CLASS_HANDLE type, void* unboxedData)
{
    FCALL_CONTRACT;

    // <TODO>TODO: if we care, we could do a fast trial allocation
    // and avoid the building the frame most times</TODO>
    OBJECTREF newobj = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();    // Set up a frame
    GCPROTECT_BEGININTERIOR(unboxedData);
    HELPER_METHOD_POLL();

    // A null can be passed for boxing of a null ref.
    if (unboxedData == NULL)
        COMPlusThrow(kNullReferenceException);

    TypeHandle clsHnd(type);

    _ASSERTE(!clsHnd.IsTypeDesc());  // boxable types have method tables

    MethodTable *pMT = clsHnd.AsMethodTable();

    pMT->CheckRestore();

    _ASSERTE (pMT->IsValueType() && !pMT->IsByRefLike());

#ifdef _DEBUG
    if (g_pConfig->FastGCStressLevel()) {
        GetThread()->DisableStressHeap();
    }
#endif // _DEBUG

    newobj = pMT->FastBox(&unboxedData);

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();
    return(OBJECTREFToObject(newobj));
}
HCIMPLEND

/*************************************************************/
NOINLINE HCIMPL3(VOID, JIT_Unbox_Nullable_Framed, void * destPtr, MethodTable* typeMT, OBJECTREF objRef)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_1(objRef);
    if (!Nullable::UnBox(destPtr, objRef, typeMT))
    {
        COMPlusThrowInvalidCastException(&objRef, TypeHandle(typeMT));
    }
    HELPER_METHOD_POLL();
    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*************************************************************/
HCIMPL3(VOID, JIT_Unbox_Nullable, void * destPtr, CORINFO_CLASS_HANDLE type, Object* obj)
{
    FCALL_CONTRACT;

    TypeHandle typeHnd(type);
    _ASSERTE(Nullable::IsNullableType(typeHnd));

    MethodTable* typeMT = typeHnd.AsMethodTable();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    if (Nullable::UnBoxNoGC(destPtr, objRef, typeMT))
    {
        // exact match (type equivalence not needed)
        FC_GC_POLL();
        return;
    }

    // Fall back to a framed helper that handles type equivalence.
    ENDFORBIDGC();
    HCCALL3(JIT_Unbox_Nullable_Framed, destPtr, typeMT, objRef);
}
HCIMPLEND

/*************************************************************/
/* framed Unbox helper that handles enums and full-blown type equivalence */
NOINLINE HCIMPL2(LPVOID, Unbox_Helper_Framed, MethodTable* pMT1, Object* obj)
{
    FCALL_CONTRACT;

    LPVOID result = NULL;
    MethodTable* pMT2 = obj->GetMethodTable();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    HELPER_METHOD_FRAME_BEGIN_RET_1(objRef);
    HELPER_METHOD_POLL();

    if (pMT1->GetInternalCorElementType() == pMT2->GetInternalCorElementType() &&
            (pMT1->IsEnum() || pMT1->IsTruePrimitive()) &&
            (pMT2->IsEnum() || pMT2->IsTruePrimitive()))
    {
        // we allow enums and their primitive type to be interchangeable
        result = objRef->GetData();
    }
    else if (pMT1->IsEquivalentTo(pMT2))
    {
        // the structures are equivalent
        result = objRef->GetData();
    }
    else
    {
        COMPlusThrowInvalidCastException(&objRef, TypeHandle(pMT1));
    }
    HELPER_METHOD_FRAME_END();

    return result;
}
HCIMPLEND

/*************************************************************/
/* Unbox helper that handles enums */
HCIMPL2(LPVOID, Unbox_Helper, CORINFO_CLASS_HANDLE type, Object* obj)
{
    FCALL_CONTRACT;

    TypeHandle typeHnd(type);
    // boxable types have method tables
    _ASSERTE(!typeHnd.IsTypeDesc());

    MethodTable* pMT1 = typeHnd.AsMethodTable();
    // must be a value type
    _ASSERTE(pMT1->IsValueType());

    MethodTable* pMT2 = obj->GetMethodTable();

    // we allow enums and their primitive type to be interchangeable.
    // if suspension is requested, defer to the framed helper.
    if (pMT1->GetInternalCorElementType() == pMT2->GetInternalCorElementType() &&
            (pMT1->IsEnum() || pMT1->IsTruePrimitive()) &&
            (pMT2->IsEnum() || pMT2->IsTruePrimitive()) &&
            g_TrapReturningThreads.LoadWithoutBarrier() == 0)
    {
        return obj->GetData();
    }

    // Fall back to a framed helper that can also handle GC suspension and type equivalence.
    ENDFORBIDGC();
    return HCCALL2(Unbox_Helper_Framed, pMT1, obj);
}
HCIMPLEND

/*************************************************************/
HCIMPL2_IV(LPVOID, JIT_GetRefAny, CORINFO_CLASS_HANDLE type, TypedByRef typedByRef)
{
    FCALL_CONTRACT;

    TypeHandle clsHnd(type);
    // <TODO>@TODO right now we check for precisely the correct type.
    // do we want to allow inheritance?  (watch out since value
    // classes inherit from object but do not normal object layout).</TODO>
    if (clsHnd != typedByRef.type) {
        FCThrow(kInvalidCastException);
    }

    return(typedByRef.data);
}
HCIMPLEND


/*************************************************************/
HCIMPL2(BOOL, JIT_IsInstanceOfException, CORINFO_CLASS_HANDLE type, Object* obj)
{
    FCALL_CONTRACT;
    TypeHandle clsHnd(type);
    return ExceptionIsOfRightType(clsHnd, obj->GetTypeHandle());
}
HCIMPLEND


//========================================================================
//
//      GENERICS HELPERS
//
//========================================================================

/***********************************************************************/
// JIT_GenericHandle and its cache
//
// Perform a "polytypic" operation related to shared generic code at runtime, possibly filling in an entry in
// either a generic dictionary cache associated with a descriptor or placing an entry in the global
// JitGenericHandle cache.
//
// A polytypic operation is one such as
//      * new List<T>
//      * castclass List<T>
// where the code being executed is shared generic code. In these cases the outcome of the operation depends
// on the exact value for T, which is acquired from a dynamic parameter.
//
// The actual operation always boils down to finding a "handle" (TypeHandle, MethodDesc, call address,
// dispatch stub address etc.) based on some static information (passed as tokens) and on the exact runtime
// type context (passed as one or two parameters classHnd and methodHnd).
//
// The static information specifies which polytypic operation (and thus which kind of handle) we're
// interested in.
//
// The dynamic information (the type context, i.e. the exact instantiation of class and method type
// parameters is specified in one of two ways:
// * If classHnd is null then the methodHnd should be an exact method descriptor wrapping shared code that
//     satisfies SharedByGenericMethodInstantiations().
//
//     For example:
//         * We may be running the shared code for a generic method instantiation C::m<object>. The methodHnd
//             will carry the exact instantiation, e.g. C::m<string>
//
// * If classHnd is non-null (e.g. a type D<exact>) then:
//     * methodHnd will indicate the representative code being run (which will be
//         !SharedByGenericMethodInstantiations but will be SharedByGenericClassInstantiations). Let's say
//         this code is C<repr>::m().
//     * the type D will be a descendent of type C. In particular D<exact> will relate to some type C<exact'>
//         where C<repr> is the representative instantiation of C<exact>'
//     * the relevant dictionary will be the one attached to C<exact'>.
//
// The JitGenericHandleCache is a global data structure shared across all application domains. It is only
// used if generic dictionaries have overflowed. It is flushed each time an application domain is unloaded.

struct JitGenericHandleCacheKey
{
    JitGenericHandleCacheKey(CORINFO_CLASS_HANDLE classHnd, CORINFO_METHOD_HANDLE methodHnd, void *signature)
    {
        LIMITED_METHOD_CONTRACT;
        m_Data1 = (size_t)classHnd;
        m_Data2 = (size_t)methodHnd;
        m_Data3 = (size_t)signature;
        m_type = 0;
    }

    JitGenericHandleCacheKey(MethodTable* pMT, CORINFO_CLASS_HANDLE classHnd, CORINFO_METHOD_HANDLE methodHnd)
    {
        LIMITED_METHOD_CONTRACT;
        m_Data1 = (size_t)pMT;
        m_Data2 = (size_t)classHnd;
        m_Data3 = (size_t)methodHnd;
        m_type = 1;
    }

    size_t  m_Data1;
    size_t  m_Data2;
    size_t  m_Data3;

    // The type of the entry:
    //  0 - JIT_GenericHandle entry
    //  1 - JIT_VirtualFunctionPointer entry
    unsigned char m_type;
};

class JitGenericHandleCacheTraits
{
public:
    static EEHashEntry_t *AllocateEntry(const JitGenericHandleCacheKey *pKey, BOOL bDeepCopy, AllocationHeap pHeap = 0)
    {
        LIMITED_METHOD_CONTRACT;
        EEHashEntry_t *pEntry = (EEHashEntry_t *) new (nothrow) BYTE[SIZEOF_EEHASH_ENTRY + sizeof(JitGenericHandleCacheKey)];
        if (!pEntry)
            return NULL;
        *((JitGenericHandleCacheKey*)pEntry->Key) = *pKey;
        return pEntry;
    }

    static void DeleteEntry(EEHashEntry_t *pEntry, AllocationHeap pHeap = 0)
    {
        LIMITED_METHOD_CONTRACT;
        delete [] (BYTE*)pEntry;
    }

    static BOOL CompareKeys(EEHashEntry_t *pEntry, const JitGenericHandleCacheKey *e2)
    {
        LIMITED_METHOD_CONTRACT;
        const JitGenericHandleCacheKey *e1 = (const JitGenericHandleCacheKey*)&pEntry->Key;
        return (e1->m_Data1 == e2->m_Data1) && (e1->m_Data2 == e2->m_Data2) && (e1->m_Data3 == e2->m_Data3) &&
            (e1->m_type == e2->m_type);
    }

    static DWORD Hash(const JitGenericHandleCacheKey *k)
    {
        LIMITED_METHOD_CONTRACT;
        return (DWORD)k->m_Data1 + _rotl((DWORD)k->m_Data2,5) + _rotr((DWORD)k->m_Data3,5);
    }

    static const JitGenericHandleCacheKey *GetKey(EEHashEntry_t *pEntry)
    {
        LIMITED_METHOD_CONTRACT;
        return (const JitGenericHandleCacheKey*)&pEntry->Key;
    }
};

typedef EEHashTable<const JitGenericHandleCacheKey *, JitGenericHandleCacheTraits, FALSE> JitGenericHandleCache;

JitGenericHandleCache *g_pJitGenericHandleCache = NULL;    //cache of calls to JIT_GenericHandle
CrstStatic g_pJitGenericHandleCacheCrst;

void AddToGenericHandleCache(JitGenericHandleCacheKey* pKey, HashDatum datum)
{
     CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pKey));
        PRECONDITION(CheckPointer(datum));
    } CONTRACTL_END;

    EX_TRY
    {
        GCX_COOP();

        CrstHolder lock(&g_pJitGenericHandleCacheCrst);

        HashDatum entry;
        if (!g_pJitGenericHandleCache->GetValue(pKey,&entry))
            g_pJitGenericHandleCache->InsertValue(pKey,datum);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)  // Swallow OOM
}

/* static */
void ClearJitGenericHandleCache()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;


    // We call this on every ALC unload, because entries in the cache might include
    // pointers into the ALC being unloaded.  We would prefer to
    // only flush entries that have that are no longer valid, but the entries don't yet contain
    // enough information to do that.  However everything in the cache can be found again by calling
    // loader functions, and the total number of entries in the cache is typically very small (indeed
    // normally the cache is not used at all - it is only used when the generic dictionaries overflow).
    if (g_pJitGenericHandleCache)
    {
        // It's not necessary to take the lock here because this function should only be called when EE is suspended,
        // the lock is only taken to fulfill the threadsafety check and to be consistent. If the lock becomes a problem, we
        // could put it in a "ifdef _DEBUG" block
        CrstHolder lock(&g_pJitGenericHandleCacheCrst);
        EEHashTableIteration iter;
        g_pJitGenericHandleCache->IterateStart(&iter);
        BOOL keepGoing = g_pJitGenericHandleCache->IterateNext(&iter);
        while(keepGoing)
        {
            const JitGenericHandleCacheKey *key = g_pJitGenericHandleCache->IterateGetKey(&iter);
            // Advance the iterator before we delete!!  See notes in EEHash.h
            keepGoing = g_pJitGenericHandleCache->IterateNext(&iter);
            g_pJitGenericHandleCache->DeleteValue(key);
        }
    }
}

// Factored out most of the body of JIT_GenericHandle so it could be called easily from the CER reliability code to pre-populate the
// cache.
CORINFO_GENERIC_HANDLE JIT_GenericHandleWorker(MethodDesc * pMD, MethodTable * pMT, LPVOID signature, DWORD dictionaryIndexAndSlot, Module* pModule)
{
     CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

     uint32_t dictionaryIndex = 0;
     MethodTable * pDeclaringMT = NULL;

    if (pMT != NULL)
    {
        if (pModule != NULL)
        {
#ifdef _DEBUG
            // Only in R2R mode are the module, dictionary index and dictionary slot provided as an input
            _ASSERTE(dictionaryIndexAndSlot != (DWORD)-1);
            _ASSERT(ReadyToRunInfo::IsNativeImageSharedBy(pModule, ExecutionManager::FindReadyToRunModule(dac_cast<TADDR>(signature))));
#endif
            dictionaryIndex = (dictionaryIndexAndSlot >> 16);
        }
        else
        {
            SigPointer ptr((PCCOR_SIGNATURE)signature);

            uint32_t kind; // DictionaryEntryKind
            IfFailThrow(ptr.GetData(&kind));

            // We need to normalize the class passed in (if any) for reliability purposes. That's because preparation of a code region that
            // contains these handle lookups depends on being able to predict exactly which lookups are required (so we can pre-cache the
            // answers and remove any possibility of failure at runtime). This is hard to do if the lookup (in this case the lookup of the
            // dictionary overflow cache) is keyed off the somewhat arbitrary type of the instance on which the call is made (we'd need to
            // prepare for every possible derived type of the type containing the method). So instead we have to locate the exactly
            // instantiated (non-shared) super-type of the class passed in.

            _ASSERTE(dictionaryIndexAndSlot == (DWORD)-1);
            IfFailThrow(ptr.GetData(&dictionaryIndex));
        }

        pDeclaringMT = pMT;
        while (true)
        {
            MethodTable * pParentMT = pDeclaringMT->GetParentMethodTable();
            if (pParentMT->GetNumDicts() <= dictionaryIndex)
                break;
            pDeclaringMT = pParentMT;
        }

        if (pDeclaringMT != pMT)
        {
            JitGenericHandleCacheKey key((CORINFO_CLASS_HANDLE)pDeclaringMT, NULL, signature);
            HashDatum res;
            if (g_pJitGenericHandleCache->GetValue(&key,&res))
            {
                // Add the denormalized key for faster lookup next time. This is not a critical entry - no need
                // to specify appdomain affinity.
                JitGenericHandleCacheKey denormKey((CORINFO_CLASS_HANDLE)pMT, NULL, signature);
                AddToGenericHandleCache(&denormKey, res);
                return (CORINFO_GENERIC_HANDLE) (DictionaryEntry) res;
            }
        }
    }

    DictionaryEntry * pSlot;
    CORINFO_GENERIC_HANDLE result = (CORINFO_GENERIC_HANDLE)Dictionary::PopulateEntry(pMD, pDeclaringMT, signature, FALSE, &pSlot, dictionaryIndexAndSlot, pModule);

    if (pSlot == NULL)
    {
        // If we've overflowed the dictionary write the result to the cache.
        // Add the normalized key (pDeclaringMT) here so that future lookups of any
        // inherited types are faster next time rather than just just for this specific pMT.
        JitGenericHandleCacheKey key((CORINFO_CLASS_HANDLE)pDeclaringMT, (CORINFO_METHOD_HANDLE)pMD, signature);
        AddToGenericHandleCache(&key, (HashDatum)result);
    }

    if (pMT != NULL && pDeclaringMT != pMT)
    {
        // If the dictionary on the base type got expanded, update the current type's base type dictionary
        // pointer to use the new one on the base type.

        Dictionary* pMTDictionary = pMT->GetPerInstInfo()[dictionaryIndex];
        Dictionary* pDeclaringMTDictionary = pDeclaringMT->GetPerInstInfo()[dictionaryIndex];
        if (pMTDictionary != pDeclaringMTDictionary)
        {
            TypeHandle** pPerInstInfo = (TypeHandle**)pMT->GetPerInstInfo();
            InterlockedExchangeT(pPerInstInfo + dictionaryIndex, (TypeHandle*)pDeclaringMTDictionary);
        }
    }

    return result;
} // JIT_GenericHandleWorker

/*********************************************************************/
// slow helper to tail call from the fast one
NOINLINE HCIMPL5(CORINFO_GENERIC_HANDLE, JIT_GenericHandle_Framed,
        CORINFO_CLASS_HANDLE classHnd,
        CORINFO_METHOD_HANDLE methodHnd,
        LPVOID signature,
        DWORD dictionaryIndexAndSlot,
        CORINFO_MODULE_HANDLE moduleHnd)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(classHnd != NULL || methodHnd != NULL);
        PRECONDITION(classHnd == NULL || methodHnd == NULL);
    } CONTRACTL_END;

    // Result is a generic handle (in fact, a CORINFO_CLASS_HANDLE, CORINFO_METHOD_HANDLE, or a code pointer)
    CORINFO_GENERIC_HANDLE result = NULL;

    MethodDesc * pMD = GetMethod(methodHnd);
    MethodTable * pMT = TypeHandle(classHnd).AsMethodTable();
    Module * pModule = GetModule(moduleHnd);

    // Set up a frame
    HELPER_METHOD_FRAME_BEGIN_RET_0();

    result = JIT_GenericHandleWorker(pMD, pMT, signature, dictionaryIndexAndSlot, pModule);

    HELPER_METHOD_FRAME_END();

    _ASSERTE(result != NULL);

    // Return the handle
    return result;
}
HCIMPLEND

/*********************************************************************/
#include <optsmallperfcritical.h>
HCIMPL2(CORINFO_GENERIC_HANDLE, JIT_GenericHandleMethod, CORINFO_METHOD_HANDLE  methodHnd, LPVOID signature)
{
     CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(methodHnd));
        PRECONDITION(CheckPointer(signature));
    } CONTRACTL_END;

    JitGenericHandleCacheKey key(NULL, methodHnd, signature);
    HashDatum res;
    if (g_pJitGenericHandleCache->GetValueSpeculative(&key,&res))
        return (CORINFO_GENERIC_HANDLE) (DictionaryEntry) res;

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL5(JIT_GenericHandle_Framed, NULL, methodHnd, signature, -1, NULL);
}
HCIMPLEND

HCIMPL2(CORINFO_GENERIC_HANDLE, JIT_GenericHandleMethodWithSlotAndModule, CORINFO_METHOD_HANDLE  methodHnd, GenericHandleArgs * pArgs)
{
    CONTRACTL{
        FCALL_CHECK;
        PRECONDITION(CheckPointer(methodHnd));
        PRECONDITION(CheckPointer(pArgs));
    } CONTRACTL_END;

    JitGenericHandleCacheKey key(NULL, methodHnd, pArgs->signature);
    HashDatum res;
    if (g_pJitGenericHandleCache->GetValueSpeculative(&key, &res))
        return (CORINFO_GENERIC_HANDLE)(DictionaryEntry)res;

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL5(JIT_GenericHandle_Framed, NULL, methodHnd, pArgs->signature, pArgs->dictionaryIndexAndSlot, pArgs->module);
}
HCIMPLEND
#include <optdefault.h>

/*********************************************************************/
HCIMPL2(CORINFO_GENERIC_HANDLE, JIT_GenericHandleMethodLogging, CORINFO_METHOD_HANDLE  methodHnd, LPVOID signature)
{
     CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(methodHnd));
        PRECONDITION(CheckPointer(signature));
    } CONTRACTL_END;

    JitGenericHandleCacheKey key(NULL, methodHnd, signature);
    HashDatum res;
    if (g_pJitGenericHandleCache->GetValueSpeculative(&key,&res))
        return (CORINFO_GENERIC_HANDLE) (DictionaryEntry) res;

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL5(JIT_GenericHandle_Framed, NULL, methodHnd, signature, -1, NULL);
}
HCIMPLEND

/*********************************************************************/
#include <optsmallperfcritical.h>
HCIMPL2(CORINFO_GENERIC_HANDLE, JIT_GenericHandleClass, CORINFO_CLASS_HANDLE classHnd, LPVOID signature)
{
     CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(classHnd));
        PRECONDITION(CheckPointer(signature));
    } CONTRACTL_END;

    JitGenericHandleCacheKey key(classHnd, NULL, signature);
    HashDatum res;
    if (g_pJitGenericHandleCache->GetValueSpeculative(&key,&res))
        return (CORINFO_GENERIC_HANDLE) (DictionaryEntry) res;

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL5(JIT_GenericHandle_Framed, classHnd, NULL, signature, -1, NULL);
}
HCIMPLEND

HCIMPL2(CORINFO_GENERIC_HANDLE, JIT_GenericHandleClassWithSlotAndModule, CORINFO_CLASS_HANDLE classHnd, GenericHandleArgs * pArgs)
{
    CONTRACTL{
        FCALL_CHECK;
        PRECONDITION(CheckPointer(classHnd));
        PRECONDITION(CheckPointer(pArgs));
    } CONTRACTL_END;

    JitGenericHandleCacheKey key(classHnd, NULL, pArgs->signature);
    HashDatum res;
    if (g_pJitGenericHandleCache->GetValueSpeculative(&key, &res))
        return (CORINFO_GENERIC_HANDLE)(DictionaryEntry)res;

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL5(JIT_GenericHandle_Framed, classHnd, NULL, pArgs->signature, pArgs->dictionaryIndexAndSlot, pArgs->module);
}
HCIMPLEND
#include <optdefault.h>

/*********************************************************************/
HCIMPL2(CORINFO_GENERIC_HANDLE, JIT_GenericHandleClassLogging, CORINFO_CLASS_HANDLE classHnd, LPVOID signature)
{
     CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(classHnd));
        PRECONDITION(CheckPointer(signature));
    } CONTRACTL_END;

    JitGenericHandleCacheKey key(classHnd, NULL, signature);
    HashDatum res;
    if (g_pJitGenericHandleCache->GetValueSpeculative(&key,&res))
        return (CORINFO_GENERIC_HANDLE) (DictionaryEntry) res;

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL5(JIT_GenericHandle_Framed, classHnd, NULL, signature, -1, NULL);
}
HCIMPLEND

/*********************************************************************/
// Resolve a virtual method at run-time, either because of
// aggressive backpatching or because the call is to a generic
// method which is itself virtual.
//
// classHnd is the actual run-time type for the call is made.
// methodHnd is the exact (instantiated) method descriptor corresponding to the
// static method signature (i.e. might be for a superclass of classHnd)

// slow helper to tail call from the fast one
NOINLINE HCIMPL3(CORINFO_MethodPtr, JIT_VirtualFunctionPointer_Framed, Object * objectUNSAFE,
                                                       CORINFO_CLASS_HANDLE classHnd,
                                                       CORINFO_METHOD_HANDLE methodHnd)
{
    FCALL_CONTRACT;

        // The address of the method that's returned.
    CORINFO_MethodPtr   addr = NULL;

    OBJECTREF objRef = ObjectToOBJECTREF(objectUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_RET_1(objRef);    // Set up a frame

    if (objRef == NULL)
        COMPlusThrow(kNullReferenceException);

    // This is the static method descriptor describing the call.
    // It is not the destination of the call, which we must compute.
    MethodDesc* pStaticMD = (MethodDesc*) methodHnd;
    TypeHandle staticTH(classHnd);

    pStaticMD->CheckRestore();

    // MDIL: If IL specifies callvirt/ldvirtftn it remains a "virtual" instruction
    // even if the target is an instance method at MDIL generation time because
    // we want to keep MDIL as resilient as IL. Right now we can end up here with
    // non-virtual generic methods called from a "shared generic code".
    // As soon as this deficiency is fixed in the binder we can get rid of this test.
    if (!pStaticMD->IsVtableMethod())
    {
        addr = (CORINFO_MethodPtr) pStaticMD->GetMultiCallableAddrOfCode();
        _ASSERTE(addr);
    }
    else
    {
        // This is the new way of resolving a virtual call, including generic virtual methods.
        // The code is now also used by reflection, remoting etc.
        addr = (CORINFO_MethodPtr) pStaticMD->GetMultiCallableAddrOfVirtualizedCode(&objRef, staticTH);
        _ASSERTE(addr);

        // This is not a critical entry - no need to specify appdomain affinity
        JitGenericHandleCacheKey key(objRef->GetMethodTable(), classHnd, methodHnd);
        AddToGenericHandleCache(&key, (HashDatum)addr);
    }

    HELPER_METHOD_FRAME_END();

    return addr;
}
HCIMPLEND

HCIMPL3(void, Jit_NativeMemSet, void* pDest, int value, size_t length)
{
    _ASSERTE(pDest != nullptr);
    FCALL_CONTRACT;
    memset(pDest, value, length);
}
HCIMPLEND

HCIMPL1(Object*, JIT_GetRuntimeFieldStub, CORINFO_FIELD_HANDLE field)
{
    FCALL_CONTRACT;

    OBJECTREF stubRuntimeField = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    FieldDesc *pField = (FieldDesc *)field;
    stubRuntimeField = (OBJECTREF)pField->GetStubFieldInfo();

    HELPER_METHOD_FRAME_END();

    return (OBJECTREFToObject(stubRuntimeField));
}
HCIMPLEND

HCIMPL1(Object*, JIT_GetRuntimeMethodStub, CORINFO_METHOD_HANDLE method)
{
    FCALL_CONTRACT;

    OBJECTREF stubRuntimeMethod = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    MethodDesc *pMethod = (MethodDesc *)method;
    stubRuntimeMethod = (OBJECTREF)pMethod->GetStubMethodInfo();

    HELPER_METHOD_FRAME_END();

    return (OBJECTREFToObject(stubRuntimeMethod));
}
HCIMPLEND


NOINLINE HCIMPL1(Object*, JIT_GetRuntimeType_Framed, CORINFO_CLASS_HANDLE type)
{
    FCALL_CONTRACT;

    TypeHandle typeHandle(type);

    // Array/other type handle case.
    OBJECTREF refType = typeHandle.GetManagedClassObjectFast();
    if (refType == NULL)
    {
        HELPER_METHOD_FRAME_BEGIN_RET_1(refType);
        refType = typeHandle.GetManagedClassObject();
        HELPER_METHOD_FRAME_END();
    }

    return OBJECTREFToObject(refType);
}
HCIMPLEND

#include <optsmallperfcritical.h>
HCIMPL1(Object*, JIT_GetRuntimeType, CORINFO_CLASS_HANDLE type)
{
    FCALL_CONTRACT;

    TypeHandle typeHnd(type);

    if (!typeHnd.IsTypeDesc())
    {
        // Most common... and fastest case
        OBJECTREF typePtr = typeHnd.AsMethodTable()->GetManagedClassObjectIfExists();
        if (typePtr != NULL)
        {
            return OBJECTREFToObject(typePtr);
        }
    }

    ENDFORBIDGC();
    return HCCALL1(JIT_GetRuntimeType_Framed, type);
}
HCIMPLEND

HCIMPL1(Object*, JIT_GetRuntimeType_MaybeNull, CORINFO_CLASS_HANDLE type)
{
    FCALL_CONTRACT;

    if (type == NULL)
        return NULL;;

    ENDFORBIDGC();
    return HCCALL1(JIT_GetRuntimeType, type);
}
HCIMPLEND
#include <optdefault.h>

/*********************************************************************/
#include <optsmallperfcritical.h>
HCIMPL3(CORINFO_MethodPtr, JIT_VirtualFunctionPointer, Object * objectUNSAFE,
                                                       CORINFO_CLASS_HANDLE classHnd,
                                                       CORINFO_METHOD_HANDLE methodHnd)
{
    FCALL_CONTRACT;

    OBJECTREF objRef = ObjectToOBJECTREF(objectUNSAFE);

    if (objRef != NULL)
    {
        JitGenericHandleCacheKey key(objRef->GetMethodTable(), classHnd, methodHnd);
        HashDatum res;
        if (g_pJitGenericHandleCache->GetValueSpeculative(&key,&res))
            return (CORINFO_GENERIC_HANDLE)res;
    }

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL3(JIT_VirtualFunctionPointer_Framed, OBJECTREFToObject(objRef), classHnd, methodHnd);
}
HCIMPLEND

HCIMPL2(CORINFO_MethodPtr, JIT_VirtualFunctionPointer_Dynamic, Object * objectUNSAFE, VirtualFunctionPointerArgs * pArgs)
{
    FCALL_CONTRACT;

    OBJECTREF objRef = ObjectToOBJECTREF(objectUNSAFE);

    if (objRef != NULL)
    {
        JitGenericHandleCacheKey key(objRef->GetMethodTable(), pArgs->classHnd, pArgs->methodHnd);
        HashDatum res;
        if (g_pJitGenericHandleCache->GetValueSpeculative(&key,&res))
            return (CORINFO_GENERIC_HANDLE)res;
    }

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL3(JIT_VirtualFunctionPointer_Framed, OBJECTREFToObject(objRef), pArgs->classHnd, pArgs->methodHnd);
}
HCIMPLEND

#include <optdefault.h>

// Helper for synchronized static methods in shared generics code
#include <optsmallperfcritical.h>
HCIMPL1(CORINFO_CLASS_HANDLE, JIT_GetClassFromMethodParam, CORINFO_METHOD_HANDLE methHnd_)
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(methHnd_ != NULL);
    } CONTRACTL_END;

    MethodDesc *  pMD = (MethodDesc*)  methHnd_;

    MethodTable * pMT = pMD->GetMethodTable();
    _ASSERTE(!pMT->IsSharedByGenericInstantiations());

    return((CORINFO_CLASS_HANDLE)pMT);
HCIMPLEND
#include <optdefault.h>



//========================================================================
//
//      MONITOR HELPERS
//
//========================================================================

/*********************************************************************/
NOINLINE static void JIT_MonEnter_Helper(Object* obj, BYTE* pbLockTaken, LPVOID __me)
{
    FC_INNER_PROLOG_NO_ME_SETUP();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, objRef);

    if (objRef == NULL)
        COMPlusThrow(kArgumentNullException);

    GCPROTECT_BEGININTERIOR(pbLockTaken);

    if (GET_THREAD()->CatchAtSafePointOpportunistic())
    {
        GET_THREAD()->PulseGCMode();
    }
    objRef->EnterObjMonitor();

    if (pbLockTaken != 0) *pbLockTaken = 1;

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
}

/*********************************************************************/
#include <optsmallperfcritical.h>

HCIMPL_MONHELPER(JIT_MonEnterWorker_Portable, Object* obj)
{
    FCALL_CONTRACT;

    if (obj != nullptr && obj->TryEnterObjMonitorSpinHelper())
    {
        MONHELPER_STATE(*pbLockTaken = 1);
        return;
    }

    FC_INNER_RETURN_VOID(JIT_MonEnter_Helper(obj, MONHELPER_ARG, GetEEFuncEntryPointMacro(JIT_MonEnter)));
}
HCIMPLEND

HCIMPL1(void, JIT_MonEnter_Portable, Object* obj)
{
    FCALL_CONTRACT;

    if (obj != nullptr && obj->TryEnterObjMonitorSpinHelper())
    {
        return;
    }

    FC_INNER_RETURN_VOID(JIT_MonEnter_Helper(obj, NULL, GetEEFuncEntryPointMacro(JIT_MonEnter)));
}
HCIMPLEND

HCIMPL2(void, JIT_MonReliableEnter_Portable, Object* obj, BYTE* pbLockTaken)
{
    FCALL_CONTRACT;

    if (obj != nullptr && obj->TryEnterObjMonitorSpinHelper())
    {
        *pbLockTaken = 1;
        return;
    }

    FC_INNER_RETURN_VOID(JIT_MonEnter_Helper(obj, pbLockTaken, GetEEFuncEntryPointMacro(JIT_MonReliableEnter)));
}
HCIMPLEND

#include <optdefault.h>


/*********************************************************************/
NOINLINE static void JIT_MonTryEnter_Helper(Object* obj, INT32 timeOut, BYTE* pbLockTaken)
{
    FC_INNER_PROLOG(JIT_MonTryEnter);

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, objRef);

    if (objRef == NULL)
        COMPlusThrow(kArgumentNullException);

    if (timeOut < -1)
        COMPlusThrow(kArgumentOutOfRangeException);

    GCPROTECT_BEGININTERIOR(pbLockTaken);

    if (GET_THREAD()->CatchAtSafePointOpportunistic())
    {
        GET_THREAD()->PulseGCMode();
    }

    BOOL result = objRef->TryEnterObjMonitor(timeOut);
    *pbLockTaken = result != FALSE;

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
}

#include <optsmallperfcritical.h>
HCIMPL3(void, JIT_MonTryEnter_Portable, Object* obj, INT32 timeOut, BYTE* pbLockTaken)
{
    FCALL_CONTRACT;

    AwareLock::EnterHelperResult result;
    Thread * pCurThread;

    if (obj == NULL)
    {
        goto FramedLockHelper;
    }

    if (timeOut < -1)
    {
        goto FramedLockHelper;
    }

    pCurThread = GetThread();

    if (pCurThread->CatchAtSafePointOpportunistic())
    {
        goto FramedLockHelper;
    }

    result = obj->EnterObjMonitorHelper(pCurThread);
    if (result == AwareLock::EnterHelperResult_Entered)
    {
        *pbLockTaken = 1;
        return;
    }
    if (result == AwareLock::EnterHelperResult_Contention)
    {
        if (timeOut == 0)
        {
            return;
        }

        result = obj->EnterObjMonitorHelperSpin(pCurThread);
        if (result == AwareLock::EnterHelperResult_Entered)
        {
            *pbLockTaken = 1;
            return;
        }
    }

FramedLockHelper:
    FC_INNER_RETURN_VOID(JIT_MonTryEnter_Helper(obj, timeOut, pbLockTaken));
}
HCIMPLEND
#include <optdefault.h>

/*********************************************************************/
NOINLINE static void JIT_MonExit_Helper(Object* obj, BYTE* pbLockTaken)
{
    FC_INNER_PROLOG(JIT_MonExit);

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_NO_THREAD_ABORT|Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, objRef);

    if (objRef == NULL)
        COMPlusThrow(kArgumentNullException);

    if (!objRef->LeaveObjMonitor())
        COMPlusThrow(kSynchronizationLockException);

    if (pbLockTaken != 0) *pbLockTaken = 0;

    if (GET_THREAD()->IsAbortRequested()) {
        GET_THREAD()->HandleThreadAbort();
    }

    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
}

NOINLINE static void JIT_MonExit_Signal(Object* obj)
{
    FC_INNER_PROLOG(JIT_MonExit);

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_NO_THREAD_ABORT|Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, objRef);

    // Signal the event
    SyncBlock *psb = objRef->PassiveGetSyncBlock();
    if (psb != NULL)
        psb->QuickGetMonitor()->Signal();

    if (GET_THREAD()->IsAbortRequested()) {
        GET_THREAD()->HandleThreadAbort();
    }

    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
}

#include <optsmallperfcritical.h>
FCIMPL1(void, JIT_MonExit_Portable, Object* obj)
{
    FCALL_CONTRACT;

    AwareLock::LeaveHelperAction action;

    if (obj == NULL)
    {
        goto FramedLockHelper;
    }

    // Handle the simple case without erecting helper frame
    action = obj->LeaveObjMonitorHelper(GetThread());
    if (action == AwareLock::LeaveHelperAction_None)
    {
        return;
    }
    if (action == AwareLock::LeaveHelperAction_Signal)
    {
        FC_INNER_RETURN_VOID(JIT_MonExit_Signal(obj));
    }

FramedLockHelper:
    FC_INNER_RETURN_VOID(JIT_MonExit_Helper(obj, NULL));
}
HCIMPLEND

HCIMPL_MONHELPER(JIT_MonExitWorker_Portable, Object* obj)
{
    FCALL_CONTRACT;

    MONHELPER_STATE(_ASSERTE(pbLockTaken != NULL));
    MONHELPER_STATE(if (*pbLockTaken == 0) return;)

    AwareLock::LeaveHelperAction action;

    if (obj == NULL)
    {
        goto FramedLockHelper;
    }

    // Handle the simple case without erecting helper frame
    action = obj->LeaveObjMonitorHelper(GetThread());
    if (action == AwareLock::LeaveHelperAction_None)
    {
        MONHELPER_STATE(*pbLockTaken = 0;)
        return;
    }
    if (action == AwareLock::LeaveHelperAction_Signal)
    {
        MONHELPER_STATE(*pbLockTaken = 0;)
        FC_INNER_RETURN_VOID(JIT_MonExit_Signal(obj));
    }

FramedLockHelper:
    FC_INNER_RETURN_VOID(JIT_MonExit_Helper(obj, MONHELPER_ARG));
}
HCIMPLEND
#include <optdefault.h>

/*********************************************************************/
NOINLINE static void JIT_MonEnterStatic_Helper(AwareLock *lock, BYTE* pbLockTaken)
{
    // The following makes sure that Monitor.Enter shows up on thread abort
    // stack walks (otherwise Monitor.Enter called within a CER can block a
    // thread abort indefinitely). Setting the __me internal variable (normally
    // only set for fcalls) will cause the helper frame below to be able to
    // backtranslate into the method desc for the Monitor.Enter fcall.
    FC_INNER_PROLOG(JIT_MonEnter);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2);
    lock->Enter();
    MONHELPER_STATE(*pbLockTaken = 1;)
    HELPER_METHOD_FRAME_END_POLL();

    FC_INNER_EPILOG();
}

#include <optsmallperfcritical.h>
HCIMPL_MONHELPER(JIT_MonEnterStatic_Portable, AwareLock *lock)
{
    FCALL_CONTRACT;

    _ASSERTE(lock);

    MONHELPER_STATE(_ASSERTE(pbLockTaken != NULL && *pbLockTaken == 0));

    Thread *pCurThread = GetThread();
    if (pCurThread->CatchAtSafePointOpportunistic())
    {
        goto FramedLockHelper;
    }

    if (lock->TryEnterHelper(pCurThread))
    {
#if defined(_DEBUG) && defined(TRACK_SYNC)
        // The best place to grab this is from the ECall frame
        Frame * pFrame = pCurThread->GetFrame();
        int     caller = (pFrame && pFrame != FRAME_TOP ? (int) pFrame->GetReturnAddress() : -1);
        pCurThread->m_pTrackSync->EnterSync(caller, lock);
#endif

        MONHELPER_STATE(*pbLockTaken = 1;)
        return;
    }

FramedLockHelper:
    FC_INNER_RETURN_VOID(JIT_MonEnterStatic_Helper(lock, MONHELPER_ARG));
}
HCIMPLEND
#include <optdefault.h>

/*********************************************************************/
NOINLINE static void JIT_MonExitStatic_Helper(AwareLock *lock, BYTE* pbLockTaken)
{
    FC_INNER_PROLOG(JIT_MonExit);

    HELPER_METHOD_FRAME_BEGIN_ATTRIB(Frame::FRAME_ATTR_NO_THREAD_ABORT|Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2);

    // Error, yield or contention
    if (!lock->Leave())
        COMPlusThrow(kSynchronizationLockException);
    MONHELPER_STATE(*pbLockTaken = 0;)

    if (GET_THREAD()->IsAbortRequested()) {
        GET_THREAD()->HandleThreadAbort();
    }

    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
}

NOINLINE static void JIT_MonExitStatic_Signal(AwareLock *lock)
{
    FC_INNER_PROLOG(JIT_MonExit);

    HELPER_METHOD_FRAME_BEGIN_ATTRIB(Frame::FRAME_ATTR_NO_THREAD_ABORT|Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2);

    lock->Signal();

    if (GET_THREAD()->IsAbortRequested()) {
        GET_THREAD()->HandleThreadAbort();
    }

    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
}

#include <optsmallperfcritical.h>
HCIMPL_MONHELPER(JIT_MonExitStatic_Portable, AwareLock *lock)
{
    FCALL_CONTRACT;

    _ASSERTE(lock);

    MONHELPER_STATE(_ASSERTE(pbLockTaken != NULL));
    MONHELPER_STATE(if (*pbLockTaken == 0) return;)

    // Handle the simple case without erecting helper frame
    AwareLock::LeaveHelperAction action = lock->LeaveHelper(GetThread());
    if (action == AwareLock::LeaveHelperAction_None)
    {
        MONHELPER_STATE(*pbLockTaken = 0;)
        return;
    }
    else
    if (action == AwareLock::LeaveHelperAction_Signal)
    {
        MONHELPER_STATE(*pbLockTaken = 0;)
        FC_INNER_RETURN_VOID(JIT_MonExitStatic_Signal(lock));
    }

    FC_INNER_RETURN_VOID(JIT_MonExitStatic_Helper(lock, MONHELPER_ARG));
}
HCIMPLEND
#include <optdefault.h>

HCIMPL1(void *, JIT_GetSyncFromClassHandle, CORINFO_CLASS_HANDLE typeHnd_)
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(typeHnd_ != NULL);
    } CONTRACTL_END;

    void * result = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();    // Set up a frame

    TypeHandle typeHnd(typeHnd_);
    MethodTable *pMT = typeHnd.AsMethodTable();

    OBJECTREF ref = pMT->GetManagedClassObject();
    _ASSERTE(ref);

    result = (void*)ref->GetSyncBlock()->GetMonitor();

    HELPER_METHOD_FRAME_END();

    return(result);

HCIMPLEND


//========================================================================
//
//      EXCEPTION HELPERS
//
//========================================================================

// In general, we want to use COMPlusThrow to throw exceptions.  However,
// the IL_Throw helper is a special case.  Here, we're called from
// managed code.  We have a guarantee that the first FS:0 handler
// is our COMPlusFrameHandler.  We could call COMPlusThrow(), which pushes
// another handler, but there is a significant (10% on JGFExceptionBench)
// performance gain if we avoid this by calling RaiseTheException()
// directly.
//

/*************************************************************/

#ifdef FEATURE_EH_FUNCLETS
void ThrowNew(OBJECTREF oref)
{
    if (oref == 0)
        DispatchManagedException(kNullReferenceException);
    else
    if (!IsException(oref->GetMethodTable()))
    {
        GCPROTECT_BEGIN(oref);

        WrapNonCompliantException(&oref);

        GCPROTECT_END();
    }
    else
    {   // We know that the object derives from System.Exception

        // If the flag indicating ForeignExceptionRaise has been set,
        // then do not clear the "_stackTrace" field of the exception object.
        if (GetThread()->GetExceptionState()->IsRaisingForeignException())
        {
            ((EXCEPTIONREF)oref)->SetStackTraceString(NULL);
        }
        else
        {
            ((EXCEPTIONREF)oref)->ClearStackTracePreservingRemoteStackTrace();
        }
    }

    DispatchManagedException(oref, /* preserveStackTrace */ false);
}
#endif // FEATURE_EH_FUNCLETS

HCIMPL1(void, IL_Throw,  Object* obj)
{
    FCALL_CONTRACT;

    /* Make no assumptions about the current machine state */
    ResetCurrentContext();

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    OBJECTREF oref = ObjectToOBJECTREF(obj);

#if defined(_DEBUG) && defined(TARGET_X86)
    __helperframe.InsureInit(false, NULL);
    g_ExceptionEIP = (LPVOID)__helperframe.GetReturnAddress();
#endif // defined(_DEBUG) && defined(TARGET_X86)

#ifdef FEATURE_EH_FUNCLETS
    if (g_isNewExceptionHandlingEnabled)
    {
        ThrowNew(oref);
        UNREACHABLE();
    }
#endif

    if (oref == 0)
        COMPlusThrow(kNullReferenceException);
    else
    if (!IsException(oref->GetMethodTable()))
    {
        GCPROTECT_BEGIN(oref);

        WrapNonCompliantException(&oref);

        GCPROTECT_END();
    }
    else
    {   // We know that the object derives from System.Exception

        // If the flag indicating ForeignExceptionRaise has been set,
        // then do not clear the "_stackTrace" field of the exception object.
        if (GetThread()->GetExceptionState()->IsRaisingForeignException())
        {
            ((EXCEPTIONREF)oref)->SetStackTraceString(NULL);
        }
        else
        {
            ((EXCEPTIONREF)oref)->ClearStackTracePreservingRemoteStackTrace();
        }
    }

    RaiseTheExceptionInternalOnly(oref, FALSE);

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*************************************************************/

#ifdef FEATURE_EH_FUNCLETS
void RethrowNew()
{
    Thread *pThread = GetThread();

    ExInfo *pActiveExInfo = (ExInfo*)pThread->GetExceptionState()->GetCurrentExceptionTracker();

    CONTEXT exceptionContext;
    RtlCaptureContext(&exceptionContext);

    ExInfo exInfo(pThread, pActiveExInfo->m_ptrs.ExceptionRecord, &exceptionContext, ExKind::None);

    GCPROTECT_BEGIN(exInfo.m_exception);
    PREPARE_NONVIRTUAL_CALLSITE(METHOD__EH__RH_RETHROW);
    DECLARE_ARGHOLDER_ARRAY(args, 2);

    args[ARGNUM_0] = PTR_TO_ARGHOLDER(pActiveExInfo);
    args[ARGNUM_1] = PTR_TO_ARGHOLDER(&exInfo);

    pThread->IncPreventAbort();

    //Ex.RhRethrow(ref ExInfo activeExInfo, ref ExInfo exInfo)
    CALL_MANAGED_METHOD_NORET(args)
    GCPROTECT_END();
}
#endif // FEATURE_EH_FUNCLETS

HCIMPL0(void, IL_Rethrow)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

#ifdef FEATURE_EH_FUNCLETS
    if (g_isNewExceptionHandlingEnabled)
    {
        RethrowNew();
        UNREACHABLE();
    }
#endif

    OBJECTREF throwable = GetThread()->GetThrowable();
    if (throwable != NULL)
    {
        RaiseTheExceptionInternalOnly(throwable, TRUE);
    }
    else
    {
        // This can only be the result of bad IL (or some internal EE failure).
        _ASSERTE(!"No throwable on rethrow");
        RealCOMPlusThrow(kInvalidProgramException, (UINT)IDS_EE_RETHROW_NOT_ALLOWED);
    }

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
HCIMPL0(void, JIT_RngChkFail)
{
    FCALL_CONTRACT;

    /* Make no assumptions about the current machine state */
    ResetCurrentContext();

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    COMPlusThrow(kIndexOutOfRangeException);

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
HCIMPL0(void, JIT_ThrowArgumentException)
{
    FCALL_CONTRACT;

    /* Make no assumptions about the current machine state */
    ResetCurrentContext();

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    COMPlusThrow(kArgumentException);

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
HCIMPL0(void, JIT_ThrowArgumentOutOfRangeException)
{
    FCALL_CONTRACT;

    /* Make no assumptions about the current machine state */
    ResetCurrentContext();

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    COMPlusThrow(kArgumentOutOfRangeException);

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
HCIMPL0(void, JIT_ThrowNotImplementedException)
{
    FCALL_CONTRACT;

    /* Make no assumptions about the current machine state */
    ResetCurrentContext();

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    COMPlusThrow(kNotImplementedException);

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
HCIMPL0(void, JIT_ThrowPlatformNotSupportedException)
{
    FCALL_CONTRACT;

    /* Make no assumptions about the current machine state */
    ResetCurrentContext();

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    COMPlusThrow(kPlatformNotSupportedException);

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
HCIMPL0(void, JIT_ThrowTypeNotSupportedException)
{
    FCALL_CONTRACT;

    /* Make no assumptions about the current machine state */
    ResetCurrentContext();

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    COMPlusThrow(kNotSupportedException, W("Arg_TypeNotSupported"));

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
HCIMPL3(void, JIT_ThrowAmbiguousResolutionException,
    MethodDesc *method,
    MethodTable *interfaceType,
    MethodTable *targetType)
{
    FCALL_CONTRACT;

    SString strMethodName;
    SString strInterfaceName;
    SString strTargetClassName;

    HELPER_METHOD_FRAME_BEGIN_0();    // Set up a frame

    TypeString::AppendMethod(strMethodName, method, method->GetMethodInstantiation());
    TypeString::AppendType(strInterfaceName, TypeHandle(interfaceType));
    TypeString::AppendType(strTargetClassName, targetType);

    HELPER_METHOD_FRAME_END();    // Set up a frame

    FCThrowExVoid(
        kAmbiguousImplementationException,
        IDS_CLASSLOAD_AMBIGUOUS_OVERRIDE,
        strMethodName,
        strInterfaceName,
        strTargetClassName);
}
HCIMPLEND

/*********************************************************************/
HCIMPL3(void, JIT_ThrowEntryPointNotFoundException,
    MethodDesc *method,
    MethodTable *interfaceType,
    MethodTable *targetType)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();    // Set up a frame

    SString strMethodName;
    SString strInterfaceName;
    SString strTargetClassName;
    SString assemblyName;

    targetType->GetAssembly()->GetDisplayName(assemblyName);
    TypeString::AppendMethod(strMethodName, method, method->GetMethodInstantiation());
    TypeString::AppendType(strInterfaceName, TypeHandle(interfaceType));
    TypeString::AppendType(strTargetClassName, targetType);

    COMPlusThrow(
        kEntryPointNotFoundException,
        IDS_CLASSLOAD_METHOD_NOT_IMPLEMENTED,
        strMethodName,
        strInterfaceName,
        strTargetClassName,
        assemblyName);

    HELPER_METHOD_FRAME_END();    // Set up a frame
}
HCIMPLEND

/*********************************************************************/
HCIMPL0(void, JIT_Overflow)
{
    FCALL_CONTRACT;

    /* Make no assumptions about the current machine state */
    ResetCurrentContext();

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    COMPlusThrow(kOverflowException);

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
HCIMPL0(void, JIT_ThrowDivZero)
{
    FCALL_CONTRACT;

    /* Make no assumptions about the current machine state */
    ResetCurrentContext();

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    COMPlusThrow(kDivideByZeroException);

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
HCIMPL0(void, JIT_ThrowNullRef)
{
  FCALL_CONTRACT;

  /* Make no assumptions about the current machine state */
  ResetCurrentContext();

  FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

  HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

  COMPlusThrow(kNullReferenceException);

  HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
HCIMPL1(void, IL_VerificationError,  int ilOffset)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    COMPlusThrow(kVerificationException);

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
static RuntimeExceptionKind MapCorInfoExceptionToRuntimeExceptionKind(unsigned exceptNum)
{
    LIMITED_METHOD_CONTRACT;

    static const RuntimeExceptionKind map[CORINFO_Exception_Count] =
    {
        kNullReferenceException,
        kDivideByZeroException,
        kInvalidCastException,
        kIndexOutOfRangeException,
        kOverflowException,
        kSynchronizationLockException,
        kArrayTypeMismatchException,
        kRankException,
        kArgumentNullException,
        kArgumentException,
    };

        // spot check of the array above
    _ASSERTE(map[CORINFO_NullReferenceException] == kNullReferenceException);
    _ASSERTE(map[CORINFO_DivideByZeroException] == kDivideByZeroException);
    _ASSERTE(map[CORINFO_IndexOutOfRangeException] == kIndexOutOfRangeException);
    _ASSERTE(map[CORINFO_OverflowException] == kOverflowException);
    _ASSERTE(map[CORINFO_SynchronizationLockException] == kSynchronizationLockException);
    _ASSERTE(map[CORINFO_ArrayTypeMismatchException] == kArrayTypeMismatchException);
    _ASSERTE(map[CORINFO_RankException] == kRankException);
    _ASSERTE(map[CORINFO_ArgumentNullException] == kArgumentNullException);
    _ASSERTE(map[CORINFO_ArgumentException] == kArgumentException);

    PREFIX_ASSUME(exceptNum < CORINFO_Exception_Count);
    return map[exceptNum];
}

/*********************************************************************/
HCIMPL1(void, JIT_InternalThrow, unsigned exceptNum)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXACT_DEPTH);
    COMPlusThrow(MapCorInfoExceptionToRuntimeExceptionKind(exceptNum));
    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
HCIMPL1(void*, JIT_InternalThrowFromHelper, unsigned exceptNum)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC
    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_NOPOLL(Frame::FRAME_ATTR_CAPTURE_DEPTH_2|Frame::FRAME_ATTR_EXACT_DEPTH);
    COMPlusThrow(MapCorInfoExceptionToRuntimeExceptionKind(exceptNum));
    HELPER_METHOD_FRAME_END();
    return NULL;
}
HCIMPLEND

#ifndef STATUS_STACK_BUFFER_OVERRUN  // Not defined yet in CESDK includes
# define STATUS_STACK_BUFFER_OVERRUN      ((NTSTATUS)0xC0000409L)
#endif

/*********************************************************************
 * Kill process without using any potentially corrupted data:
 *      o Do not throw an exception
 *      o Do not call any indirect/virtual functions
 *      o Do not depend on any global data
 *
 * This function is used by the security checks for unsafe buffers (VC's -GS checks)
 */

void DoJITFailFast ()
{
    CONTRACTL {
        MODE_ANY;
        WRAPPER(GC_TRIGGERS);
        WRAPPER(THROWS);
    } CONTRACTL_END;

    LOG((LF_ALWAYS, LL_FATALERROR, "Unsafe buffer security check failure: Buffer overrun detected"));

#ifdef _DEBUG
    if (g_pConfig->fAssertOnFailFast())
        _ASSERTE(!"About to FailFast. set DOTNET_AssertOnFailFast=0 if this is expected");
#endif

#ifndef TARGET_UNIX
    // Use the function provided by the C runtime.
    //
    // Ideally, this function is called directly from managed code so
    // that the address of the managed function will be included in the
    // error log. However, this function is also used by the stackwalker.
    // To keep things simple, we just call it from here.
#if defined(TARGET_X86)
    __report_gsfailure();
#else // !defined(TARGET_X86)
    // On AMD64/IA64/ARM, we need to pass a stack cookie, which will be saved in the context record
    // that is used to raise the buffer-overrun exception by __report_gsfailure.
    __report_gsfailure((ULONG_PTR)0);
#endif // defined(TARGET_X86)
#else // TARGET_UNIX
    if(ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context, FailFast))
    {
        // Fire an ETW FailFast event
        FireEtwFailFast(W("Unsafe buffer security check failure: Buffer overrun detected"),
                       (const PVOID)GetThread()->GetFrame()->GetIP(),
                       STATUS_STACK_BUFFER_OVERRUN,
                       COR_E_EXECUTIONENGINE,
                       GetClrInstanceId());
    }

    CrashDumpAndTerminateProcess(STATUS_STACK_BUFFER_OVERRUN);
#endif // !TARGET_UNIX
}

HCIMPL0(void, JIT_FailFast)
{
    FCALL_CONTRACT;
    DoJITFailFast ();
}
HCIMPLEND

HCIMPL2(void, JIT_ThrowMethodAccessException, CORINFO_METHOD_HANDLE caller, CORINFO_METHOD_HANDLE callee)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    MethodDesc* pCallerMD = GetMethod(caller);

    _ASSERTE(pCallerMD != NULL);
    AccessCheckContext accessContext(pCallerMD);

    ThrowMethodAccessException(&accessContext, GetMethod(callee));

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

HCIMPL2(void, JIT_ThrowFieldAccessException, CORINFO_METHOD_HANDLE caller, CORINFO_FIELD_HANDLE callee)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    MethodDesc* pCallerMD = GetMethod(caller);

    _ASSERTE(pCallerMD != NULL);
    AccessCheckContext accessContext(pCallerMD);

    ThrowFieldAccessException(&accessContext, reinterpret_cast<FieldDesc *>(callee));

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND;

HCIMPL2(void, JIT_ThrowClassAccessException, CORINFO_METHOD_HANDLE caller, CORINFO_CLASS_HANDLE callee)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    MethodDesc* pCallerMD = GetMethod(caller);

    _ASSERTE(pCallerMD != NULL);
    AccessCheckContext accessContext(pCallerMD);

    ThrowTypeAccessException(&accessContext, TypeHandle(callee).GetMethodTable());

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND;

//========================================================================
//
//      DEBUGGER/PROFILER HELPERS
//
//========================================================================

/*********************************************************************/
// Called by the JIT whenever a cee_break instruction should be executed.
//
HCIMPL0(void, JIT_UserBreakpoint)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();    // Set up a frame

#ifdef DEBUGGING_SUPPORTED
    FrameWithCookie<DebuggerExitFrame> __def;

    MethodDescCallSite debuggerBreak(METHOD__DEBUGGER__BREAK);

    debuggerBreak.Call((ARG_SLOT*)NULL);

    __def.Pop();
#else // !DEBUGGING_SUPPORTED
    _ASSERTE(!"JIT_UserBreakpoint called, but debugging support is not available in this build.");
#endif // !DEBUGGING_SUPPORTED

    HELPER_METHOD_FRAME_END_POLL();
}
HCIMPLEND

#if defined(_MSC_VER)
// VC++ Compiler intrinsic.
extern "C" void * _ReturnAddress(void);
#endif

/*********************************************************************/
// Callback for Just-My-Code probe
// Probe looks like:
//  if (*pFlag != 0) call JIT_DbgIsJustMyCode
// So this is only called if the flag (obtained by GetJMCFlagAddr) is
// non-zero.
HCIMPL0(void, JIT_DbgIsJustMyCode)
{
    FCALL_CONTRACT;

    // We need to get both the ip of the managed function this probe is in
    // (which will be our return address) and the frame pointer for that
    // function (since we can't get it later because we're pushing unmanaged
    // frames on the stack).
    void * ip = NULL;

    // <NOTE>
    // In order for the return address to be correct, we must NOT call any
    // function before calling _ReturnAddress().
    // </NOTE>
    ip = _ReturnAddress();

    _ASSERTE(ip != NULL);

    // Call into debugger proper
    g_pDebugInterface->OnMethodEnter(ip);

    return;
}
HCIMPLEND

#ifdef PROFILING_SUPPORTED

//---------------------------------------------------------------------------------------
//
// Sets the profiler's enter/leave/tailcall hooks into the JIT's dynamic helper
// function table.
//
// Arguments:
//      pFuncEnter - Enter hook
//      pFuncLeave - Leave hook
//      pFuncTailcall - Tailcall hook
//
//      For each hook parameter, if NULL is passed in, that will cause the JIT
//      to insert calls to its default stub replacement for that hook, which
//      just does a ret.
//
// Return Value:
//      HRESULT indicating success or failure
//
// Notes:
//      On IA64, this will allocate space for stubs to update GP, and that
//      allocation may take locks and may throw on failure.  Callers be warned.
//

HRESULT EEToProfInterfaceImpl::SetEnterLeaveFunctionHooksForJit(FunctionEnter3 * pFuncEnter,
                                                                FunctionLeave3 * pFuncLeave,
                                                                FunctionTailcall3 * pFuncTailcall)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    SetJitHelperFunction(
        CORINFO_HELP_PROF_FCN_ENTER,
        (pFuncEnter == NULL) ?
            reinterpret_cast<void *>(JIT_ProfilerEnterLeaveTailcallStub) :
            reinterpret_cast<void *>(pFuncEnter));

    SetJitHelperFunction(
        CORINFO_HELP_PROF_FCN_LEAVE,
        (pFuncLeave == NULL) ?
            reinterpret_cast<void *>(JIT_ProfilerEnterLeaveTailcallStub) :
            reinterpret_cast<void *>(pFuncLeave));

    SetJitHelperFunction(
        CORINFO_HELP_PROF_FCN_TAILCALL,
        (pFuncTailcall == NULL) ?
            reinterpret_cast<void *>(JIT_ProfilerEnterLeaveTailcallStub) :
            reinterpret_cast<void *>(pFuncTailcall));

    return (S_OK);
}
#endif // PROFILING_SUPPORTED

/*************************************************************/
HCIMPL1(void, JIT_LogMethodEnter, CORINFO_METHOD_HANDLE methHnd_)
    FCALL_CONTRACT;

    //
    // Record an access to this method desc
    //

HCIMPLEND



//========================================================================
//
//      GC HELPERS
//
//========================================================================

/*************************************************************/
HCIMPL3(VOID, JIT_StructWriteBarrier, void *dest, void* src, CORINFO_CLASS_HANDLE typeHnd_)
{
    FCALL_CONTRACT;

    TypeHandle typeHnd(typeHnd_);
    MethodTable *pMT = typeHnd.AsMethodTable();

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();    // Set up a frame
    CopyValueClass(dest, src, pMT);
    HELPER_METHOD_FRAME_END_POLL();

}
HCIMPLEND

/*************************************************************/
// Slow helper to tailcall from the fast one
NOINLINE HCIMPL0(void, JIT_PollGC_Framed)
{
    BEGIN_PRESERVE_LAST_ERROR;

    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();    // Set up a frame
#ifdef _DEBUG
    BOOL GCOnTransition = FALSE;
    if (g_pConfig->FastGCStressLevel()) {
        GCOnTransition = GC_ON_TRANSITIONS (FALSE);
    }
#endif // _DEBUG
    CommonTripThread();         // Indicate we are at a GC safe point
#ifdef _DEBUG
    if (g_pConfig->FastGCStressLevel()) {
        GC_ON_TRANSITIONS (GCOnTransition);
    }
#endif // _DEBUG
    HELPER_METHOD_FRAME_END();
    END_PRESERVE_LAST_ERROR;
}
HCIMPLEND

HCIMPL0(VOID, JIT_PollGC)
{
    FCALL_CONTRACT;

    // As long as we can have GCPOLL_CALL polls, it would not hurt to check the trap flag.
    if (!g_TrapReturningThreads.LoadWithoutBarrier())
        return;

    // Does someone want this thread stopped?
    if (!GetThread()->CatchAtSafePointOpportunistic())
        return;

    // Tailcall to the slow helper
    ENDFORBIDGC();
    HCCALL0(JIT_PollGC_Framed);
}
HCIMPLEND


/*************************************************************/
// This helper is similar to JIT_RareDisableHelper, but has more operations
// tailored to the post-pinvoke operations.
extern "C" FCDECL0(VOID, JIT_PInvokeEndRarePath);

HCIMPL0(void, JIT_PInvokeEndRarePath)
{
    BEGIN_PRESERVE_LAST_ERROR;

    FCALL_CONTRACT;

    Thread *thread = GetThread();

    // We need to disable the implicit FORBID GC region that exists inside an FCALL
    // in order to call RareDisablePreemptiveGC().
    FC_CAN_TRIGGER_GC();
    thread->RareDisablePreemptiveGC();
    FC_CAN_TRIGGER_GC_END();

    FC_GC_POLL_NOT_NEEDED();

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();    // Set up a frame
    thread->HandleThreadAbort();
    HELPER_METHOD_FRAME_END();

    thread->m_pFrame->Pop(thread);

    END_PRESERVE_LAST_ERROR;
}
HCIMPLEND

/*************************************************************/
// For an inlined N/Direct call (and possibly for other places that need this service)
// we have noticed that the returning thread should trap for one reason or another.
// ECall sets up the frame.

extern "C" FCDECL0(VOID, JIT_RareDisableHelper);

#if defined(TARGET_ARM) || defined(TARGET_AMD64)
// The JIT expects this helper to preserve the return value on AMD64 and ARM. We should eventually
// switch other platforms to the same convention since it produces smaller code.
extern "C" FCDECL0(VOID, JIT_RareDisableHelperWorker);

HCIMPL0(void, JIT_RareDisableHelperWorker)
#else
HCIMPL0(void, JIT_RareDisableHelper)
#endif
{
    // We do this here (before we set up a frame), because the following scenario
    // We are in the process of doing an inlined pinvoke.  Since we are in preemtive
    // mode, the thread is allowed to continue.  The thread continues and gets a context
    // switch just after it has cleared the preemptive mode bit but before it gets
    // to this helper.    When we do our stack crawl now, we think this thread is
    // in cooperative mode (and believed that it was suspended in the SuspendEE), so
    // we do a getthreadcontext (on the unsuspended thread!) and get an EIP in jitted code.
    // and proceed.   Assume the crawl of jitted frames is proceeding on the other thread
    // when this thread wakes up and sets up a frame.   Eventually the other thread
    // runs out of jitted frames and sees the frame we just established.  This causes
    // an assert in the stack crawling code.  If this assert is ignored, however, we
    // will end up scanning the jitted frames twice, which will lead to GC holes
    //
    // <TODO>TODO:  It would be MUCH more robust if we should remember which threads
    // we suspended in the SuspendEE, and only even consider using EIP if it was suspended
    // in the first phase.
    //      </TODO>

    BEGIN_PRESERVE_LAST_ERROR;

    FCALL_CONTRACT;

    Thread *thread = GetThread();

    // We need to disable the implicit FORBID GC region that exists inside an FCALL
    // in order to call RareDisablePreemptiveGC().
    FC_CAN_TRIGGER_GC();
    thread->RareDisablePreemptiveGC();
    FC_CAN_TRIGGER_GC_END();

    FC_GC_POLL_NOT_NEEDED();

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();    // Set up a frame
    thread->HandleThreadAbort();
    HELPER_METHOD_FRAME_END();

    END_PRESERVE_LAST_ERROR;
}
HCIMPLEND

/*********************************************************************/
// This is called by the JIT after every instruction in fully interruptible
// code to make certain our GC tracking is OK
HCIMPL0(VOID, JIT_StressGC_NOP)
{
    FCALL_CONTRACT;
}
HCIMPLEND


HCIMPL0(VOID, JIT_StressGC)
{
    FCALL_CONTRACT;

#ifdef _DEBUG
    HELPER_METHOD_FRAME_BEGIN_0();    // Set up a frame

    bool fSkipGC = false;

    if (!fSkipGC)
        GCHeapUtilities::GetGCHeap()->GarbageCollect();

// <TODO>@TODO: the following ifdef is in error, but if corrected the
// compiler complains about the *__ms->pRetAddr() saying machine state
// doesn't allow -></TODO>
#ifdef _X86
                // Get the machine state, (from HELPER_METHOD_FRAME_BEGIN)
                // and wack our return address to a nop function
        BYTE* retInstrs = ((BYTE*) *__ms->pRetAddr()) - 4;
        _ASSERTE(retInstrs[-1] == 0xE8);                // it is a call instruction
                // Wack it to point to the JITStressGCNop instead
        InterlockedExchange((LONG*) retInstrs), (LONG) JIT_StressGC_NOP);
#endif // _X86

    HELPER_METHOD_FRAME_END();
#endif // _DEBUG
}
HCIMPLEND



FCIMPL0(INT32, JIT_GetCurrentManagedThreadId)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();

    Thread * pThread = GetThread();
    return pThread->GetThreadId();
}
FCIMPLEND


/*********************************************************************/
/* we don't use HCIMPL macros because we don't want the overhead even in debug mode */

HCIMPL1_RAW(Object*, JIT_CheckObj, Object* obj)
{
    FCALL_CONTRACT;

    if (obj != 0) {
        MethodTable* pMT = obj->GetMethodTable();
        if (!pMT->ValidateWithPossibleAV()) {
            _ASSERTE_ALL_BUILDS(!"Bad Method Table");
        }
    }
    return obj;
}
HCIMPLEND_RAW

static int loopChoice = 0;

// This function supports a JIT mode in which we're debugging the mechanism for loop cloning.
// We want to clone loops, then make a semi-random choice, on each execution of the loop,
// whether to run the original loop or the cloned copy.  We do this by incrementing the contents
// of a memory location, and testing whether the result is odd or even.  The "loopChoice" variable
// above provides that memory location, and this JIT helper merely informs the JIT of the address of
// "loopChoice".
HCIMPL0(void*, JIT_LoopCloneChoiceAddr)
{
     CONTRACTL {
        FCALL_CHECK;
     } CONTRACTL_END;

     return &loopChoice;
}
HCIMPLEND

// Prints a message that loop cloning optimization has occurred.
HCIMPL0(void, JIT_DebugLogLoopCloning)
{
     CONTRACTL {
        FCALL_CHECK;
     } CONTRACTL_END;

#ifdef _DEBUG
     printf(">> Logging loop cloning optimization\n");
#endif
}
HCIMPLEND

#ifdef FEATURE_ON_STACK_REPLACEMENT

// Helper method to jit the OSR version of a method.
//
// Returns the address of the jitted code.
// Returns NULL if osr method can't be created.
static PCODE JitPatchpointWorker(MethodDesc* pMD, EECodeInfo& codeInfo, int ilOffset)
{
    STANDARD_VM_CONTRACT;
    PCODE osrVariant = NULL;

    // Fetch the patchpoint info for the current method
    EEJitManager* jitMgr = ExecutionManager::GetEEJitManager();
    CodeHeader* codeHdr = jitMgr->GetCodeHeaderFromStartAddress(codeInfo.GetStartAddress());
    PTR_BYTE debugInfo = codeHdr->GetDebugInfo();
    PatchpointInfo* patchpointInfo = CompressDebugInfo::RestorePatchpointInfo(debugInfo);

    if (patchpointInfo == NULL)
    {
        // Unexpected, but not fatal
        STRESS_LOG1(LF_TIEREDCOMPILATION, LL_WARNING, "JitPatchpointWorker: failed to restore patchpoint info for Method=0x%pM\n", pMD);
        return NULL;
    }

    // Set up a new native code version for the OSR variant of this method.
    NativeCodeVersion osrNativeCodeVersion;
    {
        CodeVersionManager::LockHolder codeVersioningLockHolder;

        NativeCodeVersion currentNativeCodeVersion = codeInfo.GetNativeCodeVersion();
        ILCodeVersion ilCodeVersion = currentNativeCodeVersion.GetILCodeVersion();
        HRESULT hr = ilCodeVersion.AddNativeCodeVersion(pMD, NativeCodeVersion::OptimizationTier1OSR, &osrNativeCodeVersion, patchpointInfo, ilOffset);
        if (FAILED(hr))
        {
            // Unexpected, but not fatal
            STRESS_LOG1(LF_TIEREDCOMPILATION, LL_WARNING, "JitPatchpointWorker: failed to add native code version for Method=0x%pM\n", pMD);
            return NULL;
        }
    }

    // Invoke the jit to compile the OSR version
    LOG((LF_TIEREDCOMPILATION, LL_INFO10, "JitPatchpointWorker: creating OSR version of Method=0x%pM (%s::%s) at offset %d\n",
        pMD, pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, ilOffset));

    PrepareCodeConfigBuffer configBuffer(osrNativeCodeVersion);
    PrepareCodeConfig *config = configBuffer.GetConfig();
    osrVariant = pMD->PrepareCode(config);

    return osrVariant;
}

// Helper method wrapper to set up a frame so we can invoke methods that might GC
HCIMPL3(PCODE, JIT_Patchpoint_Framed, MethodDesc* pMD, EECodeInfo& codeInfo, int ilOffset)
{
    PCODE result = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    GCX_PREEMP();
    result = JitPatchpointWorker(pMD, codeInfo, ilOffset);

    HELPER_METHOD_FRAME_END();

    return result;
}
HCIMPLEND

// Jit helper invoked at a patchpoint.
//
// Checks to see if this is a known patchpoint, if not,
// an entry is added to the patchpoint table.
//
// When the patchpoint has been hit often enough to trigger
// a transition, create an OSR method.
//
// Currently, counter is a pointer into the Tier0 method stack
// frame so we have exclusive access.

void JIT_Patchpoint(int* counter, int ilOffset)
{
    // BEGIN_PRESERVE_LAST_ERROR;
    DWORD dwLastError = ::GetLastError();

    // This method may not return normally
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    // Patchpoint identity is the helper return address
    PCODE ip = (PCODE)_ReturnAddress();

    // Fetch or setup patchpoint info for this patchpoint.
    EECodeInfo codeInfo(ip);
    MethodDesc* pMD = codeInfo.GetMethodDesc();
    LoaderAllocator* allocator = pMD->GetLoaderAllocator();
    OnStackReplacementManager* manager = allocator->GetOnStackReplacementManager();
    PerPatchpointInfo * ppInfo = manager->GetPerPatchpointInfo(ip);
    PCODE osrMethodCode = NULL;
    bool isNewMethod = false;

    // In the current prototype, counter is shared by all patchpoints
    // in a method, so no matter what happens below, we don't want to
    // impair those other patchpoints.
    //
    // One might be tempted, for instance, to set the counter for
    // invalid or ignored patchpoints to some high value to reduce
    // the amount of back and forth with the runtime, but this would
    // lock out other patchpoints in the method.
    //
    // So we always reset the counter to the bump value.
    //
    // In the prototype, counter is a location in a stack frame,
    // so we can update it without worrying about other threads.
    const int counterBump = g_pConfig->OSR_CounterBump();
    *counter = counterBump;

#if _DEBUG
    const int ppId = ppInfo->m_patchpointId;
#endif

    // Is this a patchpoint that was previously marked as invalid? If so, just return to the Tier0 method.
    if ((ppInfo->m_flags & PerPatchpointInfo::patchpoint_invalid) == PerPatchpointInfo::patchpoint_invalid)
    {
        LOG((LF_TIEREDCOMPILATION, LL_INFO1000, "Jit_Patchpoint: invalid patchpoint [%d] (0x%p) in Method=0x%pM (%s::%s) at offset %d\n",
                ppId, ip, pMD, pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, ilOffset));

        goto DONE;
    }

    // See if we have an OSR method for this patchpoint.
    osrMethodCode = ppInfo->m_osrMethodCode;

    if (osrMethodCode == NULL)
    {
        // No OSR method yet, let's see if we should create one.
        //
        // First, optionally ignore some patchpoints to increase
        // coverage (stress mode).
        //
        // Because there are multiple patchpoints in a method, and
        // each OSR method covers the remainder of the method from
        // that point until the method returns, if we trigger on an
        // early patchpoint in a method, we may never see triggers on
        // a later one.

#ifdef _DEBUG
        const int lowId = g_pConfig->OSR_LowId();
        const int highId = g_pConfig->OSR_HighId();

        if ((ppId < lowId) || (ppId > highId))
        {
            LOG((LF_TIEREDCOMPILATION, LL_INFO10, "Jit_Patchpoint: ignoring patchpoint [%d] (0x%p) in Method=0x%pM (%s::%s) at offset %d\n",
                    ppId, ip, pMD, pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, ilOffset));
            goto DONE;
        }
#endif

        // Second, only request the OSR method if this patchpoint has
        // been hit often enough.
        //
        // Note the initial invocation of the helper depends on the
        // initial counter value baked into jitted code (call this J);
        // subsequent invocations depend on the counter bump (call
        // this B).
        //
        // J and B may differ, so the total number of loop iterations
        // before an OSR method is created is:
        //
        // J, if hitLimit <= 1;
        // J + (hitLimit-1)* B, if hitLimit > 1;
        //
        // Current thinking is:
        //
        // J should be in the range of tens to hundreds, so that newly
        // called Tier0 methods that already have OSR methods
        // available can transition to OSR methods quickly, but
        // methods called only a few times do not invoke this
        // helper and so create PerPatchpoint runtime state.
        //
        // B should be in the range of hundreds to thousands, so that
        // we're not too eager to create OSR methods (since there is
        // some jit cost), but are eager enough to transition before
        // we run too much Tier0 code.
        //
        const int hitLimit = g_pConfig->OSR_HitLimit();
        const int hitCount = InterlockedIncrement(&ppInfo->m_patchpointCount);
        const int hitLogLevel = (hitCount == 1) ? LL_INFO10 : LL_INFO1000;

        LOG((LF_TIEREDCOMPILATION, hitLogLevel, "Jit_Patchpoint: patchpoint [%d] (0x%p) hit %d in Method=0x%pM (%s::%s) [il offset %d] (limit %d)\n",
            ppId, ip, hitCount, pMD, pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, ilOffset, hitLimit));

        // Defer, if we haven't yet reached the limit
        if (hitCount < hitLimit)
        {
            goto DONE;
        }

        // Third, make sure no other thread is trying to create the OSR method.
        LONG oldFlags = ppInfo->m_flags;
        if ((oldFlags & PerPatchpointInfo::patchpoint_triggered) == PerPatchpointInfo::patchpoint_triggered)
        {
            LOG((LF_TIEREDCOMPILATION, LL_INFO1000, "Jit_Patchpoint: AWAITING OSR method for patchpoint [%d] (0x%p)\n", ppId, ip));
            goto DONE;
        }

        LONG newFlags = oldFlags | PerPatchpointInfo::patchpoint_triggered;
        BOOL triggerTransition = InterlockedCompareExchange(&ppInfo->m_flags, newFlags, oldFlags) == oldFlags;

        if (!triggerTransition)
        {
            LOG((LF_TIEREDCOMPILATION, LL_INFO1000, "Jit_Patchpoint: (lost race) AWAITING OSR method for patchpoint [%d] (0x%p)\n", ppId, ip));
            goto DONE;
        }

        // Time to create the OSR method.
        //
        // We currently do this synchronously. We could instead queue
        // up a request on some worker thread, like we do for
        // rejitting, and return control to the Tier0 method. It may
        // eventually return here, if the patchpoint is hit often
        // enough.
        //
        // There is a chance the async version will create methods
        // that are never used (just like there is a chance that Tier1
        // methods are ever called).
        //
        // In this prototype we want to expose bugs in the jitted code
        // for OSR methods, so we stick with synchronous creation.
        LOG((LF_TIEREDCOMPILATION, LL_INFO10, "Jit_Patchpoint: patchpoint [%d] (0x%p) TRIGGER at count %d\n", ppId, ip, hitCount));

        // Invoke the helper to build the OSR method
        osrMethodCode = HCCALL3(JIT_Patchpoint_Framed, pMD, codeInfo, ilOffset);

        // If that failed, mark the patchpoint as invalid.
        if (osrMethodCode == NULL)
        {
            // Unexpected, but not fatal
            STRESS_LOG3(LF_TIEREDCOMPILATION, LL_WARNING, "Jit_Patchpoint: patchpoint (0x%p) OSR method creation failed,"
                " marking patchpoint invalid for Method=0x%pM il offset %d\n", ip, pMD, ilOffset);

            InterlockedOr(&ppInfo->m_flags, (LONG)PerPatchpointInfo::patchpoint_invalid);
            goto DONE;
        }

        // We've successfully created the osr method; make it available.
        _ASSERTE(ppInfo->m_osrMethodCode == NULL);
        ppInfo->m_osrMethodCode = osrMethodCode;
        isNewMethod = true;
    }

    // If we get here, we have code to transition to...
    _ASSERTE(osrMethodCode != NULL);

    {
        Thread *pThread = GetThread();

#ifdef FEATURE_HIJACK
        // We can't crawl the stack of a thread that currently has a hijack pending
        // (since the hijack routine won't be recognized by any code manager). So we
        // Undo any hijack, the EE will re-attempt it later.
        pThread->UnhijackThread();
#endif

        // Find context for the original method
        CONTEXT *pFrameContext = NULL;
#if defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
        DWORD contextSize = 0;
        ULONG64 xStateCompactionMask = 0;
        DWORD contextFlags = CONTEXT_FULL;
        if (Thread::AreCetShadowStacksEnabled())
        {
            xStateCompactionMask = XSTATE_MASK_CET_U;
            contextFlags |= CONTEXT_XSTATE;
        }

        // The initialize call should fail but return contextSize
        BOOL success = g_pfnInitializeContext2 ?
            g_pfnInitializeContext2(NULL, contextFlags, NULL, &contextSize, xStateCompactionMask) :
            InitializeContext(NULL, contextFlags, NULL, &contextSize);

        _ASSERTE(!success && (GetLastError() == ERROR_INSUFFICIENT_BUFFER));

        PVOID pBuffer = _alloca(contextSize);
        success = g_pfnInitializeContext2 ?
            g_pfnInitializeContext2(pBuffer, contextFlags, &pFrameContext, &contextSize, xStateCompactionMask) :
            InitializeContext(pBuffer, contextFlags, &pFrameContext, &contextSize);
        _ASSERTE(success);
#else // TARGET_WINDOWS && TARGET_AMD64
        CONTEXT frameContext;
        frameContext.ContextFlags = CONTEXT_FULL;
        pFrameContext = &frameContext;
#endif // TARGET_WINDOWS && TARGET_AMD64

        // Find context for the original method
        RtlCaptureContext(pFrameContext);

#if defined(TARGET_WINDOWS) && defined(TARGET_AMD64)
        if (Thread::AreCetShadowStacksEnabled())
        {
            pFrameContext->ContextFlags |= CONTEXT_XSTATE;
            SetXStateFeaturesMask(pFrameContext, xStateCompactionMask);
            SetSSP(pFrameContext, _rdsspq());
        }
#endif // TARGET_WINDOWS && TARGET_AMD64

        // Walk back to the original method frame
        pThread->VirtualUnwindToFirstManagedCallFrame(pFrameContext);

        // Remember original method FP and SP because new method will inherit them.
        UINT_PTR currentSP = GetSP(pFrameContext);
        UINT_PTR currentFP = GetFP(pFrameContext);

        // We expect to be back at the right IP
        if ((UINT_PTR)ip != GetIP(pFrameContext))
        {
            // Should be fatal
            STRESS_LOG2(LF_TIEREDCOMPILATION, LL_FATALERROR, "Jit_Patchpoint: patchpoint (0x%p) TRANSITION"
                " unexpected context IP 0x%p\n", ip, GetIP(pFrameContext));
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        }

        // Now unwind back to the original method caller frame.
        EECodeInfo callerCodeInfo(GetIP(pFrameContext));
        ULONG_PTR establisherFrame = 0;
        PVOID handlerData = NULL;
        RtlVirtualUnwind(UNW_FLAG_NHANDLER, callerCodeInfo.GetModuleBase(), GetIP(pFrameContext), callerCodeInfo.GetFunctionEntry(),
            pFrameContext, &handlerData, &establisherFrame, NULL);

        // Now, set FP and SP back to the values they had just before this helper was called,
        // since the new method must have access to the original method frame.
        //
        // TODO: if we access the patchpointInfo here, we can read out the FP-SP delta from there and
        // use that to adjust the stack, likely saving some stack space.

#if defined(TARGET_AMD64)
        // If calls push the return address, we need to simulate that here, so the OSR
        // method sees the "expected" SP misalgnment on entry.
        _ASSERTE(currentSP % 16 == 0);
        currentSP -= 8;

#if defined(TARGET_WINDOWS)
        DWORD64 ssp = GetSSP(pFrameContext);
        if (ssp != 0)
        {
            SetSSP(pFrameContext, ssp - 8);
        }
#endif // TARGET_WINDOWS

        pFrameContext->Rbp = currentFP;
#endif // TARGET_AMD64

        SetSP(pFrameContext, currentSP);

        // Note we can get here w/o triggering, if there is an existing OSR method and
        // we hit the patchpoint.
        const int transitionLogLevel = isNewMethod ? LL_INFO10 : LL_INFO1000;
        LOG((LF_TIEREDCOMPILATION, transitionLogLevel, "Jit_Patchpoint: patchpoint [%d] (0x%p) TRANSITION to ip 0x%p\n", ppId, ip, osrMethodCode));

        // Install new entry point as IP
        SetIP(pFrameContext, osrMethodCode);

        // Restore last error (since call below does not return)
        // END_PRESERVE_LAST_ERROR;
        ::SetLastError(dwLastError);

        // Transition!
        ClrRestoreNonvolatileContext(pFrameContext);
    }

 DONE:

    // END_PRESERVE_LAST_ERROR;
    ::SetLastError(dwLastError);
}

// Jit helper invoked at a partial compilation patchpoint.
//
// Similar to Jit_Patchpoint, but invoked when execution
// reaches a point in a method where the continuation
// was never jitted (eg an exceptional path).
//
// Unlike regular patchpoints, partial compilation patchpoints
// must always transition.
//
HCIMPL1(VOID, JIT_PartialCompilationPatchpoint, int ilOffset)
{
    FCALL_CONTRACT;

    // BEGIN_PRESERVE_LAST_ERROR;
    DWORD dwLastError = ::GetLastError();
    PerPatchpointInfo* ppInfo = NULL;
    bool isNewMethod = false;
    CONTEXT frameContext;

    // Patchpoint identity is the helper return address
    PCODE ip = (PCODE)_ReturnAddress();

#if _DEBUG
    // Friendly ID number
    int ppId = 0;
#endif

    HELPER_METHOD_FRAME_BEGIN_0();

    // Fetch or setup patchpoint info for this patchpoint.
    EECodeInfo codeInfo(ip);
    MethodDesc* pMD = codeInfo.GetMethodDesc();
    LoaderAllocator* allocator = pMD->GetLoaderAllocator();
    OnStackReplacementManager* manager = allocator->GetOnStackReplacementManager();
    ppInfo = manager->GetPerPatchpointInfo(ip);

#if _DEBUG
    ppId = ppInfo->m_patchpointId;
#endif

    // See if we have an OSR method for this patchpoint.
    DWORD backoffs = 0;

    // Enable GC while we jit or wait for the continuation to be jitted.
    {
        GCX_PREEMP();

        while (ppInfo->m_osrMethodCode == NULL)
        {
            // Invalid patchpoints are fatal, for partial compilation patchpoints
            //
            if ((ppInfo->m_flags & PerPatchpointInfo::patchpoint_invalid) == PerPatchpointInfo::patchpoint_invalid)
            {
                LOG((LF_TIEREDCOMPILATION, LL_FATALERROR, "Jit_PartialCompilationPatchpoint: invalid patchpoint [%d] (0x%p) in Method=0x%pM (%s::%s) at offset %d\n",
                        ppId, ip, pMD, pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, ilOffset));
                EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
            }

            // Make sure no other thread is trying to create the OSR method.
            //
            LONG oldFlags = ppInfo->m_flags;
            if ((oldFlags & PerPatchpointInfo::patchpoint_triggered) == PerPatchpointInfo::patchpoint_triggered)
            {
                LOG((LF_TIEREDCOMPILATION, LL_INFO1000, "Jit_PartialCompilationPatchpoint: AWAITING OSR method for patchpoint [%d] (0x%p)\n", ppId, ip));
                __SwitchToThread(0, backoffs++);
                continue;
            }

            // Make sure we win the race to create the OSR method
            //
            LONG newFlags = ppInfo->m_flags | PerPatchpointInfo::patchpoint_triggered;
            BOOL triggerTransition = InterlockedCompareExchange(&ppInfo->m_flags, newFlags, oldFlags) == oldFlags;

            if (!triggerTransition)
            {
                LOG((LF_TIEREDCOMPILATION, LL_INFO1000, "Jit_PartialCompilationPatchpoint: (lost race) AWAITING OSR method for patchpoint [%d] (0x%p)\n", ppId, ip));
                __SwitchToThread(0, backoffs++);
                continue;
            }

            // Invoke the helper to build the OSR method
            //
            // TODO: may not want to optimize this part of the method, if it's truly partial compilation
            // and can't possibly rejoin into the main flow.
            //
            // (but consider: throw path in method with try/catch, OSR method will contain more than just the throw?)
            //
            LOG((LF_TIEREDCOMPILATION, LL_INFO10, "Jit_PartialCompilationPatchpoint: patchpoint [%d] (0x%p) TRIGGER\n", ppId, ip));
            PCODE newMethodCode = JitPatchpointWorker(pMD, codeInfo, ilOffset);

            // If that failed, mark the patchpoint as invalid.
            // This is fatal, for partial compilation patchpoints
            //
            if (newMethodCode == NULL)
            {
                STRESS_LOG3(LF_TIEREDCOMPILATION, LL_WARNING, "Jit_PartialCompilationPatchpoint: patchpoint (0x%p) OSR method creation failed,"
                    " marking patchpoint invalid for Method=0x%pM il offset %d\n", ip, pMD, ilOffset);
                InterlockedOr(&ppInfo->m_flags, (LONG)PerPatchpointInfo::patchpoint_invalid);
                EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
                break;
            }

            // We've successfully created the osr method; make it available.
            _ASSERTE(ppInfo->m_osrMethodCode == NULL);
            ppInfo->m_osrMethodCode = newMethodCode;
            isNewMethod = true;
        }
    }

    // If we get here, we have code to transition to...
    PCODE osrMethodCode = ppInfo->m_osrMethodCode;
    _ASSERTE(osrMethodCode != NULL);

    Thread *pThread = GetThread();

#ifdef FEATURE_HIJACK
    // We can't crawl the stack of a thread that currently has a hijack pending
    // (since the hijack routine won't be recognized by any code manager). So we
    // Undo any hijack, the EE will re-attempt it later.
    pThread->UnhijackThread();
#endif

    // Find context for the original method
    frameContext.ContextFlags = CONTEXT_FULL;
    RtlCaptureContext(&frameContext);

    // Walk back to the original method frame
    pThread->VirtualUnwindToFirstManagedCallFrame(&frameContext);

    // Remember original method FP and SP because new method will inherit them.
    UINT_PTR currentSP = GetSP(&frameContext);
    UINT_PTR currentFP = GetFP(&frameContext);

    // We expect to be back at the right IP
    if ((UINT_PTR)ip != GetIP(&frameContext))
    {
        // Should be fatal
        STRESS_LOG2(LF_TIEREDCOMPILATION, LL_INFO10, "Jit_PartialCompilationPatchpoint: patchpoint (0x%p) TRANSITION"
            " unexpected context IP 0x%p\n", ip, GetIP(&frameContext));
        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
    }

    // Now unwind back to the original method caller frame.
    EECodeInfo callerCodeInfo(GetIP(&frameContext));
    frameContext.ContextFlags = CONTEXT_FULL;
    ULONG_PTR establisherFrame = 0;
    PVOID handlerData = NULL;
    RtlVirtualUnwind(UNW_FLAG_NHANDLER, callerCodeInfo.GetModuleBase(), GetIP(&frameContext), callerCodeInfo.GetFunctionEntry(),
        &frameContext, &handlerData, &establisherFrame, NULL);

    // Now, set FP and SP back to the values they had just before this helper was called,
    // since the new method must have access to the original method frame.
    //
    // TODO: if we access the patchpointInfo here, we can read out the FP-SP delta from there and
    // use that to adjust the stack, likely saving some stack space.

#if defined(TARGET_AMD64)
    // If calls push the return address, we need to simulate that here, so the OSR
    // method sees the "expected" SP misalgnment on entry.
    _ASSERTE(currentSP % 16 == 0);
    currentSP -= 8;
#endif

    SetSP(&frameContext, currentSP);

#if defined(TARGET_AMD64)
    frameContext.Rbp = currentFP;
#endif

    // Note we can get here w/o triggering, if there is an existing OSR method and
    // we hit the patchpoint.
    const int transitionLogLevel = isNewMethod ? LL_INFO10 : LL_INFO1000;
    LOG((LF_TIEREDCOMPILATION, transitionLogLevel, "Jit_PartialCompilationPatchpoint: patchpoint [%d] (0x%p) TRANSITION to ip 0x%p\n", ppId, ip, osrMethodCode));

    // Install new entry point as IP
    SetIP(&frameContext, osrMethodCode);

    // This method doesn't return normally so we have to manually restore things.
    HELPER_METHOD_FRAME_END();
    ENDFORBIDGC();
    ::SetLastError(dwLastError);

    // Transition!
    __asan_handle_no_return();
    RtlRestoreContext(&frameContext, NULL);
}
HCIMPLEND

#else

void JIT_Patchpoint(int* counter, int ilOffset)
{
    // Stub version if OSR feature is disabled
    //
    // Should not be called.

    UNREACHABLE();
}

HCIMPL1(VOID, JIT_PartialCompilationPatchpoint, int ilOffset)
{
    // Stub version if OSR feature is disabled
    //
    // Should not be called.

    UNREACHABLE();
}
HCIMPLEND

#endif // FEATURE_ON_STACK_REPLACEMENT

static unsigned HandleHistogramProfileRand()
{
    // Generate a random number (xorshift32)
    //
    // Intentionally simple for faster random. It's stored in TLS to avoid
    // multithread contention.
    //
    static thread_local unsigned s_rng = 100;

    unsigned x = s_rng;
    x ^= x << 13;
    x ^= x >> 17;
    x ^= x << 5;
    s_rng = x;
    return x;
}

template<typename T>
FORCEINLINE static bool CheckSample(T* pIndex, size_t* sampleIndex)
{
    const unsigned S = ICorJitInfo::HandleHistogram32::SIZE;
    const unsigned N = ICorJitInfo::HandleHistogram32::SAMPLE_INTERVAL;
    static_assert_no_msg(N >= S);
    static_assert_no_msg((std::is_same<T, uint32_t>::value || std::is_same<T, uint64_t>::value));

    // If table is not yet full, just add entries in
    // and increment the table index.
    //
    T const index = *pIndex;

    if (index < S)
    {
        *sampleIndex = static_cast<size_t>(index);
        *pIndex = index + 1;
        return true;
    }

    unsigned const x = HandleHistogramProfileRand();

    // N is the sampling window size,
    // it should be larger than the table size.
    //
    // If we let N == count then we are building an entire
    // run sample -- probability of update decreases over time.
    // Would be a good strategy for an AOT profiler.
    //
    // But for TieredPGO we would prefer something that is more
    // weighted to recent observations.
    //
    // For S=4, N=128, we'll sample (on average) every 32nd call.
    //
    if ((x % N) >= S)
    {
        return false;
    }

    *sampleIndex = static_cast<size_t>(x % S);
    return true;
}

HCIMPL2(void, JIT_ValueProfile32, intptr_t val, ICorJitInfo::ValueHistogram32* valueProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    size_t sampleIndex;
    if (!CheckSample(&valueProfile->Count, &sampleIndex))
    {
        return;
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(valueProfile);
    PgoManager::VerifyAddress(valueProfile + 1);
#endif

    valueProfile->ValueTable[sampleIndex] = val;
}
HCIMPLEND

HCIMPL2(void, JIT_ValueProfile64, intptr_t val, ICorJitInfo::ValueHistogram64* valueProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    size_t sampleIndex;
    if (!CheckSample(&valueProfile->Count, &sampleIndex))
    {
        return;
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(valueProfile);
    PgoManager::VerifyAddress(valueProfile + 1);
#endif

    valueProfile->ValueTable[sampleIndex] = val;
}
HCIMPLEND

HCIMPL2(void, JIT_ClassProfile32, Object *obj, ICorJitInfo::HandleHistogram32* classProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(objRef);

    size_t sampleIndex;
    if (!CheckSample(&classProfile->Count, &sampleIndex) || objRef == NULL)
    {
        return;
    }

    MethodTable* pMT = objRef->GetMethodTable();

    // If the object class is collectible, record an unknown typehandle.
    // We do this instead of recording NULL so that we won't over-estimate
    // the likelihood of known type handles.
    //
    if (pMT->Collectible())
    {
        pMT = (MethodTable*)DEFAULT_UNKNOWN_HANDLE;
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(classProfile);
    PgoManager::VerifyAddress(classProfile + 1);
#endif

    classProfile->HandleTable[sampleIndex] = (CORINFO_CLASS_HANDLE)pMT;
}
HCIMPLEND

// Version of helper above used when the count is 64-bit
HCIMPL2(void, JIT_ClassProfile64, Object *obj, ICorJitInfo::HandleHistogram64* classProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(objRef);

    size_t sampleIndex;
    if (!CheckSample(&classProfile->Count, &sampleIndex) || objRef == NULL)
    {
        return;
    }

    MethodTable* pMT = objRef->GetMethodTable();

    if (pMT->Collectible())
    {
        pMT = (MethodTable*)DEFAULT_UNKNOWN_HANDLE;
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(classProfile);
    PgoManager::VerifyAddress(classProfile + 1);
#endif

    classProfile->HandleTable[sampleIndex] = (CORINFO_CLASS_HANDLE)pMT;
}
HCIMPLEND

HCIMPL2(void, JIT_DelegateProfile32, Object *obj, ICorJitInfo::HandleHistogram32* methodProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(objRef);

    size_t methodSampleIndex;
    if (!CheckSample(&methodProfile->Count, &methodSampleIndex) || objRef == NULL)
    {
        return;
    }

    MethodTable* pMT = objRef->GetMethodTable();

    _ASSERTE(pMT->IsDelegate());

    // Resolve method. We handle only the common "direct" delegate as that is
    // in any case the only one we can reasonably do GDV for. For instance,
    // open delegates are filtered out here, and many cases with inner
    // "complicated" logic as well (e.g. static functions, multicast, unmanaged
    // functions).
    //
    MethodDesc* pRecordedMD = (MethodDesc*)DEFAULT_UNKNOWN_HANDLE;
    DELEGATEREF del = (DELEGATEREF)objRef;
    if ((del->GetInvocationCount() == 0) && (del->GetMethodPtrAux() == NULL))
    {
        MethodDesc* pMD = NonVirtualEntry2MethodDesc(del->GetMethodPtr());
        if ((pMD != nullptr) && !pMD->GetLoaderAllocator()->IsCollectible() && !pMD->IsDynamicMethod())
        {
            pRecordedMD = pMD;
        }
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(methodProfile);
    PgoManager::VerifyAddress(methodProfile + 1);
#endif

    // If table is not yet full, just add entries in.
    //
    methodProfile->HandleTable[methodSampleIndex] = (CORINFO_METHOD_HANDLE)pRecordedMD;
}
HCIMPLEND

// Version of helper above used when the count is 64-bit
HCIMPL2(void, JIT_DelegateProfile64, Object *obj, ICorJitInfo::HandleHistogram64* methodProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(objRef);

    size_t methodSampleIndex;
    if (!CheckSample(&methodProfile->Count, &methodSampleIndex) || objRef == NULL)
    {
        return;
    }

    MethodTable* pMT = objRef->GetMethodTable();

    _ASSERTE(pMT->IsDelegate());

    // Resolve method. We handle only the common "direct" delegate as that is
    // in any case the only one we can reasonably do GDV for. For instance,
    // open delegates are filtered out here, and many cases with inner
    // "complicated" logic as well (e.g. static functions, multicast, unmanaged
    // functions).
    //
    MethodDesc* pRecordedMD = (MethodDesc*)DEFAULT_UNKNOWN_HANDLE;
    DELEGATEREF del = (DELEGATEREF)objRef;
    if ((del->GetInvocationCount() == 0) && (del->GetMethodPtrAux() == NULL))
    {
        MethodDesc* pMD = NonVirtualEntry2MethodDesc(del->GetMethodPtr());
        if ((pMD != nullptr) && !pMD->GetLoaderAllocator()->IsCollectible() && !pMD->IsDynamicMethod())
        {
            pRecordedMD = pMD;
        }
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(methodProfile);
    PgoManager::VerifyAddress(methodProfile + 1);
#endif

    // If table is not yet full, just add entries in.
    //
    methodProfile->HandleTable[methodSampleIndex] = (CORINFO_METHOD_HANDLE)pRecordedMD;
}
HCIMPLEND

HCIMPL3(void, JIT_VTableProfile32, Object* obj, CORINFO_METHOD_HANDLE baseMethod, ICorJitInfo::HandleHistogram32* methodProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(objRef);

    size_t methodSampleIndex;
    if (!CheckSample(&methodProfile->Count, &methodSampleIndex) || objRef == NULL)
    {
        return;
    }

    MethodDesc* pBaseMD = GetMethod(baseMethod);

    // Method better be virtual
    _ASSERTE(pBaseMD->IsVirtual());

    // We do not expect to see interface methods here as we cannot efficiently
    // use method handle information for these anyway.
    _ASSERTE(!pBaseMD->IsInterface());

    // Shouldn't be doing this for instantiated methods as they live elsewhere
    _ASSERTE(!pBaseMD->HasMethodInstantiation());

    MethodTable* pMT = objRef->GetMethodTable();

    // Resolve method
    WORD slot = pBaseMD->GetSlot();
    _ASSERTE(slot < pBaseMD->GetMethodTable()->GetNumVirtuals());

    MethodDesc* pMD = pMT->GetMethodDescForSlot(slot);

    MethodDesc* pRecordedMD = (MethodDesc*)DEFAULT_UNKNOWN_HANDLE;
    if (!pMD->GetLoaderAllocator()->IsCollectible() && !pMD->IsDynamicMethod())
    {
        pRecordedMD = pMD;
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(methodProfile);
    PgoManager::VerifyAddress(methodProfile + 1);
#endif

    methodProfile->HandleTable[methodSampleIndex] = (CORINFO_METHOD_HANDLE)pRecordedMD;
}
HCIMPLEND

HCIMPL3(void, JIT_VTableProfile64, Object* obj, CORINFO_METHOD_HANDLE baseMethod, ICorJitInfo::HandleHistogram64* methodProfile)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    VALIDATEOBJECTREF(objRef);

    size_t methodSampleIndex;
    if (!CheckSample(&methodProfile->Count, &methodSampleIndex) || objRef == NULL)
    {
        return;
    }

    MethodDesc* pBaseMD = GetMethod(baseMethod);

    // Method better be virtual
    _ASSERTE(pBaseMD->IsVirtual());

    // We do not expect to see interface methods here as we cannot efficiently
    // use method handle information for these anyway.
    _ASSERTE(!pBaseMD->IsInterface());

    // Shouldn't be doing this for instantiated methods as they live elsewhere
    _ASSERTE(!pBaseMD->HasMethodInstantiation());

    MethodTable* pMT = objRef->GetMethodTable();

    // Resolve method
    WORD slot = pBaseMD->GetSlot();
    _ASSERTE(slot < pBaseMD->GetMethodTable()->GetNumVirtuals());

    MethodDesc* pMD = pMT->GetMethodDescForSlot(slot);

    MethodDesc* pRecordedMD = (MethodDesc*)DEFAULT_UNKNOWN_HANDLE;
    if (!pMD->GetLoaderAllocator()->IsCollectible() && !pMD->IsDynamicMethod())
    {
        pRecordedMD = pMD;
    }

#ifdef _DEBUG
    PgoManager::VerifyAddress(methodProfile);
    PgoManager::VerifyAddress(methodProfile + 1);
#endif

    methodProfile->HandleTable[methodSampleIndex] = (CORINFO_METHOD_HANDLE)pRecordedMD;
}
HCIMPLEND

// Helpers for scalable approximate counters
//
// Here threshold = 13 means we count accurately up to 2^13 = 8192 and
// then start counting probabilistically.
//
// See docs/design/features/ScalableApproximateCounting.md
//
HCIMPL1(void, JIT_CountProfile32, volatile LONG* pCounter)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    LONG count = *pCounter;
    LONG delta = 1;
    DWORD threshold = g_pConfig->TieredPGO_ScalableCountThreshold();

    if (count > 0)
    {
        DWORD logCount = 0;
        BitScanReverse(&logCount, count);

        if (logCount >= threshold)
        {
            delta = 1 << (logCount - (threshold - 1));
            const unsigned rand = HandleHistogramProfileRand();
            const bool update = (rand & (delta - 1)) == 0;
            if (!update)
            {
                return;
            }
        }
    }

    InterlockedAdd(pCounter, delta);
}
HCIMPLEND

HCIMPL1(void, JIT_CountProfile64, volatile LONG64* pCounter)
{
    FCALL_CONTRACT;
    FC_GC_POLL_NOT_NEEDED();

    LONG64 count = *pCounter;
    LONG64 delta = 1;
    DWORD threshold = g_pConfig->TieredPGO_ScalableCountThreshold();

    if (count > 0)
    {
        DWORD logCount = 0;
        BitScanReverse64(&logCount, count);

        if (logCount >= threshold)
        {
            delta = 1LL << (logCount - (threshold - 1));
            const unsigned rand = HandleHistogramProfileRand();
            const bool update = (rand & (delta - 1)) == 0;
            if (!update)
            {
                return;
            }
        }
    }

    InterlockedAdd64(pCounter, delta);
}
HCIMPLEND

//========================================================================
//
//      INTEROP HELPERS
//
//========================================================================

#ifdef HOST_64BIT

/**********************************************************************/
/* Fills out portions of an InlinedCallFrame for JIT64    */
/* The idea here is to allocate and initialize the frame to only once, */
/* regardless of how many PInvokes there are in the method            */
Thread * __stdcall JIT_InitPInvokeFrame(InlinedCallFrame *pFrame, PTR_VOID StubSecretArg)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    Thread *pThread = GetThread();

    // The JIT messed up and is initializing a frame that is already live on the stack?!?!?!?!
    _ASSERTE(pFrame != pThread->GetFrame());

    pFrame->Init();
    pFrame->m_StubSecretArg = StubSecretArg;
    pFrame->m_Next = pThread->GetFrame();

    return pThread;
}

#endif // HOST_64BIT

EXTERN_C void JIT_PInvokeBegin(InlinedCallFrame* pFrame);
EXTERN_C void JIT_PInvokeEnd(InlinedCallFrame* pFrame);

// Forward declaration
EXTERN_C void STDCALL ReversePInvokeBadTransition();

#ifndef FEATURE_EH_FUNCLETS
EXCEPTION_HANDLER_DECL(FastNExportExceptHandler);
#endif

// This is a slower version of the reverse PInvoke enter function.
NOINLINE static void JIT_ReversePInvokeEnterRare(ReversePInvokeFrame* frame, void* returnAddr, UMEntryThunk* pThunk = NULL)
{
    _ASSERTE(frame != NULL);

    Thread* thread = GetThreadNULLOk();
    if (thread == NULL)
        CREATETHREAD_IF_NULL_FAILFAST(thread, W("Failed to setup new thread during reverse P/Invoke"));

    // Verify the current thread isn't in COOP mode.
    if (thread->PreemptiveGCDisabled())
        ReversePInvokeBadTransition();

    frame->currentThread = thread;

#ifdef PROFILING_SUPPORTED
        if (CORProfilerTrackTransitions())
        {
            ProfilerUnmanagedToManagedTransitionMD(frame->pMD, COR_PRF_TRANSITION_CALL);
        }
#endif

    thread->DisablePreemptiveGC();
#ifdef DEBUGGING_SUPPORTED
    // If the debugger is attached, we use this opportunity to see if
    // we're disabling preemptive GC on the way into the runtime from
    // unmanaged code. We end up here because
    // Increment/DecrementTraceCallCount() will bump
    // g_TrapReturningThreads for us.
    if (CORDebuggerTraceCall())
        g_pDebugInterface->TraceCall(pThunk ? (const BYTE*)pThunk->GetManagedTarget() : (const BYTE*)returnAddr);
#endif // DEBUGGING_SUPPORTED
}

NOINLINE static void JIT_ReversePInvokeEnterRare2(ReversePInvokeFrame* frame, void* returnAddr, UMEntryThunk* pThunk = NULL)
{
    frame->currentThread->RareDisablePreemptiveGC();
#ifdef DEBUGGING_SUPPORTED
    // If the debugger is attached, we use this opportunity to see if
    // we're disabling preemptive GC on the way into the runtime from
    // unmanaged code. We end up here because
    // Increment/DecrementTraceCallCount() will bump
    // g_TrapReturningThreads for us.
    if (CORDebuggerTraceCall())
        g_pDebugInterface->TraceCall(pThunk ? (const BYTE*)pThunk->GetManagedTarget() : (const BYTE*)returnAddr);
#endif // DEBUGGING_SUPPORTED
}

// The following JIT_ReversePInvoke helpers are special.
// They handle setting up Reverse P/Invoke calls and transitioning back to unmanaged code.
// As a result, we may not have a thread in JIT_ReversePInvokeEnter and we will be in the wrong GC mode for the HCALL prolog.
// Additionally, we set up and tear down SEH handlers when we're on x86, so we can't use dynamic contracts anyway.
// As a result, we specially decorate this method to have the correct calling convention
// and argument ordering for an HCALL, but we don't use the HCALL macros and contracts
// since this method doesn't follow the contracts.
HCIMPL3_RAW(void, JIT_ReversePInvokeEnterTrackTransitions, ReversePInvokeFrame* frame, CORINFO_METHOD_HANDLE handle, void* secretArg)
{
    _ASSERTE(frame != NULL && handle != NULL);

    MethodDesc* pMD = GetMethod(handle);
    if (pMD->IsILStub() && secretArg != NULL)
    {
        pMD = ((UMEntryThunk*)secretArg)->GetMethod();
    }
    frame->pMD = pMD;

    Thread* thread = GetThreadNULLOk();

    // If a thread instance exists and is in the
    // correct GC mode attempt a quick transition.
    if (thread != NULL
        && !thread->PreemptiveGCDisabled())
    {
        frame->currentThread = thread;

#ifdef PROFILING_SUPPORTED
        if (CORProfilerTrackTransitions())
        {
            ProfilerUnmanagedToManagedTransitionMD(frame->pMD, COR_PRF_TRANSITION_CALL);
        }
#endif

        // Manually inline the fast path in Thread::DisablePreemptiveGC().
        thread->m_fPreemptiveGCDisabled.StoreWithoutBarrier(1);
        if (g_TrapReturningThreads.LoadWithoutBarrier() != 0)
        {
            // If we're in an IL stub, we want to trace the address of the target method,
            // not the next instruction in the stub.
            JIT_ReversePInvokeEnterRare2(frame, _ReturnAddress(), GetMethod(handle)->IsILStub() ? (UMEntryThunk*)secretArg : (UMEntryThunk*)NULL);
        }
    }
    else
    {
        // If we're in an IL stub, we want to trace the address of the target method,
        // not the next instruction in the stub.
        JIT_ReversePInvokeEnterRare(frame, _ReturnAddress(), GetMethod(handle)->IsILStub() ? (UMEntryThunk*)secretArg  : (UMEntryThunk*)NULL);
    }

#ifndef FEATURE_EH_FUNCLETS
    frame->record.m_pEntryFrame = frame->currentThread->GetFrame();
    frame->record.m_ExReg.Handler = (PEXCEPTION_ROUTINE)FastNExportExceptHandler;
    INSTALL_EXCEPTION_HANDLING_RECORD(&frame->record.m_ExReg);
#endif
}
HCIMPLEND_RAW

HCIMPL1_RAW(void, JIT_ReversePInvokeEnter, ReversePInvokeFrame* frame)
{
    _ASSERTE(frame != NULL);

    Thread* thread = GetThreadNULLOk();

    // If a thread instance exists and is in the
    // correct GC mode attempt a quick transition.
    if (thread != NULL
        && !thread->PreemptiveGCDisabled())
    {
        frame->currentThread = thread;

        // Manually inline the fast path in Thread::DisablePreemptiveGC().
        thread->m_fPreemptiveGCDisabled.StoreWithoutBarrier(1);
        if (g_TrapReturningThreads.LoadWithoutBarrier() != 0)
        {
            JIT_ReversePInvokeEnterRare2(frame, _ReturnAddress());
        }
    }
    else
    {
        JIT_ReversePInvokeEnterRare(frame, _ReturnAddress());
    }

#ifndef FEATURE_EH_FUNCLETS
    frame->record.m_pEntryFrame = frame->currentThread->GetFrame();
    frame->record.m_ExReg.Handler = (PEXCEPTION_ROUTINE)FastNExportExceptHandler;
    INSTALL_EXCEPTION_HANDLING_RECORD(&frame->record.m_ExReg);
#endif
}
HCIMPLEND_RAW

HCIMPL1_RAW(void, JIT_ReversePInvokeExitTrackTransitions, ReversePInvokeFrame* frame)
{
    _ASSERTE(frame != NULL);
    _ASSERTE(frame->currentThread == GetThread());

    // Manually inline the fast path in Thread::EnablePreemptiveGC().
    // This is a trade off with GC suspend performance. We are opting
    // to make this exit faster.
    frame->currentThread->m_fPreemptiveGCDisabled.StoreWithoutBarrier(0);

#ifndef FEATURE_EH_FUNCLETS
    UNINSTALL_EXCEPTION_HANDLING_RECORD(&frame->record.m_ExReg);
#endif

#ifdef PROFILING_SUPPORTED
    if (CORProfilerTrackTransitions())
    {
        ProfilerManagedToUnmanagedTransitionMD(frame->pMD, COR_PRF_TRANSITION_RETURN);
    }
#endif
}
HCIMPLEND_RAW

HCIMPL1_RAW(void, JIT_ReversePInvokeExit, ReversePInvokeFrame* frame)
{
    _ASSERTE(frame != NULL);
    _ASSERTE(frame->currentThread == GetThread());

    // Manually inline the fast path in Thread::EnablePreemptiveGC().
    // This is a trade off with GC suspend performance. We are opting
    // to make this exit faster.
    frame->currentThread->m_fPreemptiveGCDisabled.StoreWithoutBarrier(0);

#ifndef FEATURE_EH_FUNCLETS
    UNINSTALL_EXCEPTION_HANDLING_RECORD(&frame->record.m_ExReg);
#endif
}
HCIMPLEND_RAW

// These two do take args but have a custom calling convention.
EXTERN_C void JIT_ValidateIndirectCall();
EXTERN_C void JIT_DispatchIndirectCall();

//========================================================================
//
//      JIT HELPERS INITIALIZATION
//
//========================================================================

// verify consistency of jithelpers.h and corinfo.h
enum __CorInfoHelpFunc {
#define JITHELPER(code, pfnHelper, sig) __##code,
#include "jithelpers.h"
};
#define JITHELPER(code, pfnHelper, sig) C_ASSERT((int)__##code == (int)code);
#include "jithelpers.h"

#ifdef _DEBUG
#define HELPERDEF(code, lpv, sig) { (LPVOID)(lpv), #code },
#else // !_DEBUG
#define HELPERDEF(code, lpv, sig) { (LPVOID)(lpv) },
#endif // !_DEBUG

// static helpers - constant array
const VMHELPDEF hlpFuncTable[CORINFO_HELP_COUNT] =
{
#define JITHELPER(code, pfnHelper, sig) HELPERDEF(code, pfnHelper,sig)
#define DYNAMICJITHELPER(code, pfnHelper,sig) HELPERDEF(code, 1 + DYNAMIC_##code, sig)
#include "jithelpers.h"
};

// dynamic helpers - filled in at runtime
VMHELPDEF hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_COUNT] =
{
#define JITHELPER(code, pfnHelper, sig)
#define DYNAMICJITHELPER(code, pfnHelper, sig) HELPERDEF(DYNAMIC_ ## code, pfnHelper, sig)
#include "jithelpers.h"
};

#if defined(_DEBUG) && (defined(TARGET_AMD64) || defined(TARGET_X86)) && !defined(TARGET_UNIX)
#define HELPERCOUNTDEF(lpv) { (LPVOID)(lpv), NULL, 0 },

VMHELPCOUNTDEF hlpFuncCountTable[CORINFO_HELP_COUNT+1] =
{
#define JITHELPER(code, pfnHelper, sig) HELPERCOUNTDEF(pfnHelper)
#define DYNAMICJITHELPER(code, pfnHelper, sig) HELPERCOUNTDEF(1 + DYNAMIC_##code)
#include "jithelpers.h"
};
#endif

// Set the JIT helper function in the helper table
// Handles the case where the function does not reside in mscorwks.dll

void    _SetJitHelperFunction(DynamicCorInfoHelpFunc ftnNum, void * pFunc)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(ftnNum < DYNAMIC_CORINFO_HELP_COUNT);

    LOG((LF_JIT, LL_INFO1000000, "Setting JIT dynamic helper %3d (%s) to %p\n",
        ftnNum, hlpDynamicFuncTable[ftnNum].name, pFunc));

    hlpDynamicFuncTable[ftnNum].pfnHelper = (void *) pFunc;
}

/*********************************************************************/
// Initialize the part of the JIT helpers that require much of the
// EE infrastructure to be in place.
/*********************************************************************/
void InitJITHelpers2()
{
    STANDARD_VM_CONTRACT;

#if defined(TARGET_X86) || defined(TARGET_ARM)
    SetJitHelperFunction(CORINFO_HELP_INIT_PINVOKE_FRAME, (void *)GenerateInitPInvokeFrameHelper()->GetEntryPoint());
#endif // TARGET_X86 || TARGET_ARM

    InitJitHelperLogging();

    g_pJitGenericHandleCacheCrst.Init(CrstJitGenericHandleCache, CRST_UNSAFE_COOPGC);

    // Allocate and initialize the generic handle cache
    NewHolder <JitGenericHandleCache> tempGenericHandleCache (new JitGenericHandleCache());
    LockOwner sLock = {&g_pJitGenericHandleCacheCrst, IsOwnerOfCrst};
    if (!tempGenericHandleCache->Init(59, &sLock))
        COMPlusThrowOM();
    g_pJitGenericHandleCache = tempGenericHandleCache.Extract();
}

//========================================================================
//
//      JIT HELPERS LOGGING
//
//========================================================================

#if defined(_DEBUG) && (defined(TARGET_AMD64) || defined(TARGET_X86)) && !defined(TARGET_UNIX)
// *****************************************************************************
//  JitHelperLogging usage:
//      1) Ngen using:
//              DOTNET_HardPrejitEnabled=0
//
//         This allows us to instrument even ngen'd image calls to JIT helpers.
//         Remember to clear the key after ngen-ing and before actually running
//         the app you want to log.
//
//      2) Then set:
//              DOTNET_JitHelperLogging=1
//              DOTNET_LogEnable=1
//              DOTNET_LogLevel=1
//              DOTNET_LogToFile=1
//
//      3) Run the app that you want to log; Results will be in COMPLUS.LOG(.X)
//
//      4) JitHelperLogging=2 and JitHelperLogging=3 result in different output
//         as per code in WriteJitHelperCountToSTRESSLOG() below.
// *****************************************************************************
void WriteJitHelperCountToSTRESSLOG()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    int jitHelperLoggingLevel = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitHelperLogging);
    if (jitHelperLoggingLevel != 0)
    {
        DWORD logFacility, logLevel;

        logFacility = LF_ALL;     //LF_ALL/LL_ALWAYS is okay here only because this logging would normally
        logLevel = LL_ALWAYS;     // would never be turned on at all (used only for performance measurements)

        const int countPos = 60;

        STRESS_LOG0(logFacility, logLevel, "Writing Jit Helper COUNT table to log\n");

        VMHELPCOUNTDEF* hlpFuncCount = hlpFuncCountTable;
        while(hlpFuncCount < (hlpFuncCountTable + CORINFO_HELP_COUNT))
        {
            const char* name;
            LONG count;

            name = hlpFuncCount->helperName;
            count = hlpFuncCount->count;

            int nameLen = 0;
            switch (jitHelperLoggingLevel)
            {
            case 1:
                // This will print a comma separated list:
                // CORINFO_XXX_HELPER, 10
                // CORINFO_YYYY_HELPER, 11
                STRESS_LOG2(logFacility, logLevel, "%s, %d\n", name, count);
                break;

            case 2:
                // This will print a table like:
                // CORINFO_XXX_HELPER                       10
                // CORINFO_YYYY_HELPER                      11
                if (hlpFuncCount->helperName != NULL)
                    nameLen = (int)strlen(name);
                else
                    nameLen = (int)strlen("(null)");

                if (nameLen < countPos)
                {
                    char* buffer = new char[(countPos - nameLen) + 1];
                    memset(buffer, (int)' ', (countPos-nameLen));
                    buffer[(countPos - nameLen)] = '\0';
                    STRESS_LOG3(logFacility, logLevel, "%s%s %d\n", name, buffer, count);
                }
                else
                {
                    STRESS_LOG2(logFacility, logLevel, "%s %d\n", name, count);
                }
                break;

            case 3:
                // This will print out the counts and the address range of the helper (if we know it)
                // CORINFO_XXX_HELPER, 10, (0x12345678 -> 0x12345778)
                // CORINFO_YYYY_HELPER, 11, (0x00011234 -> 0x00012234)
                STRESS_LOG4(logFacility, logLevel, "%s, %d, (0x%p -> 0x%p)\n", name, count, hlpFuncCount->pfnRealHelper, ((LPBYTE)hlpFuncCount->pfnRealHelper + hlpFuncCount->helperSize));
                break;

            default:
                STRESS_LOG1(logFacility, logLevel, "Unsupported JitHelperLogging mode (%d)\n", jitHelperLoggingLevel);
                break;
            }

            hlpFuncCount++;
        }
    }
}
// This will do the work to instrument the JIT helper table.
void InitJitHelperLogging()
{
    STANDARD_VM_CONTRACT;

    if ((CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitHelperLogging) != 0))
    {

#ifdef TARGET_X86
        IMAGE_DOS_HEADER *pDOS = (IMAGE_DOS_HEADER *)GetClrModuleBase();
        _ASSERTE(pDOS->e_magic == VAL16(IMAGE_DOS_SIGNATURE) && pDOS->e_lfanew != 0);

        IMAGE_NT_HEADERS *pNT = (IMAGE_NT_HEADERS*)((LPBYTE)GetClrModuleBase() + VAL32(pDOS->e_lfanew));
#ifdef HOST_64BIT
        _ASSERTE(pNT->Signature == VAL32(IMAGE_NT_SIGNATURE)
            && pNT->FileHeader.SizeOfOptionalHeader == VAL16(sizeof(IMAGE_OPTIONAL_HEADER64))
            && pNT->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR_MAGIC) );
#else
        _ASSERTE(pNT->Signature == VAL32(IMAGE_NT_SIGNATURE)
            && pNT->FileHeader.SizeOfOptionalHeader == VAL16(sizeof(IMAGE_OPTIONAL_HEADER32))
            && pNT->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR_MAGIC) );
#endif
#endif // TARGET_X86

        // Make the static hlpFuncTable read/write for purposes of writing the logging thunks
        DWORD dwOldProtect;
        if (!ClrVirtualProtect((LPVOID)hlpFuncTable, (sizeof(VMHELPDEF) * CORINFO_HELP_COUNT), PAGE_EXECUTE_READWRITE, &dwOldProtect))
        {
            ThrowLastError();
        }

        LoaderHeap* pHeap = SystemDomain::GetGlobalLoaderAllocator()->GetStubHeap();

        // iterate through the jit helper tables replacing helpers with logging thunks
        //
        // NOTE: if NGEN'd images were NGEN'd with hard binding on then static helper
        //       calls will NOT be instrumented.
        VMHELPDEF* hlpFunc = const_cast<VMHELPDEF*>(hlpFuncTable);
        VMHELPCOUNTDEF* hlpFuncCount = hlpFuncCountTable;
        while(hlpFunc < (hlpFuncTable + CORINFO_HELP_COUNT))
        {
            if (hlpFunc->pfnHelper != NULL)
            {
                CPUSTUBLINKER   sl;
                CPUSTUBLINKER*  pSl = &sl;

                if (((size_t)hlpFunc->pfnHelper - 1) > DYNAMIC_CORINFO_HELP_COUNT)
                {
                    // While we're here initialize the table of VMHELPCOUNTDEF
                    // guys with info about this helper
                    hlpFuncCount->pfnRealHelper = hlpFunc->pfnHelper;
                    hlpFuncCount->helperName = hlpFunc->name;
                    hlpFuncCount->count = 0;
#ifdef TARGET_AMD64
                    ULONGLONG           uImageBase;
                    PT_RUNTIME_FUNCTION   pFunctionEntry;
                    pFunctionEntry  = RtlLookupFunctionEntry((ULONGLONG)hlpFunc->pfnHelper, &uImageBase, NULL);

                    if (pFunctionEntry != NULL)
                    {
                        _ASSERTE((uImageBase + pFunctionEntry->BeginAddress) == (ULONGLONG)hlpFunc->pfnHelper);
                        hlpFuncCount->helperSize = pFunctionEntry->EndAddress - pFunctionEntry->BeginAddress;
                    }
                    else
                    {
                        hlpFuncCount->helperSize = 0;
                    }
#else // TARGET_X86
                    // How do I get this for x86?
                    hlpFuncCount->helperSize = 0;
#endif // TARGET_AMD64

                    pSl->EmitJITHelperLoggingThunk(GetEEFuncEntryPoint(hlpFunc->pfnHelper), (LPVOID)hlpFuncCount);
                    Stub* pStub = pSl->Link(pHeap);
                    hlpFunc->pfnHelper = (void*)pStub->GetEntryPoint();
                }
                else
                {
                    _ASSERTE(((size_t)hlpFunc->pfnHelper - 1) >= 0 &&
                             ((size_t)hlpFunc->pfnHelper - 1) < ARRAY_SIZE(hlpDynamicFuncTable));
                    VMHELPDEF* dynamicHlpFunc = &hlpDynamicFuncTable[((size_t)hlpFunc->pfnHelper - 1)];

                    // While we're here initialize the table of VMHELPCOUNTDEF
                    // guys with info about this helper. There is only one table
                    // for the count dudes that contains info about both dynamic
                    // and static helpers.

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:26001) //  "Bounds checked above"
#endif /*_PREFAST_ */
                    hlpFuncCount->pfnRealHelper = dynamicHlpFunc->pfnHelper;
                    hlpFuncCount->helperName = dynamicHlpFunc->name;
                    hlpFuncCount->count = 0;
#ifdef _PREFAST_
#pragma warning(pop)
#endif /*_PREFAST_*/

#ifdef TARGET_AMD64
                    ULONGLONG           uImageBase;
                    PT_RUNTIME_FUNCTION   pFunctionEntry;
                    pFunctionEntry  = RtlLookupFunctionEntry((ULONGLONG)hlpFunc->pfnHelper, &uImageBase, NULL);

                    if (pFunctionEntry != NULL)
                    {
                        _ASSERTE((uImageBase + pFunctionEntry->BeginAddress) == (ULONGLONG)hlpFunc->pfnHelper);
                        hlpFuncCount->helperSize = pFunctionEntry->EndAddress - pFunctionEntry->BeginAddress;
                    }
                    else
                    {
                        // if we can't get a function entry for this we'll just pretend the size is 0
                        hlpFuncCount->helperSize = 0;
                    }
#else // TARGET_X86
                    // Is the address in mscoree.dll at all? (All helpers are in
                    // mscoree.dll)
                    if (dynamicHlpFunc->pfnHelper >= (LPBYTE*)GetClrModuleBase() && dynamicHlpFunc->pfnHelper < (LPBYTE*)GetClrModuleBase() + VAL32(pNT->OptionalHeader.SizeOfImage))
                    {
                        // See note above. How do I get the size on x86 for a static method?
                        hlpFuncCount->helperSize = 0;
                    }
                    else
                    {
                        Stub::RecoverStubAndSize((TADDR)dynamicHlpFunc->pfnHelper, (DWORD*)&hlpFuncCount->helperSize);
                        hlpFuncCount->helperSize -= sizeof(Stub);
                    }

#endif // TARGET_AMD64

                    pSl->EmitJITHelperLoggingThunk(GetEEFuncEntryPoint(dynamicHlpFunc->pfnHelper), (LPVOID)hlpFuncCount);
                    Stub* pStub = pSl->Link(pHeap);
                    dynamicHlpFunc->pfnHelper = (void*)pStub->GetEntryPoint();
                }
            }

            hlpFunc++;
            hlpFuncCount++;
        }

        // Restore original access rights to the static hlpFuncTable
        ClrVirtualProtect((LPVOID)hlpFuncTable, (sizeof(VMHELPDEF) * CORINFO_HELP_COUNT), dwOldProtect, &dwOldProtect);
    }

    return;
}
#endif // _DEBUG && (TARGET_AMD64 || TARGET_X86)
