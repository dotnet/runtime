// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

#if !FEATURE_PAL
using Dia;
using Dia.Util;
#endif // !FEATURE_PAL
using System.Globalization;

/******************************************************************************
 *
 *****************************************************************************/

public abstract class SymbolProvider
{
    public enum SymType
    {
        GlobalData,
        GlobalFunction,
    };

    public abstract UInt32 GetGlobalRVA(String symbolName,
                                        SymType symType);
    public abstract UInt32 GetVTableRVA(String symbolName,
                                        String keyBaseName);
}

#if !FEATURE_PAL
public class PdbSymbolProvider : SymbolProvider
{
    public PdbSymbolProvider(String symbolFilename, String dllFilename)
    {
        fPDB = new FileInfo(symbolFilename);
        df = new DiaFile(fPDB.FullName, dllFilename);
    }

    public UInt32 DebugTimestamp
    {
        get { return df.DebugTimestamp; }
    }

    public string LoadedPdbPath
    {
        get { return df.LoadedPdbPath; }
    }

    private UInt32 GetSymbolRva(DiaSymbol sy,
                                String symbolName,
                                String typeName)
    {
        if (sy == null)
        {
            // Ideally this would throw an exception and
            // cause the whole process to fail but
            // currently it's too complicated to get
            // all the ifdef'ing right for all the
            // mix of debug/checked/free multiplied by
            // x86/AMD64/IA64/etc.
            return UInt32.MaxValue;
        }
        if (sy.Address > UInt32.MaxValue)
        {
            throw new InvalidOperationException(typeName +
                                                " symbol " +
                                                symbolName +
                                                " overflows UInt32");
        }

        return (UInt32)sy.Address;
    }

    private DiaSymbol GetValidPublicSymbolEntry(String name)
    {
        IDiaEnumSymbols e = df.FindPublicSymbols(name);

        if (e.count != 1)
        {
            return null;
        }
        else
        {
            IDiaSymbol s;
            UInt32 celt;

            e.Next(1, out s, out celt);
            return new DiaSymbol(s);
        }
    }

    public override UInt32 GetGlobalRVA(String symbolName,
                                        SymType symType)
    {
        DiaSymbol sy = df.GlobalSymbol.FindSymbol(symbolName);
        if (sy == null && symType == SymType.GlobalFunction)
        {
            // Try looking for the symbol in public symbols,
            // as assembly routines do not have normal
            // global symbol table entries.  We don't know
            // how many parameters to use, so just guess
            // at a few sizes.
            for (int i = 0; i <= 16; i += 4)
            {
                // Non-fastcall.
                sy = GetValidPublicSymbolEntry("_" + symbolName + "@" + i);
                if (sy != null)
                {
                    break;
                }
                // Fastcall.
                sy = GetValidPublicSymbolEntry("@" + symbolName + "@" + i);
                if (sy != null)
                {
                    break;
                }
            }
        }

        return GetSymbolRva(sy, symbolName, "Symbol");
    }

    public override UInt32 GetVTableRVA(String symbolName,
                                        String keyBaseName)
    {
        String mangledName;

        // Single-inheritance vtable symbols have different
        // mangling from multiple-inheritance so form the
        // proper name based on which case this is.
        mangledName = "??_7" + symbolName + "@@6B";
        if (keyBaseName != null)
        {
            mangledName += keyBaseName + "@@@";
        }
        else
        {
            mangledName += "@";
        }

        return GetSymbolRva(GetValidPublicSymbolEntry(mangledName),
                            symbolName, "VTable");
    }

    FileInfo fPDB = null;
    DiaFile df = null;
}
#endif // !FEATURE_PAL

public class Shell
{
    const String dacSwitch   = "/dac:";
    const String pdbSwitch   = "/pdb:";
    const String mapSwitch   = "/map:";
    const String binSwitch   = "/bin:";
    const String dllSwitch   = "/dll:";
    const String ignoreErrorsSwitch = "/ignoreerrors";

