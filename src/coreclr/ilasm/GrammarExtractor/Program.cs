// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.RegularExpressions;
using static Patterns;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: <file>");
    return 1;
}

string filePath = args[0];

string fileContent = File.ReadAllText(filePath);

var match = GetRegexExtractMarkers().Match(fileContent);
if (!match.Success)
{
    Console.Error.WriteLine("Could not find %% markers");
    return 1;
}

//string prefix = match.Groups[1].Value;
string grammar = match.Groups[2].Value;

// Remove any text in {}
var regexRemoveTextInBraces = GetRegexRemoveTextInBraces();
string previousGrammar;

do
{
    previousGrammar = grammar;
    grammar = regexRemoveTextInBraces.Replace(grammar, "$1");
} while (grammar != previousGrammar);

// Change keyword identifiers into the string they represent (lowercase)
grammar = GetRegexKeywordIdentifiers().Replace(grammar, m => $"'{m.Groups[1].Value.ToLowerInvariant()}'");

// Change assembler directives into their string (lowercase with a period)
grammar = GetRegexAssemblerDirectives().Replace(grammar, m => $"'.{m.Groups[1].Value.ToLowerInvariant()}'");

// Handle special punctuation
grammar = GetRegexEllipsis().Replace(grammar, "'...'");
grammar = GetRegexDcolon().Replace(grammar, "'::'");

// Remove TODO comments
grammar = GetRegexRemoveTodoComments().Replace(grammar, "\n");

// Print the output header
Console.Write(@"Lexical tokens
    ID - C style alphaNumeric identifier (e.g. Hello_There2)
    DOTTEDNAME - Sequence of dot-separated IDs (e.g. System.Object)
    QSTRING  - C style quoted string (e.g.  ""hi\n"")
    SQSTRING - C style singlely quoted string(e.g.  'hi')
    INT32    - C style 32 bit integer (e.g.  235,  03423, 0x34FFF)
    INT64    - C style 64 bit integer (e.g.  -2353453636235234,  0x34FFFFFFFFFF)
    FLOAT64  - C style floating point number (e.g.  -0.2323, 354.3423, 3435.34E-5)
    INSTR_*  - IL instructions of a particular class (see opcode.def).
    HEXBYTE  - 1- or 2-digit hexadecimal number (e.g., A2, F0).
Auxiliary lexical tokens
    TYPEDEF_T - Aliased class (TypeDef or TypeRef).
    TYPEDEF_M - Aliased method.
    TYPEDEF_F - Aliased field.
    TYPEDEF_TS - Aliased type specification (TypeSpec).
    TYPEDEF_MR - Aliased field/method reference (MemberRef).
    TYPEDEF_CA - Aliased Custom Attribute.
----------------------------------------------------------------------------------
START           : decls
                ;");

// Print the output
Console.Write(grammar);

return 0;

internal static partial class Patterns
{
    [GeneratedRegex(@"^(.*)%%(.*)%%", RegexOptions.Singleline)]
    internal static partial Regex GetRegexExtractMarkers();

    [GeneratedRegex(@"\s*([^'])\{[^{}]*\}", RegexOptions.Singleline)]
    internal static partial Regex GetRegexRemoveTextInBraces();

    [GeneratedRegex(@"\b([A-Z0-9_]+)_\b", RegexOptions.Singleline)]
    internal static partial Regex GetRegexKeywordIdentifiers();

    [GeneratedRegex(@"\b_([A-Z0-9]+)\b", RegexOptions.Singleline)]
    internal static partial Regex GetRegexAssemblerDirectives();

    [GeneratedRegex(@"\bELLIPSIS\b", RegexOptions.Singleline)]
    internal static partial Regex GetRegexEllipsis();

    [GeneratedRegex(@"\bDCOLON\b", RegexOptions.Singleline)]
    internal static partial Regex GetRegexDcolon();

    [GeneratedRegex(@"\n\s*/\*[^\n]*TODO[^\n]*\*/\s*\n", RegexOptions.Singleline)]
    internal static partial Regex GetRegexRemoveTodoComments();
}
