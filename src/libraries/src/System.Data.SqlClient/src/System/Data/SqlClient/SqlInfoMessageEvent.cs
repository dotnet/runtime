// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------


namespace System.Data.SqlClient
{
    public sealed class SqlInfoMessageEventArgs : System.EventArgs
    {
        private SqlException _exception;

        internal SqlInfoMessageEventArgs(SqlException exception)
        {
            _exception = exception;
        }

        public SqlErrorCollection Errors
        {
            get { return _exception.Errors; }
        }

        private bool ShouldSerializeErrors()
        {
            return (null != _exception) && (0 < _exception.Errors.Count);
        }

        public string Message
        {
            get { return _exception.Message; }
        }

        public string Source
        {
            get { return _exception.Source; }
        }

        override public string ToString()
        {
            return Message;
        }
    }
}
