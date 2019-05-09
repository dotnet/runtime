// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.ComTypes
{
    public enum TYPEKIND
    {
        TKIND_ENUM = 0,
        TKIND_RECORD = TKIND_ENUM + 1,
        TKIND_MODULE = TKIND_RECORD + 1,
        TKIND_INTERFACE = TKIND_MODULE + 1,
        TKIND_DISPATCH = TKIND_INTERFACE + 1,
        TKIND_COCLASS = TKIND_DISPATCH + 1,
        TKIND_ALIAS = TKIND_COCLASS + 1,
        TKIND_UNION = TKIND_ALIAS + 1,
        TKIND_MAX = TKIND_UNION + 1
    }

    [Flags]
    public enum TYPEFLAGS : short
    {
        TYPEFLAG_FAPPOBJECT = 0x1,
        TYPEFLAG_FCANCREATE = 0x2,
        TYPEFLAG_FLICENSED = 0x4,
        TYPEFLAG_FPREDECLID = 0x8,
        TYPEFLAG_FHIDDEN = 0x10,
        TYPEFLAG_FCONTROL = 0x20,
        TYPEFLAG_FDUAL = 0x40,
        TYPEFLAG_FNONEXTENSIBLE = 0x80,
        TYPEFLAG_FOLEAUTOMATION = 0x100,
        TYPEFLAG_FRESTRICTED = 0x200,
        TYPEFLAG_FAGGREGATABLE = 0x400,
        TYPEFLAG_FREPLACEABLE = 0x800,
        TYPEFLAG_FDISPATCHABLE = 0x1000,
        TYPEFLAG_FREVERSEBIND = 0x2000,
        TYPEFLAG_FPROXY = 0x4000
    }

    [Flags]
    public enum IMPLTYPEFLAGS
    {
        IMPLTYPEFLAG_FDEFAULT = 0x1,
        IMPLTYPEFLAG_FSOURCE = 0x2,
        IMPLTYPEFLAG_FRESTRICTED = 0x4,
        IMPLTYPEFLAG_FDEFAULTVTABLE = 0x8,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct TYPEATTR
    {
        // Constant used with the memid fields.
        public const int MEMBER_ID_NIL = unchecked((int)0xFFFFFFFF);

        // Actual fields of the TypeAttr struct.
        public Guid guid;
        public int lcid;
        public int dwReserved;
        public int memidConstructor;
        public int memidDestructor;
        public IntPtr lpstrSchema;
        public int cbSizeInstance;
        public TYPEKIND typekind;
        public short cFuncs;
        public short cVars;
        public short cImplTypes;
        public short cbSizeVft;
        public short cbAlignment;
        public TYPEFLAGS wTypeFlags;
        public short wMajorVerNum;
        public short wMinorVerNum;
        public TYPEDESC tdescAlias;
        public IDLDESC idldescType;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FUNCDESC
    {
        public int memid;                   //MEMBERID memid;
        public IntPtr lprgscode;            // /* [size_is(cScodes)] */ SCODE RPC_FAR *lprgscode;
        public IntPtr lprgelemdescParam;    // /* [size_is(cParams)] */ ELEMDESC __RPC_FAR *lprgelemdescParam;
        public FUNCKIND funckind;           //FUNCKIND funckind;
        public INVOKEKIND invkind;          //INVOKEKIND invkind;
        public CALLCONV callconv;           //CALLCONV callconv;
        public short cParams;               //short cParams;
        public short cParamsOpt;            //short cParamsOpt;
        public short oVft;                  //short oVft;
        public short cScodes;               //short cScodes;
        public ELEMDESC elemdescFunc;       //ELEMDESC elemdescFunc;
        public short wFuncFlags;            //WORD wFuncFlags;
    }

    [Flags]
    public enum IDLFLAG : short
    {
        IDLFLAG_NONE = PARAMFLAG.PARAMFLAG_NONE,
        IDLFLAG_FIN = PARAMFLAG.PARAMFLAG_FIN,
        IDLFLAG_FOUT = PARAMFLAG.PARAMFLAG_FOUT,
        IDLFLAG_FLCID = PARAMFLAG.PARAMFLAG_FLCID,
        IDLFLAG_FRETVAL = PARAMFLAG.PARAMFLAG_FRETVAL
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct IDLDESC
    {
        public IntPtr dwReserved;
        public IDLFLAG wIDLFlags;
    }

    [Flags]
    public enum PARAMFLAG : short
    {
        PARAMFLAG_NONE = 0,
        PARAMFLAG_FIN = 0x1,
        PARAMFLAG_FOUT = 0x2,
        PARAMFLAG_FLCID = 0x4,
        PARAMFLAG_FRETVAL = 0x8,
        PARAMFLAG_FOPT = 0x10,
        PARAMFLAG_FHASDEFAULT = 0x20,
        PARAMFLAG_FHASCUSTDATA = 0x40
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct PARAMDESC
    {
        public IntPtr lpVarValue;
        public PARAMFLAG wParamFlags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct TYPEDESC
    {
        public IntPtr lpValue;
        public short vt;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ELEMDESC
    {
        public TYPEDESC tdesc;

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        public struct DESCUNION
        {
            [FieldOffset(0)]
            public IDLDESC idldesc;
            [FieldOffset(0)]
            public PARAMDESC paramdesc;
        };
        public DESCUNION desc;
    }

    public enum VARKIND : int
    {
        VAR_PERINSTANCE = 0x0,
        VAR_STATIC = 0x1,
        VAR_CONST = 0x2,
        VAR_DISPATCH = 0x3
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]

    public struct VARDESC
    {
        public int memid;
        public string lpstrSchema;

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        public struct DESCUNION
        {
            [FieldOffset(0)]
            public int oInst;
            [FieldOffset(0)]
            public IntPtr lpvarValue;
        };

        public DESCUNION desc;

        public ELEMDESC elemdescVar;
        public short wVarFlags;
        public VARKIND varkind;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPPARAMS
    {
        public IntPtr rgvarg;
        public IntPtr rgdispidNamedArgs;
        public int cArgs;
        public int cNamedArgs;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct EXCEPINFO
    {
        public short wCode;
        public short wReserved;
        [MarshalAs(UnmanagedType.BStr)] public string bstrSource;
        [MarshalAs(UnmanagedType.BStr)] public string bstrDescription;
        [MarshalAs(UnmanagedType.BStr)] public string bstrHelpFile;
        public int dwHelpContext;
        public IntPtr pvReserved;
        public IntPtr pfnDeferredFillIn;
        public int scode;
    }

    public enum FUNCKIND : int
    {
        FUNC_VIRTUAL = 0,
        FUNC_PUREVIRTUAL = 1,
        FUNC_NONVIRTUAL = 2,
        FUNC_STATIC = 3,
        FUNC_DISPATCH = 4
    }

    [Flags]
    public enum INVOKEKIND : int
    {
        INVOKE_FUNC = 0x1,
        INVOKE_PROPERTYGET = 0x2,
        INVOKE_PROPERTYPUT = 0x4,
        INVOKE_PROPERTYPUTREF = 0x8
    }

    public enum CALLCONV : int
    {
        CC_CDECL = 1,
        CC_MSCPASCAL = 2,
        CC_PASCAL = CC_MSCPASCAL,
        CC_MACPASCAL = 3,
        CC_STDCALL = 4,
        CC_RESERVED = 5,
        CC_SYSCALL = 6,
        CC_MPWCDECL = 7,
        CC_MPWPASCAL = 8,
        CC_MAX = 9
    }

    [Flags]
    public enum FUNCFLAGS : short
    {
        FUNCFLAG_FRESTRICTED = 0x1,
        FUNCFLAG_FSOURCE = 0x2,
        FUNCFLAG_FBINDABLE = 0x4,
        FUNCFLAG_FREQUESTEDIT = 0x8,
        FUNCFLAG_FDISPLAYBIND = 0x10,
        FUNCFLAG_FDEFAULTBIND = 0x20,
        FUNCFLAG_FHIDDEN = 0x40,
        FUNCFLAG_FUSESGETLASTERROR = 0x80,
        FUNCFLAG_FDEFAULTCOLLELEM = 0x100,
        FUNCFLAG_FUIDEFAULT = 0x200,
        FUNCFLAG_FNONBROWSABLE = 0x400,
        FUNCFLAG_FREPLACEABLE = 0x800,
        FUNCFLAG_FIMMEDIATEBIND = 0x1000
    }

    [Flags]
    public enum VARFLAGS : short
    {
        VARFLAG_FREADONLY = 0x1,
        VARFLAG_FSOURCE = 0x2,
        VARFLAG_FBINDABLE = 0x4,
        VARFLAG_FREQUESTEDIT = 0x8,
        VARFLAG_FDISPLAYBIND = 0x10,
        VARFLAG_FDEFAULTBIND = 0x20,
        VARFLAG_FHIDDEN = 0x40,
        VARFLAG_FRESTRICTED = 0x80,
        VARFLAG_FDEFAULTCOLLELEM = 0x100,
        VARFLAG_FUIDEFAULT = 0x200,
        VARFLAG_FNONBROWSABLE = 0x400,
        VARFLAG_FREPLACEABLE = 0x800,
        VARFLAG_FIMMEDIATEBIND = 0x1000
    }

    [Guid("00020401-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface ITypeInfo
    {
        void GetTypeAttr(out IntPtr ppTypeAttr);
        void GetTypeComp(out ITypeComp ppTComp);
        void GetFuncDesc(int index, out IntPtr ppFuncDesc);
        void GetVarDesc(int index, out IntPtr ppVarDesc);
        void GetNames(int memid, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] string[] rgBstrNames, int cMaxNames, out int pcNames);
        void GetRefTypeOfImplType(int index, out int href);
        void GetImplTypeFlags(int index, out IMPLTYPEFLAGS pImplTypeFlags);
        void GetIDsOfNames([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1), In] string[] rgszNames, int cNames, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] int[] pMemId);
        void Invoke([MarshalAs(UnmanagedType.IUnknown)] object pvInstance, int memid, short wFlags, ref DISPPARAMS pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, out int puArgErr);
        void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);
        void GetDllEntry(int memid, INVOKEKIND invKind, IntPtr pBstrDllName, IntPtr pBstrName, IntPtr pwOrdinal);
        void GetRefTypeInfo(int hRef, out ITypeInfo ppTI);
        void AddressOfMember(int memid, INVOKEKIND invKind, out IntPtr ppv);
        void CreateInstance([MarshalAs(UnmanagedType.IUnknown)] object? pUnkOuter, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown), Out] out object ppvObj);
        void GetMops(int memid, out string? pBstrMops);
        void GetContainingTypeLib(out ITypeLib ppTLB, out int pIndex);
        [PreserveSig]
        void ReleaseTypeAttr(IntPtr pTypeAttr);
        [PreserveSig]
        void ReleaseFuncDesc(IntPtr pFuncDesc);
        [PreserveSig]
        void ReleaseVarDesc(IntPtr pVarDesc);
    }
}
