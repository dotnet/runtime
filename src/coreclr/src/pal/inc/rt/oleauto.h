// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// ===========================================================================
// File: oleauto.h
// 
// ===========================================================================
// simplified oleauto.h for PAL

#ifndef _OLEAUTO_H_
#define _OLEAUTO_H_
#include "oaidl.h"

#ifndef BEGIN_INTERFACE
#define BEGIN_INTERFACE
#define END_INTERFACE
#endif

// OleAut's VT_CY and VT_DECIMAL declarations required by System.Decimal and System.Currency

typedef struct {
    INT   cDig;
    ULONG dwInFlags;
    ULONG dwOutFlags;
    INT   cchUsed;
    INT   nBaseShift;
    INT   nPwr10;
} NUMPARSE;

#define NUMPRS_STD              0x1FFF

/* flags used by both dwInFlags and dwOutFlags:
 */
#define NUMPRS_LEADING_WHITE    0x0001
#define NUMPRS_TRAILING_WHITE   0x0002
#define NUMPRS_LEADING_PLUS     0x0004
#define NUMPRS_TRAILING_PLUS    0x0008
#define NUMPRS_LEADING_MINUS    0x0010
#define NUMPRS_TRAILING_MINUS   0x0020
#define NUMPRS_HEX_OCT          0x0040
#define NUMPRS_PARENS           0x0080
#define NUMPRS_DECIMAL          0x0100
#define NUMPRS_THOUSANDS        0x0200
#define NUMPRS_CURRENCY         0x0400
#define NUMPRS_EXPONENT         0x0800
#define NUMPRS_USE_ALL          0x1000

/* flags used by dwOutFlags only:
 */
#define NUMPRS_NEG              0x10000
#define NUMPRS_INEXACT          0x20000
/* flags used by VarNumFromParseNum to indicate acceptable result types:
 */
#define VTBIT_I1        (1 << VT_I1)
#define VTBIT_UI1       (1 << VT_UI1)
#define VTBIT_I2        (1 << VT_I2)
#define VTBIT_UI2       (1 << VT_UI2)
#define VTBIT_I4        (1 << VT_I4)
#define VTBIT_UI4       (1 << VT_UI4)
#define VTBIT_I8		(1 << VT_I8)
#define VTBIT_UI8		(1 << VT_UI8)
#define VTBIT_R4        (1 << VT_R4)
#define VTBIT_R8        (1 << VT_R8)
#define VTBIT_CY        (1 << VT_CY)
#define VTBIT_DECIMAL   (1 << VT_DECIMAL)

#define LOCALE_NOUSEROVERRIDE   0x80000000    /* OR in to avoid user override */
/*
 * Use NLS functions to format date, currency, time, and number.
 */
#ifndef LOCALE_USE_NLS
#define LOCALE_USE_NLS 0x10000000
#endif

// Compare results for VarDecCmp.  These are returned as a SUCCESS HResult.
// Subtracting one gives the usual values of -1 for Less Than, 
// 0 for Equal To, +1 for Greater Than.
//
#define VARCMP_LT   0
#define VARCMP_EQ   1
#define VARCMP_GT   2
#define VARCMP_NULL 3

STDAPI VariantChangeType(VARIANTARG * pvargDest,
                VARIANTARG * pvarSrc, USHORT wFlags, VARTYPE vt);

STDAPI VarCyFromR4(FLOAT fltIn, CY * pcyOut);
STDAPI VarCyFromR8(DOUBLE dblIn, CY * pcyOut);
STDAPI VarCyFromDec(DECIMAL *pdecIn, CY *pcyOut);
STDAPI VarCyAdd(CY cyLeft, CY cyRight, LPCY pcyResult);
STDAPI VarCySub(CY cyLeft, CY cyRight, LPCY pcyResult);
STDAPI VarCyMul(CY cyLeft, CY cyRight, LPCY pcyResult);
STDAPI VarCyInt(CY cyIn, LPCY pcyResult);
STDAPI VarCyRound(CY cyIn, INT cDecimals, LPCY pcyResult);
STDAPI VarCyFix(CY cyIn, LPCY pcyResult);

STDAPI VarR8FromCy(CY cyIn, DOUBLE * pdblOut);
STDAPI VarR4FromCy(CY cyIn, FLOAT * pfltOut);

STDAPI VarDecFromR4(FLOAT fltIn, DECIMAL *pdecOut);
STDAPI VarDecFromR8(DOUBLE dblIn, DECIMAL *pdecOut);
STDAPI VarDecFromCy(CY cyIn, DECIMAL *pdecOut);
STDAPI VarDecAdd(LPDECIMAL pdecLeft, LPDECIMAL pdecRight, LPDECIMAL pdecResult);
STDAPI VarDecSub(LPDECIMAL pdecLeft, LPDECIMAL pdecRight, LPDECIMAL pdecResult);
STDAPI VarDecMul(LPDECIMAL pdecLeft, LPDECIMAL pdecRight, LPDECIMAL pdecResult);
STDAPI VarDecDiv(LPDECIMAL pdecLeft, LPDECIMAL pdecRight, LPDECIMAL pdecResult);
STDAPI VarDecCmp(LPDECIMAL pdecLeft, LPDECIMAL pdecRight);
STDAPI VarDecInt(LPDECIMAL pdecIn, LPDECIMAL pdecResult);
STDAPI VarDecRound(LPDECIMAL pdecIn, INT cDecimals, LPDECIMAL pdecResult);
STDAPI VarDecFix(LPDECIMAL pdecIn, LPDECIMAL pdecResult);
STDAPI VarDecNeg(LPDECIMAL pdecIn, LPDECIMAL pdecResult);
STDAPI VarDecFromI4(LONG I4in, DECIMAL *pdecOut);
STDAPI VarDecFromUI4(ULONG UI4in, DECIMAL *pdecOut);

STDAPI VarI1FromDec(DECIMAL *pdecIn, CHAR *pI1In);
STDAPI VarUI1FromDec(DECIMAL *pdecIn, BYTE *pUI1In);
STDAPI VarI2FromDec(DECIMAL *pdecIn, SHORT *pI2In);
STDAPI VarUI2FromDec(DECIMAL *pdecIn, USHORT *pUI2In);
STDAPI VarI4FromDec(DECIMAL *pdecIn, LONG *pI4In);
STDAPI VarUI4FromDec(DECIMAL *pdecIn, ULONG *pUI4In);
STDAPI VarR8FromDec(DECIMAL *pdecIn, DOUBLE *pdblOut);
STDAPI VarR4FromDec(DECIMAL *pdecIn, FLOAT *pfltOut);

STDAPI VarI1FromR8(DOUBLE dblIn, CHAR *pcOut);
STDAPI VarI2FromR8(DOUBLE dblIn, SHORT * psOut);
STDAPI VarI4FromR8(DOUBLE dblIn, LONG * plOut);
STDAPI VarUI1FromR8(DOUBLE dblIn, BYTE * pbOut);
STDAPI VarUI2FromR8(DOUBLE dblIn, USHORT *puiOut);
STDAPI VarUI4FromR8(DOUBLE dblIn, ULONG *pulOut);

#endif // _OLEAUTO_H_
