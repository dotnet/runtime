// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------

using System.ComponentModel;

namespace System.Data.Common
{
    public abstract class DbDataRecord : ICustomTypeDescriptor, IDataRecord
    {
        protected DbDataRecord() : base()
        {
        }

        public abstract int FieldCount
        {
            get;
        }

        public abstract object this[int i]
        {
            get;
        }

        public abstract object this[string name]
        {
            get;
        }

        public abstract bool GetBoolean(int i);

        public abstract byte GetByte(int i);

        public abstract long GetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length);

        public abstract char GetChar(int i);

        public abstract long GetChars(int i, long dataIndex, char[] buffer, int bufferIndex, int length);

        public IDataReader GetData(int i)
        {
            return GetDbDataReader(i);
        }

        virtual protected DbDataReader GetDbDataReader(int i)
        {
            // NOTE: This method is virtual because we're required to implement
            //       it however most providers won't support it. Only the OLE DB 
            //       provider supports it right now, and they can override it.
            throw ADP.NotSupported();
        }

        public abstract string GetDataTypeName(int i);

        public abstract DateTime GetDateTime(int i);

        public abstract Decimal GetDecimal(int i);

        public abstract double GetDouble(int i);

        public abstract Type GetFieldType(int i);

        public abstract float GetFloat(int i);

        public abstract Guid GetGuid(int i);

        public abstract Int16 GetInt16(int i);

        public abstract Int32 GetInt32(int i);

        public abstract Int64 GetInt64(int i);

        public abstract string GetName(int i);

        public abstract int GetOrdinal(string name);

        public abstract string GetString(int i);

        public abstract object GetValue(int i);

        public abstract int GetValues(object[] values);

        public abstract bool IsDBNull(int i);

        //
        // ICustomTypeDescriptor
        //

        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return new AttributeCollection((Attribute[])null);
        }

        string ICustomTypeDescriptor.GetClassName()
        {
            return null;
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return null;
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return null;
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return null;
        }


        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return null;
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return null;
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return new EventDescriptorCollection(null);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return new EventDescriptorCollection(null);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return ((ICustomTypeDescriptor)this).GetProperties(null);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            return new PropertyDescriptorCollection(null);
        }

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }
    }
}
