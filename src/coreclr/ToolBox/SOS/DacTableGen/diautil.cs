// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.IO;
using DIALib;
using System.Runtime.InteropServices;
using System.Diagnostics;

/***************************************************************************************
 *
 ***************************************************************************************/

namespace Dia.Util
{

/***************************************************************************************
 *
 ***************************************************************************************/

class Util
{
    public static IDiaSymbol FindSymbol(String symName, IDiaSymbol parent, SymTagEnum symTag)
    {
        IDiaEnumSymbols e;
        parent.findChildren(symTag,
                            symName,
                            (uint)(NameSearchOptions.nsfCaseSensitive),
                            out e);

        IDiaSymbol s;
        uint celt;

        if (e == null || e.count == 0)
            return null;

        e.Next(1, out s, out celt);

        if (e.count > 1)
        {
            for (int i = 1; i < e.count; i++)
            {
                IDiaSymbol s2;
                e.Next(1, out s2, out celt);

                // Diasym reader returns multiple symbols with same RVA in some cases. Issue the warning only
                // if the returned symbols actually differ.
                if (s.virtualAddress != s2.virtualAddress)
                {
                    Shell.Error("Symbol " + symName + " has " + e.count + " matches. Taking first.");
                    break;
                }
            }
        }

        return s;
    }

    public static IDiaSymbol FindClassSymbol(String name, IDiaSymbol sym, SymTagEnum tag)
    {
        IDiaSymbol res = null;
        //Console.WriteLine("Looking for " + name + " in " + sym.name);

        res = Util.FindSymbol(name, sym, tag);

        if (res == null)
        {
            IDiaEnumSymbols e;

            sym.findChildren(
                SymTagEnum.SymTagBaseClass,
                null,
                (uint)NameSearchOptions.nsNone,
                out e);

            if (e == null || e.count == 0)
                return null;

            for (int i = 0; i < e.count && res == null; i++)
            {
                UInt32 celt;
                IDiaSymbol s;
                e.Next(1, out s, out celt);
                res = FindClassSymbol(name, s.type, tag);
            }
        }

        return res;
    }
}

/***************************************************************************************
 *
 ***************************************************************************************/

public class DiaFile
{
    IDiaDataSource  m_dsc;
    IDiaSession     m_session;
    DiaSymbol       m_global;
    IDiaEnumSymbols m_publicsEnum;
    UInt32          m_debugTimestamp;
    String          m_loadedPdbPath;

    public DiaFile(String pdbFile, String dllFile)
    {
        m_dsc = GetDiaSourceClass();
        string pdbPath = System.IO.Path.GetDirectoryName(pdbFile);

        // Open the PDB file, validating it matches the supplied DLL file
        DiaLoadCallback loadCallback = new DiaLoadCallback();
        try
        {
            m_dsc.loadDataForExe(dllFile, pdbPath, loadCallback);
        }
        catch (System.Exception diaEx)
        {
            // Provide additional diagnostics context and rethrow
            string msg = "ERROR from DIA loading PDB for specified DLL";
            COMException comEx = diaEx as COMException;
            if (comEx != null)
            {
                if (Enum.IsDefined(typeof(DiaHResults), comEx.ErrorCode))
                {
                    // This is a DIA-specific error code,
                    DiaHResults hr = (DiaHResults)comEx.ErrorCode;
                    msg += ": " + hr.ToString();

                    // Additional clarification for the common case of the DLL not matching the PDB
                    if (hr == DiaHResults.E_PDB_NOT_FOUND)
                    {
                        msg += " - The specified PDB file does not match the specified DLL file";
                    }
                }
            }
            throw new ApplicationException(msg, diaEx);
        }

        // Save the path of the PDB file actually loaded
        Debug.Assert(loadCallback.LoadedPdbPath != null, "Didn't get PDB load callback");
        m_loadedPdbPath = loadCallback.LoadedPdbPath;

        // Also use DIA to get the debug directory entry in the DLL referring
        // to the PDB, and save it's timestamp comparison at runtime.
        m_debugTimestamp = loadCallback.DebugTimeDateStamp;
        Debug.Assert(m_debugTimestamp != 0, "Didn't find debug directory entry");

        m_dsc.openSession(out m_session);
        m_global = new DiaSymbol(m_session.globalScope);
        m_publicsEnum = null;
    }

