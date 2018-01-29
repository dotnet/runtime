// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: Number.cpp
//

//

#include "common.h"
#include "excep.h"
#include "number.h"
#include "string.h"
#include "decimal.h"
#include "bignum.h"
#include "grisu3.h"
#include "fp.h"
#include <stdlib.h>

typedef wchar_t wchar;

#define INT32_PRECISION 10
#define UINT32_PRECISION INT32_PRECISION
#define INT64_PRECISION 19
#define UINT64_PRECISION 20
#define FLOAT_PRECISION 7
#define DOUBLE_PRECISION 15
#define LARGE_BUFFER_SIZE 600
#define MIN_BUFFER_SIZE 105

#define SCALE_NAN 0x80000000
#define SCALE_INF 0x7FFFFFFF


static const char* const posCurrencyFormats[] = {
    "$#", "#$", "$ #", "# $"};

static const char* const negCurrencyFormats[] = {
    "($#)", "-$#", "$-#", "$#-",
    "(#$)", "-#$", "#-$", "#$-",
    "-# $", "-$ #", "# $-", "$ #-",
    "$ -#", "#- $", "($ #)", "(# $)"};

static const char* const posPercentFormats[] = {
    "# %", "#%", "%#", "% #"                // Last one is new in Whidbey
};

static const char* const negPercentFormats[] = {
    "-# %", "-#%", "-%#",
    "%-#", "%#-",                        // Last 9 are new in WHidbey
    "#-%", "#%-",
    "-% #", "# %-", "% #-",
    "% -#", "#- %"
};

static const char* const negNumberFormats[] = {
    "(#)", "-#", "- #", "#-", "# -",
};

static const char posNumberFormat[] = "#";

#if defined(_TARGET_X86_) && !defined(FEATURE_PAL)

extern "C" void _cdecl /*__stdcall*/ DoubleToNumber(double value, int precision, NUMBER* number);
extern "C" void _cdecl /*__stdcall*/ NumberToDouble(NUMBER* number, double* value);

#pragma warning(disable:4035)

wchar_t* COMNumber::Int32ToDecChars(__in wchar_t* p, unsigned int value, int digits)
{
    LIMITED_METHOD_CONTRACT

    _asm {
        mov     eax,value
        mov     ebx,10
        mov     ecx,digits
        mov     edi,p
        jmp     L2
L1:             xor     edx,edx
        div     ebx
        add     edx,'0'          //promote dl to edx to avoid partial register stall and LCP stall  
        sub     edi,2
        mov     [edi],dx
L2:             dec     ecx
        jge     L1
        or      eax,eax
        jne     L1
        mov     eax,edi
    }
}

#pragma warning(default:4035)

#else // _TARGET_X86_ && !FEATURE_PAL

void Dragon4( double value, int count, int* dec, int* sign, wchar_t* digits )
{
    // ========================================================================================================================================
    // This implementation is based on the paper: https://www.cs.indiana.edu/~dyb/pubs/FP-Printing-PLDI96.pdf
    // Besides the paper, some of the code and ideas are modified from http://www.ryanjuckett.com/programming/printing-floating-point-numbers/
    // You must read these two materials to fully understand the code.
    //
    // Note: we only support fixed format input.
    // ======================================================================================================================================== 
    //
    // Overview:
    //
    // The input double number can be represented as:
    // value = f * 2^e = r / s.
    //
    // f: the output mantissa. Note: f is not the 52 bits mantissa of the input double number. 
    // e: biased exponent.
    // r: numerator.
    // s: denominator.
    // k: value = d0.d1d2 . . . dn * 10^k

    // Step 1:
    // Extract meta data from the input double value.
    //
    // Refer to IEEE double precision floating point format.
    UINT64 f = 0;
    int e = 0;
    ExtractFractionAndBiasedExponent(value, &f, &e);

    UINT32 mantissaHighBitIdx = 0;
    if (((FPDOUBLE*)&value)->exp != 0)
    {
        mantissaHighBitIdx = 52;
    }
    else
    {
        mantissaHighBitIdx = BigNum::LogBase2(f);
    }

    // Step 2:
    // Estimate k. We'll verify it and fix any error later.
    //
    // This is an improvement of the estimation in the original paper.
    // Inspired by http://www.ryanjuckett.com/programming/printing-floating-point-numbers/
    //
    // LOG10V2 = 0.30102999566398119521373889472449
    // DRIFT_FACTOR = 0.69 = 1 - log10V2 - epsilon (a small number account for drift of floating point multiplication)
    int k = (int)(ceil(double((int)mantissaHighBitIdx + e) * LOG10V2 - DRIFT_FACTOR));

    // Step 3:
    // Store the input double value in BigNum format.
    //
    // To keep the precision, we represent the double value as r/s.
    // We have several optimization based on following table in the paper.
    //
    //     ----------------------------------------------------------------------------------------------------------
    //     |               e >= 0                   |                         e < 0                                 |
    //     ----------------------------------------------------------------------------------------------------------
    //     |  f != b^(P - 1)  |  f = b^(P - 1)      | e = min exp or f != b^(P - 1) | e > min exp and f = b^(P - 1) |
    // --------------------------------------------------------------------------------------------------------------
    // | r |  f * b^e * 2     |  f * b^(e + 1) * 2  |          f * 2                |            f * b * 2          |
    // --------------------------------------------------------------------------------------------------------------
    // | s |        2         |        b * 2        |          b^(-e) * 2           |            b^(-e + 1) * 2     |
    // --------------------------------------------------------------------------------------------------------------  
    //
    // Note, we do not need m+ and m- because we only support fixed format input here.
    // m+ and m- are used for free format input, which need to determine the exact range of values 
    // that would round to value when input so that we can generate the shortest correct digits.
    //
    // In our case, we just output digits until reaching the expected precision. 
    BigNum r(f);
    BigNum s;
    if (e >= 0)
    {
        // When f != b^(P - 1):
        // r = f * b^e * 2
        // s = 2
        // value = r / s = f * b^e * 2 / 2 = f * b^e / 1
        //
        // When f = b^(P - 1):
        // r = f * b^(e + 1) * 2
        // s = b * 2
        // value = r / s =  f * b^(e + 1) * 2 / b * 2 = f * b^e / 1
        //
        // Therefore, we can simply say that when e >= 0:
        // r = f * b^e = f * 2^e
        // s = 1

        r.ShiftLeft(e);
        s.SetUInt64(1);
    }
    else
    {
        // When e = min exp or f != b^(P - 1):
        // r = f * 2
        // s = b^(-e) * 2
        // value = r / s = f * 2 / b^(-e) * 2 = f / b^(-e)
        //
        // When e > min exp and f = b^(P - 1):
        // r = f * b * 2
        // s = b^(-e + 1) * 2
        // value = r / s =  f * b * 2 / b^(-e + 1) * 2 = f / b^(-e)
        //
        // Therefore, we can simply say that when e < 0:
        // r = f
        // s = b^(-e) = 2^(-e)

        BigNum::ShiftLeft(1, -e, s);
    }

    // According to the paper, we should use k >= 0 instead of k > 0 here.
    // However, if k = 0, both r and s won't be changed, we don't need to do any operation.
    //
    // Following are the Scheme code from the paper:
    // --------------------------------------------------------------------------------
    // (if (>= est 0)
    // (fixup r (* s (exptt B est)) m+ m− est B low-ok? high-ok? )
    // (let ([scale (exptt B (− est))])
    // (fixup (* r scale) s (* m+ scale) (* m− scale) est B low-ok? high-ok? ))))
    // --------------------------------------------------------------------------------
    //
    // If est is 0, (* s (exptt B est)) = s, (* r scale) = (* r (exptt B (− est)))) = r.
    //
    // So we just skip when k = 0.
    
    if (k > 0)
    {
        BigNum poweredValue;
        BigNum::Pow10(k, poweredValue);
        s.Multiply(poweredValue);
    }
    else if (k < 0)
    {
        BigNum poweredValue;
        BigNum::Pow10(-k, poweredValue);
        r.Multiply(poweredValue);
    }

    if (BigNum::Compare(r, s) >= 0)
    {
        // The estimation was incorrect. Fix the error by increasing 1.
        k += 1;
    }
    else
    {
        r.Multiply10();
    }

    *dec = k - 1;

    // This the prerequisite of calling BigNum::HeuristicDivide().
    BigNum::PrepareHeuristicDivide(&r, &s);

    // Step 4:
    // Calculate digits.
    //
    // Output digits until reaching the last but one precision or the numerator becomes zero.
    int digitsNum = 0;
    int currentDigit = 0;
    while (true)
    {
        currentDigit = BigNum::HeuristicDivide(&r, s);
        if (r.IsZero() || digitsNum + 1 == count)
        {
            break;
        }

        digits[digitsNum] = L'0' + currentDigit;
        ++digitsNum;

        r.Multiply10();
    }

    // Step 5:
    // Set the last digit.
    //
    // We round to the closest digit by comparing value with 0.5:
    //  compare( value, 0.5 )
    //  = compare( r / s, 0.5 )
    //  = compare( r, 0.5 * s)
    //  = compare(2 * r, s)
    //  = compare(r << 1, s)
    r.ShiftLeft(1);
    int compareResult = BigNum::Compare(r, s);
    bool isRoundDown = compareResult < 0;

    // We are in the middle, round towards the even digit (i.e. IEEE rouding rules)
    if (compareResult == 0)
    {
        isRoundDown = (currentDigit & 1) == 0;
    }

    if (isRoundDown)
    {
        digits[digitsNum] = L'0' + currentDigit;
        ++digitsNum;
    }
    else
    {
        wchar_t* pCurDigit = digits + digitsNum;

        // Rounding up for 9 is special.
        if (currentDigit == 9)
        {
            // find the first non-nine prior digit
            while (true)
            {
                // If we are at the first digit
                if (pCurDigit == digits)
                {
                    // Output 1 at the next highest exponent
                    *pCurDigit = L'1';
                    ++digitsNum;
                    *dec += 1;
                    break;
                }

                --pCurDigit;
                --digitsNum;
                if (*pCurDigit != L'9')
                {
                    // increment the digit
                    *pCurDigit += 1;
                    ++digitsNum;
                    break;
                }
            }
        }
        else
        {
            // It's simple if the digit is not 9.
            *pCurDigit = L'0' + currentDigit + 1;
            ++digitsNum;
        }
    }

    while (digitsNum < count)
    {
        digits[digitsNum] = L'0';
        ++digitsNum;
    }

    digits[count] = 0;

    ++*dec;
    *sign = ((FPDOUBLE*)&value)->sign;
}

