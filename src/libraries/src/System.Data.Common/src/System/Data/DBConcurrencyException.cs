// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Data
{
    [Serializable]
    public sealed class DBConcurrencyException : SystemException
    {
        private DataRow[] _dataRows;

        public DBConcurrencyException() : this(SR.ADP_DBConcurrencyExceptionMessage, null)
        {
        }

        public DBConcurrencyException(string message) : this(message, null)
        {
        }

        public DBConcurrencyException(string message, Exception inner) : base(message, inner)
        {
            HResult = HResults.DBConcurrency;
        }

        public DBConcurrencyException(string message, Exception inner, DataRow[] dataRows) : base(message, inner)
        {
            HResult = HResults.DBConcurrency;
            _dataRows = dataRows;
        }

        // runtime will call even if private...
        private DBConcurrencyException(SerializationInfo si, StreamingContext sc) : base(si, sc)
        {
        }

        public override void GetObjectData(SerializationInfo si, StreamingContext context)
        {
            if (null == si)
            {
                throw new ArgumentNullException(nameof(si));
            }
            base.GetObjectData(si, context);
        }

        public DataRow Row
        {
            get
            {
                DataRow[] rows = _dataRows;
                return (((null != rows) && (0 < rows.Length)) ? rows[0] : null);
            }
            set
            {
                _dataRows = new DataRow[1] { value };
            }
        }

        public int RowCount
        {
            get
            {
                DataRow[] dataRows = _dataRows;
                return ((null != dataRows) ? dataRows.Length : 0);
            }
        }

        public void CopyToRows(DataRow[] array)
        {
            CopyToRows(array, 0);
        }

        public void CopyToRows(DataRow[] array, int arrayIndex)
        {
            DataRow[] dataRows = _dataRows;
            if (null != dataRows)
            {
                dataRows.CopyTo(array, arrayIndex);
            }
        }
    }
}
