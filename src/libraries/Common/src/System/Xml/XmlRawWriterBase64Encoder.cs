// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    internal partial class XmlRawWriterBase64Encoder : Base64Encoder
    {
        XmlRawWriter rawWriter;

        internal XmlRawWriterBase64Encoder(XmlRawWriter rawWriter)
        {
            this.rawWriter = rawWriter;
        }

        internal override void WriteChars(char[] chars, int index, int count)
        {
            rawWriter.WriteRaw(chars, index, count);
        }
    }
}
