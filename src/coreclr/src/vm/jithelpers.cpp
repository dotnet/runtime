// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



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
#include "security.h"
#include "dllimport.h"
#include "gcheaputilities.h"
#include "comdelegate.h"
#include "jitperf.h" // to track jit perf
#include "corprof.h"
#include "eeprofinterfaces.h"

#ifndef FEATURE_PAL
// Included for referencing __report_gsfailure
#include "process.h"
#endif // !FEATURE_PAL

#include "perfcounters.h"
#ifdef PROFILING_SUPPORTED
#include "proftoeeinterfaceimpl.h"
#endif
#include "tls.h"
#include "ecall.h"
#include "generics.h"
#include "typestring.h"
#include "stackprobe.h"
#include "typedesc.h"
#include "genericdict.h"
#include "array.h"
#include "debuginfostore.h"
#include "security.h"
#include "safemath.h"
#include "threadstatics.h"

#ifdef FEATURE_PREJIT
#include "compile.h"
#endif

#ifdef HAVE_GCCOVER
#include "gccover.h"
#endif // HAVE_GCCOVER

#include "runtimehandles.h"

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
//      SECURITY HELPERS
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

#if !defined(_TARGET_X86_) || defined(FEATURE_PAL)
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
#endif // !_TARGET_X86_ || FEATURE_PAL

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

#if !defined(BIT64) && !defined(_TARGET_X86_)
/*********************************************************************/
HCIMPL2_VV(UINT64, JIT_LLsh, UINT64 num, int shift)
{
    FCALL_CONTRACT;
    return num << shift;
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(INT64, JIT_LRsh, INT64 num, int shift)
{
    FCALL_CONTRACT;
    return num >> shift;
}
HCIMPLEND

/*********************************************************************/
HCIMPL2_VV(UINT64, JIT_LRsz, UINT64 num, int shift)
{
    FCALL_CONTRACT;
    return num >> shift;
}
HCIMPLEND
#endif // !BIT64 && !_TARGET_X86_

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
#if defined(_TARGET_ARM_) && defined(FEATURE_CORESYSTEM)
        // @ARMTODO: On ARM when building on CoreSystem (where we link against the system CRT) an attempt to
        // use fmod(float, float) fails to link (apparently this is converted to a reference to fmodf, which
        // is not included in the system CRT). Use the double version instead.
        if (fmod(double(integerPart), double(2.0)) == 0.0)
            return integerPart;
#else
        if (fmod(ftype(integerPart), ftype(2.0)) == 0.0)
            return integerPart;
#endif

        // Else return the nearest even integer
        return (ftype)_copysign(ceil(fabs(value+0.5)),
                         value);
    }

    // Otherwise round to closest
    return (ftype)_copysign(floor(fabs(value)+0.5), 
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
#ifdef _TARGET_X86_
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


#if !defined(_TARGET_X86_) || defined(FEATURE_PAL)

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

    if (divisor==0 || !_finite(dividend))
    {
        UINT32 NaN = CLR_NAN_32;
        return *(float *)(&NaN);
    }
    else if (!_finite(divisor) && !_isnan(divisor))
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
    if (divisor==0 || !_finite(dividend))
    {
        UINT64 NaN = CLR_NAN_64; 
        return *(double *)(&NaN);
    }
    else if (!_finite(divisor) && !_isnan(divisor))
    {
        return dividend;
    }
    // else...
    return(fmod(dividend,divisor));
}
HCIMPLEND

#endif // !_TARGET_X86_ || FEATURE_PAL

#include <optdefault.h>


//========================================================================
//
//      INSTANCE FIELD HELPERS
//
//========================================================================

/*********************************************************************/
// Returns the address of the field in the object (This is an interior
// pointer and the caller has to use it appropriately). obj can be
// either a reference or a byref
HCIMPL2(void*, JIT_GetFieldAddr_Framed, Object *obj, FieldDesc* pFD)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pFD));
    } CONTRACTL_END;

    void * fldAddr = NULL;
    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    HELPER_METHOD_FRAME_BEGIN_RET_1(objRef);

    if (objRef == NULL)
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL2(JIT_GetFieldAddr_Framed, obj, pFD);
    }

    return pFD->GetAddressGuaranteedInHeap(obj);
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
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

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL3(JIT_SetFieldObj_Framed, obj, pFD, value);
    }

    void * address = pFD->GetAddressGuaranteedInHeap(obj);
    SetObjectReference((OBJECTREF*)address, ObjectToOBJECTREF(value), GetAppDomain());
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
        CopyValueClass(retBuff, pAddr, pFieldMT, objRef->GetAppDomain());
    }

    HELPER_METHOD_FRAME_END();          // Tear down the frame
}
HCIMPLEND

#include <optsmallperfcritical.h>
HCIMPL4(VOID, JIT_GetFieldStruct, LPVOID retBuff, Object *obj, FieldDesc *pFD, MethodTable *pFieldMT)
{
    FCALL_CONTRACT;

    _ASSERTE(pFieldMT->IsValueType());

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL4(JIT_GetFieldStruct_Framed, retBuff, obj, pFD, pFieldMT);
    }

    void * pAddr = pFD->GetAddressGuaranteedInHeap(obj);
    CopyValueClass(retBuff, pAddr, pFieldMT, obj->GetAppDomain());
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
        CopyValueClass(pAddr, valuePtr, pFieldMT, objRef->GetAppDomain());
    }

    HELPER_METHOD_FRAME_END();          // Tear down the frame
}
HCIMPLEND

#include <optsmallperfcritical.h>
HCIMPL4(VOID, JIT_SetFieldStruct, Object *obj, FieldDesc *pFD, MethodTable *pFieldMT, LPVOID valuePtr)
{
    FCALL_CONTRACT;

    _ASSERTE(pFieldMT->IsValueType());

    if (obj == NULL || obj->IsTransparentProxy() || g_IBCLogger.InstrEnabled() || pFD->IsEnCNew())
    {
        ENDFORBIDGC();
        return HCCALL4(JIT_SetFieldStruct_Framed, obj, pFD, pFieldMT, valuePtr);
    }

    void * pAddr = pFD->GetAddressGuaranteedInHeap(obj);
    CopyValueClass(pAddr, valuePtr, pFieldMT, obj->GetAppDomain());
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
    // already have initialized the Gloabl Class <Module>
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
    _ASSERTE(!pMT->IsClassPreInited());

    if (pMT->GetDomainLocalModule()->IsClassInitialized(pMT))
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

HCIMPL2(void*, JIT_GetSharedNonGCStaticBase_Portable, SIZE_T moduleDomainID, DWORD dwClassDomainID)
{
    FCALL_CONTRACT;

    DomainLocalModule *pLocalModule = NULL;

    if (!Module::IsEncodedModuleIndex(moduleDomainID))
        pLocalModule = (DomainLocalModule *) moduleDomainID;
    else
    {
        DomainLocalBlock *pLocalBlock = GetAppDomain()->GetDomainLocalBlock();
        pLocalModule = pLocalBlock->GetModuleSlot(Module::IDToIndex(moduleDomainID));
    }

    // If type doesn't have a class constructor, the contents of this if statement may 
    // still get executed.  JIT_GetSharedNonGCStaticBaseNoCtor should be used in this case.
    if (pLocalModule->IsPrecomputedClassInitialized(dwClassDomainID))
    {
        return (void*)pLocalModule->GetPrecomputedNonGCStaticsBasePointer();
    }

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL2(JIT_GetSharedNonGCStaticBase_Helper, pLocalModule, dwClassDomainID);
}
HCIMPLEND

// No constructor version of JIT_GetSharedNonGCStaticBase.  Does not check if class has 
// been initialized.
HCIMPL1(void*, JIT_GetSharedNonGCStaticBaseNoCtor_Portable, SIZE_T moduleDomainID)
{
    FCALL_CONTRACT;

    DomainLocalModule *pLocalModule = NULL;

    if (!Module::IsEncodedModuleIndex(moduleDomainID))
        pLocalModule = (DomainLocalModule *) moduleDomainID;
    else
    {
        DomainLocalBlock *pLocalBlock = GetAppDomain()->GetDomainLocalBlock();
        pLocalModule = pLocalBlock->GetModuleSlot(Module::IDToIndex(moduleDomainID));
    }

    return (void*)pLocalModule->GetPrecomputedNonGCStaticsBasePointer();
}
HCIMPLEND

HCIMPL2(void*, JIT_GetSharedGCStaticBase_Portable, SIZE_T moduleDomainID, DWORD dwClassDomainID)
{
    FCALL_CONTRACT;

    DomainLocalModule *pLocalModule = NULL;

    if (!Module::IsEncodedModuleIndex(moduleDomainID))
        pLocalModule = (DomainLocalModule *) moduleDomainID;
    else
    {
        DomainLocalBlock *pLocalBlock = GetAppDomain()->GetDomainLocalBlock();
        pLocalModule = pLocalBlock->GetModuleSlot(Module::IDToIndex(moduleDomainID));
    }

    // If type doesn't have a class constructor, the contents of this if statement may 
    // still get executed.  JIT_GetSharedGCStaticBaseNoCtor should be used in this case.
    if (pLocalModule->IsPrecomputedClassInitialized(dwClassDomainID))
    {
        return (void*)pLocalModule->GetPrecomputedGCStaticsBasePointer();
    }
    
    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL2(JIT_GetSharedGCStaticBase_Helper, pLocalModule, dwClassDomainID);
}
HCIMPLEND

// No constructor version of JIT_GetSharedGCStaticBase.  Does not check if class has been
// initialized.
HCIMPL1(void*, JIT_GetSharedGCStaticBaseNoCtor_Portable, SIZE_T moduleDomainID)
{
    FCALL_CONTRACT;

    DomainLocalModule *pLocalModule = NULL;

    if (!Module::IsEncodedModuleIndex(moduleDomainID))
        pLocalModule = (DomainLocalModule *) moduleDomainID;
    else
    {
        DomainLocalBlock *pLocalBlock = GetAppDomain()->GetDomainLocalBlock();
        pLocalModule = pLocalBlock->GetModuleSlot(Module::IDToIndex(moduleDomainID));
    }

    return (void*)pLocalModule->GetPrecomputedGCStaticsBasePointer();
}
HCIMPLEND

#include <optdefault.h>


// The following two functions can be tail called from platform dependent versions of 
// JIT_GetSharedGCStaticBase and JIT_GetShareNonGCStaticBase 
HCIMPL2(void*, JIT_GetSharedNonGCStaticBase_Helper, DomainLocalModule *pLocalModule, DWORD dwClassDomainID)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_RET_0();
    
    // Obtain Method table
    MethodTable * pMT = pLocalModule->GetMethodTableFromClassDomainID(dwClassDomainID);

    PREFIX_ASSUME(pMT != NULL);
    pMT->CheckRunClassInitThrowing();
    HELPER_METHOD_FRAME_END();
    
    return (void*)pLocalModule->GetPrecomputedNonGCStaticsBasePointer();
}
HCIMPLEND

HCIMPL2(void*, JIT_GetSharedGCStaticBase_Helper, DomainLocalModule *pLocalModule, DWORD dwClassDomainID)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_RET_0();
    
    // Obtain Method table
    MethodTable * pMT = pLocalModule->GetMethodTableFromClassDomainID(dwClassDomainID);
    
    PREFIX_ASSUME(pMT != NULL);
    pMT->CheckRunClassInitThrowing();
    HELPER_METHOD_FRAME_END();

    return (void*)pLocalModule->GetPrecomputedGCStaticsBasePointer();
}
HCIMPLEND

/*********************************************************************/
// Slow helper to tail call from the fast one
HCIMPL2(void*, JIT_GetSharedNonGCStaticBaseDynamicClass_Helper, DomainLocalModule *pLocalModule, DWORD dwDynamicClassDomainID)
{
    FCALL_CONTRACT;

    void* result = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    MethodTable *pMT = pLocalModule->GetDomainFile()->GetModule()->GetDynamicClassMT(dwDynamicClassDomainID);
    _ASSERTE(pMT);

    pMT->CheckRunClassInitThrowing();

    result = (void*)pLocalModule->GetDynamicEntryNonGCStaticsBasePointer(dwDynamicClassDomainID, pMT->GetLoaderAllocator());
    HELPER_METHOD_FRAME_END();

    return result;
}
HCIMPLEND

/*************************************************************/
#include <optsmallperfcritical.h>
HCIMPL2(void*, JIT_GetSharedNonGCStaticBaseDynamicClass, SIZE_T moduleDomainID, DWORD dwDynamicClassDomainID)
{
    FCALL_CONTRACT;

    DomainLocalModule *pLocalModule;

    if (!Module::IsEncodedModuleIndex(moduleDomainID))
        pLocalModule = (DomainLocalModule *) moduleDomainID;
    else
    {
        DomainLocalBlock *pLocalBlock = GetAppDomain()->GetDomainLocalBlock();
        pLocalModule = pLocalBlock->GetModuleSlot(Module::IDToIndex(moduleDomainID));
    }

    DomainLocalModule::PTR_DynamicClassInfo pLocalInfo = pLocalModule->GetDynamicClassInfoIfInitialized(dwDynamicClassDomainID);
    if (pLocalInfo != NULL)
    {
        PTR_BYTE retval;
        GET_DYNAMICENTRY_NONGCSTATICS_BASEPOINTER(pLocalModule->GetDomainFile()->GetModule()->GetLoaderAllocator(), 
                                               pLocalInfo, 
                                               &retval);

        return retval;
    }

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL2(JIT_GetSharedNonGCStaticBaseDynamicClass_Helper, pLocalModule, dwDynamicClassDomainID);
}
HCIMPLEND
#include <optdefault.h>

/*************************************************************/
// Slow helper to tail call from the fast one
HCIMPL2(void, JIT_ClassInitDynamicClass_Helper, DomainLocalModule *pLocalModule, DWORD dwDynamicClassDomainID)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    MethodTable *pMT = pLocalModule->GetDomainFile()->GetModule()->GetDynamicClassMT(dwDynamicClassDomainID);
    _ASSERTE(pMT);

    pMT->CheckRunClassInitThrowing();

    HELPER_METHOD_FRAME_END();

    return;
}
HCIMPLEND

#include <optsmallperfcritical.h>
HCIMPL2(void, JIT_ClassInitDynamicClass, SIZE_T moduleDomainID, DWORD dwDynamicClassDomainID)
{
    FCALL_CONTRACT;

    DomainLocalModule *pLocalModule;

    if (!Module::IsEncodedModuleIndex(moduleDomainID))
        pLocalModule = (DomainLocalModule *) moduleDomainID;
    else
    {
        DomainLocalBlock *pLocalBlock = GetAppDomain()->GetDomainLocalBlock();
        pLocalModule = pLocalBlock->GetModuleSlot(Module::IDToIndex(moduleDomainID));
    }

    DomainLocalModule::PTR_DynamicClassInfo pLocalInfo = pLocalModule->GetDynamicClassInfoIfInitialized(dwDynamicClassDomainID);
    if (pLocalInfo != NULL)
    {
        return;
    }

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL2(JIT_ClassInitDynamicClass_Helper, pLocalModule, dwDynamicClassDomainID);
}
HCIMPLEND
#include <optdefault.h>

