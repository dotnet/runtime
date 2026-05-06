// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
#if DEBUG
using System.Globalization;
using System.Text;
#endif

namespace System.Data.OleDb
{
    internal enum DBBindStatus
    {
        OK = 0,
        BADORDINAL = 1,
        UNSUPPORTEDCONVERSION = 2,
        BADBINDINFO = 3,
        BADSTORAGEFLAGS = 4,
        NOINTERFACE = 5,
        MULTIPLESTORAGE = 6
    }

#if false
    typedef struct tagDBPARAMBINDINFO {
        LPOLESTR pwszDataSourceType;
        LPOLESTR pwszName;
        DBLENGTH ulParamSize;
        DBPARAMFLAGS dwFlags;
        BYTE bPrecision;
        BYTE bScale;
    }
#endif


    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct tagDBPARAMBINDINFO_x86
    {
        internal IntPtr pwszDataSourceType;
        internal IntPtr pwszName;
        internal IntPtr ulParamSize;
        internal int dwFlags;
        internal byte bPrecision;
        internal byte bScale;

#if DEBUG
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("tagDBPARAMBINDINFO_x86").Append(Environment.NewLine);
            if (IntPtr.Zero != pwszDataSourceType)
            {
                builder.Append("pwszDataSourceType =").Append(Marshal.PtrToStringUni(pwszDataSourceType)).Append(Environment.NewLine);
            }
            builder.AppendLine($"\tulParamSize  ={ulParamSize}");
            builder.AppendLine($"\tdwFlags      =0x{dwFlags:X4}");
            builder.AppendLine($"\tPrecision    ={bPrecision}");
            builder.AppendLine($"\tScale        ={bScale}");
            return builder.ToString();
        }
#endif
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct tagDBPARAMBINDINFO
    {
        internal IntPtr pwszDataSourceType;
        internal IntPtr pwszName;
        internal IntPtr ulParamSize;
        internal int dwFlags;
        internal byte bPrecision;
        internal byte bScale;

#if DEBUG
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("tagDBPARAMBINDINFO").Append(Environment.NewLine);
            if (IntPtr.Zero != pwszDataSourceType)
            {
                builder.AppendLine($"pwszDataSourceType ={Marshal.PtrToStringUni(pwszDataSourceType)}");
            }
            builder.AppendLine($"\tulParamSize  ={ulParamSize}");
            builder.AppendLine($"\tdwFlags     =0x{dwFlags:X4}");
            builder.AppendLine($"\tPrecision   ={bPrecision}");
            builder.AppendLine($"\tScale       ={bScale}");
            return builder.ToString();
        }
#endif
    }

#if false
    typedef struct tagDBBINDING {
        DBORDINAL iOrdinal;
        DBBYTEOFFSET obValue;
        DBBYTEOFFSET obLength;
        DBBYTEOFFSET obStatus;
        ITypeInfo *pTypeInfo;
        DBOBJECT *pObject;
        DBBINDEXT *pBindExt;
        DBPART dwPart;
        DBMEMOWNER dwMemOwner;
        DBPARAMIO eParamIO;
        DBLENGTH cbMaxLen;
        DWORD dwFlags;
        DBTYPE wType;
        BYTE bPrecision;
        BYTE bScale;
    }
#endif

