// Copyright 2022 Aaron R Robinson
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#define NOMINMAX
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <Objbase.h>
#include <combaseapi.h>

#include <stdint.h>
#include <stdlib.h>

#include <minipal_com.h>

LPVOID PAL_CoTaskMemAlloc(SIZE_T a)
{
    return CoTaskMemAlloc(a);
}

void PAL_CoTaskMemFree(LPVOID a)
{
    CoTaskMemFree(a);
}

size_t PAL_wcslen(WCHAR const* a)
{
    return wcslen(a);
}

int PAL_wcscmp(WCHAR const* a, WCHAR const* b)
{
    return wcscmp(a, b);
}

WCHAR* PAL_wcsstr(WCHAR const* a, WCHAR const* b)
{
    return wcsstr(a, b);
}

HRESULT PAL_CoCreateGuid(GUID* a)
{
    return CoCreateGuid(a);
}

BOOL PAL_IsEqualGUID(GUID const* a, GUID const* b)
{
    return IsEqualGUID(a, b);
}

int32_t PAL_StringFromGUID2(GUID const* a, LPOLESTR b, int32_t c)
{
    return StringFromGUID2(a, b, c);
}

HRESULT PAL_IIDFromString(LPCOLESTR a, IID* b)
{
    return IIDFromString(a, b);
}
