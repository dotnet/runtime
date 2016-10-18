// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.s

namespace System.Security
{
    [Serializable]
    public sealed partial class SecurityElement
    {
        public SecurityElement(string tag) { }
        public SecurityElement(string tag, string text) { }
        public System.Collections.Hashtable Attributes { get; set; }
        public System.Collections.ArrayList Children { get; set; }
        public string Tag { get; set; }
        public string Text { get; set; }
        public void AddAttribute(string name, string value) { }
        public void AddChild(SecurityElement child) { }
        public string Attribute(string name) { return default(string); }
        public SecurityElement Copy() { return this; }
        public bool Equal(SecurityElement other) { return false; }
        public static string Escape(string str) { return default(string); }
        public static SecurityElement FromString(string xml) { return default(SecurityElement); }
        public static bool IsValidAttributeName(string name) { return false; }
        public static bool IsValidAttributeValue(string value) { return false; }
        public static bool IsValidTag(string tag) { return false; }
        public static bool IsValidText(string text) { return false; }
        public SecurityElement SearchForChildByTag(string tag) { return default(SecurityElement); }
        public string SearchForTextOfTag(string tag) { return default(string); }
        public override string ToString() => base.ToString();
    }
}
