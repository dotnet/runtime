// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Xml.Tests
{
    internal static class NodeReaderTestHelper
    {
        internal static XmlNodeReader CreateNodeReader(string xml)
        {            
            var document = new XmlDocument();
            document.LoadXml(xml);
            var nodeReader = new XmlNodeReader(document);
            return nodeReader;
        }

        internal static byte[] HexStringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}
