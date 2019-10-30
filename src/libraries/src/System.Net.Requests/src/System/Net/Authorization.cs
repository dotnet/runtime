// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    public class Authorization
    {
        private string[] _protectionRealm;
        private bool _mutualAuth;

        public Authorization(string token) :
            this(token, true) { }

        public Authorization(string token, bool finished) :
            this(token, finished, null) { }

        public Authorization(string token, bool finished, string connectionGroupId) :
            this(token, finished, connectionGroupId, false) { }

        internal Authorization(string token, bool finished, string connectionGroupId, bool mutualAuth)
        {
            Message = string.IsNullOrEmpty(token) ? null : token;
            ConnectionGroupId = string.IsNullOrEmpty(connectionGroupId) ? null : connectionGroupId;
            Complete = finished;
            _mutualAuth = mutualAuth;
        }

        public string Message { get; }

        public string ConnectionGroupId { get; }

        public bool Complete { get; internal set; }

        public string[] ProtectionRealm
        {
            get { return _protectionRealm; }
            set { _protectionRealm = value != null && value.Length != 0 ? value : null; }
        }

        public bool MutuallyAuthenticated
        {
            get { return Complete && _mutualAuth; }
            set { _mutualAuth = value; }
        }
    }
}
