// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
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

        string? pdbFile = null;
        string? imageFile = null;
        List<string> symbolNames = new List<string>();

        for (int argIndex = 0; argIndex < args.Length; argIndex++)
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
            else if (pdbFile is null)
            {
                pdbFile = arg;
            }
            else
            {
                symbolNames.Add(arg);
            }
        }

        if (pdbFile is null)
        {
            if (imageFile is not null)
            {
                throw new Exception("Missing PDB file argument");
            }
            DisplayUsage();
            return;
        }

        MSDiaSymbolReader reader = new MSDiaSymbolReader(pdbFile);

        if (imageFile is not null)
        {
            ValidatePdbMatchesImage(reader, imageFile);
        }

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

    /// <summary>
    /// Verify that the PDB carries a nonzero PDB-info identity and that its GUID and age
    /// match the CodeView / RSDS debug record embedded in the output image. This guards
    /// against native PDBs whose identity does not satisfy the image's symbol-server lookup key.
    /// </summary>
    private static void ValidatePdbMatchesImage(MSDiaSymbolReader reader, string imageFile)
    {
        CodeViewDebugDirectoryData codeViewData = default;
        bool hasCodeView = false;
        using (FileStream imageStream = File.OpenRead(imageFile))
        using (PEReader peReader = new PEReader(imageStream))
        {
            foreach (DebugDirectoryEntry entry in peReader.ReadDebugDirectory())
            {
                if (entry.Type == DebugDirectoryEntryType.CodeView)
                {
                    codeViewData = peReader.ReadCodeViewDebugDirectoryData(entry);
                    hasCodeView = true;
                    break;
                }
            }
        }

        Console.WriteLine("Image file:     {0}", imageFile);

        if (!hasCodeView)
        {
            throw new Exception($"Image file {imageFile} does not contain a CodeView (RSDS) debug directory entry");
        }

        Console.WriteLine("Image GUID:     {0}", codeViewData.Guid);
        Console.WriteLine("Image age:      {0}", codeViewData.Age);

        if (reader.PdbGuid == Guid.Empty)
        {
            throw new Exception($"PDB-info GUID is all zero; it cannot satisfy the symbol-server lookup key of image {imageFile}");
        }

        if (reader.PdbGuid != codeViewData.Guid || reader.PdbAge != codeViewData.Age)
        {
            throw new Exception(
                $"PDB-info identity {reader.PdbGuid}/{reader.PdbAge} does not match image CodeView identity {codeViewData.Guid}/{codeViewData.Age}");
        }

        Console.WriteLine("PDB identity matches image CodeView identity: {0}/{1}", reader.PdbGuid, reader.PdbAge);
    }

    private static void DisplayUsage()
    {
        Console.WriteLine("Usage: PdbChecker <pdb file to check> [--image <image file to match PDB identity>] { <symbol to check for existence in the PDB file> }");
    }
}
