// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: decimal.cpp
//

//

#include "common.h"
#include "object.h"
#include "excep.h"
#include "frames.h"
#include "vars.hpp"
#include "decimal.h"
#include "string.h"

LONG g_OLEAUT32_Loaded = 0;

unsigned int DecDivMod1E9(DECIMAL* value);
void DecMul10(DECIMAL* value);
void DecAddInt32(DECIMAL* value, unsigned int i);

#define COPYDEC(dest, src) {DECIMAL_SIGNSCALE(dest) = DECIMAL_SIGNSCALE(src); DECIMAL_HI32(dest) = DECIMAL_HI32(src); DECIMAL_LO64_SET(dest, DECIMAL_LO64_GET(src));}

FCIMPL2_IV(void, COMDecimal::InitSingle, DECIMAL *_this, float value)
{
    FCALL_CONTRACT;

    ENSURE_OLEAUT32_LOADED();

    _ASSERTE(_this != NULL);
    HRESULT hr = VarDecFromR4(value, _this);
    if (FAILED(hr))
        FCThrowResVoid(kOverflowException, W("Overflow_Decimal"));
    _this->wReserved = 0;
}
FCIMPLEND

FCIMPL2_IV(void, COMDecimal::InitDouble, DECIMAL *_this, double value)
{
    FCALL_CONTRACT;

    ENSURE_OLEAUT32_LOADED();

    _ASSERTE(_this != NULL);
    HRESULT hr = VarDecFromR8(value, _this);
    if (FAILED(hr))
        FCThrowResVoid(kOverflowException, W("Overflow_Decimal"));
    _this->wReserved = 0;
}
FCIMPLEND


#ifdef _MSC_VER
// C4702: unreachable code on IA64 retail
#pragma warning(push)
#pragma warning(disable:4702)
#endif
FCIMPL2(INT32, COMDecimal::DoCompare, DECIMAL * d1, DECIMAL * d2)
{
    FCALL_CONTRACT;

    ENSURE_OLEAUT32_LOADED();

    HRESULT hr = VarDecCmp(d1, d2);
    if (FAILED(hr) || (int)hr == VARCMP_NULL) {
        _ASSERTE(!"VarDecCmp failed in Decimal::Compare");
        FCThrowRes(kOverflowException, W("Overflow_Decimal"));
    }
    
    INT32 retVal = ((int)hr) - 1;
    FC_GC_POLL_RET ();
    return retVal;
}
FCIMPLEND
#ifdef _MSC_VER
#pragma warning(pop)
#endif

FCIMPL1(void, COMDecimal::DoFloor, DECIMAL * d)
{
    FCALL_CONTRACT;

    ENSURE_OLEAUT32_LOADED();

    DECIMAL decRes;
    HRESULT hr;
    hr = VarDecInt(d, &decRes);

    // VarDecInt can't overflow, as of source for OleAut32 build 4265.
    // It only returns NOERROR
    _ASSERTE(hr==NOERROR);

    // copy decRes into d
    COPYDEC(*d, decRes)
    d->wReserved = 0;
    FC_GC_POLL();
}
FCIMPLEND

FCIMPL1(INT32, COMDecimal::GetHashCode, DECIMAL *d)
{
    FCALL_CONTRACT;

    ENSURE_OLEAUT32_LOADED();

    _ASSERTE(d != NULL);
    double dbl;
    VarR8FromDec(d, &dbl);
    if (dbl == 0.0) {
        // Ensure 0 and -0 have the same hash code
        return 0;
    }
    // conversion to double is lossy and produces rounding errors so we mask off the lowest 4 bits
    // 
    // For example these two numerically equal decimals with different internal representations produce
    // slightly different results when converted to double:
    //
    // decimal a = new decimal(new int[] { 0x76969696, 0x2fdd49fa, 0x409783ff, 0x00160000 });
    //                     => (decimal)1999021.176470588235294117647000000000 => (double)1999021.176470588
    // decimal b = new decimal(new int[] { 0x3f0f0f0f, 0x1e62edcc, 0x06758d33, 0x00150000 }); 
    //                     => (decimal)1999021.176470588235294117647000000000 => (double)1999021.1764705882
    //
    return ((((int *)&dbl)[0]) & 0xFFFFFFF0) ^ ((int *)&dbl)[1];
}
FCIMPLEND

FCIMPL3(void, COMDecimal::DoMultiply, DECIMAL * d1, DECIMAL * d2, CLR_BOOL * overflowed)
{
    FCALL_CONTRACT;

    ENSURE_OLEAUT32_LOADED();

    DECIMAL decRes;

    // GC is only triggered for throwing, no need to protect result 
    HRESULT hr = VarDecMul(d1, d2, &decRes);
    if (FAILED(hr)) {
        *overflowed = true;
        FC_GC_POLL();
        return;
    }

    // copy decRes into d1
    COPYDEC(*d1, decRes)
    d1->wReserved = 0;
    *overflowed = false;
    FC_GC_POLL();
} 
FCIMPLEND


FCIMPL2(void, COMDecimal::DoMultiplyThrow, DECIMAL * d1, DECIMAL * d2)
{
    FCALL_CONTRACT;

    ENSURE_OLEAUT32_LOADED();

    DECIMAL decRes;

    // GC is only triggered for throwing, no need to protect result 
    HRESULT hr = VarDecMul(d1, d2, &decRes);
    if (FAILED(hr)) {
        FCThrowResVoid(kOverflowException, W("Overflow_Decimal"));
    }

    // copy decRes into d1
    COPYDEC(*d1, decRes)
    d1->wReserved = 0;
    FC_GC_POLL();
} 
FCIMPLEND

FCIMPL2(void, COMDecimal::DoRound, DECIMAL * d, INT32 decimals)
{
    FCALL_CONTRACT;

    ENSURE_OLEAUT32_LOADED();

    DECIMAL decRes;
    
    // GC is only triggered for throwing, no need to protect result 
    if (decimals < 0 || decimals > 28)
        FCThrowArgumentOutOfRangeVoid(W("decimals"), W("ArgumentOutOfRange_DecimalRound"));
    HRESULT hr = VarDecRound(d, decimals, &decRes);
    if (FAILED(hr))
        FCThrowResVoid(kOverflowException, W("Overflow_Decimal"));

    // copy decRes into d
    COPYDEC(*d, decRes)
    d->wReserved = 0;
    FC_GC_POLL();
}
FCIMPLEND

FCIMPL2_IV(void, COMDecimal::DoToCurrency, CY * result, DECIMAL d)
{
    FCALL_CONTRACT;

    ENSURE_OLEAUT32_LOADED();

    // GC is only triggered for throwing, no need to protect result
    HRESULT hr = VarCyFromDec(&d, result);
    if (FAILED(hr)) {
        _ASSERTE(hr != E_INVALIDARG);
        FCThrowResVoid(kOverflowException, W("Overflow_Currency"));
    }
}
FCIMPLEND

FCIMPL1(double, COMDecimal::ToDouble, FC_DECIMAL d)
{
    FCALL_CONTRACT;

    ENSURE_OLEAUT32_LOADED();

    double result = 0.0;
    // Note: this can fail if the input is an invalid decimal, but for compatibility we should return 0
    VarR8FromDec(&d, &result);
    return result;
}
FCIMPLEND

FCIMPL1(INT32, COMDecimal::ToInt32, FC_DECIMAL d)
{
    FCALL_CONTRACT;

    ENSURE_OLEAUT32_LOADED();

    DECIMAL result;
    HRESULT hr = VarDecRound(&d, 0, &result);
    if (FAILED(hr))
        FCThrowRes(kOverflowException, W("Overflow_Decimal"));

    result.wReserved = 0;
    
    if( DECIMAL_SCALE(result) != 0) {
        d = result;
        VarDecFix(&d, &result);
    }

    if (DECIMAL_HI32(result) == 0 && DECIMAL_MID32(result) == 0) {
        INT32 i = DECIMAL_LO32(result);
        if ((INT16)DECIMAL_SIGNSCALE(result) >= 0) {
            if (i >= 0) return i;
        }
        else {
            i = -i;
            if (i <= 0) return i;
        }
    }
    FCThrowRes(kOverflowException, W("Overflow_Int32"));    
}
FCIMPLEND

FCIMPL1(float, COMDecimal::ToSingle, FC_DECIMAL d)
{
    FCALL_CONTRACT;

    ENSURE_OLEAUT32_LOADED();

    float result = 0.0f;
    // Note: this can fail if the input is an invalid decimal, but for compatibility we should return 0
    VarR4FromDec(&d, &result);
    return result;
}
FCIMPLEND

FCIMPL1(void, COMDecimal::DoTruncate, DECIMAL * d)
{
    FCALL_CONTRACT;

    ENSURE_OLEAUT32_LOADED();

    DECIMAL decRes;

    VarDecFix(d, &decRes);

    // copy decRes into d
    COPYDEC(*d, decRes)
    d->wReserved = 0;
    FC_GC_POLL();
}
FCIMPLEND


void COMDecimal::DecimalToNumber(DECIMAL* value, NUMBER* number)
{
    WRAPPER_NO_CONTRACT
    _ASSERTE(number != NULL);
    _ASSERTE(value != NULL);

    wchar_t buffer[DECIMAL_PRECISION+1];
    DECIMAL d = *value;
    number->precision = DECIMAL_PRECISION;
    number->sign = DECIMAL_SIGN(d)? 1: 0;
    wchar_t* p = buffer + DECIMAL_PRECISION;
    while (DECIMAL_MID32(d) | DECIMAL_HI32(d)) {
        p = COMNumber::Int32ToDecChars(p, DecDivMod1E9(&d), 9);
        _ASSERTE(p != NULL);
    }
    p = COMNumber::Int32ToDecChars(p, DECIMAL_LO32(d), 0);
    _ASSERTE(p != NULL);
    int i = (int) (buffer + DECIMAL_PRECISION - p);
    number->scale = i - DECIMAL_SCALE(d);
    wchar_t* dst = number->digits;
    _ASSERTE(dst != NULL);
    while (--i >= 0) *dst++ = *p++;
    *dst = 0;
    
}

int COMDecimal::NumberToDecimal(NUMBER* number, DECIMAL* value)
{
    WRAPPER_NO_CONTRACT
    _ASSERTE(number != NULL);
    _ASSERTE(value != NULL);

    DECIMAL d;
    d.wReserved = 0;
    DECIMAL_SIGNSCALE(d) = 0;
    DECIMAL_HI32(d) = 0;
    DECIMAL_LO32(d) = 0;
    DECIMAL_MID32(d) = 0;
    wchar_t* p = number->digits;
    _ASSERT(p != NULL);
    int e = number->scale;
    if (!*p) {
        // To avoid risking an app-compat issue with pre 4.5 (where some app was illegally using Reflection to examine the internal scale bits), we'll only force
        // the scale to 0 if the scale was previously positive
        if (e > 0) {
            e = 0;
        }
    } else {
        if (e > DECIMAL_PRECISION) return 0;
        while ((e > 0 || (*p && e > -28)) &&
                (DECIMAL_HI32(d) < 0x19999999 || (DECIMAL_HI32(d) == 0x19999999 &&
                    (DECIMAL_MID32(d) < 0x99999999 || (DECIMAL_MID32(d) == 0x99999999 &&
                        (DECIMAL_LO32(d) < 0x99999999 || (DECIMAL_LO32(d) == 0x99999999 && *p <= '5'))))))) {
            DecMul10(&d);
            if (*p) DecAddInt32(&d, *p++ - '0');
            e--;
        }
        if (*p++ >= '5') {
            bool round = true;
            if (*(p-1) == '5' && *(p-2) % 2 == 0) { // Check if previous digit is even, only if the when we are unsure whether hows to do Banker's rounding
                                                    // For digits > 5 we will be roundinp up anyway.
                int count = 20; // Look at the next 20 digits to check to round
                while (*p == '0' && count != 0) {
                    p++;
                    count--;
                }
                if (*p == '\0' || count == 0) 
                    round = false;// Do nothing
            }

            if (round) {
                DecAddInt32(&d, 1);
                if ((DECIMAL_HI32(d) | DECIMAL_MID32(d) | DECIMAL_LO32(d)) == 0) {
                    DECIMAL_HI32(d) = 0x19999999;
                    DECIMAL_MID32(d) = 0x99999999;
                    DECIMAL_LO32(d) = 0x9999999A;
                    e++;
                }
            }
        }
    }
    if (e > 0) return 0;
    if (e <= -DECIMAL_PRECISION) 
    {
        // Parsing a large scale zero can give you more precision than fits in the decimal.
        // This should only happen for actual zeros or very small numbers that round to zero.
        DECIMAL_SIGNSCALE(d) = 0;
        DECIMAL_HI32(d) = 0;
        DECIMAL_LO32(d) = 0;
        DECIMAL_MID32(d) = 0;
        DECIMAL_SCALE(d) = (DECIMAL_PRECISION - 1);
    }
    else 
    {
        DECIMAL_SCALE(d) = static_cast<BYTE>(-e);
    }
    DECIMAL_SIGN(d) = number->sign? DECIMAL_NEG: 0;
    *value = d;
    return 1;
}

