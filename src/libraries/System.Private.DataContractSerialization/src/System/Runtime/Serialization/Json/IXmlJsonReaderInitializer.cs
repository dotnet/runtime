// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    public interface IXmlJsonReaderInitializer
    {
        void SetInput(byte[] buffer, int offset, int count, Encoding? encoding, XmlDictionaryReaderQuotas quotas,
            OnXmlDictionaryReaderClose? onClose);

        void SetInput(Stream stream, Encoding? encoding, XmlDictionaryReaderQuotas quotas,
            OnXmlDictionaryReaderClose? onClose);
    }
}
