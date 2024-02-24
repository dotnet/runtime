// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common
{
    public class DbDataReaderEnumerator : IEnumerator<DbDataRecord>, IAsyncEnumerator<DbDataRecord>
    {
        private readonly DbDataReader _reader;
        private DbDataRecord? _current;
        private SchemaInfo[]? _schemaInfo; // shared schema info among all the data records
        private PropertyDescriptorCollection? _descriptors; // cached property descriptors
        private FieldNameLookup? _fieldNameLookup;
        private readonly bool _closeReader;

        // users must get enumerators off of the datareader interfaces
        public DbDataReaderEnumerator(DbDataReader reader)
        {
            if (null == reader)
            {
                throw ADP.ArgumentNull(nameof(reader));
            }
            _reader = reader;
        }

        public DbDataReaderEnumerator(DbDataReader reader, bool closeReader)
        {
            if (null == reader)
            {
                throw ADP.ArgumentNull(nameof(reader));
            }
            _reader = reader;
            _closeReader = closeReader;
        }

        public DbDataRecord Current
        {
            get
            {
                Debug.Assert(null != _current, nameof(_current) + " was null.");
                return _current;
            }
        }
        object IEnumerator.Current => Current;

        private void MoveNextInternal()
        {
            if (null == _schemaInfo)
            {
                BuildSchemaInfo();
            }
            Debug.Assert(null != _schemaInfo && null != _descriptors && _fieldNameLookup != null, "Unable to build schema information!");
            _current = null;
            // setup our current record
            object[] values = new object[_schemaInfo.Length];
            _reader.GetValues(values);
            _current = new DataRecordInternal(_schemaInfo, values, _descriptors, _fieldNameLookup);
        }

        public bool MoveNext()
        {
            bool anyRead = _reader.Read();
            if (anyRead) { MoveNextInternal(); }
            else
            {
                if (_closeReader)
                {
                    _reader.Close();
                }
            }
            return anyRead;
        }

        public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            bool anyRead = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (anyRead) { MoveNextInternal(); }
            else
            {
                if (_closeReader)
                {
                    await _reader.CloseAsync().ConfigureAwait(false);
                }
            }
            return anyRead;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            return await MoveNextAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_closeReader)
            {
                _reader.Close();
            }
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (_closeReader)
            {
                await _reader.DisposeAsync().ConfigureAwait(false);
            }
            GC.SuppressFinalize(this);
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
                SchemaInfo s = default;
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
            private readonly int _ordinal;
            private readonly Type _type;

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

            public override object? GetValue(object? component) => ((IDataRecord)component!)[_ordinal];

            public override void ResetValue(object component)
            {
                throw ADP.NotSupported();
            }

            public override void SetValue(object? component, object? value)
            {
                throw ADP.NotSupported();
            }

            public override bool ShouldSerializeValue(object component) => false;
        }
    }
}