/*************************************************************/
// Slow helper to tail call from the fast one
HCIMPL2(void*, JIT_GetSharedGCStaticBaseDynamicClass_Helper, DomainLocalModule *pLocalModule, DWORD dwDynamicClassDomainID)
{
    FCALL_CONTRACT;

    void* result = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    MethodTable *pMT = pLocalModule->GetDomainFile()->GetModule()->GetDynamicClassMT(dwDynamicClassDomainID);
    _ASSERTE(pMT);

    pMT->CheckRunClassInitThrowing();

    result = (void*)pLocalModule->GetDynamicEntryGCStaticsBasePointer(dwDynamicClassDomainID, pMT->GetLoaderAllocator());
    HELPER_METHOD_FRAME_END();

    return result;
}
HCIMPLEND

/*************************************************************/
#include <optsmallperfcritical.h>
HCIMPL2(void*, JIT_GetSharedGCStaticBaseDynamicClass, SIZE_T moduleDomainID, DWORD dwDynamicClassDomainID)
{
    FCALL_CONTRACT;

    DomainLocalModule *pLocalModule;

    if (!Module::IsEncodedModuleIndex(moduleDomainID))
        pLocalModule = (DomainLocalModule *) moduleDomainID;
    else
    {
        DomainLocalBlock *pLocalBlock = GetAppDomain()->GetDomainLocalBlock();
        pLocalModule = pLocalBlock->GetModuleSlot(Module::IDToIndex(moduleDomainID));
    }

    DomainLocalModule::PTR_DynamicClassInfo pLocalInfo = pLocalModule->GetDynamicClassInfoIfInitialized(dwDynamicClassDomainID);
    if (pLocalInfo != NULL)
    {
        PTR_BYTE retval;
        GET_DYNAMICENTRY_GCSTATICS_BASEPOINTER(pLocalModule->GetDomainFile()->GetModule()->GetLoaderAllocator(), 
                                               pLocalInfo, 
                                               &retval);

        return retval;
    }

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL2(JIT_GetSharedGCStaticBaseDynamicClass_Helper, pLocalModule, dwDynamicClassDomainID);
}
HCIMPLEND
#include <optdefault.h>

/*********************************************************************/
// Slow helper to tail call from the fast one
NOINLINE HCIMPL1(void*, JIT_GetGenericsGCStaticBase_Framed, MethodTable *pMT)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(pMT->HasGenericsStaticsInfo());
    } CONTRACTL_END;

    void* base = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    pMT->CheckRestore();

    pMT->CheckRunClassInitThrowing();

    base = (void*) pMT->GetGCStaticsBasePointer();
    CONSISTENCY_CHECK(base != NULL);

    HELPER_METHOD_FRAME_END();

    return base;
}
HCIMPLEND

/*********************************************************************/
#include <optsmallperfcritical.h>
HCIMPL1(void*, JIT_GetGenericsGCStaticBase, MethodTable *pMT)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(pMT->HasGenericsStaticsInfo());
    } CONTRACTL_END;
   
    DWORD dwDynamicClassDomainID;
    PTR_Module pModuleForStatics = pMT->GetGenericsStaticsModuleAndID(&dwDynamicClassDomainID);

    DomainLocalModule *pLocalModule = pModuleForStatics->GetDomainLocalModule();
    _ASSERTE(pLocalModule);

    DomainLocalModule::PTR_DynamicClassInfo pLocalInfo = pLocalModule->GetDynamicClassInfoIfInitialized(dwDynamicClassDomainID);
    if (pLocalInfo != NULL)
    {
        PTR_BYTE retval;
        GET_DYNAMICENTRY_GCSTATICS_BASEPOINTER(pMT->GetLoaderAllocator(), 
                                               pLocalInfo, 
                                               &retval);

        return retval;
    }

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL1(JIT_GetGenericsGCStaticBase_Framed, pMT);
}
HCIMPLEND
#include <optdefault.h>

/*********************************************************************/
// Slow helper to tail call from the fast one
NOINLINE HCIMPL1(void*, JIT_GetGenericsNonGCStaticBase_Framed, MethodTable *pMT)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(pMT->HasGenericsStaticsInfo());
    } CONTRACTL_END;

    void* base = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    pMT->CheckRestore();

    // If pMT refers to a method table that requires some initialization work,
    // then pMT cannot to a method table that is shared by generic instantiations,
    // because method tables that are shared by generic instantiations do not have
    // a base for statics to live in.
    _ASSERTE(pMT->IsClassPreInited() || !pMT->IsSharedByGenericInstantiations());

    pMT->CheckRunClassInitThrowing();

    // We could just return null here instead of returning base when this helper is called just to trigger the cctor
    base = (void*) pMT->GetNonGCStaticsBasePointer();

    HELPER_METHOD_FRAME_END();

    return base;
}
HCIMPLEND

/*********************************************************************/
#include <optsmallperfcritical.h>
HCIMPL1(void*, JIT_GetGenericsNonGCStaticBase, MethodTable *pMT)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(pMT->HasGenericsStaticsInfo());
    } CONTRACTL_END;

    // This fast path will typically always be taken once the slow framed path below
    // has executed once.  Sometimes the slow path will be executed more than once,
    // e.g. if static fields are accessed during the call to CheckRunClassInitThrowing()
    // in the slow path.
    
    DWORD dwDynamicClassDomainID;
    PTR_Module pModuleForStatics = pMT->GetGenericsStaticsModuleAndID(&dwDynamicClassDomainID);

    DomainLocalModule *pLocalModule = pModuleForStatics->GetDomainLocalModule();
    _ASSERTE(pLocalModule);

    DomainLocalModule::PTR_DynamicClassInfo pLocalInfo = pLocalModule->GetDynamicClassInfoIfInitialized(dwDynamicClassDomainID);
    if (pLocalInfo != NULL)
    {
        PTR_BYTE retval;
        GET_DYNAMICENTRY_NONGCSTATICS_BASEPOINTER(pMT->GetLoaderAllocator(), 
                                               pLocalInfo, 
                                               &retval);

        return retval;
    }

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL1(JIT_GetGenericsNonGCStaticBase_Framed, pMT);
}
HCIMPLEND
#include <optdefault.h>


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
  
    // For generics, we need to call CheckRestore() for some reason
    if (pMT->HasGenericsStaticsInfo())
        pMT->CheckRestore();

    // Get the TLM
    ThreadLocalModule * pThreadLocalModule = ThreadStatics::GetTLM(pMT);
    _ASSERTE(pThreadLocalModule != NULL);

    // Check if the class constructor needs to be run
    pThreadLocalModule->CheckRunClassInitThrowing(pMT);

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
  
    // For generics, we need to call CheckRestore() for some reason
    if (pMT->HasGenericsStaticsInfo())
        pMT->CheckRestore();

    // Get the TLM
    ThreadLocalModule * pThreadLocalModule = ThreadStatics::GetTLM(pMT);
    _ASSERTE(pThreadLocalModule != NULL);

    // Check if the class constructor needs to be run
    pThreadLocalModule->CheckRunClassInitThrowing(pMT);

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
HCIMPL2(void*, JIT_GetSharedNonGCThreadStaticBase, SIZE_T moduleDomainID, DWORD dwClassDomainID)
{
    FCALL_CONTRACT;

    // Get the ModuleIndex
    ModuleIndex index = 
        (Module::IsEncodedModuleIndex(moduleDomainID)) ?
            Module::IDToIndex(moduleDomainID) :
            ((DomainLocalModule *)moduleDomainID)->GetModuleIndex();

    // Get the relevant ThreadLocalModule
    ThreadLocalModule * pThreadLocalModule = ThreadStatics::GetTLMIfExists(index);

    // If the TLM has been allocated and the class has been marked as initialized,
    // get the pointer to the non-GC statics base and return
    if (pThreadLocalModule != NULL && pThreadLocalModule->IsPrecomputedClassInitialized(dwClassDomainID))
        return (void*)pThreadLocalModule->GetPrecomputedNonGCStaticsBasePointer();

    // If the TLM was not allocated or if the class was not marked as initialized
    // then we have to go through the slow path

    // Get the DomainLocalModule
    DomainLocalModule *pDomainLocalModule =
        (Module::IsEncodedModuleIndex(moduleDomainID)) ?
            GetAppDomain()->GetDomainLocalBlock()->GetModuleSlot(Module::IDToIndex(moduleDomainID)) :
            (DomainLocalModule *) moduleDomainID;
    
    // Obtain the MethodTable
    MethodTable * pMT = pDomainLocalModule->GetMethodTableFromClassDomainID(dwClassDomainID);
    _ASSERTE(!pMT->HasGenericsStaticsInfo());

    ENDFORBIDGC();
    return HCCALL1(JIT_GetNonGCThreadStaticBase_Helper, pMT);
}
HCIMPLEND
#include <optdefault.h>

// *** This helper corresponds to both CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE and
//     CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR. Even though we always check
//     if the class constructor has been run, we have a separate helper ID for the "no ctor"
//     version because it allows the JIT to do some reordering that otherwise wouldn't be
//     possible.

#include <optsmallperfcritical.h>
HCIMPL2(void*, JIT_GetSharedGCThreadStaticBase, SIZE_T moduleDomainID, DWORD dwClassDomainID)
{
    FCALL_CONTRACT;

    // Get the ModuleIndex
    ModuleIndex index = 
        (Module::IsEncodedModuleIndex(moduleDomainID)) ?
            Module::IDToIndex(moduleDomainID) :
            ((DomainLocalModule *)moduleDomainID)->GetModuleIndex();

    // Get the relevant ThreadLocalModule
    ThreadLocalModule * pThreadLocalModule = ThreadStatics::GetTLMIfExists(index);

    // If the TLM has been allocated and the class has been marked as initialized,
    // get the pointer to the GC statics base and return
    if (pThreadLocalModule != NULL && pThreadLocalModule->IsPrecomputedClassInitialized(dwClassDomainID))
        return (void*)pThreadLocalModule->GetPrecomputedGCStaticsBasePointer();

    // If the TLM was not allocated or if the class was not marked as initialized
    // then we have to go through the slow path

    // Get the DomainLocalModule
    DomainLocalModule *pDomainLocalModule =
        (Module::IsEncodedModuleIndex(moduleDomainID)) ?
            GetAppDomain()->GetDomainLocalBlock()->GetModuleSlot(Module::IDToIndex(moduleDomainID)) :
            (DomainLocalModule *) moduleDomainID;
    
    // Obtain the MethodTable
    MethodTable * pMT = pDomainLocalModule->GetMethodTableFromClassDomainID(dwClassDomainID);
    _ASSERTE(!pMT->HasGenericsStaticsInfo());

    ENDFORBIDGC();
    return HCCALL1(JIT_GetGCThreadStaticBase_Helper, pMT);
}
HCIMPLEND
#include <optdefault.h>

// *** This helper corresponds to CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_DYNAMICCLASS

#include <optsmallperfcritical.h>
HCIMPL2(void*, JIT_GetSharedNonGCThreadStaticBaseDynamicClass, SIZE_T moduleDomainID, DWORD dwDynamicClassDomainID)
{
    FCALL_CONTRACT;

    // Get the ModuleIndex
    ModuleIndex index = 
        (Module::IsEncodedModuleIndex(moduleDomainID)) ?
            Module::IDToIndex(moduleDomainID) :
            ((DomainLocalModule *)moduleDomainID)->GetModuleIndex();

    // Get the relevant ThreadLocalModule
    ThreadLocalModule * pThreadLocalModule = ThreadStatics::GetTLMIfExists(index);

    // If the TLM has been allocated and the class has been marked as initialized,
    // get the pointer to the non-GC statics base and return
    if (pThreadLocalModule != NULL)
    { 
        ThreadLocalModule::PTR_DynamicClassInfo pLocalInfo = pThreadLocalModule->GetDynamicClassInfoIfInitialized(dwDynamicClassDomainID);
        if (pLocalInfo != NULL)
            return (void*)pLocalInfo->m_pDynamicEntry->GetNonGCStaticsBasePointer();
    }

    // If the TLM was not allocated or if the class was not marked as initialized
    // then we have to go through the slow path

    // Obtain the DomainLocalModule
    DomainLocalModule *pDomainLocalModule =
        (Module::IsEncodedModuleIndex(moduleDomainID)) ?
            GetAppDomain()->GetDomainLocalBlock()->GetModuleSlot(Module::IDToIndex(moduleDomainID)) :
            (DomainLocalModule *) moduleDomainID;
   
    // Obtain the Module
    Module * pModule = pDomainLocalModule->GetDomainFile()->GetModule();

    // Obtain the MethodTable
    MethodTable * pMT = pModule->GetDynamicClassMT(dwDynamicClassDomainID);
    _ASSERTE(pMT != NULL);  
    _ASSERTE(!pMT->IsSharedByGenericInstantiations());

    // Tailcall to the slow helper
    ENDFORBIDGC();

    return HCCALL1(JIT_GetNonGCThreadStaticBase_Helper, pMT);

}
HCIMPLEND
#include <optdefault.h>

// *** This helper corresponds to CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_DYNAMICCLASS

#include <optsmallperfcritical.h>
HCIMPL2(void*, JIT_GetSharedGCThreadStaticBaseDynamicClass, SIZE_T moduleDomainID, DWORD dwDynamicClassDomainID)
{
    FCALL_CONTRACT;

    // Get the ModuleIndex
    ModuleIndex index = 
        (Module::IsEncodedModuleIndex(moduleDomainID)) ?
            Module::IDToIndex(moduleDomainID) :
            ((DomainLocalModule *)moduleDomainID)->GetModuleIndex();

    // Get the relevant ThreadLocalModule
    ThreadLocalModule * pThreadLocalModule = ThreadStatics::GetTLMIfExists(index);

    // If the TLM has been allocated and the class has been marked as initialized,
    // get the pointer to the GC statics base and return
    if (pThreadLocalModule != NULL)
    { 
        ThreadLocalModule::PTR_DynamicClassInfo pLocalInfo = pThreadLocalModule->GetDynamicClassInfoIfInitialized(dwDynamicClassDomainID);
        if (pLocalInfo != NULL)
            return (void*)pLocalInfo->m_pDynamicEntry->GetGCStaticsBasePointer();
    }

    // If the TLM was not allocated or if the class was not marked as initialized
    // then we have to go through the slow path

    // Obtain the DomainLocalModule
    DomainLocalModule *pDomainLocalModule =
        (Module::IsEncodedModuleIndex(moduleDomainID)) ?
            GetAppDomain()->GetDomainLocalBlock()->GetModuleSlot(Module::IDToIndex(moduleDomainID)) :
            (DomainLocalModule *) moduleDomainID;
   
    // Obtain the Module
    Module * pModule = pDomainLocalModule->GetDomainFile()->GetModule();

    // Obtain the MethodTable
    MethodTable * pMT = pModule->GetDynamicClassMT(dwDynamicClassDomainID);
    _ASSERTE(pMT != NULL);  
    _ASSERTE(!pMT->IsSharedByGenericInstantiations());

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL1(JIT_GetGCThreadStaticBase_Helper, pMT);
}
HCIMPLEND
#include <optdefault.h>

