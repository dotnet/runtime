// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Data
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class DBConcurrencyException : SystemException
    {
        private DataRow[]? _dataRows;

        public DBConcurrencyException() : this(SR.ADP_DBConcurrencyExceptionMessage, null)
        {
        }

        public DBConcurrencyException(string? message) : this(message, null)
        {
        }

        public DBConcurrencyException(string? message, Exception? inner) : base(message ?? SR.ADP_DBConcurrencyExceptionMessage, inner)
        {
            HResult = HResults.DBConcurrency;
        }

        public DBConcurrencyException(string? message, Exception? inner, DataRow[]? dataRows) : base(message ?? SR.ADP_DBConcurrencyExceptionMessage, inner)
        {
            HResult = HResults.DBConcurrency;
            _dataRows = dataRows;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private DBConcurrencyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        [DisallowNull]
        public DataRow? Row
        {
            get
            {
                DataRow[]? rows = _dataRows;
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
                DataRow[]? dataRows = _dataRows;
                return ((null != dataRows) ? dataRows.Length : 0);
            }
        }

        public void CopyToRows(DataRow[] array)
        {
            CopyToRows(array, 0);
        }

        public void CopyToRows(DataRow[] array, int arrayIndex)
        {
            _dataRows?.CopyTo(array, arrayIndex);
        }
    }
}
