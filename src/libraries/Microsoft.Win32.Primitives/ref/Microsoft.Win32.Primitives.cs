// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.ComponentModel
{
    public partial class Win32Exception : System.Runtime.InteropServices.ExternalException
    {
        public Win32Exception() { }
        public Win32Exception(int error) { }
        public Win32Exception(int error, string? message) { }
        [System.ObsoleteAttribute("Legacy formatter-based serialization (IMPL) is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0050", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected Win32Exception(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public Win32Exception(string? message) { }
        public Win32Exception(string? message, System.Exception? innerException) { }
        public int NativeErrorCode { get { throw null; } }
        [System.ObsoleteAttribute("Legacy formatter-based serialization (IMPL) is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0050", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
}