    public DiaSymbol GlobalSymbol
    {
        get
        { return m_global; }
    }

    /// <summary>
    /// Path of the PDB file actually loaded (must be non-null)
    /// </summary>
    public String LoadedPdbPath
    {
        get { return m_loadedPdbPath; }
    }

    /// <summary>
    /// Timestamp in the debug directory of the DLL corresponding to the PDB loaded (always set non-zero).
    /// </summary>
    public UInt32 DebugTimestamp
    {
        get { return m_debugTimestamp; }
    }

    private void GetPublicsEnum()
    {
        if (m_publicsEnum != null)
            return;

        m_session.findChildren(m_global.IDiaSymbol, SymTagEnum.SymTagPublicSymbol,
                               null, (UInt32) NameSearchOptions.nsNone, out m_publicsEnum);
    }

    public IDiaEnumSymbols PublicSymbols
    {
        get
        {
            GetPublicsEnum();

            if (m_publicsEnum != null)
            {
                IDiaEnumSymbols en;
                m_publicsEnum.Clone(out en);
                return en;
            }
            else
                return null;
        }
    }

    public IDiaEnumSymbols FindPublicSymbols(String name)
    {
        IDiaEnumSymbols se;
        m_session.findChildren(m_global.IDiaSymbol, SymTagEnum.SymTagPublicSymbol,
                               name, (UInt32) NameSearchOptions.nsNone, out se);

        return se;
    }

    // Use only the path we supply, don't look for PDBs anywhere else
    private class DiaLoadCallback : IDiaLoadCallback2
    {
        /// <summary>
        /// The path from with the PDB file was actually loaded, or null if none yet.
        /// </summary>
        public string LoadedPdbPath
        {
            get { return m_loadedPdbPath; }
        }

        /// <summary>
        /// The time stamp in the debug directory corresponding to the PDB that was loaded
        /// </summary>
        public UInt32 DebugTimeDateStamp
        {
            get { return m_debugTimeDateStamp; }
        }

        private string m_loadedPdbPath = null;
        private UInt32 m_debugTimeDateStamp = 0;

        public void NotifyDebugDir(int fExecutable, uint cbData, ref IMAGE_DEBUG_DIRECTORY pbData)
        {
            Debug.Assert(cbData == Marshal.SizeOf(typeof(IMAGE_DEBUG_DIRECTORY)), "Got unexpected size for IMAGE_DEBUG_DIRECTORY");
            // There may be mutliple calls, or calls with no timestamp, but only one entry should be
            // for the code-view record describing the PDB file.
            if (pbData.Type == ImageDebugType.IMAGE_DEBUG_TYPE_CODEVIEW)
            {
                Debug.Assert(fExecutable == 1, "Got debug directory that wasn't read from an executable");
                Debug.Assert(m_debugTimeDateStamp == 0, "Got unexpected duplicate NotifyDebugDir callback");
                Debug.Assert(pbData.TimeDateStamp != 0, "Got unexpected 0 debug timestamp in DLL");
                m_debugTimeDateStamp = pbData.TimeDateStamp;
            }
        }

        public void NotifyOpenDBG(string dbgPath, int resultCode)
        {
            Debug.Assert(false, "Unexpected DBG opening: " + dbgPath);
        }

        public void NotifyOpenPDB(string pdbPath, int resultCode)
        {
            // Keep track of the path from which DIA loaded the PDB.
            // If resultCode is non-zero, it means DIA is not loading this PDB (eg. just probing paths
            // that may not exist).
            // The DIA SDK docs say the caller will generally ignore any error HResult we return, so we
            // don't get the chance to reject any non-matching paths.
            if (resultCode >= 0)
            {
                Debug.Assert(m_loadedPdbPath == null, "DIA indicated it was loading more than one PDB file!");
                m_loadedPdbPath = pdbPath;
            }
        }

        public int RestrictRegistryAccess()
        {
            return 1;   // don't query the registry
        }

        public int RestrictSymbolServerAccess()
        {
            return 1;   // don't use the symbol server
        }

