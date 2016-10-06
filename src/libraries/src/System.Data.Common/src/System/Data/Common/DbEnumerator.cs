// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.ComponentModel;

namespace System.Data.Common
{
    public class DbEnumerator : IEnumerator
    {
        internal IDataReader _reader;
        internal DbDataRecord _current;
        internal SchemaInfo[] _schemaInfo; // shared schema info among all the data records
        internal PropertyDescriptorCollection _descriptors; // cached property descriptors
        private FieldNameLookup _fieldNameLookup;
        private bool _closeReader;

        // users must get enumerators off of the datareader interfaces
        public DbEnumerator(IDataReader reader)
        {
            if (null == reader)
            {
                throw ADP.ArgumentNull(nameof(reader));
            }
            _reader = reader;
        }

        public DbEnumerator(IDataReader reader, bool closeReader)
        {
            if (null == reader)
            {
                throw ADP.ArgumentNull(nameof(reader));
            }
            _reader = reader;
            _closeReader = closeReader;
        }

        public DbEnumerator(DbDataReader reader)
            : this((IDataReader)reader)
        {
        }

        public DbEnumerator(DbDataReader reader, bool closeReader)
            : this((IDataReader)reader, closeReader)
        {
        }

        public object Current => _current;

        public bool MoveNext()
        {
            if (null == _schemaInfo)
            {
                BuildSchemaInfo();
            }

            Debug.Assert(null != _schemaInfo && null != _descriptors, "unable to build schema information!");
            _current = null;

            if (_reader.Read())
            {
                // setup our current record
                object[] values = new object[_schemaInfo.Length];
                _reader.GetValues(values); // this.GetValues()
                _current = new DataRecordInternal(_schemaInfo, values, _descriptors, _fieldNameLookup);
                return true;
            }
            if (_closeReader)
            {
                _reader.Close();
            }
            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
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
            PropertyDescriptor[] props = new PropertyDescriptor[_reader.FieldCount];
            for (int i = 0; i < si.Length; i++)
            {
                SchemaInfo s = new SchemaInfo();
                s.name = _reader.GetName(i);
                s.type = _reader.GetFieldType(i);
                s.typeName = _reader.GetDataTypeName(i);
                props[i] = new DbColumnDescriptor(i, fieldnames[i], s.type);
                si[i] = s;
            }

            _schemaInfo = si;
            _fieldNameLookup = new FieldNameLookup(_reader, -1);
            _descriptors = new PropertyDescriptorCollection(props);
        }

        private sealed class DbColumnDescriptor : PropertyDescriptor
        {
            private int _ordinal;
            private Type _type;

            internal DbColumnDescriptor(int ordinal, string name, Type type)
                : base(name, null)
            {
                _ordinal = ordinal;
                _type = type;
            }

            public override Type ComponentType => typeof(IDataRecord);

            public override bool IsReadOnly => true;

            public override Type PropertyType => _type;

            public override bool CanResetValue(object component) => false;

            public override object GetValue(object component) => ((IDataRecord)component)[_ordinal];

            public override void ResetValue(object component)
            {
                throw ADP.NotSupported();
            }

            public override void SetValue(object component, object value)
            {
                throw ADP.NotSupported();
            }

            public override bool ShouldSerializeValue(object component) => false;
        }
    }
}