    public static void Help()
    {
        HelpHdr();
        Console.WriteLine();
        HelpBody();
    }

    public static void HelpHdr()
    {
String helpHdr =

////////////
@"Microsoft (R) CLR External Data Access Data Table Generator Version 0.3
Copyright (C) Microsoft Corp.  All rights reserved.";
////////////

        Console.WriteLine(helpHdr);
    }

    public static void HelpBody()
    {

String helpMsg =

////////////
@"Usage:
  DacTableGen /dac:<file> [/pdb:<file> /dll:<file>] [/map:<file>] /bin:<file> [/ignoreerrors]

Required:
  /dac:   The data access header file containing items to be added.
  /pdb:   The PDB file from which to get details.
  /map:   The MAP file from which to get details.
          In Windows, this file is created by providing /MAP in link.exe.
          In UNIX, this file is created by the nm utility.
          OBSOLETE - Use DacTableGen.pl instead for UNIX systems
  /dll:   The DLL which matches the specified PDB or MAP file.
  /bin:   The binary output file.
  /ignoreerrors: Turn errors into warnings. The produced binary may hit
                 runtime failures
";
////////////

        Console.WriteLine(helpMsg);
    }

    public static bool MatchArg(String arg, String cmd)
    {
        if (arg.Length >= cmd.Length &&
            arg.Substring(0, cmd.Length).ToLower(CultureInfo.InvariantCulture).Equals(cmd.ToLower(CultureInfo.InvariantCulture)))
            return true;

        return false;
    }

    public static int DoMain(String[] args)
    {
        String dacFile    = null;
        String pdbFile    = null;
        String mapFile    = null;
        String binFile    = null;
        String dllFile    = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (MatchArg(args[i], dacSwitch))
            {
                dacFile = args[i].Substring(dacSwitch.Length);
            }
            else if (MatchArg(args[i], pdbSwitch))
            {
                pdbFile = args[i].Substring(pdbSwitch.Length);
            }
            else if (MatchArg(args[i], mapSwitch))
            {
                mapFile = args[i].Substring(mapSwitch.Length);
            }
            else if (MatchArg(args[i], binSwitch))
            {
                binFile = args[i].Substring(binSwitch.Length);
            }
            else if (MatchArg(args[i], dllSwitch))
            {
                dllFile = args[i].Substring(dllSwitch.Length);
            }
            else if (MatchArg(args[i], ignoreErrorsSwitch))
            {
                s_ignoreErrors = true;
            }
            else
            {
                Help();
                return 1;
            }
        }

        if (dacFile == null ||
            (pdbFile == null && mapFile == null) ||
            (dllFile == null && pdbFile != null) ||
            binFile == null)
        {
            HelpHdr();
            Console.WriteLine();
            Console.WriteLine("Required option missing.");
            // Provide some extra help if just the new dllFile option is missing
            if ((dllFile == null) && (dacFile != null) && (binFile != null) && (pdbFile != null))
            {
                Console.WriteLine("NOTE that /dll is a new required argument which must point to mscorwks.dll.");
                Console.WriteLine("Ideally all uses of DacTableGen.exe should use the build logic in ndp/clr/src/DacUpdateDll.");
            }
            Console.WriteLine();
            HelpBody();

            return 1;
        }

        // Validate the specified files exist
        string[] inputFiles = new string[] { dacFile, pdbFile, mapFile, dllFile };
        foreach (string file in inputFiles)
        {
            if (file != null && !File.Exists(file))
            {
                Console.WriteLine("ERROR, file does not exist: " + file);
                return 1;
            }
        }

        HelpHdr();
        Console.WriteLine();

        List<UInt32> rvaArray = new List<UInt32>();
        UInt32 numGlobals;
        UInt32 debugTimestamp = 0;

