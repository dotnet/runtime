// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Xml;
using System.Runtime.CompilerServices;

#if NET_NATIVE || MERGE_DCJS
namespace System.Runtime.Serialization.Json
{
    internal interface IXmlJsonReaderInitializer
    {
        void SetInput(byte[] buffer, int offset, int count, Encoding encoding, XmlDictionaryReaderQuotas quotas,
            OnXmlDictionaryReaderClose onClose);

        void SetInput(Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas,
            OnXmlDictionaryReaderClose onClose);
    }
}
#endif
