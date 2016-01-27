// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



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
