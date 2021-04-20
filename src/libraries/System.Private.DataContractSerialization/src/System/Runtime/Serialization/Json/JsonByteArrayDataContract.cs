// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Xml;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization.Json
{
    internal sealed class JsonByteArrayDataContract : JsonDataContract
    {
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public JsonByteArrayDataContract(ByteArrayDataContract traditionalByteArrayDataContract)
            : base(traditionalByteArrayDataContract)
        {
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson? context)
        {
            if (context == null)
            {
                return TryReadNullAtTopLevel(jsonReader) ? null : jsonReader.ReadElementContentAsBase64();
            }
            else
            {
                return HandleReadValue(jsonReader.ReadElementContentAsBase64(), context);
            }
        }
    }
}
