// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // https://www.secg.org/sec1-v2.pdf, C.1
    //
    // FieldID { FIELD-ID:IOSet } ::= SEQUENCE { -- Finite field
    //   fieldType FIELD-ID.&id({IOSet}),
    //   parameters FIELD-ID.&Type({IOSet}{@fieldType})
    // }
    [StructLayout(LayoutKind.Sequential)]
    internal struct FieldID
    {
        [ObjectIdentifier]
        public string FieldType;

        [AnyValue]
        public ReadOnlyMemory<byte> Parameters;
    }
}
