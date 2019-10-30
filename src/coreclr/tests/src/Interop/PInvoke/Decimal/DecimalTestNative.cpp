// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "xplatform.h"
#include <new>

struct DecimalWrapper
{
    DECIMAL dec;
};

struct CurrencyWrapper
{
    CURRENCY currency;
};

namespace
{
    BOOL operator==(CURRENCY lhs, CURRENCY rhs)
    {
        return lhs.int64 == rhs.int64 ? TRUE : FALSE;
    }

    BOOL operator==(DECIMAL lhs, DECIMAL rhs)
    {
        return lhs.signscale == rhs.signscale && lhs.Hi32 == rhs.Hi32 && lhs.Lo64 == rhs.Lo64 ? TRUE : FALSE;
    }
}

extern "C" DLL_EXPORT DECIMAL STDMETHODCALLTYPE CreateDecimalFromInt(int32_t i)
{
    DECIMAL result;
    result.Hi32 = 0;
    result.Lo64 = abs(i);
    result.sign = i < 0 ? 1 : 0;
    result.scale = 0;
    result.wReserved = 0;
    return result;
}

extern "C" DLL_EXPORT LPDECIMAL STDMETHODCALLTYPE CreateLPDecimalFromInt(int32_t i)
{
    return new (CoreClrAlloc(sizeof(DECIMAL))) DECIMAL(CreateDecimalFromInt(i));
}

extern "C" DLL_EXPORT DecimalWrapper STDMETHODCALLTYPE CreateWrappedDecimalFromInt(int32_t i)
{
    return { CreateDecimalFromInt(i) };
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE DecimalEqualToInt(DECIMAL dec, int32_t i)
{
    DECIMAL intDecimal = CreateDecimalFromInt(i);
    return dec == intDecimal;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE LPDecimalEqualToInt(LPDECIMAL dec, int32_t i)
{
    DECIMAL intDecimal = CreateDecimalFromInt(i);
    return *dec == intDecimal;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE WrappedDecimalEqualToInt(DecimalWrapper dec, int32_t i)
{
    DECIMAL intDecimal = CreateDecimalFromInt(i);
    return dec.dec == intDecimal;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateAndChangeDecimal(DECIMAL* dec, int32_t expected, int32_t newValue)
{
    BOOL result = *dec == CreateDecimalFromInt(expected);
    *dec = CreateDecimalFromInt(newValue);
    return result;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateAndChangeWrappedDecimal(DecimalWrapper* dec, int32_t expected, int32_t newValue)
{
    BOOL result = dec->dec == CreateDecimalFromInt(expected);
    dec->dec = CreateDecimalFromInt(newValue);
    return result;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateAndChangeLPDecimal(LPDECIMAL* dec, int32_t expected, int32_t newValue)
{
    BOOL result = **dec == CreateDecimalFromInt(expected);
    *dec = new(CoreClrAlloc(sizeof(DECIMAL))) DECIMAL(CreateDecimalFromInt(newValue));
    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetDecimalForInt(int32_t i, DECIMAL* dec)
{
    *dec = CreateDecimalFromInt(i);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetLPDecimalForInt(int32_t i, LPDECIMAL* dec)
{
    *dec = new (CoreClrAlloc(sizeof(DECIMAL))) DECIMAL(CreateDecimalFromInt(i));
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetWrappedDecimalForInt(int32_t i, DecimalWrapper* dec)
{
    dec->dec = CreateDecimalFromInt(i);
}

extern "C" DLL_EXPORT CURRENCY STDMETHODCALLTYPE CreateCurrencyFromInt(int32_t i)
{
    CY currency;
    currency.int64 = i * 10000;
    return currency;
}

extern "C" DLL_EXPORT CurrencyWrapper STDMETHODCALLTYPE CreateWrappedCurrencyFromInt(int32_t i)
{
    return { CreateCurrencyFromInt(i) };
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE CurrencyEqualToInt(CURRENCY currency, int32_t i)
{
    CURRENCY intCurrency = CreateCurrencyFromInt(i);
    return currency == intCurrency;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE WrappedCurrencyEqualToInt(CurrencyWrapper currency, int32_t i)
{
    CURRENCY intCurrency = CreateCurrencyFromInt(i);
    return currency.currency == intCurrency;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateAndChangeCurrency(CURRENCY* currency, int32_t expected, int32_t newValue)
{
    BOOL result = *currency == CreateCurrencyFromInt(expected);
    *currency = CreateCurrencyFromInt(newValue);
    return result;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateAndChangeWrappedCurrency(CurrencyWrapper* currency, int32_t expected, int32_t newValue)
{
    BOOL result = currency->currency == CreateCurrencyFromInt(expected);
    currency->currency = CreateCurrencyFromInt(newValue);
    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetCurrencyForInt(int32_t i, CURRENCY* currency)
{
    *currency = CreateCurrencyFromInt(i);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetWrappedCurrencyForInt(int32_t i, CurrencyWrapper* currency)
{
    currency->currency = CreateCurrencyFromInt(i);
}

using DecimalCallback = void(STDMETHODCALLTYPE*)(DECIMAL);

extern "C" DLL_EXPORT void STDMETHODCALLTYPE PassThroughDecimalToCallback(DECIMAL dec, DecimalCallback cb)
{
    cb(dec);
}

using LPDecimalCallback = void(STDMETHODCALLTYPE*)(LPDECIMAL);

extern "C" DLL_EXPORT void STDMETHODCALLTYPE PassThroughLPDecimalToCallback(LPDECIMAL dec, LPDecimalCallback cb)
{
    cb(dec);
}

using CurrencyCallback = void(STDMETHODCALLTYPE*)(CURRENCY);

extern "C" DLL_EXPORT void STDMETHODCALLTYPE PassThroughCurrencyToCallback(CURRENCY cy, CurrencyCallback cb)
{
    cb(cy);
}
