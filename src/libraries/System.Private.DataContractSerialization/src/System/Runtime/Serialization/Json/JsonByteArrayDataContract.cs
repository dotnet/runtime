// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal class JsonByteArrayDataContract : JsonDataContract
    {
        public JsonByteArrayDataContract(ByteArrayDataContract traditionalByteArrayDataContract)
            : base(traditionalByteArrayDataContract)
        {
        }

        public override object ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
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