// Convert a double value to a NUMBER struct.
//
// 1. You should ensure the input value is not infinity or NaN.
// 2. For 0.0, number->digits will be set as an empty string. i.e the value of the first bucket is 0.
void DoubleToNumberWorker( double value, int count, int* dec, int* sign, wchar_t* digits )
{
    _ASSERTE(dec != nullptr && sign != nullptr && digits != nullptr);

    // The caller of DoubleToNumberWorker should already checked the Infinity and NAN values.
    _ASSERTE(((FPDOUBLE*)&value)->exp != 0x7ff);

    // Shortcut for zero.
    if (value == 0.0)
    {
        *dec = 0;
        *sign = 0;

        // Instead of zeroing digits, we just make it as an empty string due to performance reason.
        *digits = 0;

        return;
    }

    // Try Grisu3 first.
    if (Grisu3::Run(value, count, dec, sign, digits))
    {
        return;
    }

    // Grisu3 failed, fall back to Dragon4.
    Dragon4(value, count, dec, sign, digits);
}

void DoubleToNumber(double value, int precision, NUMBER* number)
{
    WRAPPER_NO_CONTRACT
    _ASSERTE(number != NULL);

    number->precision = precision;
    if (((FPDOUBLE*)&value)->exp == 0x7FF) {
        number->scale = (((FPDOUBLE*)&value)->mantLo || ((FPDOUBLE*)&value)->mantHi) ? SCALE_NAN: SCALE_INF;
        number->sign = ((FPDOUBLE*)&value)->sign;
        number->digits[0] = 0;
    }
    else {
        DoubleToNumberWorker(value, precision, &number->scale, &number->sign, number->digits);
    }
}

/*===========================================================
    Portable NumberToDouble implementation
    --------------------------------------

    - does the conversion with the best possible precision.
    - does not use any float arithmetic so it is not sensitive
    to differences in precision of floating point calculations
    across platforms.

    The internal integer representation of the float number is
    UINT64 mantissa + INT exponent. The mantissa is kept normalized
    ie with the most significant one being 63-th bit of UINT64.
===========================================================*/

//
// get 32-bit integer from at most 9 digits
//
static unsigned DigitsToInt(__in_ecount(count) wchar* p, int count)
{
    LIMITED_METHOD_CONTRACT

    _ASSERTE(1 <= count && count <= 9);
    wchar* end = p + count;
    unsigned res = *p - '0';
    for ( p = p + 1; p < end; p++) {
        res = 10 * res + *p - '0';
    }
    return res;
}

//
// helper macro to multiply two 32-bit uints
//
#define Mul32x32To64(a, b) ((UINT64)((UINT32)(a)) * (UINT64)((UINT32)(b)))


//
// multiply two numbers in the internal integer representation
//
static UINT64 Mul64Lossy(UINT64 a, UINT64 b, INT* pexp)
{
    LIMITED_METHOD_CONTRACT

    // it's ok to losse some precision here - Mul64 will be called
    // at most twice during the conversion, so the error won't propagate
    // to any of the 53 significant bits of the result
    UINT64 val = Mul32x32To64(a >> 32, b >> 32) +
        (Mul32x32To64(a >> 32, b) >> 32) +
        (Mul32x32To64(a, b >> 32) >> 32);

    // normalize
    if ((val & I64(0x8000000000000000)) == 0) { val <<= 1; *pexp -= 1; }

    return val;
}

//
// precomputed tables with powers of 10. These allows us to do at most
// two Mul64 during the conversion. This is important not only
// for speed, but also for precision because of Mul64 computes with 1 bit error.
//

static const UINT64 rgval64Power10[] = {
// powers of 10
/*1*/ I64(0xa000000000000000),
/*2*/ I64(0xc800000000000000),
/*3*/ I64(0xfa00000000000000),
/*4*/ I64(0x9c40000000000000),
/*5*/ I64(0xc350000000000000),
/*6*/ I64(0xf424000000000000),
/*7*/ I64(0x9896800000000000),
/*8*/ I64(0xbebc200000000000),
/*9*/ I64(0xee6b280000000000),
/*10*/ I64(0x9502f90000000000),
/*11*/ I64(0xba43b74000000000),
/*12*/ I64(0xe8d4a51000000000),
/*13*/ I64(0x9184e72a00000000),
/*14*/ I64(0xb5e620f480000000),
/*15*/ I64(0xe35fa931a0000000),

// powers of 0.1
/*1*/ I64(0xcccccccccccccccd),
/*2*/ I64(0xa3d70a3d70a3d70b),
/*3*/ I64(0x83126e978d4fdf3c),
/*4*/ I64(0xd1b71758e219652e),
/*5*/ I64(0xa7c5ac471b478425),
/*6*/ I64(0x8637bd05af6c69b7),
/*7*/ I64(0xd6bf94d5e57a42be),
/*8*/ I64(0xabcc77118461ceff),
/*9*/ I64(0x89705f4136b4a599),
/*10*/ I64(0xdbe6fecebdedd5c2),
/*11*/ I64(0xafebff0bcb24ab02),
/*12*/ I64(0x8cbccc096f5088cf),
/*13*/ I64(0xe12e13424bb40e18),
/*14*/ I64(0xb424dc35095cd813),
/*15*/ I64(0x901d7cf73ab0acdc),
};

static const INT8 rgexp64Power10[] = {
// exponents for both powers of 10 and 0.1
/*1*/ 4,
/*2*/ 7,
/*3*/ 10,
/*4*/ 14,
/*5*/ 17,
/*6*/ 20,
/*7*/ 24,
/*8*/ 27,
/*9*/ 30,
/*10*/ 34,
/*11*/ 37,
/*12*/ 40,
/*13*/ 44,
/*14*/ 47,
/*15*/ 50,
};