        if (pdbFile != null)
        {
#if FEATURE_PAL
            throw new InvalidOperationException("PDBs are only supported on Windows.");
#else
            PdbSymbolProvider pdbSymProvider = new PdbSymbolProvider(pdbFile, dllFile);

            // Read the mscorwks debug directory timestamp
            debugTimestamp = pdbSymProvider.DebugTimestamp;
            if (debugTimestamp == 0)
            {
                throw new System.ApplicationException("Didn't get debug directory timestamp from DIA");
            }
            DateTime dt = new DateTime(1970, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(debugTimestamp).ToLocalTime();

            // Output information about the PDB loaded
            Console.WriteLine("Processing DLL with PDB timestamp: {0}", dt.ToString("F"));
            Console.WriteLine("Loaded PDB file: " + pdbSymProvider.LoadedPdbPath);
            if (Path.GetFullPath(pdbSymProvider.LoadedPdbPath).ToLowerInvariant() != Path.GetFullPath(pdbFile).ToLowerInvariant())
            {
                // DIA loaded a PDB oter than the one the user asked for.  This could possibly happen if the PDB
                // also exists in a sub-directory that DIA automatically probes for ("retail" etc.).  There doesn't
                // appear to be any mechanism for turning this sub-directory probing off, but all other searching mechanisms
                // should be turned off by the DiaLoadCallback.  This could also happen if the user specified an incorrect
                // (but still existing) filename in a path containing the real PDB.  Since DIA loaded it, it must match the DLL,
                // and so should only be an exact copy of the requested PDB (if the requested PDB actually matches the DLL).  So
                // go ahead and use it anyway with a warning.  To be less confusing, we could update the command-line syntax
                // to take a PDB search path instead of a filename, but that inconsistent with the map path, and probably not
                // worth changing semantics for.  In practice this warning will probably never be hit.
                Shell.Error("Loaded PDB path differs from requested path: " + pdbFile);
            }
            Console.WriteLine();

            ScanDacFile(dacFile,
                pdbSymProvider,
                rvaArray,
                out numGlobals);

            if (mapFile != null)
            {
                List<UInt32> mapRvaArray = new List<UInt32>();
                UInt32 mapNumGlobals;

                // check that both map file and pdb file produce same output to avoid breakages
                ScanDacFile(dacFile,
                    new MapSymbolProvider(mapFile),
                    mapRvaArray,
                    out mapNumGlobals);

                // Produce a nice message to include with any errors.  For some reason, binplace will silently fail
                // when a PDB can't be updated due to file locking.  This means that problems of this nature usually
                // occur when mscorwks.pdb was locked when mscorwks.dll was last rebuilt.
                string diagMsg = String.Format(".  This is usually caused by mscorwks.pdb and mscorwks.map being out of sync.  " +
                    "Was {0} (last modified {1}) in-use and locked when {2} was built (last modified {3})?  " +
                    "Both should have been created when {4} was last rebuilt (last modified {5}).",
                    pdbFile, File.GetLastWriteTime(pdbFile),
                    mapFile, File.GetLastWriteTime(mapFile),
                    dllFile, File.GetLastWriteTime(dllFile));

                if (rvaArray.Count != mapRvaArray.Count)
                    throw new InvalidOperationException("Number of RVAs differes between pdb file and map file: " +
                        numGlobals + " " + mapNumGlobals + diagMsg);

                for (int i = 0; i < rvaArray.Count; i++)
                {
                    if (rvaArray[i] != mapRvaArray[i]
                        // it is ok if we find more stuff in the MAP file
                        && rvaArray[i] != UInt32.MaxValue)
                    {
                        throw new InvalidOperationException("RVAs differ between pdb file and map file: " +
                            ToHexNB(rvaArray[i]) + " " + ToHexNB(mapRvaArray[i]) + diagMsg);
                    }
                }

                if (numGlobals != mapNumGlobals)
                    throw new InvalidOperationException("Number of globals differes between pdb file and map file: " +
                        numGlobals + " " + mapNumGlobals + diagMsg);
            }
#endif
        }
        else
        {
            ScanDacFile(dacFile,
                new MapSymbolProvider(mapFile),
                rvaArray,
                out numGlobals);
        }

        if (s_errors && !s_ignoreErrors)
        {
            Console.Error.WriteLine(
                "DacTableGen : fatal error : Failing due to above validation errors. " +
                "Do you have an #ifdef (or name) mismatch between the symbol definition and the entry specified? " +
                "Or perhaps the symbol referenced was optimized away as unused? " +
                "If you're stuck, send e-mail to 'ClrDac'.  Worst case, these errors can be temporarily ignored by passing the /ignoreerrors switch - but you may cause runtime failures instead.");
            return 1;
        }

        UInt32 numVptrs;
        numVptrs = (UInt32)rvaArray.Count - numGlobals;

        FileStream outFile = new FileStream(binFile, FileMode.Create,
                                            FileAccess.Write);
        BinaryWriter binWrite = new BinaryWriter(outFile);

        // Write header information
        binWrite.Write(numGlobals);
        binWrite.Write(numVptrs);
        binWrite.Write(debugTimestamp);
        binWrite.Write(0);                  // On Windows we only need a 4-byte timestamp, but on Mac we use
        binWrite.Write(0);                  // a 16-byte UUID.  We need to be consistent here.
        binWrite.Write(0);

        // Write out the table of RVAs
        for (int i = 0; i < numGlobals + numVptrs; i++)
        {
            binWrite.Write(rvaArray[i]);
        }

        binWrite.Close();
        return 0;
    }

