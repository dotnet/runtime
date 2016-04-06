// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Collections;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.ComponentModel;

namespace System.Data.Common
{
    public class DbEnumerator : IEnumerator
    {
        internal DbDataReader _reader;
        internal DbDataRecord _current;
        internal SchemaInfo[] _schemaInfo; // shared schema info among all the data records
        private FieldNameLookup _fieldNameLookup;

        private bool _closeReader;

        public DbEnumerator(DbDataReader reader, bool closeReader)
        {
            if (null == reader)
            {
                throw ADP.ArgumentNull(nameof(reader));
            }
            _reader = reader;
            _closeReader = closeReader;
        }


        public object Current
        {
            get
            {
                return _current;
            }
        }

        public bool MoveNext()
        {
            if (null == _schemaInfo)
            {
                BuildSchemaInfo();
            }

            Debug.Assert(null != _schemaInfo, "unable to build schema information!");
            _current = null;

            if (_reader.Read())
            {
                // setup our current record
                object[] values = new object[_schemaInfo.Length];
                _reader.GetValues(values); // this.GetValues()
                _current = new DataRecordInternal(_schemaInfo, values, _fieldNameLookup); // this.NewRecord()
                return true;
            }
            if (_closeReader)
            {
                _reader.Dispose();
            }
            return false;
        }

        [EditorBrowsableAttribute(EditorBrowsableState.Never)]
        public void Reset()
        {
            throw ADP.NotSupported();
        }

        private void BuildSchemaInfo()
        {
            int count = _reader.FieldCount;
            string[] fieldnames = new string[count];
            for (int i = 0; i < count; ++i)
            {
                fieldnames[i] = _reader.GetName(i);
            }
            ADP.BuildSchemaTableInfoTableNames(fieldnames);

            SchemaInfo[] si = new SchemaInfo[count];
            for (int i = 0; i < si.Length; i++)
            {
                SchemaInfo s = new SchemaInfo();
                s.name = _reader.GetName(i);
                s.type = _reader.GetFieldType(i);
                s.typeName = _reader.GetDataTypeName(i);
                si[i] = s;
            }

            _schemaInfo = si;
            _fieldNameLookup = new FieldNameLookup(_reader, -1);
        }
    }
}
