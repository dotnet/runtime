// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// #define DACTABLEGEN_DEBUG

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Globalization;
/***************************************************************************************
 *
 ***************************************************************************************/

public class MapSymbolProvider : SymbolProvider
{    
    public MapSymbolProvider(String symbolFilename) {
        mf = new MapFile(symbolFilename);
    }

    public override UInt32 GetGlobalRVA(String symbolName,
                                        SymType symType) {
        return GetRVA(symbolName);
        
    }

    public override UInt32 GetVTableRVA(String symbolName,
                                        String keyBaseName) {
        if (keyBaseName != null) {
            return GetRVA(symbolName + "__" + keyBaseName);
        } else {
            return GetRVA(symbolName);
        }
    }

    UInt32 GetRVA(String symbolName) {

        SymbolInfo si = mf.FindSymbol(symbolName);
        
        if (si == null) {
            // Ideally this would throw an exception and
            // cause the whole process to fail but
            // currently it's too complicated to get
            // all the ifdef'ing right for all the
            // mix of debug/checked/free multiplied by
            // x86/AMD64/IA64/etc.
            return UInt32.MaxValue;
        }

        return mf.GetRVA(si);
    }
        
    MapFile mf = null;
}


public class SymbolInfo
{
    public SymbolInfo(int Segment, UInt32 Address)
    {
        m_Segment = Segment;
        m_Address = Address;
        m_dupFound = false;
    }

    public int Segment
    {
        get
        { return m_Segment; }
    }

    public UInt32 Address
    {
        get
        { return m_Address; }
    }

    public bool dupFound
    {
        get 
        { return m_dupFound; }
        set 
        { m_dupFound = value; }
    }

    int    m_Segment;  // The segment index. (Used only in Windows MAP file.)        
    UInt32 m_Address;
    
    bool m_dupFound;   // Have we found a duplicated entry for this key?
}


public class MapFile
{
    const String Reg_ExWhiteSpaces = @"\s+";

    // 
    // These are regular expression strings for Windows MAP file.
    //
    const String Reg_MapAddress = @"^ (?<addrPart1>[0-9a-f]{4}):(?<addrPart2>[0-9a-f]{8})";

    enum WindowsSymbolTypes {
        ModuleNameClassNameFieldName,
        ClassNameFieldName,
        GlobalVarName,
        GlobalVarName2, 
        GlobalVarName3, 
        SingleVtAddr,
        MultiVtAddr,
    };
     
    readonly String[] RegExps_WindowsMapfile = {

        // ModuleNameClassNameFieldName
        //   Example: ?ephemeral_heap_segment@gc_heap@WKS@@
        Reg_MapAddress +  
        Reg_ExWhiteSpaces + 
        @"\?(?<fieldName>[^\?@]+)@(?<className>[^\?@]+)@(?<moduleName>[^\?@]+)@@",
        
        // ClassNameFieldName
        //   Example: ?m_RangeTree@ExecutionManager@@
        Reg_MapAddress +  
        Reg_ExWhiteSpaces + 
        @"\?(?<fieldName>[^\?@]+)@(?<className>[^\?@]+)@@",
 
        // GlobalVarName
        //   Example: ?g_pNotificationTable@@
        //       (or) ?JIT_LMul@@
        Reg_MapAddress + 
        Reg_ExWhiteSpaces + 
        @"\?(?<globalVarName>[^\?@]+)@@",
        
        // GlobalVarName2
        //   Example: @JIT_WriteBarrier@
        //       (or) _JIT_FltRem@
        //       (or) _JIT_Dbl2Lng@
        //       (or) _JIT_LLsh@
        Reg_MapAddress + 
        Reg_ExWhiteSpaces + 
        @"[@_](?<globalVarName>[^\?@]+)@",

        // GlobalVarName3
        //   Example:  _g_card_table    795e53a4
        Reg_MapAddress + 
        Reg_ExWhiteSpaces + 
        @"_(?<globalVarName>[^\s@]+)" +
        Reg_ExWhiteSpaces + 
        @"[0-9a-f]{8}",
 
        // Single-inheritance VtAddr
        //   Example: ??_7Thread@@6B@
        Reg_MapAddress + 
        Reg_ExWhiteSpaces + 
        @"\?\?_7(?<FunctionName>[^\?@]+)@@6B@",
 
        // Multiple-inheritance VtAddr
        //   Example: ??_7CompilationDomain@@6BAppDomain@@@
        Reg_MapAddress + 
        Reg_ExWhiteSpaces + 
        @"\?\?_7(?<FunctionName>[^\?@]+)@@6B(?<BaseName>[^@]+)@@@"
    };
    const String Reg_Length = @"(?<length>[0-9a-f]{8})H";
    const String Reg_SegmentInfo = Reg_MapAddress + " " + Reg_Length;

