// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: 
** Search for a number of tokens within strings.  Check that the return values
** are what is expected, and also that the strings match up with our expected
** results.
**
**
**==========================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_wcstok_test1_paltest_wcstok_test1, "c_runtime/wcstok/test1/paltest_wcstok_test1")
{
    /* foo bar baz */
    WCHAR str[] = {'f','o','o',' ','b','a','r',' ','b','a','z','\0'};
    
    /* foo \0ar baz */
    WCHAR result1[] = {'f','o','o',' ','\0','a','r',' ','b','a','z','\0'};
    
    /* foo \0a\0 baz */
    WCHAR result2[] = {'f','o','o',' ','\0','a','\0',' ','b','a','z','\0'};
    
    WCHAR* tempString;
    int len = 0;
    WCHAR *ptr;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    len = (wcslen(str)*sizeof(WCHAR)) + 2;

    /* Tokenize 'str'.  It will hit the 'b' delimiter first.  Check to see
       that the ptr is pointing to the start of the string and do a compare
       to ensure the tokenized string is what we expected.
    */
    
    tempString = convert("bz");
    ptr = wcstok(str, tempString);
    free(tempString);
    
    if (ptr != str)
    {
        Fail("ERROR: Expected wcstok() to return %p, got %p!\n", str, ptr);
    }
    
    if (memcmp(str, result1, len) != 0)
    {
        Fail("ERROR: wcstok altered the string in an unexpected fashion.");
    }

    /* If NULL is passed as the first parameter, wcstok will continue 
       tokenizing the same string.  Test that this works properly.
    */
    tempString = convert("r ");
    ptr = wcstok(NULL, tempString);
    free(tempString);
    
    if (ptr != str + 5)
    {
        Fail("ERROR: Expected wcstok() to return %p, got %p!\n", str+5, ptr);
    }
    
    if (memcmp(str, result2, len) != 0)
    {
        Fail("ERROR: wcstok altered the string in an unexpected fashion.");
    }

    /* Continue onward, and search for 'X' now, which won't be found.  The
       pointer should point just after the last NULL in the string.  And
       the string itself shouldn't have changed.
    */
    tempString = convert("X");
    ptr = wcstok(NULL, tempString);
    free(tempString);

    if (ptr != str + 7)
    {
        Fail("ERROR: Expected wcstok() to return %p, got %p!\n", str + 7, ptr);
    }
    
    if (memcmp(str, result2, len) != 0)
    {
        Fail("ERROR: wcstok altered the string in an unexpeced fashion.\n");
    }

    /* Call wcstok again.  Now the ptr should point to the end of the 
       string at NULL.  And the string itself shouldn't have changed.
    */
    tempString = convert("X");
    ptr = wcstok(NULL, tempString);
    free(tempString);

    if (ptr != NULL)
    {
        Fail("ERROR: Expected wcstok() to return %p, got %p!\n", NULL, ptr);
    }
    
    if (memcmp(str, result2, len) != 0)
    {
        Fail("ERROR: wcstok altered the string in an unexpeced fashion.\n");
    }

    PAL_Terminate();
    return PASS;
}
