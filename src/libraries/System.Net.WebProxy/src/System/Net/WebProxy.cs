// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;

namespace System.Net
{
    /// <summary>
    /// Contains HTTP proxy settings for the <see cref="T:System.Net.Http.HttpClient" /> class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="WebProxy"/> class contains the proxy settings that <see cref="T:System.Net.Http.HttpClient"/> instances use to determine whether a Web proxy is used to send requests.
    /// Global Web proxy settings can be specified in machine and application configuration files, and applications can use instances of the <see cref="WebProxy"/> class to customize Web proxy use.
    /// The <see cref="WebProxy"/> class is the base implementation of the <see cref="IWebProxy"/> interface.
    /// </para>
    /// <para>
    /// To obtain instances of the Web proxy class, you can use any of the following methods:
    /// </para>
    /// <list type="bullet">
    /// <item>The <see cref="WebProxy()"/> constructor.</item>
    /// <item>The <see cref="GetDefaultProxy"/> method.</item>
    /// </list>
    /// <para>
    /// These methods each supply a <see cref="WebProxy"/> instance that you can further customize; the difference between them is how the instance is initialized before it is returned to your application.
    /// The <see cref="WebProxy()"/> constructor returns an instance of the <see cref="WebProxy"/> class with the <see cref="Address"/> property set to <see langword="null"/>.
    /// When a request uses a <see cref="WebProxy"/> instance in this state, no proxy is used to send the request.
    /// </para>
    /// <para>
    /// The <see cref="WebProxy"/> class supports automatic detection and execution of proxy configuration scripts. This feature is also known as Web Proxy Auto-Discovery (WPAD).
    /// When using automatic proxy configuration, a configuration script, typically named Wpad.dat, must be located, downloaded, compiled, and run.
    /// If these operations are successful, the script returns the proxies that can be used for a request.
    /// </para>
    /// </remarks>
    public partial class WebProxy : IWebProxy, ISerializable
    {
        private ChangeTrackingArrayList? _bypassList;
        private Regex[]? _regexBypassList;