// *** This helper corresponds to CORINFO_HELP_GETGENERICS_NONGCTHREADSTATIC_BASE
    
#include <optsmallperfcritical.h>
HCIMPL1(void*, JIT_GetGenericsNonGCThreadStaticBase, MethodTable *pMT)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(pMT->HasGenericsStaticsInfo());
    } CONTRACTL_END;

    // This fast path will typically always be taken once the slow framed path below
    // has executed once.  Sometimes the slow path will be executed more than once,
    // e.g. if static fields are accessed during the call to CheckRunClassInitThrowing()
    // in the slow path.

    // Get the Module and dynamic class ID
    DWORD dwDynamicClassDomainID;
    PTR_Module pModule = pMT->GetGenericsStaticsModuleAndID(&dwDynamicClassDomainID);

    // Get ModuleIndex
    ModuleIndex index = pModule->GetModuleIndex();

    // Get the relevant ThreadLocalModule
    ThreadLocalModule * pThreadLocalModule = ThreadStatics::GetTLMIfExists(index);

    // If the TLM has been allocated and the class has been marked as initialized,
    // get the pointer to the non-GC statics base and return
    if (pThreadLocalModule != NULL)
    { 
        ThreadLocalModule::PTR_DynamicClassInfo pLocalInfo = pThreadLocalModule->GetDynamicClassInfoIfInitialized(dwDynamicClassDomainID);
        if (pLocalInfo != NULL)
            return (void*)pLocalInfo->m_pDynamicEntry->GetNonGCStaticsBasePointer();
    }
    
    // If the TLM was not allocated or if the class was not marked as initialized
    // then we have to go through the slow path

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL1(JIT_GetNonGCThreadStaticBase_Helper, pMT);
}
HCIMPLEND
#include <optdefault.h>

// *** This helper corresponds to CORINFO_HELP_GETGENERICS_GCTHREADSTATIC_BASE

#include <optsmallperfcritical.h>
HCIMPL1(void*, JIT_GetGenericsGCThreadStaticBase, MethodTable *pMT)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(pMT->HasGenericsStaticsInfo());
    } CONTRACTL_END;

    // This fast path will typically always be taken once the slow framed path below
    // has executed once.  Sometimes the slow path will be executed more than once,
    // e.g. if static fields are accessed during the call to CheckRunClassInitThrowing()
    // in the slow path.

    // Get the Module and dynamic class ID
    DWORD dwDynamicClassDomainID;
    PTR_Module pModule = pMT->GetGenericsStaticsModuleAndID(&dwDynamicClassDomainID);

    // Get ModuleIndex
    ModuleIndex index = pModule->GetModuleIndex();

    // Get the relevant ThreadLocalModule
    ThreadLocalModule * pThreadLocalModule = ThreadStatics::GetTLMIfExists(index);

    // If the TLM has been allocated and the class has been marked as initialized,
    // get the pointer to the GC statics base and return
    if (pThreadLocalModule != NULL)
    { 
        ThreadLocalModule::PTR_DynamicClassInfo pLocalInfo = pThreadLocalModule->GetDynamicClassInfoIfInitialized(dwDynamicClassDomainID);
        if (pLocalInfo != NULL)
            return (void*)pLocalInfo->m_pDynamicEntry->GetGCStaticsBasePointer();
    }
    
    // If the TLM was not allocated or if the class was not marked as initialized
    // then we have to go through the slow path

    // Tailcall to the slow helper
    ENDFORBIDGC();
    return HCCALL1(JIT_GetGCThreadStaticBase_Helper, pMT);
}
HCIMPLEND
#include <optdefault.h>

//========================================================================
//
//      STATIC FIELD DYNAMIC HELPERS
//
//========================================================================

#include <optsmallperfcritical.h>
HCIMPL1_RAW(TADDR, JIT_StaticFieldAddress_Dynamic, StaticFieldAddressArgs * pArgs)
{
    FCALL_CONTRACT;

    TADDR base = HCCALL2(pArgs->staticBaseHelper, pArgs->arg0, pArgs->arg1);
    return base + pArgs->offset;
}
HCIMPLEND_RAW
#include <optdefault.h>

#include <optsmallperfcritical.h>
HCIMPL1_RAW(TADDR, JIT_StaticFieldAddressUnbox_Dynamic, StaticFieldAddressArgs * pArgs)
{
    FCALL_CONTRACT;

    TADDR base = HCCALL2(pArgs->staticBaseHelper, pArgs->arg0, pArgs->arg1);
    return *(TADDR *)(base + pArgs->offset) + Object::GetOffsetOfFirstField();
}
HCIMPLEND_RAW
#include <optdefault.h>

//========================================================================
//
//      CASTING HELPERS
//
//========================================================================

// pObject MUST be an instance of an array.
TypeHandle::CastResult ArrayIsInstanceOfNoGC(Object *pObject, TypeHandle toTypeHnd)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(pObject));
        PRECONDITION(pObject->GetMethodTable()->IsArray());
        PRECONDITION(toTypeHnd.IsArray());
    } CONTRACTL_END;

    ArrayBase *pArray = (ArrayBase*) pObject;
    ArrayTypeDesc *toArrayType = toTypeHnd.AsArray();

    // GetRank touches EEClass. Try to avoid it for SZArrays.
    if (toArrayType->GetInternalCorElementType() == ELEMENT_TYPE_SZARRAY)
    {
        if (pArray->GetMethodTable()->IsMultiDimArray())
            return TypeHandle::CannotCast;
    }
    else
    {
        if (pArray->GetRank() != toArrayType->GetRank())
            return TypeHandle::CannotCast;
    }
    _ASSERTE(pArray->GetRank() == toArrayType->GetRank());

    // ArrayBase::GetTypeHandle consults the loader tables to find the
    // exact type handle for an array object.  This can be disproportionately slow - but after
    // all, why should we need to go looking up hash tables just to do a cast test?
    //
    // Thus we can always special-case the casting logic to avoid fetching this
    // exact type handle.  Here we have only done so for one
    // particular case, i.e. when we are trying to cast to an array type where
    // there is an exact match between the rank, kind and element type of the two
    // array types.  This happens when, for example, assigning an int32[] into an int32[][].
    //

    TypeHandle elementTypeHandle = pArray->GetArrayElementTypeHandle();
    TypeHandle toElementTypeHandle = toArrayType->GetArrayElementTypeHandle();

    if (elementTypeHandle == toElementTypeHandle)
        return TypeHandle::CanCast;

    // By this point we know that toArrayType->GetInternalCorElementType matches the element type of the Array object
    // so we can use a faster constructor to create the TypeDesc. (It so happens that ArrayTypeDescs derives from ParamTypeDesc
    // and can be created as identical in a slightly faster way with the following set of parameters.)
    ParamTypeDesc arrayType(toArrayType->GetInternalCorElementType(), pArray->GetMethodTable(), elementTypeHandle);
    return arrayType.CanCastToNoGC(toTypeHnd);
}

// pObject MUST be an instance of an array.
TypeHandle::CastResult ArrayObjSupportsBizarreInterfaceNoGC(Object *pObject, MethodTable * pInterfaceMT)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(pObject));
        PRECONDITION(pObject->GetMethodTable()->IsArray());
        PRECONDITION(pInterfaceMT->IsInterface());
    } CONTRACTL_END;

    ArrayBase *pArray = (ArrayBase*) pObject;

    // IList<T> & IReadOnlyList<T> only supported for SZ_ARRAYS
    if (pArray->GetMethodTable()->IsMultiDimArray())
        return TypeHandle::CannotCast;

    if (pInterfaceMT->GetLoadLevel() < CLASS_DEPENDENCIES_LOADED)
    {
        if (!pInterfaceMT->HasInstantiation())
            return TypeHandle::CannotCast;
        // The slow path will take care of restoring the interface
        return TypeHandle::MaybeCast;
    }

    if (!IsImplicitInterfaceOfSZArray(pInterfaceMT))
        return TypeHandle::CannotCast;

    return TypeDesc::CanCastParamNoGC(pArray->GetArrayElementTypeHandle(), pInterfaceMT->GetInstantiation()[0]);
}

TypeHandle::CastResult STDCALL ObjIsInstanceOfNoGC(Object *pObject, TypeHandle toTypeHnd)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(pObject));
    } CONTRACTL_END;


    MethodTable *pMT = pObject->GetMethodTable();

    // Quick exact match first
    if (TypeHandle(pMT) == toTypeHnd)
        return TypeHandle::CanCast;

    if (pMT->IsTransparentProxy() ||
           (toTypeHnd.IsInterface() && ( pMT->IsComObjectType() || pMT->IsICastable() ))
       )
    {      
        return TypeHandle::MaybeCast;
    }

    if (pMT->IsArray())
    {
        if (toTypeHnd.IsArray())
            return ArrayIsInstanceOfNoGC(pObject, toTypeHnd);

        if (toTypeHnd.IsInterface())
        {
            MethodTable * pInterfaceMT = toTypeHnd.AsMethodTable();
            if (pInterfaceMT->HasInstantiation())
                return ArrayObjSupportsBizarreInterfaceNoGC(pObject, pInterfaceMT);
            return pMT->ImplementsInterface(pInterfaceMT) ? TypeHandle::CanCast : TypeHandle::CannotCast;
        }

        if (toTypeHnd == TypeHandle(g_pObjectClass) || toTypeHnd == TypeHandle(g_pArrayClass))
            return TypeHandle::CanCast;

        return TypeHandle::CannotCast;
    }

    if (toTypeHnd.IsTypeDesc())
        return TypeHandle::CannotCast;

    // allow an object of type T to be cast to Nullable<T> (they have the same representation)
    if (Nullable::IsNullableForTypeNoGC(toTypeHnd, pMT))
        return TypeHandle::CanCast;

    return pMT->CanCastToClassOrInterfaceNoGC(toTypeHnd.AsMethodTable());
}

BOOL ObjIsInstanceOf(Object *pObject, TypeHandle toTypeHnd, BOOL throwCastException)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pObject));
    } CONTRACTL_END;

    BOOL fCast = FALSE;

    OBJECTREF obj = ObjectToOBJECTREF(pObject);

    GCPROTECT_BEGIN(obj);

    TypeHandle fromTypeHnd = obj->GetTypeHandle();

    // If we are trying to cast a proxy we need to delegate to remoting
    // services which will determine whether the proxy and the type are compatible.
    // Start by doing a quick static cast check to see if the type information captured in
    // the metadata indicates that the cast is legal.
    if (fromTypeHnd.CanCastTo(toTypeHnd))
    {
        fCast = TRUE;
    }
    else
#ifdef FEATURE_COMINTEROP
    // If we are casting a COM object from interface then we need to do a check to see 
    // if it implements the interface.
    if (toTypeHnd.IsInterface() && fromTypeHnd.GetMethodTable()->IsComObjectType())
    {
        fCast = ComObject::SupportsInterface(obj, toTypeHnd.AsMethodTable());
    }
    else
#endif // FEATURE_COMINTEROP
    if (Nullable::IsNullableForType(toTypeHnd, obj->GetMethodTable()))
    {
        // allow an object of type T to be cast to Nullable<T> (they have the same representation)
        fCast = TRUE;
    }
#ifdef FEATURE_ICASTABLE
    // If type implements ICastable interface we give it a chance to tell us if it can be casted 
    // to a given type.
    else if (toTypeHnd.IsInterface() && fromTypeHnd.GetMethodTable()->IsICastable())
    {
        // Make actuall call to ICastableHelpers.IsInstanceOfInterface(obj, interfaceTypeObj, out exception)
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
#endif // FEATURE_ICASTABLE

    if (!fCast && throwCastException) 
    {
        COMPlusThrowInvalidCastException(&obj, toTypeHnd);
    }    

    GCPROTECT_END(); // obj

    return(fCast);
}

//
// This optimization is intended for all non-framed casting helpers
//

#include <optsmallperfcritical.h>

HCIMPL2(Object*, JIT_ChkCastClass_Portable, MethodTable* pTargetMT, Object* pObject)
{
    FCALL_CONTRACT;

    //
    // casts pObject to type pMT
    //

    if (NULL == pObject)
    {
        return NULL;
    }

    PTR_VOID pMT = pObject->GetMethodTable();

    do {
        if (pMT == pTargetMT)
            return pObject;

        pMT = MethodTable::GetParentMethodTable(pMT);
    } while (pMT);

    ENDFORBIDGC();
    return HCCALL2(JITutil_ChkCastAny, CORINFO_CLASS_HANDLE(pTargetMT), pObject);
}
HCIMPLEND

//
// This helper assumes that the check for the trivial cases has been inlined by the JIT.
//
HCIMPL2(Object*, JIT_ChkCastClassSpecial_Portable, MethodTable* pTargetMT, Object* pObject)
{
    CONTRACTL {
        FCALL_CHECK;
        // This assumes that the check for the trivial cases has been inlined by the JIT.
        PRECONDITION(pObject != NULL);
        PRECONDITION(pObject->GetMethodTable() != pTargetMT);
    } CONTRACTL_END;

    PTR_VOID pMT = MethodTable::GetParentMethodTable(pObject->GetMethodTable());

    while (pMT)
    {
        if (pMT == pTargetMT)
            return pObject;

        pMT = MethodTable::GetParentMethodTable(pMT);
    }

    ENDFORBIDGC();
    return HCCALL2(JITutil_ChkCastAny, CORINFO_CLASS_HANDLE(pTargetMT), pObject);
}
HCIMPLEND

HCIMPL2(Object*, JIT_IsInstanceOfClass_Portable, MethodTable* pTargetMT, Object* pObject)
{
    FCALL_CONTRACT;

    //
    // casts pObject to type pMT
    //

    if (NULL == pObject)
    {
        return NULL;
    }

    PTR_VOID pMT = pObject->GetMethodTable();

    do {
        if (pMT == pTargetMT)
            return pObject;

        pMT = MethodTable::GetParentMethodTable(pMT);
    } while (pMT);

    if (!pObject->GetMethodTable()->HasTypeEquivalence())
    {
        return NULL;
    }

    ENDFORBIDGC();
    return HCCALL2(JITutil_IsInstanceOfAny, CORINFO_CLASS_HANDLE(pTargetMT), pObject);
}
HCIMPLEND

HCIMPL2(Object*, JIT_ChkCastInterface_Portable, MethodTable *pInterfaceMT, Object* pObject)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(pInterfaceMT->IsInterface());
    } CONTRACTL_END;

    if (NULL == pObject)
    {
        return pObject;
    }

    if (pObject->GetMethodTable()->ImplementsInterfaceInline(pInterfaceMT))
    {
        return pObject;
    }

    ENDFORBIDGC();
    return HCCALL2(JITutil_ChkCastInterface, pInterfaceMT, pObject);
}
HCIMPLEND

