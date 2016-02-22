// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  exceptionsxs.c (exception_handling\pal_sxs\test1)
**
** Purpose: Test to make sure the PAL_EXCEPT block is executed
**          after an exception occurs in the PAL_TRY block with
**          multiple PALs in the process.
**
**
**===================================================================*/

extern "C" int InitializeDllTest1();
extern "C" int InitializeDllTest2();
extern "C" int DllTest1();
extern "C" int DllTest2();

int main(int argc, char *argv[])
{
#if !defined(__FreeBSD__) && !defined(__NetBSD__)
    if (0 != InitializeDllTest1())
    {
        return 1;
    }

    if (0 != InitializeDllTest2())
    {
        return 1;
    }

    DllTest2();
    DllTest1();
    DllTest2();
#endif
    return 0;
}