#if defined(_TARGET_X86_)
        
#pragma warning(disable:4035)

unsigned int DecDivMod1E9(DECIMAL* value)
{
    LIMITED_METHOD_CONTRACT

    _asm {
        mov     ebx,value
        mov     ecx,1000000000
        xor     edx,edx
        mov     eax,[ebx+4]
        div     ecx
        mov     [ebx+4],eax
        mov     eax,[ebx+12]
        div     ecx
        mov     [ebx+12],eax
        mov     eax,[ebx+8]
        div     ecx
        mov     [ebx+8],eax
        mov     eax,edx
    }
}

void DecMul10(DECIMAL* value)
{
    LIMITED_METHOD_CONTRACT

    _asm {
        mov     ebx,value
        mov     eax,[ebx+8]
        mov     edx,[ebx+12]
        mov     ecx,[ebx+4]
        shl     eax,1
        rcl     edx,1
        rcl     ecx,1
        shl     eax,1
        rcl     edx,1
        rcl     ecx,1
        add     eax,[ebx+8]
        adc     edx,[ebx+12]
        adc     ecx,[ebx+4]
        shl     eax,1
        rcl     edx,1
        rcl     ecx,1
        mov     [ebx+8],eax
        mov     [ebx+12],edx
        mov     [ebx+4],ecx
    }
}

void DecAddInt32(DECIMAL* value, unsigned int i)
{
    LIMITED_METHOD_CONTRACT

    _asm {
        mov     edx,value
        mov     eax,i
        add     dword ptr [edx+8],eax
        adc     dword ptr [edx+12],0
        adc     dword ptr [edx+4],0
    }
}

#pragma warning(default:4035)
        
#else // !(defined(_TARGET_X86_)

unsigned int D32DivMod1E9(unsigned int hi32, ULONG* lo32)
{
    LIMITED_METHOD_CONTRACT
    _ASSERTE(lo32 != NULL);

    unsigned __int64 n = (unsigned __int64)hi32 << 32 | *lo32;
    *lo32 = (unsigned int)(n / 1000000000);
    return (unsigned int)(n % 1000000000);
}

unsigned int DecDivMod1E9(DECIMAL* value)
{
    WRAPPER_NO_CONTRACT
    _ASSERTE(value != NULL);

    return D32DivMod1E9(D32DivMod1E9(D32DivMod1E9(0,
        &DECIMAL_HI32(*value)), &DECIMAL_MID32(*value)), &DECIMAL_LO32(*value));
}

void DecShiftLeft(DECIMAL* value)
{
    LIMITED_METHOD_CONTRACT
    _ASSERTE(value != NULL);

    unsigned int c0 = DECIMAL_LO32(*value) & 0x80000000? 1: 0;
    unsigned int c1 = DECIMAL_MID32(*value) & 0x80000000? 1: 0;
    DECIMAL_LO32(*value) <<= 1;
    DECIMAL_MID32(*value) = DECIMAL_MID32(*value) << 1 | c0;
    DECIMAL_HI32(*value) = DECIMAL_HI32(*value) << 1 | c1;
}

int D32AddCarry(ULONG* value, unsigned int i)
{
    LIMITED_METHOD_CONTRACT
    _ASSERTE(value != NULL);

    unsigned int v = *value;
    unsigned int sum = v + i;
    *value = sum;
    return sum < v || sum < i? 1: 0;
}

void DecAdd(DECIMAL* value, DECIMAL* d)
{
    WRAPPER_NO_CONTRACT
    _ASSERTE(value != NULL && d != NULL);

    if (D32AddCarry(&DECIMAL_LO32(*value), DECIMAL_LO32(*d))) {
        if (D32AddCarry(&DECIMAL_MID32(*value), 1)) {
            D32AddCarry(&DECIMAL_HI32(*value), 1);
        }
    }
    if (D32AddCarry(&DECIMAL_MID32(*value), DECIMAL_MID32(*d))) {
        D32AddCarry(&DECIMAL_HI32(*value), 1);
    }
    D32AddCarry(&DECIMAL_HI32(*value), DECIMAL_HI32(*d));
}

void DecMul10(DECIMAL* value)
{
    WRAPPER_NO_CONTRACT
    _ASSERTE(value != NULL);

    DECIMAL d = *value;
    DecShiftLeft(value);
    DecShiftLeft(value);
    DecAdd(value, &d);
    DecShiftLeft(value);
}

void DecAddInt32(DECIMAL* value, unsigned int i)
{
    WRAPPER_NO_CONTRACT
    _ASSERTE(value != NULL);

    if (D32AddCarry(&DECIMAL_LO32(*value), i)) {
        if (D32AddCarry(&DECIMAL_MID32(*value), 1)) {
            D32AddCarry(&DECIMAL_HI32(*value), 1);
        }
    }
}

#endif

/***
* 
*  Decimal Code ported from OleAut32
* 
***********************************************************************/

// This OleAut code is only used on 64-bit and rotor platforms. It is desiriable to continue
// to call the OleAut routines in X86 because of the performance of the hand-tuned assembly 
// code and because there are currently no inconsistencies in behavior accross platforms.

#ifndef UInt32x32To64
#define UInt32x32To64(a, b) ((DWORDLONG)((DWORD)(a)) * (DWORDLONG)((DWORD)(b)))
#endif

typedef union {
    DWORDLONG int64;  
    struct {         
#if BIGENDIAN
      ULONG Hi;
      ULONG Lo;
#else            
      ULONG Lo;
      ULONG Hi;
#endif           
    } u;
} SPLIT64;

#define OVFL_MAX_1_HI   429496729
#define DEC_SCALE_MAX   28
#define POWER10_MAX 9

#define OVFL_MAX_9_HI   4u
#define OVFL_MAX_9_MID  1266874889u
#define OVFL_MAX_9_LO   3047500985u

#define OVFL_MAX_5_HI   42949


const ULONG rgulPower10[POWER10_MAX+1] = {1, 10, 100, 1000, 10000, 100000, 1000000,
                    10000000, 100000000, 1000000000};

struct DECOVFL
{
    ULONG Hi;
    ULONG Mid;
    ULONG Lo;
};

const DECOVFL PowerOvfl[] = {
// This is a table of the largest values that can be in the upper two
// ULONGs of a 96-bit number that will not overflow when multiplied
// by a given power.  For the upper word, this is a table of 
// 2^32 / 10^n for 1 <= n <= 9.  For the lower word, this is the
// remaining fraction part * 2^32.  2^32 = 4294967296.
// 
    { 429496729u, 2576980377u, 2576980377u }, // 10^1 remainder 0.6
    { 42949672u,  4123168604u, 687194767u  }, // 10^2 remainder 0.16
    { 4294967u,   1271310319u, 2645699854u }, // 10^3 remainder 0.616
    { 429496u,    3133608139u, 694066715u  }, // 10^4 remainder 0.1616
    { 42949u,     2890341191u, 2216890319u }, // 10^5 remainder 0.51616
    { 4294u,      4154504685u, 2369172679u }, // 10^6 remainder 0.551616
    { 429u,       2133437386u, 4102387834u }, // 10^7 remainder 0.9551616
    { 42u,        4078814305u, 410238783u  }, // 10^8 remainder 0.09991616
    { 4u,         1266874889u, 3047500985u }, // 10^9 remainder 0.709551616
};


/***
* IncreaseScale
*
* Entry:
*   rgulNum - Pointer to 96-bit number as array of ULONGs, least-sig first
*   ulPwr   - Scale factor to multiply by
*
* Purpose:
*   Multiply the two numbers.  The low 96 bits of the result overwrite
*   the input.  The last 32 bits of the product are the return value.
*
* Exit:
*   Returns highest 32 bits of product.
*
* Exceptions:
*   None.
*
***********************************************************************/

ULONG IncreaseScale(ULONG *rgulNum, ULONG ulPwr)
{
    LIMITED_METHOD_CONTRACT;
    
    SPLIT64   sdlTmp;

    sdlTmp.int64 = UInt32x32To64(rgulNum[0], ulPwr);
    rgulNum[0] = sdlTmp.u.Lo;
    sdlTmp.int64 = UInt32x32To64(rgulNum[1], ulPwr) + sdlTmp.u.Hi;
    rgulNum[1] = sdlTmp.u.Lo;
    sdlTmp.int64 = UInt32x32To64(rgulNum[2], ulPwr) + sdlTmp.u.Hi;
    rgulNum[2] = sdlTmp.u.Lo;
    return sdlTmp.u.Hi;
}


/***
* SearchScale
*
* Entry:
*   ulResHi - Top ULONG of quotient
*   ulResMid - Middle ULONG of quotient
*   ulResLo - Bottom ULONG of quotient
*   iScale  - Scale factor of quotient, range -DEC_SCALE_MAX to DEC_SCALE_MAX
*
* Purpose:
*   Determine the max power of 10, <= 9, that the quotient can be scaled
*   up by and still fit in 96 bits.
*
* Exit:
*   Returns power of 10 to scale by, -1 if overflow error.
*
***********************************************************************/

int SearchScale(ULONG ulResHi, ULONG ulResMid, ULONG ulResLo, int iScale)
{
    WRAPPER_NO_CONTRACT;

    int   iCurScale;

    // Quick check to stop us from trying to scale any more.
    //
    if (ulResHi > OVFL_MAX_1_HI || iScale >= DEC_SCALE_MAX) {
      iCurScale = 0;
      goto HaveScale;
    }

    if (iScale > DEC_SCALE_MAX - 9) {
      // We can't scale by 10^9 without exceeding the max scale factor.
      // See if we can scale to the max.  If not, we'll fall into
      // standard search for scale factor.
      //
      iCurScale = DEC_SCALE_MAX - iScale;
      if (ulResHi < PowerOvfl[iCurScale - 1].Hi)
    goto HaveScale;

      if (ulResHi == PowerOvfl[iCurScale - 1].Hi) {
  UpperEq:
        if (ulResMid > PowerOvfl[iCurScale - 1].Mid ||
              (ulResMid == PowerOvfl[iCurScale - 1].Mid && ulResLo > PowerOvfl[iCurScale - 1].Lo)) {
          iCurScale--;
        }
      goto HaveScale;
      }
    }
    else if (ulResHi < OVFL_MAX_9_HI || (ulResHi == OVFL_MAX_9_HI && 
      ulResMid < OVFL_MAX_9_MID) || (ulResHi == OVFL_MAX_9_HI && ulResMid == OVFL_MAX_9_MID && ulResLo <= OVFL_MAX_9_LO))
      return 9;

    // Search for a power to scale by < 9.  Do a binary search
    // on PowerOvfl[].
    //
    iCurScale = 5;
    if (ulResHi < OVFL_MAX_5_HI)
      iCurScale = 7;
    else if (ulResHi > OVFL_MAX_5_HI)
      iCurScale = 3;
    else
      goto UpperEq;

    // iCurScale is 3 or 7.
    //
    if (ulResHi < PowerOvfl[iCurScale - 1].Hi)
      iCurScale++;
    else if (ulResHi > PowerOvfl[iCurScale - 1].Hi)
      iCurScale--;
    else
      goto UpperEq;

    // iCurScale is 2, 4, 6, or 8.
    //
    // In all cases, we already found we could not use the power one larger.
    // So if we can use this power, it is the biggest, and we're done.  If
    // we can't use this power, the one below it is correct for all cases 
    // unless it's 10^1 -- we might have to go to 10^0 (no scaling).
    // 
    if (ulResHi > PowerOvfl[iCurScale - 1].Hi)
      iCurScale--;

    if (ulResHi == PowerOvfl[iCurScale - 1].Hi)
      goto UpperEq;

HaveScale:
    // iCurScale = largest power of 10 we can scale by without overflow, 
    // iCurScale < 9.  See if this is enough to make scale factor 
    // positive if it isn't already.
    // 
    if (iCurScale + iScale < 0)
      iCurScale = -1;

    return iCurScale;
}

