// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source : test.c
**
** Purpose: Test for FormatMessageW() function
**
**
**=========================================================*/

#define UNICODE
#include <palsuite.h>

WCHAR OutBuffer_FormatMessageW_test2[1024];

/* Pass this test the string "INSERT" and it will succeed */

int test1(int num, ...)
{

    WCHAR * TheString = convert("Pal %1!s! Testing");
    int ReturnResult;
    va_list TheList;
    va_start(TheList,num);
    memset( OutBuffer_FormatMessageW_test2, 0, 1024 * sizeof(OutBuffer_FormatMessageW_test2[0]) );

    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer_FormatMessageW_test2,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        &TheList                             /* array of message inserts */
        );

    va_end(TheList);

    if(ReturnResult == 0)
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string,"
             " with the 's' formatter.");

    }

    if(memcmp(OutBuffer_FormatMessageW_test2, convert("Pal INSERT Testing"),
              wcslen(OutBuffer_FormatMessageW_test2)*2+2) != 0)
    {
        Fail("ERROR:  The formatted string should have been 'Pal INSERT "
             "Testing' but '%s' was returned.",
             convertC(OutBuffer_FormatMessageW_test2));
    }


    return PASS;
}

/* Pass this test the int 40 and it will succeed */

int test2(int num, ...)
{

    WCHAR * TheString = convert("Pal %1!i! Testing");
    int ReturnResult;
    va_list TheList;
    va_start(TheList,num);

    memset( OutBuffer_FormatMessageW_test2, 0, 1024 * sizeof(OutBuffer_FormatMessageW_test2[0]) );

    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer_FormatMessageW_test2,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        &TheList                             /* array of message inserts */
        );

    va_end(TheList);

    if(ReturnResult == 0)
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string,"
             " with the 'i' formatter.");
    }

    if(memcmp(OutBuffer_FormatMessageW_test2, convert("Pal 40 Testing"),wcslen(OutBuffer_FormatMessageW_test2)*2+2) != 0)
    {
        Fail("ERROR:  The formatted string should have been 'Pal 40 Testing' "
             "but '%s' was returned.", convertC(OutBuffer_FormatMessageW_test2));
    }
    return PASS;
}

/* Pass this test the character 'a' and it will succeed */

int test3(int num, ...) {

    WCHAR * TheString = convert("Pal %1!c! Testing");
    int ReturnResult;
    va_list TheList;
    va_start(TheList,num);
    memset( OutBuffer_FormatMessageW_test2, 0, 1024 * sizeof(OutBuffer_FormatMessageW_test2[0]) );
    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer_FormatMessageW_test2,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        &TheList                             /* array of message inserts */
        );

    va_end(TheList);

    if(ReturnResult == 0)
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string,"
             " with the 'c' formatter.");
    }

    if(memcmp(OutBuffer_FormatMessageW_test2, convert("Pal a Testing"),wcslen(OutBuffer_FormatMessageW_test2)*2+2) != 0)
    {
        Fail("ERROR:  The formatted string should have been 'Pal a Testing' "
             "but '%s' was returned.", convertC(OutBuffer_FormatMessageW_test2));

    }

    return PASS;
}

/* Pass this test the character 'a' and it will succeed */

int test4(int num, ...) {

    WCHAR * TheString = convert("Pal %1!C! Testing");
    int ReturnResult;
    va_list TheList;
    va_start(TheList,num);
    memset( OutBuffer_FormatMessageW_test2, 0, 1024 * sizeof(OutBuffer_FormatMessageW_test2[0]) );
    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer_FormatMessageW_test2,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        &TheList                             /* array of message inserts */
        );

    va_end(TheList);

    if(ReturnResult == 0)
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string,"
             " with the 'C' formatter.");
    }

    if(memcmp(OutBuffer_FormatMessageW_test2, convert("Pal a Testing"),wcslen(OutBuffer_FormatMessageW_test2)*2+2) != 0)
    {
        Fail("ERROR:  The formatted string should have been 'Pal a Testing' "
             "but '%s' was returned.",convertC(OutBuffer_FormatMessageW_test2));
    }

    return PASS;
}

/* Pass this test the number 57 and it will succeed */

int test5(int num, ...)
{

    WCHAR * TheString = convert("Pal %1!d! Testing");
    int ReturnResult;
    va_list TheList;
    va_start(TheList,num);
    memset( OutBuffer_FormatMessageW_test2, 0, 1024 * sizeof(OutBuffer_FormatMessageW_test2[0]) );

    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer_FormatMessageW_test2,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        &TheList                             /* array of message inserts */
        );

    va_end(TheList);

    if(ReturnResult == 0)
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string, "
             "with the 'd' formatter.");

    }

    if(memcmp(OutBuffer_FormatMessageW_test2, convert("Pal 57 Testing"),wcslen(OutBuffer_FormatMessageW_test2)*2+2) != 0)
    {
        Fail("ERROR:  The formatted string should have been 'Pal 57 Testing' "
             "but '%s' was returned.",convertC(OutBuffer_FormatMessageW_test2));

    }

    return PASS;
}

