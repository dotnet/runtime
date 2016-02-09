// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    environ.cpp

Abstract:

    Implementation of functions manipulating environment variables.

Revision History:



--*/

#include "pal/palinternal.h"
#include "pal/critsect.h"
#include "pal/dbgmsg.h"
#include "pal/environ.h"
#include "pal/malloc.hpp"

#include <stdlib.h>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(MISC);

char **palEnvironment = nullptr;
CRITICAL_SECTION gcsEnvironment;

/*++
Function:
  GetEnvironmentVariableA

The GetEnvironmentVariable function retrieves the value of the
specified variable from the environment block of the calling
process. The value is in the form of a null-terminated string of
characters.

Parameters

lpName 
       [in] Pointer to a null-terminated string that specifies the environment variable. 
lpBuffer 
       [out] Pointer to a buffer to receive the value of the specified environment variable. 
nSize 
       [in] Specifies the size, in TCHARs, of the buffer pointed to by the lpBuffer parameter. 

Return Values

If the function succeeds, the return value is the number of TCHARs
stored into the buffer pointed to by lpBuffer, not including the
terminating null character.

If the specified environment variable name was not found in the
environment block for the current process, the return value is zero.

If the buffer pointed to by lpBuffer is not large enough, the return
value is the buffer size, in TCHARs, required to hold the value string
and its terminating null character.

--*/
DWORD
PALAPI
GetEnvironmentVariableA(
            IN LPCSTR lpName,
            OUT LPSTR lpBuffer,
            IN DWORD nSize)
{
    char  *value;
    DWORD dwRet = 0;

    PERF_ENTRY(GetEnvironmentVariableA);
    ENTRY("GetEnvironmentVariableA(lpName=%p (%s), lpBuffer=%p, nSize=%u)\n",
        lpName ? lpName : "NULL",
        lpName ? lpName : "NULL", lpBuffer, nSize);
    
    if (lpName == nullptr)
    {
        ERROR("lpName is null\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    if (lpName[0] == 0)
    {
        TRACE("lpName is an empty string\n", lpName);
        SetLastError(ERROR_ENVVAR_NOT_FOUND);
        goto done;
    }

    if (strchr(lpName, '=') != nullptr)
    {
        // GetEnvironmentVariable doesn't permit '=' in variable names.
        value = nullptr;
    }
    else
    {
        value = EnvironGetenv(lpName); ///////// make this not return a copy, or have it fill out the buffer
    }

    if (value == nullptr)
    {
        TRACE("%s is not found\n", lpName);
        SetLastError(ERROR_ENVVAR_NOT_FOUND);
        goto done;
    }

    if (strlen(value) < nSize)
    {
        strcpy_s(lpBuffer, nSize, value);
        dwRet = strlen(value);
    }
    else 
    {
        dwRet = strlen(value)+1;
    }

    SetLastError(ERROR_SUCCESS);

done:
    LOGEXIT("GetEnvironmentVariableA returns DWORD 0x%x\n", dwRet);
    PERF_EXIT(GetEnvironmentVariableA);
    return dwRet;
}


/*++
Function:
  GetEnvironmentVariableW

See MSDN doc.
--*/
DWORD
PALAPI
GetEnvironmentVariableW(
            IN LPCWSTR lpName,
            OUT LPWSTR lpBuffer,
            IN DWORD nSize)
{
    CHAR *inBuff = nullptr;
    CHAR *outBuff = nullptr;
    INT inBuffSize;
    DWORD size = 0;

    PERF_ENTRY(GetEnvironmentVariableW);
    ENTRY("GetEnvironmentVariableW(lpName=%p (%S), lpBuffer=%p, nSize=%u)\n",
          lpName ? lpName : W16_NULLSTRING,
          lpName ? lpName : W16_NULLSTRING, lpBuffer, nSize);

    inBuffSize = WideCharToMultiByte(CP_ACP, 0, lpName, -1,
                                     inBuff, 0, nullptr, nullptr);
    if (0 == inBuffSize)
    {
        ERROR("lpName has to be a valid parameter\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    inBuff = (CHAR *)PAL_malloc(inBuffSize);
    if (inBuff == nullptr)
    {
        ERROR("malloc failed\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }

    if (nSize)
    {
        outBuff = (CHAR *)PAL_malloc(nSize*2);
        if (outBuff == nullptr)
        {
            ERROR("malloc failed\n");
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto done;
        }
    }

    if (0 == WideCharToMultiByte(CP_ACP, 0, lpName, -1, inBuff,
                                   inBuffSize, NULL, NULL))
    {
        ASSERT("WideCharToMultiByte failed!\n");
        SetLastError(ERROR_INTERNAL_ERROR);
        goto done;
    }

    size = GetEnvironmentVariableA(inBuff, outBuff, nSize);
    if (size > nSize)
    {
        TRACE("Insufficient buffer\n");
    }
    else if (size == 0)
    {
        /* error handle in GetEnvironmentVariableA */
    }
    else
    {
        size = MultiByteToWideChar(CP_ACP, 0, outBuff, -1, lpBuffer, nSize);
        if (0 != size)
        {
            /* Not including the NULL. */
            size--;
        }
        else
        {
            ASSERT("MultiByteToWideChar failed!\n");
            SetLastError(ERROR_INTERNAL_ERROR);
            size = 0;
            *lpBuffer = '\0';
        }
    }

done:
    PAL_free(outBuff);
    PAL_free(inBuff);

    LOGEXIT("GetEnvironmentVariableW returns DWORD 0x%x\n", size);
    PERF_EXIT(GetEnvironmentVariableW);

    return size;
}


/*++
Function:
  SetEnvironmentVariableW

The SetEnvironmentVariable function sets the value of an environment
variable for the current process.

Parameters

lpName 
       [in] Pointer to a null-terminated string that specifies the
       environment variable whose value is being set. The operating
       system creates the environment variable if it does not exist
       and lpValue is not NULL.
lpValue
       [in] Pointer to a null-terminated string containing the new
       value of the specified environment variable. If this parameter
       is NULL, the variable is deleted from the current process's
       environment.

Return Values

If the function succeeds, the return value is nonzero.

If the function fails, the return value is zero. To get extended error
information, call GetLastError.

Remarks

This function has no effect on the system environment variables or the
environment variables of other processes.

--*/
BOOL
PALAPI
SetEnvironmentVariableW(
            IN LPCWSTR lpName,
            IN LPCWSTR lpValue)
{
    PCHAR name = nullptr;
    PCHAR value = nullptr;
    INT nameSize = 0;
    INT valueSize = 0;
    BOOL bRet = FALSE;

    PERF_ENTRY(SetEnvironmentVariableW);
    ENTRY("SetEnvironmentVariableW(lpName=%p (%S), lpValue=%p (%S))\n",
        lpName?lpName:W16_NULLSTRING,
        lpName?lpName:W16_NULLSTRING, lpValue?lpValue:W16_NULLSTRING, lpValue?lpValue:W16_NULLSTRING);

    if ((nameSize = WideCharToMultiByte(CP_ACP, 0, lpName, -1, name, 0, 
                                        nullptr, nullptr)) == 0)
    {
        ERROR("WideCharToMultiByte failed\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    name = (PCHAR)PAL_malloc(sizeof(CHAR)* nameSize);
    if (name == nullptr)
    {
        ERROR("malloc failed\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }
    
    if (0 == WideCharToMultiByte(CP_ACP, 0, lpName,  -1, 
                                 name,  nameSize, nullptr, nullptr))
    {
        ASSERT("WideCharToMultiByte returned 0\n");
        SetLastError(ERROR_INTERNAL_ERROR);
        goto done;
    }

    if (lpValue != nullptr)
    {
        if ((valueSize = WideCharToMultiByte(CP_ACP, 0, lpValue, -1, value, 
                                             0, NULL, NULL)) == 0)
        {
            ERROR("WideCharToMultiByte failed\n");
            SetLastError(ERROR_INVALID_PARAMETER);
            goto done;
        }

        value = (PCHAR)PAL_malloc(sizeof(CHAR)*valueSize);

        if (value == nullptr)
        {
            ERROR("malloc failed\n");
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto done;
        }

        if (0 == WideCharToMultiByte(CP_ACP, 0, lpValue, -1,
                                     value, valueSize, NULL, NULL))
        {
            ASSERT("WideCharToMultiByte failed\n");
            SetLastError( ERROR_INTERNAL_ERROR );
            goto done;
        }
    }

    bRet = SetEnvironmentVariableA(name, value);
done:
    PAL_free(value);
    PAL_free(name);

    LOGEXIT("SetEnvironmentVariableW returning BOOL %d\n", bRet);
    PERF_EXIT(SetEnvironmentVariableW);
    return bRet;
}


/*++
Function:
  GetEnvironmentStringsW

The GetEnvironmentStrings function retrieves the environment block for
the current process.

Parameters

This function has no parameters.

Return Values

The return value is a pointer to an environment block for the current process.

Remarks

The GetEnvironmentStrings function returns a pointer to the
environment block of the calling process. This should be treated as a
read-only block; do not modify it directly.  Instead, use the
GetEnvironmentVariable and SetEnvironmentVariable functions to
retrieve or change the environment variables within this block. When
the block is no longer needed, it should be freed by calling
FreeEnvironmentStrings.

--*/
LPWSTR
PALAPI
GetEnvironmentStringsW(
               VOID)
{
    WCHAR *wenviron = nullptr, *tempEnviron;
    int i, len, envNum;

    PERF_ENTRY(GetEnvironmentStringsW);
    ENTRY("GetEnvironmentStringsW()\n");

    PALCEnterCriticalSection(&gcsEnvironment);

    envNum = 0;
    len    = 0;

    /* get total length of the bytes that we need to allocate */
    for (i = 0; palEnvironment[i] != 0; i++)
    {
        len = MultiByteToWideChar(CP_ACP, 0, palEnvironment[i], -1, wenviron, 0);
        envNum += len;
    }

    wenviron = (WCHAR *)PAL_malloc(sizeof(WCHAR)* (envNum + 1));
    if (wenviron == NULL) 
    {
        ERROR("malloc failed\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    len = 0;
    tempEnviron = wenviron;
    for (i = 0; palEnvironment[i] != 0; i++)
    {
        len = MultiByteToWideChar(CP_ACP, 0, palEnvironment[i], -1, tempEnviron, envNum);
        tempEnviron += len;
        envNum      -= len;
    }

    *tempEnviron = 0; /* Put an extra NULL at the end */
    
 EXIT:
    PALCLeaveCriticalSection(&gcsEnvironment);

    LOGEXIT("GetEnvironmentStringsW returning %p\n", wenviron);
    PERF_EXIT(GetEnvironmentStringsW);
    return wenviron;
}


/*++
Function:
  GetEnvironmentStringsA

See GetEnvironmentStringsW.

--*/
LPSTR
PALAPI
GetEnvironmentStringsA(
               VOID)
{
    char *environ = NULL, *tempEnviron;
    int i, len, envNum;

    PERF_ENTRY(GetEnvironmentStringsA);
    ENTRY("GetEnvironmentStringsA()\n");

    PALCEnterCriticalSection(&gcsEnvironment);

    envNum = 0;
    len    = 0;

    /* get total length of the bytes that we need to allocate */
    for (i = 0; palEnvironment[i] != 0; i++)
    {
        len = strlen(palEnvironment[i]) + 1;
        envNum += len;
    }

    environ = (char *)PAL_malloc(envNum + 1);
    if (environ == NULL)
    {
        ERROR("malloc failed\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    len = 0;
    tempEnviron = environ;
    for (i = 0; palEnvironment[i] != 0; i++)
    {
        len = strlen(palEnvironment[i]) + 1;
        memcpy(tempEnviron, palEnvironment[i], len);
        tempEnviron += len;
        envNum      -= len;
    }

    *tempEnviron = 0; /* Put an extra NULL at the end */
    
 EXIT:
    PALCLeaveCriticalSection(&gcsEnvironment);

    LOGEXIT("GetEnvironmentStringsA returning %p\n", environ);
    PERF_EXIT(GetEnvironmentStringsA);
    return environ;
}


/*++
Function:
  FreeEnvironmentStringsW

The FreeEnvironmentStrings function frees a block of environment strings.

Parameters

lpszEnvironmentBlock   [in] Pointer to a block of environment strings. The pointer to
                            the block must be obtained by calling the
                            GetEnvironmentStrings function. 

Return Values

If the function succeeds, the return value is nonzero.  If the
function fails, the return value is zero. To get extended error
information, call GetLastError.

Remarks

When GetEnvironmentStrings is called, it allocates memory for a block
of environment strings. When the block is no longer needed, it should
be freed by calling FreeEnvironmentStrings.

--*/
BOOL
PALAPI
FreeEnvironmentStringsW(
            IN LPWSTR lpValue)
{
    PERF_ENTRY(FreeEnvironmentStringsW);
    ENTRY("FreeEnvironmentStringsW(lpValue=%p (%S))\n", lpValue?lpValue:W16_NULLSTRING, lpValue?lpValue:W16_NULLSTRING);

    if (lpValue != NULL)
    {
        PAL_free(lpValue);
    }

    LOGEXIT("FreeEnvironmentStringW returning BOOL TRUE\n");
    PERF_EXIT(FreeEnvironmentStringsW);
    return TRUE ;
}


/*++
Function:
  FreeEnvironmentStringsA

See FreeEnvironmentStringsW.

--*/
BOOL
PALAPI
FreeEnvironmentStringsA(
            IN LPSTR lpValue)
{
    PERF_ENTRY(FreeEnvironmentStringsA);
    ENTRY("FreeEnvironmentStringsA(lpValue=%p (%s))\n", lpValue?lpValue:"NULL", lpValue?lpValue:"NULL");

    if (lpValue != NULL)
    {
        PAL_free(lpValue);
    }

    LOGEXIT("FreeEnvironmentStringA returning BOOL TRUE\n");
    PERF_EXIT(FreeEnvironmentStringsA);
    return TRUE ;
}


/*++
Function:
  SetEnvironmentVariableA

The SetEnvironmentVariable function sets the value of an environment
variable for the current process.

Parameters

lpName
       [in] Pointer to a null-terminated string that specifies the
       environment variable whose value is being set. The operating
       system creates the environment variable if it does not exist
       and lpValue is not NULL.
lpValue
       [in] Pointer to a null-terminated string containing the new
       value of the specified environment variable. If this parameter
       is NULL, the variable is deleted from the current process's
       environment.

Return Values

If the function succeeds, the return value is nonzero.

If the function fails, the return value is zero. To get extended error
information, call GetLastError.

Remarks

This function has no effect on the system environment variables or the
environment variables of other processes.

--*/
BOOL
PALAPI
SetEnvironmentVariableA(
            IN LPCSTR lpName,
            IN LPCSTR lpValue)
{

    BOOL bRet = FALSE;
    int nResult =0;
    PERF_ENTRY(SetEnvironmentVariableA);
    ENTRY("SetEnvironmentVariableA(lpName=%p (%s), lpValue=%p (%s))\n",
        lpName ? lpName : "NULL", lpName ? lpName : "NULL",
        lpValue ? lpValue : "NULL", lpValue ? lpValue : "NULL");

    // exit if the input variable name is null
    if ((lpName == nullptr) || (lpName[0] == 0))
    {
        ERROR("lpName is NULL\n");
        goto done;
    }

    /* check if the input value is null and if so
     * check if the input name is valid and delete
     * the variable name from process environment */
    if (lpValue == nullptr)
    {
        if ((lpValue = EnvironGetenv(lpName)) == nullptr)
        {
            ERROR("Couldn't find environment variable (%s)\n", lpName);
            SetLastError(ERROR_ENVVAR_NOT_FOUND);
            goto done;
        }

        EnvironUnsetenv(lpName);
    }
    else
    {
        // All the conditions are met. Set the variable.
        int iLen = strlen(lpName) + strlen(lpValue) + 2;
        LPSTR string = (LPSTR) PAL_malloc(iLen);
        if (string == nullptr)
        {
            bRet = FALSE;
            ERROR("Unable to allocate memory\n");
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto done;
        }

        sprintf_s(string, iLen, "%s=%s", lpName, lpValue);
        nResult = EnvironPutenv(string, FALSE) ? 0 : -1;

        PAL_free(string);
        string = NULL;

        // If EnvironPutenv returns FALSE, it almost certainly failed to
        // allocate memory.          ////////////////////////////still true?
        if (nResult == -1)
        {
            bRet = FALSE;
            ERROR("Unable to allocate memory\n");
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto done;
        }
    }

    bRet = TRUE;

done:
    LOGEXIT("SetEnvironmentVariableA returning BOOL %d\n", bRet);
    PERF_EXIT(SetEnvironmentVariableA);
    return bRet;
}


/*++
Function:
  MiscGetEnvArray

Get a reference to the process's environ array into palEnvironment

NOTE: This function MUST be called while holding the gcsEnvironment 
      critical section (except if the caller is the initialization 
      routine)
--*/
void
MiscGetEnvArray(void)
{
#if HAVE__NSGETENVIRON
    palEnvironment = *(_NSGetEnviron());
#else   // HAVE__NSGETENVIRON
    extern char **environ;
    palEnvironment = environ;
#endif  // HAVE__NSGETENVIRON
}

/*++
Function:
  MiscSetEnvArray

Make sure the process's environ array is in sync with palEnvironment variable

NOTE: This function MUST be called while holding the gcsEnvironment 
      critical section (except if the caller is the initialization 
      routine)
--*/
void
MiscSetEnvArray(void)
{
#if HAVE__NSGETENVIRON
    *(_NSGetEnviron()) = palEnvironment;
#else   // HAVE__NSGETENVIRON
    extern char **environ;
    environ = palEnvironment;
#endif  // HAVE__NSGETENVIRON
}


/*++
Function:
  EnvironInitialize

Initialization function called from PAL_Initialize.

Note: This is called before debug channels are initialized, so it
      cannot use debug tracing calls.
--*/
BOOL
EnvironInitialize(void)
{
    InternalInitializeCriticalSection(&gcsEnvironment);
    MiscGetEnvArray();

    return TRUE;
}

/*++

Function : _putenv.
    
See MSDN for more details.

Note:   The BSD implementation can cause
        memory leaks. See man pages for more details.
--*/
int
__cdecl 
_putenv( const char * envstring )
{
    int ret = -1;

    PERF_ENTRY(_putenv);
    ENTRY( "_putenv( %p (%s) )\n", envstring ? envstring : "NULL", envstring ? envstring : "NULL") ;
    
    if (!envstring)
    {
        ERROR( "_putenv() called with NULL envstring!\n");
        goto EXIT;
    }

    ret = EnvironPutenv(envstring, TRUE) ? 0 : -1;

EXIT:
    LOGEXIT( "_putenv returning %d\n", ret);
    PERF_EXIT(_putenv);
    return ret;
}

/*++

Function : PAL_getenv
    
See MSDN for more details.
--*/
char * __cdecl PAL_getenv(const char *varname)
{
    char *retval;

    PERF_ENTRY(getenv);
    ENTRY("getenv (%p (%s))\n", varname ? varname : "NULL", varname ? varname : "NULL");

    if (strcmp(varname, "") == 0)
    {
        ERROR("getenv called with a empty variable name\n");
        LOGEXIT("getenv returning NULL\n");
        PERF_EXIT(getenv);
        return(NULL);
    }
    retval = EnvironGetenv(varname);

    LOGEXIT("getenv returning %p\n", retval);
    PERF_EXIT(getenv);
    return(retval);
}


/*++
Function:
  EnvironGetenv

Gets an environment variable's value from environ. The returned buffer
must not be modified or freed.
--*/
char *EnvironGetenv(const char *name)
{
    int i, length;
    char *equals;
    char *pRet = NULL;
    CPalThread * pthrCurrent = InternalGetCurrentThread();

    InternalEnterCriticalSection(pthrCurrent, &gcsEnvironment);

    length = strlen(name);
    for(i = 0; palEnvironment[i] != NULL; i++)
    {
        if (memcmp(palEnvironment[i], name, length) == 0)
        {
            equals = palEnvironment[i] + length;
            if (*equals == '\0')
            {
                pRet = (char *) "";
                goto done;
            } 
            else if (*equals == '=') 
            {
                pRet = equals + 1;
                goto done;
            }
        }
    }

done:
    InternalLeaveCriticalSection(pthrCurrent, &gcsEnvironment);
    return pRet;
}

/*++
Function:
  EnvironPutenv

Sets an environment variable's value by directly modifying palEnvironment.
Returns TRUE if the variable was set, or FALSE if PAL_malloc or realloc
failed or if the given string is malformed.
--*/
BOOL EnvironPutenv(const char *string, BOOL deleteIfEmpty)
{
    const char *equals, *existingEquals;
    char *copy = NULL;
    int length;
    int i, j;
    bool fOwningCS = false;
    BOOL result = FALSE;
    CPalThread * pthrCurrent = InternalGetCurrentThread();
    
    equals = strchr(string, '=');
    if (equals == string || equals == NULL)
    {
        // "=foo" and "foo" have no meaning
        goto done;
    }
    if (equals[1] == '\0' && deleteIfEmpty)
    {
        // "foo=" removes foo from the environment in _putenv() on Windows.
        // The same string can result from a call to SetEnvironmentVariable()
        // with the empty string as the value, but in that case we want to
        // set the variable's value to "". deleteIfEmpty will be FALSE in
        // that case.
        length = strlen(string);
        copy = (char *) InternalMalloc(length);
        if (copy == NULL)
        {
            goto done;
        }
        memcpy(copy, string, length - 1);
        copy[length - 1] = '\0';    // Change '=' to '\0'
        EnvironUnsetenv(copy);
        result = TRUE;
    }
    else
    {
        // See if we are replacing an item or adding one.
        
        // Make our copy up front, since we'll use it either way.
        copy = strdup(string);
        if (copy == NULL)
        {
            goto done;
        }
        
        length = equals - string;

        InternalEnterCriticalSection(pthrCurrent, &gcsEnvironment);
        fOwningCS = true;
        
        for(i = 0; palEnvironment[i] != NULL; i++)
        {
            existingEquals = strchr(palEnvironment[i], '=');
            if (existingEquals == NULL)
            {
                // The PAL screens out malformed strings, but
                // environ comes from the system, so it might
                // have strings without '='. We treat the entire
                // string as a name in that case.
                existingEquals = palEnvironment[i] + strlen(palEnvironment[i]);
            }
            if (existingEquals - palEnvironment[i] == length)
            {
                if (memcmp(string, palEnvironment[i], length) == 0)
                {
                    // Replace this one. Don't free the original,
                    // though, because there may be outstanding
                    // references to it that were acquired via
                    // getenv. This is an unavoidable memory leak.
                    palEnvironment[i] = copy;
                    
                    // Set 'copy' to NULL so it won't be freed
                    copy = NULL;
                    
                    result = TRUE;
                    break;
                }
            }
        }
        if (palEnvironment[i] == NULL)
        {            
            static BOOL sAllocatedEnviron = FALSE;
            // Add a new environment variable.
            // We'd like to realloc palEnvironment, but we can't do that the
            // first time through.
            char **newEnviron = NULL;
            
            if (sAllocatedEnviron) {
                if (NULL == (newEnviron = 
                        (char **)InternalRealloc(palEnvironment, (i + 2) * sizeof(char *))))
                {
                    goto done;
                }
            }
            else
            {
                // Allocate palEnvironment ourselves so we can realloc it later.
                newEnviron = (char **)InternalMalloc((i + 2) * sizeof(char *));
                if (newEnviron == NULL)
                {
                    goto done;
                }
                for(j = 0; palEnvironment[j] != NULL; j++)
                {
                    newEnviron[j] = palEnvironment[j];
                }
                sAllocatedEnviron = TRUE;
            }
            palEnvironment = newEnviron;
            MiscSetEnvArray();
            palEnvironment[i] = copy;
            palEnvironment[i + 1] = NULL;

            // Set 'copy' to NULL so it won't be freed
            copy = NULL;
            
            result = TRUE;
        }
    }
done:

    if (fOwningCS)
    {
        InternalLeaveCriticalSection(pthrCurrent, &gcsEnvironment);
    }
    if (NULL != copy)
    {
        InternalFree(copy);
    }
    return result;
}

/*++
Function:
  EnvironUnsetenv

Removes a variable from the environment. Does nothing if the variable
does not exist in the environment.
--*/
void EnvironUnsetenv(const char *name)
{
    const char *equals;
    int length;
    int i, j;
    CPalThread * pthrCurrent = InternalGetCurrentThread();
    
    length = strlen(name);

    InternalEnterCriticalSection(pthrCurrent, &gcsEnvironment);
    for(i = 0; palEnvironment[i] != NULL; i++)
    {
        equals = strchr(palEnvironment[i], '=');
        if (equals == NULL)
        {
            equals = palEnvironment[i] + strlen(palEnvironment[i]);
        }
        if (equals - palEnvironment[i] == length)
        {
            if (memcmp(name, palEnvironment[i], length) == 0)
            {
                // Remove this one. Don't free it, though, since
                // there might be oustanding references to it that
                // were acquired via getenv. This is an
                // unavoidable memory leak.
                for(j = i + 1; palEnvironment[j] != NULL; j++) { }
                // i is now the one we want to remove. j is the
                // last index in palEnvironment, which is NULL.

                // Shift palEnvironment down by the difference between i and j.
                memmove(palEnvironment + i, palEnvironment + i + 1, (j - i) * sizeof(char *));
            }
        }
    }
    InternalLeaveCriticalSection(pthrCurrent, &gcsEnvironment);
}
