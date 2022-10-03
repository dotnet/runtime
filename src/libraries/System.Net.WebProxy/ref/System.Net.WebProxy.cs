// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
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
        public WebProxy(string? Address) { }
        public WebProxy(string? Address, bool BypassOnLocal) { }
        public WebProxy(string? Address, bool BypassOnLocal, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant)] string[]? BypassList) { }
        public WebProxy(string? Address, bool BypassOnLocal, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant)] string[]? BypassList, System.Net.ICredentials? Credentials) { }
        public WebProxy(string Host, int Port) { }
        public WebProxy(System.Uri? Address) { }
        public WebProxy(System.Uri? Address, bool BypassOnLocal) { }
        public WebProxy(System.Uri? Address, bool BypassOnLocal, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant)] string[]? BypassList) { }
        public WebProxy(System.Uri? Address, bool BypassOnLocal, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant)] string[]? BypassList, System.Net.ICredentials? Credentials) { }
        public System.Uri? Address { get { throw null; } set { } }
        public System.Collections.ArrayList BypassArrayList { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string[] BypassList { get { throw null; } set { } }
        public bool BypassProxyOnLocal { get { throw null; } set { } }
        public System.Net.ICredentials? Credentials { get { throw null; } set { } }
        public bool UseDefaultCredentials { get { throw null; } set { } }
        [System.ObsoleteAttribute("WebProxy.GetDefaultProxy has been deprecated. Use the proxy selected for you by default.")]
        public static System.Net.WebProxy GetDefaultProxy() { throw null; }
        protected virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public System.Uri? GetProxy(System.Uri destination) { throw null; }
        public bool IsBypassed(System.Uri host) { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
}
