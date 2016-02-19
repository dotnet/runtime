// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.Versioning;

namespace System.Xml
{
    /// <summary>
    /// Three-state logic enumeration.
    /// </summary>
    internal enum TriState
    {
        Unknown = -1,
        False = 0,
        True = 1,
    };

    internal enum XmlStandalone
    {
        // Do not change the constants - XmlBinaryWriter depends in it
        Omit = 0,
        Yes = 1,
        No = 2,
    }

    // XmlWriterSettings class specifies basic features of an XmlWriter.
    public sealed class XmlWriterSettings
    {
        //
        // Fields
        //

        private bool _useAsync;

        // Text settings
        private Encoding _encoding;

#if FEATURE_LEGACYNETCF
        private bool dontWriteEncodingTag;
#endif

        private bool _omitXmlDecl;
        private NewLineHandling _newLineHandling;
        private string _newLineChars;
        private TriState _indent;
        private string _indentChars;
        private bool _newLineOnAttributes;
        private bool _closeOutput;
        private NamespaceHandling _namespaceHandling;

        // Conformance settings
        private ConformanceLevel _conformanceLevel;
        private bool _checkCharacters;
        private bool _writeEndDocumentOnClose;


        // read-only flag
        private bool _isReadOnly;

        //
        // Constructor
        //
        public XmlWriterSettings()
        {
            Initialize();
        }

        //
        // Properties
        //

        public bool Async
        {
            get
            {
                return _useAsync;
            }
            set
            {
                CheckReadOnly("Async");
                _useAsync = value;
            }
        }

        // Text
        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }
            set
            {
                CheckReadOnly("Encoding");
                _encoding = value;
            }
        }

#if FEATURE_LEGACYNETCF
        internal bool DontWriteEncodingTag
        {
            get
            {
                return dontWriteEncodingTag;
            }
            set
            {
                CheckReadOnly("DontWriteEncodingTag");
                dontWriteEncodingTag = value;
            }
        }
