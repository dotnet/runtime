// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Mail
{
    internal enum SupportedAuth
    {
        None = 0,
        Login = 1,
        NTLM = 2,
        GSSAPI = 4
    };

    internal partial class SmtpConnection
    {
        private bool _serverSupportsEai;
        private bool _dsnEnabled;
        private bool _serverSupportsStartTls;
        private bool _sawNegotiate;
        private SupportedAuth _supportedAuth = SupportedAuth.None;
        private readonly ISmtpAuthenticationModule[] _authenticationModules;

        // accounts for the '=' or ' ' character after AUTH
        private const int SizeOfAuthString = 5;
        private const int SizeOfAuthExtension = 4;

        private static readonly char[] s_authExtensionSplitters = new char[] { ' ', '=' };
        private const string AuthExtension = "auth";
        private const string AuthLogin = "login";
        private const string AuthNtlm = "ntlm";
        private const string AuthGssapi = "gssapi";

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
                        else if (string.Equals(authType, AuthNtlm, StringComparison.OrdinalIgnoreCase))
                        {
                            _supportedAuth |= SupportedAuth.NTLM;
                        }
                        else if (string.Equals(authType, AuthGssapi, StringComparison.OrdinalIgnoreCase))
                        {
                            _supportedAuth |= SupportedAuth.GSSAPI;
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
            if (module is SmtpLoginAuthenticationModule)
            {
                if ((_supportedAuth & SupportedAuth.Login) > 0)
                {
                    return true;
                }
            }
            else if (module is SmtpNegotiateAuthenticationModule)
            {
                if ((_supportedAuth & SupportedAuth.GSSAPI) > 0)
                {
                    _sawNegotiate = true;
                    return true;
                }
            }
            else if (module is SmtpNtlmAuthenticationModule)
            {
                // Don't try ntlm if negotiate has been tried
                if ((!_sawNegotiate && (_supportedAuth & SupportedAuth.NTLM) > 0))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
