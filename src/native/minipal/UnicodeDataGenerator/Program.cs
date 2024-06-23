// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

Console.WriteLine(@"// Licensed to the .NET Foundation under one or more agreements.
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
{");

string sourceFileName = args[0];

int numberOfCases = 0;
using StreamReader sourceFile = File.OpenText(sourceFileName);
while (sourceFile.ReadLine() is string line)
{
    var fields = line.Split(';');

    var code = int.Parse(fields[0], NumberStyles.HexNumber);

    bool hasUpperCaseMapping = fields[12].Length != 0;
    bool hasLowerCaseMapping = fields[13].Length != 0;

    if (!hasLowerCaseMapping && !hasUpperCaseMapping)
        continue;


    int opposingCase = hasUpperCaseMapping ?
        int.Parse(fields[12], NumberStyles.HexNumber) :
        int.Parse(fields[13], NumberStyles.HexNumber);

    // These won't fit in 16 bits - no point carrying them
    if (code > 0xFFFF)
        continue;

    Debug.Assert(opposingCase <= 0xFFFF);

    string specifier = hasUpperCaseMapping ? "LOWER_CASE" : "UPPER_CASE";
    Console.WriteLine($"    {{ 0x{code:X}, {specifier}, 0x{opposingCase:X} }},");

    numberOfCases++;
}

Console.WriteLine("};");

Console.WriteLine($@"
#define UNICODE_DATA_SIZE {numberOfCases}");

Console.WriteLine(@"
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
}");
