// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    internal partial class XmlRawWriterBase64Encoder : Base64Encoder
    {
        private XmlRawWriter _rawWriter;

        internal XmlRawWriterBase64Encoder(XmlRawWriter rawWriter)
        {
            _rawWriter = rawWriter;
        }

        internal override void WriteChars(char[] chars, int index, int count)
        {
            _rawWriter.WriteRaw(chars, index, count);
        }
    }
}