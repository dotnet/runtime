// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Data.OleDb
{
#if DEBUG
    using Globalization;
    using Text;
#endif

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

#if (WIN32 && !ARCH_arm)
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
    internal struct tagDBPARAMBINDINFO
    {
        internal IntPtr pwszDataSourceType;
        internal IntPtr pwszName;
        internal IntPtr ulParamSize;
        internal Int32 dwFlags;
        internal Byte bPrecision;
        internal Byte bScale;

#if DEBUG
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("tagDBPARAMBINDINFO").Append(Environment.NewLine);
            if (IntPtr.Zero != pwszDataSourceType)
            {
                builder.Append("pwszDataSourceType =").Append(Marshal.PtrToStringUni(pwszDataSourceType)).Append(Environment.NewLine);
            }
            builder.Append("\tulParamSize  =" + ulParamSize.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\tdwFlags     =0x" + dwFlags.ToString("X4", CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\tPrecision   =" + bPrecision.ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\tScale       =" + bScale.ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
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

        internal Int32 dwPart;
        internal Int32 dwMemOwner;
        internal Int32 eParamIO;

        internal IntPtr cbMaxLen;

        internal Int32 dwFlags;
        internal Int16 wType;
        internal byte bPrecision;
        internal byte bScale;

        internal tagDBBINDING()
        {
        }

#if DEBUG
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("tagDBBINDING").Append(Environment.NewLine);
            builder.Append("\tOrdinal     =" + iOrdinal.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\tValueOffset =" + obValue.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\tLengthOffset=" + obLength.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\tStatusOffset=" + obStatus.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\tMaxLength   =" + cbMaxLen.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\tDB_Type     =" + ODB.WLookup(wType)).Append(Environment.NewLine);
            builder.Append("\tPrecision   =" + bPrecision.ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\tScale       =" + bScale.ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
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
        internal Int32 dwStatus;
        internal IntPtr cbMaxLen;
        internal IntPtr dwReserved;
        internal Int16 wType;
        internal Byte bPrecision;
        internal Byte bScale;
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
        internal Int32 eKind;
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
        internal Int32 eKind;
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
    sealed internal class tagDBLITERALINFO
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        internal String pwszLiteralValue = null;

        [MarshalAs(UnmanagedType.LPWStr)]
        internal String pwszInvalidChars = null;

        [MarshalAs(UnmanagedType.LPWStr)]
        internal String pwszInvalidStartingChars = null;

        internal Int32 it;

        internal Int32 fSupported;

        internal Int32 cchMaxLen;

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
    sealed internal class tagDBPROPSET
    {
        internal IntPtr rgProperties;
        internal Int32 cProperties;
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
#if (WIN32 && !ARCH_arm)
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
    sealed internal class tagDBPROP
    {
        internal Int32 dwPropertyID;
        internal Int32 dwOptions;
        internal OleDbPropertyStatus dwStatus;

        internal tagDBIDX columnid;

        // Variant
        [MarshalAs(UnmanagedType.Struct)] internal object vValue;

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
    sealed internal class tagDBPARAMS
    {
        internal IntPtr pData;
        internal Int32 cParamSets;
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
    sealed internal class tagDBCOLUMNINFO
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        internal String pwszName = null;

        //[MarshalAs(UnmanagedType.Interface)]
        internal IntPtr pTypeInfo = (IntPtr)0;

        internal IntPtr iOrdinal = (IntPtr)0;

        internal Int32 dwFlags = 0;

        internal IntPtr ulColumnSize = (IntPtr)0;

        internal Int16 wType = 0;

        internal Byte bPrecision = 0;

        internal Byte bScale = 0;

        internal tagDBIDX columnid;

        internal tagDBCOLUMNINFO()
        {
        }
#if DEBUG
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("tagDBCOLUMNINFO: " + Convert.ToString(pwszName, CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\t" + iOrdinal.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\t" + "0x" + dwFlags.ToString("X8", CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\t" + ulColumnSize.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\t" + "0x" + wType.ToString("X2", CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\t" + bPrecision.ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\t" + bScale.ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
            builder.Append("\t" + columnid.eKind.ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
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
    sealed internal class tagDBPROPINFOSET
    {
        internal IntPtr rgPropertyInfos;
        internal Int32 cPropertyInfos;
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
#if (WIN32 && !ARCH_arm)
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
    sealed internal class tagDBPROPINFO
    {
        [MarshalAs(UnmanagedType.LPWStr)] internal string pwszDescription;

        internal Int32 dwPropertyID;
        internal Int32 dwFlags;

        internal Int16 vtType;

        [MarshalAs(UnmanagedType.Struct)] internal object vValue;

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
        internal Int32 cPropertyIDs;
        internal Guid guidPropertySet;
    }
}

