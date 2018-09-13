// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Globalization;
using System.Threading;

namespace System
{
    public class UriBuilder
    {
        // fields

        private bool _changed = true;
        private string _fragment = string.Empty;
        private string _host = "localhost";
        private string _password = string.Empty;
        private string _path = "/";
        private int _port = -1;
        private string _query = string.Empty;
        private string _scheme = "http";
        private string _schemeDelimiter = Uri.SchemeDelimiter;
        private Uri _uri;
        private string _username = string.Empty;

        // constructors

        public UriBuilder()
        {
        }

        public UriBuilder(string uri)
        {
            // setting allowRelative=true for a string like www.acme.org
            Uri tryUri = new Uri(uri, UriKind.RelativeOrAbsolute);

            if (tryUri.IsAbsoluteUri)
            {
                Init(tryUri);
            }
            else
            {
                uri = Uri.UriSchemeHttp + Uri.SchemeDelimiter + uri;
                Init(new Uri(uri));
            }
        }

        public UriBuilder(Uri uri)
        {
            if ((object)uri == null)
                throw new ArgumentNullException(nameof(uri));

            Init(uri);
        }

        private void Init(Uri uri)
        {
            _fragment = uri.Fragment;
            _query = uri.Query;
            _host = uri.Host;
            _path = uri.AbsolutePath;
            _port = uri.Port;
            _scheme = uri.Scheme;
            _schemeDelimiter = uri.HasAuthority ? Uri.SchemeDelimiter : ":";

            string userInfo = uri.UserInfo;

            if (!string.IsNullOrEmpty(userInfo))
            {
                int index = userInfo.IndexOf(':');

                if (index != -1)
                {
                    _password = userInfo.Substring(index + 1);
                    _username = userInfo.Substring(0, index);
                }
                else
                {
                    _username = userInfo;
                }
            }
            SetFieldsFromUri(uri);
        }

        public UriBuilder(string schemeName, string hostName)
        {
            Scheme = schemeName;
            Host = hostName;
        }

        public UriBuilder(string scheme, string host, int portNumber) : this(scheme, host)
        {
            Port = portNumber;
        }

        public UriBuilder(string scheme,
                          string host,
                          int port,
                          string pathValue
                          ) : this(scheme, host, port)
        {
            Path = pathValue;
        }

        public UriBuilder(string scheme,
                          string host,
                          int port,
                          string path,
                          string extraValue
                          ) : this(scheme, host, port, path)
        {
            try
            {
                Extra = extraValue;
            }
            catch (Exception exception)
            {
                if (exception is OutOfMemoryException)
                {
                    throw;
                }

                throw new ArgumentException(SR.Argument_ExtraNotValid, nameof(extraValue));
            }
        }

        // properties

        private string Extra
        {
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                if (value.Length > 0)
                {
                    if (value[0] == '#')
                    {
                        Fragment = value.Substring(1);
                    }
                    else if (value[0] == '?')
                    {
                        int end = value.IndexOf('#');
                        if (end == -1)
                        {
                            end = value.Length;
                        }
                        else
                        {
                            Fragment = value.Substring(end + 1);
                        }
                        Query = value.Substring(1, end - 1);
                    }
                    else
                    {
                        throw new ArgumentException(SR.Argument_ExtraNotValid, nameof(value));
                    }
                }
                else
                {
                    Fragment = string.Empty;
                    Query = string.Empty;
                }
            }
        }

        public string Fragment
        {
            get
            {
                return _fragment;
            }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                if (value.Length > 0 && value[0] != '#')
                {
                    value = '#' + value;
                }
                _fragment = value;
                _changed = true;
            }
        }

        public string Host
        {
            get
            {
                return _host;
            }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                _host = value;
                //probable ipv6 address - Note: this is only supported for cases where the authority is inet-based.
                if (_host.Contains(':'))
                {
                    //set brackets
                    if (_host[0] != '[')
                        _host = "[" + _host + "]";
                }
                _changed = true;
            }
        }

        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                _password = value;
                _changed = true;
            }
        }

        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                if ((value == null) || (value.Length == 0))
                {
                    value = "/";
                }
                _path = Uri.InternalEscapeString(value.Replace('\\', '/'));
                _changed = true;
            }
        }

        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                if (value < -1 || value > 0xFFFF)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _port = value;
                _changed = true;
            }
        }

        public string Query
        {
            get
            {
                return _query;
            }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                if (value.Length > 0 && value[0] != '?')
                {
                    value = '?' + value;
                }
                _query = value;
                _changed = true;
            }
        }

        public string Scheme
        {
            get
            {
                return _scheme;
            }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }

                int index = value.IndexOf(':');
                if (index != -1)
                {
                    value = value.Substring(0, index);
                }

                if (value.Length != 0)
                {
                    if (!Uri.CheckSchemeName(value))
                    {
                        throw new ArgumentException(SR.net_uri_BadScheme, nameof(value));
                    }
                    value = value.ToLowerInvariant();
                }
                _scheme = value;
                _changed = true;
            }
        }

        public Uri Uri
        {
            get
            {
                if (_changed)
                {
                    _uri = new Uri(ToString());
                    SetFieldsFromUri(_uri);
                    _changed = false;
                }
                return _uri;
            }
        }

        public string UserName
        {
            get
            {
                return _username;
            }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                _username = value;
                _changed = true;
            }
        }

        // methods

        public override bool Equals(object rparam)
        {
            if (rparam == null)
            {
                return false;
            }
            return Uri.Equals(rparam.ToString());
        }

        public override int GetHashCode()
        {
            return Uri.GetHashCode();
        }

        private void SetFieldsFromUri(Uri uri)
        {
            _fragment = uri.Fragment;
            _query = uri.Query;
            _host = uri.Host;
            _path = uri.AbsolutePath;
            _port = uri.Port;
            _scheme = uri.Scheme;
            _schemeDelimiter = uri.HasAuthority ? Uri.SchemeDelimiter : ":";

            string userInfo = uri.UserInfo;

            if (userInfo.Length > 0)
            {
                int index = userInfo.IndexOf(':');

                if (index != -1)
                {
                    _password = userInfo.Substring(index + 1);
                    _username = userInfo.Substring(0, index);
                }
                else
                {
                    _username = userInfo;
                }
            }
        }

        public override string ToString()
        {
            if (_username.Length == 0 && _password.Length > 0)
            {
                throw new UriFormatException(SR.net_uri_BadUserPassword);
            }

            if (_scheme.Length != 0)
            {
                UriParser syntax = UriParser.GetSyntax(_scheme);
                if (syntax != null)
                    _schemeDelimiter = syntax.InFact(UriSyntaxFlags.MustHaveAuthority) ||
                                        (_host.Length != 0 && syntax.NotAny(UriSyntaxFlags.MailToLikeUri) && syntax.InFact(UriSyntaxFlags.OptionalAuthority))
                            ? Uri.SchemeDelimiter
                            : ":";
                else
                    _schemeDelimiter = _host.Length != 0 ? Uri.SchemeDelimiter : ":";
            }

            string result = _scheme.Length != 0 ? (_scheme + _schemeDelimiter) : string.Empty;
            return result
                    + _username
                    + ((_password.Length > 0) ? (":" + _password) : string.Empty)
                    + ((_username.Length > 0) ? "@" : string.Empty)
                    + _host
                    + (((_port != -1) && (_host.Length > 0)) ? (":" + _port.ToString()) : string.Empty)
                    + (((_host.Length > 0) && (_path.Length != 0) && (_path[0] != '/')) ? "/" : string.Empty) + _path
                    + _query
                    + _fragment;
        }
    }
}
