//------------------------------------------------------------------------------
// <copyright file="LdapDirectoryIdentifier.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

namespace System.DirectoryServices.Protocols {
     using System;

     public class LdapDirectoryIdentifier :DirectoryIdentifier {
        string[] servers = null;
        bool fullyQualifiedDnsHostName = false;
        bool connectionless = false;
        int portNumber = 389;
        
        public LdapDirectoryIdentifier(string server) :this(server != null ? new string[] {server} : null, false, false)
        {
        }
        
        public LdapDirectoryIdentifier(string server, int portNumber) :this(server != null ? new string[] {server} : null, portNumber, false, false)
        {
        }

        public LdapDirectoryIdentifier(string server, bool fullyQualifiedDnsHostName, bool connectionless) :this(server != null ? new string[] {server} : null, fullyQualifiedDnsHostName, connectionless)
        {
        }

        public LdapDirectoryIdentifier(string server, int portNumber, bool fullyQualifiedDnsHostName, bool connectionless) :this(server != null ? new string[] {server} : null, portNumber, fullyQualifiedDnsHostName, connectionless)
        {
        }

        public LdapDirectoryIdentifier(string[] servers, bool fullyQualifiedDnsHostName, bool connectionless)
        {
            // validate the servers, we don't allow space in the server name
            if(servers != null)
            {
                this.servers = new string[servers.Length];
                for(int i = 0; i < servers.Length; i++)
                {
                    if(servers[i] != null)
                    {
                        string trimmedName = servers[i].Trim();
                        string[] result = trimmedName.Split(new char[] {' '});
                        if(result.Length > 1)
                            throw new ArgumentException(Res.GetString(Res.WhiteSpaceServerName));
                        this.servers[i] = trimmedName;
                    
                    }
                }
            }            
            this.fullyQualifiedDnsHostName = fullyQualifiedDnsHostName;
            this.connectionless = connectionless;            
        }

        public LdapDirectoryIdentifier(string[] servers, int portNumber, bool fullyQualifiedDnsHostName, bool connectionless) :this(servers, fullyQualifiedDnsHostName, connectionless)
        {
            this.portNumber = portNumber;
        }

        public string[] Servers {
            get {
                if(servers == null)
                    return new string[0];
                else
                {
                    string[] temporaryServers = new string[servers.Length];
                    for(int i = 0; i<servers.Length; i++)
                    {
                        if(servers[i] != null)
                            temporaryServers[i] = String.Copy(servers[i]);
                        else
                            temporaryServers[i] = null;
                    }
                    return temporaryServers;
                }
            }
        }

        public bool Connectionless {
            get {
                return connectionless;
            }            
        }

        public bool FullyQualifiedDnsHostName {
            get {
                return fullyQualifiedDnsHostName;
            }            
        }

        public int PortNumber {
            get {
                return portNumber;
            }            
        }

     }
}
