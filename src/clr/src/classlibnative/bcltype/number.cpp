// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: Number.cpp
//

//

#include "common.h"
#include "number.h"
#include "bignum.h"
#include "grisu3.h"
#include "fp.h"

typedef wchar_t wchar;

#define SCALE_NAN 0x80000000
#define SCALE_INF 0x7FFFFFFF

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
        *sign = _signbit(value);

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

FCIMPL3_VII(void, COMNumber::DoubleToNumberFC, double value, int precision, NUMBER* number)
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