HCIMPL2(Object*, JIT_IsInstanceOfInterface_Portable, MethodTable *pInterfaceMT, Object* pObject)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(pInterfaceMT->IsInterface());
    } CONTRACTL_END;

    if (NULL == pObject)
    {
        return NULL;
    }

    if (pObject->GetMethodTable()->ImplementsInterfaceInline(pInterfaceMT))
    {
        return pObject;
    }

    if (!pObject->GetMethodTable()->InstanceRequiresNonTrivialInterfaceCast())
    {
        return NULL;
    }

    ENDFORBIDGC();
    return HCCALL2(JITutil_IsInstanceOfInterface, pInterfaceMT, pObject);
}
HCIMPLEND

HCIMPL2(Object *, JIT_ChkCastArray, CORINFO_CLASS_HANDLE type, Object *pObject)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(TypeHandle(type).IsArray());
    } CONTRACTL_END;

    if (pObject == NULL)
    {
        return NULL;
    }

    OBJECTREF refObj = ObjectToOBJECTREF(pObject);
    VALIDATEOBJECTREF(refObj);

    TypeHandle::CastResult result = refObj->GetMethodTable()->IsArray() ? 
        ArrayIsInstanceOfNoGC(pObject, TypeHandle(type)) : TypeHandle::CannotCast;

    if (result == TypeHandle::CanCast)
    {
        return pObject;
    }

    ENDFORBIDGC();
    Object* pRet = HCCALL2(JITutil_ChkCastAny, type, pObject);
    // Make sure that the fast helper have not lied
    _ASSERTE(result != TypeHandle::CannotCast);
    return pRet;
}
HCIMPLEND


HCIMPL2(Object *, JIT_IsInstanceOfArray, CORINFO_CLASS_HANDLE type, Object *pObject)
{
     CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(TypeHandle(type).IsArray());
    } CONTRACTL_END;

    if (pObject == NULL)
    {
        return NULL;
    }

    OBJECTREF refObj = ObjectToOBJECTREF(pObject);
    VALIDATEOBJECTREF(refObj);
    MethodTable *pMT = refObj->GetMethodTable();

    if (!pMT->IsArray())
    {
        // We know that the clsHnd is an array so check the object.  If it is not an array return null
        return NULL;
    }
    else
    {
        switch (ArrayIsInstanceOfNoGC(pObject, TypeHandle(type))) {
        case TypeHandle::CanCast:
            return pObject;
        case TypeHandle::CannotCast:
            return NULL;
        default:
            // fall through to the slow helper
            break;
        }
    }

    ENDFORBIDGC();
    return HCCALL2(JITutil_IsInstanceOfAny, type, pObject);
}
HCIMPLEND

/*********************************************************************/
// IsInstanceOf test used for unusual cases (naked type parameters, variant generic types)
// Unlike the IsInstanceOfInterface, IsInstanceOfClass, and IsIsntanceofArray functions,
// this test must deal with all kinds of type tests
HCIMPL2(Object *, JIT_IsInstanceOfAny, CORINFO_CLASS_HANDLE type, Object* obj)
{
    FCALL_CONTRACT;

    if (NULL == obj)
    {
        return NULL;
    }

    switch (ObjIsInstanceOfNoGC(obj, TypeHandle(type))) {
    case TypeHandle::CanCast:
        return obj;
    case TypeHandle::CannotCast:
        return NULL;
    default:
        // fall through to the slow helper
        break;
    }

    ENDFORBIDGC();
    return HCCALL2(JITutil_IsInstanceOfAny, type, obj);
}
HCIMPLEND

// ChkCast test used for unusual cases (naked type parameters, variant generic types)
// Unlike the ChkCastInterface, ChkCastClass, and ChkCastArray functions,
// this test must deal with all kinds of type tests
HCIMPL2(Object *, JIT_ChkCastAny, CORINFO_CLASS_HANDLE type, Object *obj)
{
    FCALL_CONTRACT;

    if (NULL == obj)
    {
        return NULL;
    }

    TypeHandle::CastResult result = ObjIsInstanceOfNoGC(obj, TypeHandle(type));

    if (result == TypeHandle::CanCast)
    {
        return obj;
    }

    ENDFORBIDGC();
    Object* pRet = HCCALL2(JITutil_ChkCastAny, type, obj);
    // Make sure that the fast helper have not lied
    _ASSERTE(result != TypeHandle::CannotCast);
    return pRet;
}
HCIMPLEND


NOINLINE HCIMPL2(Object *, JITutil_IsInstanceOfInterface, MethodTable *pInterfaceMT, Object* obj)
{
    FCALL_CONTRACT;

    if (obj->GetMethodTable()->IsArray())
    {
        switch (ArrayObjSupportsBizarreInterfaceNoGC(obj, pInterfaceMT)) {
        case TypeHandle::CanCast:
            return obj;
        case TypeHandle::CannotCast:
            return NULL;
        default:
            // fall through to the slow helper
            break;
        }
    }

    ENDFORBIDGC();
    return HCCALL2(JITutil_IsInstanceOfAny, CORINFO_CLASS_HANDLE(pInterfaceMT), obj);

}
HCIMPLEND

NOINLINE HCIMPL2(Object *, JITutil_ChkCastInterface, MethodTable *pInterfaceMT, Object *obj)
{
    FCALL_CONTRACT;

    if (obj->GetMethodTable()->IsArray())
    {
        if (ArrayObjSupportsBizarreInterfaceNoGC(obj, pInterfaceMT) == TypeHandle::CanCast)
        {
            return obj;
        }
    }

    ENDFORBIDGC();
    return HCCALL2(JITutil_ChkCastAny, CORINFO_CLASS_HANDLE(pInterfaceMT), obj);
}
HCIMPLEND


#include <optdefault.h>


//
// Framed helpers
//
NOINLINE HCIMPL2(Object *, JITutil_ChkCastAny, CORINFO_CLASS_HANDLE type, Object *obj)
{
    FCALL_CONTRACT;

    // This case should be handled by frameless helper
     _ASSERTE(obj != NULL);

    OBJECTREF oref = ObjectToOBJECTREF (obj);
    VALIDATEOBJECTREF(oref);

    TypeHandle clsHnd(type);

    HELPER_METHOD_FRAME_BEGIN_RET_1(oref);
    if (!ObjIsInstanceOf(OBJECTREFToObject(oref), clsHnd, TRUE))
    {
        UNREACHABLE(); //ObjIsInstanceOf will throw if cast can't be done
    }
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(oref);
}
HCIMPLEND

NOINLINE HCIMPL2(Object *, JITutil_IsInstanceOfAny, CORINFO_CLASS_HANDLE type, Object *obj)
{
    FCALL_CONTRACT;

    // This case should be handled by frameless helper
     _ASSERTE(obj != NULL);

    OBJECTREF oref = ObjectToOBJECTREF (obj);
    VALIDATEOBJECTREF(oref);

    TypeHandle clsHnd(type);

    HELPER_METHOD_FRAME_BEGIN_RET_1(oref);
    if (!ObjIsInstanceOf(OBJECTREFToObject(oref), clsHnd))
        oref = NULL;
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
        _ASSERTE(!typeHandle.IsTypeDesc());
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

#if CHECK_APP_DOMAIN_LEAKS
        if (g_pConfig->AppDomainLeaks())
        {
            object->SetAppDomain();
        }
#endif // CHECK_APP_DOMAIN_LEAKS

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

    _ASSERTE(!typeHnd.IsTypeDesc());                                   // we never use this helper for arrays
    MethodTable *pMT = typeHnd.AsMethodTable();
    _ASSERTE(pMT->IsRestored_NoLogging());

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

#if CHECK_APP_DOMAIN_LEAKS
        if (g_pConfig->AppDomainLeaks())
        {
            stringObject->SetAppDomain();
        }
#endif // CHECK_APP_DOMAIN_LEAKS

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
        SO_INTOLERANT;
    } CONTRACTL_END;

    STRINGREF result;
    result = SlowAllocateString(stringLength);
    
    return((StringObject*) OBJECTREFToObject(result));
}
HCIMPLEND_RAW

HCIMPL1(StringObject*, FramedAllocateString, DWORD stringLength)
{
    FCALL_CONTRACT;

    STRINGREF result = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_0();    // Set up a frame

    result = SlowAllocateString(stringLength);

    HELPER_METHOD_FRAME_END();
    return((StringObject*) OBJECTREFToObject(result));
}
HCIMPLEND

/*********************************************************************/
OBJECTHANDLE ConstructStringLiteral(CORINFO_MODULE_HANDLE scopeHnd, mdToken metaTok)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    _ASSERTE(TypeFromToken(metaTok) == mdtString);

    Module* module = GetModule(scopeHnd);


    // If our module is ngenned and we're calling this API, it means that we're not going through
    // the fixup mechanism for strings. This can happen 2 ways:
    //
    //      a) Lazy string object construction: This happens when JIT decides that initizalizing a
    //         string via fixup on method entry is very expensive. This is normally done for strings
    //         that appear in rarely executed blocks, such as throw blocks.
    //
    //      b) The ngen image isn't complete (it's missing classes), therefore we're jitting methods.
    //
    //  If we went ahead and called ResolveStringRef directly, we would be breaking the per module
    //  interning we're guaranteeing, so we will have to detect the case and handle it appropiately.
#ifdef FEATURE_PREJIT
    if (module->HasNativeImage() && module->IsNoStringInterning())
    {
        return module->ResolveStringRef(metaTok, module->GetAssembly()->Parent(), true);
    }
#endif
    return module->ResolveStringRef(metaTok, module->GetAssembly()->Parent(), false);
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
        array->SetArrayMethodTable(pArrayMT);
        _ASSERTE(static_cast<DWORD>(componentCount) == componentCount);
        array->m_NumComponents = static_cast<DWORD>(componentCount);

#if CHECK_APP_DOMAIN_LEAKS
        if (g_pConfig->AppDomainLeaks())
        {
            array->SetAppDomain();
        }
#endif // CHECK_APP_DOMAIN_LEAKS

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
        array->SetArrayMethodTable(pArrayMT);
        _ASSERTE(static_cast<DWORD>(componentCount) == componentCount);
        array->m_NumComponents = static_cast<DWORD>(componentCount);

#if CHECK_APP_DOMAIN_LEAKS
        if (g_pConfig->AppDomainLeaks())
        {
            array->SetAppDomain();
        }
#endif // CHECK_APP_DOMAIN_LEAKS

        return array;
    } while (false);

    // Tail call to the slow helper
    ENDFORBIDGC();
    return HCCALL2(JIT_NewArr1, arrayMT, size);
}
HCIMPLEND

//*************************************************************
// R2R-specific array allocation wrapper that extracts array method table from ArrayTypeDesc
//
HCIMPL2(Object*, JIT_NewArr1_R2R, CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size)
{
    FCALL_CONTRACT;

    TypeHandle arrayTypeHandle(arrayTypeHnd_);
    ArrayTypeDesc *pArrayTypeDesc = arrayTypeHandle.AsArray();
    MethodTable *pArrayMT = pArrayTypeDesc->GetTemplateMethodTable();

    ENDFORBIDGC();
    return HCCALL2(JIT_NewArr1, (CORINFO_CLASS_HANDLE)pArrayMT, size);
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

#ifdef _WIN64
    // Even though ECMA allows using a native int as the argument to newarr instruction
    // (therefore size is INT_PTR), ArrayBase::m_NumComponents is 32-bit, so even on 64-bit
    // platforms we can't create an array whose size exceeds 32 bits.
    if (size > INT_MAX)
        EX_THROW(EEMessageException, (kOverflowException, IDS_EE_ARRAY_DIMENSIONS_EXCEEDED));
#endif

    //
    // is this a primitive type?
    //

    CorElementType elemType = pArrayMT->GetArrayElementType();

    if (CorTypeInfo::IsPrimitiveType(elemType)
#ifdef FEATURE_64BIT_ALIGNMENT
        // On platforms where 64-bit types require 64-bit alignment and don't obtain it naturally force us
        // through the slow path where this will be handled.
        && (elemType != ELEMENT_TYPE_I8)
        && (elemType != ELEMENT_TYPE_U8)
        && (elemType != ELEMENT_TYPE_R8)
#endif
        )
    {
#ifdef _DEBUG
        if (g_pConfig->FastGCStressLevel()) {
            GetThread()->DisableStressHeap();
        }
#endif // _DEBUG

        // Disallow the creation of void[] (an array of System.Void)
        if (elemType == ELEMENT_TYPE_VOID)
            COMPlusThrow(kArgumentException);

        BOOL bAllocateInLargeHeap = FALSE;
#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
        if ((elemType == ELEMENT_TYPE_R8) && 
            (static_cast<DWORD>(size) >= g_pConfig->GetDoubleArrayToLargeObjectHeapThreshold()))
        {
            STRESS_LOG1(LF_GC, LL_INFO10, "Allocating double array of size %d to large object heap\n", size);
            bAllocateInLargeHeap = TRUE;
        }
#endif

        if (g_pPredefinedArrayTypes[elemType] == NULL)
        {
            TypeHandle elemTypeHnd = TypeHandle(MscorlibBinder::GetElementType(elemType));

            g_pPredefinedArrayTypes[elemType] = ClassLoader::LoadArrayTypeThrowing(elemTypeHnd, ELEMENT_TYPE_SZARRAY, 0).AsArray();
        }

        newArray = FastAllocatePrimitiveArray(pArrayMT, static_cast<DWORD>(size), bAllocateInLargeHeap);
    }
    else
    {
#ifdef _DEBUG
        if (g_pConfig->FastGCStressLevel()) {
            GetThread()->DisableStressHeap();
        }
#endif // _DEBUG
        INT32 size32 = (INT32)size;
        newArray = AllocateArrayEx(pArrayMT, &size32, 1);
    }

    HELPER_METHOD_FRAME_END();

    return(OBJECTREFToObject(newArray));
}
HCIMPLEND

/*********************************************************************
// Allocate a multi-dimensional array
*/
OBJECTREF allocNewMDArr(TypeHandle typeHnd, unsigned dwNumArgs, va_list args)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(dwNumArgs > 0);
    } CONTRACTL_END;

    // Get the arguments in the right order

    INT32* fwdArgList;

#ifdef _TARGET_X86_
    fwdArgList = (INT32*)args;

    // reverse the order
    INT32* p = fwdArgList;
    INT32* q = fwdArgList + (dwNumArgs-1);
    while (p < q)
    {
        INT32 t = *p; *p = *q; *q = t;
        p++; q--;
    }
#else
    // create an array where fwdArgList[0] == arg[0] ...
    fwdArgList = (INT32*) _alloca(dwNumArgs * sizeof(INT32));
    for (unsigned i = 0; i < dwNumArgs; i++)
    {
        fwdArgList[i] = va_arg(args, INT32);
    }
