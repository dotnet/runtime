// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Dia2Lib;

class MSDiaSymbolReader
{
    [return: MarshalAs(UnmanagedType.Interface)]
    [DllImport("msdia140.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
    private static extern object DllGetClassObject(
        [In] in Guid rclsid,
        [In] in Guid riid);

    [ComImport, ComVisible(false), Guid("00000001-0000-0000-C000-000000000046"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IClassFactory
    {
        void CreateInstance([MarshalAs(UnmanagedType.Interface)] object? aggregator,
                            [In] in Guid refiid,
                            [MarshalAs(UnmanagedType.Interface)] out object createdObject);
        void LockServer(bool incrementRefCount);
    }

    private readonly IDiaDataSource _diaDataSource;
    private readonly IDiaSession _diaSession;

    private readonly List<string> _pdbSymbols;

    public MSDiaSymbolReader(string pdbFile, string? imageFile = null)
    {
        try
        {
            var dia140SourceClassGuid = new Guid("{e6756135-1e65-4d17-8576-610761398c3c}");
            IClassFactory diaClassFactory = (IClassFactory)DllGetClassObject(dia140SourceClassGuid, typeof(IClassFactory).GetTypeInfo().GUID);
            diaClassFactory.CreateInstance(null, typeof(IDiaDataSource).GetTypeInfo().GUID, out object comObject);
            
            _diaDataSource = (IDiaDataSource)comObject;

            if (imageFile is not null)
            {
                // Validate the PDB against the image's CodeView / RSDS identity. DIA throws if the
                // PDB-info GUID / age don't match the image
                (Guid imageGuid, uint imageAge) = ReadImageCodeViewIdentity(imageFile);
                Console.WriteLine("Image file:     {0}", imageFile);
                Console.WriteLine("Image GUID:     {0}", imageGuid);
                Console.WriteLine("Image age:      {0}", imageAge);
                // sig is 0: R2R images use the RSDS (PDB 7.0) CodeView record, whose identity is the
                // GUID + age only; the legacy NB10 32-bit signature does not apply.
                _diaDataSource.loadAndValidateDataFromPdb(pdbFile, ref imageGuid, 0, imageAge);
            }
            else
            {
                _diaDataSource.loadDataFromPdb(pdbFile);
            }

            _diaDataSource.openSession(out _diaSession);

            _pdbSymbols = new List<string>();

            _diaSession.getSymbolsByAddr(out IDiaEnumSymbolsByAddr symbolEnum);
            int symbolsTotal = 0;
            for (IDiaSymbol symbol = symbolEnum.symbolByRVA(0); symbol != null; symbolEnum.Next(1, out symbol, out uint fetched))
            {
                symbolsTotal++;
                if (symbol.symTag == (uint)SymTagEnum.SymTagFunction || symbol.symTag == (uint)SymTagEnum.SymTagPublicSymbol)
                {
                    _pdbSymbols.Add(symbol.name);
                }
            }

            IDiaSymbol globalScope = _diaSession.globalScope;
            Console.WriteLine("PDB file:       {0}", pdbFile);
            Console.WriteLine("PDB GUID:       {0}", globalScope.guid);
            Console.WriteLine("PDB age:        {0}", globalScope.age);
            Console.WriteLine("Total symbols:  {0}", symbolsTotal);
            Console.WriteLine("Public symbols: {0}", _pdbSymbols.Count);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error opening PDB file {pdbFile}", ex);
        }
    }

    /// <summary>
    /// Read the CodeView / RSDS debug record identity (GUID and age) from a PE image.
    /// </summary>
    private static (Guid Guid, uint Age) ReadImageCodeViewIdentity(string imageFile)
    {
        using FileStream imageStream = File.OpenRead(imageFile);
        using PEReader peReader = new PEReader(imageStream);
        foreach (DebugDirectoryEntry entry in peReader.ReadDebugDirectory())
        {
            if (entry.Type == DebugDirectoryEntryType.CodeView)
            {
                CodeViewDebugDirectoryData codeViewData = peReader.ReadCodeViewDebugDirectoryData(entry);
                return (codeViewData.Guid, (uint)codeViewData.Age);
            }
        }

        throw new Exception($"Image file {imageFile} does not contain a CodeView (RSDS) debug directory entry");
    }

    public void DumpSymbols()
    {
        Console.WriteLine("PDB public symbol list:");
        foreach (string symbol in _pdbSymbols.OrderBy(s => s))
        {
            Console.WriteLine(symbol);
        }
        Console.WriteLine("End of PDB public symbol list");
    }

    public bool ContainsSymbol(string symbolName) => _pdbSymbols.Any(s => s.Contains(symbolName));
}
