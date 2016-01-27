// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: FloatNative.cpp
// 

#include <common.h>

#include "floatnative.h"
#include "floatclass.h"

#define IS_DBL_INFINITY(x) ((*((UINT64 *)((void *)&x)) & UI64(0x7FFFFFFFFFFFFFFF)) == UI64(0x7FF0000000000000))
#define IS_DBL_ONE(x)      ((*((UINT64 *)((void *)&x))) == UI64(0x3FF0000000000000))
#define IS_DBL_NEGATIVEONE(x)      ((*((UINT64 *)((void *)&x))) == UI64(0xBFF0000000000000))


// Default compilation mode is /fp:precise, which disables fp intrinsics. This has caused
// regression in floating point code. I've grouped all the helpers that are really simple
// (where /fp:fast semantics are really unlikely to cause any regression) and grouped them 
// here in order to get back to Everett performance numbers

////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
///
///                         Beggining of /fp:fast scope    
///
////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////

#ifdef _MSC_VER
#pragma float_control(precise, off)
#endif

/*====================================Floor=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Floor, double d) 
    FCALL_CONTRACT;

    return (double) floor(d);
FCIMPLEND


/*====================================Ceil=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Ceil, double d) 
    FCALL_CONTRACT;

    return (double) ceil(d);
FCIMPLEND

/*=====================================Sqrt=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Sqrt, double d) 
    FCALL_CONTRACT;

    return (double) sqrt(d);
FCIMPLEND

/*=====================================Acos=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Acos, double d) 
    FCALL_CONTRACT;

    return (double) acos(d);
FCIMPLEND


/*=====================================Asin=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Asin, double d) 
    FCALL_CONTRACT;

    return (double) asin(d);
FCIMPLEND


/*=====================================AbsFlt=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMDouble::AbsFlt, float f) 
    FCALL_CONTRACT;

    FCUnique(0x14);

    return fabsf(f);
FCIMPLEND

/*=====================================AbsDbl=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::AbsDbl, double d) 
    FCALL_CONTRACT;

    return fabs(d);
FCIMPLEND

/*=====================================Atan=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Atan, double d) 
    FCALL_CONTRACT;

    return (double) atan(d);
FCIMPLEND

/*=====================================Atan2=====================================
**
==============================================================================*/
FCIMPL2_VV(double, COMDouble::Atan2, double x, double y) 
    FCALL_CONTRACT;

        // the intrinsic for Atan2 does not produce Nan for Atan2(+-inf,+-inf)
    if (IS_DBL_INFINITY(x) && IS_DBL_INFINITY(y)) {
        return(x / y);      // create a NaN
    }
    return (double) atan2(x, y);
FCIMPLEND

// COMDouble::Sin/Cos/Tan are all implemented in JitHelpers_Fast.asm as x87 floating
// point for code AMD64 (on Windows) because the CRT helpers is too slow (apparently they don't
// have a /fp:fast v ersion).
#if !defined(_TARGET_AMD64_) || defined(FEATURE_PAL)

/*=====================================Sin=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Sin, double d) 
    FCALL_CONTRACT;

    return (double) sin(d);
FCIMPLEND

/*=====================================Cos=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Cos, double d) 
    FCALL_CONTRACT;

    return (double) cos(d);
FCIMPLEND

/*=====================================Tan=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Tan, double d) 
    FCALL_CONTRACT;

    return (double) tan(d);
FCIMPLEND

#endif // !defined(_TARGET_AMD64_) || defined(FEATURE_PAL)

/*=====================================Sinh====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Sinh, double d) 
    FCALL_CONTRACT;

    return (double) sinh(d);
FCIMPLEND

/*=====================================Cosh====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Cosh, double d) 
    FCALL_CONTRACT;

    return (double) cosh(d);
FCIMPLEND

/*=====================================Tanh====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Tanh, double d) 
    FCALL_CONTRACT;

    return (double) tanh(d);
FCIMPLEND

FCIMPL1(double, COMDouble::ModFDouble, double* pdblValue)
    FCALL_CONTRACT;

    double      dblFrac;
    dblFrac = modf(*pdblValue, pdblValue);
    return dblFrac;
FCIMPLEND

#ifdef _MSC_VER
#pragma float_control(precise, on )
#endif

////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
///
///                         End of /fp:fast scope    
///
////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
//
// Log, Log10 and Exp are slower with /fp:fast on SSE2 enabled HW (see #500373)
// So we'll leave them as fp precise for the moment

/*=====================================Log======================================
**This is the natural log
==============================================================================*/
FCIMPL1_V(double, COMDouble::Log, double d) 
    FCALL_CONTRACT;

    return (double) log(d);
