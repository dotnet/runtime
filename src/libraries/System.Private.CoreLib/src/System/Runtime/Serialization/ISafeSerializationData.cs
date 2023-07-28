// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization
{
    // This type was intended to support Code Access Security (CAS) scenarios in .NET Framework.
    // Since CAS is no longer supported in .NET 5+, this interface has no further use.

    [Obsolete(Obsoletions.LegacyFormatterMessage, DiagnosticId = Obsoletions.LegacyFormatterDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    public interface ISafeSerializationData
    {
        void CompleteDeserialization(object deserialized);
    }
}
