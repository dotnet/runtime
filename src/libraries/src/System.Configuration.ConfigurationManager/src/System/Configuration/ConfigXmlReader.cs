// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace System.Configuration
{
    internal sealed class ConfigXmlReader : XmlTextReader, IConfigErrorInfo
    {
        private readonly string _filename;

        // Used in a decrypted configuration section to locate
        // the line where the ecnrypted section begins.
        private readonly bool _lineNumberIsConstant;
        private readonly int _lineOffset;

        internal ConfigXmlReader(string rawXml, string filename, int lineOffset) :
            this(rawXml, filename, lineOffset, false)
        { }

        internal ConfigXmlReader(string rawXml, string filename, int lineOffset, bool lineNumberIsConstant) :
            base(new StringReader(rawXml))
        {
            RawXml = rawXml;
            _filename = filename;
            _lineOffset = lineOffset;
            _lineNumberIsConstant = lineNumberIsConstant;

            Debug.Assert(!_lineNumberIsConstant || (_lineOffset > 0),
                "!_lineNumberIsConstant || _lineOffset > 0");
        }

        internal string RawXml { get; }

        int IConfigErrorInfo.LineNumber
        {
            get
            {
                if (_lineNumberIsConstant) return _lineOffset;
                if (_lineOffset > 0) return LineNumber + (_lineOffset - 1);
                return LineNumber;
            }
        }

        string IConfigErrorInfo.Filename => _filename;

        internal ConfigXmlReader Clone()
        {
            return new ConfigXmlReader(RawXml, _filename, _lineOffset, _lineNumberIsConstant);
        }
    }
}