FCIMPLEND


/*====================================Log10=====================================
**This is log-10
==============================================================================*/
FCIMPL1_V(double, COMDouble::Log10, double d) 
    FCALL_CONTRACT;

    return (double) log10(d);
FCIMPLEND


/*=====================================Exp======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Exp, double x) 
    FCALL_CONTRACT;

        // The C intrinsic below does not handle +- infinity properly
        // so we handle these specially here
    if (IS_DBL_INFINITY(x)) {
        if (x < 0)      
            return(+0.0);
        return(x);      // Must be + infinity
    }
    return((double) exp(x));
FCIMPLEND

#if defined(_TARGET_X86_)
/*=====================================Pow======================================
**This is the power function.  Simple powers are done inline, and special
  cases are sent to the CRT via the helper. 
==============================================================================*/
FCIMPL2_VV(double, COMDouble::PowHelperSimple, double x, double y)
{
    FCALL_CONTRACT;

    return (double) pow(x,y);
}
FCIMPLEND

FCIMPL2_VV(double, COMDouble::PowHelper, double x, double y) 
{
    FCALL_CONTRACT;

    double r1;

    // TODO: we can get rid following code if VC fixes pow function someday.
    if(_isnan(y)) {
        return y; // IEEE 754-2008: NaN payload must be preserved
    }
    if(_isnan(x)) {
        return x; // IEEE 754-2008: NaN payload must be preserved
    }
    if(IS_DBL_INFINITY(y)) {
        if(IS_DBL_ONE(x)) {
            return x;        
        }

        if(IS_DBL_NEGATIVEONE(x)) {
            *((INT64 *)(&r1)) = CLR_NAN_64;
            return r1;
        }    
    }
    
    return (double) pow(x, y);
}
FCIMPLEND

#if defined (_DEBUG)
__declspec(naked) static double F_CALL_CONV PowRetail(double x, double y)
#else
__declspec(naked) double F_CALL_CONV COMDouble::Pow(double x, double y)
#endif
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    // Arguments:
    // exponent: esp+4
    // base:     esp+12
    
    _asm
    {
        mov     ecx, [esp+8]           ; high dword of exponent
        mov     edx, [esp+16]          ; high dword of base
        
        and     ecx,  7ff00000H        ; check for special exponent
        cmp     ecx,  7ff00000H
        je      callHelper

        and     edx,  7ff00000H        ; check for special base
        cmp     edx,  7ff00000H
        je      callHelper

        test    edx,  7ff00000H        ; see if the base has a zero exponent
        jz      test_if_we_have_zero_base

base_is_not_zero:

        mov     cl,  [esp+19]          ; Handle negative base in the helper
        and     cl,  80H
        jnz     callHelper

	jmp	COMDouble::PowHelperSimple	;

test_if_we_have_zero_base:
            
        mov     eax, [esp+16]
        and     eax, 000fffffH
        or      eax, [esp+12]
        jnz     base_is_not_zero
        ; fall through to the helper

callHelper:

        jmp     COMDouble::PowHelper   ; The helper will return control
                                       ; directly to our caller.
    }
}

#ifdef _DEBUG

#define EPSILON 0.0000000001

