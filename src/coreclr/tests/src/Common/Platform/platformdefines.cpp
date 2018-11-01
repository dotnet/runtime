// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#include "platformdefines.h"

LPWSTR HackyConvertToWSTR(const char* pszInput)
{
    size_t cchInput;
    LPWSTR pwszOutput;
    char*  pStr;

    if (NULL == pszInput) return NULL;

    // poor mans strlen
    pStr     = (char*)pszInput;
    cchInput = 0;
    while('\0' != *pStr) {cchInput++; pStr++;}
    pwszOutput = new WCHAR[ cchInput + 1];

    for(size_t i=0; i<=cchInput; i++)
    {
        pwszOutput[i] = (WCHAR)pszInput[i];
    }

    return pwszOutput;
}

LPSTR HackyConvertToSTR(LPWSTR pwszInput)
{
    size_t cchInput;
    LPSTR  pszOutput;

    if (NULL == pwszInput) return NULL;

    cchInput = wcslen(pwszInput);
    pszOutput = new char[ cchInput + 1];

    for(size_t i=0; i<=cchInput; i++)
    {
        // ugly down cast
        pszOutput[i] = (char)pwszInput[i];
    }

    return pszOutput;
}

error_t TP_scpy_s(LPWSTR strDestination, size_t sizeInWords, LPCWSTR strSource)
{
    size_t cnt;
    // copy sizeInBytes bytes of strSource into strDestination

    if (NULL == strDestination || NULL == strSource) return 1;

    cnt = 0;
    while(cnt < sizeInWords && '\0' != strSource[cnt])
    {
        strDestination[cnt] = strSource[cnt];
        cnt++;
    }
    strDestination[cnt] = '\0';

    return 0;
}

error_t TP_scat_s(LPWSTR strDestination, size_t sizeInWords, LPCWSTR strSource)
{
    LPWSTR strEnd;
    // locate the end (ie. '\0') and TP_scpy_s the string

    if (NULL == strDestination || NULL == strSource) return 1;

    strEnd = strDestination;
    while('\0' != *strEnd) strEnd++;

    return TP_scpy_s(strEnd, sizeInWords - ((strEnd - strDestination) / sizeof(WCHAR)), strSource);
}

size_t TP_slen(LPCWSTR str)
{
    size_t len;

    if (NULL == str) return 0;

    len = 0;
    while('\0' != *(str+len)) len++;

    return len;
}

int TP_scmp_s(LPCSTR str1, LPCSTR str2)
{
    // < 0 str1 less than str2
    // 0  str1 identical to str2
    // > 0 str1 greater than str2

    if (NULL == str1 && NULL != str2) return -1;
    if (NULL != str1 && NULL == str2) return 1;
    if (NULL == str1 && NULL == str2) return 0;

    while (*str1 == *str2 && '\0' != *str1 && '\0' != *str2)
    {
        str1++;
        str2++;
    }

    if ('\0' == *str1 && '\0' == *str2) return 0;

    if ('\0' != *str1) return -1;
    if ('\0' != *str2) return 1;

    return (*str1 > *str2) ? 1 : -1;
}

int TP_wcmp_s(LPCWSTR str1, LPCWSTR str2)
{
    // < 0 str1 less than str2
    // 0  str1 identical to str2
    // > 0 str1 greater than str2

    if (NULL == str1 && NULL != str2) return -1;
    if (NULL != str1 && NULL == str2) return 1;
    if (NULL == str1 && NULL == str2) return 0;

    while (*str1 == *str2 && '\0' != *str1 && '\0' != *str2)
    {
        str1++;
        str2++;
    }

    if ('\0' == *str1 && '\0' == *str2) return 0;

    if ('\0' != *str1) return -1;
    if ('\0' != *str2) return 1;

    return (*str1 > *str2) ? 1 : -1;
}

error_t TP_getenv_s(size_t* pReturnValue, LPWSTR buffer, size_t sizeInWords, LPCWSTR varname)
{
    if (NULL == pReturnValue || NULL == varname) return 1;

#ifdef WINDOWS
    
     size_t  returnValue;
     WCHAR   buf[100];
     if( 0 != _wgetenv_s(&returnValue, buf, 100, varname) || returnValue<=0 )
        return 2;
    
    
    TP_scpy_s(buffer, sizeInWords, (LPWSTR)buf);
#else
    LPSTR pRet;
    pRet = getenv( HackyConvertToSTR((LPWSTR)varname) );
    if (NULL == pRet) return 2;
    TP_scpy_s(buffer, sizeInWords, HackyConvertToWSTR(pRet));
#endif
    return 0;
}

