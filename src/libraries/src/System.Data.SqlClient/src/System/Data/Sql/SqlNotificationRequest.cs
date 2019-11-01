// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Data.SqlClient;

namespace System.Data.Sql
{
    public sealed class SqlNotificationRequest
    {
        private string _userData;
        private string _options;
        private int _timeout;

        public SqlNotificationRequest()
                : this(null, null, SQL.SqlDependencyTimeoutDefault) { }

        public SqlNotificationRequest(string userData, string options, int timeout)
        {
            UserData = userData;
            Timeout = timeout;
            Options = options;
        }

        public string Options
        {
            get
            {
                return _options;
            }
            set
            {
                if ((null != value) && (ushort.MaxValue < value.Length))
                {
                    throw ADP.ArgumentOutOfRange(string.Empty, nameof(Options));
                }
                _options = value;
            }
        }

        public int Timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                if (0 > value)
                {
                    throw ADP.ArgumentOutOfRange(string.Empty, nameof(Timeout));
                }
                _timeout = value;
            }
        }

        public string UserData
        {
            get
            {
                return _userData;
            }
            set
            {
                if ((null != value) && (ushort.MaxValue < value.Length))
                {
                    throw ADP.ArgumentOutOfRange(string.Empty, nameof(UserData));
                }
                _userData = value;
            }
        }
    }
}