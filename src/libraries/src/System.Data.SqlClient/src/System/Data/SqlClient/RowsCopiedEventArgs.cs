// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

namespace System.Data.SqlClient
{
    public class SqlRowsCopiedEventArgs : System.EventArgs
    {
        private bool _abort;
        private long _rowsCopied;

        public SqlRowsCopiedEventArgs(long rowsCopied)
        {
            _rowsCopied = rowsCopied;
        }

        public bool Abort
        {
            get
            {
                return _abort;
            }
            set
            {
                _abort = value;
            }
        }

        public long RowsCopied
        {
            get
            {
                return _rowsCopied;
            }
        }
    }
}
