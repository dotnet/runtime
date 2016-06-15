// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <windows.h>

#if defined(_M_IX86)
#include <winnt.h>
#endif

extern "C" void __cdecl __security_init_cookie(void);

extern "C" BOOL WINAPI _DllMainCRTStartup(HANDLE hDllHandle, DWORD dwReason, LPVOID lpreserved)
{
    if (dwReason == DLL_PROCESS_ATTACH)
    {
        __security_init_cookie();
        DisableThreadLibraryCalls((HMODULE)hDllHandle);
    }

    return TRUE;
}

extern "C" void* __cdecl malloc(size_t size)
{
    return CoTaskMemAlloc(size);
}

extern "C" void __cdecl free(PVOID block)
{
    CoTaskMemFree(block);
}

extern "C" __declspec(noreturn) void __cdecl __report_rangecheckfailure(void)
{
    __fastfail(FAST_FAIL_RANGE_CHECK_FAILURE);
}

#if defined(_M_IX86)
extern "C" __declspec(noreturn) void __cdecl __report_gsfailure(void)
#else
extern "C" __declspec(noreturn) void __cdecl __report_gsfailure(uintptr_t StackCookie)
#endif
{
    __fastfail(FAST_FAIL_STACK_COOKIE_CHECK_FAILURE);
}

/*
 * Union to facilitate converting from FILETIME to unsigned __int64
 */
typedef union {
    unsigned __int64 ft_scalar;
    FILETIME ft_struct;
} FT;

/*
 * Default value used for the global /GS security cookie, defined here and
 * in gs_support.c (since standalone SDK build can't use CRT's internal.h).
 */

#ifdef  _WIN64
#define DEFAULT_SECURITY_COOKIE 0x00002B992DDFA232
#else
#define DEFAULT_SECURITY_COOKIE 0xBB40E64E
#endif

/*
 * The global security cookie.  This name is known to the compiler.
 * Initialize to a garbage non-zero value just in case we have a buffer overrun
 * in any code that gets run before __security_init_cookie() has a chance to
 * initialize the cookie to the final value.
 */

extern "C" DECLSPEC_SELECTANY UINT_PTR __security_cookie = DEFAULT_SECURITY_COOKIE;

extern "C" DECLSPEC_SELECTANY UINT_PTR __security_cookie_complement = ~(DEFAULT_SECURITY_COOKIE);

/***
*__security_init_cookie(cookie) - init buffer overrun security cookie.
*
*Purpose:
*       Initialize the global buffer overrun security cookie which is used by
*       the /GS compile switch to detect overwrites to local array variables
*       the potentially corrupt the return address.  This routine is called
*       at EXE/DLL startup.
*
*Entry:
*
*Exit:
*
*Exceptions:
*
*******************************************************************************/

//
// We don't want define any local variables inside __security_init_cookie
// because doing so make the compiler generate cookie checking on __security_init_cookie
// which for sure will fail because __security_init_cookie will change the cookie 
// value
// 
    UINT_PTR cookie;
    FT systime={0};
    LARGE_INTEGER perfctr;
#if defined(CHECK_FOR_LATE_COOKIE_INIT)
    PEXCEPTION_REGISTRATION_RECORD ehrec;
#endif

extern "C" void __cdecl __security_init_cookie(void)
{
    /*
     * Do nothing if the global cookie has already been initialized.  On x86,
     * reinitialize the cookie if it has been previously initialized to a
     * value with the high word 0x0000.  Some versions of Windows will init
     * the cookie in the loader, but using an older mechanism which forced the
     * high word to zero.
     */

    if (__security_cookie != DEFAULT_SECURITY_COOKIE
#if defined(_M_IX86)
        && (__security_cookie & 0xFFFF0000) != 0
#endif
       )
    {
        __security_cookie_complement = ~__security_cookie;
        return;
    }

#if defined(CHECK_FOR_LATE_COOKIE_INIT)
    /*
     * The security cookie is used to ensure the integrity of exception
     * handling.  That means the cookie must be initialized in an image before
     * that image registers an exception handler that will use the cookie.  We
     * attempt to detect this situation by checking if the calling thread has
     * already registered SEH handler _except_handler4.
     */

    for (ehrec = (PEXCEPTION_REGISTRATION_RECORD)(UINT_PTR)
                    __readfsdword(FIELD_OFFSET(NT_TIB, ExceptionList));
         ehrec != EXCEPTION_CHAIN_END;
         ehrec = ehrec->Next)
    {
        if (ehrec->Handler == &_except_handler4)
        {
            /*
             * We've detected __except_handler4, alert the user by issuing
             * error R6035 and terminate the process.  We use FatalAppExitW
             * instead of _NMSG_WRITE because the loader lock may be held now,
             * and _NMSG_WRITE may do GetLibrary("user32.dll") for MessageBox,
             * which is unsafe under the loader lock.
             */
            FatalAppExitW(0, _RT_COOKIE_INIT_TXT);
        }

        if (ehrec >= ehrec->Next)
        {
            /*
             * Nodes in the exception record linked list are supposed to appear
             * at increasing addresses within the thread's stack.  If the next
             * node is at a lower address, then the exception list is corrupted,
             * and we stop our check to avoid hitting a cycle in the list.
             */

            break;
        }
    }

#endif

    /*
     * Initialize the global cookie with an unpredictable value which is
     * different for each module in a process.  Combine a number of sources
     * of randomness.
     */

    GetSystemTimeAsFileTime(&systime.ft_struct);
#if defined(_WIN64)
    cookie = systime.ft_scalar;
#else
    cookie = systime.ft_struct.dwLowDateTime;
    cookie ^= systime.ft_struct.dwHighDateTime;
#endif

    cookie ^= GetCurrentThreadId();

#if defined(_WIN64)
    cookie ^= (((UINT_PTR) GetTickCount64()) << 24);
#endif
    cookie ^= (UINT_PTR) GetTickCount64();

    QueryPerformanceCounter(&perfctr);
#if defined(_WIN64)
    cookie ^= (((UINT_PTR)perfctr.LowPart << 32) ^ perfctr.QuadPart);
#else
    cookie ^= perfctr.LowPart;
    cookie ^= perfctr.HighPart;
#endif

    /*
     * Increase entropy using ASLR relocation
     */
    cookie ^= (UINT_PTR)&cookie;

#if defined(_WIN64)
    /*
     * On Win64, generate a cookie with the most significant word set to zero,
     * as a defense against buffer overruns involving null-terminated strings.
     * Don't do so on Win32, as it's more important to keep 32 bits of cookie.
     */
    cookie &= 0x0000FFFFffffFFFFi64;
#endif

    /*
     * Make sure the cookie is initialized to a value that will prevent us from
     * reinitializing it if this routine is ever called twice.
     */

    if (cookie == DEFAULT_SECURITY_COOKIE)
    {
        cookie = DEFAULT_SECURITY_COOKIE + 1;
    }
#if defined(_M_IX86)
    else if ((cookie & 0xFFFF0000) == 0)
    {
        cookie |= ( (cookie|0x4711) << 16);
    }
#endif

    __security_cookie = cookie;
    __security_cookie_complement = ~cookie;

}