//***********************************************************************
//
// Arithmetic Inlines
//

#define Div64by32(num, den) ((ULONG)((DWORDLONG)(num) / (ULONG)(den)))
#define Mod64by32(num, den) ((ULONG)((DWORDLONG)(num) % (ULONG)(den)))

inline DWORDLONG DivMod64by32(DWORDLONG num, ULONG den)
{
    WRAPPER_NO_CONTRACT;

    SPLIT64  sdl;

    sdl.u.Lo = Div64by32(num, den);
    sdl.u.Hi = Mod64by32(num, den);
    return sdl.int64;
}

/***
* Div128By96
*
* Entry:
*   rgulNum - Pointer to 128-bit dividend as array of ULONGs, least-sig first
*   rgulDen - Pointer to 96-bit divisor.
*
* Purpose:
*   Do partial divide, yielding 32-bit result and 96-bit remainder.
*   Top divisor ULONG must be larger than top dividend ULONG.  This is
*   assured in the initial call because the divisor is normalized
*   and the dividend can't be.  In subsequent calls, the remainder
*   is multiplied by 10^9 (max), so it can be no more than 1/4 of
*   the divisor which is effectively multiplied by 2^32 (4 * 10^9).
*
* Exit:
*   Remainder overwrites lower 96-bits of dividend.
*   Returns quotient.
*
* Exceptions:
*   None.
*
***********************************************************************/

ULONG Div128By96(ULONG *rgulNum, ULONG *rgulDen)
{
    LIMITED_METHOD_CONTRACT;

    SPLIT64 sdlQuo;
    SPLIT64 sdlNum;
    SPLIT64 sdlProd1;
    SPLIT64 sdlProd2;

    sdlNum.u.Lo = rgulNum[0];
    sdlNum.u.Hi = rgulNum[1];

    if (rgulNum[3] == 0 && rgulNum[2] < rgulDen[2])
      // Result is zero.  Entire dividend is remainder.
      //
      return 0;

    // DivMod64by32 returns quotient in Lo, remainder in Hi.
    //
    sdlQuo.u.Lo = rgulNum[2];
    sdlQuo.u.Hi = rgulNum[3];
    sdlQuo.int64 = DivMod64by32(sdlQuo.int64, rgulDen[2]);

    // Compute full remainder, rem = dividend - (quo * divisor).
    //
    sdlProd1.int64 = UInt32x32To64(sdlQuo.u.Lo, rgulDen[0]); // quo * lo divisor
    sdlProd2.int64 = UInt32x32To64(sdlQuo.u.Lo, rgulDen[1]); // quo * mid divisor
    sdlProd2.int64 += sdlProd1.u.Hi;
    sdlProd1.u.Hi = sdlProd2.u.Lo;

    sdlNum.int64 -= sdlProd1.int64;
    rgulNum[2] = sdlQuo.u.Hi - sdlProd2.u.Hi; // sdlQuo.Hi is remainder

    // Propagate carries
    //
    if (sdlNum.int64 > ~sdlProd1.int64) {
      rgulNum[2]--;
      if (rgulNum[2] >= ~sdlProd2.u.Hi)
    goto NegRem;
    }
    else if (rgulNum[2] > ~sdlProd2.u.Hi) {
NegRem:
      // Remainder went negative.  Add divisor back in until it's positive,
      // a max of 2 times.
      //
      sdlProd1.u.Lo = rgulDen[0];
      sdlProd1.u.Hi = rgulDen[1];

      for (;;) {
    sdlQuo.u.Lo--;
    sdlNum.int64 += sdlProd1.int64;
    rgulNum[2] += rgulDen[2];

    if (sdlNum.int64 < sdlProd1.int64) {
      // Detected carry. Check for carry out of top
      // before adding it in.
      //
      if (rgulNum[2]++ < rgulDen[2])
        break;
    }
    if (rgulNum[2] < rgulDen[2])
      break; // detected carry
      }
    }

    rgulNum[0] = sdlNum.u.Lo;
    rgulNum[1] = sdlNum.u.Hi;
    return sdlQuo.u.Lo;
}



/***
* Div96By32
*
* Entry:
*   rgulNum - Pointer to 96-bit dividend as array of ULONGs, least-sig first
*   ulDen   - 32-bit divisor.
*
* Purpose:
*   Do full divide, yielding 96-bit result and 32-bit remainder.
*
* Exit:
*   Quotient overwrites dividend.
*   Returns remainder.
*
* Exceptions:
*   None.
*
***********************************************************************/

ULONG Div96By32(ULONG *rgulNum, ULONG ulDen)
{
    LIMITED_METHOD_CONTRACT;

    SPLIT64  sdlTmp;

    sdlTmp.u.Hi = 0;

    if (rgulNum[2] != 0)
      goto Div3Word;

    if (rgulNum[1] >= ulDen)
      goto Div2Word;

    sdlTmp.u.Hi = rgulNum[1];
    rgulNum[1] = 0;
    goto Div1Word;

Div3Word:
    sdlTmp.u.Lo = rgulNum[2];
    sdlTmp.int64 = DivMod64by32(sdlTmp.int64, ulDen);
    rgulNum[2] = sdlTmp.u.Lo;
Div2Word:
    sdlTmp.u.Lo = rgulNum[1];
    sdlTmp.int64 = DivMod64by32(sdlTmp.int64, ulDen);
    rgulNum[1] = sdlTmp.u.Lo;
Div1Word:
    sdlTmp.u.Lo = rgulNum[0];
    sdlTmp.int64 = DivMod64by32(sdlTmp.int64, ulDen);
    rgulNum[0] = sdlTmp.u.Lo;
    return sdlTmp.u.Hi;
}


/***
* Div96By64
*
* Entry:
*   rgulNum - Pointer to 96-bit dividend as array of ULONGs, least-sig first
*   sdlDen  - 64-bit divisor.
*
* Purpose:
*   Do partial divide, yielding 32-bit result and 64-bit remainder.
*   Divisor must be larger than upper 64 bits of dividend.
*
* Exit:
*   Remainder overwrites lower 64-bits of dividend.
*   Returns quotient.
*
* Exceptions:
*   None.
*
***********************************************************************/

ULONG Div96By64(ULONG *rgulNum, SPLIT64 sdlDen)
{
    LIMITED_METHOD_CONTRACT;
    
    SPLIT64 sdlQuo;
    SPLIT64 sdlNum;
    SPLIT64 sdlProd;

    sdlNum.u.Lo = rgulNum[0];

    if (rgulNum[2] >= sdlDen.u.Hi) {
      // Divide would overflow.  Assume a quotient of 2^32, and set
      // up remainder accordingly.  Then jump to loop which reduces
      // the quotient.
      //
      sdlNum.u.Hi = rgulNum[1] - sdlDen.u.Lo;
      sdlQuo.u.Lo = 0;
      goto NegRem;
    }

    // Hardware divide won't overflow
    //
    if (rgulNum[2] == 0 && rgulNum[1] < sdlDen.u.Hi)
      // Result is zero.  Entire dividend is remainder.
      //
      return 0;

    // DivMod64by32 returns quotient in Lo, remainder in Hi.
    //
    sdlQuo.u.Lo = rgulNum[1];
    sdlQuo.u.Hi = rgulNum[2];
    sdlQuo.int64 = DivMod64by32(sdlQuo.int64, sdlDen.u.Hi);
    sdlNum.u.Hi = sdlQuo.u.Hi; // remainder

    // Compute full remainder, rem = dividend - (quo * divisor).
    //
    sdlProd.int64 = UInt32x32To64(sdlQuo.u.Lo, sdlDen.u.Lo); // quo * lo divisor
    sdlNum.int64 -= sdlProd.int64;

    if (sdlNum.int64 > ~sdlProd.int64) {
NegRem:
      // Remainder went negative.  Add divisor back in until it's positive,
      // a max of 2 times.
      //
      do {
    sdlQuo.u.Lo--;
    sdlNum.int64 += sdlDen.int64;
      }while (sdlNum.int64 >= sdlDen.int64);
    }

    rgulNum[0] = sdlNum.u.Lo;
    rgulNum[1] = sdlNum.u.Hi;
    return sdlQuo.u.Lo;
}

// Add a 32 bit unsigned long to an array of 3 unsigned longs representing a 96 integer
// Returns FALSE if there is an overflow
BOOL Add32To96(ULONG *rgulNum, ULONG ulValue) {
    rgulNum[0] += ulValue;
    if (rgulNum[0] < ulValue) {
        if (++rgulNum[1] == 0) {                
            if (++rgulNum[2] == 0) {                
                return FALSE;
            }            
        }
    }
    return TRUE;
}

// Adjust the quotient to deal with an overflow. We need to divide by 10, 
// feed in the high bit to undo the overflow and then round as required, 
void OverflowUnscale(ULONG *rgulQuo, BOOL fRemainder) {
    LIMITED_METHOD_CONTRACT;

    SPLIT64  sdlTmp;

    // We have overflown, so load the high bit with a one.
    sdlTmp.u.Hi = 1u;
    sdlTmp.u.Lo = rgulQuo[2];
    sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10u);
    rgulQuo[2] = sdlTmp.u.Lo;
    sdlTmp.u.Lo = rgulQuo[1];
    sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10u);
    rgulQuo[1] = sdlTmp.u.Lo;
    sdlTmp.u.Lo = rgulQuo[0];
    sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10u);
    rgulQuo[0] = sdlTmp.u.Lo;
    // The remainder is the last digit that does not fit, so we can use it to work out if we need to round up
    if ((sdlTmp.u.Hi > 5) || ((sdlTmp.u.Hi == 5) && ( fRemainder || (rgulQuo[0] & 1)))) {
        Add32To96(rgulQuo, 1u);
    }
}



//**********************************************************************
//
// VarDecDiv - Decimal Divide
//
//**********************************************************************

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif

