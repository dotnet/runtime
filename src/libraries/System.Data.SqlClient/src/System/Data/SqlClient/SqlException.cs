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
    public sealed class SqlException : System.Data.Common.DbException
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
            _errors = (SqlErrorCollection)si.GetValue("Errors", typeof(SqlErrorCollection));
            _clientConnectionId = (Guid)si.GetValue("ClientConnectionId", typeof(Guid));
        }

        public override void GetObjectData(SerializationInfo si, StreamingContext context)
        {
            if (null == si)
            {
                throw new ArgumentNullException(nameof(si));
            }

            si.AddValue("Errors", _errors, typeof(SqlErrorCollection));
            si.AddValue("ClientConnectionId", _clientConnectionId, typeof(Guid));
            base.GetObjectData(si, context);
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
            get { return this.Errors[0].Class; }
        }

        public int LineNumber
        {
            get { return this.Errors[0].LineNumber; }
        }

        public int Number
        {
            get { return this.Errors[0].Number; }
        }

        public string Procedure
        {
            get { return this.Errors[0].Procedure; }
        }

        public string Server
        {
            get { return this.Errors[0].Server; }
        }

        public byte State
        {
            get { return this.Errors[0].State; }
        }

        override public string Source
        {
            get { return this.Errors[0].Source; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString());
            sb.AppendLine();
            sb.AppendFormat(SQLMessage.ExClientConnectionId(), _clientConnectionId);

            // Append the error number, state and class if the server provided it
            if (Number != 0)
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

        static internal SqlException CreateException(SqlErrorCollection errorCollection, string serverVersion)
        {
            return CreateException(errorCollection, serverVersion, Guid.Empty);
        }

        static internal SqlException CreateException(SqlErrorCollection errorCollection, string serverVersion, SqlInternalConnectionTds internalConnection, Exception innerException = null)
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

        static internal SqlException CreateException(SqlErrorCollection errorCollection, string serverVersion, Guid conId, Exception innerException = null)
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
