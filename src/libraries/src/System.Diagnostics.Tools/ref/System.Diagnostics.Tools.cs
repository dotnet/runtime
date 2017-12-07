// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.CodeDom.Compiler
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767), Inherited = false, AllowMultiple = false)]
    public sealed partial class GeneratedCodeAttribute : System.Attribute
    {
        public GeneratedCodeAttribute(string tool, string version) { }
        public string Tool { get { throw null; } }
        public string Version { get { throw null; } }
    }
}
namespace System.Diagnostics.CodeAnalysis
{
    [System.AttributeUsageAttribute((System.AttributeTargets)749, Inherited=false, AllowMultiple=false)]
    public sealed partial class ExcludeFromCodeCoverageAttribute : System.Attribute {
        public ExcludeFromCodeCoverageAttribute() { }
    }    
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767), Inherited = false, AllowMultiple = true)]
    [System.Diagnostics.ConditionalAttribute("CODE_ANALYSIS")]
    public sealed partial class SuppressMessageAttribute : System.Attribute
    {
        public SuppressMessageAttribute(string category, string checkId) { }
        public string Category { get { throw null; } }
        public string CheckId { get { throw null; } }
        public string Justification { get { throw null; } set { } }
        public string MessageId { get { throw null; } set { } }
        public string Scope { get { throw null; } set { } }
        public string Target { get { throw null; } set { } }
    }
}
