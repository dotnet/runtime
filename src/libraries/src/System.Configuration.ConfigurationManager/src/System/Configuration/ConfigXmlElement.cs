// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;
using System.Xml;

namespace System.Configuration
{
    internal sealed class ConfigXmlElement : XmlElement, IConfigErrorInfo
    {
        private string _filename;
        private int _line;

        public ConfigXmlElement(string filename, int line, string prefix, string localName, string namespaceUri,
            XmlDocument doc)
            : base(prefix, localName, namespaceUri, doc)
        {
            _line = line;
            _filename = filename;
        }

        int IConfigErrorInfo.LineNumber => _line;

        string IConfigErrorInfo.Filename => _filename;

        public override XmlNode CloneNode(bool deep)
        {
            XmlNode cloneNode = base.CloneNode(deep);
            ConfigXmlElement clone = cloneNode as ConfigXmlElement;
            if (clone != null)
            {
                clone._line = _line;
                clone._filename = _filename;
            }
            return cloneNode;
        }
    }
}