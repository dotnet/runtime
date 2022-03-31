// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace JavaScript.MarshalerGenerator
{
    internal static class Constants
    {
        public const int JavaScriptMarshalerArgSize = 16;
        public const string JavaScriptMarshal = "System.Runtime.InteropServices.JavaScript.JavaScriptMarshal";
        public const string JavaScriptPublic = "System.Runtime.InteropServices.JavaScript";

        public const string JavaScriptMarshalGlobal = "global::" + JavaScriptMarshal;
        public const string JavaScriptMarshalerSignatureGlobal = "global::System.Runtime.InteropServices.JavaScript.JavaScriptMarshalerSignature";
        public const string ModuleInitializerAttributeGlobal = "global::System.Runtime.CompilerServices.ModuleInitializerAttribute";
        public const string DynamicDependencyAttributeGlobal = "global::System.Diagnostics.CodeAnalysis.DynamicDependencyAttribute";
    }
}
