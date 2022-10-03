// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("// Licensed to the .NET Foundation under one or more agreements.");
        Console.WriteLine("// The .NET Foundation licenses this file to you under the MIT license.");
        Console.WriteLine();

        Console.WriteLine("#include \"pal/unicodedata.h\"");

        Console.WriteLine();
        Console.WriteLine("//");
        Console.WriteLine("// THIS FILE IS GENERATED. DO NOT HAND EDIT.");
        Console.WriteLine("// IF YOU NEED TO UPDATE UNICODE VERSION FOLLOW THE GUIDE AT src/libraries/System.Private.CoreLib/Tools/GenUnicodeProp/Updating-Unicode-Versions.md");
        Console.WriteLine("//");
        Console.WriteLine();

        Console.WriteLine("CONST UnicodeDataRec UnicodeData[] = {");

        string sourceFileName = args[0];

        using (StreamReader sourceFile = File.OpenText(sourceFileName))
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

                Console.WriteLine($"  {{ 0x{code:X}, {specifier}, 0x{opposingCase:X} }},");
            }

        Console.WriteLine("};");

        Console.WriteLine("CONST UINT UNICODE_DATA_SIZE = sizeof(UnicodeData)/sizeof(UnicodeDataRec);");
    }
}
