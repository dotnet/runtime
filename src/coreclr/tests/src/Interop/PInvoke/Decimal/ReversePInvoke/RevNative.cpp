// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <iostream>
#include <xplatform.h>
#include "platformdefines.h"

DECIMAL g_DECIMAL_MaxValue = { 0, {{ 0, 0 }}, static_cast<int>(0xffffffff), {{static_cast<int>(0xffffffff), static_cast<int>(0xffffffff)}} };
DECIMAL g_DECIMAL_MinValue  = { 0, {{ 0, DECIMAL_NEG }}, static_cast<int>(0xffffffff), {{static_cast<int>(0xffffffff), static_cast<int>(0xffffffff) }}};
DECIMAL g_DECIMAL_Zero = { 0 };

CY g_CY_MaxValue = { {static_cast<int>(0xffffffff), 0x7fffffff} };
CY g_CY_MinValue = { {(LONG)0x00000000, (LONG)0x80000000} };
CY g_CY_Zero = { {0} };

typedef struct _Stru_Seq_DecAsStructAsFld
{
    int number;
    DECIMAL dec;
} Stru_Seq_DecAsStructAsFld;

typedef struct _Stru_Exp_DecAsCYAsFld
{
    WCHAR wc;
    CY cy;
} Stru_Exp_DecAsCYAsFld;

typedef struct _Stru_Seq_DecAsLPStructAsFld
{
    DOUBLE dblVal;
    CHAR cVal;
    DECIMAL* dec;
} Stru_Seq_DecAsLPStructAsFld;

// As Struct
typedef BOOL (STDMETHODCALLTYPE *Fp_Dec)(DECIMAL*);
typedef DECIMAL (STDMETHODCALLTYPE *Fp_RetDec)();
typedef BOOL (STDMETHODCALLTYPE *Fp_Stru_Seq_DecAsStructAsFld)(Stru_Seq_DecAsStructAsFld*);
// As CY
typedef BOOL (STDMETHODCALLTYPE *Fp_CY)(CY*);
typedef CY (STDMETHODCALLTYPE *Fp_RetCY)();
typedef BOOL (STDMETHODCALLTYPE *Fp_Stru_Exp_DecAsCYAsFld)(Stru_Exp_DecAsCYAsFld*);
// As LPStruct
typedef BOOL (STDMETHODCALLTYPE *Fp_DecAsLPStruct)(DECIMAL**);
typedef DECIMAL* (STDMETHODCALLTYPE *Fp_RetDecAsLPStruct)();
typedef BOOL (STDMETHODCALLTYPE *Fp_Stru_Seq_DecAsLPStructAsFld)(Stru_Seq_DecAsLPStructAsFld*);

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

bool IntEqualsToExpected(LPCSTR err_id, int number, int expected)
{
    
    if(number == expected)
    {
        return true;
    }
    else
    {
        std::wcout << "\t#Native Side Err# -- " << err_id 
                  << "\n\tnumber Expected = " << expected << std::endl;

        std::wcout << "\tnumber Actual is = " << number << std::endl;

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

template<typename T>
T* RetSpecificTypeInstancePtr(T tVal)
{
    T* lpT = (T*)CoreClrAlloc(sizeof(T));
    *lpT = tVal;
    return lpT;
}

// As Struct
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseCall_TakeDecByInOutRef(Fp_Dec fp)
{
    DECIMAL* lpDec = RetSpecificTypeInstancePtr(g_DECIMAL_MaxValue);

    if((*fp)(lpDec))
        return DecEqualsToExpected("001.01", g_DECIMAL_MinValue, *lpDec);
    else
        return false;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseCall_TakeDecByOutRef(Fp_Dec fp)
{
    DECIMAL* lpDec = RetSpecificTypeInstancePtr(g_DECIMAL_MaxValue);

    if((*fp)(lpDec))
        return DecEqualsToExpected("001.02", g_DECIMAL_Zero, *lpDec);
    else
        return false;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseCall_DecRet(Fp_RetDec fp)
{
    return DecEqualsToExpected("001.03", g_DECIMAL_MinValue, (*fp)());
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseCall_TakeStru_Seq_DecAsStructAsFldByInOutRef(Fp_Stru_Seq_DecAsStructAsFld fp)
{
    Stru_Seq_DecAsStructAsFld s = { 1, g_DECIMAL_MaxValue };

    if((*fp)(&s))
        return DecEqualsToExpected("001.04", g_DECIMAL_MinValue, s.dec) && IntEqualsToExpected("001.05", s.number, 2);
    else
        return false;
}

// As CY
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseCall_TakeCYByInOutRef(Fp_CY fp)
{
    CY* lpCy = RetSpecificTypeInstancePtr(g_CY_MaxValue);

    if((*fp)(lpCy))
        return CYEqualsToExpected("002.01", g_CY_MinValue, *lpCy);
    else
        return false;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseCall_TakeCYByOutRef(Fp_CY fp)
{
    CY* lpCy = RetSpecificTypeInstancePtr(g_CY_MaxValue);

    if((*fp)(lpCy))
        return CYEqualsToExpected("002.02", g_CY_Zero, *lpCy);
    else
        return false;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseCall_CYRet(Fp_RetCY fp)
{
    return CYEqualsToExpected("002.03", g_CY_MinValue, (*fp)());
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseCall_TakeStru_Exp_DecAsCYAsFldByOutRef(Fp_Stru_Exp_DecAsCYAsFld fp)
{
    Stru_Exp_DecAsCYAsFld s = { 0 };

    if((*fp)(&s))
        return CYEqualsToExpected("002.04", g_CY_MaxValue, s.cy) && Equals("002.05", W('C'), s.wc);
    else
        return false;
}

// As LPStrcut
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseCall_TakeDecByInOutRefAsLPStruct(Fp_DecAsLPStruct fp)
{
    DECIMAL* lpDec = RetSpecificTypeInstancePtr(g_DECIMAL_MaxValue);
    DECIMAL**    lppDec = &lpDec;

    if((*fp)(lppDec))
        return DecEqualsToExpected("003.01", g_DECIMAL_MinValue, **lppDec);
    else
        return false;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseCall_TakeDecByOutRefAsLPStruct(Fp_DecAsLPStruct fp)
{
    DECIMAL* lpDecAsLPStruct = RetSpecificTypeInstancePtr(g_DECIMAL_MaxValue);
    DECIMAL** lppDecAsLPStruct = &lpDecAsLPStruct;

    if((*fp)(lppDecAsLPStruct))
        return DecEqualsToExpected("003.02", g_DECIMAL_Zero, **lppDecAsLPStruct);
    else
        return false;
}

//************** ReverseCall Return Int From Net **************//
typedef int (*Fp_RetInt)();
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseCall_IntRet(Fp_RetInt fp)
{
    return 0x12345678 == (*fp)();
}