#endif

    return AllocateArrayEx(typeHnd, fwdArgList, dwNumArgs);
}

/*********************************************************************
// Allocate a multi-dimensional array with lower bounds specified.
// The caller pushes both sizes AND/OR bounds for every dimension
*/

HCIMPL2VA(Object*, JIT_NewMDArr, CORINFO_CLASS_HANDLE classHnd, unsigned dwNumArgs)
{
    FCALL_CONTRACT;

    OBJECTREF    ret = 0;
    HELPER_METHOD_FRAME_BEGIN_RET_1(ret);    // Set up a frame

    TypeHandle typeHnd(classHnd);
    typeHnd.CheckRestore();
    _ASSERTE(typeHnd.GetMethodTable()->IsArray());

    va_list dimsAndBounds;
    va_start(dimsAndBounds, dwNumArgs);

    ret = allocNewMDArr(typeHnd, dwNumArgs, dimsAndBounds);
    va_end(dimsAndBounds);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(ret);
}
HCIMPLEND

/*************************************************************/
HCIMPL3(Object*, JIT_NewMDArrNonVarArg, CORINFO_CLASS_HANDLE classHnd, unsigned dwNumArgs, INT32 * pArgList)
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

/*************************************************************/
/* returns '&array[idx], after doing all the proper checks */

#include <optsmallperfcritical.h>
HCIMPL3(void*, JIT_Ldelema_Ref, PtrArray* array, unsigned idx, CORINFO_CLASS_HANDLE type)
{
    FCALL_CONTRACT;

    RuntimeExceptionKind except;
       // This has been carefully arranged to ensure that in the common
        // case the branches are predicted properly (fall through).
        // and that we dont spill registers unnecessarily etc.
    if (array != 0)
        if (idx < array->GetNumComponents())
            if (array->GetArrayElementTypeHandle() == TypeHandle(type))
                return(&array->m_Array[idx]);
            else
                except = kArrayTypeMismatchException;
        else
            except = kIndexOutOfRangeException;
    else
        except = kNullReferenceException;

    FCThrow(except);
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

#if CHECK_APP_DOMAIN_LEAKS
    if (g_pConfig->AppDomainLeaks())
      (*pElement)->AssignAppDomain((*pArray)->GetAppDomain());
#endif // CHECK_APP_DOMAIN_LEAKS

    if (!ObjIsInstanceOf(*pElement, (*pArray)->GetArrayElementTypeHandle()))
        COMPlusThrow(kArrayTypeMismatchException);

    HELPER_METHOD_FRAME_END();

    return (LPVOID)0; // Used to aid epilog walker
}
HCIMPLEND

/****************************************************************************/
/* assigns 'val to 'array[idx], after doing all the proper checks */

HCIMPL3(void, JIT_Stelem_Ref_Portable, PtrArray* array, unsigned idx, Object *val)
{
    FCALL_CONTRACT;

    if (!array) 
    {
        FCThrowVoid(kNullReferenceException);
    }
    if (idx >= array->GetNumComponents()) 
    {
        FCThrowVoid(kIndexOutOfRangeException);
    }

    if (val) 
    {
        MethodTable *valMT = val->GetMethodTable();
        TypeHandle arrayElemTH = array->GetArrayElementTypeHandle();

#if CHECK_APP_DOMAIN_LEAKS
        // If the instance is agile or check agile
        if (g_pConfig->AppDomainLeaks() && !arrayElemTH.IsAppDomainAgile() && !arrayElemTH.IsCheckAppDomainAgile())
        {
            // FCALL_CONTRACT increase ForbidGC count.  Normally, HELPER_METHOD_FRAME macros decrease the count.
            // But to avoid perf hit, we manually decrease the count here before calling another HCCALL.
            ENDFORBIDGC();

            if (HCCALL2(ArrayStoreCheck,(Object**)&val, (PtrArray**)&array) != NULL)
            {
                // This return is never executed. It helps epilog walker to find its way out.
                return;
            }
        }
        else
#endif
        if (arrayElemTH != TypeHandle(valMT) && arrayElemTH != TypeHandle(g_pObjectClass))
        {   
            TypeHandle::CastResult result = ObjIsInstanceOfNoGC(val, arrayElemTH);
            if (result != TypeHandle::CanCast)
            {
                // FCALL_CONTRACT increase ForbidGC count.  Normally, HELPER_METHOD_FRAME macros decrease the count.
                // But to avoid perf hit, we manually decrease the count here before calling another HCCALL.
                ENDFORBIDGC();

                if (HCCALL2(ArrayStoreCheck,(Object**)&val, (PtrArray**)&array) != NULL)
                {
                    // This return is never executed. It helps epilog walker to find its way out.
                    return;
                }
            }
        }

#ifdef _TARGET_ARM64_
        SetObjectReferenceUnchecked((OBJECTREF*)&array->m_Array[idx], ObjectToOBJECTREF(val));
#else
        // The performance gain of the optimized JIT_Stelem_Ref in
        // jitinterfacex86.cpp is mainly due to calling JIT_WriteBarrier
        // By calling write barrier directly here,
        // we can avoid translating in-line assembly from MSVC to gcc
        // while keeping most of the performance gain.
        HCCALL2(JIT_WriteBarrier, (Object **)&array->m_Array[idx], val);
#endif

    }
    else
    {
        // no need to go through write-barrier for NULL
        ClearObjectReference(&array->m_Array[idx]);
    }
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

    TypeHandle clsHnd(type);

    _ASSERTE(!clsHnd.IsTypeDesc());  // we never use this helper for arrays

    MethodTable *pMT = clsHnd.AsMethodTable();

    pMT->CheckRestore();

    // You can only box valuetypes
    if (!pMT->IsValueType())
        COMPlusThrow(kInvalidCastException, W("Arg_ObjObj"));

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
        return;
    }

    // Fall back to a framed helper that handles type equivalence.
    ENDFORBIDGC();
    HCCALL3(JIT_Unbox_Nullable_Framed, destPtr, typeMT, objRef);
}
HCIMPLEND
    
/*************************************************************/
/* framed helper that handles full-blown type equivalence */
NOINLINE HCIMPL2(LPVOID, JIT_Unbox_Helper_Framed, CORINFO_CLASS_HANDLE type, Object* obj)
{
    FCALL_CONTRACT;

    LPVOID result = NULL;

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    HELPER_METHOD_FRAME_BEGIN_RET_1(objRef);
    if (TypeHandle(type).IsEquivalentTo(objRef->GetTypeHandle()))
    {
        // the structures are equivalent
        result = objRef->GetData();
    }
    else
    {
        COMPlusThrowInvalidCastException(&objRef, TypeHandle(type));
    }
    HELPER_METHOD_FRAME_END();

    return result;
}
HCIMPLEND

/*************************************************************/
/* the uncommon case for the helper below (allowing enums to be unboxed
   as their underlying type */
LPVOID __fastcall JIT_Unbox_Helper(CORINFO_CLASS_HANDLE type, Object* obj)
{
    FCALL_CONTRACT;

    TypeHandle typeHnd(type);

    CorElementType type1 = typeHnd.GetInternalCorElementType();

        // we allow enums and their primtive type to be interchangable

    MethodTable* pMT2 = obj->GetMethodTable();
    CorElementType type2 = pMT2->GetInternalCorElementType();
    if (type1 == type2)
    {
        MethodTable* pMT1 = typeHnd.GetMethodTable();
        if (pMT1 && (pMT1->IsEnum() || pMT1->IsTruePrimitive()) &&
            (pMT2->IsEnum() || pMT2->IsTruePrimitive()))
        {
            _ASSERTE(CorTypeInfo::IsPrimitiveType_NoThrow(type1));
            return(obj->GetData());
        }
    }

    // Even less common cases (type equivalence) go to a framed helper.
    ENDFORBIDGC();
    return HCCALL2(JIT_Unbox_Helper_Framed, type, obj);
}

/*************************************************************/
HCIMPL2(LPVOID, JIT_Unbox, CORINFO_CLASS_HANDLE type, Object* obj)
{
    FCALL_CONTRACT;

    TypeHandle typeHnd(type);
    VALIDATEOBJECT(obj);
    _ASSERTE(!typeHnd.IsTypeDesc());       // value classes are always unshared

        // This has been tuned so that branch predictions are good
        // (fall through for forward branches) for the common case
    if (obj != NULL) {
        if (obj->GetMethodTable() == typeHnd.AsMethodTable())
            return(obj->GetData());
        else {
                // Stuff the uncommon case into a helper so that
                // its register needs don't cause spills that effect
                // the common case above.
            return JIT_Unbox_Helper(type, obj);
        }
    }

    FCThrow(kNullReferenceException);
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


//========================================================================
//
//      GENERICS HELPERS
//
//========================================================================

/***********************************************************************/
// JIT_GenericHandle and its cache
// 
// Perform a "polytypic" operation related to shared generic code at runtime, possibly filling in an entry in
// either a generic dictionary cache assocaited with a descriptor or placing an entry in the global
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
//         where C<repr> is the represntative instantiation of C<exact>'
//     * the relevant dictionary will be the one attached to C<exact'>.
//         
// The JitGenericHandleCache is a global data structure shared across all application domains. It is only
// used if generic dictionaries have overflowed. It is flushed each time an application domain is unloaded.

struct JitGenericHandleCacheKey
{
    JitGenericHandleCacheKey(CORINFO_CLASS_HANDLE classHnd, CORINFO_METHOD_HANDLE methodHnd, void *signature, BaseDomain* pDomain=NULL)
    {
        LIMITED_METHOD_CONTRACT;        
        m_Data1 = (size_t)classHnd;
        m_Data2 = (size_t)methodHnd;
        m_Data3 = (size_t)signature;
        m_pDomainAndType = 0 | (size_t)pDomain;
    }

    JitGenericHandleCacheKey(MethodTable* pMT, CORINFO_CLASS_HANDLE classHnd, CORINFO_METHOD_HANDLE methodHnd, BaseDomain* pDomain=NULL)
    {
        LIMITED_METHOD_CONTRACT;
        m_Data1 = (size_t)pMT;
        m_Data2 = (size_t)classHnd;
        m_Data3 = (size_t)methodHnd;
        m_pDomainAndType = 1 | (size_t)pDomain;
    }

    size_t GetType() const
    {
        LIMITED_METHOD_CONTRACT;
        return (m_pDomainAndType & 1);
    }

    BaseDomain* GetDomain() const
    {
        LIMITED_METHOD_CONTRACT;
        return (BaseDomain*)(m_pDomainAndType & ~1);
    }

    size_t  m_Data1;
    size_t  m_Data2;
    size_t  m_Data3;

    size_t  m_pDomainAndType; // Which domain the entry belongs to. Not actually part of the key.
                        // Used only so we can scrape the table on AppDomain termination.
                        // NULL appdomain means that the entry should be scratched 
                        // on any appdomain unload.
                        //
                        // The lowest bit is used to indicate the type of the entry:
                        //  0 - JIT_GenericHandle entry
                        //  1 - JIT_VirtualFunctionPointer entry
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
            (e1->GetType() == e2->GetType()) &&
            // Any domain will work if the lookup key does not specify it
            ((e2->GetDomain() == NULL) || (e1->GetDomain() == e2->GetDomain()));
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
void ClearJitGenericHandleCache(AppDomain *pDomain)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;


    // We call this on every AppDomain unload, because entries in the cache might include
    // pointers into the AppDomain being unloaded.  We would prefer to
    // only flush entries that have that are no longer valid, but the entries don't yet contain
    // enough information to do that.  However everything in the cache can be found again by calling
    // loader functions, and the total number of entries in the cache is typically very small (indeed
    // normally the cache is not used at all - it is only used when the generic dictionaries overflow).
    if (g_pJitGenericHandleCache)
    {
        // It's not necessary to take the lock here because this function should only be called when EE is suspended,
        // the lock is only taken to fullfill the threadsafety check and to be consistent. If the lock becomes a problem, we
        // could put it in a "ifdef _DEBUG" block
        CrstHolder lock(&g_pJitGenericHandleCacheCrst);
        EEHashTableIteration iter;
        g_pJitGenericHandleCache->IterateStart(&iter);
        BOOL keepGoing = g_pJitGenericHandleCache->IterateNext(&iter);
        while(keepGoing)
        {
            const JitGenericHandleCacheKey *key = g_pJitGenericHandleCache->IterateGetKey(&iter);
            BaseDomain* pKeyDomain = key->GetDomain();
            if (pKeyDomain == pDomain || pKeyDomain == NULL
                // We compute fake domain for types during NGen (see code:ClassLoader::ComputeLoaderModule).
                // To avoid stale handles, we need to clear the cache unconditionally during NGen.
                || IsCompilationProcess())
            {
                // Advance the iterator before we delete!!  See notes in EEHash.h
                keepGoing = g_pJitGenericHandleCache->IterateNext(&iter);
                g_pJitGenericHandleCache->DeleteValue(key);
            }
            else
            {
                keepGoing = g_pJitGenericHandleCache->IterateNext(&iter);
            }
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
 
    MethodTable * pDeclaringMT = NULL;

    if (pMT != NULL)
    {
        ULONG dictionaryIndex = 0;

        if (pModule != NULL)
        {
#ifdef _DEBUG
            // Only in R2R mode are the module, dictionary index and dictionary slot provided as an input
            _ASSERTE(dictionaryIndexAndSlot != -1);
            _ASSERT(ExecutionManager::FindReadyToRunModule(dac_cast<TADDR>(signature)) == pModule);
#endif
            dictionaryIndex = (dictionaryIndexAndSlot >> 16);
        }
        else
        {
            SigPointer ptr((PCCOR_SIGNATURE)signature);

            ULONG kind; // DictionaryEntryKind
            IfFailThrow(ptr.GetData(&kind));

            // We need to normalize the class passed in (if any) for reliability purposes. That's because preparation of a code region that
            // contains these handle lookups depends on being able to predict exactly which lookups are required (so we can pre-cache the
            // answers and remove any possibility of failure at runtime). This is hard to do if the lookup (in this case the lookup of the
            // dictionary overflow cache) is keyed off the somewhat arbitrary type of the instance on which the call is made (we'd need to
            // prepare for every possible derived type of the type containing the method). So instead we have to locate the exactly
            // instantiated (non-shared) super-type of the class passed in.

            _ASSERTE(dictionaryIndexAndSlot == -1);
            IfFailThrow(ptr.GetData(&dictionaryIndex));
        }

        pDeclaringMT = pMT;
        for (;;)
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
        BaseDomain *pDictDomain = NULL;

        if (pMT != NULL)
        {
            pDictDomain = pDeclaringMT->GetDomain();
        }
        else
        {
            pDictDomain = pMD->GetDomain();
        }

        // Add the normalized key (pDeclaringMT) here so that future lookups of any
        // inherited types are faster next time rather than just just for this specific pMT.
        JitGenericHandleCacheKey key((CORINFO_CLASS_HANDLE)pDeclaringMT, (CORINFO_METHOD_HANDLE)pMD, signature, pDictDomain);
        AddToGenericHandleCache(&key, (HashDatum)result);
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
        PRECONDITION(GetMethod(methodHnd)->IsRestored());
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
        PRECONDITION(GetMethod(methodHnd)->IsRestored());
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
        PRECONDITION(GetMethod(methodHnd)->IsRestored());
        PRECONDITION(CheckPointer(signature));
    } CONTRACTL_END;

    g_IBCLogger.LogMethodDescAccess(GetMethod(methodHnd));

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
        PRECONDITION(TypeHandle(classHnd).IsRestored());
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
        PRECONDITION(TypeHandle(classHnd).IsRestored());
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
        PRECONDITION(TypeHandle(classHnd).IsRestored());
        PRECONDITION(CheckPointer(signature));
    } CONTRACTL_END;

    g_IBCLogger.LogMethodTableAccess((MethodTable *)classHnd);

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

        // The cache can be used only if MethodTable is a real one
        if (!objRef->IsTransparentProxy())
        {
            // This is not a critical entry - no need to specify appdomain affinity
            JitGenericHandleCacheKey key(objRef->GetMethodTable(), classHnd, methodHnd);
            AddToGenericHandleCache(&key, (HashDatum)addr);
        }
    }

    HELPER_METHOD_FRAME_END();

    return addr;
}
HCIMPLEND

HCIMPL2(VOID, JIT_GetRuntimeFieldHandle, Object ** destPtr, CORINFO_FIELD_HANDLE field)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    FieldDesc *pField = (FieldDesc *)field;
    SetObjectReference((OBJECTREF*) destPtr,
                       pField->GetStubFieldInfo(), GetAppDomain());

    HELPER_METHOD_FRAME_END();
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

HCIMPL2(VOID, JIT_GetRuntimeMethodHandle, Object ** destPtr, CORINFO_METHOD_HANDLE method)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    MethodDesc *pMethod = (MethodDesc *)method;
    SetObjectReference((OBJECTREF*) destPtr,
                       pMethod->GetStubMethodInfo(), GetAppDomain());

    HELPER_METHOD_FRAME_END();
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

HCIMPL2(VOID, JIT_GetRuntimeTypeHandle, Object ** destPtr, CORINFO_CLASS_HANDLE type)
{
    FCALL_CONTRACT;

    TypeHandle typeHnd(type);

    if (!typeHnd.IsTypeDesc())
    {
        // Most common... and fastest case
        OBJECTREF typePtr = typeHnd.AsMethodTable()->GetManagedClassObjectIfExists();
        if (typePtr != NULL)
        {
            SetObjectReference((OBJECTREF*) destPtr,
                               typePtr, GetAppDomain());
            return;
        }
    }

    HELPER_METHOD_FRAME_BEGIN_0();

    SetObjectReference((OBJECTREF*) destPtr,
                       typeHnd.GetManagedClassObject(), GetAppDomain());

    HELPER_METHOD_FRAME_END();
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

#ifdef _DEBUG
    Thread *pThread = GetThread();
    DWORD lockCount = pThread->m_dwLockCount;
#endif
    if (GET_THREAD()->CatchAtSafePointOpportunistic())
    {
        GET_THREAD()->PulseGCMode();
    }
    objRef->EnterObjMonitor();
    _ASSERTE ((objRef->GetSyncBlock()->GetMonitor()->m_Recursion == 1 && pThread->m_dwLockCount == lockCount + 1) ||
              pThread->m_dwLockCount == lockCount);
    if (pbLockTaken != 0) *pbLockTaken = 1;

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
}

/*********************************************************************/
NOINLINE static void JIT_MonContention_Helper(Object* obj, BYTE* pbLockTaken, LPVOID __me)
{
    FC_INNER_PROLOG_NO_ME_SETUP();

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, objRef);    

    GCPROTECT_BEGININTERIOR(pbLockTaken);

    objRef->GetSyncBlock()->QuickGetMonitor()->Contention();
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
    
    AwareLock::EnterHelperResult result;
    Thread * pCurThread;

    if (obj == NULL)
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
        MONHELPER_STATE(*pbLockTaken = 1;)
        return;
    }
    else
    if (result == AwareLock::EnterHelperResult_Contention)
    {
        AwareLock::EnterHelperResult resultSpin = obj->EnterObjMonitorHelperSpin(pCurThread);
        if (resultSpin == AwareLock::EnterHelperResult_Entered)
        {
            MONHELPER_STATE(*pbLockTaken = 1;)
            return;
        }
        if (resultSpin == AwareLock::EnterHelperResult_Contention)
        {
            FC_INNER_RETURN_VOID(JIT_MonContention_Helper(obj, MONHELPER_ARG, GetEEFuncEntryPointMacro(JIT_MonEnter)));
        }
    }

