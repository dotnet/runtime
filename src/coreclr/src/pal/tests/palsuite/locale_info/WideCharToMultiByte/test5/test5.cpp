// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source: test4.c
**
** Purpose: Tests WideCharMultiByte with UTF-8 encoding
**
**
**==========================================================================*/

#include <palsuite.h>

PALTEST(locale_info_WideCharToMultiByte_test5_paltest_widechartomultibyte_test5, "locale_info/WideCharToMultiByte/test5/paltest_widechartomultibyte_test5")
{    
    int ret;
    int ret2;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    const WCHAR * const unicodeStrings[] =
    {
        // Correct strings
        
        // Empty string
        W(""),
        // 1 byte encoded 1 character long string
        W("A"),
        // 2 byte encoded 1 character long string
        W("\x0080"),
        // 3 byte encoded 1 character long string
        W("\x0800"),
        // 1 byte encoded characters only
        W("ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
        // 2 byte encoded characters only
        W("\x0080\x00FF\x01C1\x07FF"),
        // valid 3 byte encoded characters only
        W("\x0800\x1D88\x1000\xFFFF"),
        // 1 byte and 2 byte encoded characters interleaved 1:1 starting and ending with 1 byte char
        W("\x0041\x0080\x0042\x00FF\x0043\x01C1\x0044\x07FF\x0045"),
        // 1 byte and 2 byte encoded characters interleaved 1:1 starting with 1 byte char, ending with 2 byte one
        W("\x0041\x0080\x0042\x00FF\x0043\x01C1\x0044\x07FF"),
        // 1 byte and 2 byte encoded characters interleaved 1:1 starting with 2 byte char, ending with 1 byte one
        W("\x0080\x0042\x00FF\x0043\x01C1\x0044\x07FF\x0045"),
        // 1 byte and 2 byte encoded characters interleaved 1:1 starting and ending with 2 byte char
        W("\x0080\x0042\x00FF\x0043\x01C1\x0044\x07FF"),
        // 1 byte and 2 byte encoded characters interleaved 2:2 starting and ending with 1 byte char
        W("\x0041\x0042\x0080\x00FF\x0043\x0044\x01C1\x07FF\x0045\x0046"),
        // 1 byte and 2 byte encoded characters interleaved 2:2 starting with 1 byte char, ending with 2 byte one
        W("\x0041\x0042\x0080\x00FF\x0043\x0044\x01C1\x07FF"),
        // 1 byte and 2 byte encoded characters interleaved 2:2 starting with 2 byte char, ending with 1 byte one
        W("\x0080\x00FF\x0043\x0044\x01C1\x07FF\x0045\x0046"),
        // 1 byte and 2 byte encoded characters interleaved 2:2 starting and ending with 2 byte char
        W("\x0080\x00FF\x0043\x0044\x01C1\x07FF"),
        // Surrogates
        W("\xD800\xDC00\xD800\xDE40\xDAC0\xDFB0\xDBFF\xDFFF"),

        // Strings with errors
        
        // Single high surrogate
        W("\xD800"),
        // Single low surrogate
        W("\xDC00"),
        // Character followed by single high surrogate
        W("\x0041\xD800"),
        // Character followed by single low surrogate
        W("\x0041\xDC00"),
        // Single high surrogate between two characters
        W("\x0041\xD800\x0042"),
        // Single low surrogate between two characters
        W("\x0041\xDC00\x0042"),
    };
    
    const char * const utf8Strings[] =
    {
        // Correct strings
        
        // Empty string
        "",
        // 1 byte encoded 1 character long string
        "A",
        // 2 byte encoded 1 character long string
        "\xC2\x80",
        // 3 byte encoded 1 character long string
        "\xE0\xA0\x80",
        // 1 byte encoded characters only
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
        // valid 2 byte encoded characters only
        "\xC2\x80\xC3\xBF\xC7\x81\xDF\xBF",
        // valid 3 byte encoded characters only
        "\xE0\xA0\x80\xE1\xB6\x88\xE1\x80\x80\xEF\xBF\xBF",
        // 1 byte and 2 byte encoded characters interleaved 1:1 starting and ending with 1 byte char
        "\x41\xC2\x80\x42\xC3\xBF\x43\xC7\x81\x44\xDF\xBF\x45",
        // 1 byte and 2 byte encoded characters interleaved 1:1 starting with 1 byte char, ending with 2 byte one
        "\x41\xC2\x80\x42\xC3\xBF\x43\xC7\x81\x44\xDF\xBF",
        // 1 byte and 2 byte encoded characters interleaved 1:1 starting with 2 byte char, ending with 1 byte one
        "\xC2\x80\x42\xC3\xBF\x43\xC7\x81\x44\xDF\xBF\x45",
        // 1 byte and 2 byte encoded characters interleaved 1:1 starting and ending with 2 byte char
        "\xC2\x80\x42\xC3\xBF\x43\xC7\x81\x44\xDF\xBF",
        // 1 byte and 2 byte encoded characters interleaved 2:2 starting and ending with 1 byte char
        "\x41\x42\xC2\x80\xC3\xBF\x43\x44\xC7\x81\xDF\xBF\x45\x46",
        // 1 byte and 2 byte encoded characters interleaved 2:2 starting with 1 byte char, ending with 2 byte one
        "\x41\x42\xC2\x80\xC3\xBF\x43\x44\xC7\x81\xDF\xBF",
        // 1 byte and 2 byte encoded characters interleaved 2:2 starting with 2 byte char, ending with 1 byte one
        "\xC2\x80\xC3\xBF\x43\x44\xC7\x81\xDF\xBF\x45\x46",
        // 1 byte and 2 byte encoded characters interleaved 2:2 starting and ending with 2 byte char
        "\xC2\x80\xC3\xBF\x43\x44\xC7\x81\xDF\xBF",
        // Surrogates
        "\xF0\x90\x80\x80\xF0\x90\x89\x80\xF3\x80\x8E\xB0\xF4\x8F\xBF\xBF",
        
        // Strings with errors

        // Single high surrogate
        "\xEF\xBF\xBD",
        // Single low surrogate
        "\xEF\xBF\xBD",
        // Character followed by single high surrogate
        "\x41\xEF\xBF\xBD",
        // Character followed by single low surrogate
        "\x41\xEF\xBF\xBD",
        // Single high surrogate between two characters
        "\x41\xEF\xBF\xBD\x42",
        // Single low surrogate between two characters
        "\x41\xEF\xBF\xBD\x42",
    };

    for (int i = 0; i < (sizeof(unicodeStrings) / sizeof(unicodeStrings[0])); i++)
    {
        ret = WideCharToMultiByte(CP_UTF8, 0, unicodeStrings[i], -1, NULL, 0, NULL, NULL);
        CHAR* utf8Buffer = (CHAR*)malloc(ret * sizeof(CHAR));
        ret2 = WideCharToMultiByte(CP_UTF8, 0, unicodeStrings[i], -1, utf8Buffer, ret, NULL, NULL);
        if (ret != ret2)
        {
            Fail("WideCharToMultiByte string %d: returned different string length for empty and real dest buffers!\n"
                "Got %d for the empty one, %d for real one.\n", i, ret2, ret);
        }
        
        if (strcmp(utf8Buffer, utf8Strings[i]) != 0)
        {
            Fail("WideCharToMultiByte string %d: the resulting string doesn't match the expected one!\n", i);
        }
        
        free(utf8Buffer);
    }
   
    PAL_Terminate();

    return PASS;
}
