// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization
{
    // This type exists for public surface compatibility only.
    [Obsolete(Obsoletions.LegacyFormatterMessage, DiagnosticId = Obsoletions.LegacyFormatterDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    public sealed class SafeSerializationEventArgs : EventArgs
    {
        private SafeSerializationEventArgs() { }

        public void AddSerializedState(ISafeSerializationData serializedState)
        {
        }

        public StreamingContext StreamingContext { get; }
    }
}