FramedLockHelper:
    FC_INNER_RETURN_VOID(JIT_MonEnter_Helper(obj, MONHELPER_ARG, GetEEFuncEntryPointMacro(JIT_MonEnter)));
}
HCIMPLEND

HCIMPL1(void, JIT_MonEnter_Portable, Object* obj)
{
    FCALL_CONTRACT;

    Thread * pCurThread;
    AwareLock::EnterHelperResult result;
    
    if (obj == NULL)
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
        return;
    }
    else
    if (result == AwareLock::EnterHelperResult_Contention)
    {
        AwareLock::EnterHelperResult resultSpin = obj->EnterObjMonitorHelperSpin(pCurThread);
        if (resultSpin == AwareLock::EnterHelperResult_Entered)
        {
            return;
        }
        if (resultSpin == AwareLock::EnterHelperResult_Contention)
        {
            FC_INNER_RETURN_VOID(JIT_MonContention_Helper(obj, NULL, GetEEFuncEntryPointMacro(JIT_MonEnter)));
        }
    }

FramedLockHelper:
    FC_INNER_RETURN_VOID(JIT_MonEnter_Helper(obj, NULL, GetEEFuncEntryPointMacro(JIT_MonEnter)));
}
HCIMPLEND

HCIMPL2(void, JIT_MonReliableEnter_Portable, Object* obj, BYTE* pbLockTaken)
{
    FCALL_CONTRACT;

    Thread * pCurThread;
    AwareLock::EnterHelperResult result;
    
    if (obj == NULL)
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
    else
    if (result == AwareLock::EnterHelperResult_Contention)
    {
        AwareLock::EnterHelperResult resultSpin = obj->EnterObjMonitorHelperSpin(pCurThread);
        if (resultSpin == AwareLock::EnterHelperResult_Entered)
        {
            *pbLockTaken = 1;
            return;
        }
        if (resultSpin == AwareLock::EnterHelperResult_Contention)
        {
            FC_INNER_RETURN_VOID(JIT_MonContention_Helper(obj, pbLockTaken, GetEEFuncEntryPointMacro(JIT_MonReliableEnter)));
        }
    }

FramedLockHelper:
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
    else
    if (result == AwareLock::EnterHelperResult_Contention)
    {
        if (timeOut == 0)
            return;

        AwareLock::EnterHelperResult resultSpin = obj->EnterObjMonitorHelperSpin(pCurThread);
        if (resultSpin == AwareLock::EnterHelperResult_Entered)
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

    TESTHOOKCALL(AppDomainCanBeUnloaded(GET_THREAD()->GetDomain()->GetId().m_dwId,FALSE));
    
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

    TESTHOOKCALL(AppDomainCanBeUnloaded(GET_THREAD()->GetDomain()->GetId().m_dwId,FALSE));
    
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
    else
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
    else
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

    if (lock->EnterHelper(pCurThread) == AwareLock::EnterHelperResult_Entered)
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

    TESTHOOKCALL(AppDomainCanBeUnloaded(GET_THREAD()->GetDomain()->GetId().m_dwId,FALSE));
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

    TESTHOOKCALL(AppDomainCanBeUnloaded(GET_THREAD()->GetDomain()->GetId().m_dwId,FALSE));
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

/*********************************************************************/
// JITutil_Mon* are helpers than handle slow paths for JIT_Mon* methods
// implemented in assembly. They are not doing any spinning compared 
// to the full fledged portable implementations above.
/*********************************************************************/

/*********************************************************************/
HCIMPL_MONHELPER(JITutil_MonEnterWorker, Object* obj)
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    // The following makes sure that Monitor.Enter shows up on thread abort
    // stack walks (otherwise Monitor.Enter called within a CER can block a
    // thread abort indefinitely). Setting the __me internal variable (normally
    // only set for fcalls) will cause the helper frame below to be able to
    // backtranslate into the method desc for the Monitor.Enter fcall.
    //
    // Note that we need explicitly initialize Monitor.Enter fcall in
    // code:SystemDomain::LoadBaseSystemClasses to make this work in the case
    // where the first call ever to Monitor.Enter is done as JIT helper 
    // for synchronized method.
    __me = GetEEFuncEntryPointMacro(JIT_MonEnter);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_EXACT_DEPTH, objRef);

    if (objRef == NULL)
        COMPlusThrow(kArgumentNullException);

    MONHELPER_STATE(GCPROTECT_BEGININTERIOR(pbLockTaken);)

#ifdef _DEBUG
    Thread *pThread = GetThread();
    DWORD lockCount = pThread->m_dwLockCount;
#endif
    if (GET_THREAD()->CatchAtSafePointOpportunistic())
    {
        GET_THREAD()->PulseGCMode();
    }
    objRef->EnterObjMonitor();
    _ASSERTE ((objRef->GetSyncBlock()->GetMonitor()->m_Recursion == 1 && pThread->m_dwLockCount == lockCount + 1) ||
              pThread->m_dwLockCount == lockCount);
    MONHELPER_STATE(if (pbLockTaken != 0) *pbLockTaken = 1;)

    MONHELPER_STATE(GCPROTECT_END();)
    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/

// This helper is only ever used as part of FCall, but it is implemented using HCIMPL macro
// so that it can be tail called from assembly helper without triggering asserts in debug.
HCIMPL2(void, JITutil_MonReliableEnter, Object* obj, BYTE* pbLockTaken)
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    // The following makes sure that Monitor.Enter shows up on thread abort
    // stack walks (otherwise Monitor.Enter called within a CER can block a
    // thread abort indefinitely). Setting the __me internal variable (normally
    // only set for fcalls) will cause the helper frame below to be able to
    // backtranslate into the method desc for the Monitor.Enter fcall.
    __me = GetEEFuncEntryPointMacro(JIT_MonReliableEnter);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_EXACT_DEPTH, objRef);    

    if (objRef == NULL)
        COMPlusThrow(kArgumentNullException);

    GCPROTECT_BEGININTERIOR(pbLockTaken);

#ifdef _DEBUG
    Thread *pThread = GetThread();
    DWORD lockCount = pThread->m_dwLockCount;
#endif
    if (GET_THREAD()->CatchAtSafePointOpportunistic())
    {
        GET_THREAD()->PulseGCMode();
    }
    objRef->EnterObjMonitor();
    _ASSERTE ((objRef->GetSyncBlock()->GetMonitor()->m_Recursion == 1 && pThread->m_dwLockCount == lockCount + 1) ||
              pThread->m_dwLockCount == lockCount);
    *pbLockTaken = 1;

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();
}
HCIMPLEND


/*********************************************************************/

// This helper is only ever used as part of FCall, but it is implemented using HCIMPL macro
// so that it can be tail called from assembly helper without triggering asserts in debug.
HCIMPL3(void, JITutil_MonTryEnter, Object* obj, INT32 timeOut, BYTE* pbLockTaken)
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    BOOL result = FALSE;

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    // The following makes sure that Monitor.TryEnter shows up on thread
    // abort stack walks (otherwise Monitor.TryEnter called within a CER can
    // block a thread abort for long periods of time). Setting the __me internal
    // variable (normally only set for fcalls) will cause the helper frame below
    // to be able to backtranslate into the method desc for the Monitor.TryEnter
    // fcall.
    __me = GetEEFuncEntryPointMacro(JIT_MonTryEnter);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_EXACT_DEPTH, objRef);    

    if (objRef == NULL)
        COMPlusThrow(kArgumentNullException);

    if (timeOut < -1)
        COMPlusThrow(kArgumentOutOfRangeException);

    GCPROTECT_BEGININTERIOR(pbLockTaken);

    if (GET_THREAD()->CatchAtSafePointOpportunistic())
    {
        GET_THREAD()->PulseGCMode();
    }

    result = objRef->TryEnterObjMonitor(timeOut);
    *pbLockTaken = result != FALSE;

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
HCIMPL_MONHELPER(JITutil_MonExitWorker, Object* obj)
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    MONHELPER_STATE(if (pbLockTaken != NULL && *pbLockTaken == 0) return;)

    OBJECTREF objRef = ObjectToOBJECTREF(obj);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_NO_THREAD_ABORT|Frame::FRAME_ATTR_EXACT_DEPTH, objRef);    
    
    if (objRef == NULL)
        COMPlusThrow(kArgumentNullException);

    if (!objRef->LeaveObjMonitor())
        COMPlusThrow(kSynchronizationLockException);

    MONHELPER_STATE(if (pbLockTaken != 0) *pbLockTaken = 0;)

    TESTHOOKCALL(AppDomainCanBeUnloaded(GET_THREAD()->GetDomain()->GetId().m_dwId,FALSE));
    
    if (GET_THREAD()->IsAbortRequested()) {
        GET_THREAD()->HandleThreadAbort();
    }

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
// A helper for JIT_MonEnter that is on the callee side of an ecall
// frame and handles the contention case.

HCIMPL_MONHELPER(JITutil_MonContention, AwareLock* lock)
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    // The following makes sure that Monitor.Enter shows up on thread abort
    // stack walks (otherwise Monitor.Enter called within a CER can block a
    // thread abort indefinitely). Setting the __me internal variable (normally
    // only set for fcalls) will cause the helper frame below to be able to
    // backtranslate into the method desc for the Monitor.Enter fcall.
    __me = GetEEFuncEntryPointMacro(JIT_MonEnter);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB(Frame::FRAME_ATTR_EXACT_DEPTH);    
    MONHELPER_STATE(GCPROTECT_BEGININTERIOR(pbLockTaken);)

#ifdef _DEBUG
    Thread *pThread = GetThread();
    DWORD lockCount = pThread->m_dwLockCount;
#endif
    lock->Contention();
    _ASSERTE (pThread->m_dwLockCount == lockCount + 1);
    MONHELPER_STATE(if (pbLockTaken != 0) *pbLockTaken = 1;)

    MONHELPER_STATE(GCPROTECT_END();)
    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

// This helper is only ever used as part of FCall, but it is implemented using HCIMPL macro
// so that it can be tail called from assembly helper without triggering asserts in debug.
HCIMPL2(void, JITutil_MonReliableContention, AwareLock* lock, BYTE* pbLockTaken)
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    // The following makes sure that Monitor.Enter shows up on thread abort
    // stack walks (otherwise Monitor.Enter called within a CER can block a
    // thread abort indefinitely). Setting the __me internal variable (normally
    // only set for fcalls) will cause the helper frame below to be able to
    // backtranslate into the method desc for the Monitor.Enter fcall.
    __me = GetEEFuncEntryPointMacro(JIT_MonReliableEnter);

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB(Frame::FRAME_ATTR_EXACT_DEPTH);    
    GCPROTECT_BEGININTERIOR(pbLockTaken);

