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

struct NUMBER {
    int precision;
    int scale;
    int sign;
    wchar_t digits[NUMBER_MAXDIGITS + 1];
    wchar_t* allDigits;
    NUMBER() : precision(0), scale(0), sign(0), allDigits(NULL) {}
};

class COMNumber
{
public:
    static FCDECL3_VII(void, DoubleToNumberFC, double value, int precision, NUMBER* number);
    static FCDECL1(double, NumberToDoubleFC, NUMBER* number);
};

#include <poppack.h>

#endif // _NUMBER_H_
