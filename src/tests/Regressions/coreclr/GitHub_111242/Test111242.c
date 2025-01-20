// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdint.h>
#include <stdio.h>
#include <setjmp.h>


#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif // _MSC_VER

DLLEXPORT void TestSetJmp(void (*managedCallback)(void *))
{
    jmp_buf jmpBuf;
    if (!setjmp(jmpBuf))
    {
        managedCallback(&jmpBuf);
    }
    else
    {
        printf("longjmp called\n");
    }
}

DLLEXPORT void TestLongJmp(void *jmpBuf)
{
    longjmp(*(jmp_buf*)jmpBuf, 1);
}