#ifdef _DEBUG
    Thread *pThread = GetThread();
    DWORD lockCount = pThread->m_dwLockCount;
#endif
    lock->Contention();
    _ASSERTE (pThread->m_dwLockCount == lockCount + 1);
    *pbLockTaken = 1;

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*********************************************************************/
// A helper for JIT_MonExit and JIT_MonExitStatic that is on the
// callee side of an ecall frame and handles cases that might allocate,
// throw or block.
HCIMPL_MONHELPER(JITutil_MonSignal, AwareLock* lock)
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    // Monitor helpers are used as both hcalls and fcalls, thus we need exact depth.
    HELPER_METHOD_FRAME_BEGIN_ATTRIB(Frame::FRAME_ATTR_EXACT_DEPTH | Frame::FRAME_ATTR_NO_THREAD_ABORT);

    lock->Signal();
    MONHELPER_STATE(if (pbLockTaken != 0) *pbLockTaken = 0;)

    TESTHOOKCALL(AppDomainCanBeUnloaded(GET_THREAD()->GetDomain()->GetId().m_dwId,FALSE));
    
    if (GET_THREAD()->IsAbortRequested()) {
        GET_THREAD()->HandleThreadAbort();
    }

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

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

HCIMPL1(void, IL_Throw,  Object* obj)
{
    FCALL_CONTRACT;

    // This "violation" isn't a really a violation. 
    // We are calling a assembly helper that can't have an SO Tolerance contract
    CONTRACT_VIOLATION(SOToleranceViolation);
    /* Make no assumptions about the current machine state */
    ResetCurrentContext();

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    OBJECTREF oref = ObjectToOBJECTREF(obj);

#if defined(_DEBUG) && defined(_TARGET_X86_)
    __helperframe.InsureInit(false, NULL);
    g_ExceptionEIP = (LPVOID)__helperframe.GetReturnAddress();
#endif // defined(_DEBUG) && defined(_TARGET_X86_)


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
        if (g_CLRPolicyRequested &&
            oref->GetMethodTable() == g_pOutOfMemoryExceptionClass)
        {
            EEPolicy::HandleOutOfMemory();
        }

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

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
    if (!g_pConfig->LegacyCorruptedStateExceptionsPolicy())
    {
        // Within the VM, we could have thrown and caught a managed exception. This is done by
        // RaiseTheException that will flag that exception's corruption severity to be used
        // incase it leaks out to managed code.
        //
        // If it does not leak out, but ends up calling into managed code that throws,
        // we will come here. In such a case, simply reset the corruption-severity
        // since we want the exception being thrown to have its correct severity set
        // when CLR's managed code exception handler sets it.

        ThreadExceptionState *pExState = GetThread()->GetExceptionState();
        pExState->SetLastActiveExceptionCorruptionSeverity(NotSet);
    }
#endif // FEATURE_CORRUPTING_EXCEPTIONS

    RaiseTheExceptionInternalOnly(oref, FALSE);

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND

/*************************************************************/

HCIMPL0(void, IL_Rethrow)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    OBJECTREF throwable = GetThread()->GetThrowable();
    if (throwable != NULL)
    {
        if (g_CLRPolicyRequested &&
            throwable->GetMethodTable() == g_pOutOfMemoryExceptionClass)
        {
            EEPolicy::HandleOutOfMemory();
        }

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
HCIMPL1(void, JIT_SecurityUnmanagedCodeException, CORINFO_CLASS_HANDLE typeHnd_)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();    // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_EXCEPTION);    // Set up a frame

    COMPlusThrow(kSecurityException);

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
        SO_NOT_MAINLINE; // If process is coming down, SO probe is not going to do much good
    } CONTRACTL_END;

    LOG((LF_ALWAYS, LL_FATALERROR, "Unsafe buffer security check failure: Buffer overrun detected"));

#ifdef _DEBUG
    if (g_pConfig->fAssertOnFailFast())
        _ASSERTE(!"About to FailFast. set ComPlus_AssertOnFailFast=0 if this is expected");
#endif

#ifndef FEATURE_PAL
    // Use the function provided by the C runtime.
    //
    // Ideally, this function is called directly from managed code so
    // that the address of the managed function will be included in the
    // error log. However, this function is also used by the stackwalker.
    // To keep things simple, we just call it from here.
#if defined(_TARGET_X86_)
    __report_gsfailure();
#else // !defined(_TARGET_X86_)
    // On AMD64/IA64/ARM, we need to pass a stack cookie, which will be saved in the context record 
    // that is used to raise the buffer-overrun exception by __report_gsfailure.
    __report_gsfailure((ULONG_PTR)0);
#endif // defined(_TARGET_X86_)
#else // FEATURE_PAL
    if(ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, FailFast))
    {
        // Fire an ETW FailFast event
        FireEtwFailFast(W("Unsafe buffer security check failure: Buffer overrun detected"),
                       (const PVOID)GetThread()->GetFrame()->GetIP(), 
                       STATUS_STACK_BUFFER_OVERRUN, 
                       COR_E_EXECUTIONENGINE, 
                       GetClrInstanceId());
    }

    TerminateProcess(GetCurrentProcess(), STATUS_STACK_BUFFER_OVERRUN);
#endif // !FEATURE_PAL
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
    StaticAccessCheckContext accessContext(pCallerMD);

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
    StaticAccessCheckContext accessContext(pCallerMD);

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
    StaticAccessCheckContext accessContext(pCallerMD);

    ThrowTypeAccessException(&accessContext, TypeHandle(callee).GetMethodTable());

    HELPER_METHOD_FRAME_END();
}
HCIMPLEND;

//========================================================================
//
//      SECURITY HELPERS
//
//========================================================================

HCIMPL2(void, JIT_DelegateSecurityCheck, CORINFO_CLASS_HANDLE delegateHnd, CORINFO_METHOD_HANDLE calleeMethodHnd)
{
    FCALL_CONTRACT;
}
HCIMPLEND

HCIMPL4(void, JIT_MethodAccessCheck, CORINFO_METHOD_HANDLE callerMethodHnd, CORINFO_METHOD_HANDLE calleeMethodHnd, CORINFO_CLASS_HANDLE calleeTypeHnd, CorInfoSecurityRuntimeChecks check)
{
    FCALL_CONTRACT;
}
HCIMPLEND

HCIMPL3(void, JIT_FieldAccessCheck, CORINFO_METHOD_HANDLE callerMethodHnd, CORINFO_FIELD_HANDLE calleeFieldHnd, CorInfoSecurityRuntimeChecks check)
{
    FCALL_CONTRACT;
}
HCIMPLEND

HCIMPL3(void, JIT_ClassAccessCheck, CORINFO_METHOD_HANDLE callerMethodHnd, CORINFO_CLASS_HANDLE calleeClassHnd, CorInfoSecurityRuntimeChecks check)
{
    FCALL_CONTRACT;
}
HCIMPLEND

HCIMPL2(void, JIT_Security_Prolog, CORINFO_METHOD_HANDLE methHnd_, OBJECTREF* ppFrameSecDesc)
{
    FCALL_CONTRACT;
}
HCIMPLEND

HCIMPL2(void, JIT_Security_Prolog_Framed, CORINFO_METHOD_HANDLE methHnd_, OBJECTREF* ppFrameSecDesc)
{
    FCALL_CONTRACT;
}
HCIMPLEND

HCIMPL1(void, JIT_VerificationRuntimeCheck, CORINFO_METHOD_HANDLE methHnd_)
{
    FCALL_CONTRACT;
}
HCIMPLEND


//========================================================================
//
//      DEBUGGER/PROFILER HELPERS
//
//========================================================================

/*********************************************************************/
// JIT_UserBreakpoint
// Called by the JIT whenever a cee_break instruction should be executed.
// This ensures that enough info will be pushed onto the stack so that
// we can continue from the exception w/o having special code elsewhere.
// Body of function is written by debugger team
// Args: None
//
// <TODO> make sure this actually gets called by all JITters</TODO>
// Note: this code is duplicated in the ecall in VM\DebugDebugger:Break,
// so propogate changes to there

HCIMPL0(void, JIT_UserBreakpoint)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();    // Set up a frame

#ifdef DEBUGGING_SUPPORTED
    FrameWithCookie<DebuggerExitFrame> __def;

    MethodDescCallSite breakCanThrow(METHOD__DEBUGGER__BREAK_CAN_THROW);

    // Call Diagnostic.Debugger.BreakCanThrow instead. This will make us demand 
    // UnmanagedCode permission if debugger is not attached.
    //
    breakCanThrow.Call((ARG_SLOT*)NULL);

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
// Body of this function is maintained by the debugger people.
HCIMPL0(void, JIT_DbgIsJustMyCode)
{
    FCALL_CONTRACT;
    SO_NOT_MAINLINE_FUNCTION;

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

#if !(defined(_TARGET_X86_) || defined(_WIN64))
void JIT_ProfilerEnterLeaveTailcallStub(UINT_PTR ProfilerHandle)
{
    return;
}
#endif // !(_TARGET_X86_ || _WIN64)

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

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();

    g_IBCLogger.LogMethodCodeAccess(GetMethod(methHnd_));

    HELPER_METHOD_FRAME_END_POLL();

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
    CopyValueClassUnchecked(dest, src, pMT);
    HELPER_METHOD_FRAME_END_POLL();

}
HCIMPLEND

/*************************************************************/
HCIMPL0(VOID, JIT_PollGC)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();

    Thread  *thread = GetThread();
    if (thread->CatchAtSafePointOpportunistic())    // Does someone want this thread stopped?
    {
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
    }
}
HCIMPLEND

/*************************************************************/
// For an inlined N/Direct call (and possibly for other places that need this service)
// we have noticed that the returning thread should trap for one reason or another.
// ECall sets up the frame.

#if defined(_TARGET_ARM_) || defined(_TARGET_AMD64_)
// The JIT expects this helper to preserve the return value on AMD64 and ARM. We should eventually
// switch other platforms to the same convention since it produces smaller code.
extern "C" FCDECL0(VOID, JIT_RareDisableHelper);
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
// This is called by the JIT after every instruction in fully interuptable
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
        FastInterlockExchange((LONG*) retInstrs), (LONG) JIT_StressGC_NOP);
#endif // _X86

    HELPER_METHOD_FRAME_END();
#endif // _DEBUG
}
HCIMPLEND



HCIMPL0(INT32, JIT_GetCurrentManagedThreadId)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();

    Thread * pThread = GetThread();
    return pThread->GetThreadId();
}
HCIMPLEND


/*********************************************************************/
/* we don't use HCIMPL macros because we don't want the overhead even in debug mode */

