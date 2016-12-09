// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;
using System.Xml;

namespace System.Configuration
{
    internal sealed class ConfigXmlSignificantWhitespace : XmlSignificantWhitespace, IConfigErrorInfo
    {
        private string _filename;
        private int _line;

        public ConfigXmlSignificantWhitespace(string filename, int line, string strData, XmlDocument doc)
            : base(strData, doc)
        {
            _line = line;
            _filename = filename;
        }

        int IConfigErrorInfo.LineNumber => _line;

        string IConfigErrorInfo.Filename => _filename;

        public override XmlNode CloneNode(bool deep)
        {
            XmlNode cloneNode = base.CloneNode(deep);
            ConfigXmlSignificantWhitespace clone = cloneNode as ConfigXmlSignificantWhitespace;
            if (clone != null)
            {
                clone._line = _line;
                clone._filename = _filename;
            }
            return cloneNode;
        }
    }
}