FCIMPL2(void, COMDecimal::DoDivideThrow, DECIMAL * pdecL, DECIMAL * pdecR)
{
    FCALL_CONTRACT;

    ULONG   rgulQuo[3];
    ULONG   rgulQuoSave[3];
    ULONG   rgulRem[4];
    ULONG   rgulDivisor[3];
    ULONG   ulPwr;
    ULONG   ulTmp;
    ULONG   ulTmp1;
    SPLIT64 sdlTmp;
    SPLIT64 sdlDivisor;
    int     iScale;
    int     iCurScale;
    BOOL    fUnscale;

    iScale = DECIMAL_SCALE(*pdecL) - DECIMAL_SCALE(*pdecR);
    fUnscale = FALSE;
    rgulDivisor[0] = DECIMAL_LO32(*pdecR);
    rgulDivisor[1] = DECIMAL_MID32(*pdecR);
    rgulDivisor[2] = DECIMAL_HI32(*pdecR);

    if (rgulDivisor[1] == 0 && rgulDivisor[2] == 0) {
      // Divisor is only 32 bits.  Easy divide.
      //
      if (rgulDivisor[0] == 0)
        FCThrowVoid(kDivideByZeroException);

      rgulQuo[0] = DECIMAL_LO32(*pdecL);
      rgulQuo[1] = DECIMAL_MID32(*pdecL);
      rgulQuo[2] = DECIMAL_HI32(*pdecL);
      rgulRem[0] = Div96By32(rgulQuo, rgulDivisor[0]);

      for (;;) {
    if (rgulRem[0] == 0) {
      if (iScale < 0) {
        iCurScale = min(9, -iScale);
        goto HaveScale;
      }
      break;
    }
    // We need to unscale if and only if we have a non-zero remainder
    fUnscale = TRUE;

    // We have computed a quotient based on the natural scale 
    // ( <dividend scale> - <divisor scale> ).  We have a non-zero 
    // remainder, so now we should increase the scale if possible to 
    // include more quotient bits.
    // 
    // If it doesn't cause overflow, we'll loop scaling by 10^9 and 
    // computing more quotient bits as long as the remainder stays 
    // non-zero.  If scaling by that much would cause overflow, we'll 
    // drop out of the loop and scale by as much as we can.
    // 
    // Scaling by 10^9 will overflow if rgulQuo[2].rgulQuo[1] >= 2^32 / 10^9 
    // = 4.294 967 296.  So the upper limit is rgulQuo[2] == 4 and 
    // rgulQuo[1] == 0.294 967 296 * 2^32 = 1,266,874,889.7+.  Since 
    // quotient bits in rgulQuo[0] could be all 1's, then 1,266,874,888 
    // is the largest value in rgulQuo[1] (when rgulQuo[2] == 4) that is 
    // assured not to overflow.
    // 
    iCurScale = SearchScale(rgulQuo[2], rgulQuo[1], rgulQuo[0], iScale);
    if (iCurScale == 0) {
      // No more scaling to be done, but remainder is non-zero.
      // Round quotient.
      //
      ulTmp = rgulRem[0] << 1;
      if (ulTmp < rgulRem[0] || (ulTmp >= rgulDivisor[0] &&
          (ulTmp > rgulDivisor[0] || (rgulQuo[0] & 1)))) {
RoundUp:
        if (!Add32To96(rgulQuo, 1)) {
            if (iScale == 0) {
                FCThrowResVoid(kOverflowException, W("Overflow_Decimal"));
            }
            iScale--;
            OverflowUnscale(rgulQuo, TRUE);
            break;
        }      
      }
      break;
    }

    if (iCurScale < 0) {
      FCThrowResVoid(kOverflowException, W("Overflow_Decimal"));
    }

HaveScale:
    ulPwr = rgulPower10[iCurScale];
    iScale += iCurScale;

    if (IncreaseScale(rgulQuo, ulPwr) != 0) {
      FCThrowResVoid(kOverflowException, W("Overflow_Decimal"));
    }


    sdlTmp.int64 = DivMod64by32(UInt32x32To64(rgulRem[0], ulPwr), rgulDivisor[0]);
    rgulRem[0] = sdlTmp.u.Hi;

    if (!Add32To96(rgulQuo, sdlTmp.u.Lo)) {
        if (iScale == 0) {
            FCThrowResVoid(kOverflowException, W("Overflow_Decimal"));
        }
        iScale--;
        OverflowUnscale(rgulQuo, (rgulRem[0] != 0));
        break;
    }
      } // for (;;)
    }
    else {
      // Divisor has bits set in the upper 64 bits.
      //
      // Divisor must be fully normalized (shifted so bit 31 of the most 
      // significant ULONG is 1).  Locate the MSB so we know how much to 
      // normalize by.  The dividend will be shifted by the same amount so 
      // the quotient is not changed.
      //
      if (rgulDivisor[2] == 0)
    ulTmp = rgulDivisor[1];
      else
    ulTmp = rgulDivisor[2];

      iCurScale = 0;
      if (!(ulTmp & 0xFFFF0000)) {
    iCurScale += 16;
    ulTmp <<= 16;
      }
      if (!(ulTmp & 0xFF000000)) {
    iCurScale += 8;
    ulTmp <<= 8;
      }
      if (!(ulTmp & 0xF0000000)) {
    iCurScale += 4;
    ulTmp <<= 4;
      }
      if (!(ulTmp & 0xC0000000)) {
    iCurScale += 2;
    ulTmp <<= 2;
      }
      if (!(ulTmp & 0x80000000)) {
    iCurScale++;
    ulTmp <<= 1;
      }
    
      // Shift both dividend and divisor left by iCurScale.
      // 
      sdlTmp.int64 = DECIMAL_LO64_GET(*pdecL) << iCurScale;
      rgulRem[0] = sdlTmp.u.Lo;
      rgulRem[1] = sdlTmp.u.Hi;
      sdlTmp.u.Lo = DECIMAL_MID32(*pdecL);
      sdlTmp.u.Hi = DECIMAL_HI32(*pdecL);
      sdlTmp.int64 <<= iCurScale;
      rgulRem[2] = sdlTmp.u.Hi;
      rgulRem[3] = (DECIMAL_HI32(*pdecL) >> (31 - iCurScale)) >> 1;

      sdlDivisor.u.Lo = rgulDivisor[0];
      sdlDivisor.u.Hi = rgulDivisor[1];
      sdlDivisor.int64 <<= iCurScale;

      if (rgulDivisor[2] == 0) {
    // Have a 64-bit divisor in sdlDivisor.  The remainder 
    // (currently 96 bits spread over 4 ULONGs) will be < divisor.
    // 
    sdlTmp.u.Lo = rgulRem[2];
    sdlTmp.u.Hi = rgulRem[3];

    rgulQuo[2] = 0;
    rgulQuo[1] = Div96By64(&rgulRem[1], sdlDivisor);
    rgulQuo[0] = Div96By64(rgulRem, sdlDivisor);

    for (;;) {
      if ((rgulRem[0] | rgulRem[1]) == 0) {
        if (iScale < 0) {
          iCurScale = min(9, -iScale);
          goto HaveScale64;
        }
        break;
      }

      // We need to unscale if and only if we have a non-zero remainder
      fUnscale = TRUE;

      // Remainder is non-zero.  Scale up quotient and remainder by 
      // powers of 10 so we can compute more significant bits.
      // 
      iCurScale = SearchScale(rgulQuo[2], rgulQuo[1], rgulQuo[0], iScale);
      if (iCurScale == 0) {
        // No more scaling to be done, but remainder is non-zero.
        // Round quotient.
        //
        sdlTmp.u.Lo = rgulRem[0];
        sdlTmp.u.Hi = rgulRem[1];
        if (sdlTmp.u.Hi >= 0x80000000 || (sdlTmp.int64 <<= 1) > sdlDivisor.int64 ||
        (sdlTmp.int64 == sdlDivisor.int64 && (rgulQuo[0] & 1)))
          goto RoundUp;
        break;
      }

      if (iCurScale < 0) {
          FCThrowResVoid(kOverflowException, W("Overflow_Decimal"));
      }

HaveScale64:
      ulPwr = rgulPower10[iCurScale];
      iScale += iCurScale;

      if (IncreaseScale(rgulQuo, ulPwr) != 0) {
        FCThrowResVoid(kOverflowException, W("Overflow_Decimal"));
      }


      rgulRem[2] = 0;  // rem is 64 bits, IncreaseScale uses 96
      IncreaseScale(rgulRem, ulPwr);
      ulTmp = Div96By64(rgulRem, sdlDivisor);
      if (!Add32To96(rgulQuo, ulTmp)) {
        if (iScale == 0) {
            FCThrowResVoid(kOverflowException, W("Overflow_Decimal"));
        }
        iScale--;
        OverflowUnscale(rgulQuo, (rgulRem[0] != 0 || rgulRem[1] != 0));
        break;
      }      

    } // for (;;)
      }
      else {
    // Have a 96-bit divisor in rgulDivisor[].
    //
    // Start by finishing the shift left by iCurScale.
    //
    sdlTmp.u.Lo = rgulDivisor[1];
    sdlTmp.u.Hi = rgulDivisor[2];
    sdlTmp.int64 <<= iCurScale;
    rgulDivisor[0] = sdlDivisor.u.Lo;
    rgulDivisor[1] = sdlDivisor.u.Hi;
    rgulDivisor[2] = sdlTmp.u.Hi;

    // The remainder (currently 96 bits spread over 4 ULONGs) 
    // will be < divisor.
    // 
    rgulQuo[2] = 0;
    rgulQuo[1] = 0;
    rgulQuo[0] = Div128By96(rgulRem, rgulDivisor);

    for (;;) {
      if ((rgulRem[0] | rgulRem[1] | rgulRem[2]) == 0) {
        if (iScale < 0) {
          iCurScale = min(9, -iScale);
          goto HaveScale96;
        }
        break;
      }

      // We need to unscale if and only if we have a non-zero remainder
      fUnscale = TRUE;

      // Remainder is non-zero.  Scale up quotient and remainder by 
      // powers of 10 so we can compute more significant bits.
      // 
      iCurScale = SearchScale(rgulQuo[2], rgulQuo[1], rgulQuo[0], iScale);
      if (iCurScale == 0) {
        // No more scaling to be done, but remainder is non-zero.
        // Round quotient.
        //
        if (rgulRem[2] >= 0x80000000)
          goto RoundUp;

        ulTmp = rgulRem[0] > 0x80000000;
        ulTmp1 = rgulRem[1] > 0x80000000;
        rgulRem[0] <<= 1;
        rgulRem[1] = (rgulRem[1] << 1) + ulTmp;
        rgulRem[2] = (rgulRem[2] << 1) + ulTmp1;

        if (rgulRem[2] > rgulDivisor[2] || (rgulRem[2] == rgulDivisor[2] &&
        (rgulRem[1] > rgulDivisor[1] || (rgulRem[1] == rgulDivisor[1] &&
        (rgulRem[0] > rgulDivisor[0] || (rgulRem[0] == rgulDivisor[0] &&
        (rgulQuo[0] & 1)))))))
          goto RoundUp;
        break;
      }

      if (iCurScale < 0) {
        FCThrowResVoid(kOverflowException, W("Overflow_Decimal"));
      }

HaveScale96:
      ulPwr = rgulPower10[iCurScale];
      iScale += iCurScale;

      if (IncreaseScale(rgulQuo, ulPwr) != 0) {
        FCThrowResVoid(kOverflowException, W("Overflow_Decimal"));
      }

      rgulRem[3] = IncreaseScale(rgulRem, ulPwr);
      ulTmp = Div128By96(rgulRem, rgulDivisor);
      if (!Add32To96(rgulQuo, ulTmp)) {
        if (iScale == 0) {
          FCThrowResVoid(kOverflowException, W("Overflow_Decimal"));
        }
        iScale--;
        OverflowUnscale(rgulQuo, (rgulRem[0] != 0 || rgulRem[1] != 0 || rgulRem[2] != 0 || rgulRem[3] != 0));
        break;
      }      

    } // for (;;)
      }
    }

    // We need to unscale if and only if we have a non-zero remainder
    if (fUnscale) {
        // Try extracting any extra powers of 10 we may have 
        // added.  We do this by trying to divide out 10^8, 10^4, 10^2, and 10^1.
        // If a division by one of these powers returns a zero remainder, then
        // we keep the quotient.  If the remainder is not zero, then we restore
        // the previous value.
        // 
        // Since 10 = 2 * 5, there must be a factor of 2 for every power of 10
        // we can extract.  We use this as a quick test on whether to try a
        // given power.
        // 
        while ((rgulQuo[0] & 0xFF) == 0 && iScale >= 8) {
            rgulQuoSave[0] = rgulQuo[0];
            rgulQuoSave[1] = rgulQuo[1];
            rgulQuoSave[2] = rgulQuo[2];

            if (Div96By32(rgulQuoSave, 100000000) == 0) {
            rgulQuo[0] = rgulQuoSave[0];
            rgulQuo[1] = rgulQuoSave[1];
            rgulQuo[2] = rgulQuoSave[2];
            iScale -= 8;
            }
            else
            break;
        }

        if ((rgulQuo[0] & 0xF) == 0 && iScale >= 4) {
            rgulQuoSave[0] = rgulQuo[0];
            rgulQuoSave[1] = rgulQuo[1];
            rgulQuoSave[2] = rgulQuo[2];

            if (Div96By32(rgulQuoSave, 10000) == 0) {
            rgulQuo[0] = rgulQuoSave[0];
            rgulQuo[1] = rgulQuoSave[1];
            rgulQuo[2] = rgulQuoSave[2];
            iScale -= 4;
            }
        }

        if ((rgulQuo[0] & 3) == 0 && iScale >= 2) {
            rgulQuoSave[0] = rgulQuo[0];
            rgulQuoSave[1] = rgulQuo[1];
            rgulQuoSave[2] = rgulQuo[2];

            if (Div96By32(rgulQuoSave, 100) == 0) {
            rgulQuo[0] = rgulQuoSave[0];
            rgulQuo[1] = rgulQuoSave[1];
            rgulQuo[2] = rgulQuoSave[2];
            iScale -= 2;
            }
        }

        if ((rgulQuo[0] & 1) == 0 && iScale >= 1) {
            rgulQuoSave[0] = rgulQuo[0];
            rgulQuoSave[1] = rgulQuo[1];
            rgulQuoSave[2] = rgulQuo[2];

            if (Div96By32(rgulQuoSave, 10) == 0) {
            rgulQuo[0] = rgulQuoSave[0];
            rgulQuo[1] = rgulQuoSave[1];
            rgulQuo[2] = rgulQuoSave[2];
            iScale -= 1;
            }
        }
    }

    DECIMAL_SIGN(*pdecL) = DECIMAL_SIGN(*pdecL) ^ DECIMAL_SIGN(*pdecR);
    DECIMAL_HI32(*pdecL) = rgulQuo[2];
    DECIMAL_MID32(*pdecL) = rgulQuo[1];
    DECIMAL_LO32(*pdecL) = rgulQuo[0];
    DECIMAL_SCALE(*pdecL) = (BYTE)iScale;

    pdecL->wReserved = 0;
    FC_GC_POLL();
}
FCIMPLEND


