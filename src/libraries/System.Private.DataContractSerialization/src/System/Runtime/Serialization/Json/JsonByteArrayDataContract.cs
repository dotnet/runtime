// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.DataContracts;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal sealed class JsonByteArrayDataContract : JsonDataContract
    {
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public JsonByteArrayDataContract(ByteArrayDataContract traditionalByteArrayDataContract)
            : base(traditionalByteArrayDataContract)
        {
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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
