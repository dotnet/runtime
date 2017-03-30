// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Data.Odbc
{
    public delegate void OdbcInfoMessageEventHandler(object sender, OdbcInfoMessageEventArgs e);

    public sealed class OdbcInfoMessageEventArgs : System.EventArgs
    {
        private OdbcErrorCollection _errors;

        internal OdbcInfoMessageEventArgs(OdbcErrorCollection errors)
        {
            _errors = errors;
        }

        public OdbcErrorCollection Errors
        {
            get { return _errors; }
        }

        public string Message
        { // MDAC 84407
            get
            {
                StringBuilder builder = new StringBuilder();
                foreach (OdbcError error in Errors)
                {
                    if (0 < builder.Length) { builder.Append(Environment.NewLine); }
                    builder.Append(error.Message);
                }
                return builder.ToString();
            }
        }

        public override string ToString()
        {
            // MDAC 84407
            return Message;
        }
    }
}