FCIMPL3(void, COMDecimal::DoDivide, DECIMAL * pdecL, DECIMAL * pdecR, CLR_BOOL * overflowed)
{
    FCALL_CONTRACT;

    ULONG   rgulQuo[3];
    ULONG   rgulQuoSave[3];
    ULONG   rgulRem[4];
    ULONG   rgulDivisor[3];
    ULONG   ulPwr;
    ULONG   ulTmp;
    ULONG   ulTmp1;
    SPLIT64 sdlTmp;
    SPLIT64 sdlDivisor;
    int     iScale;
    int     iCurScale;
    BOOL    fUnscale;

    iScale = DECIMAL_SCALE(*pdecL) - DECIMAL_SCALE(*pdecR);
    fUnscale = FALSE;
    rgulDivisor[0] = DECIMAL_LO32(*pdecR);
    rgulDivisor[1] = DECIMAL_MID32(*pdecR);
    rgulDivisor[2] = DECIMAL_HI32(*pdecR);

    if (rgulDivisor[1] == 0 && rgulDivisor[2] == 0) {
      // Divisor is only 32 bits.  Easy divide.
      //
      if (rgulDivisor[0] == 0)
        FCThrowVoid(kDivideByZeroException);

      rgulQuo[0] = DECIMAL_LO32(*pdecL);
      rgulQuo[1] = DECIMAL_MID32(*pdecL);
      rgulQuo[2] = DECIMAL_HI32(*pdecL);
      rgulRem[0] = Div96By32(rgulQuo, rgulDivisor[0]);

      for (;;) {
    if (rgulRem[0] == 0) {
      if (iScale < 0) {
        iCurScale = min(9, -iScale);
        goto HaveScale;
      }
      break;
    }
    // We need to unscale if and only if we have a non-zero remainder
    fUnscale = TRUE;

    // We have computed a quotient based on the natural scale 
    // ( <dividend scale> - <divisor scale> ).  We have a non-zero 
    // remainder, so now we should increase the scale if possible to 
    // include more quotient bits.
    // 
    // If it doesn't cause overflow, we'll loop scaling by 10^9 and 
    // computing more quotient bits as long as the remainder stays 
    // non-zero.  If scaling by that much would cause overflow, we'll 
    // drop out of the loop and scale by as much as we can.
    // 
    // Scaling by 10^9 will overflow if rgulQuo[2].rgulQuo[1] >= 2^32 / 10^9 
    // = 4.294 967 296.  So the upper limit is rgulQuo[2] == 4 and 
    // rgulQuo[1] == 0.294 967 296 * 2^32 = 1,266,874,889.7+.  Since 
    // quotient bits in rgulQuo[0] could be all 1's, then 1,266,874,888 
    // is the largest value in rgulQuo[1] (when rgulQuo[2] == 4) that is 
    // assured not to overflow.
    // 
    iCurScale = SearchScale(rgulQuo[2], rgulQuo[1], rgulQuo[0], iScale);
    if (iCurScale == 0) {
      // No more scaling to be done, but remainder is non-zero.
      // Round quotient.
      //
      ulTmp = rgulRem[0] << 1;
      if (ulTmp < rgulRem[0] || (ulTmp >= rgulDivisor[0] &&
          (ulTmp > rgulDivisor[0] || (rgulQuo[0] & 1)))) {
RoundUp:
        if (!Add32To96(rgulQuo, 1)) {
            if (iScale == 0) {
                *overflowed = true;
                FC_GC_POLL();
                return;
            }
            iScale--;
            OverflowUnscale(rgulQuo, TRUE);
            break;
        }      
      }
      break;
    }

    if (iCurScale < 0) {
        *overflowed = true;
        FC_GC_POLL();
        return;
    }

HaveScale:
    ulPwr = rgulPower10[iCurScale];
    iScale += iCurScale;

    if (IncreaseScale(rgulQuo, ulPwr) != 0) {
        *overflowed = true;
        FC_GC_POLL();
        return;
    }


    sdlTmp.int64 = DivMod64by32(UInt32x32To64(rgulRem[0], ulPwr), rgulDivisor[0]);
    rgulRem[0] = sdlTmp.u.Hi;

    if (!Add32To96(rgulQuo, sdlTmp.u.Lo)) {
        if (iScale == 0) {
            *overflowed = true;
            FC_GC_POLL();
            return;
        }
        iScale--;
        OverflowUnscale(rgulQuo, (rgulRem[0] != 0));
        break;
    }
      } // for (;;)
    }
    else {
      // Divisor has bits set in the upper 64 bits.
      //
      // Divisor must be fully normalized (shifted so bit 31 of the most 
      // significant ULONG is 1).  Locate the MSB so we know how much to 
      // normalize by.  The dividend will be shifted by the same amount so 
      // the quotient is not changed.
      //
      if (rgulDivisor[2] == 0)
    ulTmp = rgulDivisor[1];
      else
    ulTmp = rgulDivisor[2];

      iCurScale = 0;
      if (!(ulTmp & 0xFFFF0000)) {
    iCurScale += 16;
    ulTmp <<= 16;
      }
      if (!(ulTmp & 0xFF000000)) {
    iCurScale += 8;
    ulTmp <<= 8;
      }
      if (!(ulTmp & 0xF0000000)) {
    iCurScale += 4;
    ulTmp <<= 4;
      }
      if (!(ulTmp & 0xC0000000)) {
    iCurScale += 2;
    ulTmp <<= 2;
      }
      if (!(ulTmp & 0x80000000)) {
    iCurScale++;
    ulTmp <<= 1;
      }
    
      // Shift both dividend and divisor left by iCurScale.
      // 
      sdlTmp.int64 = DECIMAL_LO64_GET(*pdecL) << iCurScale;
      rgulRem[0] = sdlTmp.u.Lo;
      rgulRem[1] = sdlTmp.u.Hi;
      sdlTmp.u.Lo = DECIMAL_MID32(*pdecL);
      sdlTmp.u.Hi = DECIMAL_HI32(*pdecL);
      sdlTmp.int64 <<= iCurScale;
      rgulRem[2] = sdlTmp.u.Hi;
      rgulRem[3] = (DECIMAL_HI32(*pdecL) >> (31 - iCurScale)) >> 1;

      sdlDivisor.u.Lo = rgulDivisor[0];
      sdlDivisor.u.Hi = rgulDivisor[1];
      sdlDivisor.int64 <<= iCurScale;

      if (rgulDivisor[2] == 0) {
    // Have a 64-bit divisor in sdlDivisor.  The remainder 
    // (currently 96 bits spread over 4 ULONGs) will be < divisor.
    // 
    sdlTmp.u.Lo = rgulRem[2];
    sdlTmp.u.Hi = rgulRem[3];

    rgulQuo[2] = 0;
    rgulQuo[1] = Div96By64(&rgulRem[1], sdlDivisor);
    rgulQuo[0] = Div96By64(rgulRem, sdlDivisor);

    for (;;) {
      if ((rgulRem[0] | rgulRem[1]) == 0) {
        if (iScale < 0) {
          iCurScale = min(9, -iScale);
          goto HaveScale64;
        }
        break;
      }

      // We need to unscale if and only if we have a non-zero remainder
      fUnscale = TRUE;

      // Remainder is non-zero.  Scale up quotient and remainder by 
      // powers of 10 so we can compute more significant bits.
      // 
      iCurScale = SearchScale(rgulQuo[2], rgulQuo[1], rgulQuo[0], iScale);
      if (iCurScale == 0) {
        // No more scaling to be done, but remainder is non-zero.
        // Round quotient.
        //
        sdlTmp.u.Lo = rgulRem[0];
        sdlTmp.u.Hi = rgulRem[1];
        if (sdlTmp.u.Hi >= 0x80000000 || (sdlTmp.int64 <<= 1) > sdlDivisor.int64 ||
        (sdlTmp.int64 == sdlDivisor.int64 && (rgulQuo[0] & 1)))
          goto RoundUp;
        break;
      }

      if (iCurScale < 0) {
          *overflowed = true;
          FC_GC_POLL();
          return;
      }

HaveScale64:
      ulPwr = rgulPower10[iCurScale];
      iScale += iCurScale;

      if (IncreaseScale(rgulQuo, ulPwr) != 0) {
          *overflowed = true;
          FC_GC_POLL();
          return;
      }


      rgulRem[2] = 0;  // rem is 64 bits, IncreaseScale uses 96
      IncreaseScale(rgulRem, ulPwr);
      ulTmp = Div96By64(rgulRem, sdlDivisor);
      if (!Add32To96(rgulQuo, ulTmp)) {
        if (iScale == 0) {
            *overflowed = true;
            FC_GC_POLL();
            return;
        }
        iScale--;
        OverflowUnscale(rgulQuo, (rgulRem[0] != 0 || rgulRem[1] != 0));
        break;
      }      

    } // for (;;)
      }
      else {
    // Have a 96-bit divisor in rgulDivisor[].
    //
    // Start by finishing the shift left by iCurScale.
    //
    sdlTmp.u.Lo = rgulDivisor[1];
    sdlTmp.u.Hi = rgulDivisor[2];
    sdlTmp.int64 <<= iCurScale;
    rgulDivisor[0] = sdlDivisor.u.Lo;
    rgulDivisor[1] = sdlDivisor.u.Hi;
    rgulDivisor[2] = sdlTmp.u.Hi;

    // The remainder (currently 96 bits spread over 4 ULONGs) 
    // will be < divisor.
    // 
    rgulQuo[2] = 0;
    rgulQuo[1] = 0;
    rgulQuo[0] = Div128By96(rgulRem, rgulDivisor);

    for (;;) {
      if ((rgulRem[0] | rgulRem[1] | rgulRem[2]) == 0) {
        if (iScale < 0) {
          iCurScale = min(9, -iScale);
          goto HaveScale96;
        }
        break;
      }

      // We need to unscale if and only if we have a non-zero remainder
      fUnscale = TRUE;

      // Remainder is non-zero.  Scale up quotient and remainder by 
      // powers of 10 so we can compute more significant bits.
      // 
      iCurScale = SearchScale(rgulQuo[2], rgulQuo[1], rgulQuo[0], iScale);
      if (iCurScale == 0) {
        // No more scaling to be done, but remainder is non-zero.
        // Round quotient.
        //
        if (rgulRem[2] >= 0x80000000)
          goto RoundUp;

        ulTmp = rgulRem[0] > 0x80000000;
        ulTmp1 = rgulRem[1] > 0x80000000;
        rgulRem[0] <<= 1;
        rgulRem[1] = (rgulRem[1] << 1) + ulTmp;
        rgulRem[2] = (rgulRem[2] << 1) + ulTmp1;

        if (rgulRem[2] > rgulDivisor[2] || (rgulRem[2] == rgulDivisor[2] &&
        (rgulRem[1] > rgulDivisor[1] || (rgulRem[1] == rgulDivisor[1] &&
        (rgulRem[0] > rgulDivisor[0] || (rgulRem[0] == rgulDivisor[0] &&
        (rgulQuo[0] & 1)))))))
          goto RoundUp;
        break;
      }

      if (iCurScale < 0) {
          *overflowed = true;
          FC_GC_POLL();
          return;
      }

HaveScale96:
      ulPwr = rgulPower10[iCurScale];
      iScale += iCurScale;

      if (IncreaseScale(rgulQuo, ulPwr) != 0) {
          *overflowed = true;
          FC_GC_POLL();
          return;
      }

      rgulRem[3] = IncreaseScale(rgulRem, ulPwr);
      ulTmp = Div128By96(rgulRem, rgulDivisor);
      if (!Add32To96(rgulQuo, ulTmp)) {
        if (iScale == 0) {
          *overflowed = true;
          FC_GC_POLL();
          return;
        }
        iScale--;
        OverflowUnscale(rgulQuo, (rgulRem[0] != 0 || rgulRem[1] != 0 || rgulRem[2] != 0 || rgulRem[3] != 0));
        break;
      }      

    } // for (;;)
      }
    }

    // We need to unscale if and only if we have a non-zero remainder
    if (fUnscale) {
        // Try extracting any extra powers of 10 we may have 
        // added.  We do this by trying to divide out 10^8, 10^4, 10^2, and 10^1.
        // If a division by one of these powers returns a zero remainder, then
        // we keep the quotient.  If the remainder is not zero, then we restore
        // the previous value.
        // 
        // Since 10 = 2 * 5, there must be a factor of 2 for every power of 10
        // we can extract.  We use this as a quick test on whether to try a
        // given power.
        // 
        while ((rgulQuo[0] & 0xFF) == 0 && iScale >= 8) {
            rgulQuoSave[0] = rgulQuo[0];
            rgulQuoSave[1] = rgulQuo[1];
            rgulQuoSave[2] = rgulQuo[2];

            if (Div96By32(rgulQuoSave, 100000000) == 0) {
            rgulQuo[0] = rgulQuoSave[0];
            rgulQuo[1] = rgulQuoSave[1];
            rgulQuo[2] = rgulQuoSave[2];
            iScale -= 8;
            }
            else
            break;
        }

        if ((rgulQuo[0] & 0xF) == 0 && iScale >= 4) {
            rgulQuoSave[0] = rgulQuo[0];
            rgulQuoSave[1] = rgulQuo[1];
            rgulQuoSave[2] = rgulQuo[2];

            if (Div96By32(rgulQuoSave, 10000) == 0) {
            rgulQuo[0] = rgulQuoSave[0];
            rgulQuo[1] = rgulQuoSave[1];
            rgulQuo[2] = rgulQuoSave[2];
            iScale -= 4;
            }
        }

        if ((rgulQuo[0] & 3) == 0 && iScale >= 2) {
            rgulQuoSave[0] = rgulQuo[0];
            rgulQuoSave[1] = rgulQuo[1];
            rgulQuoSave[2] = rgulQuo[2];

            if (Div96By32(rgulQuoSave, 100) == 0) {
            rgulQuo[0] = rgulQuoSave[0];
            rgulQuo[1] = rgulQuoSave[1];
            rgulQuo[2] = rgulQuoSave[2];
            iScale -= 2;
            }
        }

        if ((rgulQuo[0] & 1) == 0 && iScale >= 1) {
            rgulQuoSave[0] = rgulQuo[0];
            rgulQuoSave[1] = rgulQuo[1];
            rgulQuoSave[2] = rgulQuo[2];

            if (Div96By32(rgulQuoSave, 10) == 0) {
            rgulQuo[0] = rgulQuoSave[0];
            rgulQuo[1] = rgulQuoSave[1];
            rgulQuo[2] = rgulQuoSave[2];
            iScale -= 1;
            }
        }
    }

    DECIMAL_SIGN(*pdecL) = DECIMAL_SIGN(*pdecL) ^ DECIMAL_SIGN(*pdecR);
    DECIMAL_HI32(*pdecL) = rgulQuo[2];
    DECIMAL_MID32(*pdecL) = rgulQuo[1];
    DECIMAL_LO32(*pdecL) = rgulQuo[0];
    DECIMAL_SCALE(*pdecL) = (BYTE)iScale;

    pdecL->wReserved = 0;
    *overflowed = false;
    FC_GC_POLL();
}
FCIMPLEND