        public int RestrictDBGAccess()
        {
            return 1;   // don't look for DBG files
        }

        public int RestrictOriginalPathAccess()
        {
            return 1;   // don't look in the full path specified in the debug directory
        }

        public int RestrictReferencePathAccess()
        {
            return 1;   // Don't look in the directory next to the DLL
        }

        public int RestrictSystemRootAccess()
        {
            return 1;   // Don't look in the system directory
        }
    }

    // From WinNt.h
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct IMAGE_DEBUG_DIRECTORY
    {
        public UInt32 Characteristics;
        public UInt32 TimeDateStamp;
        public UInt16 MajorVersion;
        public UInt16 MinorVersion;
        public ImageDebugType Type;
        public UInt32 SizeOfData;
        public UInt32 AddressOfRawData;
        public UInt32 PointerToRawData;
    }
    private enum ImageDebugType : uint
    {
        IMAGE_DEBUG_TYPE_UNKNOWN = 0,
        IMAGE_DEBUG_TYPE_COFF = 1,
        IMAGE_DEBUG_TYPE_CODEVIEW = 2,
        IMAGE_DEBUG_TYPE_FPO = 3,
        IMAGE_DEBUG_TYPE_MISC = 4,
        IMAGE_DEBUG_TYPE_EXCEPTION = 5,
        IMAGE_DEBUG_TYPE_FIXUP = 6,
        IMAGE_DEBUG_TYPE_BORLAND = 9
    }

    // DIA Callback interface definitions
    // These should be in the interop assembly, but due to a bug in dia2.idl, they're missing.
    // dia2.idl should include these interfaces in the library section to get them into the TLB.
    // However, it's handy to be able to define them ourselves, because it lets us easily
    // set the interface that's most convenient (specifically PreserveSig, and explicit use
    // of IMAGE_DEBUG_DIRECTORY)
    [ComImport,
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
     Guid("C32ADB82-73F4-421B-95D5-A4706EDF5DBE"),
     System.Security.SuppressUnmanagedCodeSecurity]
    private interface IDiaLoadCallback
    {
        void NotifyDebugDir(int fExecutable, uint cbData, [In] ref IMAGE_DEBUG_DIRECTORY pbData);

        void NotifyOpenDBG([In, MarshalAs(UnmanagedType.LPWStr)] string dbgPath, int resultCode);

        void NotifyOpenPDB([In, MarshalAs(UnmanagedType.LPWStr)] string pdbPath, int resultCode);

        [PreserveSig]
        // return S_OK (0) to allow symbol path lookup from registry
        int RestrictRegistryAccess();

        [PreserveSig]
        // return S_OK (0) to allow symbol server lookup
        int RestrictSymbolServerAccess();
    }