static const UINT64 rgval64Power10By16[] = {
// powers of 10^16
/*1*/ I64(0x8e1bc9bf04000000),
/*2*/ I64(0x9dc5ada82b70b59e),
/*3*/ I64(0xaf298d050e4395d6),
/*4*/ I64(0xc2781f49ffcfa6d4),
/*5*/ I64(0xd7e77a8f87daf7fa),
/*6*/ I64(0xefb3ab16c59b14a0),
/*7*/ I64(0x850fadc09923329c),
/*8*/ I64(0x93ba47c980e98cde),
/*9*/ I64(0xa402b9c5a8d3a6e6),
/*10*/ I64(0xb616a12b7fe617a8),
/*11*/ I64(0xca28a291859bbf90),
/*12*/ I64(0xe070f78d39275566),
/*13*/ I64(0xf92e0c3537826140),
/*14*/ I64(0x8a5296ffe33cc92c),
/*15*/ I64(0x9991a6f3d6bf1762),
/*16*/ I64(0xaa7eebfb9df9de8a),
/*17*/ I64(0xbd49d14aa79dbc7e),
/*18*/ I64(0xd226fc195c6a2f88),
/*19*/ I64(0xe950df20247c83f8),
/*20*/ I64(0x81842f29f2cce373),
/*21*/ I64(0x8fcac257558ee4e2),

// powers of 0.1^16
/*1*/ I64(0xe69594bec44de160),
/*2*/ I64(0xcfb11ead453994c3),
/*3*/ I64(0xbb127c53b17ec165),
/*4*/ I64(0xa87fea27a539e9b3),
/*5*/ I64(0x97c560ba6b0919b5),
/*6*/ I64(0x88b402f7fd7553ab),
/*7*/ I64(0xf64335bcf065d3a0),
/*8*/ I64(0xddd0467c64bce4c4),
/*9*/ I64(0xc7caba6e7c5382ed),
/*10*/ I64(0xb3f4e093db73a0b7),
/*11*/ I64(0xa21727db38cb0053),
/*12*/ I64(0x91ff83775423cc29),
/*13*/ I64(0x8380dea93da4bc82),
/*14*/ I64(0xece53cec4a314f00),
/*15*/ I64(0xd5605fcdcf32e217),
/*16*/ I64(0xc0314325637a1978),
/*17*/ I64(0xad1c8eab5ee43ba2),
/*18*/ I64(0x9becce62836ac5b0),
/*19*/ I64(0x8c71dcd9ba0b495c),
/*20*/ I64(0xfd00b89747823938),
/*21*/ I64(0xe3e27a444d8d991a),
};

static const INT16 rgexp64Power10By16[] = {
// exponents for both powers of 10^16 and 0.1^16
/*1*/ 54,
/*2*/ 107,
/*3*/ 160,
/*4*/ 213,
/*5*/ 266,
/*6*/ 319,
/*7*/ 373,
/*8*/ 426,
/*9*/ 479,
/*10*/ 532,
/*11*/ 585,
/*12*/ 638,
/*13*/ 691,
/*14*/ 745,
/*15*/ 798,
/*16*/ 851,
/*17*/ 904,
/*18*/ 957,
/*19*/ 1010,
/*20*/ 1064,
/*21*/ 1117,
};

#ifdef _DEBUG
//
// slower high precision version of Mul64 for computation of the tables
//
static UINT64 Mul64Precise(UINT64 a, UINT64 b, INT* pexp)
{
    LIMITED_METHOD_CONTRACT

    UINT64 hilo =
        ((Mul32x32To64(a >> 32, b) >> 1) +
        (Mul32x32To64(a, b >> 32) >> 1) +
        (Mul32x32To64(a, b) >> 33)) >> 30;

    UINT64 val = Mul32x32To64(a >> 32, b >> 32) + (hilo >> 1) + (hilo & 1);

    // normalize
    if ((val & I64(0x8000000000000000)) == 0) { val <<= 1; *pexp -= 1; }

    return val;
}


//
// debug-only verification of the precomputed tables
//
static void CheckTable(UINT64 val, INT exp, LPCVOID table, int size, LPCSTR name, int tabletype)
{
    WRAPPER_NO_CONTRACT

    UINT64 multval = val;
    INT mulexp = exp;
    bool fBad = false;
    for (int i = 0; i < size; i++) {
        switch (tabletype) {
        case 1:
            if (((UINT64*)table)[i] != val) {
                if (!fBad) {
                    fprintf(stderr, "%s:\n", name);
                    fBad = true;
                }
                fprintf(stderr, "/*%d*/ I64(0x%I64x),\n", i+1, val);
            }
            break;
        case 2:
            if (((INT8*)table)[i] != exp) {
                if (!fBad) {
                    fprintf(stderr, "%s:\n", name);
                    fBad = true;
                }
                fprintf(stderr, "/*%d*/ %d,\n", i+1, exp);
            }
            break;
        case 3:
            if (((INT16*)table)[i] != exp) {
                if (!fBad) {
                    fprintf(stderr, "%s:\n", name);
                    fBad = true;
                }
                fprintf(stderr, "/*%d*/ %d,\n", i+1, exp);
            }
            break;
        default:
            _ASSERTE(false);
            break;
        }

        exp += mulexp;
        val = Mul64Precise(val, multval, &exp);
    }
    _ASSERTE(!fBad || !"NumberToDouble table not correct. Correct version dumped to stderr.");
}

void CheckTables()
{
    WRAPPER_NO_CONTRACT

    UINT64 val; INT exp;

    val = I64(0xa000000000000000); exp = 4; // 10
    CheckTable(val, exp, rgval64Power10, 15, "rgval64Power10", 1);
    CheckTable(val, exp, rgexp64Power10, 15, "rgexp64Power10", 2);

    val = I64(0x8e1bc9bf04000000); exp = 54; //10^16
    CheckTable(val, exp, rgval64Power10By16, 21, "rgval64Power10By16", 1);
    CheckTable(val, exp, rgexp64Power10By16, 21, "rgexp64Power10By16", 3);

    val = I64(0xCCCCCCCCCCCCCCCD); exp = -3; // 0.1
    CheckTable(val, exp, rgval64Power10+15, 15, "rgval64Power10 - inv", 1);

    val = I64(0xe69594bec44de160); exp = -53; // 0.1^16
    CheckTable(val, exp, rgval64Power10By16+21, 21, "rgval64Power10By16 - inv", 1);
}
#endif // _DEBUG

void NumberToDouble(NUMBER* number, double* value)
{
    WRAPPER_NO_CONTRACT

    UINT64 val;
    INT exp;
    wchar* src = number->digits;
    int remaining;
    int total;
    int count;
    int scale;
    int absscale;
    int index;

#ifdef _DEBUG
    static bool fCheckedTables = false;
    if (!fCheckedTables) {
        CheckTables();
        fCheckedTables = true;
    }
#endif // _DEBUG

    total = (int)wcslen(src);
    remaining = total;

    // skip the leading zeros
    while (*src == '0') {
        remaining--;
        src++;
    }

    if (remaining == 0) {
        *value = 0;
        goto done;
    }

    count = min(remaining, 9);
    remaining -= count;
    val = DigitsToInt(src, count);

    if (remaining > 0) {
        count = min(remaining, 9);
        remaining -= count;

        // get the denormalized power of 10
        UINT32 mult = (UINT32)(rgval64Power10[count-1] >> (64 - rgexp64Power10[count-1]));
        val = Mul32x32To64(val, mult) + DigitsToInt(src+9, count);
    }

    scale = number->scale - (total - remaining);
    absscale = abs(scale);
    if (absscale >= 22 * 16) {
        // overflow / underflow
        *(UINT64*)value = (scale > 0) ? I64(0x7FF0000000000000) : 0;
        goto done;
    }

    exp = 64;

    // normalize the mantissa
    if ((val & I64(0xFFFFFFFF00000000)) == 0) { val <<= 32; exp -= 32; }
    if ((val & I64(0xFFFF000000000000)) == 0) { val <<= 16; exp -= 16; }
    if ((val & I64(0xFF00000000000000)) == 0) { val <<= 8; exp -= 8; }
    if ((val & I64(0xF000000000000000)) == 0) { val <<= 4; exp -= 4; }
    if ((val & I64(0xC000000000000000)) == 0) { val <<= 2; exp -= 2; }
    if ((val & I64(0x8000000000000000)) == 0) { val <<= 1; exp -= 1; }

    index = absscale & 15;
    if (index) {
        INT multexp = rgexp64Power10[index-1];
        // the exponents are shared between the inverted and regular table
        exp += (scale < 0) ? (-multexp + 1) : multexp;

        UINT64 multval = rgval64Power10[index + ((scale < 0) ? 15 : 0) - 1];
        val = Mul64Lossy(val, multval, &exp);
    }

    index = absscale >> 4;
    if (index) {
        INT multexp = rgexp64Power10By16[index-1];
        // the exponents are shared between the inverted and regular table
        exp += (scale < 0) ? (-multexp + 1) : multexp;

        UINT64 multval = rgval64Power10By16[index + ((scale < 0) ? 21 : 0) - 1];
        val = Mul64Lossy(val, multval, &exp);
    }


    // round & scale down
    if ((UINT32)val & (1 << 10))
    {
        // IEEE round to even
        UINT64 tmp = val + ((1 << 10) - 1) + (((UINT32)val >> 11) & 1);
        if (tmp < val) {
            // overflow
            tmp = (tmp >> 1) | I64(0x8000000000000000);
            exp += 1;
        }
        val = tmp;
    }

    // return the exponent to a biased state
    exp += 0x3FE;

    // handle overflow, underflow, "Epsilon - 1/2 Epsilon", denormalized, and the normal case
    if (exp <= 0) {
        if (exp == -52 && (val  >= I64(0x8000000000000058))) {
            // round X where {Epsilon > X >= 2.470328229206232730000000E-324} up to Epsilon (instead of down to zero)
            val = I64(0x0000000000000001);
        }
        else if (exp <= -52) {
            // underflow
            val = 0;
        }
        else {
            // denormalized
            val >>= (-exp + 11 + 1);
        }
    }
    else if (exp >= 0x7FF) {
        // overflow
        val = I64(0x7FF0000000000000);
    }
    else {
        // normal postive exponent case
        val = ((UINT64)exp << 52) + ((val >> 11) & I64(0x000FFFFFFFFFFFFF));
    }

    *(UINT64*)value = val;

done:
    if (number->sign) *(UINT64*)value |= I64(0x8000000000000000);
}

