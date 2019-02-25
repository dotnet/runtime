// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <iostream>
#include <xplatform.h>
#include "platformdefines.h"

DECIMAL g_DECIMAL_MaxValue = { 0, {{ 0, 0 }}, static_cast<int>(0xffffffff), {{static_cast<int>(0xffffffff), static_cast<int>(0xffffffff)}} };
DECIMAL g_DECIMAL_MinValue  = { 0, {{ 0, DECIMAL_NEG }}, static_cast<int>(0xffffffff), {{static_cast<int>(0xffffffff), static_cast<int>(0xffffffff)}} };
DECIMAL g_DECIMAL_Zero = { 0 };

CY g_CY_MaxValue = { { static_cast<int>(0xffffffff), 0x7fffffff} };
CY g_CY_MinValue = { { (LONG)0x00000000, (LONG)0x80000000 } };
CY g_CY_Zero = { { 0 } };

typedef struct _Stru_Exp_DecAsCYAsFld
{
    WCHAR wc;
    CY cy;
} Stru_Exp_DecAsCYAsFld;

typedef struct _Stru_Seq_DecAsLPStructAsFld
{
    DOUBLE dblVal;
    WCHAR cVal;
    DECIMAL* lpDec;
} Stru_Seq_DecAsLPStructAsFld;

void DecDisplay(const DECIMAL& dec)
{
    std::cout << "\twReserved" << "\t\t" << dec.wReserved << "\n"
        << "\tscale" << "\t\t" << dec.scale << "\n"
        << "\tsign" << "\t\t" << dec.sign << "\n"
        << "\tsignscale" << "\t\t" << dec.signscale << "\n"
        << "\tHi32" << "\t\t" << dec.Hi32 << "\n"
        << "\tLo32" << "\t\t" << dec.Lo32 << "\n"
        << "\tMid32" << "\t\t" << dec.Mid32 << "\n"
        << "\tLo64" << "\t\t" << dec.Lo64 << std::endl;
}

template<typename T>
bool operator==(const T& t1, const T& t2)
{
    return 0 == memcmp((void*)&t1, (void*)&t2, sizeof(T));
}

template<typename T>
bool Equals(LPCSTR err_id, T expected, T actual)
{
    if(expected == actual)
        return true;
    else
    {
        std::cout << "\t#Native Side Err# -- " << err_id 
            << "\n\tExpected = " << expected << std::endl;
        std::wcout << "\tActual is = " << actual << std::endl;

        return false;
    }
}

bool DecEqualsToExpected(LPCSTR err_id, const DECIMAL& expected, const DECIMAL& actual)
{
    if(expected == actual)
        return true;
    else
    {
        std::cout << "\t#Native Side Err # -- " << err_id 
            << "DECIMAL Expected is :" << std::endl;
        DecDisplay(expected);

        std::cout << "\t" << "____________________________________" << std::endl;

        std::cout << "\tDECIMAL Actual is :" << std::endl;
        DecDisplay(actual);

        return false;
    }
}

bool CYEqualsToExpected(LPCSTR err_id, const CY& expected, const CY& actual)
{
    if(expected == actual)
        return true;
    else
    {
        std::cout << "\t#Native Side Err# -- " << err_id 
            << "\n\tCY Expected is :" << "Hi = " << expected.Hi
            << "Lo = " << expected.Lo << std::endl;

        std::cout << "\tCY Actual is :" << "Hi = " << actual.Hi
            << "Lo = " << actual.Lo << std::endl;

        return false;
    }
}

// DECIMAL
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDecAsInOutParamAsLPStructByRef(DECIMAL** lppDec)
{
    if(DecEqualsToExpected("001.01", g_DECIMAL_MaxValue, **lppDec))
    {
        **lppDec = g_DECIMAL_MinValue;
        return true;
    }
    else
        return false;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeDecAsOutParamAsLPStructByRef(DECIMAL** lppDec)
{
    if(*lppDec)
    {
        std::cout << "\t#Native Side Err# -- 001.02 DECIMAL* is not NULL" << std::endl;
        return false;
    }
    else
    {
        *lppDec = (DECIMAL*)CoreClrAlloc(sizeof(DECIMAL));
        **lppDec = g_DECIMAL_MinValue;

        return true;
    }
}

extern "C" DLL_EXPORT DECIMAL* STDMETHODCALLTYPE RetDec()
{
    return &g_DECIMAL_MaxValue;
}

// CY
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeCYAsInOutParamAsLPStructByRef(CY* lpCy)
{
    if(CYEqualsToExpected("002.01", g_CY_MaxValue, *lpCy))
    {
        *lpCy = g_CY_MinValue;
        return true;
    }
    else
        return false;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeCYAsOutParamAsLPStructByRef(CY* lpCy)
{
    if(g_CY_Zero == *lpCy) 
    {        
        *lpCy = g_CY_MinValue;

        return true;
    }
    else
    {
        std::cout << "\t#Native Side Err# -- 002.02 CY is not clear up." << std::endl;
        return false;
    }
}

extern "C" DLL_EXPORT CY STDMETHODCALLTYPE RetCY()
{
    return g_CY_MinValue;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE TakeStru_Exp_DecAsCYAsFldByInOutRef(Stru_Exp_DecAsCYAsFld* s)
{
    if(CYEqualsToExpected("001.04.01", g_CY_Zero, s->cy) && Equals("001.04.02", W('\0'), s->wc))
    {
        s->cy = g_CY_MaxValue;
        s->wc = W('C');

        return true;
    }
    else
    {
        std::cout << "\t#Native Side Err# -- 002.02 Stru_Exp_DecAsCYAsFld is not clear up." << std::endl;
        return false;
    }
}