/* Pass this test the characters 'a' and 'b' and it will succeed. */

int test6(int num, ...) {

    WCHAR * TheString = convert("Pal %1!hc! and %2!hC! Testing");
    int ReturnResult;
    va_list TheList;
    va_start(TheList,num);
    memset( OutBuffer_FormatMessageW_test2, 0, 1024 * sizeof(OutBuffer_FormatMessageW_test2[0]) );

    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer_FormatMessageW_test2,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        &TheList                             /* array of message inserts */
        );

    va_end(TheList);

    if(ReturnResult == 0)
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string, "
             "with the 'hc' and 'hC' formatters.");

    }

    if(memcmp(OutBuffer_FormatMessageW_test2, convert("Pal a and b Testing"),
              wcslen(OutBuffer_FormatMessageW_test2)*2+2) != 0)
    {
        Fail("ERROR:  The formatted string should have been 'Pal a and b "
             "Testing' but '%s' was returned.", convertC(OutBuffer_FormatMessageW_test2));

    }

    return PASS;
}

/* Pass this test 90, the string 'foo' and the string 'bar' to succeed */

int test7(int num, ...)
{

    WCHAR * TheString = convert("Pal %1!hd! and %2!hs! and %3!hS! Testing");
    int ReturnResult;
    va_list TheList;
    va_start(TheList,num);
    memset( OutBuffer_FormatMessageW_test2, 0, 1024 * sizeof(OutBuffer_FormatMessageW_test2[0]) );

    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer_FormatMessageW_test2,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        &TheList                             /* array of message inserts */
        );

    va_end(TheList);

    if(ReturnResult == 0)
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string, "
             "with the 'hd', 'hs' and 'hS' formatters.");

    }

    if(memcmp(OutBuffer_FormatMessageW_test2,
              convert("Pal 90 and foo and bar Testing"),
              wcslen(OutBuffer_FormatMessageW_test2)*2+2) != 0)
    {
        Fail("ERROR:  The formatted string should have been 'Pal 90 and foo "
             "and bar Testing' but '%s' was returned.",convertC(OutBuffer_FormatMessageW_test2));
    }

    return PASS;
}

/* Pass this test the characters 'a', 'b' and the numbers 50 and 100 */

int test8(int num, ...)
{

    WCHAR * TheString =
        convert("Pal %1!lc! and %2!lC! and %3!ld! and %4!li! Testing");
    int ReturnResult;
    va_list TheList;
    va_start(TheList,num);
    memset( OutBuffer_FormatMessageW_test2, 0, 1024 * sizeof(OutBuffer_FormatMessageW_test2[0]) );

    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer_FormatMessageW_test2,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        &TheList                             /* array of message inserts */
        );

    va_end(TheList);

    if(ReturnResult == 0)
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string, "
             "with the 'lc', 'lC', 'ld' and 'li' formatters.");

    }

    if(memcmp(OutBuffer_FormatMessageW_test2,
              convert("Pal a and b and 50 and 100 Testing"),
              wcslen(OutBuffer_FormatMessageW_test2)*2+2) != 0)
    {
        Fail("ERROR:  The formatted string should have been 'Pal a and b and 50"
             " and 100 Testing' but '%s' was returned.",convertC(OutBuffer_FormatMessageW_test2));

    }

    return PASS;
}

/* Pass this test the wide string 'foo' and 'bar' and the unsigned
   int 56 to pass
*/

int test9(int num, ...) {

    WCHAR * TheString = convert("Pal %1!ls! and %2!ls! and %3!lu! Testing");
    int ReturnResult;
    va_list TheList;
    va_start(TheList,num);
    memset( OutBuffer_FormatMessageW_test2, 0, 1024 * sizeof(OutBuffer_FormatMessageW_test2[0]) );
    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer_FormatMessageW_test2,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        &TheList                             /* array of message inserts */
        );

    va_end(TheList);

    if(ReturnResult == 0)
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string,"
             " with the 'ls', 'lS' and 'lu' formatters.");

    }

    if(memcmp(OutBuffer_FormatMessageW_test2,
              convert("Pal foo and bar and 56 Testing"),
              wcslen(OutBuffer_FormatMessageW_test2)*2+2) != 0)
    {
        Fail("ERROR:  The formatted string should have been 'Pal foo and bar "
             "and 56 Testing' but '%s' was returned.",convertC(OutBuffer_FormatMessageW_test2));

    }

    return PASS;
}

/* Pass this test the hex values 0x123ab and 0x123cd */

int test10(int num, ...)
{

    WCHAR * TheString = convert("Pal %1!lx! and %2!lX! Testing");
    int ReturnResult;
    va_list TheList;
    va_start(TheList,num);
    memset( OutBuffer_FormatMessageW_test2, 0, 1024 * sizeof(OutBuffer_FormatMessageW_test2[0]) );
    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer_FormatMessageW_test2,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        &TheList                             /* array of message inserts */
        );

    va_end(TheList);

    if(ReturnResult == 0)
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string, "
             "with the 'lx' and 'lX' formatters.");

    }

    if(memcmp(OutBuffer_FormatMessageW_test2,
              convert("Pal 123ab and 123CD Testing"),
              wcslen(OutBuffer_FormatMessageW_test2)*2+2) != 0)
    {
        Fail("ERROR:  The formatted string should have been 'Pal 123ab and "
             "123CD Testing' but '%s' was returned.", convertC(OutBuffer_FormatMessageW_test2));

    }

    return PASS;
}

