// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

#if HAVE_CRT_EXTERNS_H
#include <crt_externs.h>
#endif

#include <stdlib.h>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(MISC);

char **palEnvironment = nullptr;
int palEnvironmentCount = 0;
int palEnvironmentCapacity = 0;

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

    CPalThread * pthrCurrent = InternalGetCurrentThread();

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
        // Enter the environment critical section so that we can safely get
        // the environment variable value without EnvironGetenv making an
        // intermediate copy. We will just copy the string to the output
        // buffer anyway, so just stay in the critical section until then.
        InternalEnterCriticalSection(pthrCurrent, &gcsEnvironment);

        value = EnvironGetenv(lpName, /* copyValue */ FALSE);

        if (value != nullptr)
        {
            DWORD valueLength = strlen(value);
            if (valueLength < nSize)
            {
                strcpy_s(lpBuffer, nSize, value);
                dwRet = valueLength;
            }
            else
            {
                dwRet = valueLength + 1;
            }

            SetLastError(ERROR_SUCCESS);
        }

        InternalLeaveCriticalSection(pthrCurrent, &gcsEnvironment);
    }

    if (value == nullptr)
    {
        TRACE("%s is not found\n", lpName);
        SetLastError(ERROR_ENVVAR_NOT_FOUND);
    }

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
                                   inBuffSize, nullptr, nullptr))
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
        // If size is 0, it either means GetEnvironmentVariableA failed, or that
        // it succeeded and the value of the variable is empty. Check GetLastError
        // to determine which. If the call failed, we won't touch the buffer.
        if (GetLastError() == ERROR_SUCCESS)
        {
            *lpBuffer = '\0';
        }
    }
    else
    {
        size = MultiByteToWideChar(CP_ACP, 0, outBuff, -1, lpBuffer, nSize);
        if (0 != size)
        {
            // -1 for the null.
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
    free(outBuff);
    free(inBuff);

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
       and lpValue is not null.
lpValue
       [in] Pointer to a null-terminated string containing the new
       value of the specified environment variable. If this parameter
       is null, the variable is deleted from the current process's
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
                                             0, nullptr, nullptr)) == 0)
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
                                     value, valueSize, nullptr, nullptr))
        {
            ASSERT("WideCharToMultiByte failed\n");
            SetLastError( ERROR_INTERNAL_ERROR );
            goto done;
        }
    }

    bRet = SetEnvironmentVariableA(name, value);
done:
    free(value);
    free(name);

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

    CPalThread * pthrCurrent = InternalGetCurrentThread();
    InternalEnterCriticalSection(pthrCurrent, &gcsEnvironment);

    envNum = 0;
    len    = 0;

    /* get total length of the bytes that we need to allocate */
    for (i = 0; palEnvironment[i] != 0; i++)
    {
        len = MultiByteToWideChar(CP_ACP, 0, palEnvironment[i], -1, wenviron, 0);
        envNum += len;
    }

    wenviron = (WCHAR *)PAL_malloc(sizeof(WCHAR)* (envNum + 1));
    if (wenviron == nullptr)
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

    *tempEnviron = 0; /* Put an extra null at the end */

 EXIT:
    InternalLeaveCriticalSection(pthrCurrent, &gcsEnvironment);

    LOGEXIT("GetEnvironmentStringsW returning %p\n", wenviron);
    PERF_EXIT(GetEnvironmentStringsW);
    return wenviron;
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
    ENTRY("FreeEnvironmentStringsW(lpValue=%p (%S))\n", lpValue ? lpValue : W16_NULLSTRING, lpValue ? lpValue : W16_NULLSTRING);

    if (lpValue != nullptr)
    {
        free(lpValue);
    }

    LOGEXIT("FreeEnvironmentStringW returning BOOL TRUE\n");
    PERF_EXIT(FreeEnvironmentStringsW);
    return TRUE;
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
       and lpValue is not null.
