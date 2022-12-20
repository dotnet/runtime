// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data
{
    public enum SerializationFormat
    {
        Xml = 0,

        [Obsolete(
            Obsoletions.SystemDataSerializationFormatBinaryMessage,
            DiagnosticId = Obsoletions.SystemDataSerializationFormatBinaryDiagId)]
        Binary = 1
    }
}
