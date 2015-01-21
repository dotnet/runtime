// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    internal partial class XmlTextWriterBase64Encoder : Base64Encoder
    {
        readonly XmlTextEncoder xmlTextEncoder;

        internal XmlTextWriterBase64Encoder(XmlTextEncoder xmlTextEncoder)
        {
            this.xmlTextEncoder = xmlTextEncoder;
        }

        internal override void WriteChars(char[] chars, int index, int count)
        {
            xmlTextEncoder.WriteRaw(chars, index, count);
        }
    }
}