error_t TP_putenv_s(LPTSTR name, LPTSTR value)
{
    if (NULL == name || NULL == value) return 1;

#ifdef WINDOWS
    if( 0 != _putenv_s(name, value))
        return 2;
    else
        return 0;
#else
    int retVal = 0;
    char *assignment = (char*) malloc(sizeof(char) * (strlen(name) + strlen(value) + 1));
    sprintf(assignment, "%s=%s", name, value);

    if (0 != putenv(assignment))
        retVal = 2;
    free(assignment);
    return retVal;
#endif
}

void TP_ZeroMemory(LPVOID buffer, size_t sizeInBytes)
{
    BYTE* bBuf;

    // clear out the memory with 0's
    if (NULL == buffer) return;

    bBuf = (BYTE*)buffer;
    for(size_t i=0; i<sizeInBytes; i++)
    {
        bBuf[i] = 0;
    }
}

error_t TP_itow_s(int num, LPWSTR buffer, size_t sizeInCharacters, int radix)
{
    size_t len;
    int tmpNum;

    // only support radix == 10 and only positive numbers
    if (10 != radix) return 1;
    if (0 > num) return 2;
    if (NULL == buffer) return 3;
    if (2 > sizeInCharacters) return 4;

    // take care of the trivial case
    if (0 == num)
    {
        buffer[0] = '\0';
        buffer[1] = '\0';
    }

    // get length of final string (dumb implementation)
    len = 0;
    tmpNum = num;
    while (0 < tmpNum)
    {
        tmpNum /= 10;
        len++;
    }

    if (len >= sizeInCharacters) return 5;

    // convert num into a string (backwards)
    buffer[len] = '\0';
    while(0 < num && 0 < len)
    {
        len--;
        buffer[len] = (WCHAR)((num % 10) + '0');
        num /= 10;
    }

    return 0;
}

LPWSTR TP_sstr(LPWSTR str, LPWSTR searchStr)
{
    LPWSTR start;
    LPWSTR current;
    LPWSTR searchCurrent;

    if (NULL == str || NULL == searchStr) return NULL;

    // return a pointer to where searchStr
    //  exists in str
    current = str;
    start   = NULL;
    searchCurrent = searchStr;
    while('\0' != *current)
    {
        if (NULL != start && '\0' == *searchCurrent)
        {
            break;
        }

        if (*current == *searchCurrent)
        {
            searchCurrent++;
            if (NULL == start) start = current;
        }
        else
        {
            searchCurrent = searchStr;
            start = NULL;
        }
        current++;
    }

    return start;
}

DWORD TP_GetFullPathName(LPWSTR fileName, DWORD nBufferLength, LPWSTR lpBuffer)
{
#ifdef WINDOWS
    return GetFullPathNameW(fileName, nBufferLength, lpBuffer, NULL);
#else
    char nativeFullPath[MAX_PATH];
    (void)realpath(HackyConvertToSTR(fileName), nativeFullPath);
    LPWSTR fullPathForCLR = HackyConvertToWSTR(nativeFullPath);
    wcscpy_s(lpBuffer, MAX_PATH, fullPathForCLR);
    return wcslen(lpBuffer);
#endif
}
DWORD TP_CreateThread(THREAD_ID* tThread, LPTHREAD_START_ROUTINE worker,  LPVOID lpParameter)
{
#ifdef WINDOWS
    DWORD ret;
    *tThread = CreateThread(
        NULL,
        0,
        worker,
        lpParameter,
        0,
        &ret);
    return ret;
#else
    pthread_create(
        tThread,
        NULL,
        (MacWorker)worker,
        lpParameter);
#ifdef MAC64
    // This is a major kludge...64 bit posix threads just can't be cast into a DWORD and there just isn't
    // a great way to get what we're using for the ID. The fact that we're casting this at all is kind of
    // silly since we're returing the actual thread handle and everything being done to manipulate the thread
    // is done with that. Anyhow, the only thing done with the dword returned from this method is a printf
    // which is good since this DWORD really shouldn't be reliably used. Just in case it is though, return
    // a value that can be traced back to here.
    return 42;
#else
    return (DWORD)*tThread;
#endif
#endif
}

void TP_JoinThread(THREAD_ID tThread)
{
#ifdef WINDOWS
    WaitForSingleObject(tThread, INFINITE);
#else
    pthread_join(tThread, NULL);
#endif
}

