// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for wsprintfW() function
**
**
**=========================================================*/
#define UNICODE
#include <palsuite.h>


char * ErrorMessage = NULL; 
WCHAR * BadResult = NULL; 
WCHAR buf[256];

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with strlen */

BOOL test1()
{
   WCHAR wc = 'b';

   wsprintf(buf, convert("foo %c"), wc);
   if (memcmp(buf, convert("foo b"), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 1) Failed. The correct string is"
                      " 'foo b' and the result returned was ";
       BadResult = buf;
       return FAIL;
   }

   wsprintf(buf, convert("foo %hc"), wc);
   if (memcmp(buf, convert("foo b"), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 2) Failed. The correct string is 'foo b'"
                      " and the result returned was ";
       BadResult = buf;
       return FAIL;
   }

   wsprintf(buf, convert("foo %lc"), wc);
   if (memcmp(buf, convert("foo b"), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 3) Failed. The correct string is 'foo b'"
                      " and the result returned was ";
       BadResult = buf;
       return FAIL;
   }

   wsprintf(buf, convert("foo %5c"), wc);
   if (memcmp(buf, convert("foo     b"), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 4) Failed. The correct string is"
                      " 'foo     b' and the result returned was ";
       BadResult = buf;
       return FAIL;
   }

   wsprintf(buf, convert("foo %-5c"), wc);
   if (memcmp(buf, convert("foo b    "), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 5) Failed. The correct string is"
                      " 'foo b    ' and the result returned was ";
       BadResult = buf;
       return FAIL;
   }

   wsprintf(buf, convert("foo %05c"), wc);
   if (memcmp(buf, convert("foo 0000b"), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 6) Failed. The correct string is"
                      " 'foo 0000b' and the result returned was ";
       BadResult = buf;
       return FAIL;
   }

   wsprintf(buf, convert("foo %#c"), wc);
   if (memcmp(buf, convert("foo b"), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 7) Failed. The correct string is 'foo b'"
                      " and the result returned was ";
       BadResult = buf;
       return FAIL;
   }
   return PASS;
}
int __cdecl main(int argc, char *argv[])
{

   /*
    * Initialize the PAL and return FAILURE if this fails
    */

   if(0 != (PAL_Initialize(argc, argv)))
   {
       return FAIL;
   }

   if(test1())
   {
       Fail("%s '%s'\n",ErrorMessage,convertC(BadResult));

   }

   PAL_Terminate();
   return PASS;

}


