// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Runtime.Serialization.Formatters.Binary
{
    public sealed partial class BinaryFormatter : IFormatter
    {
        [Obsolete(Obsoletions.BinaryFormatterMessage, DiagnosticId = Obsoletions.BinaryFormatterDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public object Deserialize(Stream serializationStream)
            => throw new PlatformNotSupportedException(SR.BinaryFormatter_SerializationDisallowed);

        [Obsolete(Obsoletions.BinaryFormatterMessage, DiagnosticId = Obsoletions.BinaryFormatterDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public void Serialize(Stream serializationStream, object graph)
            => throw new PlatformNotSupportedException(SR.BinaryFormatter_SerializationDisallowed);
    }
}
