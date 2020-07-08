// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// FPCW.cpp : Defines the exported functions for the DLL application.
//

#include "fpcw.h"
#include <stdio.h>
#include <float.h>

LONG WINAPI ExcepFilter(struct _EXCEPTION_POINTERS *pExp)
{
   printf( "In ExcepFilter \n" );

   if (pExp->ExceptionRecord->ExceptionCode == 0xc00002b5 ||
    pExp->ExceptionRecord->ExceptionCode == 0xc00002b4 )
     return EXCEPTION_EXECUTE_HANDLER;
   else
     return EXCEPTION_CONTINUE_SEARCH;
}

// This is an example of an exported function.
// Disable optimization otherwise compiler removes dividebyzero operation
#pragma optimize( "", off )

int TestDivByZero(void)
{
    unsigned int fpcw;

    // DivByZero
    printf("DivByZero: ");

    // Enable all FP exceptions
    int err = _controlfp_s(&fpcw, 0, _MCW_EM);
    if (err)
    {
        printf("Error setting FPCW: %d\n", err);
        return -1;
    }

    __try
    {
        __try
        {
            float d = 2.0 - (0.0+2.0);
            float f = 2.0f/d;
            printf("Shouldn't execute!\n");
            (void)f; // nop to disable warning C4189
            return 99;
        }
        __except((GetExceptionCode()==0xc000008e)?EXCEPTION_EXECUTE_HANDLER:EXCEPTION_CONTINUE_SEARCH)
        {
            printf("Caught exception!\n");
            
            // Clear FPSW else the "waiting" FPU instructions used in _controlfp_s will result in
            // FPU raising the exception again.
            _clearfp();

            // Reset FPCW
            err = _controlfp_s(&fpcw, 0x1f, _MCW_EM);
            if (err)
            {
                printf("Error setting FPCW: %d\n", err);
                return -1;
            }

            printf("Passed\n");
        }
    }
    __except(ExcepFilter(GetExceptionInformation()))
    {
        printf("Caught exception in Filter!\n");

        // Clear FPSW else the "waiting" FPU instructions used in _controlfp_s will result in
        // FPU raising the exception again.
        _clearfp();

        // Reset FPCW
        err = _controlfp_s(&fpcw, 0x1f, _MCW_EM);
        if (err)
        {
            printf("Error setting FPCW: %d\n", err);
            return -1;
        }
    }

    return 100;
}

int TestUnderflow(void)
{
    unsigned int fpcw;

    // Underflow
    printf("Underflow: ");

    // Enable all FP exceptions
    int err = _controlfp_s(&fpcw, 0, _MCW_EM);
    if (err)
    {
        printf("Error setting FPCW: %d\n", err);
        return -1;
    }

    __try
    {
        __try
        {
            double a = 1e-40, b;
            float  y;
            y = (float)a;
            b = 2.0;
            printf("Shouldn't execute!\n");
            return 98;
        }
        __except((GetExceptionCode()==0xc0000093)?EXCEPTION_EXECUTE_HANDLER:EXCEPTION_CONTINUE_SEARCH)
        {
            printf("Caught exception!\n");
            
            // Clear FPSW else the "waiting" FPU instructions used in _controlfp_s will result in
            // FPU raising the exception again.
            _clearfp();

            // Reset FPCW
            err = _controlfp_s(&fpcw, 0x1f, _MCW_EM);
            if (err)
            {
                printf("Error setting FPCW: %d\n", err);
                return -1;
            }
            printf("Passed\n");
        }
    }
    __except(ExcepFilter(GetExceptionInformation()))
    {
        printf("Caught exception in Filter!\n");
        // Clear FPSW else the "waiting" FPU instructions used in _controlfp_s will result in
        // FPU raising the exception again.
        _clearfp();

        // Reset FPCW
        err = _controlfp_s(&fpcw, 0x1f, _MCW_EM);
        if (err)
        {
            printf("Error setting FPCW: %d\n", err);
            return -1;
        }
    }

    return 100;
}

int TestOverflow(void)
{
    unsigned int fpcw;

    // Overflow
    printf("Overflow: ");

    // Enable all FP exceptions
    int err = _controlfp_s(&fpcw, 0, _MCW_EM);
    if (err)
    {
        printf("Error setting FPCW: %d\n", err);
        return -1;
    }

    __try
    {
        __try
        {
            double a = 1e+40, b;
            float  y;
            y = (float)a;
            b = 2.0;
            printf("Shouldn't execute!\n");
            return 97;
        }
        __except((GetExceptionCode()==0xc0000091)?EXCEPTION_EXECUTE_HANDLER:EXCEPTION_CONTINUE_SEARCH)
        {
            printf("Caught exception!\n");
            
            // Clear FPSW else the "waiting" FPU instructions used in _controlfp_s will result in
            // FPU raising the exception again.
            _clearfp();

            // Reset FPCW
            err = _controlfp_s(&fpcw, 0x1f, _MCW_EM);
            if (err)
            {
                printf("Error setting FPCW: %d\n", err);
                return -1;
            }
            printf("Passed\n");
        }
    }
    __except(ExcepFilter(GetExceptionInformation()))
    {
        printf("Caught exception in Filter!\n");
     
        // Clear FPSW else the "waiting" FPU instructions used in _controlfp_s will result in
        // FPU raising the exception again.
        _clearfp();

        // Reset FPCW
        err = _controlfp_s(&fpcw, 0x1f, _MCW_EM);
        if (err)
        {
            printf("Error setting FPCW: %d\n", err);
            return -1;
        }
    }

    return 100;
}

extern "C" FPCW_API int RaiseFPException(void)
{
    int result = TestDivByZero();

    if (result != 100)
    {
        return result;
    }

    result = TestUnderflow();

    if (result != 100)
    {
        return result;
    }

    return TestOverflow();
}
