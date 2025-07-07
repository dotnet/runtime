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
#include "pal/dbgmsg.h"
#include "pal/environ.h"
#include "minipal/env.h"

#include <stdlib.h>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(MISC);

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
        goto done;
    }
    else
    {
        size_t valueLength = 0;
        if (minipal_env_get_s(&valueLength, lpBuffer, nSize, lpName))
        {
            // minipal_env_get_s returns the length of the value including the null terminator.
            if (valueLength > 0)
            {
                valueLength--;
            }

            if (valueLength < nSize)
            {
                // The value fits in the buffer, so return the length of the value without the null terminator.
                dwRet = (DWORD)valueLength;
            }
            else
            {
                // The value doesn't fit in the buffer, so return the size required to hold it including the null terminator.
                dwRet = (DWORD)(valueLength + 1);
            }

            SetLastError(ERROR_SUCCESS);
        }
        else
        {
            TRACE("%s is not found\n", lpName);
            SetLastError(ERROR_ENVVAR_NOT_FOUND);
        }
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

    inBuff = (CHAR *)malloc(inBuffSize);
    if (inBuff == nullptr)
    {
        ERROR("malloc failed\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }

    if (nSize)
    {
        outBuff = (CHAR *)malloc(nSize*2);
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

    name = (PCHAR)malloc(sizeof(CHAR)* nameSize);
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

        value = (PCHAR)malloc(sizeof(CHAR)*valueSize);

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

typedef struct _EnvironmentStringsCallbackCookie
{
    WCHAR* wenviron;
    WCHAR* tempEnviron;
    int envNum;
} EnvironmentStringsCallbackCookie;

bool EnvironmentStringsCallback(const char* s, void* cookie)
{
    EnvironmentStringsCallbackCookie* envStrings = (EnvironmentStringsCallbackCookie*)cookie;
    if (envStrings != nullptr && s != nullptr)
    {
        if (envStrings->wenviron != nullptr)
        {
            int len = MultiByteToWideChar(CP_ACP, 0, s, -1, envStrings->tempEnviron, envStrings->envNum);
            envStrings->tempEnviron += len;
            envStrings->envNum      -= len;
        }
        else
        {
            int len = MultiByteToWideChar(CP_ACP, 0, s, -1, nullptr, 0);
            envStrings->envNum += len;
        }
    }

    return true;
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
    PERF_ENTRY(GetEnvironmentStringsW);
    ENTRY("GetEnvironmentStringsW()\n");

    EnvironmentStringsCallbackCookie envStrings = { 0 };
    minipal_env_foreach(EnvironmentStringsCallback, &envStrings);

    envStrings.wenviron = (WCHAR *)malloc(sizeof(WCHAR)* (envStrings.envNum + 1));
    if (envStrings.wenviron == nullptr)
    {
        ERROR("malloc failed\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    envStrings.tempEnviron = envStrings.wenviron;
    minipal_env_foreach(EnvironmentStringsCallback, &envStrings);

    *envStrings.tempEnviron = 0; /* Put an extra null at the end */

 EXIT:

    LOGEXIT("GetEnvironmentStringsW returning %p\n", envStrings.wenviron);
    PERF_EXIT(GetEnvironmentStringsW);
    return envStrings.wenviron;
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
        if (!minipal_env_exists(lpName))
        {
            ERROR("Couldn't find environment variable (%s)\n", lpName);
            SetLastError(ERROR_ENVVAR_NOT_FOUND);
            goto done;
        }

        minipal_env_unset(lpName);
    }
    else
    {
        if (!minipal_env_set(lpName, lpValue, true))
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
  EnvironUnsetenv

Remove the environment variable with the given name from the PAL version
of the environment if it exists.

Parameters

    name
           [in] Name of variable to unset.

--*/
void EnvironUnsetenv(const char *name)
{
    minipal_env_unset(name);
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

    const char *equalsSignPosition = strchr(entry, '=');
    if (equalsSignPosition == entry || equalsSignPosition == nullptr)
    {
        // "=foo" and "foo" have no meaning
        return FALSE;
    }

    int nameLength = equalsSignPosition - entry;

    if (equalsSignPosition[1] == '\0' && deleteIfEmpty)
    {
        char* copy = strdup(entry);
        if (copy == nullptr)
        {
            return FALSE;
        }

        // "foo=" removes foo from the environment in _putenv() on Windows.
        // The same string can result from a call to SetEnvironmentVariable()
        // with the empty string as the value, but in that case we want to
        // set the variable's value to "". deleteIfEmpty will be FALSE in
        // that case.

        // Change '=' to '\0'
        copy[nameLength] = '\0';

        minipal_env_unset(copy);
        free(copy);

        result = TRUE;
    }
    else
    {
        // See if we are replacing an item or adding one.
        result = minipal_env_put(entry) ? TRUE : FALSE;
    }

    return result;
}

/*++
Function:
  EnvironCheckenv

Check if environment variable with the given name exists in environment.

Parameters

    name
            [in] The name of the environment variable to check.

Return Value

    Returns TRUE if environment variable exists in environment,
    otherwise FALSE.

--*/
BOOL EnvironCheckenv(const char* name)
{
    return minipal_env_exists(name) ? TRUE : FALSE;
}

/*++
Function:
  EnvironGetenv

Get the value of environment variable with the given name.
Caller should free the returned string if it is not NULL.

Parameters

    name
            [in] The name of the environment variable to get.

Return Value

    A pointer to the value of the environment variable if it exists,
    or nullptr otherwise.

--*/
char* EnvironGetenv(const char* name)
{
    return minipal_env_get(name);
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
    return minipal_env_load_environ() ? TRUE : FALSE;
}

/*++
Function:
  EnvironGetUnsafe

Get the current environment. This is similar accessing
global environ variable and is not thread safe. This function
should only be called from code that guarantees environment won't
change while using returned pointer.

Return Value

    A pointer to the environment. NOTE, caller should never manipulate
    or free returned pointer.
--*/
char** EnvironGetUnsafe(void)
{
    return minipal_env_get_environ_unsafe();
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
