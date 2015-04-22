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

#define TEST_ARR_SIZE 10
extern void FILECanonicalizePath(LPSTR lpUnixPath);

int __cdecl main(int argc, char *argv[])
{
	if (PAL_Initialize(argc,argv) != 0)
    {
        return FAIL;
    }

    char paths[TEST_ARR_SIZE] = {};

    // Case 1: // transforms to /
    strcpy(paths, "//");
    FILECanonicalizePath(paths);
    if (strcmp(paths, "/") != 0)
    {
    	Fail("FILECanonicalizePath error: // did not transform to /, translated to: %s", paths);
    }
    memset(paths, '\0', TEST_ARR_SIZE);
    
    // Case 2: /./ transforms to /
    strcpy(paths, "/./");
    FILECanonicalizePath(paths);
    if (strcmp(paths, "/") != 0)
    {
    	Fail("FILECanonicalizePath error: /./ did not transform to /, translated to: %s", paths);
    }
    memset(paths, '\0', TEST_ARR_SIZE);
    
    // Case 3: /<name>/../ transforms to /
    strcpy(paths, "/Test/../");
    FILECanonicalizePath(paths);
    if (strcmp(paths, "/") != 0)
    {
    	Fail("FILECanonicalizePath error: /Test/../ did not transform to /, translated to: %s", paths);
    }
    memset(paths, '\0', TEST_ARR_SIZE);
    
    // Case 4: /Test/Foo/.. transforms to /Test
    strcpy(paths, "/Test/Foo/..");
    FILECanonicalizePath(paths);
    if (strcmp(paths, "/Test") != 0)
    {
    	Fail("FILECanonicalizePath error: /Test/Foo/.. did not transform to /, translated to: %s", paths);
    }
    memset(paths, '\0', TEST_ARR_SIZE);
    
    // Case 5: /Test/.. transforms to /
    strcpy(paths, "/Test/..");
    FILECanonicalizePath(paths);
    if (strcmp(paths, "/") != 0)
    {
    	Fail("FILECanonicalizePath error: /Test/.. did not transform to /, translated to: %s", paths);
    }
    memset(paths, '\0', TEST_ARR_SIZE);
    
    // Case 6: /. transforms to /
    strcpy(paths, "/.");
    FILECanonicalizePath(paths);
    if (strcmp(paths, "/") != 0)
    {
    	Fail("FILECanonicalizePath error: /. did not transform to /, translated to: %s", paths);
    }
    memset(paths, '\0', TEST_ARR_SIZE);
    
    // Case 7: /<name/. transforms to /<name>
    strcpy(paths, "/Test/.");
    FILECanonicalizePath(paths);
    if (strcmp(paths, "/Test") != 0)
    {
    	Fail("FILECanonicalizePath error: /<name>/. did not transform to /, translated to: %s", paths);
    }
    memset(paths, '\0', TEST_ARR_SIZE);
    
    PAL_Terminate();
    return PASS;
}
