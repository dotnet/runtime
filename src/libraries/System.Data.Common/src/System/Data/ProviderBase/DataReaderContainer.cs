// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.ProviderBase
{
    internal abstract class DataReaderContainer
    {
        protected readonly IDataReader _dataReader;
        protected int _fieldCount;

        internal static DataReaderContainer Create(IDataReader dataReader, bool returnProviderSpecificTypes)
        {
            if (returnProviderSpecificTypes)
            {
                DbDataReader? providerSpecificDataReader = (dataReader as DbDataReader);
                if (null != providerSpecificDataReader)
                {
                    return new ProviderSpecificDataReader(dataReader, providerSpecificDataReader);
                }
            }
            return new CommonLanguageSubsetDataReader(dataReader);
        }

        protected DataReaderContainer(IDataReader dataReader)
        {
            Debug.Assert(null != dataReader, "null dataReader");
            _dataReader = dataReader;
        }

        internal int FieldCount
        {
            get
            {
                return _fieldCount;
            }
        }

        internal abstract bool ReturnProviderSpecificTypes { get; }
        protected abstract int VisibleFieldCount { get; }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
        internal abstract Type GetFieldType(int ordinal);
        internal abstract object GetValue(int ordinal);
        internal abstract int GetValues(object[] values);

        internal string GetName(int ordinal)
        {
            string fieldName = _dataReader.GetName(ordinal);
            Debug.Assert(null != fieldName, "null GetName");
            return ((null != fieldName) ? fieldName : "");
        }
        internal DataTable? GetSchemaTable()
        {
            return _dataReader.GetSchemaTable();
        }
        internal bool NextResult()
        {
            _fieldCount = 0;
            if (_dataReader.NextResult())
            {
                _fieldCount = VisibleFieldCount;
                return true;
            }
            return false;
        }
        internal bool Read()
        {
            return _dataReader.Read();
        }

        private sealed class ProviderSpecificDataReader : DataReaderContainer
        {
            private readonly DbDataReader _providerSpecificDataReader;

            internal ProviderSpecificDataReader(IDataReader dataReader, DbDataReader dbDataReader) : base(dataReader)
            {
                Debug.Assert(null != dataReader, "null dbDataReader");
                _providerSpecificDataReader = dbDataReader;
                _fieldCount = VisibleFieldCount;
            }

            internal override bool ReturnProviderSpecificTypes
            {
                get
                {
                    return true;
                }
            }
            protected override int VisibleFieldCount
            {
                get
                {
                    int fieldCount = _providerSpecificDataReader.VisibleFieldCount;
                    Debug.Assert(0 <= fieldCount, "negative FieldCount");
                    return ((0 <= fieldCount) ? fieldCount : 0);
                }
            }

            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
            internal override Type GetFieldType(int ordinal)
            {
                Type fieldType = _providerSpecificDataReader.GetProviderSpecificFieldType(ordinal);
                Debug.Assert(null != fieldType, "null FieldType");
                return fieldType;
            }
            internal override object GetValue(int ordinal)
            {
                return _providerSpecificDataReader.GetProviderSpecificValue(ordinal);
            }
            internal override int GetValues(object[] values)
            {
                return _providerSpecificDataReader.GetProviderSpecificValues(values);
            }
        }

        private sealed class CommonLanguageSubsetDataReader : DataReaderContainer
        {
            internal CommonLanguageSubsetDataReader(IDataReader dataReader) : base(dataReader)
            {
                _fieldCount = VisibleFieldCount;
            }

            internal override bool ReturnProviderSpecificTypes
            {
                get
                {
                    return false;
                }
            }
            protected override int VisibleFieldCount
            {
                get
                {
                    int fieldCount = _dataReader.FieldCount;
                    Debug.Assert(0 <= fieldCount, "negative FieldCount");
                    return ((0 <= fieldCount) ? fieldCount : 0);
                }
            }

            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
            internal override Type GetFieldType(int ordinal)
            {
                return _dataReader.GetFieldType(ordinal);
            }
            internal override object GetValue(int ordinal)
            {
                return _dataReader.GetValue(ordinal);
            }
            internal override int GetValues(object[] values)
            {
                return _dataReader.GetValues(values);
            }
        }
    }
}