wchar_t* COMNumber::Int32ToDecChars(__in wchar_t* p, unsigned int value, int digits)
{
    LIMITED_METHOD_CONTRACT
    _ASSERTE(p != NULL);

    while (--digits >= 0 || value != 0) {
        *--p = value % 10 + '0';
        value /= 10;
    }
    return p;
}
#endif // _TARGET_X86_ && !FEATURE_PAL

#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize("y", on)		// Small critical routines, don't put in EBP frame 
#endif

inline void AddStringRef(__in wchar** ppBuffer, STRINGREF strRef)
{
    WRAPPER_NO_CONTRACT
    _ASSERTE(ppBuffer != NULL && strRef != NULL);

    wchar* buffer = strRef->GetBuffer();
    _ASSERTE(buffer != NULL);
    DWORD length = strRef->GetStringLength();
    for (wchar* str = buffer; str < buffer + length; (*ppBuffer)++, str++)
    {
        **ppBuffer = *str;
    }
}

inline wchar* GetDigitsBuffer(NUMBER* number)
{
    return (number->allDigits != NULL) ? number->allDigits : number->digits;
}

#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize("", on)		// Go back to command line default optimizations
#endif


void RoundNumber(NUMBER* number, int pos)
{
    LIMITED_METHOD_CONTRACT
    _ASSERTE(number != NULL);

    wchar_t* digits = GetDigitsBuffer(number);
    int i = 0;
    while (i < pos && digits[i] != 0) i++;
    if (i == pos && digits[i] >= '5') {
        while (i > 0 && digits[i - 1] == '9') i--;
        if (i > 0) {
            digits[i - 1]++;
        }
        else {
            number->scale++;
            digits[0] = '1';
            i = 1;
        }
    }
    else {
        while (i > 0 && digits[i - 1] == '0') i--;
    }
    if (i == 0) {
        number->scale = 0;
        number->sign = 0;
    }
    digits[i] = 0;

}

#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize("y", on)		// Small critical routines, don't put in EBP frame 
#endif

wchar ParseFormatSpecifier(STRINGREF str, int* digits)
{
    WRAPPER_NO_CONTRACT
    _ASSERTE(digits != NULL);

    if (str != 0) {
        wchar* p = str->GetBuffer();
        _ASSERTE(p != NULL);
        wchar ch = *p;
        if (ch != 0) {
            if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z')) {
                p++;
                int n = -1;
                if (*p >= '0' && *p <= '9') {
                    n = *p++ - '0';
                    while (*p >= '0' && *p <= '9') {
                        n = n * 10 + *p++ - '0';
                        if (n >= 10) break;
                    }
                }
                if (*p == 0) {
                    *digits = n;
                    return ch;
                }
            }
            return 0;
        }
    }
    *digits = -1;
    return 'G';
}

wchar* FormatExponent(__in wchar* buffer, int value, wchar expChar,
    STRINGREF posSignStr, STRINGREF negSignStr, int minDigits)
{
    WRAPPER_NO_CONTRACT
    _ASSERTE(buffer != NULL);

    wchar digits[11];
    *buffer++ = expChar;
    if (value < 0) {
        _ASSERTE(negSignStr != NULL);
        AddStringRef(&buffer, negSignStr);
        value = -value;
    }
    else {
        if (posSignStr!= NULL) {
            AddStringRef(&buffer, posSignStr);
        }
    }
    wchar* p = COMNumber::Int32ToDecChars(digits + 10, value, minDigits);
    _ASSERTE(p != NULL);
    int i = (int) (digits + 10 - p);
    while (--i >= 0) *buffer++ = *p++;
    return buffer;
}

#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize("", on)		// Go back to command line default optimizations
#endif

wchar* FormatGeneral(__in_ecount(cchBuffer) wchar* buffer, SIZE_T cchBuffer, NUMBER* number, int nMinDigits, int nMaxDigits, wchar expChar,
    STRINGREF sNumberDecimal, STRINGREF sPositive, STRINGREF sNegative, STRINGREF sZero, BOOL bSuppressScientific = FALSE)
{
        WRAPPER_NO_CONTRACT
        _ASSERTE(number != NULL);
        _ASSERTE(buffer != NULL);

    int digPos = number->scale;
    int scientific = 0;
    if (!bSuppressScientific) { // Don't switch to scientific notation
        if (digPos > nMaxDigits || digPos < -3) {
            digPos = 1;
            scientific = 1;
        }
    }

    wchar* dig = GetDigitsBuffer(number);
    _ASSERT(dig != NULL);
    if (digPos > 0) {
        do {
            *buffer++ = *dig != 0? *dig++: '0';
        } while (--digPos > 0);
    }
    else {
        *buffer++ = '0';
    }
    if (*dig != 0 || digPos < 0) {
        AddStringRef(&buffer, sNumberDecimal);
        while (digPos < 0) {
            *buffer++ = '0';
            digPos++;
        }
        while (*dig != 0) {
            *buffer++ = *dig++;
        }
    }
    if (scientific) buffer = FormatExponent(buffer, number->scale - 1, expChar, sPositive, sNegative, 2);
    return buffer;
}

wchar* FormatScientific(__in_ecount(cchBuffer) wchar* buffer, SIZE_T cchBuffer, NUMBER* number, int nMinDigits, int nMaxDigits, wchar expChar,
    STRINGREF sNumberDecimal, STRINGREF sPositive, STRINGREF sNegative, STRINGREF sZero)
{
    WRAPPER_NO_CONTRACT
    _ASSERTE(number != NULL);
    _ASSERTE(buffer != NULL);

    wchar* dig = GetDigitsBuffer(number);
    _ASSERTE(dig != NULL);
    *buffer++ = *dig != 0? *dig++: '0';
    if (nMaxDigits != 1) // For E0 we would like to suppress the decimal point
        AddStringRef(&buffer, sNumberDecimal);
    while (--nMaxDigits > 0) *buffer++ = *dig != 0? *dig++: '0';
    int e = (GetDigitsBuffer(number))[0] == 0 ? 0 : number->scale - 1;
    buffer = FormatExponent(buffer, e, expChar, sPositive, sNegative, 3);
    _ASSERTE(buffer != NULL);
    return buffer;
}

