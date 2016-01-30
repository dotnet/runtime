// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Data.Common;

namespace System.Data.SqlClient
{
    public sealed partial class SqlParameterCollection : DbParameterCollection
    {
        private bool _isDirty;
        private static Type s_itemType = typeof(SqlParameter);

        internal SqlParameterCollection() : base()
        {
        }

        internal bool IsDirty
        {
            get
            {
                return _isDirty;
            }
            set
            {
                _isDirty = value;
            }
        }

        new public SqlParameter this[int index]
        {
            get
            {
                return (SqlParameter)GetParameter(index);
            }
            set
            {
                SetParameter(index, value);
            }
        }

        new public SqlParameter this[string parameterName]
        {
            get
            {
                return (SqlParameter)GetParameter(parameterName);
            }
            set
            {
                SetParameter(parameterName, value);
            }
        }

        public SqlParameter Add(SqlParameter value)
        {
            Add((object)value);
            return value;
        }

        public SqlParameter AddWithValue(string parameterName, object value)
        { // 79027
            return Add(new SqlParameter(parameterName, value));
        }

        public SqlParameter Add(string parameterName, SqlDbType sqlDbType)
        {
            return Add(new SqlParameter(parameterName, sqlDbType));
        }

        public SqlParameter Add(string parameterName, SqlDbType sqlDbType, int size)
        {
            return Add(new SqlParameter(parameterName, sqlDbType, size));
        }

        public SqlParameter Add(string parameterName, SqlDbType sqlDbType, int size, string sourceColumn)
        {
            return Add(new SqlParameter(parameterName, sqlDbType, size, sourceColumn));
        }

        public void AddRange(SqlParameter[] values)
        {
            AddRange((Array)values);
        }

        override public bool Contains(string value)
        { // WebData 97349
            return (-1 != IndexOf(value));
        }

        public bool Contains(SqlParameter value)
        {
            return (-1 != IndexOf(value));
        }

        public void CopyTo(SqlParameter[] array, int index)
        {
            CopyTo((Array)array, index);
        }

        public int IndexOf(SqlParameter value)
        {
            return IndexOf((object)value);
        }

        public void Insert(int index, SqlParameter value)
        {
            Insert(index, (object)value);
        }

        private void OnChange()
        {
            IsDirty = true;
        }

        public void Remove(SqlParameter value)
        {
            Remove((object)value);
        }
    }
}