        /// <summary>
        /// Initializes an empty instance of the <see cref="WebProxy" /> class.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The parameterless constructor initializes an empty instance of the <see cref="WebProxy"/> class with the <see cref="Address"/> property set to <see langword="null"/>.
        /// </para>
        /// <para>
        /// When the <see cref="Address"/> property is <see langword="null"/>, the <see cref="IsBypassed"/> method returns <see langword="true"/> and the <see cref="GetProxy"/> method returns the destination address.
        /// </para>
        /// </remarks>
        public WebProxy() : this((Uri?)null, false, null, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebProxy" /> class from the specified <see cref="Uri" /> instance.
        /// </summary>
        /// <param name="Address">The address of the proxy server.</param>
        public WebProxy(Uri? Address) : this(Address, false, null, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebProxy" /> class with the <see cref="Uri" /> instance and bypass setting.
        /// </summary>
        /// <param name="Address">A <see cref="Uri" /> instance that contains the address of the proxy server.</param>
        /// <param name="BypassOnLocal"><see langword="true" /> to bypass the proxy for local addresses; otherwise, <see langword="false" />.</param>
        public WebProxy(Uri? Address, bool BypassOnLocal) : this(Address, BypassOnLocal, null, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebProxy" /> class with the specified <see cref="Uri" /> instance, bypass setting, and list of URIs to bypass.
        /// </summary>
        /// <param name="Address">A <see cref="Uri" /> instance that contains the address of the proxy server.</param>
        /// <param name="BypassOnLocal"><see langword="true" /> to bypass the proxy for local addresses; otherwise, <see langword="false" />.</param>
        /// <param name="BypassList">An array of regular expression strings that contains the URIs of the servers to bypass.</param>
        public WebProxy(Uri? Address, bool BypassOnLocal, [StringSyntax(StringSyntaxAttribute.Regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)] string[]? BypassList) : this(Address, BypassOnLocal, BypassList, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebProxy" /> class with the specified <see cref="Uri" /> instance, bypass setting, list of URIs to bypass, and credentials.
        /// </summary>
        /// <param name="Address">A <see cref="Uri" /> instance that contains the address of the proxy server.</param>
        /// <param name="BypassOnLocal"><see langword="true" /> to bypass the proxy for local addresses; otherwise, <see langword="false" />.</param>
        /// <param name="BypassList">An array of regular expression strings that contains the URIs of the servers to bypass.</param>
        /// <param name="Credentials">An <see cref="ICredentials" /> instance to submit to the proxy server for authentication.</param>
        public WebProxy(Uri? Address, bool BypassOnLocal, [StringSyntax(StringSyntaxAttribute.Regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)] string[]? BypassList, ICredentials? Credentials)
        {
            this.Address = Address;
            this.Credentials = Credentials;
            this.BypassProxyOnLocal = BypassOnLocal;
            if (BypassList != null)
            {
                _bypassList = new ChangeTrackingArrayList(BypassList);
                UpdateRegexList(); // prompt creation of the Regex instances so that any exceptions are propagated
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebProxy" /> class with the specified host and port number.
        /// </summary>
        /// <param name="Host">The name of the proxy host.</param>
        /// <param name="Port">The port number on <paramref name="Host" /> to use.</param>
        /// <exception cref="UriFormatException">The URI formed by combining <paramref name="Host" /> and <paramref name="Port" /> is not a valid URI.</exception>
        /// <remarks>
        /// The <see cref="WebProxy"/> instance is initialized with the <see cref="Address"/> property set to a <see cref="Uri"/> instance of the form <c>http://</c><paramref name="Host"/><c>:</c><paramref name="Port"/>.
        /// </remarks>
        public WebProxy(string Host, int Port)
            : this(CreateProxyUri(Host, Port), false, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebProxy" /> class with the specified URI.
        /// </summary>
        /// <param name="Address">The URI of the proxy server.</param>
        /// <exception cref="UriFormatException"><paramref name="Address" /> is an invalid URI.</exception>
        public WebProxy(string? Address)
            : this(CreateProxyUri(Address), false, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebProxy" /> class with the specified URI and bypass setting.
        /// </summary>
        /// <param name="Address">The URI of the proxy server.</param>
        /// <param name="BypassOnLocal"><see langword="true" /> to bypass the proxy for local addresses; otherwise, <see langword="false" />.</param>
        /// <exception cref="UriFormatException"><paramref name="Address" /> is an invalid URI.</exception>
        public WebProxy(string? Address, bool BypassOnLocal)
            : this(CreateProxyUri(Address), BypassOnLocal, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebProxy" /> class with the specified URI, bypass setting, and list of URIs to bypass.
        /// </summary>
        /// <param name="Address">The URI of the proxy server.</param>
        /// <param name="BypassOnLocal"><see langword="true" /> to bypass the proxy for local addresses; otherwise, <see langword="false" />.</param>
        /// <param name="BypassList">An array of regular expression strings that contain the URIs of the servers to bypass.</param>
        /// <exception cref="UriFormatException"><paramref name="Address" /> is an invalid URI.</exception>
        public WebProxy(string? Address, bool BypassOnLocal, [StringSyntax(StringSyntaxAttribute.Regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)] string[]? BypassList)
            : this(CreateProxyUri(Address), BypassOnLocal, BypassList, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebProxy" /> class with the specified URI, bypass setting, list of URIs to bypass, and credentials.
        /// </summary>
        /// <param name="Address">The URI of the proxy server.</param>
        /// <param name="BypassOnLocal"><see langword="true" /> to bypass the proxy for local addresses; otherwise, <see langword="false" />.</param>
        /// <param name="BypassList">An array of regular expression strings that contains the URIs of the servers to bypass.</param>
        /// <param name="Credentials">An <see cref="ICredentials" /> instance to submit to the proxy server for authentication.</param>
        /// <exception cref="UriFormatException"><paramref name="Address" /> is an invalid URI.</exception>
        public WebProxy(string? Address, bool BypassOnLocal, [StringSyntax(StringSyntaxAttribute.Regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)] string[]? BypassList, ICredentials? Credentials)
            : this(CreateProxyUri(Address), BypassOnLocal, BypassList, Credentials)
        {
        }

        /// <summary>
        /// Gets or sets the address of the proxy server.
        /// </summary>
        /// <value>
        /// A <see cref="Uri"/> instance that contains the address of the proxy server.
        /// </value>
        public Uri? Address { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether to bypass the proxy server for local addresses.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to bypass the proxy server for local addresses; otherwise, <see langword="false"/>. The default value is <see langword="false"/>.
        /// </value>
        public bool BypassProxyOnLocal { get; set; }

        /// <summary>
        /// Gets or sets an array of addresses that do not use the proxy server.
        /// </summary>
        /// <value>
        /// An array of regular expression strings that contains the URIs of servers that should not use the proxy server when accessed.
        /// </value>
        [AllowNull]
        public string[] BypassList
        {
            get
            {
                if (_bypassList == null)
                {
                    return Array.Empty<string>();
                }

                var bypassList = new string[_bypassList.Count];
                _bypassList.CopyTo(bypassList);
                return bypassList;
            }
            set
            {
                _bypassList = value != null ? new ChangeTrackingArrayList(value) : null;
                UpdateRegexList(); // prompt creation of the Regex instances so that any exceptions are propagated
            }
        }

        /// <summary>
        /// Gets or sets an <see cref="ArrayList"/> of addresses that do not use the proxy server.
        /// </summary>
        /// <value>
        /// An <see cref="ArrayList"/> that contains a list of regular expressions that represents URIs that do not use the proxy server when accessed.
        /// </value>
        public ArrayList BypassArrayList => _bypassList ??= new ChangeTrackingArrayList();

        /// <summary>
        /// Gets or sets the credentials to submit to the proxy server for authentication.
        /// </summary>
        /// <value>
        /// An <see cref="ICredentials"/> instance that contains the credentials to submit to the proxy server for authentication.
        /// </value>
        public ICredentials? Credentials { get; set; }

        /// <summary>
        /// Gets or sets a value that controls whether the <see cref="CredentialCache.DefaultCredentials"/> are sent with requests.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the default credentials are used; otherwise, <see langword="false"/>. The default value is <see langword="false"/>.
        /// </value>
        public bool UseDefaultCredentials
        {
            get => Credentials == CredentialCache.DefaultCredentials;
            set => Credentials = value ? CredentialCache.DefaultCredentials : null;
        }

        /// <summary>
        /// Returns the URI of a proxy.
        /// </summary>
        /// <param name="destination">The <see cref="Uri"/> instance of the requested Internet resource.</param>
        /// <returns>
        /// The <see cref="Uri"/> instance of the Internet resource, if the resource is on the bypass list; otherwise, the <see cref="Uri"/> instance of the proxy.
        /// </returns>
        /// <exception cref="ArgumentNullException">The <paramref name="destination"/> parameter is <see langword="null"/>.</exception>
        public Uri? GetProxy(Uri destination)
        {
            ArgumentNullException.ThrowIfNull(destination);

            return IsBypassed(destination) ? destination : Address;
        }

        private static Uri? CreateProxyUri(string? address, int? port = null)
        {
            if (address is null)
            {
                return null;
            }

            if (!address.Contains("://", StringComparison.Ordinal))
            {
                address = "http://" + address;
            }

            var proxyUri = new Uri(address);

            if (port.HasValue && proxyUri.IsAbsoluteUri)
            {
                proxyUri = new UriBuilder(proxyUri) { Port = port.Value }.Uri;
            }

            return proxyUri;
        }

        private void UpdateRegexList()
        {
            if (_bypassList is ChangeTrackingArrayList bypassList)
            {
                Regex[]? regexBypassList = null;
                if (bypassList.Count > 0)
                {
                    regexBypassList = new Regex[bypassList.Count];
                    for (int i = 0; i < regexBypassList.Length; i++)
                    {
                        regexBypassList[i] = new Regex((string)bypassList[i]!, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    }
                }

                _regexBypassList = regexBypassList;
                bypassList.IsChanged = false;
            }
            else
            {
                _regexBypassList = null;
            }
        }

        private bool IsMatchInBypassList(Uri input)
        {
            // Update our list of Regex instances if the ArrayList has changed.
            if (_bypassList is ChangeTrackingArrayList bypassList && bypassList.IsChanged)
            {
                try
                {
                    UpdateRegexList();
                }
                catch
                {
                    _regexBypassList = null;
                }
            }

            if (_regexBypassList is Regex[] regexBypassList)
            {
                bool isDefaultPort = input.IsDefaultPort;
                int lengthRequired = checked(input.Scheme.Length + 3 + input.Host.Length +
                    // 1 for ':' and 5 for max formatted length of a port (16 bit value)
                    (isDefaultPort ? 0 : 1 + 5));

                int charsWritten;
                Span<char> url = (uint)lengthRequired <= 256 ? stackalloc char[256] : new char[lengthRequired];
                bool formatted = isDefaultPort ?
                    url.TryWrite($"{input.Scheme}://{input.Host}", out charsWritten) :
                    url.TryWrite($"{input.Scheme}://{input.Host}:{(uint)input.Port}", out charsWritten);
                Debug.Assert(formatted);
                url = url.Slice(0, charsWritten);

                foreach (Regex r in regexBypassList)
                {
                    if (r.IsMatch(url))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Indicates whether to use the proxy server for the specified host.
        /// </summary>
        /// <param name="host">The <see cref="Uri"/> instance of the host to check for proxy use.</param>
        /// <returns>
        /// <see langword="true"/> if the proxy server should not be used for <paramref name="host"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">The <paramref name="host"/> parameter is <see langword="null"/>.</exception>
        public bool IsBypassed(Uri host)
        {
            ArgumentNullException.ThrowIfNull(host);

            return
                Address == null ||
                (BypassProxyOnLocal && IsLocal(host)) ||
                IsMatchInBypassList(host);
        }

        /// <summary>
        /// Initializes an instance of the <see cref="WebProxy" /> class using previously serialized content.
        /// </summary>
        /// <param name="serializationInfo">The serialization data.</param>
        /// <param name="streamingContext">The context for the serialized data.</param>
        /// <exception cref="PlatformNotSupportedException">This method is not supported and will always throw <see cref="PlatformNotSupportedException"/>.</exception>
        /// <remarks>
        /// This method is called by the system to deserialize a <see cref="WebProxy"/> instance; applications do not call it.
        /// </remarks>
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected WebProxy(SerializationInfo serializationInfo, StreamingContext streamingContext) =>
            throw new PlatformNotSupportedException();

        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext) =>
            throw new PlatformNotSupportedException();

        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> with the data that is needed to serialize the target object.
        /// </summary>
        /// <param name="serializationInfo">The <see cref="SerializationInfo"/> to populate with data.</param>
        /// <param name="streamingContext">A <see cref="StreamingContext"/> that specifies the destination for this serialization.</param>
        /// <exception cref="PlatformNotSupportedException">This method is not supported and will always throw <see cref="PlatformNotSupportedException"/>.</exception>
        protected virtual void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext) =>
            throw new PlatformNotSupportedException();

        /// <summary>
        /// Returns the proxy information configured by the system.
        /// </summary>
        /// <returns>
        /// A <see cref="WebProxy"/> instance that contains the nondynamic proxy settings from Internet options.
        /// </returns>
        /// <exception cref="PlatformNotSupportedException">This method is not supported on .NET Core and will always throw <see cref="PlatformNotSupportedException"/>.</exception>
        [Obsolete("WebProxy.GetDefaultProxy has been deprecated. Use the proxy selected for you by default.")]
        public static WebProxy GetDefaultProxy() =>
            // The .NET Framework here returns a proxy that fetches IE settings and
            // executes JavaScript to determine the correct proxy.
            throw new PlatformNotSupportedException();

        private sealed class ChangeTrackingArrayList : ArrayList
        {
            public ChangeTrackingArrayList() { }

            public ChangeTrackingArrayList(ICollection c) : base(c) { }

            // While this type isn't intended to be mutated concurrently with reads, non-concurrent updates
            // to the list might result in lazy initialization, and it's possible concurrent HTTP requests could race
            // to trigger that initialization.
            public volatile bool IsChanged;

            // Override the methods that can add, remove, or change the regexes in the bypass list.
            // Methods that only read (like CopyTo, BinarySearch, etc.) and methods that reorder
            // the collection but that don't change the overall list of regexes (e.g. Sort) do not
            // need to be overridden.

            public override object? this[int index]
            {
                get => base[index];
                set
                {
                    IsChanged = true;
                    base[index] = value;
                }
            }

            public override int Add(object? value)
            {
                IsChanged = true;
                return base.Add(value);
            }

            public override void AddRange(ICollection c)
            {
                IsChanged = true;
                base.AddRange(c);
            }

            public override void Insert(int index, object? value)
            {
                IsChanged = true;
                base.Insert(index, value);
            }

            public override void InsertRange(int index, ICollection c)
            {
                IsChanged = true;
                base.InsertRange(index, c);
            }

            public override void SetRange(int index, ICollection c)
            {
                IsChanged = true;
                base.SetRange(index, c);
            }

            public override void Remove(object? obj)
            {
                IsChanged = true;
                base.Remove(obj);
            }

            public override void RemoveAt(int index)
            {
                IsChanged = true;
                base.RemoveAt(index);
            }

            public override void RemoveRange(int index, int count)
            {
                IsChanged = true;
                base.RemoveRange(index, count);
            }

            public override void Clear()
            {
                IsChanged = true;
                base.Clear();
            }
        }
    }
}
