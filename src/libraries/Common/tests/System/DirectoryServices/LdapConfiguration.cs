// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Xml.Linq;

namespace System.DirectoryServices.Tests
{
    internal class LdapConfiguration
    {
        private LdapConfiguration(string serverName, string searchDn, string userName, string password, string port, AuthenticationTypes at)
        {
            ServerName = serverName;
            SearchDn = searchDn;
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
        internal string SearchDn { get; set; }
        internal AuthenticationTypes AuthenticationTypes { get; set; }
        internal string LdapPath => string.IsNullOrEmpty(Port) ? $"LDAP://{ServerName}/{SearchDn}" : $"LDAP://{ServerName}:{Port}/{SearchDn}";
        internal string RootDSEPath => string.IsNullOrEmpty(Port) ? $"LDAP://{ServerName}/rootDSE" : $"LDAP://{ServerName}:{Port}/rootDSE";
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
            return string.IsNullOrEmpty(Port) ? $"LDAP://{ServerName}/{prefix},{SearchDn}" : $"LDAP://{ServerName}:{Port}/{prefix},{SearchDn}";
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
                string searchDn = "";
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

                    child = config.Element("SearchDN");
                    if (child != null)
                        searchDn = child.Value;

                    child = config.Element("Port");
                    if (child != null)
                        port = child.Value;

                    child = config.Element("User");
                    if (child != null)
                        user = child.Value;

                    child = config.Element("Password");
                    if (child != null)
                    {
                        string val = child.Value;
                        if (val.StartsWith("%") && val.EndsWith("%"))
                        {
                            val = Environment.GetEnvironmentVariable(val.Substring(1, val.Length - 2));
                        }
                        password = val;
                    }

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

                    ldapConfig = new LdapConfiguration(serverName, searchDn, user, password, port, at);
                }
            }
            catch (Exception ex)
            {
                // This runs within a test filter; if it throws, the test just skips. Instead we want to stop
                // so that it's quite clear that the server configuration is malformed.
                Environment.FailFast(ex.ToString());
            }
            return ldapConfig;
        }
    }
}