#ifdef _PREFAST_
#pragma warning(pop)
#endif


//**********************************************************************
//
// VarDecAdd - Decimal Addition
// VarDecSub - Decimal Subtraction
//
//**********************************************************************

static const ULONG ulTenToNine    = 1000000000;

/***
* ScaleResult
*
* Entry:
*   rgulRes - Array of ULONGs with value, least-significant first.
*   iHiRes  - Index of last non-zero value in rgulRes.
*   iScale  - Scale factor for this value, range 0 - 2 * DEC_SCALE_MAX
*
* Purpose:
*   See if we need to scale the result to fit it in 96 bits.
*   Perform needed scaling.  Adjust scale factor accordingly.
*
* Exit:
*   rgulRes updated in place, always 3 ULONGs.
*   New scale factor returned, -1 if overflow error.
*
***********************************************************************/

int ScaleResult(ULONG *rgulRes, int iHiRes, int iScale)
{
    LIMITED_METHOD_CONTRACT;

    int     iNewScale;
    int     iCur;
    ULONG   ulPwr;
    ULONG   ulTmp;
    ULONG   ulSticky;
    SPLIT64 sdlTmp;

    // See if we need to scale the result.  The combined scale must
    // be <= DEC_SCALE_MAX and the upper 96 bits must be zero.
    // 
    // Start by figuring a lower bound on the scaling needed to make
    // the upper 96 bits zero.  iHiRes is the index into rgulRes[]
    // of the highest non-zero ULONG.
    // 
    iNewScale =   iHiRes * 32 - 64 - 1;
    if (iNewScale > 0) {

      // Find the MSB.
      //
      ulTmp = rgulRes[iHiRes];
      if (!(ulTmp & 0xFFFF0000)) {
    iNewScale -= 16;
    ulTmp <<= 16;
      }
      if (!(ulTmp & 0xFF000000)) {
    iNewScale -= 8;
    ulTmp <<= 8;
      }
      if (!(ulTmp & 0xF0000000)) {
    iNewScale -= 4;
    ulTmp <<= 4;
      }
      if (!(ulTmp & 0xC0000000)) {
    iNewScale -= 2;
    ulTmp <<= 2;
      }
      if (!(ulTmp & 0x80000000)) {
    iNewScale--;
    ulTmp <<= 1;
      }
    
      // Multiply bit position by log10(2) to figure it's power of 10.
      // We scale the log by 256.  log(2) = .30103, * 256 = 77.  Doing this 
      // with a multiply saves a 96-byte lookup table.  The power returned
      // is <= the power of the number, so we must add one power of 10
      // to make it's integer part zero after dividing by 256.
      // 
      // Note: the result of this multiplication by an approximation of
      // log10(2) have been exhaustively checked to verify it gives the 
      // correct result.  (There were only 95 to check...)
      // 
      iNewScale = ((iNewScale * 77) >> 8) + 1;

      // iNewScale = min scale factor to make high 96 bits zero, 0 - 29.
      // This reduces the scale factor of the result.  If it exceeds the
      // current scale of the result, we'll overflow.
      // 
      if (iNewScale > iScale)
    return -1;
    }
    else
      iNewScale = 0;

    // Make sure we scale by enough to bring the current scale factor
    // into valid range.
    //
    if (iNewScale < iScale - DEC_SCALE_MAX)
      iNewScale = iScale - DEC_SCALE_MAX;

    if (iNewScale != 0) {
      // Scale by the power of 10 given by iNewScale.  Note that this is 
      // NOT guaranteed to bring the number within 96 bits -- it could 
      // be 1 power of 10 short.
      //
      iScale -= iNewScale;
      ulSticky = 0;
      sdlTmp.u.Hi = 0; // initialize remainder

      for (;;) {

    ulSticky |= sdlTmp.u.Hi; // record remainder as sticky bit

    if (iNewScale > POWER10_MAX)
      ulPwr = ulTenToNine;
    else
      ulPwr = rgulPower10[iNewScale];

    // Compute first quotient.
    // DivMod64by32 returns quotient in Lo, remainder in Hi.
    //
    sdlTmp.int64 = DivMod64by32(rgulRes[iHiRes], ulPwr);
    rgulRes[iHiRes] = sdlTmp.u.Lo;
    iCur = iHiRes - 1;

    if (iCur >= 0) {
      // If first quotient was 0, update iHiRes.
      //
      if (sdlTmp.u.Lo == 0)
        iHiRes--;

      // Compute subsequent quotients.
      //
      do {
        sdlTmp.u.Lo = rgulRes[iCur];
        sdlTmp.int64 = DivMod64by32(sdlTmp.int64, ulPwr);
        rgulRes[iCur] = sdlTmp.u.Lo;
        iCur--;
      } while (iCur >= 0);

    }

    iNewScale -= POWER10_MAX;
    if (iNewScale > 0)
      continue; // scale some more

    // If we scaled enough, iHiRes would be 2 or less.  If not,
    // divide by 10 more.
    //
    if (iHiRes > 2) {
      iNewScale = 1;
      iScale--;
      continue; // scale by 10
    }

    // Round final result.  See if remainder >= 1/2 of divisor.
    // If remainder == 1/2 divisor, round up if odd or sticky bit set.
    //
    ulPwr >>= 1;  // power of 10 always even
    if ( ulPwr <= sdlTmp.u.Hi && (ulPwr < sdlTmp.u.Hi ||
        ((rgulRes[0] & 1) | ulSticky)) ) {
      iCur = -1;
      while (++rgulRes[++iCur] == 0);

      if (iCur > 2) {
        // The rounding caused us to carry beyond 96 bits. 
        // Scale by 10 more.
        //
        iHiRes = iCur;
        ulSticky = 0;  // no sticky bit
        sdlTmp.u.Hi = 0; // or remainder
        iNewScale = 1;
        iScale--;
        continue; // scale by 10
      }
    }

    // We may have scaled it more than we planned.  Make sure the scale 
    // factor hasn't gone negative, indicating overflow.
    // 
    if (iScale < 0)
      return -1;

    return iScale;
      } // for(;;)
    }
    return iScale;
}