    public static int Main(string[] args)
    {
        // Don't catch exceptions if a debugger is attached - makes debugging easier
        if (System.Diagnostics.Debugger.IsAttached)
        {
            return DoMain(args);
        }

        int exitCode;
        try
        {
            exitCode = DoMain(args);
        }
        catch(Exception e)
        {
            Console.WriteLine("BUILDMSG: " + e.ToString());
            exitCode = 1;
        }
        return exitCode;
    }

    private static void ScanDacFile(String file,
                                    SymbolProvider sf,
                                    List<UInt32> rvaArray,
                                    out UInt32 numGlobals)
    {
        StreamReader strm =
            new StreamReader(file, System.Text.Encoding.ASCII);
        String line;
        Hashtable vtables = new Hashtable(); // hashtable to guarantee uniqueness of entries

        //
        // Scan through the data access header file looking
        // for the globals structure.
        //

        for (;;)
        {
            line = strm.ReadLine();
            if (line == null)
            {
                throw new
                    InvalidOperationException("Invalid dac header format");
            }
            else if (line == "typedef struct _DacGlobals")
            {
                break;
            }
        }

        if (strm.ReadLine() != "{")
        {
            throw new InvalidOperationException("Invalid dac header format");
        }

        //
        // All the globals come first so pick up each line that
        // begins with ULONG.
        //

        bool fFoundVptrs = false;
        numGlobals = 0;

        for (;;)
        {
            line = strm.ReadLine().Trim();

            if (   line.Equals("union {")
                || line.Equals("struct {")
                || line.Equals("};")
                || line.StartsWith("#line ")
                || line.StartsWith("# "))
            {
                // Ignore.
            }
            else if (line.StartsWith("ULONG "))
            {
                UInt32 rva = 0;

                line = line.Remove(0, 6);
                line = line.TrimEnd(";".ToCharArray());

                string vptrSuffixSingle = "__vtAddr";
                string vptrSuffixMulti = "__mvtAddr";
                string vptrSuffix = null;

                if (line.EndsWith(vptrSuffixSingle))
                {
                    vptrSuffix = vptrSuffixSingle;
                }
                else if (line.EndsWith(vptrSuffixMulti))
                {
                    vptrSuffix = vptrSuffixMulti;
                }

                if (vptrSuffix != null)
                {
                    if (!fFoundVptrs)
                    {
                        numGlobals = (UInt32)rvaArray.Count;
                        fFoundVptrs = true;
                    }

                    line = line.Remove(line.Length - vptrSuffix.Length,
                                       vptrSuffix.Length);

                    string keyBaseName = null;
                    string descTail = null;

                    if (vptrSuffix == vptrSuffixMulti)
                    {
                        // line now has the form <class>__<base>, so
                        // split off the base.
                        int basePrefix = line.LastIndexOf("__");
                        if (basePrefix < 0)
                        {
                            throw new InvalidOperationException("VPTR_MULTI_CLASS has no keyBase.");
                        }
                        keyBaseName = line.Substring(basePrefix + 2);
                        line = line.Remove(basePrefix);
                        descTail = " for " + keyBaseName;
                    }

                    rva = sf.GetVTableRVA(line, keyBaseName);

                    if (rva == UInt32.MaxValue)
                    {
                        Console.WriteLine("    " + ToHexNB(rva));
                        Shell.Error("Invalid vtable " + line + descTail);
                    }
                    else
                    {
                        String existing = (String)vtables[rva];
                        if (existing != null)
                        {
                            throw new InvalidOperationException(existing + " and " + line + " are at the same offsets." +
                                 " Add VPTR_UNIQUE(<a random unique number here>) to the offending classes to make their vtables unique.");
                        }
                        vtables[rva] = line;

                        Console.WriteLine("    " + ToHexNB(rva) +
                                          ", // vtable " + line + descTail);
                    }
                }
                else
                {
                    SymbolProvider.SymType symType;

                    if (fFoundVptrs)
                        throw new InvalidOperationException("Invalid dac header format.  Vtable pointers must be last.");

                    if (line.StartsWith("dac__"))
                    {
                        // Global variables, use the prefix.
                        line = line.Remove(0, 5);
                        symType = SymbolProvider.SymType.GlobalData;
                    }
                    else if (line.StartsWith("fn__"))
                    {
                        // Global or static functions, use the prefix.
                        line = line.Remove(0, 4);
                        line = line.Replace("__", "::");
                        symType = SymbolProvider.SymType.GlobalFunction;
                    }
                    else
                    {
                        // Static member variable, use the full name with
                        // namespace replacement.
                        line = line.Replace("__", "::");
                        symType = SymbolProvider.SymType.GlobalData;
                    }

                    if (0 == rva)
                    {
                        rva = sf.GetGlobalRVA(line, symType);

                        if (rva == UInt32.MaxValue)
                        {
                            Console.WriteLine("    " + ToHexNB(rva));
                            Shell.Error("Invalid symbol " + line);
                        }
                        else
                        {
                            Console.WriteLine("    " + ToHexNB(rva) + ", // " +
                                              line);
                        }
                    }
                }

                rvaArray.Add(rva);

            }
            else if (line == "")
            {
                // Skip blanks.
            }
            else
            {
                // We hit a non-global so we're done.
                if (!line.Equals("} DacGlobals;"))
                {
                    throw new
                        InvalidOperationException("Invalid dac header format at \"" + line + "\"");
                }
                break;
            }
        }

        if (!fFoundVptrs)
            throw new InvalidOperationException("Invalid dac header format.  Vtable pointers not found.");
    }

    private static String ToHex(Object o)
    {
        if (o is UInt32 || o is Int32)
            return String.Format("0x{0:x8}", o);
        else if (o is UInt64 || o is Int64)
            return String.Format("0x{0:x16}", o);
        else
            return null;
    }

    private static String ToHexNB(Object o)
    {
        return String.Format("0x{0:x}", o);
    }

    public static void Error(string message)
    {
        Console.Error.WriteLine((s_ignoreErrors ? "WARNING: " : "ERROR: ") + message);
        s_errors = true;
    }

    // Try to tolerate errors (as we've always done in the past), which may result in failures at run-time instead.
    private static bool s_ignoreErrors = false;
    private static bool s_errors = false;
}
