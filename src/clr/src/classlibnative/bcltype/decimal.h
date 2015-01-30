//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// File: Decimal.h
//

//

#ifndef _DECIMAL_H_
#define _DECIMAL_H_

#include <oleauto.h>

#include <pshpack1.h>

#include "number.h"

#define DECIMAL_PRECISION 29

class COMDecimal {
public:
    static FCDECL2_IV(void, InitSingle, DECIMAL *_this, float value);
    static FCDECL2_IV(void, InitDouble, DECIMAL *_this, double value);
    static FCDECL2(INT32, DoCompare, DECIMAL * d1, DECIMAL * d2);
    static FCDECL1(INT32, GetHashCode, DECIMAL *d);

    static FCDECL3(void, DoAddSubThrow, DECIMAL * d1, DECIMAL * d2, UINT8 bSign);
    static FCDECL2(void, DoDivideThrow, DECIMAL * d1, DECIMAL * d2);
    static FCDECL2(void, DoMultiplyThrow, DECIMAL * d1, DECIMAL * d2);

    static FCDECL4(void, DoAddSub, DECIMAL * d1, DECIMAL * d2, UINT8 bSign, CLR_BOOL * overflowed);
    static FCDECL3(void, DoDivide, DECIMAL * d1, DECIMAL * d2, CLR_BOOL * overflowed);
    static FCDECL3(void, DoMultiply, DECIMAL * d1, DECIMAL * d2, CLR_BOOL * overflowed);

    static FCDECL2(void, DoRound, DECIMAL * d1, INT32 decimals);
    static FCDECL2_IV(void, DoToCurrency, CY * result, DECIMAL d);
    static FCDECL1(void, DoTruncate, DECIMAL * d);
    static FCDECL1(void, DoFloor, DECIMAL * d);

    static FCDECL1(double, ToDouble, DECIMAL d);
    static FCDECL1(float, ToSingle, DECIMAL d);
    static FCDECL1(INT32, ToInt32, DECIMAL d);	
    static FCDECL1(Object*, ToString, DECIMAL d);
    
    static void DecimalToNumber(DECIMAL* value, NUMBER* number);
    static int NumberToDecimal(NUMBER* number, DECIMAL* value);
    

};

#include <poppack.h>

#endif // _DECIMAL_H_