#if (WIN32 && !ARCH_arm)
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
    internal sealed class tagDBBINDING
    {
        internal IntPtr iOrdinal;
        internal IntPtr obValue;
        internal IntPtr obLength;
        internal IntPtr obStatus;

        internal IntPtr pTypeInfo;
        internal IntPtr pObject;
        internal IntPtr pBindExt;

        internal int dwPart;
        internal int dwMemOwner;
        internal int eParamIO;

        internal IntPtr cbMaxLen;

        internal int dwFlags;
        internal short wType;
        internal byte bPrecision;
        internal byte bScale;

        internal tagDBBINDING()
        {
        }

#if DEBUG
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("tagDBBINDING");
            builder.AppendLine($"\tOrdinal     ={iOrdinal}");
            builder.AppendLine($"\tValueOffset ={obValue}");
            builder.AppendLine($"\tLengthOffset={obLength}");
            builder.AppendLine($"\tStatusOffset={obStatus}");
            builder.AppendLine($"\tMaxLength   ={cbMaxLen}");
            builder.AppendLine($"\tDB_Type     ={ODB.WLookup(wType)}");
            builder.AppendLine($"\tPrecision   ={bPrecision}");
            builder.AppendLine($"\tScale       ={bScale}");
            return builder.ToString();
        }
#endif
    }

#if false
    typedef struct tagDBCOLUMNACCESS {
        void *pData;
        DBID columnid;
        DBLENGTH cbDataLen;
        DBSTATUS dwStatus;
        DBLENGTH cbMaxLen;
        DB_DWRESERVE dwReserved;
        DBTYPE wType;
        BYTE bPrecision;
        BYTE bScale;
    }
#endif

#if (WIN32 && !ARCH_arm)
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
    internal struct tagDBCOLUMNACCESS
    {
        internal IntPtr pData;
        internal tagDBIDX columnid;
        internal IntPtr cbDataLen;
        internal int dwStatus;
        internal IntPtr cbMaxLen;
        internal IntPtr dwReserved;
        internal short wType;
        internal byte bPrecision;
        internal byte bScale;
    }

#if false
    typedef struct tagDBID {
    /* [switch_is][switch_type] */ union {
        /* [case()] */ GUID guid;
        /* [case()] */ GUID *pguid;
        /* [default] */  /* Empty union arm */
        }   uGuid;
    DBKIND eKind;
    /* [switch_is][switch_type] */ union  {
        /* [case()] */ LPOLESTR pwszName;
        /* [case()] */ ULONG ulPropid;
        /* [default] */  /* Empty union arm */
        }   uName;
    }
#endif

#if (WIN32 && !ARCH_arm)
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
    internal struct tagDBIDX
    {
        internal Guid uGuid;
        internal int eKind;
        internal IntPtr ulPropid;
    }

#if (WIN32 && !ARCH_arm)
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
    internal sealed class tagDBID
    {
        internal Guid uGuid;
        internal int eKind;
        internal IntPtr ulPropid;
    }

#if false
    typedef struct tagDBLITERALINFO {
        LPOLESTR pwszLiteralValue;
        LPOLESTR pwszInvalidChars;
        LPOLESTR pwszInvalidStartingChars;
        DBLITERAL lt;
        BOOL fSupported;
        ULONG cchMaxLen;
    }
#endif
#if (WIN32 && !ARCH_arm)
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
    internal sealed class tagDBLITERALINFO
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string? pwszLiteralValue = null;

        [MarshalAs(UnmanagedType.LPWStr)]
        internal string? pwszInvalidChars = null;

        [MarshalAs(UnmanagedType.LPWStr)]
        internal string? pwszInvalidStartingChars = null;

        internal int it;

        internal int fSupported;

        internal int cchMaxLen;

        internal tagDBLITERALINFO()
        {
        }
    }

#if false
    typedef struct tagDBPROPSET {
        /* [size_is] */ DBPROP *rgProperties;
        ULONG cProperties;
        GUID guidPropertySet;
    }
#endif
#if (WIN32 && !ARCH_arm)
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
    internal sealed class tagDBPROPSET
    {
        internal IntPtr rgProperties;
        internal int cProperties;
        internal Guid guidPropertySet;

        internal tagDBPROPSET()
        {
        }

        internal tagDBPROPSET(int propertyCount, Guid propertySet)
        {
            cProperties = propertyCount;
            guidPropertySet = propertySet;
        }
    }

