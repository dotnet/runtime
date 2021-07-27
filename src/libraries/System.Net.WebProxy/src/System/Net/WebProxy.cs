// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace System.Net
{
    public partial class WebProxy : IWebProxy, ISerializable
    {
        private ArrayList? _bypassList;
        private Regex[]? _regexBypassList;

        public WebProxy() : this((Uri?)null, false, null, null) { }

        public WebProxy(Uri? Address) : this(Address, false, null, null) { }

        public WebProxy(Uri? Address, bool BypassOnLocal) : this(Address, BypassOnLocal, null, null) { }

        public WebProxy(Uri? Address, bool BypassOnLocal, string[]? BypassList) : this(Address, BypassOnLocal, BypassList, null) { }

        public WebProxy(Uri? Address, bool BypassOnLocal, string[]? BypassList, ICredentials? Credentials)
        {
            this.Address = Address;
            this.Credentials = Credentials;
            this.BypassProxyOnLocal = BypassOnLocal;
            if (BypassList != null)
            {
                _bypassList = new ArrayList(BypassList);
                UpdateRegexList(true);
            }
        }

        public WebProxy(string Host, int Port)
            : this(new Uri(string.Create(CultureInfo.InvariantCulture, $"http://{Host}:{Port}")), false, null, null)
        {
        }

        public WebProxy(string? Address)
            : this(CreateProxyUri(Address), false, null, null)
        {
        }

        public WebProxy(string? Address, bool BypassOnLocal)
            : this(CreateProxyUri(Address), BypassOnLocal, null, null)
        {
        }

        public WebProxy(string? Address, bool BypassOnLocal, string[]? BypassList)
            : this(CreateProxyUri(Address), BypassOnLocal, BypassList, null)
        {
        }

        public WebProxy(string? Address, bool BypassOnLocal, string[]? BypassList, ICredentials? Credentials)
            : this(CreateProxyUri(Address), BypassOnLocal, BypassList, Credentials)
        {
        }

        public Uri? Address { get; set; }

        public bool BypassProxyOnLocal { get; set; }

        [AllowNull]
        public string[] BypassList
        {
            get
            {
                if (_bypassList == null)
                    return Array.Empty<string>();

                var bypassList = new string[_bypassList.Count];
                _bypassList.CopyTo(bypassList);
                return bypassList;
            }
            set
            {
                _bypassList = value != null ? new ArrayList(value) : null;
                UpdateRegexList(true);
            }
        }

        public ArrayList BypassArrayList => _bypassList ?? (_bypassList = new ArrayList());

        public ICredentials? Credentials { get; set; }

        public bool UseDefaultCredentials
        {
            get => Credentials == CredentialCache.DefaultCredentials;
            set => Credentials = value ? CredentialCache.DefaultCredentials : null;
        }

        public Uri? GetProxy(Uri destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            return IsBypassed(destination) ? destination : Address;
        }

        private static Uri? CreateProxyUri(string? address) =>
            address == null ? null :
            !address.Contains("://") ? new Uri("http://" + address) :
            new Uri(address);

        private void UpdateRegexList(bool canThrow)
        {
            Regex[]? regexBypassList = null;
            ArrayList? bypassList = _bypassList;
            try
            {
                if (bypassList != null && bypassList.Count > 0)
                {
                    regexBypassList = new Regex[bypassList.Count];
                    for (int i = 0; i < bypassList.Count; i++)
                    {
                        regexBypassList[i] = new Regex((string)bypassList[i]!, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    }
                }
            }
            catch
            {
                if (!canThrow)
                {
                    _regexBypassList = null;
                    return;
                }
                throw;
            }

            // Update field here, as it could throw earlier in the loop
            _regexBypassList = regexBypassList;
        }

        private bool IsMatchInBypassList(Uri input)
        {
            UpdateRegexList(canThrow: false);

            if (_regexBypassList != null)
            {
                Span<char> stackBuffer = stackalloc char[128];
                string matchUriString = input.IsDefaultPort ?
                    string.Create(null, stackBuffer, $"{input.Scheme}://{input.Host}") :
                    string.Create(null, stackBuffer, $"{input.Scheme}://{input.Host}:{(uint)input.Port}");

                foreach (Regex r in _regexBypassList)
                {
                    if (r.IsMatch(matchUriString))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsBypassed(Uri host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            return
                Address == null ||
                (BypassProxyOnLocal && IsLocal(host)) ||
                IsMatchInBypassList(host);
        }

        protected WebProxy(SerializationInfo serializationInfo, StreamingContext streamingContext) =>
            throw new PlatformNotSupportedException();

        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext) =>
            throw new PlatformNotSupportedException();

        protected virtual void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext) =>
            throw new PlatformNotSupportedException();

        [Obsolete("This method has been deprecated. Please use the proxy selected for you by default. https://go.microsoft.com/fwlink/?linkid=14202")]
        public static WebProxy GetDefaultProxy() =>
            // The .NET Framework here returns a proxy that fetches IE settings and
            // executes JavaScript to determine the correct proxy.
            throw new PlatformNotSupportedException();
    }
}
