// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#:property Configuration=Release

using System.Diagnostics;
using System.Globalization;

Console.WriteLine("""
    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.

    //
    // THIS FILE IS GENERATED. DO NOT HAND EDIT.
    // IF YOU NEED TO UPDATE UNICODE VERSION FOLLOW THE GUIDE AT src/libraries/System.Private.CoreLib/Tools/GenUnicodeProp/Updating-Unicode-Versions.md
    //

    #include <inttypes.h>
    #include <minipal/utils.h>
    #include <minipal/strings.h>

    typedef struct
    {
      CHAR16_T code;
      uint8_t  upperOrLower;
      CHAR16_T opposingCode;
    } UnicodeDataRec;

    #define UPPER_CASE 0
    #define LOWER_CASE 1

    static const UnicodeDataRec UnicodeData[] =
    {
    """);

string sourceFileName = args[0];

int numberOfCases = 0;
Span<Range> fields = stackalloc Range[15];
foreach (string line in File.ReadLines(sourceFileName))
{
    ReadOnlySpan<char> span = line;
    span.Split(fields, ';');

    var code = int.Parse(span[fields[0]], NumberStyles.HexNumber);

    ReadOnlySpan<char> upperCaseMapping = span[fields[12]];
    ReadOnlySpan<char> lowerCaseMapping = span[fields[13]];
    bool hasUpperCaseMapping = !upperCaseMapping.IsEmpty;
    bool hasLowerCaseMapping = !lowerCaseMapping.IsEmpty;

    if (!hasLowerCaseMapping && !hasUpperCaseMapping)
        continue;

    int opposingCase = int.Parse(hasUpperCaseMapping ? upperCaseMapping : lowerCaseMapping, NumberStyles.HexNumber);

    // These won't fit in 16 bits - no point carrying them
    if (code > 0xFFFF)
        continue;

    Debug.Assert(opposingCase <= 0xFFFF);

    string specifier = hasUpperCaseMapping ? "LOWER_CASE" : "UPPER_CASE";
    Console.WriteLine($$"""    { 0x{{code:X}}, {{specifier}}, 0x{{opposingCase:X}} },""");

    numberOfCases++;
}

Console.WriteLine($$"""
    };

    #define UNICODE_DATA_SIZE {{numberOfCases}}

    static int LIBC_CALLBACK UnicodeDataComp(const void *opposingCode, const void *elem)
    {
        CHAR16_T code = ((UnicodeDataRec*)elem)->code;

        if (*((CHAR16_T*)opposingCode) < code)
        {
            return -1;
        }
        else if (*((CHAR16_T*)opposingCode) > code)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    CHAR16_T minipal_toupper_invariant(CHAR16_T code)
    {
        UnicodeDataRec *record = (UnicodeDataRec *) bsearch(&code, UnicodeData, UNICODE_DATA_SIZE,
            sizeof(UnicodeDataRec), UnicodeDataComp);

        if (!record || record->upperOrLower != LOWER_CASE)
            return code;

        return record->opposingCode;
    }

    CHAR16_T minipal_tolower_invariant(CHAR16_T code)
    {
        UnicodeDataRec *record = (UnicodeDataRec *) bsearch(&code, UnicodeData, UNICODE_DATA_SIZE,
            sizeof(UnicodeDataRec), UnicodeDataComp);

        if (!record || record->upperOrLower != UPPER_CASE)
            return code;

        return record->opposingCode;
    }
    """);
