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

#ifndef FEATURE_BCL_FORMATTING
enum PAL_NUMBERType {
    PALNUMBERTYPE_INT     = 1,    // PAL_IntToNumber
    PALNUMBERTYPE_INT64   = 2,    // PAL_Int64ToNumber
    PALNUMBERTYPE_UINT    = 3,    // PAL_UIntToNumber
    PALNUMBERTYPE_UINT64  = 4,    // PAL_UInt64ToNumber
    PALNUMBERTYPE_DOUBLE  = 5,    // PAL_DoubleToNumber
};
#endif

struct NUMBER {
    int precision;
    int scale;
    int sign;
    wchar_t digits[NUMBER_MAXDIGITS + 1];
    wchar_t* allDigits;
#ifndef FEATURE_BCL_FORMATTING
    PAL_NUMBERHolder palNumber;
    PAL_NUMBERType palNumberType;
#endif
    NUMBER() : precision(0), scale(0), sign(0), allDigits(NULL) {}
};

class COMNumber
{
public:
    static FCDECL3_VII(Object*, FormatDecimal, FC_DECIMAL value, StringObject* formatUNSAFE, NumberFormatInfo* numfmtUNSAFE);
    static FCDECL3_VII(Object*, FormatDouble,  double  value, StringObject* formatUNSAFE, NumberFormatInfo* numfmtUNSAFE);
    static FCDECL3_VII(Object*, FormatSingle,  float   value, StringObject* formatUNSAFE, NumberFormatInfo* numfmtUNSAFE);
    static FCDECL3(Object*, FormatInt32,   INT32      value, StringObject* formatUNSAFE, NumberFormatInfo* numfmtUNSAFE);
    static FCDECL3(Object*, FormatUInt32,  UINT32     value, StringObject* formatUNSAFE, NumberFormatInfo* numfmtUNSAFE);
    static FCDECL3_VII(Object*, FormatInt64,   INT64  value, StringObject* formatUNSAFE, NumberFormatInfo* numfmtUNSAFE);
    static FCDECL3_VII(Object*, FormatUInt64,  UINT64 value, StringObject* formatUNSAFE, NumberFormatInfo* numfmtUNSAFE);
#if !defined(FEATURE_CORECLR)
    static FCDECL4(Object*, FormatNumberBuffer, BYTE* number, StringObject* formatUNSAFE, NumberFormatInfo* numfmtUNSAFE, __in_z wchar_t* allDigits);
#endif // !FEATURE_CORECLR
    static FCDECL2(FC_BOOL_RET, NumberBufferToDecimal, BYTE* number, DECIMAL* value);
    static FCDECL2(FC_BOOL_RET, NumberBufferToDouble, BYTE* number, double* value);
    
    static wchar_t* Int32ToDecChars(__in wchar_t* p, unsigned int value, int digits);
};

#include <poppack.h>

#endif // _NUMBER_H_
