// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;
using System.Xml;

namespace System.Configuration
{
    // ErrorInfoXmlDocument - the default Xml Document doesn't track line numbers, and line
    // numbers are necessary to display source on config errors.
    // These classes wrap corresponding System.Xml types and also carry 
    // the necessary information for reporting filename / line numbers.
    // Note: these classes will go away if webdata ever decides to incorporate line numbers
    // into the default XML classes.  This class could also go away if webdata brings back
    // the UserData property to hang any info off of any node.
    internal sealed class ErrorInfoXmlDocument : XmlDocument, IConfigErrorInfo
    {
        private string _filename;
        private int _lineOffset;
        private XmlTextReader _reader;

        internal int LineNumber => ((IConfigErrorInfo)this).LineNumber;

        int IConfigErrorInfo.LineNumber
        {
            get
            {
                if (_reader == null) return 0;

                if (_lineOffset > 0) return _reader.LineNumber + _lineOffset - 1;

                return _reader.LineNumber;
            }
        }

        string IConfigErrorInfo.Filename => _filename;

        public override void Load(string filename)
        {
            _filename = filename;
            try
            {
                _reader = new XmlTextReader(filename) { XmlResolver = null };
                Load(_reader);
            }
            finally
            {
                if (_reader != null)
                {
                    _reader.Close();
                    _reader = null;
                }
            }
        }

        private void LoadFromConfigXmlReader(ConfigXmlReader reader)
        {
            IConfigErrorInfo err = reader;
            _filename = err.Filename;
            _lineOffset = err.LineNumber + 1;

            try
            {
                _reader = reader;
                Load(_reader);
            }
            finally
            {
                if (_reader != null)
                {
                    _reader.Close();
                    _reader = null;
                }
            }
        }

        internal static XmlNode CreateSectionXmlNode(ConfigXmlReader reader)
        {
            ErrorInfoXmlDocument doc = new ErrorInfoXmlDocument();
            doc.LoadFromConfigXmlReader(reader);
            XmlNode xmlNode = doc.DocumentElement;

            return xmlNode;
        }

        public override XmlAttribute CreateAttribute(string prefix, string localName, string namespaceUri)
        {
            return new ConfigXmlAttribute(_filename, LineNumber, prefix, localName, namespaceUri, this);
        }

        public override XmlElement CreateElement(string prefix, string localName, string namespaceUri)
        {
            return new ConfigXmlElement(_filename, LineNumber, prefix, localName, namespaceUri, this);
        }

        public override XmlText CreateTextNode(string text)
        {
            return new ConfigXmlText(_filename, LineNumber, text, this);
        }

        public override XmlCDataSection CreateCDataSection(string data)
        {
            return new ConfigXmlCDataSection(_filename, LineNumber, data, this);
        }

        public override XmlComment CreateComment(string data)
        {
            return new ConfigXmlComment(_filename, LineNumber, data, this);
        }

        public override XmlSignificantWhitespace CreateSignificantWhitespace(string data)
        {
            return new ConfigXmlSignificantWhitespace(_filename, LineNumber, data, this);
        }

        public override XmlWhitespace CreateWhitespace(string data)
        {
            return new ConfigXmlWhitespace(_filename, LineNumber, data, this);
        }
    }
}