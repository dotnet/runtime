// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Xml;

#if NET_NATIVE
namespace System.Runtime.Serialization.Json
{
    internal class JsonStringDataContract : JsonDataContract
    {
        public JsonStringDataContract(StringDataContract traditionalStringDataContract)
            : base(traditionalStringDataContract)
        {
        }

        public override object ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
        {
            if (context == null)
            {
                return TryReadNullAtTopLevel(jsonReader) ? null : jsonReader.ReadElementContentAsString();
            }
            else
            {
                return HandleReadValue(jsonReader.ReadElementContentAsString(), context);
            }
        }
    }
}
#endif
