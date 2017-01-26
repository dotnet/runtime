// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;       //ICollection
using System.ComponentModel;    //Component
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Data.Odbc
{
    [Serializable]
    public sealed class OdbcException : System.Data.Common.DbException
    {
        private OdbcErrorCollection _odbcErrors = new OdbcErrorCollection();

        private ODBC32.RETCODE _retcode;    // DO NOT REMOVE! only needed for serialization purposes, because Everett had it.

        static internal OdbcException CreateException(OdbcErrorCollection errors, ODBC32.RetCode retcode)
        {
            StringBuilder builder = new StringBuilder();
            foreach (OdbcError error in errors)
            {
                if (builder.Length > 0)
                {
                    builder.Append(Environment.NewLine);
                }

                builder.Append(Res.GetString(Res.Odbc_ExceptionMessage, ODBC32.RetcodeToString(retcode), error.SQLState, error.Message)); // MDAC 68337
            }
            OdbcException exception = new OdbcException(builder.ToString(), errors);
            return exception;
        }

        internal OdbcException(string message, OdbcErrorCollection errors) : base(message)
        {
            _odbcErrors = errors;
            HResult = HResults.OdbcException;
        }

        // runtime will call even if private...
        private OdbcException(SerializationInfo si, StreamingContext sc) : base(si, sc)
        {
            _retcode = (ODBC32.RETCODE)si.GetValue("odbcRetcode", typeof(ODBC32.RETCODE));
            _odbcErrors = (OdbcErrorCollection)si.GetValue("odbcErrors", typeof(OdbcErrorCollection));
            HResult = HResults.OdbcException;
        }

        public OdbcErrorCollection Errors
        {
            get
            {
                return _odbcErrors;
            }
        }

        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Flags = System.Security.Permissions.SecurityPermissionFlag.SerializationFormatter)]
        override public void GetObjectData(SerializationInfo si, StreamingContext context)
        {
            // MDAC 72003
            if (null == si)
            {
                throw new ArgumentNullException("si");
            }
            si.AddValue("odbcRetcode", _retcode, typeof(ODBC32.RETCODE));
            si.AddValue("odbcErrors", _odbcErrors, typeof(OdbcErrorCollection));
            base.GetObjectData(si, context);
        }

        // mdac bug 62559 - if we don't have it return nothing (empty string)
        override public string Source
        {
            get
            {
                if (0 < Errors.Count)
                {
                    string source = Errors[0].Source;
                    return ADP.IsEmpty(source) ? "" : source; // base.Source;
                }
                return ""; // base.Source;
            }
        }
    }
}
