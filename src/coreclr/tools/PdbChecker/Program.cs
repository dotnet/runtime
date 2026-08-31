// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Dia2Lib;
class Program
{
    public static int Main(string[] args)
    {
        try
        {
            TryMain(args);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Fatal error: {0}", ex);
            return 1;
        }
    }

    private static void TryMain(string[] args)
    {
        if (args.Length == 0)
        {
            DisplayUsage();
            return;
        }

        string pdbFile = args[0];
        string? imageFile = null;
        List<string> symbolNames = new List<string>();

        for (int argIndex = 1; argIndex < args.Length; argIndex++)
        {
            string arg = args[argIndex];
            if (arg.Equals("--image", StringComparison.OrdinalIgnoreCase))
            {
                if (++argIndex >= args.Length)
                {
                    throw new Exception("Missing image file path after --image");
                }
                imageFile = args[argIndex];
            }
            else
            {
                symbolNames.Add(arg);
            }
        }

        // When an image is provided, MSDiaSymbolReader validates that the PDB-info identity
        // matches the image's CodeView / RSDS record, guarding against the zero-GUID native PDB bug.
        MSDiaSymbolReader reader = new MSDiaSymbolReader(pdbFile, imageFile);

        int matchedSymbols = 0;
        int missingSymbols = 0;
        foreach (string symbolName in symbolNames)
        {
            if (reader.ContainsSymbol(symbolName))
            {
                matchedSymbols++;
            }
            else
            {
                missingSymbols++;
                Console.Error.WriteLine("Missing symbol: {0}", symbolName);
            }
        }
        if (missingSymbols > 0)
        {
            reader.DumpSymbols();
            throw new Exception($"{missingSymbols} missing symbols ({matchedSymbols} symbols matched)");
        }
        if (matchedSymbols > 0)
        {
            Console.WriteLine("Matched all {0} symbols", matchedSymbols);
        }
    }

    private static void DisplayUsage()
    {
        Console.WriteLine("Usage: PdbChecker <pdb file to check> [--image <image file to match PDB identity>] { <symbol to check for existence in the PDB file> }");
    }
}