void assertDoublesWithinRange(double r1, double r2)
{
    WRAPPER_NO_CONTRACT;

    if (_finite(r1) && _finite(r2))
    {
        // Both numbers are finite--we need to check that they are close to
        // each other.  If they are large (> 1), the error could also be large,
        // which is acceptable, so we compare the error against EPSILON*norm.

        double norm = max(fabs(r1), fabs(r2));
        double error = fabs(r1-r2);
        
        assert((error < (EPSILON * norm)) || (error < EPSILON));
    }
    else if (!_isnan(r1) && !_isnan(r2))
    {
        // At least one of r1 and r2 is infinite, so when multiplied by
        // (1 + EPSILON) they should be the same infinity.

        assert((r1 * (1 + EPSILON)) == (r2 * (1 + EPSILON)));
    }
    else
    {
        // Otherwise at least one of r1 or r2 is a Nan.  Is that case, they better be in
        // the same class.

        assert(_fpclass(r1) == _fpclass(r2));
    }
}

FCIMPL2_VV(double, COMDouble::Pow, double x, double y) 
{
    FCALL_CONTRACT;

    double r1, r2;

    if(_isnan(y)) {
        return y; // IEEE 754-2008: NaN payload must be preserved
    }
    if(_isnan(x)) {
        return x; // IEEE 754-2008: NaN payload must be preserved
    }

    if(IS_DBL_INFINITY(y)) {
        if(IS_DBL_ONE(x)) {
            return x;        
        }

        if(IS_DBL_NEGATIVEONE(x)) {
            *((INT64 *)(&r1)) = CLR_NAN_64;
            return r1;
        }    
    }  

    // Note that PowRetail expects the argument order to be reversed
    
    r1 = (double) PowRetail(y, x);
    
    r2 = (double) pow(x, y);

    // Can't do a floating point compare in case r1 and r2 aren't 
    // valid fp numbers.

    assertDoublesWithinRange(r1, r2);

    return (double) r1; 
}
FCIMPLEND

#endif  // _DEBUG

#else   // !defined(_TARGET_X86_)
FCIMPL2_VV(double, COMDouble::Pow, double x, double y)
{
    FCALL_CONTRACT;

    double r1;

    if(_isnan(y)) {
        return y; // IEEE 754-2008: NaN payload must be preserved
    }
    if(_isnan(x)) {
        return x; // IEEE 754-2008: NaN payload must be preserved
    }

    if(IS_DBL_INFINITY(y)) {
        if(IS_DBL_ONE(x)) {
            return x;        
        }

        if(IS_DBL_NEGATIVEONE(x)) {
            *((INT64 *)(&r1)) = CLR_NAN_64;
            return r1;
        }    
    }
    
    return (double) pow(x, y);
}
FCIMPLEND

#endif  // defined(_TARGET_X86_)


/*====================================Round=====================================
**
==============================================================================*/
#if defined(_TARGET_X86_)
__declspec(naked)
double __fastcall COMDouble::Round(double d)
{
    LIMITED_METHOD_CONTRACT;

    __asm {
        fld QWORD PTR [ESP+4]
        frndint
        ret 8
    }
}

#else // !defined(_TARGET_X86_)
FCIMPL1_V(double, COMDouble::Round, double d) 
    FCALL_CONTRACT;

    double tempVal;
    double flrTempVal;
    // If the number has no fractional part do nothing
    // This shortcut is necessary to workaround precision loss in borderline cases on some platforms
    if ( d == (double)(__int64)d )
        return d;
    tempVal = (d+0.5);
    //We had a number that was equally close to 2 integers. 
    //We need to return the even one.
    flrTempVal = floor(tempVal);
    if (flrTempVal==tempVal) {
        if (0 != fmod(tempVal, 2.0)) {
            flrTempVal -= 1.0;
        }
    }
    flrTempVal = _copysign(flrTempVal, d);
    return flrTempVal;
FCIMPLEND
#endif // defined(_TARGET_X86_)