    // 
    // These are regular expression strings for Unix NM file.
    //
    const String Reg_NmAddress = @"^(?<address>[0-9a-f]{8})";
    const String Reg_TypeChar = @"[a-zA-Z]";

    enum UnixSymbolTypes {
        CxxFieldName,
        CxxFunctionName,
        CGlobal,
        VtAddr,
    };
        
    // Rather than try and encode a particular C++ mangling format here, we rely on the nm output
    // having been unmanagled through the c++filt tool.
    readonly String[] RegExps_UnixNmfile = {

        // C++ field name
        //  Examples:
        //       d WKS::gc_heap::ephemeral_heap_segment
        //       d WKS::GCHeap::hEventFinalizer
        //       s ExecutionManager::m_CodeRangeList
        //       d ExecutionManager::m_dwReaderCount
        Reg_NmAddress +  
        " " +
        Reg_TypeChar +
        " " +
        @"(?<symName>\w+(::\w+)+)$",

        // C++ function/method name
        //  Note: may have duplicates due to overloading by argument types
        //  Examples:
        //       t JIT_LMulOvf(int, int, int, long long, long long)
        //       t ThreadpoolMgr::AsyncCallbackCompletion(void*)
        //       
        //  Counter-examples we don't want to include:
        //       b JIT_NewFast(int, int, CORINFO_CLASS_STRUCT_*)::__haveCheckedRestoreState
        //       
        Reg_NmAddress +  
        " " +
        Reg_TypeChar +
        " " +
        @"(?<symName>\w+(::\w+)*)\(.*\)$",

        // C global variable / function name
        //  Examples:
        //   s _g_pNotificationTable
        //   t _JIT_LLsh
        Reg_NmAddress +  
        " " +
        Reg_TypeChar +
        " " +
        @"_(?<symName>\w+)$",
        
        // VtAddr
        //   Examples: 
        //    s vtable for Thread
        //   
        // Note that at the moment we don't have any multiple-inheritance vtables that we need
        // on UNIX (CompilationDomain is the only one which is only present on FEATURE_PREJIT).
        // It looks like classes with multiple inheritance only list a single vtable in nm output
        // (eg. RegMeta).
        // We also don't support nested classes here because there is currently no syntax for them
        // for daccess.i.
        // 
        Reg_NmAddress +  
        " " +
        Reg_TypeChar +
        " " +
        @"vtable for (?<symName>\w+)$",
    };

    public UInt32 GetRVA(SymbolInfo si)
    {
        UInt32 addr = si.Address;
        if (bIsWindowsMapfile)
        {
            addr += (UInt32)SegmentBase[si.Segment];
        }
        return addr;
    }
       
    public MapFile(String symdumpFile)
    {
        m_symdumpFile = symdumpFile;
        loadDataFromMapFile();
    }

