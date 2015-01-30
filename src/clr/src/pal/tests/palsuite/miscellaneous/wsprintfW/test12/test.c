//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
   int pos = 0x1234ab;

   wsprintf(buf, convert("foo %x"), pos);
   if (memcmp(buf, convert("foo 1234ab"), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 1) Failed. The correct string is"
                      " 'foo 1234ab' and the result returned was ";
       BadResult = buf;
       return FAIL;
   }

   wsprintf(buf, convert("foo %lx"), pos);
   if (memcmp(buf, convert("foo 1234ab"), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 2) Failed. The correct string is"
                      " 'foo 1234ab' and the result returned was ";
       BadResult = buf;
       return FAIL;
   }

   wsprintf(buf, convert("foo %7x"), pos);
   if (memcmp(buf, convert("foo  1234ab"), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 3) Failed. The correct string is"
                      " 'foo  1234ab' and the result returned was ";
       BadResult = buf;
       return FAIL;
   }

   wsprintf(buf, convert("foo %-7x"), pos);
   if (memcmp(buf, convert("foo 1234ab "), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 4) Failed. The correct string is"
                      " 'foo 1234ab ' and the result returned was ";
       BadResult = buf;
       return FAIL;
   }

   wsprintf(buf, convert("foo %.1x"), pos);
   if (memcmp(buf, convert("foo 1234ab"), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 5) Failed. The correct string is"
                      " 'foo 1234ab' and the result returned was ";
       BadResult = buf;
       return FAIL;
   }

   wsprintf(buf, convert("foo %.7x"), pos);
   if (memcmp(buf, convert("foo 01234ab"), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 6) Failed. The correct string is"
                      " 'foo 01234ab' and the result returned was ";
       BadResult = buf;
       return FAIL;
   }

   wsprintf(buf, convert("foo %07x"), pos);
   if (memcmp(buf, convert("foo 01234ab"), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 7) Failed. The correct string is"
                      " 'foo 01234ab' and the result returned was ";
       BadResult = buf;
       return FAIL;
   }

   wsprintf(buf, convert("foo %#x"), pos);
   if (memcmp(buf, convert("foo 0x1234ab"), wcslen(buf)*2 + 2) != 0)
   {
       ErrorMessage = "ERROR: (Test 8) Failed. The correct string is"
                      " 'foo 0x1234ab' and the result returned was ";
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