wchar* FormatFixed(__in_ecount(cchBuffer) wchar* buffer, SIZE_T cchBuffer, NUMBER* number, int nMinDigits, int nMaxDigits,
    I4ARRAYREF groupDigitsRef, STRINGREF sDecimal, STRINGREF sGroup, STRINGREF sNegative,STRINGREF sZero)
{
    CONTRACTL {
        THROWS;
        INJECT_FAULT(COMPlusThrowOM());
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckPointer(number));
    } CONTRACTL_END;

    int digPos = number->scale;
    wchar* dig = GetDigitsBuffer(number);
    const I4* groupDigits = NULL;
    if (groupDigitsRef != NULL) {
        groupDigits = groupDigitsRef->GetDirectConstPointerToNonObjectElements();
    }

    if (digPos > 0) {
        if (groupDigits != NULL) {

            int groupSizeIndex = 0;     // index into the groupDigits array.
            int groupSizeCount = groupDigits[groupSizeIndex];   // the current total of group size.
            int groupSizeLen   = groupDigitsRef->GetNumComponents();    // the length of groupDigits array.
            int bufferSize     = digPos;                        // the length of the result buffer string.
            int groupSeparatorLen = sGroup->GetStringLength();  // the length of the group separator string.
            int groupSize = 0;                                      // the current group size.

            //
            // Find out the size of the string buffer for the result.
            //
            if (groupSizeLen != 0) // You can pass in 0 length arrays
            {
                while (digPos > groupSizeCount) {
                    groupSize = groupDigits[groupSizeIndex];
                    if (groupSize == 0) {
                        break;
                    }

                    bufferSize += groupSeparatorLen;
                    if (groupSizeIndex < groupSizeLen - 1) {
                        groupSizeIndex++;
                    }
                    groupSizeCount += groupDigits[groupSizeIndex];
                    if (groupSizeCount < 0 || bufferSize < 0) {
                        COMPlusThrow(kArgumentOutOfRangeException); // if we overflow
                    }
                }
                if (groupSizeCount == 0) // If you passed in an array with one entry as 0, groupSizeCount == 0
                    groupSize = 0;
                else
                    groupSize = groupDigits[0];
            }

            groupSizeIndex = 0;
            int digitCount = 0;
            int digStart;
            int digLength = (int)wcslen(dig);
            digStart = (digPos<digLength)?digPos:digLength;
            wchar* p = buffer + bufferSize - 1;
            for (int i = digPos - 1; i >=0; i--) {
                *(p--) = (i<digStart)?dig[i]:'0';

                if (groupSize > 0) {
                    digitCount++;
                    if (digitCount == groupSize && i != 0) {
                        for (int j = groupSeparatorLen - 1; j >=0; j--) {
                            *(p--) = sGroup->GetBuffer()[j];
                        }

                        if (groupSizeIndex < groupSizeLen - 1) {
                            groupSizeIndex++;
                            groupSize = groupDigits[groupSizeIndex];
                        }
                        digitCount = 0;
                    }
                }
            }
            if (p < buffer - 1) {
                // This indicates a buffer underflow since we write in backwards. 
                DoJITFailFast();
            }
            buffer += bufferSize;
            dig += digStart;
        } else {
            do {
                *buffer++ = *dig != 0? *dig++: '0';
            } while (--digPos > 0);
        }
    }
    else {
        *buffer++ = '0';
    }
    if (nMaxDigits > 0) {
        AddStringRef(&buffer, sDecimal);
        while (digPos < 0 && nMaxDigits > 0) {
            *buffer++ = '0';
            digPos++;
            nMaxDigits--;
        }
        while (nMaxDigits > 0) {
            *buffer++ = *dig != 0? *dig++: '0';
            nMaxDigits--;
        }
    }
    return buffer;
}

wchar* FormatNumber(__in_ecount(cchBuffer) wchar* buffer, SIZE_T cchBuffer, NUMBER* number, int nMinDigits, int nMaxDigits, int cNegativeNumberFormat, I4ARRAYREF cNumberGroup, STRINGREF sNumberDecimal, STRINGREF sNumberGroup, STRINGREF sNegative, STRINGREF sZero)
{
    CONTRACTL {
        MODE_COOPERATIVE;
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckPointer(number));
    } CONTRACTL_END;

    char ch;
    const char* fmt;
    fmt = number->sign?
          negNumberFormats[cNegativeNumberFormat]:
          posNumberFormat;

    while ((ch = *fmt++) != 0) {
        switch (ch) {
        case '#':
            buffer = FormatFixed(buffer, cchBuffer, number, nMinDigits,nMaxDigits,
                cNumberGroup,
                sNumberDecimal, sNumberGroup,sNegative,sZero);
            break;
        case '-':
            AddStringRef(&buffer, sNegative);
            break;
        default:
            *buffer++ = ch;
        }
    }
    return buffer;

}

wchar* FormatCurrency(__in_ecount(cchBuffer) wchar* buffer, SIZE_T cchBuffer, NUMBER* number, int nMinDigits,int nMaxDigits, int cNegCurrencyFormat, int cPosCurrencyFormat, I4ARRAYREF cCurrencyGroup, 
                      STRINGREF sCurrencyDecimal, STRINGREF sCurrencyGroup, STRINGREF sNegative, STRINGREF sCurrency,STRINGREF sZero)
{
    CONTRACTL {
        MODE_COOPERATIVE;
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckPointer(number));
    } CONTRACTL_END;

    char ch;
    const char* fmt;
    fmt = number->sign?
          negCurrencyFormats[cNegCurrencyFormat]:
          posCurrencyFormats[cPosCurrencyFormat];

    while ((ch = *fmt++) != 0) {
        switch (ch) {
        case '#':
            buffer = FormatFixed(buffer, cchBuffer, number, nMinDigits,nMaxDigits,
                cCurrencyGroup,
                sCurrencyDecimal, sCurrencyGroup,sNegative,sZero);
            break;
        case '-':
            AddStringRef(&buffer, sNegative);
            break;
        case '$':
            AddStringRef(&buffer, sCurrency);
            break;
        default:
            *buffer++ = ch;
        }
    }
    return buffer;
}

wchar* FormatPercent(__in_ecount(cchBuffer) wchar* buffer, SIZE_T cchBuffer, NUMBER* number, int nMinDigits, int nMaxDigits, int cNegativePercentFormat, int cPositivePercentFormat, I4ARRAYREF cPercentGroup, 
                     STRINGREF sPercentDecimal, STRINGREF sPercentGroup, STRINGREF sNegative, STRINGREF sPercent, STRINGREF sZero)
{
    CONTRACTL {
        MODE_COOPERATIVE;
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckPointer(number));
    } CONTRACTL_END;

    char ch;
    const char* fmt;
    fmt = number->sign?
          negPercentFormats[cNegativePercentFormat]:
          posPercentFormats[cPositivePercentFormat];

    while ((ch = *fmt++) != 0) {
        switch (ch) {
        case '#':
            buffer = FormatFixed(buffer, cchBuffer, number, nMinDigits,nMaxDigits,
                cPercentGroup,
                sPercentDecimal, sPercentGroup,sNegative,sZero);
            break;
        case '-':
            AddStringRef(&buffer, sNegative);
            break;
        case '%':
            AddStringRef(&buffer, sPercent);
            break;
        default:
            *buffer++ = ch;
        }
    }
    return buffer;
}

