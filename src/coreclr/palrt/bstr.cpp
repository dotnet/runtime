// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ===========================================================================
// File: bstr.cpp
//
// ===========================================================================


/*++

Abstract:

    PALRT BSTR support

Revision History:

--*/

#include "common.h"
#include "intsafe.h"

#define CCH_BSTRMAX 0x7FFFFFFF  // 4 + (0x7ffffffb + 1 ) * 2 ==> 0xFFFFFFFC
#define CB_BSTRMAX 0xFFFFFFFa   // 4 + (0xfffffff6 + 2) ==> 0xFFFFFFFC

#define WIN32_ALLOC_ALIGN (16 - 1)

inline HRESULT CbSysStringSize(ULONG cchSize, BOOL isByteLen, ULONG *result)
{
    if (result == NULL)
        return E_INVALIDARG;

    // +2 for the null terminator
    // + DWORD_PTR to store the byte length of the string
    int constant = sizeof(WCHAR) + sizeof(DWORD_PTR) + WIN32_ALLOC_ALIGN;

    if (isByteLen)
    {
        if (SUCCEEDED(ULongAdd(constant, cchSize, result)))
        {
            *result = *result & ~WIN32_ALLOC_ALIGN;
            return NOERROR;
        }
    }
    else
    {
        ULONG temp = 0; // should not use in-place addition in ULongAdd
        if (SUCCEEDED(ULongMult(cchSize, sizeof(WCHAR), &temp)) &&
            SUCCEEDED(ULongAdd(temp, constant, result)))
        {
            *result = *result & ~WIN32_ALLOC_ALIGN;
            return NOERROR;
        }
    }
    return INTSAFE_E_ARITHMETIC_OVERFLOW;
}

/***
*BSTR SysAllocStringLen(char*, unsigned int)
*Purpose:
*  Allocation a bstr of the given length and initialize with
*  the pasted in string
*
*Entry:
*  [optional]
*
*Exit:
*  return value = BSTR, NULL if the allocation failed.
*
***********************************************************************/
STDAPI_(BSTR) SysAllocStringLen(const OLECHAR *psz, UINT len)
{

    BSTR bstr;
    DWORD cbTotal = 0;

    if (FAILED(CbSysStringSize(len, FALSE, &cbTotal)))
        return NULL;

    bstr = (OLECHAR *)PAL_malloc(cbTotal);

    if(bstr != NULL){

#if defined(HOST_64BIT)
      // NOTE: There are some apps which peek back 4 bytes to look at the size of the BSTR. So, in case of 64-bit code,
      // we need to ensure that the BSTR length can be found by looking one DWORD before the BSTR pointer.
      *(DWORD_PTR *)bstr = (DWORD_PTR) 0;
      bstr = (BSTR) ((char *) bstr + sizeof (DWORD));
#endif
      *(DWORD FAR*)bstr = (DWORD)len * sizeof(OLECHAR);

      bstr = (BSTR) ((char*) bstr + sizeof(DWORD));

      if(psz != NULL){
            memcpy(bstr, psz, len * sizeof(OLECHAR));
      }

      bstr[len] = '\0'; // always 0 terminate
    }

    return bstr;
}

/***
*BSTR SysAllocString(char*)
*Purpose:
*  Allocation a bstr using the passed in string
*
*Entry:
*  String to create a bstr for
*
*Exit:
*  return value = BSTR, NULL if allocation failed
*
***********************************************************************/
STDAPI_(BSTR) SysAllocString(const OLECHAR* psz)
{
    if(psz == NULL)
      return NULL;

    return SysAllocStringLen(psz, (DWORD)wcslen(psz));
}

STDAPI_(BSTR)
SysAllocStringByteLen(const char FAR* psz, unsigned int len)
{
    BSTR bstr;
    DWORD cbTotal = 0;

    if (FAILED(CbSysStringSize(len, TRUE, &cbTotal)))
        return FALSE;

    bstr = (OLECHAR *)PAL_malloc(cbTotal);

    if (bstr != NULL) {
#if defined(HOST_64BIT)
      *(DWORD FAR*)((char *)bstr + sizeof (DWORD)) = (DWORD)len;
#else
      *(DWORD FAR*)bstr = (DWORD)len;
#endif

      bstr = (WCHAR*) ((char*) bstr + sizeof(DWORD_PTR));

      if (psz != NULL) {
            memcpy(bstr, psz, len);
      }

      // NULL-terminate with both a narrow and wide zero.
      *((char *)bstr + len) = '\0';
      *(WCHAR *)((char *)bstr + ((len + 1) & ~1)) = 0;
    }

    return bstr;
}

/***
*void SysFreeString(BSTR)
*Purpose:
*  Free the given BSTR.
*
*Entry:
*  bstr = the BSTR to free
*
*Exit:
*  None
*
***********************************************************************/
STDAPI_(void) SysFreeString(BSTR bstr)
{
    if(bstr == NULL)
      return;
    free((BYTE *)bstr-sizeof(DWORD_PTR));
}

/***
*unsigned int SysStringLen(BSTR)
*Purpose:
*  return the length in characters of the given BSTR.
*
*Entry:
*  bstr = the BSTR to return the length of
*
*Exit:
*  return value = unsigned int, length in characters.
*
***********************************************************************/
STDAPI_(unsigned int)
SysStringLen(BSTR bstr)
{
    if(bstr == NULL)
      return 0;
    return (unsigned int)((((DWORD FAR*)bstr)[-1]) / sizeof(OLECHAR));
}

/***
*unsigned int SysStringByteLen(BSTR)
*Purpose:
*  return the size in bytes of the given BSTR.
*
*Entry:
*  bstr = the BSTR to return the size of
*
*Exit:
*  return value = unsigned int, size in bytes.
*
***********************************************************************/
STDAPI_(unsigned int)
SysStringByteLen(BSTR bstr)
{
    if(bstr == NULL)
      return 0;
    return (unsigned int)(((DWORD FAR*)bstr)[-1]);
}

extern "C" HRESULT
ErrStringCopy(BSTR bstrSource, BSTR FAR *pbstrOut)
{
    if (bstrSource == NULL) {
	*pbstrOut = NULL;
	return NOERROR;
    }
    if ((*pbstrOut = SysAllocStringLen(bstrSource,
                                       SysStringLen(bstrSource))) == NULL)
	return E_OUTOFMEMORY;

    return NOERROR;
}

/***
*PRIVATE HRESULT ErrSysAllocString(char*, BSTR*)
*Purpose:
*  This is an implementation of SysAllocString that check for the
*  NULL return value and return the corresponding error - E_OUTOFMEMORY.
*
*  This is simply a convenience, and this routine is only used
*  internally by the oledisp component.
*
*Entry:
*  psz = the source string
*
*Exit:
*  return value = HRESULT
*    S_OK
*    E_OUTOFMEMORY
*
*  *pbstrOut = the newly allocated BSTR
*
***********************************************************************/
extern "C" HRESULT
ErrSysAllocString(const OLECHAR FAR* psz, BSTR FAR* pbstrOut)
{
    if(psz == NULL){
      *pbstrOut = NULL;
      return NOERROR;
    }

    if((*pbstrOut = SysAllocString(psz)) == NULL)
      return E_OUTOFMEMORY;

    return NOERROR;
}
