// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Threading.Channels
{
    public partial class ChannelClosedException : System.InvalidOperationException
    {
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("Legacy formatter-based serialization (IMPL) is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0050", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected ChannelClosedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public abstract partial class ChannelReader<T>
    {
        public virtual System.Collections.Generic.IAsyncEnumerable<T> ReadAllAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
}