FCIMPL3(void, COMDecimal::DoAddSubThrow, DECIMAL * pdecL, DECIMAL * pdecR, UINT8 bSign)
{
    FCALL_CONTRACT;

    ULONG     rgulNum[6];
    ULONG     ulPwr;
    int       iScale;
    int       iHiProd;
    int       iCur;
    SPLIT64   sdlTmp;
    DECIMAL   decRes;
    DECIMAL   decTmp;
    LPDECIMAL pdecTmp;
    LPDECIMAL pdecLOriginal;

    _ASSERTE(bSign == 0 || bSign == DECIMAL_NEG);

    pdecLOriginal = pdecL;

    bSign ^= (DECIMAL_SIGN(*pdecR) ^ DECIMAL_SIGN(*pdecL)) & DECIMAL_NEG;

    if (DECIMAL_SCALE(*pdecR) == DECIMAL_SCALE(*pdecL)) {
      // Scale factors are equal, no alignment necessary.
      //
      DECIMAL_SIGNSCALE(decRes) = DECIMAL_SIGNSCALE(*pdecL);

AlignedAdd:
      if (bSign) {
    // Signs differ - subtract
    //
    DECIMAL_LO64_SET(decRes, (DECIMAL_LO64_GET(*pdecL) - DECIMAL_LO64_GET(*pdecR)));
    DECIMAL_HI32(decRes) = DECIMAL_HI32(*pdecL) - DECIMAL_HI32(*pdecR);

    // Propagate carry
    //
    if (DECIMAL_LO64_GET(decRes) > DECIMAL_LO64_GET(*pdecL)) {
      DECIMAL_HI32(decRes)--;
      if (DECIMAL_HI32(decRes) >= DECIMAL_HI32(*pdecL))
        goto SignFlip;
    }
    else if (DECIMAL_HI32(decRes) > DECIMAL_HI32(*pdecL)) {
      // Got negative result.  Flip its sign.
      // 
SignFlip:
      DECIMAL_LO64_SET(decRes, -(LONGLONG)DECIMAL_LO64_GET(decRes));
      DECIMAL_HI32(decRes) = ~DECIMAL_HI32(decRes);
      if (DECIMAL_LO64_GET(decRes) == 0)
        DECIMAL_HI32(decRes)++;
      DECIMAL_SIGN(decRes) ^= DECIMAL_NEG;
    }

      }
      else {
    // Signs are the same - add
    //
    DECIMAL_LO64_SET(decRes, (DECIMAL_LO64_GET(*pdecL) + DECIMAL_LO64_GET(*pdecR)));
    DECIMAL_HI32(decRes) = DECIMAL_HI32(*pdecL) + DECIMAL_HI32(*pdecR);

    // Propagate carry
    //
    if (DECIMAL_LO64_GET(decRes) < DECIMAL_LO64_GET(*pdecL)) {
      DECIMAL_HI32(decRes)++;
      if (DECIMAL_HI32(decRes) <= DECIMAL_HI32(*pdecL))
        goto AlignedScale;
    }
    else if (DECIMAL_HI32(decRes) < DECIMAL_HI32(*pdecL)) {
AlignedScale:
      // The addition carried above 96 bits.  Divide the result by 10,
      // dropping the scale factor.
      // 
      if (DECIMAL_SCALE(decRes) == 0)
        FCThrowResVoid(kOverflowException, W("Overflow_Decimal")); // DISP_E_OVERFLOW
      DECIMAL_SCALE(decRes)--;

      sdlTmp.u.Lo = DECIMAL_HI32(decRes);
      sdlTmp.u.Hi = 1;
      sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10);
      DECIMAL_HI32(decRes) = sdlTmp.u.Lo;

      sdlTmp.u.Lo = DECIMAL_MID32(decRes);
      sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10);
      DECIMAL_MID32(decRes) = sdlTmp.u.Lo;

      sdlTmp.u.Lo = DECIMAL_LO32(decRes);
      sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10);
      DECIMAL_LO32(decRes) = sdlTmp.u.Lo;

      // See if we need to round up.
      //
      if (sdlTmp.u.Hi >= 5 && (sdlTmp.u.Hi > 5 || (DECIMAL_LO32(decRes) & 1))) {
            DECIMAL_LO64_SET(decRes, DECIMAL_LO64_GET(decRes)+1);
            if (DECIMAL_LO64_GET(decRes) == 0)
          DECIMAL_HI32(decRes)++;
      }
    }
      }
    }
    else {
      // Scale factors are not equal.  Assume that a larger scale
      // factor (more decimal places) is likely to mean that number
      // is smaller.  Start by guessing that the right operand has
      // the larger scale factor.  The result will have the larger
      // scale factor.
      //
      DECIMAL_SCALE(decRes) = DECIMAL_SCALE(*pdecR);  // scale factor of "smaller"
      DECIMAL_SIGN(decRes) = DECIMAL_SIGN(*pdecL);    // but sign of "larger"
      iScale = DECIMAL_SCALE(decRes)- DECIMAL_SCALE(*pdecL);

      if (iScale < 0) {
    // Guessed scale factor wrong. Swap operands.
    //
    iScale = -iScale;
    DECIMAL_SCALE(decRes) = DECIMAL_SCALE(*pdecL);
    DECIMAL_SIGN(decRes) ^= bSign;
    pdecTmp = pdecR;
    pdecR = pdecL;
    pdecL = pdecTmp;
      }

      // *pdecL will need to be multiplied by 10^iScale so
      // it will have the same scale as *pdecR.  We could be
      // extending it to up to 192 bits of precision.
      //
      if (iScale <= POWER10_MAX) {
    // Scaling won't make it larger than 4 ULONGs
    //
    ulPwr = rgulPower10[iScale];
    DECIMAL_LO64_SET(decTmp, UInt32x32To64(DECIMAL_LO32(*pdecL), ulPwr));
    sdlTmp.int64 = UInt32x32To64(DECIMAL_MID32(*pdecL), ulPwr);
    sdlTmp.int64 += DECIMAL_MID32(decTmp);
    DECIMAL_MID32(decTmp) = sdlTmp.u.Lo;
    DECIMAL_HI32(decTmp) = sdlTmp.u.Hi;
    sdlTmp.int64 = UInt32x32To64(DECIMAL_HI32(*pdecL), ulPwr);
    sdlTmp.int64 += DECIMAL_HI32(decTmp);
    if (sdlTmp.u.Hi == 0) {
      // Result fits in 96 bits.  Use standard aligned add.
      //
      DECIMAL_HI32(decTmp) = sdlTmp.u.Lo;
      pdecL = &decTmp;
      goto AlignedAdd;
    }
    rgulNum[0] = DECIMAL_LO32(decTmp);
    rgulNum[1] = DECIMAL_MID32(decTmp);
    rgulNum[2] = sdlTmp.u.Lo;
    rgulNum[3] = sdlTmp.u.Hi;
    iHiProd = 3;
      }
      else {
    // Have to scale by a bunch.  Move the number to a buffer
    // where it has room to grow as it's scaled.
    //
    rgulNum[0] = DECIMAL_LO32(*pdecL);
    rgulNum[1] = DECIMAL_MID32(*pdecL);
    rgulNum[2] = DECIMAL_HI32(*pdecL);
    iHiProd = 2;

    // Scan for zeros in the upper words.
    //
    if (rgulNum[2] == 0) {
      iHiProd = 1;
      if (rgulNum[1] == 0) {
        iHiProd = 0;
        if (rgulNum[0] == 0) {
          // Left arg is zero, return right.
          //
          DECIMAL_LO64_SET(decRes, DECIMAL_LO64_GET(*pdecR));
          DECIMAL_HI32(decRes) = DECIMAL_HI32(*pdecR);
          DECIMAL_SIGN(decRes) ^= bSign;
          goto RetDec;
        }
      }
    }

    // Scaling loop, up to 10^9 at a time.  iHiProd stays updated
    // with index of highest non-zero ULONG.
    //
    for (; iScale > 0; iScale -= POWER10_MAX) {
      if (iScale > POWER10_MAX)
        ulPwr = ulTenToNine;
      else
        ulPwr = rgulPower10[iScale];

      sdlTmp.u.Hi = 0;
      for (iCur = 0; iCur <= iHiProd; iCur++) {
        sdlTmp.int64 = UInt32x32To64(rgulNum[iCur], ulPwr) + sdlTmp.u.Hi;
        rgulNum[iCur] = sdlTmp.u.Lo;
      }

      if (sdlTmp.u.Hi != 0)
        // We're extending the result by another ULONG.
        rgulNum[++iHiProd] = sdlTmp.u.Hi;
    }
      }

      // Scaling complete, do the add.  Could be subtract if signs differ.
      //
      sdlTmp.u.Lo = rgulNum[0];
      sdlTmp.u.Hi = rgulNum[1];

      if (bSign) {
    // Signs differ, subtract.
    //
    DECIMAL_LO64_SET(decRes, (sdlTmp.int64 - DECIMAL_LO64_GET(*pdecR)));
    DECIMAL_HI32(decRes) = rgulNum[2] - DECIMAL_HI32(*pdecR);

    // Propagate carry
    //
    if (DECIMAL_LO64_GET(decRes) > sdlTmp.int64) {
      DECIMAL_HI32(decRes)--;
      if (DECIMAL_HI32(decRes) >= rgulNum[2])
        goto LongSub;
    }
    else if (DECIMAL_HI32(decRes) > rgulNum[2]) {
LongSub:
      // If rgulNum has more than 96 bits of precision, then we need to 
      // carry the subtraction into the higher bits.  If it doesn't, 
      // then we subtracted in the wrong order and have to flip the 
      // sign of the result.
      // 
      if (iHiProd <= 2)
        goto SignFlip;

      iCur = 3;
      while(rgulNum[iCur++]-- == 0);
      if (rgulNum[iHiProd] == 0)
        iHiProd--;
    }
      }
      else {
    // Signs the same, add.
    //
    DECIMAL_LO64_SET(decRes, (sdlTmp.int64 + DECIMAL_LO64_GET(*pdecR)));
    DECIMAL_HI32(decRes) = rgulNum[2] + DECIMAL_HI32(*pdecR);

    // Propagate carry
    //
    if (DECIMAL_LO64_GET(decRes) < sdlTmp.int64) {
      DECIMAL_HI32(decRes)++;
      if (DECIMAL_HI32(decRes) <= rgulNum[2])
        goto LongAdd;
    }
    else if (DECIMAL_HI32(decRes) < rgulNum[2]) {
LongAdd:
      // Had a carry above 96 bits.
      //
      iCur = 3;
      do {
        if (iHiProd < iCur) {
          rgulNum[iCur] = 1;
          iHiProd = iCur;
          break;
        }
      }while (++rgulNum[iCur++] == 0);
    }
      }

      if (iHiProd > 2) {
    rgulNum[0] = DECIMAL_LO32(decRes);
    rgulNum[1] = DECIMAL_MID32(decRes);
    rgulNum[2] = DECIMAL_HI32(decRes);
    DECIMAL_SCALE(decRes) = (BYTE)ScaleResult(rgulNum, iHiProd, DECIMAL_SCALE(decRes));
    if (DECIMAL_SCALE(decRes) == (BYTE)-1)
      FCThrowResVoid(kOverflowException, W("Overflow_Decimal")); // DISP_E_OVERFLOW

    DECIMAL_LO32(decRes) = rgulNum[0];
    DECIMAL_MID32(decRes) = rgulNum[1];
    DECIMAL_HI32(decRes) = rgulNum[2];
      }
    }

