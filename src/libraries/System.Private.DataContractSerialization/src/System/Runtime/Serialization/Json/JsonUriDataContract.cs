// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal class JsonUriDataContract : JsonDataContract
    {
        public JsonUriDataContract(UriDataContract traditionalUriDataContract)
            : base(traditionalUriDataContract)
        {
        }

        public override object ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
        {
            if (context == null)
            {
                return TryReadNullAtTopLevel(jsonReader) ? null : jsonReader.ReadElementContentAsUri();
            }
            else
            {
                return HandleReadValue(jsonReader.ReadElementContentAsUri(), context);
            }
        }
    }
}