STRINGREF NumberToString(NUMBER* number, wchar format, int nMaxDigits, NUMFMTREF numfmt, BOOL bDecimal = FALSE )
{
    CONTRACTL {
        THROWS;
        INJECT_FAULT(COMPlusThrowOM());
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(number));
    } CONTRACTL_END;

    int nMinDigits=-1;
    STRINGREF sZero=NULL;

    // @TODO what if not sequential?

    // Do the worst case calculation
    /* US English - for Double.MinValue.ToString("C99"); we require 514 characters
    ----------
    2 paranthesis
    1 currency character
    308 characters
    103 group seperators
    1 decimal separator
    99 0's

        digPos + 99 + 6(slack) => digPos + 105
        C
        sNegative
        sCurrencyGroup
        sCurrencyDecimal
        sCurrency
        F
        sNegative
        sNumberDecimal
        N
        sNegative
        sNumberDecimal
        sNumberGroup
        E
        sNegative
        sPositive
        sNegative (for exponent)
        sPositive
        sNumberDecimal
        G
        sNegative
        sPositive
        sNegative (for exponent)
        sPositive
        sNumberDecimal
        P (+2 for some spaces)
        sNegative
        sPercentGroup
        sPercentDecimal
        sPercent
    */

    _ASSERTE(numfmt != NULL);
    UINT64 newBufferLen = MIN_BUFFER_SIZE;

    CQuickBytesSpecifySize<LARGE_BUFFER_SIZE * sizeof(WCHAR)> buf;

    wchar *buffer = NULL;
    wchar* dst = NULL;
    wchar ftype = format & 0xFFDF;
    int digCount = 0;

    switch (ftype) {
    case 'C':
        {
        nMinDigits = nMaxDigits >= 0 ? nMaxDigits : numfmt->cCurrencyDecimals;

        if (nMaxDigits< 0) 
            nMaxDigits = numfmt->cCurrencyDecimals;
        if (number->scale < 0)
            digCount = 0;
        else if (!ClrSafeInt<INT32>::addition(number->scale, nMaxDigits, digCount))
            COMPlusThrowOM();

        // It is critical to format with the same values that we use to calculate buffer size.
        int cNegCurrencyFormat = numfmt->cNegCurrencyFormat;
        int cPosCurrencyFormat = numfmt->cPosCurrencyFormat;
        I4ARRAYREF cCurrencyGroup = numfmt->cCurrencyGroup;
        STRINGREF sCurrencyDecimal = numfmt->sCurrencyDecimal;
        STRINGREF sCurrencyGroup = numfmt->sCurrencyGroup;
        STRINGREF sNegative = numfmt->sNegative;
        STRINGREF sCurrency = numfmt->sCurrency;        
        // Prefix: bogus warning 22011: newBufferLen+=digCount may be smaller than MIN_BUFFER_SIZE
        PREFIX_ASSUME(digCount >=0 && digCount <= INT32_MAX);
        newBufferLen += digCount;
        newBufferLen += sNegative->GetStringLength(); // For number and exponent
        if (!ClrSafeInt<UINT64>::addition((UINT64)sCurrencyGroup->GetStringLength() * digCount, newBufferLen, newBufferLen))
            COMPlusThrowOM();
        newBufferLen += sCurrencyDecimal->GetStringLength();
        newBufferLen += sCurrency->GetStringLength();

        _ASSERTE(newBufferLen >= MIN_BUFFER_SIZE);
        if (newBufferLen > INT32_MAX) {
            COMPlusThrowOM();
        }
        newBufferLen = newBufferLen * sizeof(WCHAR);
        dst = buffer = (WCHAR*)buf.AllocThrows(static_cast<SIZE_T>(newBufferLen));

            RoundNumber(number, number->scale + nMaxDigits); // Don't change this line to use digPos since digCount could have its sign changed.
            dst = FormatCurrency(dst, static_cast<SIZE_T>(newBufferLen/sizeof(WCHAR)), number, nMinDigits,nMaxDigits, cNegCurrencyFormat, cPosCurrencyFormat, cCurrencyGroup, sCurrencyDecimal, sCurrencyGroup, sNegative, sCurrency,sZero);
            
            break;
        }
    case 'F':
        {
        if (nMaxDigits< 0) 
            // This ensures that the PAL code pads out to the correct place even when we use the default precision
            nMaxDigits = nMinDigits = numfmt->cNumberDecimals;
        else
            nMinDigits=nMaxDigits;

        if (number->scale < 0)
            digCount = 0;
        else
            digCount = number->scale + nMaxDigits;


        // It is critical to format with the same values that we use to calculate buffer size.
        STRINGREF sNumberDecimal = numfmt->sNumberDecimal;
        STRINGREF sNegative = numfmt->sNegative;
           
        newBufferLen += digCount;
        newBufferLen += sNegative->GetStringLength(); // For number and exponent
        newBufferLen += sNumberDecimal->GetStringLength();

        _ASSERTE(newBufferLen >= MIN_BUFFER_SIZE);
        if (newBufferLen > INT32_MAX) {
            COMPlusThrowOM();
        }
        newBufferLen = newBufferLen * sizeof(WCHAR);
        dst = buffer = (WCHAR*)buf.AllocThrows(static_cast<SIZE_T>(newBufferLen));

        RoundNumber(number, number->scale + nMaxDigits);
        if (number->sign) {
            AddStringRef(&dst, sNegative);
        }
            dst = FormatFixed(dst, static_cast<SIZE_T>(newBufferLen/sizeof(WCHAR)-(dst-buffer)), number, nMinDigits,nMaxDigits,
                NULL,
                sNumberDecimal, NULL, sNegative, sZero);
        
        break;
        }
    case 'N':
        {
        if (nMaxDigits < 0) 
            // This ensures that the PAL code pads out to the correct place even when we use the default precision
            nMaxDigits = nMinDigits = numfmt->cNumberDecimals; // Since we are using digits in our calculation
        else
            nMinDigits=nMaxDigits;

        if (number->scale < 0)
            digCount = 0;
        else
            digCount = number->scale + nMaxDigits;

        // It is critical to format with the same values that we use to calculate buffer size.
        I4ARRAYREF cNumberGroup = numfmt->cNumberGroup;
        STRINGREF sNegative = numfmt->sNegative;
        STRINGREF sNumberDecimal = numfmt->sNumberDecimal;
        STRINGREF sNumberGroup = numfmt->sNumberGroup;
        int cNegativeNumberFormat = numfmt->cNegativeNumberFormat;
        newBufferLen += digCount;
        newBufferLen += sNegative->GetStringLength(); // For number and exponent
        if (!ClrSafeInt<UINT64>::addition((UINT64)sNumberGroup->GetStringLength() * digCount, newBufferLen, newBufferLen))
            COMPlusThrowOM();
        newBufferLen += sNumberDecimal->GetStringLength();

        _ASSERTE(newBufferLen >= MIN_BUFFER_SIZE);
        if (newBufferLen > INT32_MAX) {
            COMPlusThrowOM();
        }
        newBufferLen = newBufferLen * sizeof(WCHAR);
        dst = buffer = (WCHAR*)buf.AllocThrows(static_cast<SIZE_T>(newBufferLen));

        RoundNumber(number, number->scale + nMaxDigits);
        dst = FormatNumber(dst, static_cast<SIZE_T>(newBufferLen/sizeof(WCHAR)),number, nMinDigits, nMaxDigits, cNegativeNumberFormat, cNumberGroup, sNumberDecimal, sNumberGroup, sNegative, sZero);
        
        break;
        }
    case 'E':
        {
        // It is critical to format with the same values that we use to calculate buffer size.
        STRINGREF sNumberDecimal = numfmt->sNumberDecimal;
        STRINGREF sNegative = numfmt->sNegative;
        STRINGREF sPositive = numfmt->sPositive;

        if (nMaxDigits < 0) 
            // This ensures that the PAL code pads out to the correct place even when we use the default precision
            nMaxDigits = nMinDigits = 6;
        else
            nMinDigits=nMaxDigits;
        nMaxDigits++;

        newBufferLen += nMaxDigits;
        newBufferLen += (((INT64)sNegative->GetStringLength() + sPositive->GetStringLength()) *2); // For number and exponent
        newBufferLen += sNumberDecimal->GetStringLength();

        _ASSERTE(newBufferLen >= MIN_BUFFER_SIZE);
        if (newBufferLen > INT32_MAX) {
            COMPlusThrowOM();
        }
        newBufferLen = newBufferLen * sizeof(WCHAR);
        dst = buffer = (WCHAR*)buf.AllocThrows(static_cast<SIZE_T>(newBufferLen));

        RoundNumber(number, nMaxDigits);
        if (number->sign) {
            AddStringRef(&dst, sNegative);
        }
        dst = FormatScientific(dst, static_cast<SIZE_T>(newBufferLen * sizeof(WCHAR)-(dst-buffer)),number, nMinDigits,nMaxDigits, format, sNumberDecimal, sPositive, sNegative,sZero);

        break;
        }
    case 'G':
        {
            bool enableRounding = true;
            if (nMaxDigits < 1) {
                if (bDecimal && (nMaxDigits == -1)) { // Default to 29 digits precision only for G formatting without a precision specifier
                    // This ensures that the PAL code pads out to the correct place even when we use the default precision
                    nMaxDigits = nMinDigits = DECIMAL_PRECISION;
                    enableRounding = false;  // Turn off rounding for ECMA compliance to output trailing 0's after decimal as significant
                }
                else {
                    // This ensures that the PAL code pads out to the correct place even when we use the default precision
                    nMaxDigits = nMinDigits = number->precision;
                }
            }
            else
                nMinDigits=nMaxDigits;

        // It is critical to format with the same values that we use to calculate buffer size.
        STRINGREF sNumberDecimal = numfmt->sNumberDecimal;
        STRINGREF sNegative = numfmt->sNegative;
        STRINGREF sPositive = numfmt->sPositive;
        newBufferLen += nMaxDigits;
        newBufferLen += (((INT64)sNegative->GetStringLength() + sPositive->GetStringLength()) *2); // For number and exponent
        newBufferLen += sNumberDecimal->GetStringLength();

        _ASSERTE(newBufferLen >= MIN_BUFFER_SIZE);
        if (newBufferLen > INT32_MAX) {
            COMPlusThrowOM();
        }
        newBufferLen = newBufferLen * sizeof(WCHAR);
        dst = buffer = (WCHAR*)buf.AllocThrows(static_cast<SIZE_T>(newBufferLen));

            if (enableRounding) // Don't round for G formatting without precision
                RoundNumber(number, nMaxDigits); // This also fixes up the minus zero case
            else {
                if (bDecimal && ((GetDigitsBuffer(number))[0] == 0)) { // Minus zero should be formatted as 0
                    number->sign = 0;
                }
            }
            if (number->sign) {
                AddStringRef(&dst, sNegative);
            }


        dst = FormatGeneral(dst, static_cast<SIZE_T>(newBufferLen/sizeof(WCHAR)), number, nMinDigits,nMaxDigits, format - ('G' - 'E'), sNumberDecimal, sPositive, sNegative, sZero, !enableRounding);
        
        }
        break;
    case 'P':
        {
        if (nMaxDigits< 0) 
            // This ensures that the PAL code pads out to the correct place even when we use the default precision
            nMaxDigits = nMinDigits = numfmt->cPercentDecimals;
        else
            nMinDigits=nMaxDigits;
        number->scale += 2;

        if (number->scale < 0)
            digCount = 0;
        else
            digCount = number->scale + nMaxDigits;



        // It is critical to format with the same values that we use to calculate buffer size.
        int cNegativePercentFormat = numfmt->cNegativePercentFormat;
        int cPositivePercentFormat = numfmt->cPositivePercentFormat;
        I4ARRAYREF cPercentGroup = numfmt->cPercentGroup;
        STRINGREF sPercentDecimal = numfmt->sPercentDecimal;
        STRINGREF sPercentGroup = numfmt->sPercentGroup;
        STRINGREF sNegative = numfmt->sNegative;
        STRINGREF sPercent = numfmt->sPercent;

        newBufferLen += digCount;
        newBufferLen += sNegative->GetStringLength(); // For number and exponent
        if (!ClrSafeInt<UINT64>::addition((UINT64)sPercentGroup->GetStringLength() * digCount, newBufferLen, newBufferLen))
            COMPlusThrowOM();
        newBufferLen += sPercentDecimal->GetStringLength();
        newBufferLen += sPercent->GetStringLength();
    
        _ASSERTE(newBufferLen >= MIN_BUFFER_SIZE);
        if (newBufferLen > INT32_MAX) {
            COMPlusThrowOM();
        }
        newBufferLen = newBufferLen * sizeof(WCHAR);
        dst = buffer = (WCHAR*)buf.AllocThrows(static_cast<SIZE_T>(newBufferLen));
    
        RoundNumber(number, number->scale + nMaxDigits);
        dst = FormatPercent(dst, static_cast<SIZE_T>(newBufferLen/sizeof(WCHAR)),number, nMinDigits,nMaxDigits, cNegativePercentFormat, cPositivePercentFormat, cPercentGroup, sPercentDecimal, sPercentGroup, sNegative, sPercent, sZero);
        
        break;
        }
    default:
        COMPlusThrow(kFormatException, W("Argument_BadFormatSpecifier"));
    }
 // check for overflow of the preallocated buffer