#define INTSAFE_E_ARITHMETIC_OVERFLOW       ((HRESULT)0x80070216L)  // 0x216 = 534 = ERROR_ARITHMETIC_OVERFLOW
#define ULONG_ERROR     (0xffffffffUL)
#define WIN32_ALLOC_ALIGN (16 - 1)
//
// ULONGLONG -> ULONG conversion
//
HRESULT ULongLongToULong(ULONGLONG ullOperand, ULONG* pulResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pulResult = ULONG_ERROR;
    
    if (ullOperand <= ULONG_MAX)
    {
        *pulResult = (ULONG)ullOperand;
        hr = S_OK;
    }
    
    return hr;
}

HRESULT ULongAdd(ULONG ulAugend, ULONG ulAddend,ULONG* pulResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pulResult = ULONG_ERROR;

    if ((ulAugend + ulAddend) >= ulAugend)
    {
        *pulResult = (ulAugend + ulAddend);
        hr = S_OK;
    }
    
    return hr;
}

HRESULT ULongMult(ULONG ulMultiplicand, ULONG ulMultiplier, ULONG* pulResult)
{
    ULONGLONG ull64Result = UInt32x32To64(ulMultiplicand, ulMultiplier);
    
    return ULongLongToULong(ull64Result, pulResult);
}     

HRESULT CbSysStringSize(ULONG cchSize, BOOL isByteLen, ULONG *result)
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
            return S_OK;
        }
    }
    else
    {
        ULONG temp = 0; // should not use in-place addition in ULongAdd
        if (SUCCEEDED(ULongMult(cchSize, sizeof(WCHAR), &temp)) &
            SUCCEEDED(ULongAdd(temp, constant, result)))
        {
            *result = *result & ~WIN32_ALLOC_ALIGN;
            return S_OK;
        }
    }
    return INTSAFE_E_ARITHMETIC_OVERFLOW;
}

BSTR TP_SysAllocString(LPWSTR psz)
{
#ifdef WINDOWS    
    return SysAllocString(psz);
#else
    if(psz == NULL)
        return NULL;
    return TP_SysAllocStringLen(psz, (DWORD)wcslen(psz));
#endif
}

BSTR TP_SysAllocStringLen(LPWSTR psz, size_t len)
{
    ULONG cbTotal = 0;

    if (FAILED(CbSysStringSize((ULONG)len, FALSE, &cbTotal)))
        return NULL;

    BSTR bstr = (BSTR)TP_CoTaskMemAlloc(cbTotal);

    if(bstr != NULL){

#if defined(_WIN64)
      // NOTE: There are some apps which peek back 4 bytes to look at the size of the BSTR. So, in case of 64-bit code,
      // we need to ensure that the BSTR length can be found by looking one DWORD before the BSTR pointer. 
      *(DWORD_PTR *)bstr = (DWORD_PTR) 0;
      bstr = (BSTR) ((char *) bstr + sizeof (DWORD));
#endif
      *(DWORD *)bstr = (DWORD)len * sizeof(OLECHAR);

      bstr = (BSTR) ((char*) bstr + sizeof(DWORD));

      if(psz != NULL){
            memcpy(bstr, psz, len * sizeof(OLECHAR));
      }

      bstr[len] = '\0'; // always 0 terminate
    }

    return bstr; 
}

BSTR TP_SysAllocStringByteLen(LPCSTR psz, size_t len)
{
#ifdef WINDOWS    
    return SysAllocStringByteLen(psz, (UINT)len);
#else
    BSTR bstr;
    ULONG cbTotal = 0;

    if (FAILED(CbSysStringSize(len, TRUE, &cbTotal)))
        return NULL;

    bstr = (BSTR)TP_CoTaskMemAlloc(cbTotal);

    if (bstr != NULL) {
#if defined(_WIN64)
      *(DWORD *)((char *)bstr + sizeof (DWORD)) = (DWORD)len;
#else
      *(DWORD *)bstr = (DWORD)len;
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
#endif    
}

void TP_SysFreeString(BSTR bstr)
{
#ifdef WINDOWS    
    return SysFreeString(bstr);
#else
    if (bstr == NULL)
      return;
    TP_CoTaskMemFree((BYTE *)bstr - sizeof(DWORD_PTR));  
#endif    
}

size_t TP_SysStringByteLen(BSTR bstr)
{
#ifdef WINDOWS    
    return SysStringByteLen(bstr);
#else   
    if(bstr == NULL)
      return 0;
    int32_t * p32 = (int32_t *) bstr;
    int32_t * p32_1 = p32 -1;
    DWORD * d32 = (DWORD *) bstr;
    DWORD * d32_1 = d32 - 1;
    //std::cout << p32 << p32_1 << endl;
    //std::cout << d32 << d32_1 << endl;
    return (unsigned int)(((DWORD *)bstr)[-1]);
#endif    
}
