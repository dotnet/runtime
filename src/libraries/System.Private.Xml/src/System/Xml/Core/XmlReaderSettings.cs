// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Xml.Schema;

namespace System.Xml
{
    // XmlReaderSettings class specifies basic features of an XmlReader.
    public sealed class XmlReaderSettings
    {
        internal static readonly XmlReaderSettings s_defaultReaderSettings = new XmlReaderSettings() { ReadOnly = true };

        //
        // Fields
        //

        private bool _useAsync;


        // Nametable
        private XmlNameTable? _nameTable;

        // XmlResolver
        private XmlResolver? _xmlResolver;

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

        //Validation settings
        private ValidationType _validationType;
        private XmlSchemaValidationFlags _validationFlags;
        private XmlSchemaSet? _schemas;
        private ValidationEventHandler? _valEventHandler;

        // other settings
        private bool _closeInput;

        // read-only flag
        private bool _isReadOnly;

        // Creation of validating readers is hidden behind a delegate which is only initialized if the ValidationType
        // property is set. This is for AOT builds where the tree shaker can reduce the validating readers away
        // if nobody calls the ValidationType setter. Might also help with non-AOT build when ILLinker is used.
        private delegate XmlReader AddValidationFunc(XmlReader reader, XmlResolver? resolver, bool addConformanceWrapper);
        private AddValidationFunc? _addValidationFunc;

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
                CheckReadOnly(nameof(Async));
                _useAsync = value;
            }
        }

        // Nametable
        public XmlNameTable? NameTable
        {
            get
            {
                return _nameTable;
            }
            set
            {
                CheckReadOnly(nameof(NameTable));
                _nameTable = value;
            }
        }

        // XmlResolver
        internal bool IsXmlResolverSet
        {
            get;
            set; // keep set internal as we need to call it from the schema validation code
        }

        public XmlResolver? XmlResolver
        {
            set
            {
                CheckReadOnly(nameof(XmlResolver));
                _xmlResolver = value;
                IsXmlResolverSet = true;
            }
        }

        internal XmlResolver? GetXmlResolver()
        {
            return _xmlResolver;
        }

        //This is used by get XmlResolver in Xsd.
        //Check if the config set to prohibit default resovler
        //notice we must keep GetXmlResolver() to avoid dead lock when init System.Config.ConfigurationManager
        internal XmlResolver? GetXmlResolver_CheckConfig()
        {
            if (!LocalAppContextSwitches.AllowDefaultResolver && !IsXmlResolverSet)
                return null;
            else
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
                CheckReadOnly(nameof(LineNumberOffset));
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
                CheckReadOnly(nameof(LinePositionOffset));
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
                CheckReadOnly(nameof(ConformanceLevel));

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
                CheckReadOnly(nameof(CheckCharacters));
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
                CheckReadOnly(nameof(MaxCharactersInDocument));
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
                CheckReadOnly(nameof(MaxCharactersFromEntities));
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
                CheckReadOnly(nameof(IgnoreWhitespace));
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
                CheckReadOnly(nameof(IgnoreProcessingInstructions));
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
                CheckReadOnly(nameof(IgnoreComments));
                _ignoreComments = value;
            }
        }

        [Obsolete("XmlReaderSettings.ProhibitDtd has been deprecated. Use DtdProcessing instead.")]
        public bool ProhibitDtd
        {
            get
            {
                return _dtdProcessing == DtdProcessing.Prohibit;
            }
            set
            {
                CheckReadOnly(nameof(ProhibitDtd));
                _dtdProcessing = value ? DtdProcessing.Prohibit : DtdProcessing.Parse;
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
                CheckReadOnly(nameof(DtdProcessing));

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
                CheckReadOnly(nameof(CloseInput));
                _closeInput = value;
            }
        }

        public ValidationType ValidationType
        {
            get
            {
                return _validationType;
            }
            set
            {
                CheckReadOnly(nameof(ValidationType));

                // This introduces a dependency on the validation readers and along with that
                // on XmlSchema and so on. For AOT builds this brings in a LOT of code
                // which we would like to avoid unless it's needed. So the first approximation
                // is to only reference this method when somebody explicitly sets the ValidationType.
                _addValidationFunc = AddValidationInternal;

                if ((uint)value > (uint)ValidationType.Schema)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _validationType = value;
            }
        }

        public XmlSchemaValidationFlags ValidationFlags
        {
            get
            {
                return _validationFlags;
            }
            set
            {
                CheckReadOnly(nameof(ValidationFlags));

                if ((uint)value > (uint)(XmlSchemaValidationFlags.ProcessInlineSchema | XmlSchemaValidationFlags.ProcessSchemaLocation |
                                           XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessIdentityConstraints |
                                           XmlSchemaValidationFlags.AllowXmlAttributes))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _validationFlags = value;
            }
        }

        public XmlSchemaSet Schemas
        {
            get
            {
                if (_schemas == null)
                {
                    _schemas = new XmlSchemaSet();
                }
                return _schemas;
            }
            set
            {
                CheckReadOnly(nameof(Schemas));
                _schemas = value;
            }
        }

        public event ValidationEventHandler ValidationEventHandler
        {
            add
            {
                CheckReadOnly(nameof(ValidationEventHandler));
                _valEventHandler += value;
            }
            remove
            {
                CheckReadOnly(nameof(ValidationEventHandler));
                _valEventHandler -= value;
            }
        }

        //
        // Public methods
        //
        public void Reset()
        {
            CheckReadOnly(nameof(Reset));
            Initialize();
        }

        public XmlReaderSettings Clone()
        {
            XmlReaderSettings clonedSettings = (this.MemberwiseClone() as XmlReaderSettings)!;
            clonedSettings.ReadOnly = false;
            return clonedSettings;
        }

        //
        // Internal methods
        //
        internal ValidationEventHandler? GetEventHandler()
        {
            return _valEventHandler;
        }

        internal XmlReader CreateReader(string inputUri, XmlParserContext? inputContext)
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
            XmlResolver tmpResolver = this.GetXmlResolver() ?? new XmlUrlResolver();

            // create text XML reader
            XmlReader reader = new XmlTextReaderImpl(inputUri, this, inputContext, tmpResolver);

            // wrap with validating reader
            if (this.ValidationType != ValidationType.None)
            {
                reader = AddValidation(reader);
            }

            if (_useAsync)
            {
                reader = XmlAsyncCheckReader.CreateAsyncCheckWrapper(reader);
            }

            return reader;
        }

        internal XmlReader CreateReader(Stream input, Uri? baseUri, string? baseUriString, XmlParserContext? inputContext)
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

            // wrap with validating reader
            if (this.ValidationType != ValidationType.None)
            {
                reader = AddValidation(reader);
            }

            if (_useAsync)
            {
                reader = XmlAsyncCheckReader.CreateAsyncCheckWrapper(reader);
            }

            return reader;
        }

        internal XmlReader CreateReader(TextReader input, string? baseUriString, XmlParserContext? inputContext)
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

            // wrap with validating reader
            if (this.ValidationType != ValidationType.None)
            {
                reader = AddValidation(reader);
            }

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

            return AddValidationAndConformanceWrapper(reader);
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
                throw new XmlException(SR.Xml_ReadOnlyProperty, this.GetType().Name + '.' + propertyName);
            }
        }

        //
        // Private methods
        //
        private void Initialize()
        {
            Initialize(null);
        }

        private void Initialize(XmlResolver? resolver)
        {
            _nameTable = null;
            _xmlResolver = resolver;
            // limit the entity resolving to 10 million character. the caller can still
            // override it to any other value or set it to zero for unlimiting it
            _maxCharactersFromEntities = (long)1e7;

            _lineNumberOffset = 0;
            _linePositionOffset = 0;
            _checkCharacters = true;
            _conformanceLevel = ConformanceLevel.Document;

            _ignoreWhitespace = false;
            _ignorePIs = false;
            _ignoreComments = false;
            _dtdProcessing = DtdProcessing.Prohibit;
            _closeInput = false;

            _maxCharactersInDocument = 0;

            _schemas = null;
            _validationType = ValidationType.None;
            _validationFlags = XmlSchemaValidationFlags.ProcessIdentityConstraints;
            _validationFlags |= XmlSchemaValidationFlags.AllowXmlAttributes;

            _useAsync = false;

            _isReadOnly = false;
            IsXmlResolverSet = false;
        }

        internal XmlReader AddValidation(XmlReader reader)
        {
            XmlResolver? resolver = null;
            if (_validationType == ValidationType.Schema)
            {
                resolver = GetXmlResolver_CheckConfig();

                if (resolver == null &&
                    !this.IsXmlResolverSet)
                {
                    resolver = new XmlUrlResolver();
                }
            }

            return AddValidationAndConformanceInternal(reader, resolver, addConformanceWrapper: false);
        }

        private XmlReader AddValidationAndConformanceWrapper(XmlReader reader)
        {
            XmlResolver? resolver = null;
            if (_validationType == ValidationType.Schema)
            {
                resolver = GetXmlResolver_CheckConfig();
            }

            return AddValidationAndConformanceInternal(reader, resolver, addConformanceWrapper: true);
        }

        private XmlReader AddValidationAndConformanceInternal(XmlReader reader, XmlResolver? resolver, bool addConformanceWrapper)
        {
            // We have to avoid calling the _addValidationFunc delegate if there's no validation to setup
            // since it would not be initialized (to allow AOT compilers to reduce it away).
            // So if that's the case and we still need conformance wrapper add it here directly.
            // This is a slight code duplication, but it's necessary due to ordering constrains
            // of the reader wrapping as described in AddValidationInternal.
            if (_validationType == ValidationType.None)
            {
                if (addConformanceWrapper)
                {
                    reader = AddConformanceWrapper(reader);
                }
            }
            else
            {
                Debug.Assert(_addValidationFunc != null);
                reader = _addValidationFunc(reader, resolver, addConformanceWrapper);
            }

            return reader;
        }

        private XmlReader AddValidationInternal(XmlReader reader, XmlResolver? resolver, bool addConformanceWrapper)
        {
            // wrap with DTD validating reader
            if (_validationType == ValidationType.DTD)
            {
                reader = CreateDtdValidatingReader(reader);
            }

            if (addConformanceWrapper)
            {
                // add conformance checking (must go after DTD validation because XmlValidatingReader works only on XmlTextReader),
                // but before XSD validation because of typed value access
                reader = AddConformanceWrapper(reader);
            }

            if (_validationType == ValidationType.Schema)
            {
                reader = new XsdValidatingReader(reader, GetXmlResolver_CheckConfig(), this);
            }

            return reader;
        }

        private XmlValidatingReaderImpl CreateDtdValidatingReader(XmlReader baseReader)
        {
            return new XmlValidatingReaderImpl(baseReader, this.GetEventHandler(), (this.ValidationFlags & XmlSchemaValidationFlags.ProcessIdentityConstraints) != 0);
        }

        internal XmlReader AddConformanceWrapper(XmlReader baseReader)
        {
            XmlReaderSettings? baseReaderSettings = baseReader.Settings;
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

                // get the V1 XmlTextReader ref
                XmlTextReader? v1XmlTextReader = baseReader as XmlTextReader;
                if (v1XmlTextReader == null)
                {
                    XmlValidatingReader? vr = baseReader as XmlValidatingReader;
                    if (vr != null)
                    {
                        v1XmlTextReader = (XmlTextReader)vr.Reader;
                    }
                }

                // assume the V1 readers already do all conformance checking;
                // wrap only if IgnoreWhitespace, IgnoreComments, IgnoreProcessingInstructions or ProhibitDtd is true;
                if (_ignoreWhitespace)
                {
                    WhitespaceHandling wh = WhitespaceHandling.All;
                    // special-case our V1 readers to see if whey already filter whitespace
                    if (v1XmlTextReader != null)
                    {
                        wh = v1XmlTextReader.WhitespaceHandling;
                    }
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
                // DTD processing
                DtdProcessing baseDtdProcessing = DtdProcessing.Parse;
                if (v1XmlTextReader != null)
                {
                    baseDtdProcessing = v1XmlTextReader.DtdProcessing;
                }

                if ((_dtdProcessing == DtdProcessing.Prohibit && baseDtdProcessing != DtdProcessing.Prohibit) ||
                    (_dtdProcessing == DtdProcessing.Ignore && baseDtdProcessing == DtdProcessing.Parse))
                {
                    dtdProc = _dtdProcessing;
                    needWrap = true;
                }
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

                if ((_dtdProcessing == DtdProcessing.Prohibit && baseReaderSettings.DtdProcessing != DtdProcessing.Prohibit) ||
                    (_dtdProcessing == DtdProcessing.Ignore && baseReaderSettings.DtdProcessing == DtdProcessing.Parse))
                {
                    dtdProc = _dtdProcessing;
                    needWrap = true;
                }
            }

            if (needWrap)
            {
                IXmlNamespaceResolver? readerAsNSResolver = baseReader as IXmlNamespaceResolver;
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
