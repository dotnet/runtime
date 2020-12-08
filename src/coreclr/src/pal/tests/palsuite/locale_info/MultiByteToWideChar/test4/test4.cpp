// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source: test4.c
**
** Purpose: Tests MultiByteToWideChar with a UTF-8 encoding
**
**
**==========================================================================*/

#include <palsuite.h>

PALTEST(locale_info_MultiByteToWideChar_test4_paltest_multibytetowidechar_test4, "locale_info/MultiByteToWideChar/test4/paltest_multibytetowidechar_test4")
{    
    int ret;
    int ret2;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

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
        // surrogates
        "\xF0\x90\x80\x80\xF0\x90\x89\x80\xF3\x80\x8E\xB0\xF4\x8F\xBF\xBF",
        
        // Strings with errors
        // Incomplete 2 byte encoded character 1 byte missing standalone
        "\xC2",
        // Incomplete 3 byte encoded character 1 byte missing standalone
        "\xE0\xA0",
        // Incomplete 3 byte encoded character 2 bytes missing standalone
        "\xE0",
        // Incomplete surrogate character 1 byte missing standalone
        "\xF0\x90\x80",
        // Incomplete surrogate character 2 bytes missing standalone
        "\xF0\x90",
        // Incomplete surrogate character 3 bytes missing standalone
        "\xF0",
        // Trailing byte with no lead byte standalone
        "\x80",
        // Incomplete 2 byte encoded character 1 byte missing between 1 byte chars
        "\x41\xC2\x42",
        // Incomplete 3 byte encoded character 1 byte missing between 1 byte chars
        "\x41\xE0\xA0\x42",
        // Incomplete 3 byte encoded character 2 bytes missing between 1 byte chars
        "\x41\xE0\x42",
        // Trailing byte with no lead byte between 1 byte chars
        "\x41\x80\x42",
        // Incomplete 2 byte encoded character 1 byte missing before 1 byte char
        "\xC2\x42",
        // Incomplete 3 byte encoded character 1 byte missing before 1 byte char
        "\xE0\xA0\x42",
        // Incomplete 3 byte encoded character 2 bytes missing before 1 byte char
        "\xE0\x42",
        // Trailing byte with no lead byte before 1 byte char
        "\x80\x42",
        // Incomplete 2 byte encoded character 1 byte missing after 1 byte char
        "\x41\xC2",
        // Incomplete 3 byte encoded character 1 byte missing after 1 byte char
        "\x41\xE0\xA0",
        // Incomplete 3 byte encoded character 2 bytes missing after 1 byte char
        "\x41\xE0",
        // Trailing byte with no lead byte after 1 byte char
        "\x41\x80",        
        // Incomplete 2 byte encoded character 1 byte missing between 2 byte chars
        "\xC2\x80\xC2\xC3\xBF",
        // Incomplete 3 byte encoded character 1 byte missing between 2 byte chars
        "\xC2\x80\xE0\xA0\xC3\xBF",
        // Incomplete 3 byte encoded character 2 bytes missing between 2 byte chars
        "\xC2\x80\xE0\xC3\xBF",
        // Trailing byte with no lead byte between 2 byte chars
        "\xC2\x80\x80\xC3\xBF",
        // 2 byte encoded character in non-shortest form encodings (these are not allowed)
        "\xC0\x80",
        // 3 byte encoded character in non-shortest form encodings (these are not allowed)
        "\xE0\x80\x80",
        // 4 byte encoded character in non-shortest form encodings (these are not allowed)
        "\xF0\x80\x80\x80",
    };

    const WCHAR * const unicodeStrings[] =
    {
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
        // surrogates
        W("\xD800\xDC00\xD800\xDE40\xDAC0\xDFB0\xDBFF\xDFFF"),

        // Strings with errors
        // Incomplete 2 byte encoded character standalone
        W("\xFFFD"),
        // Incomplete 3 byte encoded character 1 byte missing standalone
        W("\xFFFD"),
        // Incomplete 3 byte encoded character 2 bytes missing standalone
        W("\xFFFD"),
        // Incomplete surrogate character 1 byte missing standalone
        W("\xFFFD"),
        // Incomplete surrogate character 2 bytes missing standalone
        W("\xFFFD"),
        // Incomplete surrogate character 3 bytes missing standalone
        W("\xFFFD"),
        // Trailing byte with no lead byte standalone
        W("\xFFFD"),
        // Incomplete 2 byte encoded character 1 byte missing between 1 byte chars
        W("\x0041\xFFFD\x0042"),
        // Incomplete 3 byte encoded character 1 byte missing between 1 byte chars
        W("\x0041\xFFFD\x0042"),
        // Incomplete 3 byte encoded character 2 bytes missing between 1 byte chars
        W("\x0041\xFFFD\x0042"),
        // Trailing byte with no lead byte between 1 byte chars
        W("\x0041\xFFFD\x0042"),
        // Incomplete 2 byte encoded character 1 byte missing before 1 byte char
        W("\xFFFD\x0042"),
        // Incomplete 3 byte encoded character 1 byte missing before 1 byte char
        W("\xFFFD\x0042"),
        // Incomplete 3 byte encoded character 2 bytes missing before 1 byte char
        W("\xFFFD\x0042"),
        // Trailing byte with no lead byte before 1 byte char
        W("\xFFFD\x0042"),
        // Incomplete 2 byte encoded character 1 byte missing after 1 byte char
        W("\x0041\xFFFD"),
        // Incomplete 3 byte encoded character 1 byte missing after 1 byte char
        W("\x0041\xFFFD"),
        // Incomplete 3 byte encoded character 2 bytes missing after 1 byte char
        W("\x0041\xFFFD"),
        // Trailing byte with no lead byte after 1 byte char
        W("\x0041\xFFFD"),
        // Incomplete 2 byte encoded character 1 byte missing between 2 byte chars
        W("\x0080\xFFFD\x00FF"),
        // Incomplete 3 byte encoded character 1 byte missing between 2 byte chars
        W("\x0080\xFFFD\x00FF"),
        // Incomplete 3 byte encoded character 2 bytes missing between 2 byte chars
        W("\x0080\xFFFD\x00FF"),
        // Trailing byte with no lead byte between 2 byte chars
        W("\x0080\xFFFD\x00FF"),
        // 2 byte encoded character in non-shortest form encodings (these are not allowed)
        W("\xFFFD\xFFFD"),
        // 3 byte encoded character in non-shortest form encodings (these are not allowed)
        W("\xFFFD\xFFFD"),
        // 4 byte encoded character in non-shortest form encodings (these are not allowed)
        W("\xFFFD\xFFFD\xFFFD"),
    };

    for (int i = 0; i < (sizeof(utf8Strings) / sizeof(utf8Strings[0])); i++)
    {
        ret = MultiByteToWideChar(CP_UTF8, 0, utf8Strings[i], -1, NULL, 0);
        WCHAR* wideBuffer = (WCHAR*)malloc(ret * sizeof(WCHAR));
        ret2 = MultiByteToWideChar(CP_UTF8, 0, utf8Strings[i], -1, wideBuffer, ret);
        if (ret != ret2)
        {
            Fail("MultiByteToWideChar string %d: returned different string length for empty and real dest buffers!\n"
                "Got %d for the empty one, %d for real one.\n", i, ret2, ret);
        }
        
        if (wcscmp(wideBuffer, unicodeStrings[i]) != 0)
        {
            Fail("MultiByteToWideChar string %d: the resulting string doesn't match the expected one!\n", i);
        }
        
        free(wideBuffer);
    }
   
    PAL_Terminate();

    return PASS;
}
