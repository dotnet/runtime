// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <spec>http://webdata/xml/specs/XslCompiledTransform.xml</spec>
//------------------------------------------------------------------------------

namespace System.Xml.Xsl
{
    public sealed class XsltSettings
    {
        private bool _enableDocumentFunction;
        private bool _enableScript;
        private bool _checkOnly;
        private bool _includeDebugInformation;
        private int _warningLevel = -1;     // -1 means not set
        private bool _treatWarningsAsErrors;

        public XsltSettings() { }

        public XsltSettings(bool enableDocumentFunction, bool enableScript)
        {
            _enableDocumentFunction = enableDocumentFunction;
            _enableScript = enableScript;
        }

        public static XsltSettings Default
        {
            get { return new XsltSettings(false, false); }
        }

        public static XsltSettings TrustedXslt
        {
            get { return new XsltSettings(true, true); }
        }

        public bool EnableDocumentFunction
        {
            get { return _enableDocumentFunction; }
            set { _enableDocumentFunction = value; }
        }

        public bool EnableScript
        {
            get { return _enableScript; }
            set { _enableScript = value; }
        }

        internal bool CheckOnly
        {
            get { return _checkOnly; }
            set { _checkOnly = value; }
        }

        internal bool IncludeDebugInformation
        {
            get { return _includeDebugInformation; }
            set { _includeDebugInformation = value; }
        }

        internal int WarningLevel
        {
            get { return _warningLevel; }
            set { _warningLevel = value; }
        }

        internal bool TreatWarningsAsErrors
        {
            get { return _treatWarningsAsErrors; }
            set { _treatWarningsAsErrors = value; }
        }
    }
}
