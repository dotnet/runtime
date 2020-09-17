// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: paltests.cpp
**
** Purpose: Entrypoint for all the pal tests. Written to avoid any
**          standard library usage
**
**============================================================*/
#include <palsuite.h>

PALTest* PALTest::s_tests = 0;

int PrintUsage(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    printf("paltests <PrintPalTests|TestName>\n");
    printf("Either print list of all paltests by passing PrintPalTests, or run a single PAL test.\n");

    PAL_TerminateEx(FAIL);
    return FAIL;
}

int PrintTests(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    PALTest *testCur = PALTest::s_tests;
    for (;testCur != 0; testCur = testCur->_next)
    {
        printf("%s\n", testCur->_name);
    }
    PAL_Terminate();
    return PASS;
}

int __cdecl main(int argc, char *argv[])
{
    if (argc < 2)
    {
        return PrintUsage(argc, argv);
    }

    if (strcmp(argv[1], "PrintPalTests") == 0)
    {
        return PrintTests(argc, argv);
    }
    
    PALTest *testCur = PALTest::s_tests;
    for (;testCur != 0; testCur = testCur->_next)
    {
        int i = 0;
        bool stringMatches = strcmp(testCur->_name, argv[1]) == 0;
        if (!stringMatches)
            continue;

        for (int i = 1; i < (argc - 1); i++)
        {
            argv[i] = argv[i + 1];
        }

        return testCur->_entrypoint(argc - 1, argv);
    }

    return PrintUsage(argc, argv);
}
