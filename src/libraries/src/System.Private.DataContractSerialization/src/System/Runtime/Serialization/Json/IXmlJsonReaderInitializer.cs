// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Xml;
using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization.Json
{
    public interface IXmlJsonReaderInitializer
    {
        void SetInput(byte[] buffer, int offset, int count, Encoding encoding, XmlDictionaryReaderQuotas quotas,
            OnXmlDictionaryReaderClose onClose);

        void SetInput(Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas,
            OnXmlDictionaryReaderClose onClose);
    }
}