    [ComImport,
     Guid("4688A074-5A4D-4486-AEA8-7B90711D9F7C"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
     System.Security.SuppressUnmanagedCodeSecurity]
    private interface IDiaLoadCallback2 : IDiaLoadCallback
    {
        new void NotifyDebugDir(int fExecutable, uint cbData, [In] ref IMAGE_DEBUG_DIRECTORY pbData);

        new void NotifyOpenDBG([In, MarshalAs(UnmanagedType.LPWStr)] string dbgPath, int resultCode);

        new void NotifyOpenPDB([In, MarshalAs(UnmanagedType.LPWStr)] string pdbPath, int resultCode);

        [PreserveSig]
        // return S_OK (0) to allow symbol path lookup from registry
        new int RestrictRegistryAccess();

        [PreserveSig]
        // return S_OK (0) to allow symbol server lookup
        new int RestrictSymbolServerAccess();

        [PreserveSig]
        // return S_OK (0) to allow symbol path lookup from registry
        int RestrictDBGAccess();

        [PreserveSig]
        // return S_OK (0) to allow symbol path lookup from registry
        int RestrictOriginalPathAccess();

        [PreserveSig]
        // return S_OK (0) to allow symbol path lookup from registry
        int RestrictReferencePathAccess();

        [PreserveSig]
        // return S_OK (0) to allow symbol lookup in the system directory
        int RestrictSystemRootAccess();
    }

    // DIA specific HResults from dia2.idl, for providing better diagnostics messages
    private enum DiaHResults : int
    {
        E_PDB_OK = unchecked((int)0x806d0001),
        E_PDB_USAGE                 ,
        E_PDB_OUT_OF_MEMORY         , // not used, use E_OUTOFMEMORY
        E_PDB_FILE_SYSTEM           ,
        E_PDB_NOT_FOUND             ,
        E_PDB_INVALID_SIG           ,
        E_PDB_INVALID_AGE           ,
        E_PDB_PRECOMP_REQUIRED      ,
        E_PDB_OUT_OF_TI             ,
        E_PDB_NOT_IMPLEMENTED       ,   // use E_NOTIMPL
        E_PDB_V1_PDB                ,
        E_PDB_FORMAT                ,
        E_PDB_LIMIT                 ,
        E_PDB_CORRUPT               ,
        E_PDB_TI16                  ,
        E_PDB_ACCESS_DENIED         ,  // use E_ACCESSDENIED
        E_PDB_ILLEGAL_TYPE_EDIT     ,
        E_PDB_INVALID_EXECUTABLE    ,
        E_PDB_DBG_NOT_FOUND         ,
        E_PDB_NO_DEBUG_INFO         ,
        E_PDB_INVALID_EXE_TIMESTAMP ,
        E_PDB_RESERVED              ,
        E_PDB_DEBUG_INFO_NOT_IN_PDB ,
        E_PDB_SYMSRV_BAD_CACHE_PATH ,
        E_PDB_SYMSRV_CACHE_FULL     ,
        E_PDB_MAX
    }

    // Get the DiaSourceClass from the msdia140.dll in the app directory without using COM activation
    static IDiaDataSource GetDiaSourceClass() {
	    // This is Class ID for the DiaSourceClass used by msdia140.
	    var diaSourceClassGuid = new Guid("{e6756135-1e65-4d17-8576-610761398c3c}");
	    var comClassFactory = (IClassFactory)DllGetClassObject(diaSourceClassGuid, typeof(IClassFactory).GUID);

	    // As the DLL to create a new instance of it
	    object comObject = null;
	    Guid iDiaDataSourceGuid = typeof(IDiaDataSource).GUID;
	    comClassFactory.CreateInstance(null, ref iDiaDataSourceGuid, out comObject);

	    // And return it as the type we expect
	    return (comObject as IDiaDataSource);
    }

    [return: MarshalAs(UnmanagedType.Interface)]
    [DllImport("msdia140.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
    private static extern object DllGetClassObject(
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);

    [ComImport, ComVisible(false), Guid("00000001-0000-0000-C000-000000000046"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IClassFactory
    {
	    void CreateInstance([MarshalAs(UnmanagedType.Interface)] object aggregator,
		    ref Guid refiid,
	        [MarshalAs(UnmanagedType.Interface)] out object createdObject);
	    void LockServer(bool incrementRefCount);
    }
}

/***************************************************************************************
 *
 ***************************************************************************************/

public class DiaSymbol
{
    protected IDiaSymbol      m_symbol;

    public DiaSymbol(IDiaSymbol symbol)
    {
        if (symbol == null)
            throw new ArgumentNullException();

        m_symbol = symbol;
    }

    public DiaSymbol FindSymbol(String name, SymTagEnum tag)
    {
        DiaSymbol res = null;
        IDiaSymbol sym = Util.FindSymbol(name, m_symbol, tag);

        if (sym != null)
        {
            if ((SymTagEnum)sym.symTag == SymTagEnum.SymTagData)
                res = new DiaDataSymbol(sym);
            else
                res = new DiaSymbol(sym);
        }

        return res;
    }

    public DiaSymbol FindSymbol(String name)
    {
        return FindSymbol(name, SymTagEnum.SymTagNull);
    }

    public DiaSymbol FindClassSymbol(String name, SymTagEnum tag)
    {
        DiaSymbol res = null;
        IDiaSymbol sym = Util.FindClassSymbol(name, m_symbol, tag);

        if (sym != null)
        {
            if ((SymTagEnum)sym.symTag == SymTagEnum.SymTagData)
                res = new DiaDataSymbol(sym);
            else
                res = new DiaSymbol(sym);
        }

        return res;
    }

    public DiaSymbol FindClassSymbol(String name)
    {
        return FindClassSymbol(name, SymTagEnum.SymTagNull);
    }

    public DiaSymbol FindUDTSymbol(String name)
    {
        DiaSymbol res = null;
        IDiaSymbol sym = Util.FindSymbol(name, m_symbol, SymTagEnum.SymTagUDT);

        if (sym == null)
        {
            sym = Util.FindSymbol(name, m_symbol, SymTagEnum.SymTagTypedef);

            if (sym != null)
                sym = sym.type;
        }

        if (sym != null)
            res = new DiaSymbol(sym);

        return res;
    }

    private String GetVariantString(Object o)
    {
        /*
        switch( v.vt )
        {
     //*    LONGLONG       VT_I8
        case VT_I8:
            printf( "%ld", v.llVal );
            break;
     //*    LONG           VT_I4
        case VT_I4:
            printf( "%d", v.lVal );
            break;
     //*    BYTE           VT_UI1
        case VT_UI1:
            printf( "%d", v.bVal);
            break;
     //*    SHORT          VT_I2
        case VT_I2:
            printf( "%d", v.iVal);
            break;
     //*    CHAR           VT_I1
        case VT_I1:
            printf( "%d", v.cVal);
            break;
     //*    USHORT         VT_UI2
        case VT_UI2:
            printf( "%d", v.uiVal);
            break;
    //*    ULONG          VT_UI4
        case VT_UI4:
            printf( "%d", v.ulVal);
            break;
     //*    ULONGLONG      VT_UI8
        case VT_UI8:
            printf( "%ld", v.ullVal);
            break;
     //*    INT            VT_INT
        case VT_INT:
            printf( "%d", v.intVal);
            break;
     //*    UINT           VT_UINT
        case VT_UINT:
            printf( "%d", v.uintVal);
            break;
        default:
            printf( "<Not implemented>" );
            break;
        }
        */
        return "VARIANT";
    }

    private String GetBoundString(IDiaSymbol bound)
    {
        SymTagEnum tag = (SymTagEnum) bound.symTag;
        LocationType kind = (LocationType) bound.locationType;

        System.Diagnostics.Debugger.Break();
        if (tag == SymTagEnum.SymTagData && kind == LocationType.LocIsConstant)
        {
            return GetVariantString(bound.value);
        }

        return bound.name;
    }

    public String GetTypeString()
    {
        return GetTypeString(m_symbol);
    }

    private String GetTypeString(IDiaSymbol s)
    {
        SymTagEnum tag = (SymTagEnum) s.symTag;

        if (tag == SymTagEnum.SymTagData || tag == SymTagEnum.SymTagTypedef)
        {
            s = s.type;
            tag = (SymTagEnum) s.symTag;
        }

        StringBuilder str = new StringBuilder();

        if (s.name != null)
        {
            str.Append(s.name);
        }
        else if (tag == SymTagEnum.SymTagPointerType)
        {
            str.Append(GetTypeString(s.type));
            str.Append("*");
        }
        else if (tag == SymTagEnum.SymTagBaseType)
        {
            BasicType bt = (BasicType) s.baseType;
            str.Append($"(base type={bt}, len={s.length:d})");
        }
        else if (tag == SymTagEnum.SymTagArrayType)
        {
            str.Append(GetTypeString(s.type));

            bool succ = true;
            int i;

            try
            {
                UInt32 rank = s.rank;

                IDiaEnumSymbols e;
                s.findChildren(SymTagEnum.SymTagDimension, null, (UInt32) NameSearchOptions.nsNone, out e);

                for (i = 0; i < e.count; i++)
                {
                    IDiaSymbol ds;
                    UInt32 celt;
                    e.Next(1, out ds, out celt);

                    str.Append("[" + GetBoundString(ds.lowerBound) + ".." + GetBoundString(ds.upperBound) + "]");
                }
            }
            catch (Exception)
            {
                succ = false;
            }

            if (succ == false)
            {
                try
                {
                    succ = true;
                    IDiaEnumSymbols e;
                    s.findChildren(SymTagEnum.SymTagCustomType, null, (UInt32) NameSearchOptions.nsNone, out e);

                    for (i = 0; i < e.count; i++)
                    {
                        IDiaSymbol ds;
                        UInt32 celt;
                        e.Next(1, out ds, out celt);

                        str.Append("[" + GetTypeString(ds) + "]");
                    }
                }
                catch (Exception)
                {
                    succ = false;
                }
            }

            if (succ == false)
            {
                try
                {
                    succ = true;
                    str.Append($"[{s.length/s.type.length:d}]");
                }
                catch (Exception)
                {
                    succ = false;
                }
            }
        }
        else if (tag == SymTagEnum.SymTagFunctionType)
        {
            str.Append("Function Type");
        }
        else if (tag == SymTagEnum.SymTagCustomType)
        {
            throw new Exception("NYI");
            /*
            str.Append("Custom Type: ");
            try
            {
                str.Append(s.guid.ToString());
            }
            catch (Exception e)
            {
                try
                {
                    str.AppendFormat("{0:x}:{0:x}", s.oemId, s.oemSymbolId);
                }
                catch (Exception)
                {
                }
            }
            DWORD len = 0;
            if ( s.get_types( 0, &len, NULL ) == S_OK && len > 0 ) {
                IDiaSymbol** psyms = new IDiaSymbol*[ len ];
                s.get_types( len, &len, psyms );
                for ( DWORD i = 0; i < len; ++i ) {
                    printf( " <" );
                    printType( psyms[i] );
                    printf( ">" );
                    psyms[i]->Release();
                }
                delete [] psyms;
            }
            len = 0;
            if ( s.get_dataBytes( 0, &len, NULL ) == S_OK && len > 0 ) {
                BYTE* pdata = new BYTE[ len ];
                s.get_dataBytes( len, &len, pdata );
                printf( "<data" );
                for ( DWORD i = 0; i < len; ++i ) {
                    printf( " %02x", pdata[i] );
                }
                printf( " data>" );
                delete [] pdata;
            }
            */
        } else {
            str.Append( "No Type.");
        }

        return str.ToString();
    }

    public string Name
    {
        get
        { return m_symbol.name; }
    }

    public UInt64 Address
    {
        get
        { return m_symbol.virtualAddress; }
    }

    public UInt32 Offset
    {
        get
        { return (UInt32) m_symbol.offset; }
    }

    public UInt64 Size
    {
        get
        {
            IDiaSymbol symbol = m_symbol;
            UInt64 size = symbol.length;
            if (size != 0)
                return size;

            SymTagEnum tag = (SymTagEnum) symbol.symTag;

            if (tag == SymTagEnum.SymTagData || tag == SymTagEnum.SymTagTypedef)
            {
                symbol = symbol.type;
                tag = (SymTagEnum) symbol.symTag;

                size = symbol.length;
                if (size != 0)
                    return size;
            }

            if (tag == SymTagEnum.SymTagPointerType)
            {
                // @TODO: find a way of determining native word size
                return 4;
            }
            else
            {
                throw new Exception("Unknown length.");
            }
        }
    }

    public IDiaSymbol IDiaSymbol
    {
        get
        { return m_symbol; }
    }
}

public class DiaDataSymbol : DiaSymbol
{
    public DiaDataSymbol(IDiaSymbol symbol) : base(symbol)
    {
        if ((SymTagEnum)symbol.symTag != SymTagEnum.SymTagData)
            throw new Exception("Not a data symbol.");
    }

    public DataKind DataKind
    {
        get
        { return ((DataKind) m_symbol.dataKind); }
    }

    public bool IsGlobal
    {
        get
        { return (DataKind == DataKind.DataIsGlobal); }
    }

    public bool IsFileStatic
    {
        get
        { return (DataKind == DataKind.DataIsFileStatic); }
    }

    public bool IsMember
    {
        get
        { return (DataKind == DataKind.DataIsMember); }
    }

    public bool IsStaticMember
    {
        get
        { return (DataKind == DataKind.DataIsStaticMember); }
    }
}


} // Namespace Dia.Util