RetDec:
    pdecL = pdecLOriginal;
    COPYDEC(*pdecL, decRes)
    pdecL->wReserved = 0;
    FC_GC_POLL();
}
FCIMPLEND

FCIMPL4(void, COMDecimal::DoAddSub, DECIMAL * pdecL, DECIMAL * pdecR, UINT8 bSign, CLR_BOOL * overflowed)
{
    FCALL_CONTRACT;

    ULONG     rgulNum[6];
    ULONG     ulPwr;
    int       iScale;
    int       iHiProd;
    int       iCur;
    SPLIT64   sdlTmp;
    DECIMAL   decRes;
    DECIMAL   decTmp;
    LPDECIMAL pdecTmp;
    LPDECIMAL pdecLOriginal;

    _ASSERTE(bSign == 0 || bSign == DECIMAL_NEG);

    pdecLOriginal = pdecL;

    bSign ^= (DECIMAL_SIGN(*pdecR) ^ DECIMAL_SIGN(*pdecL)) & DECIMAL_NEG;

    if (DECIMAL_SCALE(*pdecR) == DECIMAL_SCALE(*pdecL)) {
      // Scale factors are equal, no alignment necessary.
      //
      DECIMAL_SIGNSCALE(decRes) = DECIMAL_SIGNSCALE(*pdecL);

AlignedAdd:
      if (bSign) {
    // Signs differ - subtract
    //
    DECIMAL_LO64_SET(decRes, (DECIMAL_LO64_GET(*pdecL) - DECIMAL_LO64_GET(*pdecR)));
    DECIMAL_HI32(decRes) = DECIMAL_HI32(*pdecL) - DECIMAL_HI32(*pdecR);

    // Propagate carry
    //
    if (DECIMAL_LO64_GET(decRes) > DECIMAL_LO64_GET(*pdecL)) {
      DECIMAL_HI32(decRes)--;
      if (DECIMAL_HI32(decRes) >= DECIMAL_HI32(*pdecL))
        goto SignFlip;
    }
    else if (DECIMAL_HI32(decRes) > DECIMAL_HI32(*pdecL)) {
      // Got negative result.  Flip its sign.
      // 
SignFlip:
      DECIMAL_LO64_SET(decRes, -(LONGLONG)DECIMAL_LO64_GET(decRes));
      DECIMAL_HI32(decRes) = ~DECIMAL_HI32(decRes);
      if (DECIMAL_LO64_GET(decRes) == 0)
        DECIMAL_HI32(decRes)++;
      DECIMAL_SIGN(decRes) ^= DECIMAL_NEG;
    }

      }
      else {
    // Signs are the same - add
    //
    DECIMAL_LO64_SET(decRes, (DECIMAL_LO64_GET(*pdecL) + DECIMAL_LO64_GET(*pdecR)));
    DECIMAL_HI32(decRes) = DECIMAL_HI32(*pdecL) + DECIMAL_HI32(*pdecR);

    // Propagate carry
    //
    if (DECIMAL_LO64_GET(decRes) < DECIMAL_LO64_GET(*pdecL)) {
      DECIMAL_HI32(decRes)++;
      if (DECIMAL_HI32(decRes) <= DECIMAL_HI32(*pdecL))
        goto AlignedScale;
    }
    else if (DECIMAL_HI32(decRes) < DECIMAL_HI32(*pdecL)) {
AlignedScale:
      // The addition carried above 96 bits.  Divide the result by 10,
      // dropping the scale factor.
      // 
      if (DECIMAL_SCALE(decRes) == 0) {
          *overflowed = true;
          FC_GC_POLL();
          return;
      }
      DECIMAL_SCALE(decRes)--;

      sdlTmp.u.Lo = DECIMAL_HI32(decRes);
      sdlTmp.u.Hi = 1;
      sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10);
      DECIMAL_HI32(decRes) = sdlTmp.u.Lo;

      sdlTmp.u.Lo = DECIMAL_MID32(decRes);
      sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10);
      DECIMAL_MID32(decRes) = sdlTmp.u.Lo;

      sdlTmp.u.Lo = DECIMAL_LO32(decRes);
      sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10);
      DECIMAL_LO32(decRes) = sdlTmp.u.Lo;

      // See if we need to round up.
      //
      if (sdlTmp.u.Hi >= 5 && (sdlTmp.u.Hi > 5 || (DECIMAL_LO32(decRes) & 1))) {
            DECIMAL_LO64_SET(decRes, DECIMAL_LO64_GET(decRes)+1);
            if (DECIMAL_LO64_GET(decRes) == 0)
          DECIMAL_HI32(decRes)++;
      }
    }
      }
    }
    else {
      // Scale factors are not equal.  Assume that a larger scale
      // factor (more decimal places) is likely to mean that number
      // is smaller.  Start by guessing that the right operand has
      // the larger scale factor.  The result will have the larger
      // scale factor.
      //
      DECIMAL_SCALE(decRes) = DECIMAL_SCALE(*pdecR);  // scale factor of "smaller"
      DECIMAL_SIGN(decRes) = DECIMAL_SIGN(*pdecL);    // but sign of "larger"
      iScale = DECIMAL_SCALE(decRes)- DECIMAL_SCALE(*pdecL);

      if (iScale < 0) {
    // Guessed scale factor wrong. Swap operands.
    //
    iScale = -iScale;
    DECIMAL_SCALE(decRes) = DECIMAL_SCALE(*pdecL);
    DECIMAL_SIGN(decRes) ^= bSign;
    pdecTmp = pdecR;
    pdecR = pdecL;
    pdecL = pdecTmp;
      }

      // *pdecL will need to be multiplied by 10^iScale so
      // it will have the same scale as *pdecR.  We could be
      // extending it to up to 192 bits of precision.
      //
      if (iScale <= POWER10_MAX) {
    // Scaling won't make it larger than 4 ULONGs
    //
    ulPwr = rgulPower10[iScale];
    DECIMAL_LO64_SET(decTmp, UInt32x32To64(DECIMAL_LO32(*pdecL), ulPwr));
    sdlTmp.int64 = UInt32x32To64(DECIMAL_MID32(*pdecL), ulPwr);
    sdlTmp.int64 += DECIMAL_MID32(decTmp);
    DECIMAL_MID32(decTmp) = sdlTmp.u.Lo;
    DECIMAL_HI32(decTmp) = sdlTmp.u.Hi;
    sdlTmp.int64 = UInt32x32To64(DECIMAL_HI32(*pdecL), ulPwr);
    sdlTmp.int64 += DECIMAL_HI32(decTmp);
    if (sdlTmp.u.Hi == 0) {
      // Result fits in 96 bits.  Use standard aligned add.
      //
      DECIMAL_HI32(decTmp) = sdlTmp.u.Lo;
      pdecL = &decTmp;
      goto AlignedAdd;
    }
    rgulNum[0] = DECIMAL_LO32(decTmp);
    rgulNum[1] = DECIMAL_MID32(decTmp);
    rgulNum[2] = sdlTmp.u.Lo;
    rgulNum[3] = sdlTmp.u.Hi;
    iHiProd = 3;
      }
      else {
    // Have to scale by a bunch.  Move the number to a buffer
    // where it has room to grow as it's scaled.
    //
    rgulNum[0] = DECIMAL_LO32(*pdecL);
    rgulNum[1] = DECIMAL_MID32(*pdecL);
    rgulNum[2] = DECIMAL_HI32(*pdecL);
    iHiProd = 2;

    // Scan for zeros in the upper words.
    //
    if (rgulNum[2] == 0) {
      iHiProd = 1;
      if (rgulNum[1] == 0) {
        iHiProd = 0;
        if (rgulNum[0] == 0) {
          // Left arg is zero, return right.
          //
          DECIMAL_LO64_SET(decRes, DECIMAL_LO64_GET(*pdecR));
          DECIMAL_HI32(decRes) = DECIMAL_HI32(*pdecR);
          DECIMAL_SIGN(decRes) ^= bSign;
          goto RetDec;
        }
      }
    }

    // Scaling loop, up to 10^9 at a time.  iHiProd stays updated
    // with index of highest non-zero ULONG.
    //
    for (; iScale > 0; iScale -= POWER10_MAX) {
      if (iScale > POWER10_MAX)
        ulPwr = ulTenToNine;
      else
        ulPwr = rgulPower10[iScale];

      sdlTmp.u.Hi = 0;
      for (iCur = 0; iCur <= iHiProd; iCur++) {
        sdlTmp.int64 = UInt32x32To64(rgulNum[iCur], ulPwr) + sdlTmp.u.Hi;
        rgulNum[iCur] = sdlTmp.u.Lo;
      }

      if (sdlTmp.u.Hi != 0)
        // We're extending the result by another ULONG.
        rgulNum[++iHiProd] = sdlTmp.u.Hi;
    }
      }

      // Scaling complete, do the add.  Could be subtract if signs differ.
      //
      sdlTmp.u.Lo = rgulNum[0];
      sdlTmp.u.Hi = rgulNum[1];

      if (bSign) {
    // Signs differ, subtract.
    //
    DECIMAL_LO64_SET(decRes, (sdlTmp.int64 - DECIMAL_LO64_GET(*pdecR)));
    DECIMAL_HI32(decRes) = rgulNum[2] - DECIMAL_HI32(*pdecR);

    // Propagate carry
    //
    if (DECIMAL_LO64_GET(decRes) > sdlTmp.int64) {
      DECIMAL_HI32(decRes)--;
      if (DECIMAL_HI32(decRes) >= rgulNum[2])
        goto LongSub;
    }
    else if (DECIMAL_HI32(decRes) > rgulNum[2]) {
LongSub:
      // If rgulNum has more than 96 bits of precision, then we need to 
      // carry the subtraction into the higher bits.  If it doesn't, 
      // then we subtracted in the wrong order and have to flip the 
      // sign of the result.
      // 
      if (iHiProd <= 2)
        goto SignFlip;

      iCur = 3;
      while(rgulNum[iCur++]-- == 0);
      if (rgulNum[iHiProd] == 0)
        iHiProd--;
    }
      }
      else {
    // Signs the same, add.
    //
    DECIMAL_LO64_SET(decRes, (sdlTmp.int64 + DECIMAL_LO64_GET(*pdecR)));
    DECIMAL_HI32(decRes) = rgulNum[2] + DECIMAL_HI32(*pdecR);

    // Propagate carry
    //
    if (DECIMAL_LO64_GET(decRes) < sdlTmp.int64) {
      DECIMAL_HI32(decRes)++;
      if (DECIMAL_HI32(decRes) <= rgulNum[2])
        goto LongAdd;
    }
    else if (DECIMAL_HI32(decRes) < rgulNum[2]) {
LongAdd:
      // Had a carry above 96 bits.
      //
      iCur = 3;
      do {
        if (iHiProd < iCur) {
          rgulNum[iCur] = 1;
          iHiProd = iCur;
          break;
        }
      }while (++rgulNum[iCur++] == 0);
    }
      }

      if (iHiProd > 2) {
    rgulNum[0] = DECIMAL_LO32(decRes);
    rgulNum[1] = DECIMAL_MID32(decRes);
    rgulNum[2] = DECIMAL_HI32(decRes);
    DECIMAL_SCALE(decRes) = (BYTE)ScaleResult(rgulNum, iHiProd, DECIMAL_SCALE(decRes));
    if (DECIMAL_SCALE(decRes) == (BYTE)-1) {
          *overflowed = true;
          FC_GC_POLL();
          return;
    }

    DECIMAL_LO32(decRes) = rgulNum[0];
    DECIMAL_MID32(decRes) = rgulNum[1];
    DECIMAL_HI32(decRes) = rgulNum[2];
      }
    }

RetDec:
    pdecL = pdecLOriginal;
    COPYDEC(*pdecL, decRes)
    pdecL->wReserved = 0;
    FC_GC_POLL();
}
FCIMPLEND

