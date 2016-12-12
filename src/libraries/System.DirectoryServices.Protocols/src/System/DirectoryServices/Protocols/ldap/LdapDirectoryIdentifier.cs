// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;

    public class LdapDirectoryIdentifier : DirectoryIdentifier
    {
        private string[] _servers = null;
        private bool _fullyQualifiedDnsHostName = false;
        private bool _connectionless = false;
        private int _portNumber = 389;

        public LdapDirectoryIdentifier(string server) : this(server != null ? new string[] { server } : null, false, false)
        {
        }

        public LdapDirectoryIdentifier(string server, int portNumber) : this(server != null ? new string[] { server } : null, portNumber, false, false)
        {
        }

        public LdapDirectoryIdentifier(string server, bool fullyQualifiedDnsHostName, bool connectionless) : this(server != null ? new string[] { server } : null, fullyQualifiedDnsHostName, connectionless)
        {
        }

        public LdapDirectoryIdentifier(string server, int portNumber, bool fullyQualifiedDnsHostName, bool connectionless) : this(server != null ? new string[] { server } : null, portNumber, fullyQualifiedDnsHostName, connectionless)
        {
        }

        public LdapDirectoryIdentifier(string[] servers, bool fullyQualifiedDnsHostName, bool connectionless)
        {
            // validate the servers, we don't allow space in the server name
            if (servers != null)
            {
                _servers = new string[servers.Length];
                for (int i = 0; i < servers.Length; i++)
                {
                    if (servers[i] != null)
                    {
                        string trimmedName = servers[i].Trim();
                        string[] result = trimmedName.Split(new char[] { ' ' });
                        if (result.Length > 1)
                            throw new ArgumentException(Res.GetString(Res.WhiteSpaceServerName));
                        _servers[i] = trimmedName;
                    }
                }
            }
            _fullyQualifiedDnsHostName = fullyQualifiedDnsHostName;
            _connectionless = connectionless;
        }

        public LdapDirectoryIdentifier(string[] servers, int portNumber, bool fullyQualifiedDnsHostName, bool connectionless) : this(servers, fullyQualifiedDnsHostName, connectionless)
        {
            _portNumber = portNumber;
        }

        public string[] Servers
        {
            get
            {
                if (_servers == null)
                    return new string[0];
                else
                {
                    string[] temporaryServers = new string[_servers.Length];
                    for (int i = 0; i < _servers.Length; i++)
                    {
                        if (_servers[i] != null)
                            temporaryServers[i] = String.Copy(_servers[i]);
                        else
                            temporaryServers[i] = null;
                    }
                    return temporaryServers;
                }
            }
        }

        public bool Connectionless
        {
            get
            {
                return _connectionless;
            }
        }

        public bool FullyQualifiedDnsHostName
        {
            get
            {
                return _fullyQualifiedDnsHostName;
            }
        }

        public int PortNumber
        {
            get
            {
                return _portNumber;
            }
        }
    }
}