    void ReadMapHeader(StreamReader strm)
    { 
        Regex regExNmFile = new Regex("^[0-9a-f]{8} ");
        Regex regEx = new Regex(Reg_SegmentInfo);
        Regex regHeaderSection = new Regex(@"\s*Address\s*Publics by Value\s*Rva\+Base\s*Lib:Object", RegexOptions.IgnoreCase);
        Regex regnonNMHeader = new Regex(@"\s*Start\s*Length\s*Name\s*Class", RegexOptions.IgnoreCase);
        Match match = null;

        UInt32 lastSegmentIndex = 0; 
        UInt32 segmentIndex;
        UInt32 sectionStart = 0;
        UInt32 sectionLength = 0;

        String line;
        bool bInSegmentDecl = false;

        const UInt32 baseOfCode = 0x1000;
        SegmentBase.Add((UInt32)baseOfCode);

        for (;;)
        {
            line = strm.ReadLine();

            if (bInSegmentDecl) {

                if (regHeaderSection.IsMatch(line)) {
                    // Header section ends.
                    break;
                }
#if DACTABLEGEN_DEBUG                  
                Console.WriteLine("SegmentDecl: " + line);
#endif

                match = regEx.Match(line);
                if (match.Success) {
                    segmentIndex = UInt32.Parse(match.Groups["addrPart1"].ToString(), NumberStyles.AllowHexSpecifier);
                    if (segmentIndex != lastSegmentIndex) {
                        // Enter the new segment. Record what we have.
                        // Note, SegmentBase[i] is built upon SegmentBase[i-1] 
                        SegmentBase.Add((UInt32)SegmentBase[SegmentBase.Count-1] + (sectionStart + sectionLength + (UInt32)0xFFF) & (~((UInt32)0xFFF)));
                        lastSegmentIndex = segmentIndex;
                    }
                    sectionStart = UInt32.Parse(match.Groups["addrPart2"].ToString(), NumberStyles.AllowHexSpecifier);
                    sectionLength = UInt32.Parse(match.Groups["length"].ToString(), NumberStyles.AllowHexSpecifier);
                }
                
                if (line == null)
                {   
                    throw new InvalidOperationException("Invalid MAP header format.");
                }
            }
            else {
                if (!regnonNMHeader.IsMatch(line)) {
                    match = regExNmFile.Match(line);
                    if (match.Success) {
                        // It's a nm file format. There is no header for it.
                        break;
                    }
                    continue;
                }
                bInSegmentDecl = true;
                bIsWindowsMapfile = true;
                
            }            
        }

        if (bIsWindowsMapfile) {
            // 
            // Only Windows map file has SegmentBase
            //
            for (int i=1 ; i<= lastSegmentIndex; i++) {
#if DACTABLEGEN_DEBUG                  
                Console.WriteLine("SegmentBase[{0}] = {1:x8}", i, SegmentBase[i]);
#endif
            }
        }
        
    }
    
