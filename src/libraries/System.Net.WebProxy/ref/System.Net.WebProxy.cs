// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net
{
    public partial interface IWebProxyScript
    {
        void Close();
        bool Load(System.Uri scriptLocation, string script, System.Type helperType);
        string Run(string url, string host);
    }
    public partial class WebProxy : System.Net.IWebProxy, System.Runtime.Serialization.ISerializable
    {
        public WebProxy() { }
        protected WebProxy(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public WebProxy(string Address) { }
        public WebProxy(string Address, bool BypassOnLocal) { }
        public WebProxy(string Address, bool BypassOnLocal, string[] BypassList) { }
        public WebProxy(string Address, bool BypassOnLocal, string[] BypassList, System.Net.ICredentials Credentials) { }
        public WebProxy(string Host, int Port) { }
        public WebProxy(System.Uri Address) { }
        public WebProxy(System.Uri Address, bool BypassOnLocal) { }
        public WebProxy(System.Uri Address, bool BypassOnLocal, string[] BypassList) { }
        public WebProxy(System.Uri Address, bool BypassOnLocal, string[] BypassList, System.Net.ICredentials Credentials) { }
        public System.Uri Address { get { throw null; } set { } }
        public System.Collections.ArrayList BypassArrayList { get { throw null; } }
        public string[] BypassList { get { throw null; } set { } }
        public bool BypassProxyOnLocal { get { throw null; } set { } }
        public System.Net.ICredentials Credentials { get { throw null; } set { } }
        public bool UseDefaultCredentials { get { throw null; } set { } }
        [System.ObsoleteAttribute("This method has been deprecated. Please use the proxy selected for you by default. https://go.microsoft.com/fwlink/?linkid=14202")]
        public static System.Net.WebProxy GetDefaultProxy() { throw null; }
        protected virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public System.Uri GetProxy(System.Uri destination) { throw null; }
        public bool IsBypassed(System.Uri host) { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
}
