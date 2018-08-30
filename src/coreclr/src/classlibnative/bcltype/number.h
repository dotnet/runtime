// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: Number.h
//

//

#ifndef _NUMBER_H_
#define _NUMBER_H_

#include <pshpack1.h>

#define NUMBER_MAXDIGITS 50

static const double LOG10V2 = 0.30102999566398119521373889472449;

// DRIFT_FACTOR = 1 - LOG10V2 - epsilon (a small number account for drift of floating point multiplication)
static const double DRIFT_FACTOR = 0.69;

enum NUMBER_KIND : int {
    NUMBER_KIND_Unknown = 0,
    NUMBER_KIND_Integer = 1,
    NUMBER_KIND_Decimal = 2,
    NUMBER_KIND_Double = 3
};

struct NUMBER {
    int precision;                          //  0
    int scale;                              //  4
    int sign;                               //  8
    NUMBER_KIND kind;                       // 12
    wchar_t* allDigits;                     // 16
    wchar_t digits[NUMBER_MAXDIGITS + 1];   // 20 or 24
    NUMBER() : precision(0), scale(0), sign(0), kind(NUMBER_KIND_Unknown), allDigits(NULL) {}
};

class COMNumber
{
public:
    static FCDECL3_VII(void, DoubleToNumberFC, double value, int precision, NUMBER* number);
    static FCDECL1(double, NumberToDoubleFC, NUMBER* number);
};

#include <poppack.h>

#endif // _NUMBER_H_