// Review signed/unsigned mismatch in '<=' comparison.
#pragma warning(push)
#pragma warning(disable:4018)
    if (!((dst - buffer >= 0) && (dst - buffer) <= (newBufferLen / sizeof(WCHAR) ))) {
#pragma warning(pop)
        DoJITFailFast();
    }

    return StringObject::NewString(buffer, (int) (dst - buffer));
}

LPCWSTR FindSection(LPCWSTR format, int section)
{
    LIMITED_METHOD_CONTRACT
    _ASSERTE(format != NULL);

    LPCWSTR src;
    wchar ch;
    if (section == 0) return format;
    src = format;
    for (;;) {
        switch (ch = *src++) {
        case '\'':
        case '"':
            while (*src != 0 && *src++ != ch);
            break;
        case '\\':
            if (*src != 0) src++;
            break;
        case ';':
            if (--section != 0) break;
            if (*src != 0 && *src != ';') return src;
        case 0:
            return format;
        }
    }
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
STRINGREF NumberToStringFormat(NUMBER* number, STRINGREF str, NUMFMTREF numfmt)
{
    CONTRACTL {
        THROWS;
        INJECT_FAULT(COMPlusThrowOM());
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    int digitCount;
    int decimalPos;
    int firstDigit;
    int lastDigit;
    int digPos;
    int scientific;
    int percent;
    int permille;
    int thousandPos;
    int thousandCount = 0;
    int thousandSeps;
    int scaleAdjust;
    int adjust;
    wchar* format=NULL;
    LPCWSTR section=NULL;
    LPCWSTR src=NULL;
    wchar* dst=NULL;
    wchar* dig=NULL;
    wchar ch;
    wchar* buffer=NULL;
    CQuickBytes buf;

    _ASSERTE(str != NULL);
    _ASSERTE(numfmt != NULL);
    
    STRINGREF sNegative = numfmt->sNegative;
    STRINGREF sPositive = numfmt->sPositive;
    STRINGREF sNumberDecimal = numfmt->sNumberDecimal;
    STRINGREF sPercent = numfmt->sPercent;
    STRINGREF sPerMille = numfmt->sPerMille;
    STRINGREF sNumberGroup = numfmt->sNumberGroup;
    I4ARRAYREF cNumberGroup = numfmt->cNumberGroup;    

    format = str->GetBuffer();

    section = FindSection(format, (GetDigitsBuffer(number))[0] == 0 ? 2 : number->sign ? 1 : 0);

ParseSection:
    digitCount = 0;
    decimalPos = -1;
    firstDigit = 0x7FFFFFFF;
    lastDigit = 0;
    scientific = 0;
    percent = 0;
    permille = 0;
    thousandPos = -1;
    thousandSeps = 0;
    scaleAdjust = 0;
    src = section;
    _ASSERTE(src != NULL);
    while ((ch = *src++) != 0 && ch != ';') {
        switch (ch) {
        case '#':
            digitCount++;
            break;
        case '0':
            if (firstDigit == 0x7FFFFFFF) firstDigit = digitCount;
            digitCount++;
            lastDigit = digitCount;
            break;
        case '.':
            if (decimalPos < 0) {
                decimalPos = digitCount;
            }
            break;
        case ',':
            if (digitCount > 0 && decimalPos < 0) {
                if (thousandPos >= 0) {
                    if (thousandPos == digitCount) {
                        thousandCount++;
                        break;
                    }
                    thousandSeps = 1;
                }
                thousandPos = digitCount;
                thousandCount = 1;
            }
            break;
        case '%':
            percent++;
            scaleAdjust += 2;
            break;
        case 0x2030:
            permille++;
            scaleAdjust += 3;
            break;
        case '\'':
        case '"':
            while (*src != 0 && *src++ != ch);
            break;
        case '\\':
            if (*src != 0) src++;
            break;
        case 'E':
        case 'e':
            if (*src=='0' || ((*src == '+' || *src == '-') && src[1] == '0')) {
                while (*++src == '0');
                scientific = 1;
            }
            break;
        }
    }

    if (decimalPos < 0) decimalPos = digitCount;
    if (thousandPos >= 0) {
        if (thousandPos == decimalPos) {
            scaleAdjust -= thousandCount * 3;
        }
        else {
            thousandSeps = 1;
        }
    }
    if ((GetDigitsBuffer(number))[0] != 0) {
        number->scale += scaleAdjust;
        int pos = scientific? digitCount: number->scale + digitCount - decimalPos;
        RoundNumber(number, pos);
        if ((GetDigitsBuffer(number))[0] == 0) {
            src = FindSection(format, 2);
            if (src != section) {
                section = src;
                goto ParseSection;
            }
        }
    } else {
        number->sign = 0; // We need to format -0 without the sign set.
        number->scale = 0; // Decimals with scale ('0.00') should be rounded.
    }

    firstDigit = firstDigit < decimalPos? decimalPos - firstDigit: 0;
    lastDigit = lastDigit > decimalPos? decimalPos - lastDigit: 0;
    if (scientific) {
        digPos = decimalPos;
        adjust = 0;
    }
    else {
        digPos = number->scale > decimalPos? number->scale: decimalPos;
        adjust = number->scale - decimalPos;
    }
    src = section;
    dig = GetDigitsBuffer(number);

    // Find maximum number of characters that the destination string can grow by
    // in the following while loop.  Use this to avoid buffer overflows.
    // Longest strings are potentially +/- signs with 10 digit exponents,
    // or decimal numbers, or the while loops copying from a quote or a \ onwards.
    // Check for positive and negative
    UINT64 maxStrIncLen = 0; // We need this to be UINT64 since the percent computation could go beyond a UINT.
    if (number->sign) {
        maxStrIncLen = sNegative->GetStringLength();
    }
    else {
        maxStrIncLen = sPositive->GetStringLength();
    }

    // Add for any big decimal seperator
    maxStrIncLen += sNumberDecimal->GetStringLength();

    // Add for scientific
    if (scientific) {
        int inc1 = sPositive->GetStringLength();
        int inc2 = sNegative->GetStringLength();
        maxStrIncLen +=(inc1>inc2)?inc1:inc2;
    }

    // Add for percent separator
    if (percent) {
        maxStrIncLen += ((INT64)sPercent->GetStringLength()) * percent;
    }

    // Add for permilli separator
    if (permille) {
        maxStrIncLen += ((INT64)sPerMille->GetStringLength()) * permille;
    }

    //adjust can be negative, so we make this an int instead of an unsigned int.
    // adjust represents the number of characters over the formatting eg. format string is "0000" and you are trying to
    // format 100000 (6 digits). Means adjust will be 2. On the other hand if you are trying to format 10 adjust will be
    // -2 and we'll need to fixup these digits with 0 padding if we have 0 formatting as in this example.
    INT64 adjustLen=(adjust>0)?adjust:0; // We need to add space for these extra characters anyway.
    CQuickBytes thousands;
    INT32 bufferLen2 = 125;
    INT32 *thousandsSepPos = NULL;
    INT32 thousandsSepCtr = -1;

    if (thousandSeps) { // Fixup possible buffer overrun problems
        // We need to precompute this outside the number formatting loop
        if(cNumberGroup->GetNumComponents() == 0) {
            thousandSeps = 0; // Nothing to add
        }
        else {
            thousandsSepPos = (INT32 *)thousands.AllocThrows(bufferLen2 * sizeof(INT32));
            // We need this array to figure out where to insert the thousands separator. We would have to traverse the string
            // backwards. PIC formatting always traverses forwards. These indices are precomputed to tell us where to insert
            // the thousands separator so we can get away with traversing forwards. Note we only have to compute up to digPos.
            // The max is not bound since you can have formatting strings of the form "000,000..", and this
            // should handle that case too.

            const I4* groupDigits = cNumberGroup->GetDirectConstPointerToNonObjectElements();
            _ASSERTE(groupDigits != NULL);

            int groupSizeIndex = 0;     // index into the groupDigits array.
            INT64 groupTotalSizeCount = 0;
            int groupSizeLen   = cNumberGroup->GetNumComponents();    // the length of groupDigits array.
            if (groupSizeLen != 0)
                groupTotalSizeCount = groupDigits[groupSizeIndex];   // the current running total of group size.
            int groupSize = static_cast<INT32>(groupTotalSizeCount); // safe cast as long as groupDigits remains I4

            int totalDigits = digPos + ((adjust < 0)?adjust:0); // actual number of digits in o/p
            int numDigits = (firstDigit > totalDigits) ? firstDigit : totalDigits;
            while (numDigits > groupTotalSizeCount) {
                if (groupSize == 0)
                    break;
                thousandsSepPos[++thousandsSepCtr] = static_cast<INT32>(groupTotalSizeCount);
                if (groupSizeIndex < groupSizeLen - 1) {
                    groupSizeIndex++;
                    groupSize = groupDigits[groupSizeIndex];
                }
                groupTotalSizeCount += groupSize;
                if (bufferLen2 - thousandsSepCtr < 10) { // Slack of 10
                    bufferLen2 *= 2;
                    thousands.ReSizeThrows(bufferLen2*sizeof(INT32)); // memcopied by CQuickBytes automatically
                    thousandsSepPos = (INT32 *)thousands.Ptr();
                }
            }

            // We already have computed the number of separators above. Simply add space for them.
            adjustLen += ( (thousandsSepCtr + 1) * ((INT64)sNumberGroup->GetStringLength()));
        }
    }

    maxStrIncLen += adjustLen;

    // Allocate temp buffer - gotta deal with Schertz' 500 MB strings.
    // Some computations like when you specify Int32.MaxValue-2 %'s and each percent is setup to be Int32.MaxValue in length
    // will generate a result that will be largest than an unsigned int can hold. This is to protect against overflow.
    UINT64 tempLen = str->GetStringLength() + maxStrIncLen + 10;  // Include a healthy amount of temp space.
    if (tempLen > 0x7FFFFFFF)
        COMPlusThrowOM(); // if we overflow

    unsigned int bufferLen = (UINT)tempLen;
    if (bufferLen < 250) // Stay under 512 bytes
        bufferLen = 250; // This is to prevent unnecessary calls to resize
    buffer = (wchar *) buf.AllocThrows(bufferLen* sizeof(WCHAR));
    dst = buffer;


    if (number->sign && section == format) {
        AddStringRef(&dst, sNegative);
    }

    BOOL decimalWritten = FALSE;

    while ((ch = *src++) != 0 && ch != ';') {
        // Make sure temp buffer is big enough, else resize it.
        if (bufferLen - (unsigned int)(dst-buffer) < 10) {
            int offset = static_cast<INT32>(dst - buffer);
            bufferLen *= 2;
            buf.ReSizeThrows(bufferLen*sizeof(WCHAR));
            buffer = (wchar*)buf.Ptr(); // memcopied by QuickBytes automatically
            dst = buffer + offset;
        }

        if (adjust > 0) {
            switch (ch) {
            case '#':
            case '0':
            case '.':
                while (adjust > 0) { // digPos will be one greater than thousandsSepPos[thousandsSepCtr] since we are at
                    // the character after which the groupSeparator needs to be appended.
                    *dst++ = *dig != 0? *dig++: '0';
                    if (thousandSeps && digPos > 1 && thousandsSepCtr>=0) {
                        if (digPos == thousandsSepPos[thousandsSepCtr] + 1)  {
                            AddStringRef(&dst, sNumberGroup);
                            thousandsSepCtr--;
                        }
                    }
                    digPos--;
                    adjust--;
                }
            }
        }

        switch (ch) {
        case '#':
        case '0':
            {
                if (adjust < 0) {
                    adjust++;
                    ch = digPos <= firstDigit? '0': 0;
                }
                else {
                    ch = *dig != 0? *dig++: digPos > lastDigit? '0': 0;
                }
                if (ch != 0) {
                    *dst++ = ch;
                    if (thousandSeps && digPos > 1 && thousandsSepCtr>=0) {
                        if (digPos == thousandsSepPos[thousandsSepCtr] + 1) {
                            AddStringRef(&dst, sNumberGroup);
                            thousandsSepCtr--;
                        }
                    }
                }

                digPos--;
                break;
            }
        case '.':
            {
                if (digPos != 0 || decimalWritten) {
                    // For compatibility, don't echo repeated decimals
                    break;
                }
                // If the format has trailing zeros or the format has a decimal and digits remain
                if (lastDigit < 0
                    || (decimalPos < digitCount && *dig != 0)) {
                    AddStringRef(&dst, sNumberDecimal);
                    decimalWritten = TRUE;
                }
                break;
            }
        case 0x2030:
            AddStringRef(&dst, sPerMille);
            break;
        case '%':
            AddStringRef(&dst, sPercent);
            break;
        case ',':
            break;
        case '\'':
        case '"':
            // Buffer overflow possibility
            while (*src != 0 && *src != ch) {
                *dst++ = *src++;
                if ((unsigned int)(dst-buffer) == bufferLen-1) {
                    if (bufferLen - (unsigned int)(dst-buffer) < maxStrIncLen) {
                        int offset = static_cast<INT32>(dst - buffer);
                        bufferLen *= 2;
                        buf.ReSizeThrows(bufferLen*sizeof(WCHAR)); // memcopied by CQuickBytes automatically
                        buffer = (wchar *)buf.Ptr();
                        dst = buffer + offset;
                    }
                }
            }
            if (*src != 0) src++;
            break;
        case '\\':
            if (*src != 0) *dst++ = *src++;
            break;
        case 'E':
        case 'e':
            {
                STRINGREF sign = NULL;
                int i = 0;
                if (scientific) {
                    if (*src=='0') {
                        //Handles E0, which should format the same as E-0
                        i++;
                    } else if (*src == '+' && src[1] == '0') {
                        //Handles E+0
                        sign = sPositive;
                    } else if (*src == '-' && src[1] == '0') {
                        //Handles E-0
                        //Do nothing, this is just a place holder s.t. we don't break out of the loop.
                    } else {
                        *dst++ = ch;
                        break;
                    }
                    while (*++src == '0') i++;
                    if (i > 10) i = 10;
                    int exp = (GetDigitsBuffer(number))[0] == 0 ? 0 : number->scale - decimalPos;
                    dst = FormatExponent(dst, exp, ch, sign, sNegative, i);
                    scientific = 0;
                }
                else
                {
                    *dst++ = ch; // Copy E or e to output
                    if (*src== '+' || *src == '-') {
                        *dst++ = *src++;
                    }
                    while (*src == '0') {
                        *dst++ = *src++;
                    }
                }
                break;
            }
        default:
            *dst++ = ch;
        }
    }
    if (!((dst - buffer >= 0) && (dst - buffer <= (int)bufferLen))) {
        DoJITFailFast();
    }
    STRINGREF newStr = StringObject::NewString(buffer, (int)(dst - buffer));
    return newStr;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

FCIMPL3(void, COMNumber::DoubleToNumberFC, double value, int precision, NUMBER* number)
{
    FCALL_CONTRACT;

    DoubleToNumber(value, precision, number);
}
FCIMPLEND

FCIMPL1(double, COMNumber::NumberToDoubleFC, NUMBER* number)
{
    FCALL_CONTRACT;

    double d = 0;
    NumberToDouble(number, &d);
    return d;
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, COMNumber::NumberBufferToDecimal, NUMBER* number, DECIMAL* value)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(COMDecimal::NumberToDecimal(number, value) != 0);
}
FCIMPLEND
