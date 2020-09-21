// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source: test4.c
**
** Purpose: Tests that WideCharToMultiByte correctly handles WC_NO_BEST_FIT_CHARS
**
**
**==========================================================================*/


#include <palsuite.h>

/* C with a circumflex */
char16_t ustr[2] = { 0x108, 0 };

/* expected conversion when best fit is allowed on Windows */
char* lpBestFitRes = "C";

/* expected conversion when no default character is specified */
char* lpResStr1 = "?";

/* expected conversion when the default character is 'k' */
char myDefaultChar = 'k';
char* lpResStr2 = "k";

int 
TestWideCharToMultiByte(
       IN UINT CodePage,
       IN DWORD dwFlags,
       IN LPCSTR lpDefaultChar,
       IN LPSTR lpResStr)
{
  char mbstr[30];
  int ret;
  int testStatus = PASS;
  BOOL usedDefaultChar = FALSE;

  printf("WideCharToMultiByte (CodePage=%d, dwFlags=%#x, default=%c)\n",
         CodePage, dwFlags, lpDefaultChar?*lpDefaultChar:' ');
  ret = WideCharToMultiByte(CodePage, dwFlags, ustr, -1, mbstr, sizeof(mbstr), 
                            lpDefaultChar, &usedDefaultChar);
  if (ret != 0) {
    printf("   converted C with circumflex to in Unicode to multibyte: "
           "\"%s\"\n", mbstr);
    printf("   used default character?: %d\n", usedDefaultChar);
    if (strcmp(mbstr, lpResStr) != 0 || usedDefaultChar != TRUE) 
    {
       printf("!!!! failed conversion !!!!\n");
       testStatus = FAIL;
    }
  }
  else {
    printf("!!!! failed conversion !!!!\n");
    testStatus = FAIL;
  }
  return testStatus;
}

PALTEST(locale_info_WideCharToMultiByte_test4_paltest_widechartomultibyte_test4, "locale_info/WideCharToMultiByte/test4/paltest_widechartomultibyte_test4")
{    
  int testStatus = PASS;

  if (PAL_Initialize(argc, argv))
  {
      return FAIL;
  }

  /* Use WideCharToMultiByte to convert the string in code page CP_ACP.
   * Note that the resulting string will be different on Windows PAL and 
   * Unix PAL. On Windows, the default best fit behavior will map C with 
   * circumflex to C. 
   * 
   *   testStatus |= TestWideCharToMultiByte(CP_ACP, 0, NULL, lpBestFitRes);
   *
   * On Unix, where there is no support for finding best fit, it will be 
   * mapped to a '?'. In addition, it will trigger an ASSERT in the dbg/chk
   * builds.
   *
   *   testStatus |= TestWideCharToMultiByte(CP_ACP, 0, NULL, lpResStr1);
   */

  /* Use WideCharToMultiByte with WC_NO_BEST_FIR_CHARS to convert the string 
   * in CP_ACP (1252 by default). This will prevent it from mapping the C 
   * with circumflex to its closest match in the ANSI code page: C
   */
  testStatus |= TestWideCharToMultiByte(CP_ACP, WC_NO_BEST_FIT_CHARS, NULL, lpResStr1);


  /* Use WideCharToMultiByte with WC_NO_BEST_FIR_CHARS and a default character 
   * to convert the string. This will prevent it from mapping the C with 
   * circumflex to its closest match in the ANSI code page: C. It will be
   * replaced with the specified default character.
   */
  testStatus |= TestWideCharToMultiByte(CP_ACP, WC_NO_BEST_FIT_CHARS, &myDefaultChar, lpResStr2);

  /* Use WideCharToMultiByte to convert the string in code page 1253 
   * Note that the resulting string will be different on Windows PAL and 
   * Unix PAL. On Windows, the default best fit behavior will map C with 
   * circumflex to C. 
   * 
   *   testStatus |= TestWideCharToMultiByte(1253, 0, NULL, lpBestFitRes);
   *
   * On Unix, where there is no support for finding best fit, it will be 
   * mapped to a '?'. In addition, it will trigger an ASSERT in the dbg/chk
   * builds.
   *
   *   testStatus |= TestWideCharToMultiByte(1253, 0, NULL, lpResStr1);
   */

  /* Use WideCharToMultiByte with WC_NO_BEST_FIR_CHARS to convert the string 
   * in 1253. This will prevent it from mapping the C 
   * with circumflex to its closest match in the ANSI code page: C
   */
  testStatus |= TestWideCharToMultiByte(1253, WC_NO_BEST_FIT_CHARS, NULL, lpResStr1);

  /* Use WideCharToMultiByte with WC_NO_BEST_FIR_CHARS and a default 
   * character to convert the string in 1253. This will prevent it from 
   * mapping the C with circumflex to its closest match in the ANSI code 
   * page: C. It will be replaced with the specified default character.
   */
  testStatus |= TestWideCharToMultiByte(1253, WC_NO_BEST_FIT_CHARS, &myDefaultChar, lpResStr2);

  PAL_TerminateEx(testStatus);

  return testStatus;
}

