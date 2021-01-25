// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Globalization;

namespace System.Data.OleDb
{
    public sealed class OleDbEnumerator
    {
        public OleDbEnumerator()
        {
        }

        public DataTable GetElements()
        {
            DataTable dataTable = new DataTable("MSDAENUM");
            dataTable.Locale = CultureInfo.InvariantCulture;
            OleDbDataReader dataReader = GetRootEnumerator();
            OleDbDataAdapter.FillDataTable(dataReader, dataTable);
            return dataTable;
        }

        public static OleDbDataReader GetEnumerator(Type type)
        {
            return GetEnumeratorFromType(type);
        }

        internal static OleDbDataReader GetEnumeratorFromType(Type type)
        {
            object? value = Activator.CreateInstance(type, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, null, CultureInfo.InvariantCulture, null);
            return GetEnumeratorReader(value);
        }

        private static OleDbDataReader GetEnumeratorReader(object? value)
        {
            NativeMethods.ISourcesRowset? srcrowset = null;

            try
            {
                srcrowset = (NativeMethods.ISourcesRowset?)value;
            }
            catch (InvalidCastException)
            {
                throw ODB.ISourcesRowsetNotSupported();
            }
            if (null == srcrowset)
            {
                throw ODB.ISourcesRowsetNotSupported();
            }
            value = null; // still held by ISourcesRowset, reused for IRowset

            int propCount = 0;
            IntPtr propSets = ADP.PtrZero;
            OleDbHResult hr = srcrowset.GetSourcesRowset(ADP.PtrZero, ODB.IID_IRowset, propCount, propSets, out value);

            Exception? f = OleDbConnection.ProcessResults(hr, null, null);
            if (null != f)
            {
                throw f;
            }

            OleDbDataReader dataReader = new OleDbDataReader(null, null, 0, CommandBehavior.Default);
            dataReader.InitializeIRowset(value, ChapterHandle.DB_NULL_HCHAPTER, ADP.RecordsUnaffected);
            dataReader.BuildMetaInfo();
            dataReader.HasRowsRead();
            return dataReader;
        }

        public static OleDbDataReader GetRootEnumerator()
        {
            //readonly Guid CLSID_MSDAENUM = new Guid(0xc8b522d0,0x5cf3,0x11ce,0xad,0xe5,0x00,0xaa,0x00,0x44,0x77,0x3d);
            //Type msdaenum = Type.GetTypeFromCLSID(CLSID_MSDAENUM, true);
            const string PROGID_MSDAENUM = "MSDAENUM";
            Type msdaenum = Type.GetTypeFromProgID(PROGID_MSDAENUM, true)!;
            return GetEnumeratorFromType(msdaenum);
        }
    }
}