#endif

        // True if an xml declaration should *not* be written.
        public bool OmitXmlDeclaration
        {
            get
            {
                return _omitXmlDecl;
            }
            set
            {
                CheckReadOnly("OmitXmlDeclaration");
                _omitXmlDecl = value;
            }
        }

        // See NewLineHandling enum for details.
        public NewLineHandling NewLineHandling
        {
            get
            {
                return _newLineHandling;
            }
            set
            {
                CheckReadOnly("NewLineHandling");

                if ((uint)value > (uint)NewLineHandling.None)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _newLineHandling = value;
            }
        }

        // Line terminator string. By default, this is a carriage return followed by a line feed ("\r\n").
        public string NewLineChars
        {
            get
            {
                return _newLineChars;
            }
            set
            {
                CheckReadOnly("NewLineChars");

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _newLineChars = value;
            }
        }

        // True if output should be indented using rules that are appropriate to the output rules (i.e. Xml, Html, etc).
        public bool Indent
        {
            get
            {
                return _indent == TriState.True;
            }
            set
            {
                CheckReadOnly("Indent");
                _indent = value ? TriState.True : TriState.False;
            }
        }

        // Characters to use when indenting. This is usually tab or some spaces, but can be anything.
        public string IndentChars
        {
            get
            {
                return _indentChars;
            }
            set
            {
                CheckReadOnly("IndentChars");

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _indentChars = value;
            }
        }

        // Whether or not indent attributes on new lines.
        public bool NewLineOnAttributes
        {
            get
            {
                return _newLineOnAttributes;
            }
            set
            {
                CheckReadOnly("NewLineOnAttributes");
                _newLineOnAttributes = value;
            }
        }

        // Whether or not the XmlWriter should close the underlying stream or TextWriter when Close is called on the XmlWriter.
        public bool CloseOutput
        {
            get
            {
                return _closeOutput;
            }
            set
            {
                CheckReadOnly("CloseOutput");
                _closeOutput = value;
            }
        }


        // Conformance
        // See ConformanceLevel enum for details.
        public ConformanceLevel ConformanceLevel
        {
            get
            {
                return _conformanceLevel;
            }
            set
            {
                CheckReadOnly("ConformanceLevel");

                if ((uint)value > (uint)ConformanceLevel.Document)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _conformanceLevel = value;
            }
        }

        // Whether or not to check content characters that they are valid XML characters.
        public bool CheckCharacters
        {
            get
            {
                return _checkCharacters;
            }
            set
            {
                CheckReadOnly("CheckCharacters");
                _checkCharacters = value;
            }
        }

        // Whether or not to remove duplicate namespace declarations
        public NamespaceHandling NamespaceHandling
        {
            get
            {
                return _namespaceHandling;
            }
            set
            {
                CheckReadOnly("NamespaceHandling");
                if ((uint)value > (uint)(NamespaceHandling.OmitDuplicates))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _namespaceHandling = value;
            }
        }

        //Whether or not to auto complete end-element when close/dispose
        public bool WriteEndDocumentOnClose
        {
            get
            {
                return _writeEndDocumentOnClose;
            }
            set
            {
                CheckReadOnly("WriteEndDocumentOnClose");
                _writeEndDocumentOnClose = value;
            }
        }


        //
        // Public methods
        //
        public void Reset()
        {
            CheckReadOnly("Reset");
            Initialize();
        }

        // Deep clone all settings (except read-only, which is always set to false).  The original and new objects
        // can now be set independently of each other.
        public XmlWriterSettings Clone()
        {
            XmlWriterSettings clonedSettings = MemberwiseClone() as XmlWriterSettings;


            clonedSettings._isReadOnly = false;
            return clonedSettings;
        }

        //
        // Internal properties
        //


        internal XmlWriter CreateWriter(Stream output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            XmlWriter writer;

            // create raw writer
            Debug.Assert(Encoding.UTF8.WebName == "utf-8");
            if (this.Encoding.WebName == "utf-8")
            { // Encoding.CodePage is not supported in Silverlight
                // create raw UTF-8 writer
                if (this.Indent)
                {
                    writer = new XmlUtf8RawTextWriterIndent(output, this);
                }
                else
                {
                    writer = new XmlUtf8RawTextWriter(output, this);
                }
            }
            else
            {
                // create raw writer for other encodings
                if (this.Indent)
                {
                    writer = new XmlEncodedRawTextWriterIndent(output, this);
                }
                else
                {
                    writer = new XmlEncodedRawTextWriter(output, this);
                }
            }

            // wrap with well-formed writer
            writer = new XmlWellFormedWriter(writer, this);

            if (_useAsync)
            {
                writer = new XmlAsyncCheckWriter(writer);
            }

            return writer;
        }

        internal XmlWriter CreateWriter(TextWriter output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            XmlWriter writer;

            // create raw writer
            if (this.Indent)
            {
                writer = new XmlEncodedRawTextWriterIndent(output, this);
            }
            else
            {
                writer = new XmlEncodedRawTextWriter(output, this);
            }

            // wrap with well-formed writer
            writer = new XmlWellFormedWriter(writer, this);

            if (_useAsync)
            {
                writer = new XmlAsyncCheckWriter(writer);
            }
            return writer;
        }

        internal XmlWriter CreateWriter(XmlWriter output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            return AddConformanceWrapper(output);
        }


        internal bool ReadOnly
        {
            get
            {
                return _isReadOnly;
            }
            set
            {
                _isReadOnly = value;
            }
        }

        private void CheckReadOnly(string propertyName)
        {
            if (_isReadOnly)
            {
                throw new XmlException(SR.Xml_ReadOnlyProperty, this.GetType().ToString() + '.' + propertyName);
            }
        }

        //
        // Private methods
        //
        private void Initialize()
        {
            _encoding = Encoding.UTF8;
            _omitXmlDecl = false;
            _newLineHandling = NewLineHandling.Replace;
            _newLineChars = Environment.NewLine; // "\r\n" on Windows, "\n" on Unix
            _indent = TriState.Unknown;
            _indentChars = "  ";
            _newLineOnAttributes = false;
            _closeOutput = false;
            _namespaceHandling = NamespaceHandling.Default;
            _conformanceLevel = ConformanceLevel.Document;
            _checkCharacters = true;
            _writeEndDocumentOnClose = true;


            _useAsync = false;
            _isReadOnly = false;
        }

        private XmlWriter AddConformanceWrapper(XmlWriter baseWriter)
        {
            ConformanceLevel confLevel = ConformanceLevel.Auto;
            XmlWriterSettings baseWriterSettings = baseWriter.Settings;
            bool checkValues = false;
            bool checkNames = false;
            bool replaceNewLines = false;
            bool needWrap = false;

            if (baseWriterSettings == null)
            {
                // assume the V1 writer already do all conformance checking; 
                // wrap only if NewLineHandling == Replace or CheckCharacters is true
                if (_newLineHandling == NewLineHandling.Replace)
                {
                    replaceNewLines = true;
                    needWrap = true;
                }
                if (_checkCharacters)
                {
                    checkValues = true;
                    needWrap = true;
                }
            }
            else
            {
                if (_conformanceLevel != baseWriterSettings.ConformanceLevel)
                {
                    confLevel = this.ConformanceLevel;
                    needWrap = true;
                }
                if (_checkCharacters && !baseWriterSettings.CheckCharacters)
                {
                    checkValues = true;
                    checkNames = confLevel == ConformanceLevel.Auto;
                    needWrap = true;
                }
                if (_newLineHandling == NewLineHandling.Replace &&
                     baseWriterSettings.NewLineHandling == NewLineHandling.None)
                {
                    replaceNewLines = true;
                    needWrap = true;
                }
            }

            XmlWriter writer = baseWriter;

            if (needWrap)
            {
                if (confLevel != ConformanceLevel.Auto)
                {
                    writer = new XmlWellFormedWriter(writer, this);
                }
                if (checkValues || replaceNewLines)
                {
                    writer = new XmlCharCheckingWriter(writer, checkValues, checkNames, replaceNewLines, this.NewLineChars);
                }
            }


            return writer;
        }
        //
        // Internal methods
        //

    }
}
