// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Mail
{
    internal enum SupportedAuth
    {
        None = 0,
        Login = 1
    };

    internal partial class SmtpConnection
    {
        private bool _serverSupportsEai;
        private bool _dsnEnabled;
        private bool _serverSupportsStartTls;
        private SupportedAuth _supportedAuth = SupportedAuth.None;
        private readonly ISmtpAuthenticationModule[] _authenticationModules;

        // accounts for the '=' or ' ' character after AUTH
        private const int SizeOfAuthString = 5;
        private const int SizeOfAuthExtension = 4;

        private static readonly char[] s_authExtensionSplitters = new char[] { ' ', '=' };
        private const string AuthExtension = "auth";
        private const string AuthLogin = "login";

        internal SmtpConnection(ISmtpAuthenticationModule[] authenticationModules)
        {
            _authenticationModules = authenticationModules;
        }

        internal bool DSNEnabled => _dsnEnabled;

        internal bool ServerSupportsEai => _serverSupportsEai;

        internal bool ServerSupportsStartTls => _serverSupportsStartTls;

        internal void ParseExtensions(string[] extensions)
        {
            _supportedAuth = SupportedAuth.None;
            foreach (string extension in extensions)
            {
                if (string.Compare(extension, 0, AuthExtension, 0,
                    SizeOfAuthExtension, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // remove the AUTH text including the following character 
                    // to ensure that split only gets the modules supported
                    string[] authTypes = extension.Remove(0, SizeOfAuthExtension).Split(s_authExtensionSplitters, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string authType in authTypes)
                    {
                        if (string.Equals(authType, AuthLogin, StringComparison.OrdinalIgnoreCase))
                        {
                            _supportedAuth |= SupportedAuth.Login;
                        }
                    }
                }
                else if (string.Compare(extension, 0, "dsn ", 0, 3, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    _dsnEnabled = true;
                }
                else if (string.Compare(extension, 0, "STARTTLS", 0, 8, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    _serverSupportsStartTls = true;
                }
                else if (string.Compare(extension, 0, "SMTPUTF8", 0, 8, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    _serverSupportsEai = true;
                }
            }
        }

        internal bool AuthSupported(ISmtpAuthenticationModule module)
        {
            return module is SmtpLoginAuthenticationModule && (_supportedAuth & SupportedAuth.Login) > 0;
        }
    }
}
