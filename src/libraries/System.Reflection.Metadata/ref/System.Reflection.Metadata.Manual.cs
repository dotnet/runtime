// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Reflection.Metadata
{
    public readonly partial struct AssemblyDefinition
    {
        public System.Reflection.AssemblyName GetAssemblyName() { throw null; }
    }
    public readonly partial struct AssemblyReference
    {
        public System.Reflection.AssemblyName GetAssemblyName() { throw null; }
    }
    public partial class ImageFormatLimitationException : System.Exception
    {
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("Legacy formatter-based serialization (IMPL) is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0050", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected ImageFormatLimitationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
}
