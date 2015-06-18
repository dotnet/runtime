// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal class JsonQNameDataContract : JsonDataContract
    {
        public JsonQNameDataContract(QNameDataContract traditionalQNameDataContract)
            : base(traditionalQNameDataContract)
        {
        }

        public override object ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
        {
            if (context == null)
            {
                return TryReadNullAtTopLevel(jsonReader) ? null : jsonReader.ReadElementContentAsQName();
            }
            else
            {
                return HandleReadValue(jsonReader.ReadElementContentAsQName(), context);
            }
        }
    }
}
