// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security
{
    public sealed partial class SecurityElement
    {
        public SecurityElement(string tag) { }
        public SecurityElement(string tag, string text) { }
        public System.Collections.Hashtable Attributes { get; set; }
        public System.Collections.ArrayList Children { get; set; }
        public string Tag { get; set; }
        public string Text { get; set; }
        public void AddAttribute(string name, string value) { }
        public void AddChild(System.Security.SecurityElement child) { }
        public string Attribute(string name) { return default(string); }
        public System.Security.SecurityElement Copy() { return default(System.Security.SecurityElement); }
        public bool Equal(System.Security.SecurityElement other) { return default(bool); }
        public static string Escape(string str) { return default(string); }
        public static System.Security.SecurityElement FromString(string xml) { return default(System.Security.SecurityElement); }
        public static bool IsValidAttributeName(string name) { return default(bool); }
        public static bool IsValidAttributeValue(string value) { return default(bool); }
        public static bool IsValidTag(string tag) { return default(bool); }
        public static bool IsValidText(string text) { return default(bool); }
        public System.Security.SecurityElement SearchForChildByTag(string tag) { return default(System.Security.SecurityElement); }
        public string SearchForTextOfTag(string tag) { return default(string); }
        public override string ToString() => base.ToString();
    }
}
namespace System.Security.Principal
{
    public partial interface IIdentity
    {
        string AuthenticationType { get; }
        bool IsAuthenticated { get; }
        string Name { get; }
    }
    public partial interface IPrincipal
    {
        System.Security.Principal.IIdentity Identity { get; }
        bool IsInRole(string role);
    }
    public enum TokenImpersonationLevel
    {
        Anonymous = 1,
        Delegation = 4,
        Identification = 2,
        Impersonation = 3,
        None = 0,
    }
}
