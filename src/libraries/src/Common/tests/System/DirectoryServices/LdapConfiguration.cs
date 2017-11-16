// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml.Linq;

namespace System.DirectoryServices.Tests
{
    internal class LdapConfiguration
    {
        private LdapConfiguration(string serverName, string domain, string userName, string password, string port, AuthenticationTypes at)
        {
            ServerName = serverName;
            Domain = domain;
            UserName = userName;
            Password = password;
            Port = port;
            AuthenticationTypes = at;
        }

        private static LdapConfiguration s_ldapConfiguration = GetConfiguration("LDAP.Configuration.xml");

        internal static LdapConfiguration Configuration =>  s_ldapConfiguration;

        internal string ServerName { get; set; }
        internal string UserName { get; set; }
        internal string Password { get; set; }
        internal string Port { get; set; }
        internal string Domain { get; set; }
        internal AuthenticationTypes AuthenticationTypes { get; set; }
        internal string LdapPath => String.IsNullOrEmpty(Port) ? $"LDAP://{ServerName}/{Domain}" : $"LDAP://{ServerName}:{Port}/{Domain}";
        internal string RootDSEPath => String.IsNullOrEmpty(Port) ? $"LDAP://{ServerName}/rootDSE" : $"LDAP://{ServerName}:{Port}/rootDSE";
        internal string UserNameWithNoDomain
        {
            get
            {
                string [] parts = UserName.Split('\\');
                if (parts.Length > 1)
                    return parts[parts.Length - 1];

                parts = UserName.Split('@');
                if (parts.Length > 1)
                    return parts[0];

                return UserName;
            }
        }

        internal string GetLdapPath(string prefix) // like "ou=something"
        {
            return String.IsNullOrEmpty(Port) ? $"LDAP://{ServerName}/{prefix},{Domain}" : $"LDAP://{ServerName}:{Port}/{prefix},{Domain}";
        }

        private const string LDAP_CAP_ACTIVE_DIRECTORY_OID = "1.2.840.113556.1.4.800";

        internal bool IsActiveDirectoryServer
        {
            get
            {
                try
                {
                    using (DirectoryEntry rootDse = new DirectoryEntry(LdapConfiguration.Configuration.RootDSEPath,
                                            LdapConfiguration.Configuration.UserName,
                                            LdapConfiguration.Configuration.Password,
                                            LdapConfiguration.Configuration.AuthenticationTypes))
                    {
                        return rootDse.Properties["supportedCapabilities"].Contains(LDAP_CAP_ACTIVE_DIRECTORY_OID);
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        internal static LdapConfiguration GetConfiguration(string configFile)
        {
            if (!File.Exists(configFile))
                return null;

            LdapConfiguration ldapConfig = null;
            try
            {
                string serverName = "";
                string domain = "";
                string port = "";
                string user = "";
                string password = "";
                AuthenticationTypes at = AuthenticationTypes.None;

                XElement config = XDocument.Load(configFile).Element("Configuration");
                if (config != null)
                {
                    XElement child = config.Element("ServerName");
                    if (child != null)
                        serverName = child.Value;

                    child = config.Element("Domain");
                    if (child != null)
                        domain = child.Value;

                    child = config.Element("Port");
                    if (child != null)
                        port = child.Value;

                    child = config.Element("User");
                    if (child != null)
                        user = child.Value;

                    child = config.Element("Password");
                    if (child != null)
                        password = child.Value;

                    child = config.Element("AuthenticationTypes");
                    if (child != null)
                    {
                        string[] parts = child.Value.Split(',');
                        foreach (string p in parts)
                        {
                            string s = p.Trim();
                            if (s.Equals("Anonymous", StringComparison.OrdinalIgnoreCase))
                                at |= AuthenticationTypes.Anonymous;
                            if (s.Equals("Delegation", StringComparison.OrdinalIgnoreCase))
                                at |= AuthenticationTypes.Delegation;
                            if (s.Equals("Encryption", StringComparison.OrdinalIgnoreCase))
                                at |= AuthenticationTypes.FastBind;
                            if (s.Equals("FastBind", StringComparison.OrdinalIgnoreCase))
                                at |= AuthenticationTypes.FastBind;
                            if (s.Equals("ReadonlyServer", StringComparison.OrdinalIgnoreCase))
                                at |= AuthenticationTypes.ReadonlyServer;
                            if (s.Equals("Sealing", StringComparison.OrdinalIgnoreCase))
                                at |= AuthenticationTypes.Sealing;
                            if (s.Equals("Secure", StringComparison.OrdinalIgnoreCase))
                                at |= AuthenticationTypes.Secure;
                            if (s.Equals("SecureSocketsLayer", StringComparison.OrdinalIgnoreCase))
                                at |= AuthenticationTypes.SecureSocketsLayer;
                            if (s.Equals("ServerBind", StringComparison.OrdinalIgnoreCase))
                                at |= AuthenticationTypes.ServerBind;
                            if (s.Equals("Signing", StringComparison.OrdinalIgnoreCase))
                                at |= AuthenticationTypes.Signing;
                        }
                    }

                    ldapConfig = new LdapConfiguration(serverName, domain, user, password, port, at);
                }
            }
            catch
            {
                // Couldn't read the configurations, usually we'll skip the tests which depend on that
            }
            return ldapConfig;
        }
    }
}