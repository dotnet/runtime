// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: UCOMITypeInfo interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices
{
    using System;

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.TYPEKIND instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Serializable]
    public enum TYPEKIND 
    {
        TKIND_ENUM      = 0,
        TKIND_RECORD    = TKIND_ENUM + 1,
        TKIND_MODULE    = TKIND_RECORD + 1,
        TKIND_INTERFACE = TKIND_MODULE + 1,
        TKIND_DISPATCH  = TKIND_INTERFACE + 1,
        TKIND_COCLASS   = TKIND_DISPATCH + 1,
        TKIND_ALIAS     = TKIND_COCLASS + 1,
        TKIND_UNION     = TKIND_ALIAS + 1,
        TKIND_MAX       = TKIND_UNION + 1
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.TYPEFLAGS instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
[Serializable]
[Flags()]
    public enum TYPEFLAGS : short
    {
        TYPEFLAG_FAPPOBJECT         = 0x1,
        TYPEFLAG_FCANCREATE         = 0x2,
        TYPEFLAG_FLICENSED          = 0x4,
        TYPEFLAG_FPREDECLID         = 0x8,
        TYPEFLAG_FHIDDEN            = 0x10,
        TYPEFLAG_FCONTROL           = 0x20,
        TYPEFLAG_FDUAL              = 0x40,
        TYPEFLAG_FNONEXTENSIBLE     = 0x80,
        TYPEFLAG_FOLEAUTOMATION     = 0x100,
        TYPEFLAG_FRESTRICTED        = 0x200,
        TYPEFLAG_FAGGREGATABLE      = 0x400,
        TYPEFLAG_FREPLACEABLE       = 0x800,
        TYPEFLAG_FDISPATCHABLE      = 0x1000,
        TYPEFLAG_FREVERSEBIND       = 0x2000,
        TYPEFLAG_FPROXY             = 0x4000
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
[Serializable]
[Flags()]
    public enum IMPLTYPEFLAGS
    {
        IMPLTYPEFLAG_FDEFAULT       = 0x1,
        IMPLTYPEFLAG_FSOURCE        = 0x2,
        IMPLTYPEFLAG_FRESTRICTED    = 0x4,
        IMPLTYPEFLAG_FDEFAULTVTABLE = 0x8,
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.TYPEATTR instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct TYPEATTR
    { 
        // Constant used with the memid fields.
        public const int MEMBER_ID_NIL = unchecked((int)0xFFFFFFFF); 

        // Actual fields of the TypeAttr struct.
        public Guid guid;
        public Int32 lcid;
        public Int32 dwReserved;
        public Int32 memidConstructor;
        public Int32 memidDestructor;
        public IntPtr lpstrSchema;
        public Int32 cbSizeInstance;
        public TYPEKIND typekind;
        public Int16 cFuncs;
        public Int16 cVars;
        public Int16 cImplTypes;
        public Int16 cbSizeVft;
        public Int16 cbAlignment;
        public TYPEFLAGS wTypeFlags;
        public Int16 wMajorVerNum;
        public Int16 wMinorVerNum;
        public TYPEDESC tdescAlias;
        public IDLDESC idldescType;
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.FUNCDESC instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [StructLayout(LayoutKind.Sequential)]
    public struct FUNCDESC
    { 
        public int memid;                   //MEMBERID memid;
        public IntPtr lprgscode;            // /* [size_is(cScodes)] */ SCODE RPC_FAR *lprgscode;
        public IntPtr lprgelemdescParam;    // /* [size_is(cParams)] */ ELEMDESC __RPC_FAR *lprgelemdescParam;
        public FUNCKIND    funckind;           //FUNCKIND funckind;
        public INVOKEKIND invkind;          //INVOKEKIND invkind;
        public CALLCONV    callconv;           //CALLCONV callconv;
        public Int16 cParams;               //short cParams;
        public Int16 cParamsOpt;            //short cParamsOpt;
        public Int16 oVft;                  //short oVft;
        public Int16 cScodes;               //short cScodes;
        public ELEMDESC    elemdescFunc;       //ELEMDESC elemdescFunc;
        public Int16 wFuncFlags;            //WORD wFuncFlags;
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.IDLFLAG instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
[Serializable]
[Flags()]
    public enum IDLFLAG : short 
    {
        IDLFLAG_NONE    = PARAMFLAG.PARAMFLAG_NONE,
        IDLFLAG_FIN     = PARAMFLAG.PARAMFLAG_FIN,
        IDLFLAG_FOUT    = PARAMFLAG.PARAMFLAG_FOUT,
        IDLFLAG_FLCID   = PARAMFLAG.PARAMFLAG_FLCID,
        IDLFLAG_FRETVAL = PARAMFLAG.PARAMFLAG_FRETVAL
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.IDLDESC instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct IDLDESC
    {
        public int dwReserved;
        public IDLFLAG  wIDLFlags;
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.PARAMFLAG instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
[Serializable]
[Flags()]
    public enum PARAMFLAG :short 
    {
        PARAMFLAG_NONE    = 0,
        PARAMFLAG_FIN    = 0x1,
        PARAMFLAG_FOUT    = 0x2,
        PARAMFLAG_FLCID    = 0x4,
        PARAMFLAG_FRETVAL = 0x8,
        PARAMFLAG_FOPT    = 0x10,
        PARAMFLAG_FHASDEFAULT = 0x20,
        PARAMFLAG_FHASCUSTDATA = 0x40
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.PARAMDESC instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct PARAMDESC
    {
        public IntPtr lpVarValue;
        public PARAMFLAG wParamFlags;
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.TYPEDESC instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct TYPEDESC
    { 
        public IntPtr lpValue;
        public Int16 vt;
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.ELEMDESC instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct ELEMDESC
    {
        public TYPEDESC tdesc;

        [System.Runtime.InteropServices.StructLayout(LayoutKind.Explicit, CharSet=CharSet.Unicode)]
        [ComVisible(false)]
        public struct DESCUNION
        {
            [FieldOffset(0)]
            public IDLDESC idldesc;
            [FieldOffset(0)]
            public PARAMDESC paramdesc;
        };
        public DESCUNION desc;
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.VARDESC instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct VARDESC
    {
        public int memid;                   
        public String lpstrSchema;

        [System.Runtime.InteropServices.StructLayout(LayoutKind.Explicit, CharSet=CharSet.Unicode)]
        [ComVisible(false)]
        public struct DESCUNION
        {
            [FieldOffset(0)]
            public int oInst;
            [FieldOffset(0)]
            public IntPtr lpvarValue;
        };

        public ELEMDESC elemdescVar;
        public short wVarFlags;
        public VarEnum varkind;
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.DISPPARAMS instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct DISPPARAMS
    {
        public IntPtr rgvarg;
        public IntPtr rgdispidNamedArgs;
        public int cArgs;
        public int cNamedArgs;
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.EXCEPINFO instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct EXCEPINFO
    {
        public Int16 wCode;
        public Int16 wReserved;
        [MarshalAs(UnmanagedType.BStr)] public String bstrSource;
        [MarshalAs(UnmanagedType.BStr)] public String bstrDescription;
        [MarshalAs(UnmanagedType.BStr)] public String bstrHelpFile;
        public int dwHelpContext;
        public IntPtr pvReserved;
        public IntPtr pfnDeferredFillIn;
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.FUNCKIND instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Serializable]
    public enum FUNCKIND : int
    {
        FUNC_VIRTUAL = 0,
        FUNC_PUREVIRTUAL = 1,
        FUNC_NONVIRTUAL = 2,
        FUNC_STATIC = 3,
        FUNC_DISPATCH = 4
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.INVOKEKIND instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Serializable]
    public enum INVOKEKIND : int
    {
        INVOKE_FUNC = 0x1,
        INVOKE_PROPERTYGET = 0x2,
        INVOKE_PROPERTYPUT = 0x4,
        INVOKE_PROPERTYPUTREF = 0x8
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.CALLCONV instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Serializable]
    public enum CALLCONV : int
    {
        CC_CDECL    =1,
        CC_MSCPASCAL=2,
        CC_PASCAL   =CC_MSCPASCAL,
        CC_MACPASCAL=3,
        CC_STDCALL  =4,
        CC_RESERVED =5,
        CC_SYSCALL  =6,
        CC_MPWCDECL =7,
        CC_MPWPASCAL=8,
        CC_MAX      =9 
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.FUNCFLAGS instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
[Serializable]
[Flags()]
    public enum FUNCFLAGS : short
    {
        FUNCFLAG_FRESTRICTED=       0x1,
        FUNCFLAG_FSOURCE    =       0x2,
        FUNCFLAG_FBINDABLE    =       0x4,
        FUNCFLAG_FREQUESTEDIT =     0x8,
        FUNCFLAG_FDISPLAYBIND =     0x10,
        FUNCFLAG_FDEFAULTBIND =     0x20,
        FUNCFLAG_FHIDDEN =          0x40,
        FUNCFLAG_FUSESGETLASTERROR= 0x80,
        FUNCFLAG_FDEFAULTCOLLELEM=  0x100,
        FUNCFLAG_FUIDEFAULT =       0x200,
        FUNCFLAG_FNONBROWSABLE =    0x400,
        FUNCFLAG_FREPLACEABLE =     0x800,
        FUNCFLAG_FIMMEDIATEBIND =   0x1000
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.VARFLAGS instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
[Serializable]
[Flags()]
    public enum VARFLAGS : short
    {
        VARFLAG_FREADONLY        =0x1,
        VARFLAG_FSOURCE                     =0x2,
        VARFLAG_FBINDABLE        =0x4,
        VARFLAG_FREQUESTEDIT    =0x8,
        VARFLAG_FDISPLAYBIND    =0x10,
        VARFLAG_FDEFAULTBIND    =0x20,
        VARFLAG_FHIDDEN        =0x40,
        VARFLAG_FRESTRICTED    =0x80,
        VARFLAG_FDEFAULTCOLLELEM    =0x100,
        VARFLAG_FUIDEFAULT                    =0x200,
        VARFLAG_FNONBROWSABLE           =0x400,
        VARFLAG_FREPLACEABLE    =0x800,
        VARFLAG_FIMMEDIATEBIND    =0x1000
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.ITypeInfo instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Guid("00020401-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface UCOMITypeInfo
    {
        void GetTypeAttr(out IntPtr ppTypeAttr);
        void GetTypeComp(out UCOMITypeComp ppTComp);
        void GetFuncDesc(int index, out IntPtr ppFuncDesc);
        void GetVarDesc(int index, out IntPtr ppVarDesc);
        void GetNames(int memid, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] String[] rgBstrNames, int cMaxNames, out int pcNames);
        void GetRefTypeOfImplType(int index, out int href);
        void GetImplTypeFlags(int index, out int pImplTypeFlags);
        void GetIDsOfNames([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1), In] String[] rgszNames, int cNames, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] int[] pMemId);
        void Invoke([MarshalAs(UnmanagedType.IUnknown)] Object pvInstance, int memid, Int16 wFlags, ref DISPPARAMS pDispParams, out Object pVarResult, out EXCEPINFO pExcepInfo, out int puArgErr);
        void GetDocumentation(int index, out String strName, out String strDocString, out int dwHelpContext, out String strHelpFile);
        void GetDllEntry(int memid, INVOKEKIND invKind, out String pBstrDllName, out String pBstrName, out Int16 pwOrdinal);
        void GetRefTypeInfo(int hRef, out UCOMITypeInfo ppTI);
        void AddressOfMember(int memid, INVOKEKIND invKind, out IntPtr ppv);
        void CreateInstance([MarshalAs(UnmanagedType.IUnknown)] Object pUnkOuter, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown), Out] out Object ppvObj);
        void GetMops(int memid, out String pBstrMops);
        void GetContainingTypeLib(out UCOMITypeLib ppTLB, out int pIndex);
        void ReleaseTypeAttr(IntPtr pTypeAttr);
        void ReleaseFuncDesc(IntPtr pFuncDesc);
        void ReleaseVarDesc(IntPtr pVarDesc);
    }
}