    public void loadDataFromMapFile()
    {

        StreamReader strm =
            new StreamReader(m_symdumpFile, System.Text.Encoding.ASCII);
        String line;
        int i;
        String[] RegExps; 

        // Read the head of the symbol dump file and
        // determind the format of it.
        ReadMapHeader(strm);
        
        //
        // Scan through the symbol dump file looking
        // for the globals structure.
        //
        if (bIsWindowsMapfile) { 
            RegExps = RegExps_WindowsMapfile;
        }
        else {
            RegExps = RegExps_UnixNmfile;
        }
        
        Console.WriteLine("It is a {0} file.", (bIsWindowsMapfile)?"Windows MAP":"Unix NM");
       
        Regex[] RegExs = new Regex[RegExps.Length];
        for (i = 0; i < RegExps.Length; i++) {
#if DACTABLEGEN_DEBUG              
            Console.WriteLine("RegEx[{0}]: {1}", i, RegExps[i]);
#endif
            RegExs[i] = new Regex(RegExps[i]);
        }
        
        Match match = null;
        SymbolInfo si;
        String key;  
        int segment;
        UInt32 address;
        
        for (;;)
        {
            line = strm.ReadLine();

            if (line == null)
            {   
                // No more to read.
                break;
            }

            // Console.WriteLine(">{0}", line);
            for (i = 0; i < RegExps.Length; i++) {

                match = RegExs[i].Match(line);
                if (match.Success) {
                    // Console.WriteLine(line);  
                    
                    segment = 0;
                    address = 0;
                    if (bIsWindowsMapfile) {
                        switch ((WindowsSymbolTypes)i) {

                            case WindowsSymbolTypes.ModuleNameClassNameFieldName:
                                key = match.Groups["moduleName"].ToString() + "::" + match.Groups["className"].ToString() + "::" + match.Groups["fieldName"];
                                segment = Int32.Parse(match.Groups["addrPart1"].ToString(), NumberStyles.AllowHexSpecifier);
                                address = UInt32.Parse(match.Groups["addrPart2"].ToString(), NumberStyles.AllowHexSpecifier);
                                break;
                                
                            case WindowsSymbolTypes.ClassNameFieldName:
                                key = match.Groups["className"].ToString() + "::" + match.Groups["fieldName"];
                                segment = Int32.Parse(match.Groups["addrPart1"].ToString(), NumberStyles.AllowHexSpecifier);
                                address = UInt32.Parse(match.Groups["addrPart2"].ToString(), NumberStyles.AllowHexSpecifier);
                                break;
                               
                            case WindowsSymbolTypes.GlobalVarName:
                            case WindowsSymbolTypes.GlobalVarName2:
                            case WindowsSymbolTypes.GlobalVarName3:
                                key = match.Groups["globalVarName"].ToString();
                                segment = Int32.Parse(match.Groups["addrPart1"].ToString(), NumberStyles.AllowHexSpecifier);
                                address = UInt32.Parse(match.Groups["addrPart2"].ToString(), NumberStyles.AllowHexSpecifier);
                                break;

                            case WindowsSymbolTypes.SingleVtAddr:
                                key = match.Groups["FunctionName"].ToString();
                                segment = Int32.Parse(match.Groups["addrPart1"].ToString(), NumberStyles.AllowHexSpecifier);
                                address = UInt32.Parse(match.Groups["addrPart2"].ToString(), NumberStyles.AllowHexSpecifier);
                                break;

                            case WindowsSymbolTypes.MultiVtAddr:
                                key = match.Groups["FunctionName"].ToString() +
                                    "__" +
                                    match.Groups["BaseName"].ToString();
                                segment = Int32.Parse(match.Groups["addrPart1"].ToString(), NumberStyles.AllowHexSpecifier);
                                address = UInt32.Parse(match.Groups["addrPart2"].ToString(), NumberStyles.AllowHexSpecifier);
                                break;
                                
                            default:
                                throw new ApplicationException("Unknown symbolType" + i);
                        }
                    }
                    else 
                    {
                        // We've got a UNIX nm file
                        // The full unmanaged symbol name is already included in the RegEx
                        // We could consider treating VTable's differently (a vt-specific key or different
                        // hash), but we want to be consistent with the windows map case here.

                        key = match.Groups["symName"].ToString();
                        address = UInt32.Parse(match.Groups["address"].ToString(), NumberStyles.AllowHexSpecifier);

                        if (i == (int)UnixSymbolTypes.VtAddr)
                        {
                            // For VTables, what we really want is the vtAddr used at offset zero of objects. 
                            // GCC has the vtAddr point to the 3rd slot of the VTable (in contrast to MSVC where
                            // the vtAddr points to the base of the VTable).
                            // Slot 0 appears to typically be NULL
                            // Slot 1 points to the run-time type info for the type
                            // Slot 2 is the first virtual method 
                            // Fix up the symbol address here to match the value used in object headers.
                            // Note that we don't know a lot about the target here (eg. what platform it's
                            // running on), so it might be better to do this adjustment in DAC itself.  But
                            // for now doing it here is simpler and more reliable (only one place to update
                            // to make it consistent).
                            address += 2 * 4;   // assumes 32-bit
                        }
                    }

                    si = (SymbolInfo)SymbolHash[key];
                    if (si != null) 
                    {
                        // Some duplicates are expected (eg. functions with overloads), but we should never
                        // actually care about the address of such symbols.  Record that this is a dup
                        // so that we can warn/fail if we try to use it.
                        si.dupFound = true;
                    }
                    else {
                        si = new SymbolInfo(segment, address);
                        // Console.WriteLine("{0:x8} {1}", si.Segment, si.Address, key);
                        SymbolHash.Add(key, si);
                    }
                    
                }
                
            }
            
        }

        strm.Close();
    }

    
    public SymbolInfo FindSymbol(String key)
    {
        SymbolInfo si = (SymbolInfo)SymbolHash[key];
        if (si != null) 
        {
            if (si.dupFound) 
            {
                Console.WriteLine("Warning: Symbol " + key + " has duplicated entry in the symbol dump file.");
            }
        }

        return si;
    }

    bool bIsWindowsMapfile = false;
    String m_symdumpFile;
    Hashtable SymbolHash = new Hashtable();

    ArrayList SegmentBase = new ArrayList();
}
