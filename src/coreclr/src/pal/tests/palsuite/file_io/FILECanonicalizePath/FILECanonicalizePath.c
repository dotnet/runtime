//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  FILECanonicalizePath.c (test 1)
**
** Purpose: Tests the PAL implementation of the FILECanonicalizePath function.
**
**
**===================================================================*/

#include <palsuite.h>

extern void FILECanonicalizePath(LPSTR lpUnixPath);

void TestCase(LPSTR input, LPSTR expectedOutput);

int __cdecl main(int argc, char *argv[])
{
    if (PAL_Initialize(argc,argv) != 0)
    {
        return FAIL;
    }

    // Case 01: /<name> should not change
    TestCase("/Test", "/Test");

    // Case 02: /<name>/<name2> should not change
    TestCase("/Test/Foo", "/Test/Foo");

    // Case 03: // transforms to /
    TestCase("//", "/");
  
    // Case 04: /./ transforms to /
    TestCase("/./", "/");
    
    // Case 05: /<name>/../ transforms to /
    TestCase("/Test/../", "/");
        
    // Case 06: /Test/Foo/.. transforms to /Test
    TestCase("/Test/Foo/..", "/Test");
        
    // Case 07: /Test/.. transforms to /
    TestCase("/Test/..", "/");
        
    // Case 08: /. transforms to /
    TestCase("/.", "/");
        
    // Case 09: /<name/. transforms to /<name>
    TestCase("/Test/.", "/Test");
    
    // Case 10: /<name>/../. transforms to /
    TestCase("/Test/../.", "/");

    // Case 11: /.. transforms to /
    TestCase("/..", "/");

    PAL_Terminate();
    return PASS;
}

void TestCase(LPSTR input, LPSTR expectedOutput)
{
    // Save the input for debug logging since the input is edited in-place
    char* pOriginalInput = (char*)malloc(strlen(input) * sizeof(char) + 1);
    strcpy(pOriginalInput, input);

    char* pInput = (char*)malloc(strlen(input) * sizeof(char) + 1);
    strcpy(pInput, pOriginalInput);

    FILECanonicalizePath(pInput);
    if (strcmp(pInput, expectedOutput) != 0)
    {
        free(pOriginalInput);
        free(pInput);
        Fail("FILECanonicalizePath error: input %s did not match expected output %s; got %s instead", pOriginalInput, expectedOutput, pInput);
    }

    free(pOriginalInput);
    free(pInput);
}
