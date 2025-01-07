// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

    public MSDiaSymbolReader(string pdbFile)
    {
        try
        {
            var dia140SourceClassGuid = new Guid("{e6756135-1e65-4d17-8576-610761398c3c}");
            IClassFactory diaClassFactory = (IClassFactory)DllGetClassObject(dia140SourceClassGuid, typeof(IClassFactory).GetTypeInfo().GUID);
            diaClassFactory.CreateInstance(null, typeof(IDiaDataSource).GetTypeInfo().GUID, out object comObject);
            
            _diaDataSource = (IDiaDataSource)comObject;
            _diaDataSource.loadDataFromPdb(pdbFile);
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

            Console.WriteLine("PDB file:       {0}", pdbFile);
            Console.WriteLine("Total symbols:  {0}", symbolsTotal);
            Console.WriteLine("Public symbols: {0}", _pdbSymbols.Count);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error opening PDB file {pdbFile}", ex);
        }
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