/* Pass this test a pointer to 0x123ab and the string 'foo' to pass */

int test11(int num, ...)
{

    WCHAR * TheString = convert("Pal %1!p! and %2!S! Testing");
    int ReturnResult;
    va_list TheList;
    va_start(TheList,num);
    memset( OutBuffer_FormatMessageW_test2, 0, 1024 * sizeof(OutBuffer_FormatMessageW_test2[0]) );

    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer_FormatMessageW_test2,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        &TheList                             /* array of message inserts */
        );

    va_end(TheList);

    if(ReturnResult == 0)
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string, "
             "with the 'p' and 'S' formatters.");

    }

/*
**  Run only on 64 bit platforms
*/
#if defined(HOST_64BIT)
	Trace("Testing for 64 Bit Platforms \n");
	if(memcmp(OutBuffer_FormatMessageW_test2,
              convert("Pal 00000000000123AB and foo Testing"),
              wcslen(OutBuffer_FormatMessageW_test2)*2+2) != 0 &&
       /* BSD style */
       memcmp( OutBuffer_FormatMessageW_test2,
               convert( "Pal 0x123ab and foo Testing" ),
               wcslen(OutBuffer_FormatMessageW_test2)*2+2 ) != 0 )
    {
        Fail("ERROR:  The formatted string should have been 'Pal 000123AB and "
             "foo Testing' but '%s' was returned.",convertC(OutBuffer_FormatMessageW_test2));

    }

#else
	Trace("Testing for Non 64 Bit Platforms \n");
	if(memcmp(OutBuffer_FormatMessageW_test2,
              convert("Pal 000123AB and foo Testing"),
              wcslen(OutBuffer_FormatMessageW_test2)*2+2) != 0 &&
       /* BSD style */
       memcmp( OutBuffer_FormatMessageW_test2,
               convert( "Pal 0x123ab and foo Testing" ),
               wcslen(OutBuffer_FormatMessageW_test2)*2+2 ) != 0 )
    {
        Fail("ERROR:  The formatted string should have been 'Pal 000123AB and "
             "foo Testing' but '%s' was returned.",convertC(OutBuffer_FormatMessageW_test2));

    }

#endif

   return PASS;
}

/* Pass this test the unsigned int 100 and the hex values 0x123ab and 0x123cd
to succeed */

int test12(int num, ...)
{

    WCHAR * TheString = convert("Pal %1!u! and %2!x! and %3!X! Testing");
    int ReturnResult;
    va_list TheList;
    va_start(TheList,num);
    memset( OutBuffer_FormatMessageW_test2, 0, 1024 * sizeof(OutBuffer_FormatMessageW_test2[0]) );

    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer_FormatMessageW_test2,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        &TheList                             /* array of message inserts */
        );

    va_end(TheList);

    if(ReturnResult == 0)
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string, "
             "with the 'u', 'x' and 'X' formatters.");

    }

    if(memcmp(OutBuffer_FormatMessageW_test2,
              convert("Pal 100 and 123ab and 123CD Testing"),
              wcslen(OutBuffer_FormatMessageW_test2)*2+2) != 0)
    {
        Fail("ERROR:  The formatted string should have been 'Pal 100 and "
             "123ab and 123CD Testing' but '%s' was returned.",
             convertC(OutBuffer_FormatMessageW_test2));

    }

    return PASS;
}

PALTEST(miscellaneous_FormatMessageW_test2_paltest_formatmessagew_test2, "miscellaneous/FormatMessageW/test2/paltest_formatmessagew_test2")
{
    WCHAR szwInsert[] = {'I','N','S','E','R','T','\0'};
    WCHAR szwFoo[] = {'f','o','o','\0'};
    WCHAR szwBar[] = {'b','a','r','\0'};

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    if(test1(0,szwInsert) ||                        /* Test %s */
       test2(0,40) ||                               /* Test %i */
       test3(0,'a') ||                              /* Test %c */
       test4(0,'a') ||                              /* Test %C */
       test5(0,57) ||                               /* Test %d */
       test6(0,'a','b') ||                          /* Test %hc, %hC */
       test7(0,90,"foo","bar") ||                   /* Test %hd,hs,hS */
       test8(0,'a','b',50,100) ||                   /* Test %lc, lC, ld, li */
       test9(0,szwFoo,szwBar,56) ||                 /* Test %ls,lS,lu  */
       test10(0,0x123ab,0x123cd) ||                 /* Test %lx, %lX */
       test11(0,(void *)0x123ab,"foo") ||           /* Test %p, %S */
       test12(0,100,0x123ab,0x123cd))               /* Test %u,x,X */
    {


    }

    PAL_Terminate();
    return PASS;

}



