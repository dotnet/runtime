// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



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
