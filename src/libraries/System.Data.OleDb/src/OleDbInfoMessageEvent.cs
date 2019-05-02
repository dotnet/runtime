// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Data.OleDb
{
    public sealed class OleDbInfoMessageEventArgs : System.EventArgs
    {
        readonly private OleDbException exception;

        internal OleDbInfoMessageEventArgs(OleDbException exception)
        {
            Debug.Assert(null != exception, "OleDbInfoMessageEventArgs without OleDbException");
            this.exception = exception;
        }

        public int ErrorCode
        {
            get
            {
                return this.exception.ErrorCode;
            }
        }

        public OleDbErrorCollection Errors
        {
            get
            {
                return this.exception.Errors;
            }
        }

        public string Message
        {
            get
            {
                return this.exception.Message;
            }
        }

        public string Source
        {
            get
            {
                return this.exception.Source;
            }
        }

        override public string ToString()
        {
            return Message;
        }
    }
}
