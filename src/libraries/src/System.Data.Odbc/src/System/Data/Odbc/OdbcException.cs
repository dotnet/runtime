// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Runtime.Serialization;
using System.Text;

namespace System.Data.Odbc
{
    public sealed class OdbcException : System.Data.Common.DbException
    {
        private OdbcErrorCollection _odbcErrors = new OdbcErrorCollection();

        internal static OdbcException CreateException(OdbcErrorCollection errors, ODBC32.RetCode retcode)
        {
            StringBuilder builder = new StringBuilder();
            foreach (OdbcError error in errors)
            {
                if (builder.Length > 0)
                {
                    builder.Append(Environment.NewLine);
                }

                builder.Append(SR.GetString(SR.Odbc_ExceptionMessage, ODBC32.RetcodeToString(retcode), error.SQLState, error.Message)); // MDAC 68337
            }
            OdbcException exception = new OdbcException(builder.ToString(), errors);
            return exception;
        }

        internal OdbcException(string message, OdbcErrorCollection errors) : base(message)
        {
            _odbcErrors = errors;
            HResult = HResults.OdbcException;
        }

        public OdbcErrorCollection Errors
        {
            get
            {
                return _odbcErrors;
            }
        }

        public override void GetObjectData(SerializationInfo si, StreamingContext context)
        {
            base.GetObjectData(si, context);
        }

        // mdac bug 62559 - if we don't have it return nothing (empty string)
        public override string Source
        {
            get
            {
                if (0 < Errors.Count)
                {
                    string source = Errors[0].Source;
                    return string.IsNullOrEmpty(source) ? "" : source; // base.Source;
                }
                return ""; // base.Source;
            }
        }
    }
}
