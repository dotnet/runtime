// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace System.DirectoryServices.Tests
{
    internal class LdapConfiguration
    {
        private LdapConfiguration(string serverName, string searchDn, string userName, string password, string port, AuthenticationTypes at, bool useTls)
        {
            ServerName = serverName;
            SearchDn = searchDn;
            UserName = userName;
            Password = password;
            Port = port;
            AuthenticationTypes = at;
            UseTls = useTls;
        }

        private static LdapConfiguration s_ldapConfiguration = GetConfiguration("LDAP.Configuration.xml");

        internal static LdapConfiguration Configuration =>  s_ldapConfiguration;

        internal string ServerName { get; set; }
        internal string UserName { get; set; }
        internal string Password { get; set; }
        internal string Port { get; set; }
        internal string SearchDn { get; set; }
        internal AuthenticationTypes AuthenticationTypes { get; set; }
        internal bool UseTls { get; set; }
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
                    // This requires System.DirectoryServices.dll, which is Windows-only
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
                Environment.FailFast("LDAP test configuration file not found: " + configFile);

            // To use test servers, set an environment variable LDAP_TEST_SERVER_INDEX
            // to the 0-based index of the <Connection> element in LDAP.Configuration.xml
            if (!int.TryParse(Environment.GetEnvironmentVariable("LDAP_TEST_SERVER_INDEX"), out int serverIndex))
            {
                return null;
            }

            LdapConfiguration ldapConfig = null;
            try
            {
                XElement configuration = XDocument.Load(configFile).Element("Configuration");

                XElement connection = configuration.Elements("Connection").Skip(serverIndex).First();

                Debug.WriteLine($"Using test LDAP server {connection.Attribute("Name").Value}");

                string serverName = "";
                string searchDn = "";
                string port = "";
                string user = "";
                string password = "";
                AuthenticationTypes at = AuthenticationTypes.None;
                bool useTls = false;

                XElement child = connection.Element("ServerName");
                if (child != null)
                    serverName = child.Value;

                child = connection.Element("SearchDN");
                if (child != null)
                    searchDn = child.Value;

                child = connection.Element("Port");
                if (child != null)
                    port = child.Value;

                child = connection.Element("User");
                if (child != null)
                    user = child.Value;

                child = connection.Element("Password");
                if (child != null)
                {
                    string val = child.Value;
                    if (val.StartsWith("%") && val.EndsWith("%"))
                    {
                        val = Environment.GetEnvironmentVariable(val.Substring(1, val.Length - 2));
                    }
                    password = val;
                }

                child = connection.Element("UseTls");
                if (child != null)
                {
                    useTls = bool.Parse(child.Value);
                }

                child = connection.Element("AuthenticationTypes");
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

                    ldapConfig = new LdapConfiguration(serverName, searchDn, user, password, port, at, useTls);
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