HCIMPL1_RAW(Object*, JIT_CheckObj, Object* obj)
{
    FCALL_CONTRACT;

    if (obj != 0) {
        MethodTable* pMT = obj->GetMethodTable();
        if (!pMT->ValidateWithPossibleAV()) {
            _ASSERTE(!"Bad Method Table");
            FreeBuildDebugBreak();
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

//========================================================================
//
//      INTEROP HELPERS
//
//========================================================================

#ifdef _WIN64

/**********************************************************************/
/* Fills out portions of an InlinedCallFrame for JIT64    */
/* The idea here is to allocate and initalize the frame to only once, */
/* regardless of how many PInvokes there are in the method            */
Thread * __stdcall JIT_InitPInvokeFrame(InlinedCallFrame *pFrame, PTR_VOID StubSecretArg)
{
    CONTRACTL
    {
        SO_TOLERANT;
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

#endif // _WIN64

//========================================================================
//
//      JIT HELPERS IMPLEMENTED AS FCALLS
//
//========================================================================

FCIMPL3(void, JitHelpers::UnsafeSetArrayElement, PtrArray* pPtrArrayUNSAFE, INT32 index, Object* objectUNSAFE) { 
    FCALL_CONTRACT;

    PTRARRAYREF pPtrArray = (PTRARRAYREF)pPtrArrayUNSAFE;
    OBJECTREF object = (OBJECTREF)objectUNSAFE;
    
    _ASSERTE(index < (INT32)pPtrArray->GetNumComponents());
    
    pPtrArray->SetAt(index, object);
}
FCIMPLEND

#ifdef _TARGET_ARM_
// This function is used from the FCallMemcpy for GC polling
EXTERN_C VOID FCallMemCpy_GCPoll()
{
    FC_INNER_PROLOG(FCallMemcpy);
 
    Thread  *thread = GetThread();
    // CommonTripThread does this check, but doing this to avoid raising the frames
    if (thread->CatchAtSafePointOpportunistic()) 
    {
        HELPER_METHOD_FRAME_BEGIN_0();
        CommonTripThread();
        HELPER_METHOD_FRAME_END();
    }
 
    FC_INNER_EPILOG();
}
#endif // _TARGET_ARM_

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

#if defined(_DEBUG) && (defined(_TARGET_AMD64_) || defined(_TARGET_X86_)) && !defined(FEATURE_PAL)
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

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
    SetJitHelperFunction(CORINFO_HELP_INIT_PINVOKE_FRAME, (void *)GenerateInitPInvokeFrameHelper()->GetEntryPoint());
#endif // _TARGET_X86_ || _TARGET_ARM_

    ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(GetThread), ECall::InternalGetCurrentThread);

    InitJitHelperLogging();

    g_pJitGenericHandleCacheCrst.Init(CrstJitGenericHandleCache, CRST_UNSAFE_COOPGC);

    // Allocate and initialize the table
    NewHolder <JitGenericHandleCache> tempGenericHandleCache (new JitGenericHandleCache());
    LockOwner sLock = {&g_pJitGenericHandleCacheCrst, IsOwnerOfCrst};
    if (!tempGenericHandleCache->Init(59, &sLock))
        COMPlusThrowOM();
    g_pJitGenericHandleCache = tempGenericHandleCache.Extract();
}

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM_)

NOINLINE void DoCopy(CONTEXT * ctx, void * pvTempStack, size_t cbTempStack, Thread * pThread, Frame * pNewFrame)
{
    // We need to ensure that copying pvTempStack onto our stack will not in
    // *ANY* way trash the context record (or our pointer to it) that we need
    // in order to restore context
    _ASSERTE((DWORD_PTR)&ctx + sizeof(ctx) < (DWORD_PTR)GetSP(ctx));

    CONTEXT ctx2;
    if ((DWORD_PTR)ctx + sizeof(*ctx) > (DWORD_PTR)GetSP(ctx))
    {
        // The context record is in danger, copy it down
        _ASSERTE((DWORD_PTR)&ctx2 + sizeof(ctx2) < (DWORD_PTR)GetSP(ctx));
        ctx2 = *ctx;

        // Clear any context that we didn't copy...
        ctx2.ContextFlags &= CONTEXT_ALL;
        ctx = &ctx2;
    }

    _ASSERTE((DWORD_PTR)ctx + sizeof(*ctx) <= (DWORD_PTR)GetSP(ctx));

    // DevDiv 189140 - use memmove because source and dest might overlap.
    memmove((void*)GetSP(ctx), pvTempStack, cbTempStack);

    if (pNewFrame != NULL)
    {
        // Now that the memmove above is complete, pNewFrame is actually pointing at a
        // TailCallFrame, and not garbage.  So it's safe to add pNewFrame to the Frame
        // chain.
        _ASSERTE(pThread != NULL);
        pThread->SetFrame(pNewFrame);
    }

    RtlRestoreContext(ctx, NULL);
}

//
// Mostly Architecture-agnostic RtlVirtualUnwind-based tail call helper...
//
// Can't use HCIMPL macro because it requires unwind, and this method *NEVER* unwinds.
//

#define INVOKE_COPY_ARGS_HELPER(helperFunc, arg1, arg2, arg3, arg4) ((pfnCopyArgs)helperFunc)(arg1, arg2, arg3, arg4)
void F_CALL_VA_CONV JIT_TailCall(PCODE copyArgs, PCODE target, ...)
{
    // Can't have a regular contract because we would never pop it
    // We only throw a stack overflow if needed, and we can't handle
    // a GC because the incoming parameters are totally unprotected.
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE

#ifndef FEATURE_PAL
    
    Thread *pThread = GetThread();

#ifdef FEATURE_HIJACK
    // We can't crawl the stack of a thread that currently has a hijack pending
    // (since the hijack routine won't be recognized by any code manager). So we
    // undo any hijack, the EE will re-attempt it later.
    pThread->UnhijackThread();
#endif

    ULONG_PTR establisherFrame = 0; 
    PVOID     handlerData = NULL;
    CONTEXT   ctx;

    // Unwind back to our caller in managed code
    static PT_RUNTIME_FUNCTION my_pdata;
    static ULONG_PTR           my_imagebase;

    ctx.ContextFlags = CONTEXT_ALL;
    RtlCaptureContext(&ctx);

    if (!VolatileLoadWithoutBarrier(&my_imagebase)) {
        ULONG_PTR imagebase = 0;
        my_pdata = RtlLookupFunctionEntry(GetIP(&ctx), &imagebase, NULL);
        InterlockedExchangeT(&my_imagebase, imagebase);
    }

    RtlVirtualUnwind(UNW_FLAG_NHANDLER, my_imagebase, GetIP(&ctx), my_pdata, &ctx, &handlerData, 
                     &establisherFrame, NULL);

    EECodeInfo codeInfo(GetIP(&ctx));

    // Now unwind back to our caller's caller
    establisherFrame = 0; 
    RtlVirtualUnwind(UNW_FLAG_NHANDLER, codeInfo.GetModuleBase(), GetIP(&ctx), codeInfo.GetFunctionEntry(), &ctx, &handlerData,
                     &establisherFrame, NULL);

    va_list     args;

    // Compute the space needed for arguments
    va_start(args, target);

    ULONG_PTR pGCLayout = 0;
    size_t    cbArgArea = INVOKE_COPY_ARGS_HELPER(copyArgs, args, NULL, NULL, (size_t)&pGCLayout);

    va_end(args);

    // reset (in case the helper walked them)
    va_start(args, target);

    // Fake call frame (if needed)
    size_t cbCopyFrame = 0;
    bool   fCopyDown = false;
    BYTE   rgFrameBuffer[sizeof(FrameWithCookie<TailCallFrame>)];
    Frame * pNewFrame = NULL;

#if defined(_TARGET_AMD64_)
#  define STACK_ADJUST_FOR_RETURN_ADDRESS  (sizeof(void*))
#  define STACK_ALIGN_MASK                 (0xF)
#elif defined(_TARGET_ARM_)
#  define STACK_ADJUST_FOR_RETURN_ADDRESS  (0)
#  define STACK_ALIGN_MASK                 (0x7)
#else
#error "Unknown tail call architecture"
#endif

    // figure out if we can re-use an existing TailCallHelperStub
    // or if we need to create a new one.
    if ((void*)GetIP(&ctx) == JIT_TailCallHelperStub_ReturnAddress) {
        TailCallFrame * pCurrentFrame = TailCallFrame::GetFrameFromContext(&ctx);
        _ASSERTE(pThread->GetFrame() == pCurrentFrame);
        // The caller was tail called, so we can re-use that frame
        // See if we need to enlarge the ArgArea
        // This can potentially enlarge cbArgArea to the size of the
        // existing TailCallFrame.
        const size_t endOfFrame = (size_t)pCurrentFrame - (size_t)sizeof(GSCookie);
        size_t cbOldArgArea = (endOfFrame - GetSP(&ctx));
        if (cbOldArgArea >= cbArgArea) {
            cbArgArea = cbOldArgArea;
        }
        else {
            SetSP(&ctx, (endOfFrame - cbArgArea));
            fCopyDown = true;
        }

        // Reset the GCLayout
        pCurrentFrame->SetGCLayout((TADDR)pGCLayout);

        // We're jumping to the new method, not calling it
        // so make room for the return address that the 'call'
        // would have pushed.
        SetSP(&ctx, GetSP(&ctx) - STACK_ADJUST_FOR_RETURN_ADDRESS);
    }
    else {
        // Create a fake fixed frame as if the new method was called by
        // TailCallHelperStub asm stub and did an
        // alloca, then called the target method.
        cbCopyFrame = sizeof(rgFrameBuffer);
        FrameWithCookie<TailCallFrame> * CookieFrame = new (rgFrameBuffer) FrameWithCookie<TailCallFrame>(&ctx, pThread);
        TailCallFrame * tailCallFrame = &*CookieFrame;

        tailCallFrame->SetGCLayout((TADDR)pGCLayout);
        pNewFrame = TailCallFrame::AdjustContextForTailCallHelperStub(&ctx, cbArgArea, pThread);
        fCopyDown = true;

        // Eventually, we'll add pNewFrame to our frame chain, but don't do it yet. It's
        // pointing to the place on the stack where the TailCallFrame contents WILL be,
        // but aren't there yet. In order to keep the stack walkable by profilers, wait
        // until the contents are moved over properly (inside DoCopy), and then add
        // pNewFrame onto the frame chain.
    }

    // The stack should be properly aligned, modulo the pushed return
    // address (at least on x64)
    _ASSERTE((GetSP(&ctx) & STACK_ALIGN_MASK) == STACK_ADJUST_FOR_RETURN_ADDRESS);

    // Set the target pointer so we land there when we restore the context
    SetIP(&ctx, (PCODE)target);

    // Begin creating the new stack frame and copying arguments
    size_t cbTempStack = cbCopyFrame + cbArgArea + STACK_ADJUST_FOR_RETURN_ADDRESS;

    // If we're going to have to overwrite some of our incoming argument slots
    // then do a double-copy, first to temporary copy below us on the stack and
    // then back up to the real stack.
    void * pvTempStack;
    if (!fCopyDown && (((ULONG_PTR)args + cbArgArea) < GetSP(&ctx))) {

        //
        // After this our stack may no longer be walkable by the debugger!!!
        //

        pvTempStack = (void*)GetSP(&ctx);
    }
    else {
        fCopyDown = true;

        // Need to align properly for a return address (if it goes on the stack)
        //
        // AMD64 ONLY:
        //     _alloca produces 16-byte aligned buffers, but the return address,
        //     where our buffer 'starts' is off by 8, so make sure our buffer is
        //     off by 8.
        //
        pvTempStack = (BYTE*)_alloca(cbTempStack + STACK_ADJUST_FOR_RETURN_ADDRESS) + STACK_ADJUST_FOR_RETURN_ADDRESS;
    }

    _ASSERTE(((size_t)pvTempStack & STACK_ALIGN_MASK) == STACK_ADJUST_FOR_RETURN_ADDRESS);

    // Start creating the new stack (bottom up)
    BYTE * pbTempStackFill = (BYTE*)pvTempStack;
    // Return address
    if (STACK_ADJUST_FOR_RETURN_ADDRESS > 0) {
        *((PVOID*)pbTempStackFill) = (PVOID)JIT_TailCallHelperStub_ReturnAddress; // return address
        pbTempStackFill += STACK_ADJUST_FOR_RETURN_ADDRESS;
    }

    // arguments
    INVOKE_COPY_ARGS_HELPER(copyArgs, args, &ctx, (DWORD_PTR*)pbTempStackFill, cbArgArea);
    
    va_end(args);

    pbTempStackFill += cbArgArea;

    // frame (includes TailCallFrame)
    if (cbCopyFrame > 0) {
        _ASSERTE(cbCopyFrame == sizeof(rgFrameBuffer));
        memcpy(pbTempStackFill, rgFrameBuffer, cbCopyFrame);
        pbTempStackFill += cbCopyFrame;
    }

    // If this fires, check the math above, because we copied more than we should have
    _ASSERTE((size_t)((pbTempStackFill - (BYTE*)pvTempStack)) == cbTempStack);

    // If this fires, it means we messed up the math and we're about to overwrite
    // some of our locals which would be bad because we still need them to call
    // RtlRestoreContext and pop the contract...
    _ASSERTE(fCopyDown || ((DWORD_PTR)&ctx + sizeof(ctx) < (DWORD_PTR)GetSP(&ctx)));

    if (fCopyDown) {
        // We've created a dummy stack below our frame and now we overwrite
        // our own real stack.

        //
        // After this our stack may no longer be walkable by the debugger!!!
        //

        // This does the copy, adds pNewFrame to the frame chain, and calls RtlRestoreContext
        DoCopy(&ctx, pvTempStack, cbTempStack, pThread, pNewFrame);
    }

    RtlRestoreContext(&ctx, NULL);

#undef STACK_ADJUST_FOR_RETURN_ADDRESS
#undef STACK_ALIGN_MASK

#else // !FEATURE_PAL
    PORTABILITY_ASSERT("TODO: Implement JIT_TailCall for PAL");
#endif // !FEATURE_PAL

}

#endif // _TARGET_AMD64_ || _TARGET_ARM_

//========================================================================
//
//      JIT HELPERS LOGGING
//
//========================================================================

#if defined(_DEBUG) && (defined(_TARGET_AMD64_) || defined(_TARGET_X86_)) && !defined(FEATURE_PAL)
// *****************************************************************************
//  JitHelperLogging usage:
//      1) Ngen using:
//              COMPlus_HardPrejitEnabled=0 
//
//         This allows us to instrument even ngen'd image calls to JIT helpers. 
//         Remember to clear the key after ngen-ing and before actually running 
//         the app you want to log.
//
//      2) Then set:
//              COMPlus_JitHelperLogging=1
//              COMPlus_LogEnable=1
//              COMPlus_LogLevel=1
//              COMPlus_LogToFile=1
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
        SO_TOLERANT;
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
                // This will print a comma seperated list:
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

#ifdef _TARGET_X86_
        IMAGE_DOS_HEADER *pDOS = (IMAGE_DOS_HEADER *)g_pMSCorEE;
        _ASSERTE(pDOS->e_magic == VAL16(IMAGE_DOS_SIGNATURE) && pDOS->e_lfanew != 0);
        
        IMAGE_NT_HEADERS *pNT = (IMAGE_NT_HEADERS*)((LPBYTE)g_pMSCorEE + VAL32(pDOS->e_lfanew));
#ifdef _WIN64
        _ASSERTE(pNT->Signature == VAL32(IMAGE_NT_SIGNATURE) 
            && pNT->FileHeader.SizeOfOptionalHeader == VAL16(sizeof(IMAGE_OPTIONAL_HEADER64))
            && pNT->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR_MAGIC) );
#else
        _ASSERTE(pNT->Signature == VAL32(IMAGE_NT_SIGNATURE) 
            && pNT->FileHeader.SizeOfOptionalHeader == VAL16(sizeof(IMAGE_OPTIONAL_HEADER32))
            && pNT->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR_MAGIC) );
#endif
#endif // _TARGET_X86_

        if (g_pConfig->NgenHardBind() == EEConfig::NGEN_HARD_BIND_NONE)
        {
            _ASSERTE(g_pConfig->NgenHardBind() != EEConfig::NGEN_HARD_BIND_NONE && "You are "
                        "trying to log JIT helper method calls while you have NGEN HARD BINDING "
                        "set to 0. This probably means you're really trying to NGEN something for "
                        "logging purposes, NGEN breaks with JitHelperLogging turned on!!!! Please "
                        "set JitHelperLogging=0 while you NGEN, or unset HardPrejitEnabled while "
                        "running managed code.");
            return;
        }

        // Make the static hlpFuncTable read/write for purposes of writing the logging thunks
        DWORD dwOldProtect;
        if (!ClrVirtualProtect((LPVOID)hlpFuncTable, (sizeof(VMHELPDEF) * CORINFO_HELP_COUNT), PAGE_EXECUTE_READWRITE, &dwOldProtect))
        {   
            ThrowLastError();
        }
        
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
#ifdef _TARGET_AMD64_
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
#else // _TARGET_X86_
                    // How do I get this for x86?
                    hlpFuncCount->helperSize = 0;
#endif // _TARGET_AMD64_
                
                    pSl->EmitJITHelperLoggingThunk(GetEEFuncEntryPoint(hlpFunc->pfnHelper), (LPVOID)hlpFuncCount);
                    Stub* pStub = pSl->Link();
                    hlpFunc->pfnHelper = (void*)pStub->GetEntryPoint();
                }
                else
                {
                    _ASSERTE(((size_t)hlpFunc->pfnHelper - 1) >= 0 && 
                             ((size_t)hlpFunc->pfnHelper - 1) < COUNTOF(hlpDynamicFuncTable));
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

#ifdef _TARGET_AMD64_
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
#else // _TARGET_X86_
                    // Is the address in mscoree.dll at all? (All helpers are in
                    // mscoree.dll)
                    if (dynamicHlpFunc->pfnHelper >= (LPBYTE*)g_pMSCorEE && dynamicHlpFunc->pfnHelper < (LPBYTE*)g_pMSCorEE + VAL32(pNT->OptionalHeader.SizeOfImage))
                    {
                        // See note above. How do I get the size on x86 for a static method?
                        hlpFuncCount->helperSize = 0;
                    }
                    else
                    {
                        Stub::RecoverStubAndSize((TADDR)dynamicHlpFunc->pfnHelper, (DWORD*)&hlpFuncCount->helperSize);
                        hlpFuncCount->helperSize -= sizeof(Stub);
                    }

#endif // _TARGET_AMD64_

                    pSl->EmitJITHelperLoggingThunk(GetEEFuncEntryPoint(dynamicHlpFunc->pfnHelper), (LPVOID)hlpFuncCount);
                    Stub* pStub = pSl->Link();
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
#endif // _DEBUG && (_TARGET_AMD64_ || _TARGET_X86_)
