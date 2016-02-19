// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace System.Xml
{
    // XmlReaderSettings class specifies basic features of an XmlReader.
    public sealed class XmlReaderSettings
    {
        //
        // Fields
        //

        private bool _useAsync;


        // Nametable
        private XmlNameTable _nameTable;

        // XmlResolver
        private XmlResolver _xmlResolver = null;

        // Text settings
        private int _lineNumberOffset;
        private int _linePositionOffset;

        // Conformance settings
        private ConformanceLevel _conformanceLevel;
        private bool _checkCharacters;

        private long _maxCharactersInDocument;
        private long _maxCharactersFromEntities;

        // Filtering settings
        private bool _ignoreWhitespace;
        private bool _ignorePIs;
        private bool _ignoreComments;

        // security settings
        private DtdProcessing _dtdProcessing;


        // other settings
        private bool _closeInput;

        // read-only flag
        private bool _isReadOnly;

        //
        // Constructor
        //
        public XmlReaderSettings()
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

        // Nametable
        public XmlNameTable NameTable
        {
            get
            {
                return _nameTable;
            }
            set
            {
                CheckReadOnly("NameTable");
                _nameTable = value;
            }
        }


        internal XmlResolver GetXmlResolver()
        {
            return _xmlResolver;
        }


        // Text settings
        public int LineNumberOffset
        {
            get
            {
                return _lineNumberOffset;
            }
            set
            {
                CheckReadOnly("LineNumberOffset");
                _lineNumberOffset = value;
            }
        }

        public int LinePositionOffset
        {
            get
            {
                return _linePositionOffset;
            }
            set
            {
                CheckReadOnly("LinePositionOffset");
                _linePositionOffset = value;
            }
        }

        // Conformance settings
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

        public long MaxCharactersInDocument
        {
            get
            {
                return _maxCharactersInDocument;
            }
            set
            {
                CheckReadOnly("MaxCharactersInDocument");
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _maxCharactersInDocument = value;
            }
        }

        public long MaxCharactersFromEntities
        {
            get
            {
                return _maxCharactersFromEntities;
            }
            set
            {
                CheckReadOnly("MaxCharactersFromEntities");
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _maxCharactersFromEntities = value;
            }
        }

        // Filtering settings
        public bool IgnoreWhitespace
        {
            get
            {
                return _ignoreWhitespace;
            }
            set
            {
                CheckReadOnly("IgnoreWhitespace");
                _ignoreWhitespace = value;
            }
        }

        public bool IgnoreProcessingInstructions
        {
            get
            {
                return _ignorePIs;
            }
            set
            {
                CheckReadOnly("IgnoreProcessingInstructions");
                _ignorePIs = value;
            }
        }

        public bool IgnoreComments
        {
            get
            {
                return _ignoreComments;
            }
            set
            {
                CheckReadOnly("IgnoreComments");
                _ignoreComments = value;
            }
        }


        public DtdProcessing DtdProcessing
        {
            get
            {
                return _dtdProcessing;
            }
            set
            {
                CheckReadOnly("DtdProcessing");

                if ((uint)value > (uint)DtdProcessing.Parse)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _dtdProcessing = value;
            }
        }

        public bool CloseInput
        {
            get
            {
                return _closeInput;
            }
            set
            {
                CheckReadOnly("CloseInput");
                _closeInput = value;
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

        public XmlReaderSettings Clone()
        {
            XmlReaderSettings clonedSettings = this.MemberwiseClone() as XmlReaderSettings;
            clonedSettings.ReadOnly = false;
            return clonedSettings;
        }

        //
        // Internal methods
        //



        internal XmlReader CreateReader(String inputUri, XmlParserContext inputContext)
        {
            if (inputUri == null)
            {
                throw new ArgumentNullException(nameof(inputUri));
            }
            if (inputUri.Length == 0)
            {
                throw new ArgumentException(SR.XmlConvert_BadUri, nameof(inputUri));
            }

            // resolve and open the url
            XmlResolver tmpResolver = this.GetXmlResolver();
            if (tmpResolver == null)
            {
                tmpResolver = CreateDefaultResolver();
            }

            // create text XML reader
            XmlReader reader = new XmlTextReaderImpl(inputUri, this, inputContext, tmpResolver);


            if (_useAsync)
            {
                reader = XmlAsyncCheckReader.CreateAsyncCheckWrapper(reader);
            }
            return reader;
        }

        internal XmlReader CreateReader(Stream input, Uri baseUri, string baseUriString, XmlParserContext inputContext)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (baseUriString == null)
            {
                if (baseUri == null)
                {
                    baseUriString = string.Empty;
                }
                else
                {
                    baseUriString = baseUri.ToString();
                }
            }

            // create text XML reader
            XmlReader reader = new XmlTextReaderImpl(input, null, 0, this, baseUri, baseUriString, inputContext, _closeInput);


            if (_useAsync)
            {
                reader = XmlAsyncCheckReader.CreateAsyncCheckWrapper(reader);
            }

            return reader;
        }

        internal XmlReader CreateReader(TextReader input, string baseUriString, XmlParserContext inputContext)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (baseUriString == null)
            {
                baseUriString = string.Empty;
            }

            // create xml text reader
            XmlReader reader = new XmlTextReaderImpl(input, this, baseUriString, inputContext);


            if (_useAsync)
            {
                reader = XmlAsyncCheckReader.CreateAsyncCheckWrapper(reader);
            }

            return reader;
        }

        internal XmlReader CreateReader(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            // wrap with conformance layer (if needed)
            return AddConformanceWrapper(reader);
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
            _nameTable = null;
            _lineNumberOffset = 0;
            _linePositionOffset = 0;
            _checkCharacters = true;
            _conformanceLevel = ConformanceLevel.Document;

            _ignoreWhitespace = false;
            _ignorePIs = false;
            _ignoreComments = false;
            _dtdProcessing = DtdProcessing.Prohibit;
            _closeInput = false;

            _maxCharactersFromEntities = 0;
            _maxCharactersInDocument = 0;


            _useAsync = false;

            _isReadOnly = false;
        }

        private static XmlResolver CreateDefaultResolver()
        {
            return new XmlSystemPathResolver();
        }


        internal XmlReader AddConformanceWrapper(XmlReader baseReader)
        {
            XmlReaderSettings baseReaderSettings = baseReader.Settings;
            bool checkChars = false;
            bool noWhitespace = false;
            bool noComments = false;
            bool noPIs = false;
            DtdProcessing dtdProc = (DtdProcessing)(-1);
            bool needWrap = false;

            if (baseReaderSettings == null)
            {
#pragma warning disable 618

                if (_conformanceLevel != ConformanceLevel.Auto && _conformanceLevel != XmlReader.GetV1ConformanceLevel(baseReader))
                {
                    throw new InvalidOperationException(SR.Format(SR.Xml_IncompatibleConformanceLevel, _conformanceLevel.ToString()));
                }


                // assume the V1 readers already do all conformance checking; 
                // wrap only if IgnoreWhitespace, IgnoreComments, IgnoreProcessingInstructions or ProhibitDtd is true;
                if (_ignoreWhitespace)
                {
                    WhitespaceHandling wh = WhitespaceHandling.All;
                    if (wh == WhitespaceHandling.All)
                    {
                        noWhitespace = true;
                        needWrap = true;
                    }
                }
                if (_ignoreComments)
                {
                    noComments = true;
                    needWrap = true;
                }
                if (_ignorePIs)
                {
                    noPIs = true;
                    needWrap = true;
                }

                dtdProc = _dtdProcessing;
                needWrap = true;
#pragma warning restore 618
            }
            else
            {
                if (_conformanceLevel != baseReaderSettings.ConformanceLevel && _conformanceLevel != ConformanceLevel.Auto)
                {
                    throw new InvalidOperationException(SR.Format(SR.Xml_IncompatibleConformanceLevel, _conformanceLevel.ToString()));
                }
                if (_checkCharacters && !baseReaderSettings.CheckCharacters)
                {
                    checkChars = true;
                    needWrap = true;
                }
                if (_ignoreWhitespace && !baseReaderSettings.IgnoreWhitespace)
                {
                    noWhitespace = true;
                    needWrap = true;
                }
                if (_ignoreComments && !baseReaderSettings.IgnoreComments)
                {
                    noComments = true;
                    needWrap = true;
                }
                if (_ignorePIs && !baseReaderSettings.IgnoreProcessingInstructions)
                {
                    noPIs = true;
                    needWrap = true;
                }

                if (_dtdProcessing == DtdProcessing.Prohibit && baseReaderSettings.DtdProcessing != DtdProcessing.Prohibit)
                {
                    dtdProc = _dtdProcessing;
                    needWrap = true;
                }
            }

            if (needWrap)
            {
                IXmlNamespaceResolver readerAsNSResolver = baseReader as IXmlNamespaceResolver;
                if (readerAsNSResolver != null)
                {
                    return new XmlCharCheckingReaderWithNS(baseReader, readerAsNSResolver, checkChars, noWhitespace, noComments, noPIs, dtdProc);
                }
                else
                {
                    return new XmlCharCheckingReader(baseReader, checkChars, noWhitespace, noComments, noPIs, dtdProc);
                }
            }
            else
            {
                return baseReader;
            }
        }
    }
}
