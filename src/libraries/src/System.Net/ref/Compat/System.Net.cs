// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.Cookie))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.CookieCollection))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.CookieContainer))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.CookieException))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.HttpRequestHeader))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.HttpStatusCode))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.WebRequest))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.HttpWebRequest))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.WebResponse))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.HttpWebResponse))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.ICredentials))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.IWebRequestCreate))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.NetworkCredential))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.ProtocolViolationException))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.WebException))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.WebExceptionStatus))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.WebHeaderCollection))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.NetworkInformation.NetworkAddressChangedEventHandler))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.NetworkInformation.NetworkChange))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.NetworkInformation.NetworkInterface))]

namespace System.Net
{
    public partial class IPEndPointCollection : System.Collections.ObjectModel.Collection<System.Net.IPEndPoint>
    {
        public IPEndPointCollection() { }
        protected override void InsertItem(int index, System.Net.IPEndPoint item) { }
        protected override void SetItem(int index, System.Net.IPEndPoint item) { }
    }
}
