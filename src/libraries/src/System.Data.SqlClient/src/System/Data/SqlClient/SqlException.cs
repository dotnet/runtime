// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Collections;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text; // StringBuilder

namespace System.Data.SqlClient
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed partial class SqlException : System.Data.Common.DbException
    {
        private const string OriginalClientConnectionIdKey = "OriginalClientConnectionId";
        private const string RoutingDestinationKey = "RoutingDestination";
        private const int SqlExceptionHResult = unchecked((int)0x80131904);

        private SqlErrorCollection _errors;
        private Guid _clientConnectionId = Guid.Empty;

        private SqlException(string message, SqlErrorCollection errorCollection, Exception innerException, Guid conId) : base(message, innerException)
        {
            HResult = SqlExceptionHResult;
            _errors = errorCollection;
            _clientConnectionId = conId;
        }

        private SqlException(SerializationInfo si, StreamingContext sc) : base(si, sc) 
        {
            HResult = SqlExceptionHResult;
            foreach (SerializationEntry siEntry in si)
            {
                if ("ClientConnectionId" == siEntry.Name) 
                {
                    _clientConnectionId = (Guid)siEntry.Value;
                    break;
                }
            }
        }

        public override void GetObjectData(SerializationInfo si, StreamingContext context)
        {
            base.GetObjectData(si, context);
            si.AddValue("Errors", null); // Not specifying type to enable serialization of null value of non-serializable type
            si.AddValue("ClientConnectionId", _clientConnectionId, typeof(Guid));

            // Writing sqlerrors to base exception data table
            for (int i = 0; i < Errors.Count; i++)
            {
                string key = "SqlError " + (i + 1);
                if (Data.Contains(key))
                {
                    Data.Remove(key);
                }
                Data.Add(key, Errors[i].ToString());
            }
        }

        // runtime will call even if private...
        public SqlErrorCollection Errors
        {
            get
            {
                if (_errors == null)
                {
                    _errors = new SqlErrorCollection();
                }
                return _errors;
            }
        }

        public Guid ClientConnectionId
        {
            get
            {
                return _clientConnectionId;
            }
        }


        public byte Class
        {
            get { return Errors.Count > 0 ? this.Errors[0].Class : default; }
        }

        public int LineNumber
        {
            get { return Errors.Count > 0 ? Errors[0].LineNumber : default; }
        }

        public int Number
        {
            get { return Errors.Count > 0 ? Errors[0].Number : default; }
        }

        public string Procedure
        {
            get { return Errors.Count > 0 ? Errors[0].Procedure : default; }
        }

        public string Server
        {
            get { return Errors.Count > 0 ? Errors[0].Server : default; }
        }

        public byte State
        {
            get { return Errors.Count > 0 ? Errors[0].State : default; }
        }

        override public string Source
        {
            get { return Errors.Count > 0 ? Errors[0].Source : default; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString());
            sb.AppendLine();
            sb.AppendFormat(SQLMessage.ExClientConnectionId(), _clientConnectionId);

            // Append the error number, state and class if the server provided it
            if (Errors.Count > 0 && Number != 0)
            {
                sb.AppendLine();
                sb.AppendFormat(SQLMessage.ExErrorNumberStateClass(), Number, State, Class);
            }

            // If routed, include the original client connection id
            if (Data.Contains(OriginalClientConnectionIdKey))
            {
                sb.AppendLine();
                sb.AppendFormat(SQLMessage.ExOriginalClientConnectionId(), Data[OriginalClientConnectionIdKey]);
            }

            // If routed, provide the routing destination
            if (Data.Contains(RoutingDestinationKey))
            {
                sb.AppendLine();
                sb.AppendFormat(SQLMessage.ExRoutingDestination(), Data[RoutingDestinationKey]);
            }

            return sb.ToString();
        }

        internal static SqlException CreateException(SqlErrorCollection errorCollection, string serverVersion)
        {
            return CreateException(errorCollection, serverVersion, Guid.Empty);
        }

        internal static SqlException CreateException(SqlErrorCollection errorCollection, string serverVersion, SqlInternalConnectionTds internalConnection, Exception innerException = null)
        {
            Guid connectionId = (internalConnection == null) ? Guid.Empty : internalConnection._clientConnectionId;
            var exception = CreateException(errorCollection, serverVersion, connectionId, innerException);

            if (internalConnection != null)
            {
                if ((internalConnection.OriginalClientConnectionId != Guid.Empty) && (internalConnection.OriginalClientConnectionId != internalConnection.ClientConnectionId))
                {
                    exception.Data.Add(OriginalClientConnectionIdKey, internalConnection.OriginalClientConnectionId);
                }

                if (!string.IsNullOrEmpty(internalConnection.RoutingDestination))
                {
                    exception.Data.Add(RoutingDestinationKey, internalConnection.RoutingDestination);
                }
            }

            return exception;
        }

        internal static SqlException CreateException(SqlErrorCollection errorCollection, string serverVersion, Guid conId, Exception innerException = null)
        {
            Debug.Assert(null != errorCollection && errorCollection.Count > 0, "no errorCollection?");

            StringBuilder message = new StringBuilder();
            for (int i = 0; i < errorCollection.Count; i++)
            {
                if (i > 0)
                {
                    message.Append(Environment.NewLine);
                }
                message.Append(errorCollection[i].Message);
            }

            if (innerException == null && errorCollection[0].Win32ErrorCode != 0 && errorCollection[0].Win32ErrorCode != -1)
            {
                innerException = new Win32Exception(errorCollection[0].Win32ErrorCode);
            }

            SqlException exception = new SqlException(message.ToString(), errorCollection, innerException, conId);

            exception.Data.Add("HelpLink.ProdName", "Microsoft SQL Server");

            if (!string.IsNullOrEmpty(serverVersion))
            {
                exception.Data.Add("HelpLink.ProdVer", serverVersion);
            }
            exception.Data.Add("HelpLink.EvtSrc", "MSSQLServer");
            exception.Data.Add("HelpLink.EvtID", errorCollection[0].Number.ToString(CultureInfo.InvariantCulture));
            exception.Data.Add("HelpLink.BaseHelpUrl", "http://go.microsoft.com/fwlink");
            exception.Data.Add("HelpLink.LinkId", "20476");

            return exception;
        }

        internal SqlException InternalClone()
        {
            SqlException exception = new SqlException(Message, _errors, InnerException, _clientConnectionId);
            if (this.Data != null)
                foreach (DictionaryEntry entry in this.Data)
                    exception.Data.Add(entry.Key, entry.Value);
            exception._doNotReconnect = this._doNotReconnect;
            return exception;
        }

        // Do not serialize this field! It is used to indicate that no reconnection attempts are required
        internal bool _doNotReconnect = false;
    }
}