lpValue
       [in] Pointer to a null-terminated string containing the new
       value of the specified environment variable. If this parameter
       is null, the variable is deleted from the current process's
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
        ERROR("lpName is null\n");
        goto done;
    }

    /* check if the input value is null and if so
     * check if the input name is valid and delete
     * the variable name from process environment */
    if (lpValue == nullptr)
    {
        // We tell EnvironGetenv not to bother with making a copy of the
        // value since we're not going to use it for anything interesting
        // apart from checking whether it's null.
        if ((lpValue = EnvironGetenv(lpName, /* copyValue */ FALSE)) == nullptr)
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

        free(string);
        string = nullptr;

        // If EnvironPutenv returns FALSE, it almost certainly failed to allocate memory.
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
  ResizeEnvironment

Resizes the PAL environment buffer.

Parameters

    newSize
           [in] New size of palEnvironment

Return Values

    TRUE on success, FALSE otherwise

--*/
BOOL ResizeEnvironment(int newSize)
{
    CPalThread * pthrCurrent = InternalGetCurrentThread();
    InternalEnterCriticalSection(pthrCurrent, &gcsEnvironment);

    BOOL ret = FALSE;
    if (newSize >= palEnvironmentCount)
    {
        // If palEnvironment is null, realloc acts like malloc.
        char **newEnvironment = (char**)realloc(palEnvironment, newSize * sizeof(char *));
        if (newEnvironment != nullptr)
        {
            // realloc succeeded, so set palEnvironment to what it returned.
            palEnvironment = newEnvironment;
            palEnvironmentCapacity = newSize;
            ret = TRUE;
        }
    }
    else
    {
        ASSERT("ResizeEnvironment: newSize < current palEnvironmentCount!\n");
    }

    InternalLeaveCriticalSection(pthrCurrent, &gcsEnvironment);
    return ret;
}

/*++
Function:
  EnvironUnsetenv

Remove the environment variable with the given name from the PAL version
of the environment if it exists.

Parameters

    name
           [in] Name of variable to unset.

--*/
void EnvironUnsetenv(const char *name)
{
    int nameLength = strlen(name);

    CPalThread * pthrCurrent = InternalGetCurrentThread();
    InternalEnterCriticalSection(pthrCurrent, &gcsEnvironment);

    for (int i = 0; palEnvironment[i] != nullptr; ++i)
    {
        const char *equalsSignPosition = strchr(palEnvironment[i], '=');
        if (equalsSignPosition == nullptr)
        {
            equalsSignPosition = palEnvironment[i] + strlen(palEnvironment[i]);
        }

        // Check whether the name of this variable has the same length as the one
        // we're looking for before proceeding to compare them.
        if (equalsSignPosition - palEnvironment[i] == nameLength)
        {
            if (memcmp(name, palEnvironment[i], nameLength) == 0)
            {
                // Free the string we're removing.
                free(palEnvironment[i]);

                // Move the last environment variable pointer here.
                palEnvironment[i] = palEnvironment[palEnvironmentCount - 1];
                palEnvironment[palEnvironmentCount - 1] = nullptr;

                palEnvironmentCount--;
            }
        }
    }

    InternalLeaveCriticalSection(pthrCurrent, &gcsEnvironment);
}

/*++
Function:
  EnvironPutenv

Add the environment variable string provided to the PAL version
of the environment.

Parameters

    entry
            [in] The variable string to add. Should be in the format
                 "name=value", where value might be empty (see below).
    deleteIfEmpty
            [in] If this is TRUE, "name=" will unset the 'name' variable.

Return Values

    TRUE on success, FALSE otherwise

--*/
BOOL EnvironPutenv(const char* entry, BOOL deleteIfEmpty)
{
    BOOL result = FALSE;

    bool fOwningCS = false;

    CPalThread * pthrCurrent = InternalGetCurrentThread();

    const char *equalsSignPosition = strchr(entry, '=');
    if (equalsSignPosition == entry || equalsSignPosition == nullptr)
    {
        // "=foo" and "foo" have no meaning
        return FALSE;
    }

    char* copy = strdup(entry);
    if (copy == nullptr)
    {
        return FALSE;
    }

    int nameLength = equalsSignPosition - entry;

    if (equalsSignPosition[1] == '\0' && deleteIfEmpty)
    {
        // "foo=" removes foo from the environment in _putenv() on Windows.
        // The same string can result from a call to SetEnvironmentVariable()
        // with the empty string as the value, but in that case we want to
        // set the variable's value to "". deleteIfEmpty will be FALSE in
        // that case.

        // Change '=' to '\0'
        copy[nameLength] = '\0';

        EnvironUnsetenv(copy);
        free(copy);

        result = TRUE;
    }
    else
    {
        // See if we are replacing an item or adding one.

        InternalEnterCriticalSection(pthrCurrent, &gcsEnvironment);
        fOwningCS = true;

        int i;
        for (i = 0; palEnvironment[i] != nullptr; i++)
        {
            const char *existingEquals = strchr(palEnvironment[i], '=');
            if (existingEquals == nullptr)
            {
                // The PAL screens out malformed strings, but the strings which
                // came from the system during initialization might not have the
                // equals sign. We treat the entire string as a name in that case.
                existingEquals = palEnvironment[i] + strlen(palEnvironment[i]);
            }

            if (existingEquals - palEnvironment[i] == nameLength)
            {
                if (memcmp(entry, palEnvironment[i], nameLength) == 0)
                {
                    free(palEnvironment[i]);
                    palEnvironment[i] = copy;

                    result = TRUE;
                    break;
                }
            }
        }

        if (palEnvironment[i] == nullptr)
        {
            _ASSERTE(i < palEnvironmentCapacity);
            if (i == (palEnvironmentCapacity - 1))
            {
                // We found the first null, but it's the last element in our environment
                // block. We need more space in our environment, so let's double its size.
                int resizeRet = ResizeEnvironment(palEnvironmentCapacity * 2);
                if (resizeRet != TRUE)
                {
                    free(copy);
                    goto done;
                }
            }

            _ASSERTE(copy != nullptr);
            palEnvironment[i] = copy;
            palEnvironment[i + 1] = nullptr;
            palEnvironmentCount++;

            result = TRUE;
        }
    }
done:

    if (fOwningCS)
    {
        InternalLeaveCriticalSection(pthrCurrent, &gcsEnvironment);
    }

    return result;
}


/*++
Function:
  FindEnvVarValue

Get the value of environment variable with the given name.
Caller should take care of locking and releasing palEnvironment.

Parameters

    name
            [in] The name of the environment variable to get.

Return Value

    A pointer to the value of the environment variable if it exists,
    or nullptr otherwise.

--*/
char* FindEnvVarValue(const char* name)
{
    if (*name == '\0')
        return nullptr;

    for (int i = 0; palEnvironment[i] != nullptr; ++i)
    {
        const char* pch = name;
        char* p = palEnvironment[i];

        do
        {
            if (*pch == '\0')
            {
                if (*p == '=')
                    return p + 1;

                if (*p == '\0') // no = sign -> empty value
                    return p;

                break;
            }
        }
        while (*pch++ == *p++);
    }

    return nullptr;
}


/*++
Function:
  EnvironGetenv

Get the value of environment variable with the given name.

Parameters

    name
            [in] The name of the environment variable to get.
    copyValue
            [in] If this is TRUE, the function will make a copy of the
                 value and return a pointer to that. Otherwise, it will
                 return a pointer to the value in the PAL environment
                 directly. Calling this function with copyValue set to
                 FALSE is therefore unsafe without taking special pre-
                 cautions since the pointer may point to garbage later.

Return Value

    A pointer to the value of the environment variable if it exists,
    or nullptr otherwise.

--*/
char* EnvironGetenv(const char* name, BOOL copyValue)
{
    CPalThread * pthrCurrent = InternalGetCurrentThread();
    InternalEnterCriticalSection(pthrCurrent, &gcsEnvironment);

    char* retValue = FindEnvVarValue(name);

    if ((retValue != nullptr) && copyValue)
    {
        retValue = strdup(retValue);
    }

    InternalLeaveCriticalSection(pthrCurrent, &gcsEnvironment);
    return retValue;
}


/*++
Function:
  EnvironGetSystemEnvironment

Get a pointer to the array of pointers representing the process's
environment.

See 'man environ' for details.

Return Value

    A pointer to the environment.

--*/
char** EnvironGetSystemEnvironment()
{
    char** sysEnviron;

#if HAVE__NSGETENVIRON
    sysEnviron = *(_NSGetEnviron());
#else   // HAVE__NSGETENVIRON
    extern char **environ;
    sysEnviron = environ;
#endif  // HAVE__NSGETENVIRON

    return sysEnviron;
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
    BOOL ret = FALSE;

    InternalInitializeCriticalSection(&gcsEnvironment);

    CPalThread * pthrCurrent = InternalGetCurrentThread();
    InternalEnterCriticalSection(pthrCurrent, &gcsEnvironment);

    char** sourceEnviron = EnvironGetSystemEnvironment();

    int variableCount = 0;
    while (sourceEnviron[variableCount] != nullptr)
        variableCount++;

    palEnvironmentCount = 0;

    // We need to decide how much space to allocate. Since we need enough
    // space for all of the 'n' current environment variables, but we don't
    // know how many more there will be, we will initially make room for
    // '2n' variables. If even more are added, we will resize again.
    // If there are no variables, we will still make room for 1 entry to
    // store a nullptr there.
    int initialSize = (variableCount == 0) ? 1 : variableCount * 2;

    ret = ResizeEnvironment(initialSize);
    if (ret == TRUE)
    {
        _ASSERTE(palEnvironment != nullptr);
        for (int i = 0; i < variableCount; ++i)
        {
            palEnvironment[i] = strdup(sourceEnviron[i]);
            palEnvironmentCount++;
        }

        // Set the entry after the last variable to null to indicate the end.
        palEnvironment[variableCount] = nullptr;
    }

    InternalLeaveCriticalSection(pthrCurrent, &gcsEnvironment);
    return ret;
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

    if (envstring != nullptr)
    {
        ret = EnvironPutenv(envstring, TRUE) ? 0 : -1;
    }
    else
    {
        ERROR( "_putenv() called with NULL envstring!\n");
    }

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
