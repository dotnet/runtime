// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        MSDiaSymbolReader reader = new MSDiaSymbolReader(args[0]);
        int matchedSymbols = 0;
        int missingSymbols = 0;
        for (int symbolArgIndex = 1; symbolArgIndex < args.Length; symbolArgIndex++)
        {
            string symbolName = args[symbolArgIndex];
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
        Console.WriteLine("Usage: PdbChecker <pdb file to check> { <symbol to check for existence in the PDB file> }");
    }
}