#if false
    typedef struct tagDBPROP {
        DBPROPID dwPropertyID;
        DBPROPOPTIONS dwOptions;
        DBPROPSTATUS dwStatus;
        DBID colid;
        VARIANT vValue;
    }
#endif

    internal interface ItagDBPROP
    {
        OleDbPropertyStatus dwStatus { get; }
        object? vValue { get; }
        int dwPropertyID { get; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal sealed class tagDBPROP_x86 : ItagDBPROP
    {
        OleDbPropertyStatus ItagDBPROP.dwStatus => this.dwStatus;

        object? ItagDBPROP.vValue => this.vValue;

        int ItagDBPROP.dwPropertyID => this.dwPropertyID;

        internal int dwPropertyID;
        internal int dwOptions;
        internal OleDbPropertyStatus dwStatus;

        internal tagDBIDX columnid;

        // Variant
        [MarshalAs(UnmanagedType.Struct)] internal object? vValue;

        internal tagDBPROP_x86()
        {
        }

        internal tagDBPROP_x86(int propertyID, bool required, object value)
        {
            dwPropertyID = propertyID;
            dwOptions = ((required) ? ODB.DBPROPOPTIONS_REQUIRED : ODB.DBPROPOPTIONS_OPTIONAL);
            vValue = value;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal sealed class tagDBPROP : ItagDBPROP
    {
        OleDbPropertyStatus ItagDBPROP.dwStatus => this.dwStatus;

        object? ItagDBPROP.vValue => this.vValue;

        int ItagDBPROP.dwPropertyID => this.dwPropertyID;
        internal int dwPropertyID;
        internal int dwOptions;
        internal OleDbPropertyStatus dwStatus;

        internal tagDBIDX columnid;

        // Variant
        [MarshalAs(UnmanagedType.Struct)] internal object? vValue;

        internal tagDBPROP()
        {
        }

        internal tagDBPROP(int propertyID, bool required, object value)
        {
            dwPropertyID = propertyID;
            dwOptions = ((required) ? ODB.DBPROPOPTIONS_REQUIRED : ODB.DBPROPOPTIONS_OPTIONAL);
            vValue = value;
        }
    }

#if false
    typedef struct tagDBPARAMS {
        void *pData;
        DB_UPARAMS cParamSets;
        HACCESSOR hAccessor;
    }
#endif
#if (WIN32 && !ARCH_arm)
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
    internal sealed class tagDBPARAMS
    {
        internal IntPtr pData;
        internal int cParamSets;
        internal IntPtr hAccessor;

        internal tagDBPARAMS()
        {
        }
    }

#if false
    typedef struct tagDBCOLUMNINFO {
        LPOLESTR pwszName;
        ITypeInfo *pTypeInfo;
        DBORDINAL iOrdinal;
        DBCOLUMNFLAGS dwFlags;
        DBLENGTH ulColumnSize;
        DBTYPE wType;
        BYTE bPrecision;
        BYTE bScale;
        DBID columnid;
    }
#endif
#if (WIN32 && !ARCH_arm)
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
    internal sealed class tagDBCOLUMNINFO
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string? pwszName = null;

        //[MarshalAs(UnmanagedType.Interface)]
        internal IntPtr pTypeInfo = (IntPtr)0;

        internal nint iOrdinal = 0;

        internal int dwFlags;

        internal nint ulColumnSize = 0;

        internal short wType;

        internal byte bPrecision;

        internal byte bScale;

        internal tagDBIDX columnid;

        internal tagDBCOLUMNINFO()
        {
        }
#if DEBUG
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"tagDBCOLUMNINFO: {Convert.ToString(pwszName, CultureInfo.InvariantCulture)}");
            builder.AppendLine($"\t{((long)iOrdinal).ToString(CultureInfo.InvariantCulture)}");
            builder.AppendLine($"\t0x{dwFlags:X8}");
            builder.AppendLine($"\t{ulColumnSize}");
            builder.AppendLine($"\t0x{wType:X2}");
            builder.AppendLine($"\t{bPrecision}");
            builder.AppendLine($"\t{bScale}");
            builder.AppendLine($"\t{columnid.eKind}");
            return builder.ToString();
        }
#endif
    }

#if false
    typedef struct tagDBPROPINFOSET {
        /* [size_is] */ PDBPROPINFO rgPropertyInfos;
        ULONG cPropertyInfos;
        GUID guidPropertySet;
    }
#endif
#if (WIN32 && !ARCH_arm)
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
    internal sealed class tagDBPROPINFOSET
    {
        internal IntPtr rgPropertyInfos;
        internal int cPropertyInfos;
        internal Guid guidPropertySet;

        internal tagDBPROPINFOSET()
        {
        }
    }

#if false
    typedef struct tagDBPROPINFO {
        LPOLESTR pwszDescription;
        DBPROPID dwPropertyID;
        DBPROPFLAGS dwFlags;
        VARTYPE vtType;
        VARIANT vValues;
    }
#endif

    internal interface ItagDBPROPINFO
    {
        int dwPropertyID { get; }
        int dwFlags { get; }
        int vtType { get; }
        object? vValue { get; }
        string? pwszDescription { get; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal sealed class tagDBPROPINFO_x86 : ItagDBPROPINFO
    {
        int ItagDBPROPINFO.dwPropertyID => this.dwPropertyID;

        int ItagDBPROPINFO.dwFlags => this.dwFlags;

        int ItagDBPROPINFO.vtType => this.vtType;

        object? ItagDBPROPINFO.vValue => this.vValue;

        string? ItagDBPROPINFO.pwszDescription => this.pwszDescription;

        [MarshalAs(UnmanagedType.LPWStr)] internal string? pwszDescription;

        internal int dwPropertyID;
        internal int dwFlags;

        internal short vtType;

        [MarshalAs(UnmanagedType.Struct)] internal object? vValue;

        internal tagDBPROPINFO_x86()
        {
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal sealed class tagDBPROPINFO : ItagDBPROPINFO
    {
        int ItagDBPROPINFO.dwPropertyID => this.dwPropertyID;

        int ItagDBPROPINFO.dwFlags => this.dwFlags;

        int ItagDBPROPINFO.vtType => this.vtType;

        object? ItagDBPROPINFO.vValue => this.vValue;

        string? ItagDBPROPINFO.pwszDescription => this.pwszDescription;
        [MarshalAs(UnmanagedType.LPWStr)] internal string? pwszDescription;

        internal int dwPropertyID;
        internal int dwFlags;

        internal short vtType;

        [MarshalAs(UnmanagedType.Struct)] internal object? vValue;

        internal tagDBPROPINFO()
        {
        }
    }

#if false
    typedef struct tagDBPROPIDSET {
        /* [size_is] */ DBPROPID *rgPropertyIDs;
        ULONG cPropertyIDs;
        GUID guidPropertySet;
    }
#endif
#if (WIN32 && !ARCH_arm)
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
    internal struct tagDBPROPIDSET
    {
        internal IntPtr rgPropertyIDs;
        internal int cPropertyIDs;
        internal Guid guidPropertySet;
    }

    internal static class OleDbStructHelpers
    {
        internal static ItagDBPROPINFO CreateTagDbPropInfo() =>
            ODB.IsRunningOnX86 ? (ItagDBPROPINFO)new tagDBPROPINFO_x86() : new tagDBPROPINFO();

        internal static ItagDBPROP CreateTagDbProp(int propertyID, bool required, object value) =>
            ODB.IsRunningOnX86 ? (ItagDBPROP)new tagDBPROP_x86(propertyID, required, value) :
                    new tagDBPROP(propertyID, required, value);

        internal static ItagDBPROP CreateTagDbProp() =>
            ODB.IsRunningOnX86 ? (ItagDBPROP)new tagDBPROP_x86() : new tagDBPROP();
    }
}
