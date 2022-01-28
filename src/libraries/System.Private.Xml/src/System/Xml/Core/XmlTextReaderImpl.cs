// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Xml.Schema;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    internal sealed partial class XmlTextReaderImpl : XmlReader, IXmlLineInfo, IXmlNamespaceResolver
    {
        private static UTF8Encoding? s_utf8BomThrowing;

        private static UTF8Encoding UTF8BomThrowing =>
            s_utf8BomThrowing ?? (s_utf8BomThrowing = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true, throwOnInvalidBytes: true));

        //
        // Private helper types
        //
        // ParsingFunction = what should the reader do when the next Read() is called
        private enum ParsingFunction
        {
            ElementContent = 0,
            NoData,
            OpenUrl,
            SwitchToInteractive,
            SwitchToInteractiveXmlDecl,
            DocumentContent,
            MoveToElementContent,
            PopElementContext,
            PopEmptyElementContext,
            ResetAttributesRootLevel,
            Error,
            Eof,
            ReaderClosed,
            EntityReference,
            InIncrementalRead,
            FragmentAttribute,
            ReportEndEntity,
            AfterResolveEntityInContent,
            AfterResolveEmptyEntityInContent,
            XmlDeclarationFragment,
            GoToEof,
            PartialTextValue,

            // these two states must be last; see InAttributeValueIterator property
            InReadAttributeValue,
            InReadValueChunk,
            InReadContentAsBinary,
            InReadElementContentAsBinary,
        }

        private enum ParsingMode
        {
            Full,
            SkipNode,
            SkipContent,
        }

        private enum EntityType
        {
            CharacterDec,
            CharacterHex,
            CharacterNamed,
            Expanded,
            Skipped,
            FakeExpanded,
            Unexpanded,
            ExpandedInAttribute,
        }

        private enum EntityExpandType
        {
            All,
            OnlyGeneral,
            OnlyCharacter,
        }

        private enum IncrementalReadState
        {
            // Following values are used in ReadText, ReadBase64 and ReadBinHex (V1 streaming methods)
            Text,
            StartTag,
            PI,
            CDATA,
            Comment,
            Attributes,
            AttributeValue,
            ReadData,
            EndElement,
            End,

            // Following values are used in ReadTextChunk, ReadContentAsBase64 and ReadBinHexChunk (V2 streaming methods)
            ReadValueChunk_OnCachedValue,
            ReadValueChunk_OnPartialValue,

            ReadContentAsBinary_OnCachedValue,
            ReadContentAsBinary_OnPartialValue,
            ReadContentAsBinary_End,
        }

        //
        // Fields
        //
        private readonly bool _useAsync;

        #region Later Init Fields

        //later init means in the construction stage, do not open filestream and do not read any data from Stream/TextReader
        //the purpose is to make the Create of XmlReader do not block on IO.
        private sealed class LaterInitParam
        {
            public bool useAsync;

            public Stream? inputStream;
            public byte[]? inputBytes;
            public int inputByteCount;
            public Uri? inputbaseUri;
            public string? inputUriStr;
            public XmlResolver? inputUriResolver;
            public XmlParserContext? inputContext;
            public TextReader? inputTextReader;

            public InitInputType initType = InitInputType.Invalid;
        }

        private LaterInitParam? _laterInitParam;

        private enum InitInputType
        {
            UriString,
            Stream,
            TextReader,
            Invalid
        }

        #endregion

        // current parsing state (aka. scanner data)
        private ParsingState _ps;

        // parsing function = what to do in the next Read() (3-items-long stack, usually used just 2 level)
        private ParsingFunction _parsingFunction;
        private ParsingFunction _nextParsingFunction;
        private ParsingFunction _nextNextParsingFunction;

        // stack of nodes
        private NodeData[] _nodes;

        // current node
        private NodeData _curNode;

        // current index
        private int _index;

        // attributes info
        private int _curAttrIndex = -1;
        private int _attrCount;
        private int _attrHashtable;
        private int _attrDuplWalkCount;
        private bool _attrNeedNamespaceLookup;
        private bool _fullAttrCleanup;
        private NodeData[]? _attrDuplSortingArray;

        // name table
        private XmlNameTable _nameTable;
        private bool _nameTableFromSettings;

        // resolver
        private XmlResolver? _xmlResolver;

        // this is only for constructors that takes url
        private readonly string _url = string.Empty;

        // settings
        private bool _normalize;
        private bool _supportNamespaces = true;
        private WhitespaceHandling _whitespaceHandling;
        private DtdProcessing _dtdProcessing = DtdProcessing.Parse;
        private EntityHandling _entityHandling;
        private readonly bool _ignorePIs;
        private readonly bool _ignoreComments;
        private readonly bool _checkCharacters;
        private readonly int _lineNumberOffset;
        private readonly int _linePositionOffset;
        private readonly bool _closeInput;
        private readonly long _maxCharactersInDocument;
        private readonly long _maxCharactersFromEntities;

        // this flag enables XmlTextReader backwards compatibility;
        // when false, the reader has been created via XmlReader.Create
        private readonly bool _v1Compat;

        // namespace handling
        private XmlNamespaceManager? _namespaceManager;
        private string _lastPrefix = string.Empty;

        // xml context (xml:space, xml:lang, default namespace)
        private XmlContext _xmlContext;

        // stack of parsing states (=stack of entities)
        private ParsingState[]? _parsingStatesStack;
        private int _parsingStatesStackTop = -1;

        // current node base uri and encoding
        private string _reportedBaseUri = string.Empty;
        private Encoding? _reportedEncoding;

        // DTD
        private IDtdInfo? _dtdInfo;

        // fragment parsing
        private XmlNodeType _fragmentType = XmlNodeType.Document;
        private XmlParserContext? _fragmentParserContext;
        private bool _fragment;

        // incremental read
        private IncrementalReadDecoder? _incReadDecoder;
        private IncrementalReadState _incReadState;
        private LineInfo _incReadLineInfo;
        private BinHexDecoder? _binHexDecoder;
        private Base64Decoder? _base64Decoder;
        private int _incReadDepth;
        private int _incReadLeftStartPos;
        private int _incReadLeftEndPos;
        private IncrementalReadCharsDecoder? _readCharsDecoder;

        // ReadAttributeValue helpers
        private int _attributeValueBaseEntityId;
        private bool _emptyEntityInAttributeResolved;

        // Validation helpers
        private IValidationEventHandling? _validationEventHandling;
        private OnDefaultAttributeUseDelegate? _onDefaultAttributeUse;

        private bool _validatingReaderCompatFlag;

        // misc
        private bool _addDefaultAttributesAndNormalize;
        private readonly StringBuilder _stringBuilder;
        private bool _rootElementParsed;
        private bool _standalone;
        private int _nextEntityId = 1;
        private ParsingMode _parsingMode;
        private ReadState _readState = ReadState.Initial;
        private IDtdEntityInfo? _lastEntity;
        private bool _afterResetState;
        private int _documentStartBytePos;
        private int _readValueOffset;

        // Counters for security settings
        private long _charactersInDocument;
        private long _charactersFromEntities;

        // All entities that are currently being processed
        private Dictionary<IDtdEntityInfo, IDtdEntityInfo>? _currentEntities;

        // DOM helpers
        private bool _disableUndeclaredEntityCheck;

        // Outer XmlReader exposed to the user - either XmlTextReader or XmlTextReaderImpl (when created via XmlReader.Create).
        // Virtual methods called from within XmlTextReaderImpl must be called on the outer reader so in case the user overrides
        // some of the XmlTextReader methods we will call the overridden version.
        private XmlReader _outerReader;

        //indicate if the XmlResolver is explicit set
        private bool _xmlResolverIsSet;

        //
        // Atomized string constants
        //
        private readonly string _xml;
        private readonly string _xmlNs;

        //
        // Constants
        //
        private const int MaxBytesToMove = 128;
        private const int ApproxXmlDeclLength = 80;
        private const int NodesInitialSize = 8;
        private const int InitialParsingStatesDepth = 2;
        private const int MaxByteSequenceLen = 6;  // max bytes per character
        private const int MaxAttrDuplWalkCount = 250;
        private const int MinWhitespaceLookahedCount = 4096;

        private const string XmlDeclarationBeginning = "<?xml";

        //
        // Constructors
        //

        internal XmlTextReaderImpl()
        {
            _parsingFunction = ParsingFunction.NoData;
            _outerReader = this;
            _xmlContext = new XmlContext();
            _nameTable = new NameTable();
            _nodes = new NodeData[NodesInitialSize];
            _nodes[0] = new NodeData();
            _curNode = _nodes[0];
            _stringBuilder = new StringBuilder();
            _xml = _nameTable.Add("xml");
            _xmlNs = _nameTable.Add("xmlns");
        }

        // Initializes a new instance of the XmlTextReaderImpl class with the specified XmlNameTable.
        // This constructor is used when creating XmlTextReaderImpl for V1 XmlTextReader
        internal XmlTextReaderImpl(XmlNameTable nt)
        {
            Debug.Assert(nt != null);

            _v1Compat = true;
            _outerReader = this;

            _nameTable = nt;
            nt.Add(string.Empty);

            _xmlResolver = null;

            _xml = nt.Add("xml");
            _xmlNs = nt.Add("xmlns");

            Debug.Assert(_index == 0);
            _nodes = new NodeData[NodesInitialSize];
            _nodes[0] = new NodeData();
            _curNode = _nodes[0];

            _stringBuilder = new StringBuilder();
            _xmlContext = new XmlContext();

            _parsingFunction = ParsingFunction.SwitchToInteractiveXmlDecl;
            _nextParsingFunction = ParsingFunction.DocumentContent;

            _entityHandling = EntityHandling.ExpandCharEntities;
            _whitespaceHandling = WhitespaceHandling.All;
            _closeInput = true;

            _maxCharactersInDocument = 0;
            // Breaking change: entity expansion is enabled, but limit it to 10,000,000 chars (like XLinq)
            _maxCharactersFromEntities = (long)1e7;
            _charactersInDocument = 0;
            _charactersFromEntities = 0;

            _ps.lineNo = 1;
            _ps.lineStartPos = -1;
        }

        // This constructor is used when creating XmlTextReaderImpl reader via "XmlReader.Create(..)"
        private XmlTextReaderImpl(XmlResolver? resolver, XmlReaderSettings settings, XmlParserContext? context)
        {
            _useAsync = settings.Async;
            _v1Compat = false;
            _outerReader = this;

            _xmlContext = new XmlContext();

            // create or get nametable and namespace manager from XmlParserContext
            XmlNameTable? nt = settings.NameTable;
            if (context == null)
            {
                if (nt == null)
                {
                    nt = new NameTable();
                    Debug.Assert(_nameTableFromSettings == false);
                }
                else
                {
                    _nameTableFromSettings = true;
                }

                _nameTable = nt;
                _namespaceManager = new XmlNamespaceManager(nt);
            }
            else
            {
                SetupFromParserContext(context, settings);
                nt = _nameTable;
            }

            nt.Add(string.Empty);
            _xml = nt.Add("xml");
            _xmlNs = nt.Add("xmlns");

            _xmlResolver = resolver;

            Debug.Assert(_index == 0);

            _nodes = new NodeData[NodesInitialSize];
            _nodes[0] = new NodeData();
            _curNode = _nodes[0];

            _stringBuilder = new StringBuilder();

            // Needed only for XmlTextReader (reporting of entities)
            _entityHandling = EntityHandling.ExpandEntities;

            _xmlResolverIsSet = settings.IsXmlResolverSet;

            _whitespaceHandling = (settings.IgnoreWhitespace) ? WhitespaceHandling.Significant : WhitespaceHandling.All;
            _normalize = true;
            _ignorePIs = settings.IgnoreProcessingInstructions;
            _ignoreComments = settings.IgnoreComments;
            _checkCharacters = settings.CheckCharacters;
            _lineNumberOffset = settings.LineNumberOffset;
            _linePositionOffset = settings.LinePositionOffset;
            _ps.lineNo = _lineNumberOffset + 1;
            _ps.lineStartPos = -_linePositionOffset - 1;
            _curNode.SetLineInfo(_ps.LineNo - 1, _ps.LinePos - 1);
            _dtdProcessing = settings.DtdProcessing;
            _maxCharactersInDocument = settings.MaxCharactersInDocument;
            _maxCharactersFromEntities = settings.MaxCharactersFromEntities;

            _charactersInDocument = 0;
            _charactersFromEntities = 0;

            _fragmentParserContext = context;

            _parsingFunction = ParsingFunction.SwitchToInteractiveXmlDecl;
            _nextParsingFunction = ParsingFunction.DocumentContent;

            switch (settings.ConformanceLevel)
            {
                case ConformanceLevel.Auto:
                    _fragmentType = XmlNodeType.None;
                    _fragment = true;
                    break;
                case ConformanceLevel.Fragment:
                    _fragmentType = XmlNodeType.Element;
                    _fragment = true;
                    break;
                case ConformanceLevel.Document:
                    _fragmentType = XmlNodeType.Document;
                    break;
                default:
                    Debug.Fail($"Unexpected conformance level {settings.ConformanceLevel}");
                    goto case ConformanceLevel.Document;
            }
        }

        // Initializes a new instance of the XmlTextReaderImpl class with the specified stream, baseUri and nametable
        // This constructor is used when creating XmlTextReaderImpl for V1 XmlTextReader
        internal XmlTextReaderImpl(Stream input) : this(string.Empty, input, new NameTable())
        {
        }

        internal XmlTextReaderImpl(Stream input, XmlNameTable nt) : this(string.Empty, input, nt)
        {
        }

        internal XmlTextReaderImpl(string url, Stream input) : this(url, input, new NameTable())
        {
        }

        internal XmlTextReaderImpl(string? url, Stream input, XmlNameTable nt) : this(nt)
        {
            ConvertAbsoluteUnixPathToAbsoluteUri(ref url, resolver: null);
            _namespaceManager = new XmlNamespaceManager(nt);

            if (url == null || url.Length == 0)
            {
                InitStreamInput(input, null);
            }
            else
            {
                InitStreamInput(url, input, null);
            }

            _reportedBaseUri = _ps.baseUriStr;
            _reportedEncoding = _ps.encoding;
        }

        // Initializes a new instance of the XmlTextReaderImpl class with the specified TextReader, baseUri and XmlNameTable.
        // This constructor is used when creating XmlTextReaderImpl for V1 XmlTextReader
        internal XmlTextReaderImpl(TextReader input) : this(string.Empty, input, new NameTable())
        {
        }

        internal XmlTextReaderImpl(TextReader input, XmlNameTable nt) : this(string.Empty, input, nt)
        {
        }

        internal XmlTextReaderImpl(string url, TextReader input) : this(url, input, new NameTable())
        {
        }

        internal XmlTextReaderImpl(string? url, TextReader input, XmlNameTable nt) : this(nt)
        {
            ConvertAbsoluteUnixPathToAbsoluteUri(ref url, resolver: null);
            _namespaceManager = new XmlNamespaceManager(nt);
            _reportedBaseUri = (url != null) ? url : string.Empty;
            InitTextReaderInput(_reportedBaseUri, input);
            _reportedEncoding = _ps.encoding;
        }

        // Initializes a new instance of XmlTextReaderImpl class for parsing fragments with the specified stream, fragment type and parser context
        // This constructor is used when creating XmlTextReaderImpl for V1 XmlTextReader
        // SxS: The method resolves URI but does not expose the resolved value up the stack hence Resource Exposure scope is None.
        internal XmlTextReaderImpl(Stream xmlFragment, XmlNodeType fragType, XmlParserContext? context)
            : this((context != null && context.NameTable != null) ? context.NameTable : new NameTable())
        {
            Encoding? enc = (context != null) ? context.Encoding : null;
            if (context == null || context.BaseURI == null || context.BaseURI.Length == 0)
            {
                InitStreamInput(xmlFragment, enc);
            }
            else
            {
                // It is important to have valid resolver here to resolve the Xml url file path.
                // it is safe as this resolver will not be used to resolve DTD url's
                InitStreamInput(GetTempResolver().ResolveUri(null, context.BaseURI), xmlFragment, enc);
            }

            InitFragmentReader(fragType, context, false);

            _reportedBaseUri = _ps.baseUriStr;
            _reportedEncoding = _ps.encoding;
        }

        // Initializes a new instance of XmlTextRreaderImpl class for parsing fragments with the specified string, fragment type and parser context
        // This constructor is used when creating XmlTextReaderImpl for V1 XmlTextReader
        internal XmlTextReaderImpl(string xmlFragment, XmlNodeType fragType, XmlParserContext? context)
            : this(null == context || null == context.NameTable ? new NameTable() : context.NameTable)
        {
            if (xmlFragment == null)
            {
                xmlFragment = string.Empty;
            }

            if (context == null)
            {
                InitStringInput(string.Empty, Encoding.Unicode, xmlFragment);
            }
            else
            {
                _reportedBaseUri = context.BaseURI;
                InitStringInput(context.BaseURI, Encoding.Unicode, xmlFragment);
            }

            InitFragmentReader(fragType, context, false);
            _reportedEncoding = _ps.encoding;
        }

        // Following constructor assumes that the fragment node type == XmlDecl
        // We handle this node type separately because there is not real way to determine what the
        // "innerXml" of an XmlDecl is. This internal function is required by DOM. When(if) we handle/allow
        // all nodetypes in InnerXml then we should support them as part of fragment constructor as well.
        // Until then, this internal function will have to do.
        internal XmlTextReaderImpl(string xmlFragment, XmlParserContext? context)
            : this(null == context || null == context.NameTable ? new NameTable() : context.NameTable)
        {
            InitStringInput((context == null) ? string.Empty : context.BaseURI, Encoding.Unicode, $"<?xml {xmlFragment}?>");
            InitFragmentReader(XmlNodeType.XmlDeclaration, context, true);
        }

        // Initializes a new instance of the XmlTextReaderImpl class with the specified url and XmlNameTable.
        // This constructor is used when creating XmlTextReaderImpl for V1 XmlTextReader
        public XmlTextReaderImpl(string url) : this(url, new NameTable())
        {
        }

        public XmlTextReaderImpl(string url, XmlNameTable nt) : this(nt)
        {
            ArgumentException.ThrowIfNullOrEmpty(url);

            _namespaceManager = new XmlNamespaceManager(nt);

            _url = url;

            // It is important to have valid resolver here to resolve the Xml url file path.
            // it is safe as this resolver will not be used to resolve DTD url's
            _ps.baseUri = GetTempResolver().ResolveUri(null, url);
            _ps.baseUriStr = _ps.baseUri.ToString();
            _reportedBaseUri = _ps.baseUriStr;

            _parsingFunction = ParsingFunction.OpenUrl;
        }


        // Initializes a new instance of the XmlTextReaderImpl class with the specified arguments.
        // This constructor is used when creating XmlTextReaderImpl via XmlReader.Create
        internal XmlTextReaderImpl(string uriStr, XmlReaderSettings settings, XmlParserContext? context, XmlResolver uriResolver)
            : this(settings.GetXmlResolver(), settings, context)
        {
            Uri baseUri = uriResolver.ResolveUri(null, uriStr);
            string baseUriStr = baseUri.ToString();

            // get BaseUri from XmlParserContext
            if (context != null)
            {
                if (context.BaseURI != null && context.BaseURI.Length > 0 &&
                    !UriEqual(baseUri, baseUriStr, context.BaseURI, settings.GetXmlResolver()))
                {
                    if (baseUriStr.Length > 0)
                    {
                        Throw(SR.Xml_DoubleBaseUri);
                    }
                    Debug.Assert(baseUri == null);
                    baseUriStr = context.BaseURI;
                }
            }

            _reportedBaseUri = baseUriStr;
            _closeInput = true;
            _laterInitParam = new LaterInitParam();
            _laterInitParam.inputUriStr = uriStr;
            _laterInitParam.inputbaseUri = baseUri;
            _laterInitParam.inputContext = context;
            _laterInitParam.inputUriResolver = uriResolver;
            _laterInitParam.initType = InitInputType.UriString;

            if (!settings.Async)
            {
                //if not set Async flag, finish the init in create stage.
                FinishInitUriString();
            }
            else
            {
                _laterInitParam.useAsync = true;
            }
        }

        private void FinishInitUriString()
        {
            Stream? stream;
            Debug.Assert(_laterInitParam != null);
            Debug.Assert(_laterInitParam.inputUriResolver != null);
            Debug.Assert(_laterInitParam.inputbaseUri != null);
            Debug.Assert(_reportedBaseUri != null);

            if (_laterInitParam.useAsync)
            {
                // this will be hit when user create a XmlReader by setting Async, but the first call is Read() instead of ReadAsync(),
                // then we still should create an async stream here. And wait for the method finish.
                Task<object> t = _laterInitParam.inputUriResolver.GetEntityAsync(_laterInitParam.inputbaseUri, string.Empty, typeof(Stream));
                stream = (Stream)t.GetAwaiter().GetResult();
            }
            else
            {
                stream = (Stream?)_laterInitParam.inputUriResolver.GetEntity(_laterInitParam.inputbaseUri, string.Empty, typeof(Stream));
            }

            if (stream == null)
            {
                throw new XmlException(SR.Xml_CannotResolveUrl, _laterInitParam.inputUriStr);
            }

            Encoding? enc = null;
            // get Encoding from XmlParserContext
            if (_laterInitParam.inputContext != null)
            {
                enc = _laterInitParam.inputContext.Encoding;
            }

            try
            {
                // init ParsingState
                InitStreamInput(_laterInitParam.inputbaseUri, _reportedBaseUri, stream, null, 0, enc);

                _reportedEncoding = _ps.encoding;

                // parse DTD
                if (_laterInitParam.inputContext != null && _laterInitParam.inputContext.HasDtdInfo)
                {
                    ProcessDtdFromParserContext(_laterInitParam.inputContext);
                }
            }
            catch
            {
                stream.Dispose();
                throw;
            }

            _laterInitParam = null;
        }

        // Initializes a new instance of the XmlTextReaderImpl class with the specified arguments.
        // This constructor is used when creating XmlTextReaderImpl via XmlReader.Create
        internal XmlTextReaderImpl(Stream stream, byte[]? bytes, int byteCount, XmlReaderSettings settings, Uri? baseUri, string? baseUriStr,
                                    XmlParserContext? context, bool closeInput)
            : this(settings.GetXmlResolver(), settings, context)
        {
            ConvertAbsoluteUnixPathToAbsoluteUri(ref baseUriStr, settings.GetXmlResolver());

            // get BaseUri from XmlParserContext
            if (context != null)
            {
                if (context.BaseURI != null && context.BaseURI.Length > 0 &&
                    !UriEqual(baseUri, baseUriStr, context.BaseURI, settings.GetXmlResolver()))
                {
                    if (baseUriStr!.Length > 0)
                    {
                        Throw(SR.Xml_DoubleBaseUri);
                    }

                    Debug.Assert(baseUri == null);
                    baseUriStr = context.BaseURI;
                }
            }

            _reportedBaseUri = baseUriStr ?? string.Empty;
            _closeInput = closeInput;

            _laterInitParam = new LaterInitParam();
            _laterInitParam.inputStream = stream;
            _laterInitParam.inputBytes = bytes;
            _laterInitParam.inputByteCount = byteCount;
            _laterInitParam.inputbaseUri = baseUri;
            _laterInitParam.inputContext = context;

            _laterInitParam.initType = InitInputType.Stream;
            if (!settings.Async)
            {
                //if not set Async flag, finish the init in create stage.
                FinishInitStream();
            }
            else
            {
                _laterInitParam.useAsync = true;
            }
        }

        private void FinishInitStream()
        {
            Encoding? enc = null;

            Debug.Assert(_laterInitParam != null);
            Debug.Assert(_laterInitParam.inputStream != null);
            Debug.Assert(_reportedBaseUri != null);

            // get Encoding from XmlParserContext
            if (_laterInitParam.inputContext != null)
            {
                enc = _laterInitParam.inputContext.Encoding;
            }

            // init ParsingState
            InitStreamInput(_laterInitParam.inputbaseUri, _reportedBaseUri, _laterInitParam.inputStream, _laterInitParam.inputBytes, _laterInitParam.inputByteCount, enc);

            _reportedEncoding = _ps.encoding;

            // parse DTD
            if (_laterInitParam.inputContext != null && _laterInitParam.inputContext.HasDtdInfo)
            {
                ProcessDtdFromParserContext(_laterInitParam.inputContext);
            }

            _laterInitParam = null;
        }

        // Initializes a new instance of the XmlTextReaderImpl class with the specified arguments.
        // This constructor is used when creating XmlTextReaderImpl via XmlReader.Create
        internal XmlTextReaderImpl(TextReader input, XmlReaderSettings settings, string baseUriStr, XmlParserContext? context)
            : this(settings.GetXmlResolver(), settings, context)
        {
            ConvertAbsoluteUnixPathToAbsoluteUri(ref baseUriStr, settings.GetXmlResolver());
            // get BaseUri from XmlParserContext
            if (context != null)
            {
                Debug.Assert(baseUriStr == string.Empty, "BaseURI can come either from XmlParserContext or from the constructor argument, not from both");
                if (context.BaseURI != null)
                {
                    baseUriStr = context.BaseURI;
                }
            }

            _reportedBaseUri = baseUriStr;
            _closeInput = settings.CloseInput;
            _laterInitParam = new LaterInitParam();
            _laterInitParam.inputTextReader = input;
            _laterInitParam.inputContext = context;
            _laterInitParam.initType = InitInputType.TextReader;
            if (!settings.Async)
            {
                //if not set Async flag, finish the init in create stage.
                FinishInitTextReader();
            }
            else
            {
                _laterInitParam.useAsync = true;
            }
        }

        private void FinishInitTextReader()
        {
            Debug.Assert(_laterInitParam != null);
            Debug.Assert(_laterInitParam.inputTextReader != null);
            Debug.Assert(_reportedBaseUri != null);

            // init ParsingState
            InitTextReaderInput(_reportedBaseUri, _laterInitParam.inputTextReader);

            _reportedEncoding = _ps.encoding;

            // parse DTD
            if (_laterInitParam.inputContext != null && _laterInitParam.inputContext.HasDtdInfo)
            {
                ProcessDtdFromParserContext(_laterInitParam.inputContext);
            }

            _laterInitParam = null;
        }

        // Initializes a new instance of the XmlTextReaderImpl class for fragment parsing.
        // This constructor is used by XmlBinaryReader for nested text XML
        internal XmlTextReaderImpl(string xmlFragment, XmlParserContext? context, XmlReaderSettings settings)
            : this(null, settings, context)
        {
            Debug.Assert(xmlFragment != null);
            InitStringInput(string.Empty, Encoding.Unicode, xmlFragment);
            _reportedBaseUri = _ps.baseUriStr;
            _reportedEncoding = _ps.encoding;
        }

        //
        // XmlReader members
        //
        // Returns the current settings of the reader
        public override XmlReaderSettings Settings
        {
            get
            {
                XmlReaderSettings settings = new XmlReaderSettings();

                if (_nameTableFromSettings)
                {
                    settings.NameTable = _nameTable;
                }

                switch (_fragmentType)
                {
                    case XmlNodeType.None: settings.ConformanceLevel = ConformanceLevel.Auto; break;
                    case XmlNodeType.Element: settings.ConformanceLevel = ConformanceLevel.Fragment; break;
                    case XmlNodeType.Document: settings.ConformanceLevel = ConformanceLevel.Document; break;
                    default: Debug.Fail($"Unexpected fragment type {_fragmentType}"); goto case XmlNodeType.None;
                }

                settings.CheckCharacters = _checkCharacters;
                settings.LineNumberOffset = _lineNumberOffset;
                settings.LinePositionOffset = _linePositionOffset;
                settings.IgnoreWhitespace = (_whitespaceHandling == WhitespaceHandling.Significant);
                settings.IgnoreProcessingInstructions = _ignorePIs;
                settings.IgnoreComments = _ignoreComments;
                settings.DtdProcessing = _dtdProcessing;
                settings.MaxCharactersInDocument = _maxCharactersInDocument;
                settings.MaxCharactersFromEntities = _maxCharactersFromEntities;
                settings.XmlResolver = _xmlResolver;
                settings.ReadOnly = true;
                return settings;
            }
        }

        // Returns the type of the current node.
        public override XmlNodeType NodeType
        {
            get
            {
                return _curNode.type;
            }
        }

        // Returns the name of the current node, including prefix.
        public override string Name
        {
            get
            {
                return _curNode.GetNameWPrefix(_nameTable);
            }
        }

        // Returns local name of the current node (without prefix)
        public override string LocalName
        {
            get
            {
                return _curNode.localName;
            }
        }

        // Returns namespace name of the current node.
        public override string NamespaceURI
        {
            get
            {
                return _curNode.ns ?? string.Empty;
            }
        }

        // Returns prefix associated with the current node.
        public override string Prefix
        {
            get
            {
                return _curNode.prefix;
            }
        }

        // Returns the text value of the current node.
        public override string Value
        {
            get
            {
                if (_parsingFunction >= ParsingFunction.PartialTextValue)
                {
                    if (_parsingFunction == ParsingFunction.PartialTextValue)
                    {
                        FinishPartialValue();
                        _parsingFunction = _nextParsingFunction;
                    }
                    else
                    {
                        FinishOtherValueIterator();
                    }
                }

                return _curNode.StringValue;
            }
        }

        // Returns the depth of the current node in the XML element stack
        public override int Depth
        {
            get
            {
                return _curNode.depth;
            }
        }

        // Returns the base URI of the current node.
        public override string BaseURI
        {
            get
            {
                return _reportedBaseUri;
            }
        }

        // Returns true if the current node is an empty element (for example, <MyElement/>).
        public override bool IsEmptyElement
        {
            get
            {
                return _curNode.IsEmptyElement;
            }
        }

        // Returns true of the current node is a default attribute declared in DTD.
        public override bool IsDefault
        {
            get
            {
                return _curNode.IsDefaultAttribute;
            }
        }

        // Returns the quote character used in the current attribute declaration
        public override char QuoteChar
        {
            get
            {
                return _curNode.type == XmlNodeType.Attribute ? _curNode.quoteChar : '"';
            }
        }

        // Returns the current xml:space scope.
        public override XmlSpace XmlSpace
        {
            get
            {
                return _xmlContext.xmlSpace;
            }
        }

        // Returns the current xml:lang scope.</para>
        public override string XmlLang
        {
            get
            {
                return _xmlContext.xmlLang;
            }
        }

        // Returns the current read state of the reader
        public override ReadState ReadState
        {
            get
            {
                return _readState;
            }
        }

        // Returns true if the reader reached end of the input data
        public override bool EOF
        {
            get
            {
                return _parsingFunction == ParsingFunction.Eof;
            }
        }

        // Returns the XmlNameTable associated with this XmlReader
        public override XmlNameTable NameTable
        {
            get
            {
                return _nameTable;
            }
        }

        // Returns true if the XmlReader knows how to resolve general entities
        public override bool CanResolveEntity
        {
            get
            {
                return true;
            }
        }

        // Returns the number of attributes on the current node.
        public override int AttributeCount
        {
            get
            {
                return _attrCount;
            }
        }

        // Returns value of an attribute with the specified Name
        public override string? GetAttribute(string name)
        {
            int i;
            if (!name.Contains(':'))
            {
                i = GetIndexOfAttributeWithoutPrefix(name);
            }
            else
            {
                i = GetIndexOfAttributeWithPrefix(name);
            }

            return (i >= 0) ? _nodes[i].StringValue : null;
        }

        // Returns value of an attribute with the specified LocalName and NamespaceURI
        public override string? GetAttribute(string localName, string? namespaceURI)
        {
            namespaceURI = (namespaceURI == null) ? string.Empty : _nameTable.Get(namespaceURI);
            string? localNameAtomized = _nameTable.Get(localName);

            for (int i = _index + 1; i < _index + _attrCount + 1; i++)
            {
                if (Ref.Equal(_nodes[i].localName, localNameAtomized) && Ref.Equal(_nodes[i].ns, namespaceURI))
                {
                    return _nodes[i].StringValue;
                }
            }

            return null;
        }

        // Returns value of an attribute at the specified index (position)
        public override string GetAttribute(int i)
        {
            if (i < 0 || i >= _attrCount)
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }

            return _nodes[_index + i + 1].StringValue;
        }

        // Moves to an attribute with the specified Name
        public override bool MoveToAttribute(string name)
        {
            int i;
            if (!name.Contains(':'))
            {
                i = GetIndexOfAttributeWithoutPrefix(name);
            }
            else
            {
                i = GetIndexOfAttributeWithPrefix(name);
            }

            if (i >= 0)
            {
                if (InAttributeValueIterator)
                {
                    FinishAttributeValueIterator();
                }
                _curAttrIndex = i - _index - 1;
                _curNode = _nodes[i];
                return true;
            }
            else
            {
                return false;
            }
        }

        // Moves to an attribute with the specified LocalName and NamespceURI
        public override bool MoveToAttribute(string localName, string? namespaceURI)
        {
            string? namespaceURIAtomized = (namespaceURI == null) ? string.Empty : _nameTable.Get(namespaceURI);
            string? localNameAtomized = _nameTable.Get(localName);
            for (int i = _index + 1; i < _index + _attrCount + 1; i++)
            {
                if (Ref.Equal(_nodes[i].localName, localNameAtomized) &&
                     Ref.Equal(_nodes[i].ns, namespaceURIAtomized))
                {
                    _curAttrIndex = i - _index - 1;
                    _curNode = _nodes[i];

                    if (InAttributeValueIterator)
                    {
                        FinishAttributeValueIterator();
                    }
                    return true;
                }
            }

            return false;
        }

        // Moves to an attribute at the specified index (position)
        public override void MoveToAttribute(int i)
        {
            if (i < 0 || i >= _attrCount)
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }

            if (InAttributeValueIterator)
            {
                FinishAttributeValueIterator();
            }

            _curAttrIndex = i;
            _curNode = _nodes[_index + 1 + _curAttrIndex];
        }

        // Moves to the first attribute of the current node
        public override bool MoveToFirstAttribute()
        {
            if (_attrCount == 0)
            {
                return false;
            }

            if (InAttributeValueIterator)
            {
                FinishAttributeValueIterator();
            }

            _curAttrIndex = 0;
            _curNode = _nodes[_index + 1];

            return true;
        }

        // Moves to the next attribute of the current node
        public override bool MoveToNextAttribute()
        {
            if (_curAttrIndex + 1 < _attrCount)
            {
                if (InAttributeValueIterator)
                {
                    FinishAttributeValueIterator();
                }
                _curNode = _nodes[_index + 1 + ++_curAttrIndex];
                return true;
            }
            return false;
        }

        // If on attribute, moves to the element that contains the attribute node
        public override bool MoveToElement()
        {
            if (InAttributeValueIterator)
            {
                FinishAttributeValueIterator();
            }
            else if (_curNode.type != XmlNodeType.Attribute)
            {
                return false;
            }
            _curAttrIndex = -1;
            _curNode = _nodes[_index];

            return true;
        }


        private void FinishInit()
        {
            Debug.Assert(_laterInitParam != null);

            switch (_laterInitParam.initType)
            {
                case InitInputType.UriString:
                    FinishInitUriString();
                    break;
                case InitInputType.Stream:
                    FinishInitStream();
                    break;
                case InitInputType.TextReader:
                    FinishInitTextReader();
                    break;
                default:
                    //should never hit here
                    Debug.Fail("Invalid InitInputType");
                    break;
            }
        }


        // Reads next node from the input data
        public override bool Read()
        {
            if (_laterInitParam != null)
            {
                FinishInit();
            }

            while (true)
            {
                switch (_parsingFunction)
                {
                    case ParsingFunction.ElementContent:
                        return ParseElementContent();
                    case ParsingFunction.DocumentContent:
                        return ParseDocumentContent();
                    case ParsingFunction.OpenUrl:
                        OpenUrl();
                        Debug.Assert(_nextParsingFunction == ParsingFunction.DocumentContent);
                        goto case ParsingFunction.SwitchToInteractiveXmlDecl;
                    case ParsingFunction.SwitchToInteractive:
                        Debug.Assert(!_ps.appendMode);
                        _readState = ReadState.Interactive;
                        _parsingFunction = _nextParsingFunction;
                        continue;
                    case ParsingFunction.SwitchToInteractiveXmlDecl:
                        _readState = ReadState.Interactive;
                        _parsingFunction = _nextParsingFunction;
                        if (ParseXmlDeclaration(false))
                        {
                            _reportedEncoding = _ps.encoding;
                            return true;
                        }
                        _reportedEncoding = _ps.encoding;
                        continue;
                    case ParsingFunction.ResetAttributesRootLevel:
                        ResetAttributes();
                        _curNode = _nodes[_index];
                        _parsingFunction = (_index == 0) ? ParsingFunction.DocumentContent : ParsingFunction.ElementContent;
                        continue;
                    case ParsingFunction.MoveToElementContent:
                        ResetAttributes();
                        _index++;
                        _curNode = AddNode(_index, _index);
                        _parsingFunction = ParsingFunction.ElementContent;
                        continue;
                    case ParsingFunction.PopElementContext:
                        PopElementContext();
                        _parsingFunction = _nextParsingFunction;
                        Debug.Assert(_parsingFunction == ParsingFunction.ElementContent ||
                                      _parsingFunction == ParsingFunction.DocumentContent);
                        continue;
                    case ParsingFunction.PopEmptyElementContext:
                        _curNode = _nodes[_index];
                        Debug.Assert(_curNode.type == XmlNodeType.Element);
                        _curNode.IsEmptyElement = false;
                        ResetAttributes();
                        PopElementContext();
                        _parsingFunction = _nextParsingFunction;
                        continue;
                    case ParsingFunction.EntityReference:
                        _parsingFunction = _nextParsingFunction;
                        ParseEntityReference();
                        return true;
                    case ParsingFunction.ReportEndEntity:
                        SetupEndEntityNodeInContent();
                        _parsingFunction = _nextParsingFunction;
                        return true;
                    case ParsingFunction.AfterResolveEntityInContent:
                        _curNode = AddNode(_index, _index);
                        _reportedEncoding = _ps.encoding;
                        _reportedBaseUri = _ps.baseUriStr;
                        _parsingFunction = _nextParsingFunction;
                        continue;
                    case ParsingFunction.AfterResolveEmptyEntityInContent:
                        _curNode = AddNode(_index, _index);
                        _curNode.SetValueNode(XmlNodeType.Text, string.Empty);
                        _curNode.SetLineInfo(_ps.lineNo, _ps.LinePos);
                        _reportedEncoding = _ps.encoding;
                        _reportedBaseUri = _ps.baseUriStr;
                        _parsingFunction = _nextParsingFunction;
                        return true;
                    case ParsingFunction.InReadAttributeValue:
                        FinishAttributeValueIterator();
                        _curNode = _nodes[_index];
                        continue;
                    case ParsingFunction.InIncrementalRead:
                        FinishIncrementalRead();
                        return true;
                    case ParsingFunction.FragmentAttribute:
                        return ParseFragmentAttribute();
                    case ParsingFunction.XmlDeclarationFragment:
                        ParseXmlDeclarationFragment();
                        _parsingFunction = ParsingFunction.GoToEof;
                        return true;
                    case ParsingFunction.GoToEof:
                        OnEof();
                        return false;
                    case ParsingFunction.Error:
                    case ParsingFunction.Eof:
                    case ParsingFunction.ReaderClosed:
                        return false;
                    case ParsingFunction.NoData:
                        ThrowWithoutLineInfo(SR.Xml_MissingRoot);
                        return false;
                    case ParsingFunction.PartialTextValue:
                        SkipPartialTextValue();
                        continue;
                    case ParsingFunction.InReadValueChunk:
                        FinishReadValueChunk();
                        continue;
                    case ParsingFunction.InReadContentAsBinary:
                        FinishReadContentAsBinary();
                        continue;
                    case ParsingFunction.InReadElementContentAsBinary:
                        FinishReadElementContentAsBinary();
                        continue;
                    default:
                        Debug.Fail($"Unexpected parsing function {_parsingFunction}");
                        break;
                }
            }
        }

        // Closes the input stream ot TextReader, changes the ReadState to Closed and sets all properties to zero/string.Empty
        public override void Close()
        {
            Close(_closeInput);
        }

        // Skips the current node. If on element, skips to the end tag of the element.
        public override void Skip()
        {
            if (_readState != ReadState.Interactive)
                return;

            if (InAttributeValueIterator)
            {
                FinishAttributeValueIterator();
                _curNode = _nodes[_index];
            }
            else
            {
                switch (_parsingFunction)
                {
                    case ParsingFunction.InReadAttributeValue:
                        Debug.Fail($"Unexpected parsing function {_parsingFunction}");
                        break;
                    case ParsingFunction.InIncrementalRead:
                        FinishIncrementalRead();
                        break;
                    case ParsingFunction.PartialTextValue:
                        SkipPartialTextValue();
                        break;
                    case ParsingFunction.InReadValueChunk:
                        FinishReadValueChunk();
                        break;
                    case ParsingFunction.InReadContentAsBinary:
                        FinishReadContentAsBinary();
                        break;
                    case ParsingFunction.InReadElementContentAsBinary:
                        FinishReadElementContentAsBinary();
                        break;
                }
            }

            switch (_curNode.type)
            {
                // skip subtree
                case XmlNodeType.Element:
                    if (_curNode.IsEmptyElement)
                    {
                        break;
                    }
                    int initialDepth = _index;
                    _parsingMode = ParsingMode.SkipContent;
                    // skip content
                    while (_outerReader.Read() && _index > initialDepth) ;
                    Debug.Assert(_curNode.type == XmlNodeType.EndElement);
                    Debug.Assert(_parsingFunction != ParsingFunction.Eof);
                    _parsingMode = ParsingMode.Full;
                    break;
                case XmlNodeType.Attribute:
                    _outerReader.MoveToElement();
                    goto case XmlNodeType.Element;
            }
            // move to following sibling node
            _outerReader.Read();
            return;
        }

        // Returns NamespaceURI associated with the specified prefix in the current namespace scope.
        public override string? LookupNamespace(string prefix)
        {
            if (!_supportNamespaces)
            {
                return null;
            }

            Debug.Assert(_namespaceManager != null);
            return _namespaceManager.LookupNamespace(prefix);
        }

        // Iterates through the current attribute value's text and entity references chunks.
        public override bool ReadAttributeValue()
        {
            if (_parsingFunction != ParsingFunction.InReadAttributeValue)
            {
                if (_curNode.type != XmlNodeType.Attribute)
                {
                    return false;
                }
                if (_readState != ReadState.Interactive || _curAttrIndex < 0)
                {
                    return false;
                }
                if (_parsingFunction == ParsingFunction.InReadValueChunk)
                {
                    FinishReadValueChunk();
                }
                if (_parsingFunction == ParsingFunction.InReadContentAsBinary)
                {
                    FinishReadContentAsBinary();
                }

                if (_curNode.nextAttrValueChunk == null || _entityHandling == EntityHandling.ExpandEntities)
                {
                    NodeData simpleValueNode = AddNode(_index + _attrCount + 1, _curNode.depth + 1);
                    simpleValueNode.SetValueNode(XmlNodeType.Text, _curNode.StringValue);
                    simpleValueNode.lineInfo = _curNode.lineInfo2;
                    simpleValueNode.depth = _curNode.depth + 1;
                    _curNode = simpleValueNode;

                    simpleValueNode.nextAttrValueChunk = null;
                }
                else
                {
                    _curNode = _curNode.nextAttrValueChunk;

                    // Place the current node at nodes[index + attrCount + 1]. If the node type
                    // is be EntityReference and user calls ResolveEntity, the associated EndEntity
                    // node will be constructed from the information stored there.

                    // This will initialize the (index + attrCount + 1) place in nodes array
                    AddNode(_index + _attrCount + 1, _index + 2);
                    _nodes[_index + _attrCount + 1] = _curNode;

                    _fullAttrCleanup = true;
                }
                _nextParsingFunction = _parsingFunction;
                _parsingFunction = ParsingFunction.InReadAttributeValue;
                _attributeValueBaseEntityId = _ps.entityId;
                return true;
            }
            else
            {
                if (_ps.entityId == _attributeValueBaseEntityId)
                {
                    if (_curNode.nextAttrValueChunk != null)
                    {
                        _curNode = _curNode.nextAttrValueChunk;
                        _nodes[_index + _attrCount + 1] = _curNode;  // if curNode == EntityReference node, it will be picked from here by SetupEndEntityNodeInAttribute
                        return true;
                    }
                    return false;
                }
                else
                {
                    // expanded entity in attribute value
                    return ParseAttributeValueChunk();
                }
            }
        }

        // Resolves the current entity reference node
        public override void ResolveEntity()
        {
            if (_curNode.type != XmlNodeType.EntityReference)
            {
                throw new InvalidOperationException(SR.Xml_InvalidOperation);
            }

            Debug.Assert(_parsingMode == ParsingMode.Full);

            // entity in attribute value
            if (_parsingFunction == ParsingFunction.InReadAttributeValue ||
                 _parsingFunction == ParsingFunction.FragmentAttribute)
            {
                switch (HandleGeneralEntityReference(_curNode.localName, true, true, _curNode.LinePos))
                {
                    case EntityType.ExpandedInAttribute:
                    case EntityType.Expanded:
                        if (_ps.charsUsed - _ps.charPos == 0)
                        {  // entity value == ""
                            _emptyEntityInAttributeResolved = true;
                        }
                        break;
                    case EntityType.FakeExpanded:
                        _emptyEntityInAttributeResolved = true;
                        break;
                    default:
                        Debug.Fail("Unexpected entity type");
                        throw new XmlException(SR.Xml_InternalError, string.Empty);
                }
            }
            // entity in element content
            else
            {
                switch (HandleGeneralEntityReference(_curNode.localName, false, true, _curNode.LinePos))
                {
                    case EntityType.ExpandedInAttribute:
                    case EntityType.Expanded:
                        _nextParsingFunction = _parsingFunction;
                        if (_ps.charsUsed - _ps.charPos == 0 && !_ps.entity!.IsExternal)
                        {  // empty internal entity value
                            _parsingFunction = ParsingFunction.AfterResolveEmptyEntityInContent;
                        }
                        else
                        {
                            _parsingFunction = ParsingFunction.AfterResolveEntityInContent;
                        }
                        break;
                    case EntityType.FakeExpanded:
                        _nextParsingFunction = _parsingFunction;
                        _parsingFunction = ParsingFunction.AfterResolveEmptyEntityInContent;
                        break;
                    default:
                        Debug.Fail("Unexpected entity type");
                        throw new XmlException(SR.Xml_InternalError, string.Empty);
                }
            }
            _ps.entityResolvedManually = true;
            _index++;
        }

        internal XmlReader OuterReader
        {
            get
            {
                return _outerReader;
            }
            set
            {
                Debug.Assert(value is XmlTextReader);
                _outerReader = value;
            }
        }

        internal void MoveOffEntityReference()
        {
            if (_outerReader.NodeType == XmlNodeType.EntityReference &&
                 _parsingFunction == ParsingFunction.AfterResolveEntityInContent)
            {
                if (!_outerReader.Read())
                {
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
                }
            }
        }

        public override string ReadString()
        {
            Debug.Assert(_outerReader is XmlTextReaderImpl);
            MoveOffEntityReference();
            return base.ReadString();
        }

        public override bool CanReadBinaryContent
        {
            get
            {
                return true;
            }
        }

        // Reads and concatenates content nodes, base64-decodes the results and copies the decoded bytes into the provided buffer
        public override int ReadContentAsBase64(byte[] buffer!!, int index, int count)
        {
            // check arguments
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // if not the first call to ReadContentAsBase64
            if (_parsingFunction == ParsingFunction.InReadContentAsBinary)
            {
                // and if we have a correct decoder
                if (_incReadDecoder == _base64Decoder)
                {
                    // read more binary data
                    return ReadContentAsBinary(buffer, index, count);
                }
            }
            // first call of ReadContentAsBase64 -> initialize (move to first text child (for elements) and initialize incremental read state)
            else
            {
                if (_readState != ReadState.Interactive)
                {
                    return 0;
                }
                if (_parsingFunction == ParsingFunction.InReadElementContentAsBinary)
                {
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);
                }
                if (!XmlReader.CanReadContentAs(_curNode.type))
                {
                    throw CreateReadContentAsException(nameof(ReadContentAsBase64));
                }
                if (!InitReadContentAsBinary())
                {
                    return 0;
                }
            }

            // setup base64 decoder
            InitBase64Decoder();

            // read binary data
            return ReadContentAsBinary(buffer, index, count);
        }


        // Reads and concatenates content nodes, binhex-decodes the results and copies the decoded bytes into the provided buffer
        public override int ReadContentAsBinHex(byte[] buffer!!, int index, int count)
        {
            // check arguments
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // if not the first call to ReadContentAsBinHex
            if (_parsingFunction == ParsingFunction.InReadContentAsBinary)
            {
                // and if we have a correct decoder
                if (_incReadDecoder == _binHexDecoder)
                {
                    // read more binary data
                    return ReadContentAsBinary(buffer, index, count);
                }
            }
            // first call of ReadContentAsBinHex -> initialize (move to first text child (for elements) and initialize incremental read state)
            else
            {
                if (_readState != ReadState.Interactive)
                {
                    return 0;
                }
                if (_parsingFunction == ParsingFunction.InReadElementContentAsBinary)
                {
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);
                }
                if (!XmlReader.CanReadContentAs(_curNode.type))
                {
                    throw CreateReadContentAsException(nameof(ReadContentAsBinHex));
                }
                if (!InitReadContentAsBinary())
                {
                    return 0;
                }
            }

            // setup binhex decoder (when in first ReadContentAsBinHex call or when mixed with ReadContentAsBase64)
            InitBinHexDecoder();

            // read binary data
            return ReadContentAsBinary(buffer, index, count);
        }

        // Reads and concatenates content of an element, base64-decodes the results and copies the decoded bytes into the provided buffer
        public override int ReadElementContentAsBase64(byte[] buffer!!, int index, int count)
        {
            // check arguments
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // if not the first call to ReadContentAsBase64
            if (_parsingFunction == ParsingFunction.InReadElementContentAsBinary)
            {
                // and if we have a correct decoder
                if (_incReadDecoder == _base64Decoder)
                {
                    // read more binary data
                    return ReadElementContentAsBinary(buffer, index, count);
                }
            }
            // first call of ReadElementContentAsBase64 -> initialize
            else
            {
                if (_readState != ReadState.Interactive)
                {
                    return 0;
                }
                if (_parsingFunction == ParsingFunction.InReadContentAsBinary)
                {
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);
                }
                if (_curNode.type != XmlNodeType.Element)
                {
                    throw CreateReadElementContentAsException(nameof(ReadElementContentAsBinHex));
                }
                if (!InitReadElementContentAsBinary())
                {
                    return 0;
                }
            }

            // setup base64 decoder
            InitBase64Decoder();

            // read binary data
            return ReadElementContentAsBinary(buffer, index, count);
        }


        // Reads and concatenates content of an element, binhex-decodes the results and copies the decoded bytes into the provided buffer
        public override int ReadElementContentAsBinHex(byte[] buffer!!, int index, int count)
        {
            // check arguments
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // if not the first call to ReadContentAsBinHex
            if (_parsingFunction == ParsingFunction.InReadElementContentAsBinary)
            {
                // and if we have a correct decoder
                if (_incReadDecoder == _binHexDecoder)
                {
                    // read more binary data
                    return ReadElementContentAsBinary(buffer, index, count);
                }
            }
            // first call of ReadContentAsBinHex -> initialize
            else
            {
                if (_readState != ReadState.Interactive)
                {
                    return 0;
                }
                if (_parsingFunction == ParsingFunction.InReadContentAsBinary)
                {
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);
                }
                if (_curNode.type != XmlNodeType.Element)
                {
                    throw CreateReadElementContentAsException(nameof(ReadElementContentAsBinHex));
                }
                if (!InitReadElementContentAsBinary())
                {
                    return 0;
                }
            }

            // setup binhex decoder (when in first ReadContentAsBinHex call or when mixed with ReadContentAsBase64)
            InitBinHexDecoder();

            // read binary data
            return ReadElementContentAsBinary(buffer, index, count);
        }

        // Returns true if ReadValue is supported
        public override bool CanReadValueChunk
        {
            get
            {
                return true;
            }
        }

        // Iterates over Value property and copies it into the provided buffer
        public override int ReadValueChunk(char[] buffer, int index, int count)
        {
            // throw on elements
            if (!XmlReader.HasValueInternal(_curNode.type))
            {
                throw new InvalidOperationException(SR.Format(SR.Xml_InvalidReadValueChunk, _curNode.type));
            }
            // check arguments
            ArgumentNullException.ThrowIfNull(buffer);
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // first call of ReadValueChunk -> initialize incremental read state
            if (_parsingFunction != ParsingFunction.InReadValueChunk)
            {
                if (_readState != ReadState.Interactive)
                {
                    return 0;
                }
                if (_parsingFunction == ParsingFunction.PartialTextValue)
                {
                    _incReadState = IncrementalReadState.ReadValueChunk_OnPartialValue;
                }
                else
                {
                    _incReadState = IncrementalReadState.ReadValueChunk_OnCachedValue;
                    _nextNextParsingFunction = _nextParsingFunction;
                    _nextParsingFunction = _parsingFunction;
                }
                _parsingFunction = ParsingFunction.InReadValueChunk;
                _readValueOffset = 0;
            }

            if (count == 0)
            {
                return 0;
            }

            // read what is already cached in curNode
            int readCount = 0;
            int read = _curNode.CopyTo(_readValueOffset, buffer, index + readCount, count - readCount);
            readCount += read;
            _readValueOffset += read;

            if (readCount == count)
            {
                // take care of surrogate pairs spanning between buffers
                char ch = buffer[index + count - 1];
                if (XmlCharType.IsHighSurrogate(ch))
                {
                    readCount--;
                    _readValueOffset--;
                    if (readCount == 0)
                    {
                        Throw(SR.Xml_NotEnoughSpaceForSurrogatePair);
                    }
                }
                return readCount;
            }

            // if on partial value, read the rest of it
            if (_incReadState == IncrementalReadState.ReadValueChunk_OnPartialValue)
            {
                _curNode.SetValue(string.Empty);

                // read next chunk of text
                bool endOfValue = false;
                int startPos = 0;
                int endPos = 0;
                while (readCount < count && !endOfValue)
                {
                    int orChars = 0;
                    endOfValue = ParseText(out startPos, out endPos, ref orChars);

                    int copyCount = count - readCount;
                    if (copyCount > endPos - startPos)
                    {
                        copyCount = endPos - startPos;
                    }

                    Debug.Assert(_ps.chars != null);
                    BlockCopyChars(_ps.chars, startPos, buffer, (index + readCount), copyCount);

                    readCount += copyCount;
                    startPos += copyCount;
                }

                _incReadState = endOfValue ? IncrementalReadState.ReadValueChunk_OnCachedValue : IncrementalReadState.ReadValueChunk_OnPartialValue;

                if (readCount == count)
                {
                    char ch = buffer[index + count - 1];
                    if (XmlCharType.IsHighSurrogate(ch))
                    {
                        readCount--;
                        startPos--;
                        if (readCount == 0)
                        {
                            Throw(SR.Xml_NotEnoughSpaceForSurrogatePair);
                        }
                    }
                }

                _readValueOffset = 0;
                Debug.Assert(_ps.chars != null);
                _curNode.SetValue(_ps.chars, startPos, endPos - startPos);
            }
            return readCount;
        }

        //
        // IXmlLineInfo members
        //
        public bool HasLineInfo()
        {
            return true;
        }

        // Returns the line number of the current node
        public int LineNumber
        {
            get
            {
                return _curNode.LineNo;
            }
        }

        // Returns the line position of the current node
        public int LinePosition
        {
            get
            {
                return _curNode.LinePos;
            }
        }

        //
        // IXmlNamespaceResolver members
        //
        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return this.GetNamespacesInScope(scope);
        }

        string? IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            return this.LookupNamespace(prefix);
        }

        string? IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            return this.LookupPrefix(namespaceName);
        }

        // Internal IXmlNamespaceResolver methods
        internal IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
        {
            Debug.Assert(_namespaceManager != null);
            return _namespaceManager.GetNamespacesInScope(scope);
        }

        // NOTE: there already is virtual method for "string LookupNamespace(string prefix)"

        internal string? LookupPrefix(string namespaceName)
        {
            Debug.Assert(_namespaceManager != null);
            return _namespaceManager.LookupPrefix(namespaceName);
        }

        //
        // XmlTextReader members
        //
        // Disables or enables support of W3C XML 1.0 Namespaces
        internal bool Namespaces
        {
            get
            {
                return _supportNamespaces;
            }
            set
            {
                if (_readState != ReadState.Initial)
                {
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
                }

                _supportNamespaces = value;
                if (value)
                {
                    if (_namespaceManager is NoNamespaceManager)
                    {
                        if (_fragment && _fragmentParserContext != null && _fragmentParserContext.NamespaceManager != null)
                        {
                            _namespaceManager = _fragmentParserContext.NamespaceManager;
                        }
                        else
                        {
                            _namespaceManager = new XmlNamespaceManager(_nameTable);
                        }
                    }

                    Debug.Assert(_namespaceManager != null);
                    _xmlContext.defaultNamespace = _namespaceManager.LookupNamespace(string.Empty)!;
                }
                else
                {
                    if (!(_namespaceManager is NoNamespaceManager))
                    {
                        _namespaceManager = new NoNamespaceManager();
                    }

                    _xmlContext.defaultNamespace = string.Empty;
                }
            }
        }

        // Enables or disables XML 1.0 normalization (incl. end-of-line normalization and normalization of attributes)
        internal bool Normalization
        {
            get
            {
                Debug.Assert(_v1Compat, "XmlTextReaderImpl.Normalization property cannot be accessed on reader created via XmlReader.Create.");
                return _normalize;
            }
            set
            {
                Debug.Assert(_v1Compat, "XmlTextReaderImpl.Normalization property cannot be changed on reader created via XmlReader.Create.");
                if (_readState == ReadState.Closed)
                {
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
                }
                _normalize = value;

                if (_ps.entity == null || _ps.entity.IsExternal)
                {
                    _ps.eolNormalized = !value;
                }
            }
        }

        // Returns the Encoding of the XML document
        internal Encoding? Encoding
        {
            get
            {
                return (_readState == ReadState.Interactive) ? _reportedEncoding : null;
            }
        }

        // Spefifies whitespace handling of the XML document, i.e. whether return all namespaces, only significant ones or none
        internal WhitespaceHandling WhitespaceHandling
        {
            get
            {
                Debug.Assert(_v1Compat, "XmlTextReaderImpl.WhitespaceHandling property cannot be accessed on reader created via XmlReader.Create.");
                return _whitespaceHandling;
            }
            set
            {
                Debug.Assert(_v1Compat, "XmlTextReaderImpl.WhitespaceHandling property cannot be changed on reader created via XmlReader.Create.");
                if (_readState == ReadState.Closed)
                {
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
                }

                if ((uint)value > (uint)WhitespaceHandling.None)
                {
                    throw new XmlException(SR.Xml_WhitespaceHandling, string.Empty);
                }

                _whitespaceHandling = value;
            }
        }

        // Specifies how the DTD is processed in the XML document.
        internal DtdProcessing DtdProcessing
        {
            get
            {
                Debug.Assert(_v1Compat, "XmlTextReaderImpl.DtdProcessing property cannot be accessed on reader created via XmlReader.Create.");
                return _dtdProcessing;
            }
            set
            {
                Debug.Assert(_v1Compat, "XmlTextReaderImpl.DtdProcessing property cannot be changed on reader created via XmlReader.Create.");

                if ((uint)value > (uint)DtdProcessing.Parse)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _dtdProcessing = value;
            }
        }

        // Spefifies whether general entities should be automatically expanded or not
        internal EntityHandling EntityHandling
        {
            get
            {
                return _entityHandling;
            }
            set
            {
                if (value != EntityHandling.ExpandEntities && value != EntityHandling.ExpandCharEntities)
                {
                    throw new XmlException(SR.Xml_EntityHandling, string.Empty);
                }

                _entityHandling = value;
            }
        }

        // Needed to check from the schema validation if the caller set the resolver so we'll not override it
        internal bool IsResolverSet
        {
            get { return _xmlResolverIsSet; }
        }

        // Specifies XmlResolver used for opening the XML document and other external references
        internal XmlResolver? XmlResolver
        {
            set
            {
                _xmlResolver = value;
                _xmlResolverIsSet = true;
                // invalidate all baseUris on the stack
                _ps.baseUri = null;

                for (int i = 0; i <= _parsingStatesStackTop; i++)
                {
                    _parsingStatesStack![i].baseUri = null;
                }
            }
        }

        // Reset the state of the reader so the reader is ready to parse another XML document from the same stream.
        internal void ResetState()
        {
            Debug.Assert(_v1Compat, "XmlTextReaderImpl.ResetState cannot be called on reader created via XmlReader.Create.");

            if (_fragment)
            {
                Throw(new InvalidOperationException(SR.Xml_InvalidResetStateCall));
            }

            if (_readState == ReadState.Initial)
            {
                return;
            }

            // Clear
            ResetAttributes();
            Debug.Assert(_namespaceManager != null);
            while (_namespaceManager.PopScope()) ;

            while (InEntity)
            {
                HandleEntityEnd(true);
            }

            // Init
            _readState = ReadState.Initial;
            _parsingFunction = ParsingFunction.SwitchToInteractiveXmlDecl;
            _nextParsingFunction = ParsingFunction.DocumentContent;

            _curNode = _nodes[0];
            _curNode.Clear(XmlNodeType.None);
            _curNode.SetLineInfo(0, 0);
            _index = 0;
            _rootElementParsed = false;

            _charactersInDocument = 0;
            _charactersFromEntities = 0;

            _afterResetState = true;
        }

        // returns the remaining unparsed data as TextReader
        internal TextReader GetRemainder()
        {
            Debug.Assert(_v1Compat, "XmlTextReaderImpl.GetRemainder cannot be called on reader created via XmlReader.Create.");
            Debug.Assert(_stringBuilder.Length == 0);
            switch (_parsingFunction)
            {
                case ParsingFunction.Eof:
                case ParsingFunction.ReaderClosed:
                    return new StringReader(string.Empty);
                case ParsingFunction.OpenUrl:
                    OpenUrl();
                    break;
                case ParsingFunction.InIncrementalRead:
                    if (!InEntity)
                    {
                        _stringBuilder.Append(_ps.chars, _incReadLeftStartPos, _incReadLeftEndPos - _incReadLeftStartPos);
                    }
                    break;
            }

            while (InEntity)
            {
                HandleEntityEnd(true);
            }

            _ps.appendMode = false;
            do
            {
                _stringBuilder.Append(_ps.chars, _ps.charPos, _ps.charsUsed - _ps.charPos);
                _ps.charPos = _ps.charsUsed;
            } while (ReadData() != 0);

            OnEof();

            string remainer = _stringBuilder.ToString();
            _stringBuilder.Length = 0;
            return new StringReader(remainer);
        }

        // Reads the contents of an element including markup into a character buffer. Wellformedness checks are limited.
        // This method is designed to read large streams of embedded text by calling it successively.
        internal int ReadChars(char[] buffer, int index, int count)
        {
            Debug.Assert(_v1Compat, "XmlTextReaderImpl.ReadChars cannot be called on reader created via XmlReader.Create.");
            Debug.Assert(_outerReader is XmlTextReader);

            if (_parsingFunction == ParsingFunction.InIncrementalRead)
            {
                if (_incReadDecoder != _readCharsDecoder)
                { // mixing ReadChars with ReadBase64 or ReadBinHex
                    if (_readCharsDecoder == null)
                    {
                        _readCharsDecoder = new IncrementalReadCharsDecoder();
                    }
                    _readCharsDecoder.Reset();
                    _incReadDecoder = _readCharsDecoder;
                }
                return IncrementalRead(buffer, index, count);
            }
            else
            {
                if (_curNode.type != XmlNodeType.Element)
                {
                    return 0;
                }
                if (_curNode.IsEmptyElement)
                {
                    _outerReader.Read();
                    return 0;
                }

                if (_readCharsDecoder == null)
                {
                    _readCharsDecoder = new IncrementalReadCharsDecoder();
                }

                InitIncrementalRead(_readCharsDecoder);
                return IncrementalRead(buffer, index, count);
            }
        }

        // Reads the contents of an element including markup and base64-decodes it into a byte buffer. Wellformedness checks are limited.
        // This method is designed to read base64-encoded large streams of bytes by calling it successively.
        internal int ReadBase64(byte[] array, int offset, int len)
        {
            Debug.Assert(_v1Compat, "XmlTextReaderImpl.ReadBase64 cannot be called on reader created via XmlReader.Create.");
            Debug.Assert(_outerReader is XmlTextReader);

            if (_parsingFunction == ParsingFunction.InIncrementalRead)
            {
                if (_incReadDecoder != _base64Decoder)
                { // mixing ReadBase64 with ReadChars or ReadBinHex
                    InitBase64Decoder();
                }
                return IncrementalRead(array, offset, len);
            }
            else
            {
                if (_curNode.type != XmlNodeType.Element)
                {
                    return 0;
                }
                if (_curNode.IsEmptyElement)
                {
                    _outerReader.Read();
                    return 0;
                }

                if (_base64Decoder == null)
                {
                    _base64Decoder = new Base64Decoder();
                }

                InitIncrementalRead(_base64Decoder);
                return IncrementalRead(array, offset, len);
            }
        }

        // Reads the contents of an element including markup and binhex-decodes it into a byte buffer. Wellformedness checks are limited.
        // This method is designed to read binhex-encoded large streams of bytes by calling it successively.
        internal int ReadBinHex(byte[] array, int offset, int len)
        {
            Debug.Assert(_v1Compat, "XmlTextReaderImpl.ReadBinHex cannot be called on reader created via XmlReader.Create.");
            Debug.Assert(_outerReader is XmlTextReader);

            if (_parsingFunction == ParsingFunction.InIncrementalRead)
            {
                if (_incReadDecoder != _binHexDecoder)
                { // mixing ReadBinHex with ReadChars or ReadBase64
                    InitBinHexDecoder();
                }
                return IncrementalRead(array, offset, len);
            }
            else
            {
                if (_curNode.type != XmlNodeType.Element)
                {
                    return 0;
                }
                if (_curNode.IsEmptyElement)
                {
                    _outerReader.Read();
                    return 0;
                }

                if (_binHexDecoder == null)
                {
                    _binHexDecoder = new BinHexDecoder();
                }

                InitIncrementalRead(_binHexDecoder);
                return IncrementalRead(array, offset, len);
            }
        }

        //
        // Helpers for DtdParserProxy
        //
        internal XmlNameTable DtdParserProxy_NameTable
        {
            get
            {
                return _nameTable;
            }
        }

        internal IXmlNamespaceResolver? DtdParserProxy_NamespaceResolver
        {
            get
            {
                return _namespaceManager;
            }
        }

        internal bool DtdParserProxy_DtdValidation
        {
            get
            {
                return DtdValidation;
            }
        }

        internal bool DtdParserProxy_Normalization
        {
            get
            {
                return _normalize;
            }
        }

        internal bool DtdParserProxy_Namespaces
        {
            get
            {
                return _supportNamespaces;
            }
        }

        internal bool DtdParserProxy_V1CompatibilityMode
        {
            get
            {
                return _v1Compat;
            }
        }

        internal Uri? DtdParserProxy_BaseUri
        {
            // SxS: ps.baseUri may be initialized in the constructor (public XmlTextReaderImpl( string url, XmlNameTable nt )) based on
            // url provided by the user. Here the property returns ps.BaseUri - so it may expose a path.
            get
            {
                if (_ps.baseUriStr.Length > 0 && _ps.baseUri == null && _xmlResolver != null)
                {
                    _ps.baseUri = _xmlResolver.ResolveUri(null, _ps.baseUriStr);
                }

                return _ps.baseUri;
            }
        }

        internal bool DtdParserProxy_IsEof
        {
            get
            {
                return _ps.isEof;
            }
        }

        internal char[] DtdParserProxy_ParsingBuffer
        {
            get
            {
                return _ps.chars;
            }
        }

        internal int DtdParserProxy_ParsingBufferLength
        {
            get
            {
                return _ps.charsUsed;
            }
        }

        internal int DtdParserProxy_CurrentPosition
        {
            get
            {
                return _ps.charPos;
            }
            set
            {
                Debug.Assert(value >= 0 && value <= _ps.charsUsed);
                _ps.charPos = value;
            }
        }

        internal int DtdParserProxy_EntityStackLength
        {
            get
            {
                return _parsingStatesStackTop + 1;
            }
        }

        internal bool DtdParserProxy_IsEntityEolNormalized
        {
            get
            {
                return _ps.eolNormalized;
            }
        }

        internal IValidationEventHandling? DtdParserProxy_ValidationEventHandling
        {
            get
            {
                return _validationEventHandling;
            }
            set
            {
                _validationEventHandling = value;
            }
        }

        internal void DtdParserProxy_OnNewLine(int pos)
        {
            this.OnNewLine(pos);
        }

        internal int DtdParserProxy_LineNo
        {
            get
            {
                return _ps.LineNo;
            }
        }

        internal int DtdParserProxy_LineStartPosition
        {
            get
            {
                return _ps.lineStartPos;
            }
        }

        internal int DtdParserProxy_ReadData()
        {
            return this.ReadData();
        }

        internal int DtdParserProxy_ParseNumericCharRef(StringBuilder? internalSubsetBuilder)
        {
            return this.ParseNumericCharRef(true, internalSubsetBuilder, out _);
        }

        internal int DtdParserProxy_ParseNamedCharRef(bool expand, StringBuilder? internalSubsetBuilder)
        {
            return this.ParseNamedCharRef(expand, internalSubsetBuilder);
        }

        internal void DtdParserProxy_ParsePI(StringBuilder? sb)
        {
            if (sb == null)
            {
                ParsingMode pm = _parsingMode;
                _parsingMode = ParsingMode.SkipNode;
                ParsePI(null);
                _parsingMode = pm;
            }
            else
            {
                ParsePI(sb);
            }
        }

        internal void DtdParserProxy_ParseComment(StringBuilder? sb)
        {
            Debug.Assert(_parsingMode == ParsingMode.Full);

            try
            {
                if (sb == null)
                {
                    ParsingMode savedParsingMode = _parsingMode;
                    _parsingMode = ParsingMode.SkipNode;
                    ParseCDataOrComment(XmlNodeType.Comment);
                    _parsingMode = savedParsingMode;
                }
                else
                {
                    NodeData originalCurNode = _curNode;

                    _curNode = AddNode(_index + _attrCount + 1, _index);
                    ParseCDataOrComment(XmlNodeType.Comment);
                    _curNode.CopyTo(0, sb);

                    _curNode = originalCurNode;
                }
            }
            catch (XmlException e)
            {
                if (e.ResString == SR.Xml_UnexpectedEOF && _ps.entity != null)
                {
                    SendValidationEvent(XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, null, _ps.LineNo, _ps.LinePos);
                }
                else
                {
                    throw;
                }
            }
        }

        private bool IsResolverNull
        {
            get
            {
                return _xmlResolver == null || !_xmlResolverIsSet;
            }
        }

        private XmlResolver GetTempResolver()
        {
            return _xmlResolver == null ? new XmlUrlResolver() : _xmlResolver;
        }

        internal bool DtdParserProxy_PushEntity(IDtdEntityInfo entity, out int entityId)
        {
            bool retValue;
            if (entity.IsExternal)
            {
                if (IsResolverNull)
                {
                    entityId = -1;
                    return false;
                }
                retValue = PushExternalEntity(entity);
            }
            else
            {
                PushInternalEntity(entity);
                retValue = true;
            }
            entityId = _ps.entityId;
            return retValue;
        }

        internal bool DtdParserProxy_PopEntity(out IDtdEntityInfo? oldEntity, out int newEntityId)
        {
            if (_parsingStatesStackTop == -1)
            {
                oldEntity = null;
                newEntityId = -1;
                return false;
            }

            oldEntity = _ps.entity;
            PopEntity();
            newEntityId = _ps.entityId;
            return true;
        }

        // SxS: The caller did not provide any SxS sensitive name or resource. No resource is being exposed either.
        // It is OK to suppress SxS warning.
        internal bool DtdParserProxy_PushExternalSubset(string? systemId, string? publicId)
        {
            Debug.Assert(_parsingStatesStackTop == -1);
            Debug.Assert((systemId != null && systemId.Length > 0) || (publicId != null && publicId.Length > 0));

            if (IsResolverNull)
            {
                return false;
            }

            Debug.Assert(_xmlResolver != null);

            if (_ps.baseUri == null && !string.IsNullOrEmpty(_ps.baseUriStr))
            {
                _ps.baseUri = _xmlResolver.ResolveUri(null, _ps.baseUriStr);
            }

            PushExternalEntityOrSubset(publicId, systemId, _ps.baseUri, null);

            _ps.entity = null;
            _ps.entityId = 0;

            Debug.Assert(_ps.appendMode);
            int initialPos = _ps.charPos;
            if (_v1Compat)
            {
                EatWhitespaces(null);
            }
            if (!ParseXmlDeclaration(true))
            {
                _ps.charPos = initialPos;
            }

            return true;
        }

        internal void DtdParserProxy_PushInternalDtd(string baseUri, string internalDtd)
        {
            Debug.Assert(_parsingStatesStackTop == -1);
            Debug.Assert(internalDtd != null);

            PushParsingState();

            RegisterConsumedCharacters(internalDtd.Length, false);
            InitStringInput(baseUri, Encoding.Unicode, internalDtd);

            _ps.entity = null;
            _ps.entityId = 0;
            _ps.eolNormalized = false;
        }

        [DoesNotReturn]
        internal void DtdParserProxy_Throw(Exception e)
        {
            this.Throw(e);
        }

        internal void DtdParserProxy_OnSystemId(string systemId, LineInfo keywordLineInfo, LineInfo systemLiteralLineInfo)
        {
            NodeData attr = AddAttributeNoChecks("SYSTEM", _index + 1);
            attr.SetValue(systemId);
            attr.lineInfo = keywordLineInfo;
            attr.lineInfo2 = systemLiteralLineInfo;
        }

        internal void DtdParserProxy_OnPublicId(string publicId, LineInfo keywordLineInfo, LineInfo publicLiteralLineInfo)
        {
            NodeData attr = AddAttributeNoChecks("PUBLIC", _index + 1);
            attr.SetValue(publicId);
            attr.lineInfo = keywordLineInfo;
            attr.lineInfo2 = publicLiteralLineInfo;
        }

        //
        // Throw methods: Sets the reader current position to pos, sets the error state and throws exception
        //
        [DoesNotReturn]
        private void Throw(int pos, string res, string arg)
        {
            _ps.charPos = pos;
            Throw(res, arg);
        }

        [DoesNotReturn]
        private void Throw(int pos, string res, string[] args)
        {
            _ps.charPos = pos;
            Throw(res, args);
        }

        [DoesNotReturn]
        private void Throw(int pos, string res)
        {
            _ps.charPos = pos;
            Throw(res, string.Empty);
        }

        [DoesNotReturn]
        private void Throw(string res)
        {
            Throw(res, string.Empty);
        }

        [DoesNotReturn]
        private void Throw(string res, int lineNo, int linePos)
        {
            Throw(new XmlException(res, string.Empty, lineNo, linePos, _ps.baseUriStr));
        }

        [DoesNotReturn]
        private void Throw(string res, string? arg)
        {
            Throw(new XmlException(res, arg, _ps.LineNo, _ps.LinePos, _ps.baseUriStr));
        }

        [DoesNotReturn]
        private void Throw(string res, string? arg, int lineNo, int linePos)
        {
            Throw(new XmlException(res, arg, lineNo, linePos, _ps.baseUriStr));
        }

        [DoesNotReturn]
        private void Throw(string res, string?[] args)
        {
            Throw(new XmlException(res, args, _ps.LineNo, _ps.LinePos, _ps.baseUriStr));
        }

        [DoesNotReturn]
        private void Throw(string res, string? arg, Exception innerException)
        {
            Throw(res, new string?[] { arg }, innerException);
        }

        [DoesNotReturn]
        private void Throw(string res, string?[] args, Exception innerException)
        {
            Throw(new XmlException(res, args, innerException, _ps.LineNo, _ps.LinePos, _ps.baseUriStr));
        }

        [DoesNotReturn]
        private void Throw(Exception e)
        {
            SetErrorState();
            XmlException? xmlEx = e as XmlException;

            if (xmlEx != null)
            {
                _curNode.SetLineInfo(xmlEx.LineNumber, xmlEx.LinePosition);
            }

            throw e;
        }

        [DoesNotReturn]
        private void ReThrow(Exception e, int lineNo, int linePos)
        {
            Throw(new XmlException(e.Message, (Exception?)null, lineNo, linePos, _ps.baseUriStr));
        }

        [DoesNotReturn]
        private void ThrowWithoutLineInfo(string res)
        {
            Throw(new XmlException(res, string.Empty, _ps.baseUriStr));
        }

        [DoesNotReturn]
        private void ThrowWithoutLineInfo(string res, string arg)
        {
            Throw(new XmlException(res, arg, _ps.baseUriStr));
        }

        [DoesNotReturn]
        private void ThrowWithoutLineInfo(string res, string?[] args, Exception? innerException)
        {
            Throw(new XmlException(res, args, innerException, 0, 0, _ps.baseUriStr));
        }

        [DoesNotReturn]
        private void ThrowInvalidChar(char[] data, int length, int invCharPos)
        {
            Throw(invCharPos, SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(data, length, invCharPos));
        }

        private void SetErrorState()
        {
            _parsingFunction = ParsingFunction.Error;
            _readState = ReadState.Error;
        }

        private void SendValidationEvent(XmlSeverityType severity, string code, string? arg, int lineNo, int linePos)
        {
            SendValidationEvent(severity, new XmlSchemaException(code, arg, _ps.baseUriStr, lineNo, linePos));
        }

        private void SendValidationEvent(XmlSeverityType severity, XmlSchemaException exception)
        {
            if (_validationEventHandling != null)
            {
                _validationEventHandling.SendEvent(exception, severity);
            }
        }

        //
        // Private implementation methods & properties
        //
        private bool InAttributeValueIterator
        {
            get
            {
                return _attrCount > 0 && _parsingFunction >= ParsingFunction.InReadAttributeValue;
            }
        }

        private void FinishAttributeValueIterator()
        {
            Debug.Assert(InAttributeValueIterator);
            if (_parsingFunction == ParsingFunction.InReadValueChunk)
            {
                FinishReadValueChunk();
            }
            else if (_parsingFunction == ParsingFunction.InReadContentAsBinary)
            {
                FinishReadContentAsBinary();
            }

            if (_parsingFunction == ParsingFunction.InReadAttributeValue)
            {
                while (_ps.entityId != _attributeValueBaseEntityId)
                {
                    HandleEntityEnd(false);
                }
                _emptyEntityInAttributeResolved = false;
                _parsingFunction = _nextParsingFunction;
                _nextParsingFunction = (_index > 0) ? ParsingFunction.ElementContent : ParsingFunction.DocumentContent;
            }
        }

        private bool DtdValidation
        {
            get
            {
                return _validationEventHandling != null;
            }
        }

        private void InitStreamInput(Stream stream, Encoding? encoding)
        {
            InitStreamInput(null, string.Empty, stream, null, 0, encoding);
        }

        private void InitStreamInput(string baseUriStr, Stream stream, Encoding? encoding)
        {
            Debug.Assert(baseUriStr != null);
            InitStreamInput(null, baseUriStr, stream, null, 0, encoding);
        }

        private void InitStreamInput(Uri? baseUri, Stream stream, Encoding? encoding)
        {
            Debug.Assert(baseUri != null);
            InitStreamInput(baseUri, baseUri.ToString(), stream, null, 0, encoding);
        }

        private void InitStreamInput(Uri? baseUri, string baseUriStr, Stream stream, Encoding? encoding)
        {
            InitStreamInput(baseUri, baseUriStr, stream, null, 0, encoding);
        }

        private void InitStreamInput(Uri? baseUri, string baseUriStr, Stream stream, byte[]? bytes, int byteCount, Encoding? encoding)
        {
            Debug.Assert(_ps.charPos == 0 && _ps.charsUsed == 0 && _ps.textReader == null);
            Debug.Assert(baseUriStr != null);
            Debug.Assert(baseUri == null || (baseUri.ToString().Equals(baseUriStr)));

            _ps.stream = stream;
            _ps.baseUri = baseUri;
            _ps.baseUriStr = baseUriStr;

            // take over the byte buffer allocated in XmlReader.Create, if available
            int bufferSize;
            if (bytes != null)
            {
                _ps.bytes = bytes;
                _ps.bytesUsed = byteCount;
                bufferSize = _ps.bytes.Length;
            }
            else
            {
                // allocate the byte buffer
                if (_laterInitParam != null && _laterInitParam.useAsync)
                {
                    bufferSize = AsyncBufferSize;
                }
                else
                {
                    bufferSize = XmlReader.CalcBufferSize(stream);
                }
                if (_ps.bytes == null || _ps.bytes.Length < bufferSize)
                {
                    _ps.bytes = new byte[bufferSize];
                }
            }

            // allocate char buffer
            if (_ps.chars == null || _ps.chars.Length < bufferSize + 1)
            {
                _ps.chars = new char[bufferSize + 1];
            }

            // make sure we have at least 4 bytes to detect the encoding (no preamble of System.Text supported encoding is longer than 4 bytes)
            _ps.bytePos = 0;
            while (_ps.bytesUsed < 4 && _ps.bytes.Length - _ps.bytesUsed > 0)
            {
                int read = stream.Read(_ps.bytes, _ps.bytesUsed, _ps.bytes.Length - _ps.bytesUsed);
                if (read == 0)
                {
                    _ps.isStreamEof = true;
                    break;
                }
                _ps.bytesUsed += read;
            }

            // detect & setup encoding
            if (encoding == null)
            {
                encoding = DetectEncoding();
            }

            SetupEncoding(encoding);

            // eat preamble
            EatPreamble();

            _documentStartBytePos = _ps.bytePos;

            _ps.eolNormalized = !_normalize;

            // decode first characters
            _ps.appendMode = true;
            ReadData();
        }

        private void InitTextReaderInput(string baseUriStr, TextReader input)
        {
            InitTextReaderInput(baseUriStr, null, input);
        }

        private void InitTextReaderInput(string baseUriStr, Uri? baseUri, TextReader input)
        {
            Debug.Assert(_ps.charPos == 0 && _ps.charsUsed == 0 && _ps.stream == null);
            Debug.Assert(baseUriStr != null);

            _ps.textReader = input;
            _ps.baseUriStr = baseUriStr;
            _ps.baseUri = baseUri;

            if (_ps.chars == null)
            {
                if (_laterInitParam != null && _laterInitParam.useAsync)
                {
                    _ps.chars = new char[XmlReader.AsyncBufferSize + 1];
                }
                else
                {
                    _ps.chars = new char[XmlReader.DefaultBufferSize + 1];
                }
            }

            _ps.encoding = Encoding.Unicode;
            _ps.eolNormalized = !_normalize;

            // read first characters
            _ps.appendMode = true;
            ReadData();
        }

        private void InitStringInput(string baseUriStr, Encoding? originalEncoding, string str)
        {
            Debug.Assert(_ps.stream == null && _ps.textReader == null);
            Debug.Assert(_ps.charPos == 0 && _ps.charsUsed == 0);
            Debug.Assert(baseUriStr != null);
            Debug.Assert(str != null);

            _ps.baseUriStr = baseUriStr;
            _ps.baseUri = null;

            int len = str.Length;
            _ps.chars = new char[len + 1];
            str.CopyTo(0, _ps.chars, 0, str.Length);
            _ps.charsUsed = len;
            _ps.chars[len] = (char)0;

            _ps.encoding = originalEncoding;

            _ps.eolNormalized = !_normalize;
            _ps.isEof = true;
        }

        private void InitFragmentReader(XmlNodeType fragmentType, XmlParserContext? parserContext, bool allowXmlDeclFragment)
        {
            _fragmentParserContext = parserContext;

            if (parserContext != null)
            {
                if (parserContext.NamespaceManager != null)
                {
                    _namespaceManager = parserContext.NamespaceManager;
                    _xmlContext.defaultNamespace = _namespaceManager.LookupNamespace(string.Empty)!;
                }
                else
                {
                    _namespaceManager = new XmlNamespaceManager(_nameTable);
                }

                _ps.baseUriStr = parserContext.BaseURI;
                _ps.baseUri = null;
                _xmlContext.xmlLang = parserContext.XmlLang;
                _xmlContext.xmlSpace = parserContext.XmlSpace;
            }
            else
            {
                _namespaceManager = new XmlNamespaceManager(_nameTable);
                _ps.baseUriStr = string.Empty;
                _ps.baseUri = null;
            }

            _reportedBaseUri = _ps.baseUriStr;

            switch (fragmentType)
            {
                case XmlNodeType.Attribute:
                    _ps.appendMode = false;
                    _parsingFunction = ParsingFunction.SwitchToInteractive;
                    _nextParsingFunction = ParsingFunction.FragmentAttribute;
                    break;
                case XmlNodeType.Element:
                    Debug.Assert(_parsingFunction == ParsingFunction.SwitchToInteractiveXmlDecl);
                    _nextParsingFunction = ParsingFunction.DocumentContent;
                    break;
                case XmlNodeType.Document:
                    Debug.Assert(_parsingFunction == ParsingFunction.SwitchToInteractiveXmlDecl);
                    Debug.Assert(_nextParsingFunction == ParsingFunction.DocumentContent);
                    break;
                case XmlNodeType.XmlDeclaration:
                    if (allowXmlDeclFragment)
                    {
                        _ps.appendMode = false;
                        _parsingFunction = ParsingFunction.SwitchToInteractive;
                        _nextParsingFunction = ParsingFunction.XmlDeclarationFragment;
                        break;
                    }
                    else
                    {
                        goto default;
                    }
                default:
                    Throw(SR.Xml_PartialContentNodeTypeNotSupportedEx, fragmentType.ToString());
                    return;
            }

            _fragmentType = fragmentType;
            _fragment = true;
        }

        private void ProcessDtdFromParserContext(XmlParserContext context)
        {
            Debug.Assert(context != null && context.HasDtdInfo);

            switch (_dtdProcessing)
            {
                case DtdProcessing.Prohibit:
                    ThrowWithoutLineInfo(SR.Xml_DtdIsProhibitedEx);
                    break;
                case DtdProcessing.Ignore:
                    // do nothing
                    break;
                case DtdProcessing.Parse:
                    ParseDtdFromParserContext();
                    break;
                default:
                    Debug.Fail("Unhandled DtdProcessing enumeration value.");
                    break;
            }
        }

        // SxS: This method resolve Uri but does not expose it to the caller. It's OK to suppress the warning.
        private void OpenUrl()
        {
            Debug.Assert(_url != null && _url.Length > 0);

            // It is safe to use the resolver here as we don't resolve or expose any DTD to the caller
            XmlResolver tmpResolver = GetTempResolver();
            if (_ps.baseUri == null)
            {
                _ps.baseUri = tmpResolver.ResolveUri(null, _url);
                _ps.baseUriStr = _ps.baseUri.ToString();
            }

            try
            {
                _ps.stream = (Stream?)tmpResolver.GetEntity(_ps.baseUri, null, typeof(Stream));
            }
            catch
            {
                SetErrorState();
                throw;
            }

            if (_ps.stream == null)
            {
                ThrowWithoutLineInfo(SR.Xml_CannotResolveUrl, _ps.baseUriStr);
            }

            Debug.Assert(_ps.stream != null);
            InitStreamInput(_ps.baseUri, _ps.baseUriStr, _ps.stream, null);
            _reportedEncoding = _ps.encoding;
        }

        // Stream input only: detect encoding from the first 4 bytes of the byte buffer starting at ps.bytes[ps.bytePos]
        private Encoding? DetectEncoding()
        {
            Debug.Assert(_ps.bytes != null);
            Debug.Assert(_ps.bytePos == 0);

            if (_ps.bytesUsed < 2)
            {
                return null;
            }

            int first2Bytes = _ps.bytes[0] << 8 | _ps.bytes[1];
            int next2Bytes = (_ps.bytesUsed >= 4) ? (_ps.bytes[2] << 8 | _ps.bytes[3]) : 0;

            switch (first2Bytes)
            {
                // Removing USC4 encoding
                case 0x0000:
                    switch (next2Bytes)
                    {
                        case 0xFEFF:
                            return Ucs4Encoding.UCS4_Bigendian;
                        case 0x003C:
                            return Ucs4Encoding.UCS4_Bigendian;
                        case 0xFFFE:
                            return Ucs4Encoding.UCS4_2143;
                        case 0x3C00:
                            return Ucs4Encoding.UCS4_2143;
                    }
                    break;
                case 0xFEFF:
                    if (next2Bytes == 0x0000)
                    {
                        return Ucs4Encoding.UCS4_3412;
                    }
                    else
                    {
                        return Encoding.BigEndianUnicode;
                    }
                case 0xFFFE:
                    if (next2Bytes == 0x0000)
                    {
                        return Ucs4Encoding.UCS4_Littleendian;
                    }
                    else
                    {
                        return Encoding.Unicode;
                    }
                case 0x3C00:
                    if (next2Bytes == 0x0000)
                    {
                        return Ucs4Encoding.UCS4_Littleendian;
                    }
                    else
                    {
                        return Encoding.Unicode;
                    }
                case 0x003C:
                    if (next2Bytes == 0x0000)
                    {
                        return Ucs4Encoding.UCS4_3412;
                    }
                    else
                    {
                        return Encoding.BigEndianUnicode;
                    }
                case 0x4C6F:
                    if (next2Bytes == 0xA794)
                    {
                        Throw(SR.Xml_UnknownEncoding, "ebcdic");
                    }
                    break;
                case 0xEFBB:
                    if ((next2Bytes & 0xFF00) == 0xBF00)
                    {
                        return new UTF8Encoding(true, true);
                    }
                    break;
            }
            // Default encoding is ASCII (using SafeAsciiDecoder) until we read xml declaration.
            // If we set UTF8 encoding now, it will throw exceptions (=slow) when decoding non-UTF8-friendly
            // characters after the xml declaration, which may be perfectly valid in the encoding
            // specified in xml declaration.
            return null;
        }

        private void SetupEncoding(Encoding? encoding)
        {
            if (encoding == null)
            {
                Debug.Assert(_ps.charPos == 0);
                _ps.encoding = Encoding.UTF8;
                _ps.decoder = new SafeAsciiDecoder();
            }
            else
            {
                _ps.encoding = encoding;
                _ps.decoder = _ps.encoding.WebName switch // Encoding.Codepage is not supported in Silverlight
                {
                    "utf-16" => new UTF16Decoder(false),
                    "utf-16BE" => new UTF16Decoder(true),
                    _ => encoding.GetDecoder(),
                };
            }
        }

        private void EatPreamble()
        {
            Debug.Assert(_ps.encoding != null);
            Debug.Assert(_ps.bytes != null);
            ReadOnlySpan<byte> preamble = _ps.encoding.Preamble;
            int preambleLen = preamble.Length;
            int i;
            for (i = 0; i < preambleLen && i < _ps.bytesUsed; i++)
            {
                if (_ps.bytes[i] != preamble[i])
                {
                    break;
                }
            }
            if (i == preambleLen)
            {
                _ps.bytePos = preambleLen;
            }
        }

        // Switches the reader's encoding
        private void SwitchEncoding(Encoding newEncoding)
        {
            Debug.Assert(_ps.encoding != null);
            if ((newEncoding.WebName != _ps.encoding.WebName || _ps.decoder is SafeAsciiDecoder) && !_afterResetState)
            {
                Debug.Assert(_ps.stream != null);
                UnDecodeChars();
                _ps.appendMode = false;
                SetupEncoding(newEncoding);
                ReadData();
            }
        }

        // Returns the Encoding object for the given encoding name, if the reader's encoding can be switched to that encoding.
        // Performs checks whether switching from current encoding to specified encoding is allowed.
        private Encoding CheckEncoding(string newEncodingName)
        {
            Debug.Assert(_ps.encoding != null);

            // encoding can be switched on stream input only
            if (_ps.stream == null)
            {
                return _ps.encoding;
            }

            if (string.Equals(newEncodingName, "ucs-2", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(newEncodingName, "utf-16", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(newEncodingName, "iso-10646-ucs-2", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(newEncodingName, "ucs-4", StringComparison.OrdinalIgnoreCase))
            {
                if (_ps.encoding.WebName != "utf-16BE" &&
                     _ps.encoding.WebName != "utf-16" &&
                     !string.Equals(newEncodingName, "ucs-4", StringComparison.OrdinalIgnoreCase))
                {
                    if (_afterResetState)
                    {
                        Throw(SR.Xml_EncodingSwitchAfterResetState, newEncodingName);
                    }
                    else
                    {
                        ThrowWithoutLineInfo(SR.Xml_MissingByteOrderMark);
                    }
                }
                return _ps.encoding;
            }

            Encoding? newEncoding = null;
            if (string.Equals(newEncodingName, "utf-8", StringComparison.OrdinalIgnoreCase))
            {
                newEncoding = UTF8BomThrowing;
            }
            else
            {
                try
                {
                    newEncoding = Encoding.GetEncoding(newEncodingName);
                }
                catch (NotSupportedException innerEx)
                {
                    Throw(SR.Xml_UnknownEncoding, newEncodingName, innerEx);
                }
                catch (ArgumentException innerEx)
                {
                    Throw(SR.Xml_UnknownEncoding, newEncodingName, innerEx);
                }

                Debug.Assert(newEncoding != null);
                Debug.Assert(newEncoding.EncodingName != "UTF-8");
            }

            // check for invalid encoding switches after ResetState
            if (_afterResetState && _ps.encoding.WebName != newEncoding.WebName)
            {
                Throw(SR.Xml_EncodingSwitchAfterResetState, newEncodingName);
            }

            return newEncoding;
        }

        private void UnDecodeChars()
        {
            Debug.Assert(_ps.stream != null && _ps.decoder != null && _ps.bytes != null);
            Debug.Assert(_ps.appendMode, "UnDecodeChars cannot be called after ps.appendMode has been changed to false");

            Debug.Assert(_ps.charsUsed >= _ps.charPos, "The current position must be in the valid character range.");
            if (_maxCharactersInDocument > 0)
            {
                // We're returning back in the input (potentially) so we need to fixup
                //   the character counters to avoid counting some of them twice.
                // The following code effectively rolls-back all decoded characters
                //   after the ps.charPos (which typically points to the first character
                //   after the XML decl).
                Debug.Assert(_charactersInDocument >= _ps.charsUsed - _ps.charPos,
                    "We didn't correctly count some of the decoded characters against the MaxCharactersInDocument.");
                _charactersInDocument -= _ps.charsUsed - _ps.charPos;
            }

            if (_maxCharactersFromEntities > 0)
            {
                if (InEntity)
                {
                    Debug.Assert(_charactersFromEntities >= _ps.charsUsed - _ps.charPos,
                        "We didn't correctly count some of the decoded characters against the MaxCharactersFromEntities.");
                    _charactersFromEntities -= _ps.charsUsed - _ps.charPos;
                }
            }

            _ps.bytePos = _documentStartBytePos; // byte position after preamble
            if (_ps.charPos > 0)
            {
                Debug.Assert(_ps.encoding != null);
                Debug.Assert(_ps.chars != null);
                _ps.bytePos += _ps.encoding.GetByteCount(_ps.chars, 0, _ps.charPos);
            }

            _ps.charsUsed = _ps.charPos;
            _ps.isEof = false;
        }

        private void SwitchEncodingToUTF8()
        {
            SwitchEncoding(UTF8BomThrowing);
        }

        // Reads more data to the character buffer, discarding already parsed chars / decoded bytes.
        private int ReadData()
        {
            // Append Mode:  Append new bytes and characters to the buffers, do not rewrite them. Allocate new buffers
            //               if the current ones are full
            // Rewrite Mode: Reuse the buffers. If there is less than half of the char buffer left for new data, move
            //               the characters that has not been parsed yet to the front of the buffer. Same for bytes.

            if (_ps.isEof)
            {
                return 0;
            }

            Debug.Assert(_ps.chars != null);
            int charsRead;
            if (_ps.appendMode)
            {
                // the character buffer is full -> allocate a new one
                if (_ps.charsUsed == _ps.chars.Length - 1)
                {
                    // invalidate node values kept in buffer - applies to attribute values only
                    for (int i = 0; i < _attrCount; i++)
                    {
                        _nodes[_index + i + 1].OnBufferInvalidated();
                    }

                    char[] newChars = new char[_ps.chars.Length * 2];
                    BlockCopyChars(_ps.chars, 0, newChars, 0, _ps.chars.Length);
                    _ps.chars = newChars;
                }

                if (_ps.stream != null)
                {
                    // the byte buffer is full -> allocate a new one
                    if (_ps.bytesUsed - _ps.bytePos < MaxByteSequenceLen)
                    {
                        Debug.Assert(_ps.bytes != null);
                        if (_ps.bytes.Length - _ps.bytesUsed < MaxByteSequenceLen)
                        {
                            byte[] newBytes = new byte[_ps.bytes.Length * 2];
                            BlockCopy(_ps.bytes, 0, newBytes, 0, _ps.bytesUsed);
                            _ps.bytes = newBytes;
                        }
                    }
                }

                charsRead = _ps.chars.Length - _ps.charsUsed - 1;
                if (charsRead > ApproxXmlDeclLength)
                {
                    charsRead = ApproxXmlDeclLength;
                }
            }
            else
            {
                int charsLen = _ps.chars.Length;
                if (charsLen - _ps.charsUsed <= charsLen / 2)
                {
                    // invalidate node values kept in buffer - applies to attribute values only
                    for (int i = 0; i < _attrCount; i++)
                    {
                        _nodes[_index + i + 1].OnBufferInvalidated();
                    }

                    // move unparsed characters to front, unless the whole buffer contains unparsed characters
                    int copyCharsCount = _ps.charsUsed - _ps.charPos;
                    if (copyCharsCount < charsLen - 1)
                    {
                        _ps.lineStartPos = _ps.lineStartPos - _ps.charPos;
                        if (copyCharsCount > 0)
                        {
                            BlockCopyChars(_ps.chars, _ps.charPos, _ps.chars, 0, copyCharsCount);
                        }
                        _ps.charPos = 0;
                        _ps.charsUsed = copyCharsCount;
                    }
                    else
                    {
                        char[] newChars = new char[_ps.chars.Length * 2];
                        BlockCopyChars(_ps.chars, 0, newChars, 0, _ps.chars.Length);
                        _ps.chars = newChars;
                    }
                }

                if (_ps.stream != null)
                {
                    // move undecoded bytes to the front to make some space in the byte buffer
                    int bytesLeft = _ps.bytesUsed - _ps.bytePos;
                    if (bytesLeft <= MaxBytesToMove)
                    {
                        if (bytesLeft == 0)
                        {
                            _ps.bytesUsed = 0;
                        }
                        else
                        {
                            Debug.Assert(_ps.bytes != null);
                            BlockCopy(_ps.bytes, _ps.bytePos, _ps.bytes, 0, bytesLeft);
                            _ps.bytesUsed = bytesLeft;
                        }

                        _ps.bytePos = 0;
                    }
                }
                charsRead = _ps.chars.Length - _ps.charsUsed - 1;
            }

            if (_ps.stream != null)
            {
                if (!_ps.isStreamEof)
                {
                    Debug.Assert(_ps.bytes != null);

                    // read new bytes
                    if (_ps.bytePos == _ps.bytesUsed && _ps.bytes.Length - _ps.bytesUsed > 0)
                    {
                        int read = _ps.stream.Read(_ps.bytes, _ps.bytesUsed, _ps.bytes.Length - _ps.bytesUsed);
                        if (read == 0)
                        {
                            _ps.isStreamEof = true;
                        }
                        _ps.bytesUsed += read;
                    }
                }

                int originalBytePos = _ps.bytePos;

                // decode chars
                charsRead = GetChars(charsRead);
                if (charsRead == 0 && _ps.bytePos != originalBytePos)
                {
                    // GetChars consumed some bytes but it was not enough bytes to form a character -> try again
                    return ReadData();
                }
            }
            else if (_ps.textReader != null)
            {
                // read chars
                charsRead = _ps.textReader.Read(_ps.chars, _ps.charsUsed, _ps.chars.Length - _ps.charsUsed - 1);
                _ps.charsUsed += charsRead;
            }
            else
            {
                charsRead = 0;
            }

            RegisterConsumedCharacters(charsRead, InEntity);

            if (charsRead == 0)
            {
                Debug.Assert(_ps.charsUsed < _ps.chars.Length);
                _ps.isEof = true;
            }

            _ps.chars[_ps.charsUsed] = (char)0;
            return charsRead;
        }

        // Stream input only: read bytes from stream and decodes them according to the current encoding
        private int GetChars(int maxCharsCount)
        {
            Debug.Assert(_ps.stream != null && _ps.decoder != null && _ps.bytes != null);
            Debug.Assert(_ps.chars != null);
            Debug.Assert(maxCharsCount <= _ps.chars.Length - _ps.charsUsed - 1);

            // determine the maximum number of bytes we can pass to the decoder
            int bytesCount = _ps.bytesUsed - _ps.bytePos;
            if (bytesCount == 0)
            {
                return 0;
            }

            int charsCount;
            bool completed;
            try
            {
                // decode chars
                _ps.decoder.Convert(_ps.bytes, _ps.bytePos, bytesCount, _ps.chars, _ps.charsUsed, maxCharsCount, false, out bytesCount, out charsCount, out completed);
            }
            catch (ArgumentException)
            {
                InvalidCharRecovery(ref bytesCount, out charsCount);
            }

            // move pointers and return
            _ps.bytePos += bytesCount;
            _ps.charsUsed += charsCount;
            Debug.Assert(maxCharsCount >= charsCount);
            return charsCount;
        }

        private void InvalidCharRecovery(ref int bytesCount, out int charsCount)
        {
            int charsDecoded = 0;
            int bytesDecoded = 0;
            try
            {
                while (bytesDecoded < bytesCount)
                {
                    int chDec;
                    int bDec;
                    bool completed;
                    Debug.Assert(_ps.decoder != null);
                    Debug.Assert(_ps.bytes != null);
                    Debug.Assert(_ps.chars != null);
                    _ps.decoder.Convert(_ps.bytes, _ps.bytePos + bytesDecoded, 1, _ps.chars, _ps.charsUsed + charsDecoded, 2, false, out bDec, out chDec, out completed);
                    charsDecoded += chDec;
                    bytesDecoded += bDec;
                }

                Debug.Fail("We should get an exception again.");
            }
            catch (ArgumentException)
            {
            }

            if (charsDecoded == 0)
            {
                Throw(_ps.charsUsed, SR.Xml_InvalidCharInThisEncoding);
            }

            charsCount = charsDecoded;
            bytesCount = bytesDecoded;
        }

        internal void Close(bool closeInput)
        {
            if (_parsingFunction == ParsingFunction.ReaderClosed)
            {
                return;
            }

            while (InEntity)
            {
                PopParsingState();
            }

            _ps.Close(closeInput);

            _curNode = NodeData.None;
            _parsingFunction = ParsingFunction.ReaderClosed;
            _reportedEncoding = null;
            _reportedBaseUri = string.Empty;
            _readState = ReadState.Closed;
            _fullAttrCleanup = false;
            ResetAttributes();

            _laterInitParam = null;
        }

        private void ShiftBuffer(int sourcePos, int destPos, int count)
        {
            Debug.Assert(_ps.chars != null);
            BlockCopyChars(_ps.chars, sourcePos, _ps.chars, destPos, count);
        }

        // Parses the xml or text declaration and switched encoding if needed
        private bool ParseXmlDeclaration(bool isTextDecl)
        {
            while (_ps.charsUsed - _ps.charPos < 6)
            {  // minimum "<?xml "
                if (ReadData() == 0)
                {
                    goto NoXmlDecl;
                }
            }

            if (!XmlConvert.StrEqual(_ps.chars, _ps.charPos, 5, XmlDeclarationBeginning) ||
                 XmlCharType.IsNameSingleChar(_ps.chars![_ps.charPos + 5]))
            {
                goto NoXmlDecl;
            }

            if (!isTextDecl)
            {
                _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos + 2);
                _curNode.SetNamedNode(XmlNodeType.XmlDeclaration, _xml);
            }

            _ps.charPos += 5;

            // parsing of text declarations cannot change global stringBuidler or curNode as we may be in the middle of a text node
            Debug.Assert(_stringBuilder.Length == 0 || isTextDecl);
            StringBuilder sb = isTextDecl ? new StringBuilder() : _stringBuilder;

            // parse version, encoding & standalone attributes
            int xmlDeclState = 0;   // <?xml (0) version='1.0' (1) encoding='__' (2) standalone='__' (3) ?>
            Encoding? encoding = null;

            while (true)
            {
                int originalSbLen = sb.Length;
                int wsCount = EatWhitespaces(xmlDeclState == 0 ? null : sb);

                // end of xml declaration
                if (_ps.chars[_ps.charPos] == '?')
                {
                    sb.Length = originalSbLen;

                    if (_ps.chars[_ps.charPos + 1] == '>')
                    {
                        if (xmlDeclState == 0)
                        {
                            Throw(isTextDecl ? SR.Xml_InvalidTextDecl : SR.Xml_InvalidXmlDecl);
                        }

                        _ps.charPos += 2;
                        if (!isTextDecl)
                        {
                            _curNode.SetValue(sb.ToString());
                            sb.Length = 0;

                            _nextParsingFunction = _parsingFunction;
                            _parsingFunction = ParsingFunction.ResetAttributesRootLevel;
                        }

                        // switch to encoding specified in xml declaration
                        if (encoding == null)
                        {
                            if (isTextDecl)
                            {
                                Throw(SR.Xml_InvalidTextDecl);
                            }
                            if (_afterResetState)
                            {
                                // check for invalid encoding switches to default encoding
                                Debug.Assert(_ps.encoding != null);
                                string encodingName = _ps.encoding.WebName;
                                if (encodingName != "utf-8" && encodingName != "utf-16" &&
                                     encodingName != "utf-16BE" && !(_ps.encoding is Ucs4Encoding))
                                {
                                    Throw(SR.Xml_EncodingSwitchAfterResetState, (_ps.encoding.GetByteCount("A") == 1) ? "UTF-8" : "UTF-16");
                                }
                            }
                            if (_ps.decoder is SafeAsciiDecoder)
                            {
                                SwitchEncodingToUTF8();
                            }
                        }
                        else
                        {
                            SwitchEncoding(encoding);
                        }
                        _ps.appendMode = false;
                        return true;
                    }
                    else if (_ps.charPos + 1 == _ps.charsUsed)
                    {
                        goto ReadData;
                    }
                    else
                    {
                        ThrowUnexpectedToken("'>'");
                    }
                }

                if (wsCount == 0 && xmlDeclState != 0)
                {
                    ThrowUnexpectedToken("?>");
                }

                // read attribute name
                int nameEndPos = ParseName();

                NodeData? attr = null;
                switch (_ps.chars[_ps.charPos])
                {
                    case 'v':
                        if (XmlConvert.StrEqual(_ps.chars, _ps.charPos, nameEndPos - _ps.charPos, "version") && xmlDeclState == 0)
                        {
                            if (!isTextDecl)
                            {
                                attr = AddAttributeNoChecks("version", 1);
                            }
                            break;
                        }
                        goto default;
                    case 'e':
                        if (XmlConvert.StrEqual(_ps.chars, _ps.charPos, nameEndPos - _ps.charPos, "encoding") &&
                            (xmlDeclState == 1 || (isTextDecl && xmlDeclState == 0)))
                        {
                            if (!isTextDecl)
                            {
                                attr = AddAttributeNoChecks("encoding", 1);
                            }
                            xmlDeclState = 1;
                            break;
                        }
                        goto default;
                    case 's':
                        if (XmlConvert.StrEqual(_ps.chars, _ps.charPos, nameEndPos - _ps.charPos, "standalone") &&
                             (xmlDeclState == 1 || xmlDeclState == 2) && !isTextDecl)
                        {
                            if (!isTextDecl)
                            {
                                attr = AddAttributeNoChecks("standalone", 1);
                            }
                            xmlDeclState = 2;
                            break;
                        }
                        goto default;
                    default:
                        Throw(isTextDecl ? SR.Xml_InvalidTextDecl : SR.Xml_InvalidXmlDecl);
                        break;
                }

                if (!isTextDecl)
                {
                    Debug.Assert(attr != null);
                    attr.SetLineInfo(_ps.LineNo, _ps.LinePos);
                }
                sb.Append(_ps.chars, _ps.charPos, nameEndPos - _ps.charPos);
                _ps.charPos = nameEndPos;

                // parse equals and quote char;
                if (_ps.chars[_ps.charPos] != '=')
                {
                    EatWhitespaces(sb);
                    if (_ps.chars[_ps.charPos] != '=')
                    {
                        ThrowUnexpectedToken("=");
                    }
                }
                sb.Append('=');
                _ps.charPos++;

                char quoteChar = _ps.chars[_ps.charPos];
                if (quoteChar != '"' && quoteChar != '\'')
                {
                    EatWhitespaces(sb);
                    quoteChar = _ps.chars[_ps.charPos];
                    if (quoteChar != '"' && quoteChar != '\'')
                    {
                        ThrowUnexpectedToken("\"", "'");
                    }
                }
                sb.Append(quoteChar);
                _ps.charPos++;
                if (!isTextDecl)
                {
                    Debug.Assert(attr != null);
                    attr.quoteChar = quoteChar;
                    attr.SetLineInfo2(_ps.LineNo, _ps.LinePos);
                }

                // parse attribute value
                int pos = _ps.charPos;
                char[] chars;
            Continue:
                chars = _ps.chars;
                while (XmlCharType.IsAttributeValueChar(chars[pos]))
                {
                    pos++;
                }

                if (_ps.chars[pos] == quoteChar)
                {
                    switch (xmlDeclState)
                    {
                        // version
                        case 0:
                            // VersionNum  ::=  '1.0'        (XML Fourth Edition and earlier)
                            if (XmlConvert.StrEqual(_ps.chars, _ps.charPos, pos - _ps.charPos, "1.0"))
                            {
                                if (!isTextDecl)
                                {
                                    Debug.Assert(attr != null);
                                    attr.SetValue(_ps.chars, _ps.charPos, pos - _ps.charPos);
                                }
                                xmlDeclState = 1;
                            }
                            else
                            {
                                string badVersion = new string(_ps.chars, _ps.charPos, pos - _ps.charPos);
                                Throw(SR.Xml_InvalidVersionNumber, badVersion);
                            }
                            break;
                        case 1:
                            string encName = new string(_ps.chars, _ps.charPos, pos - _ps.charPos);
                            encoding = CheckEncoding(encName);
                            if (!isTextDecl)
                            {
                                Debug.Assert(attr != null);
                                attr.SetValue(encName);
                            }
                            xmlDeclState = 2;
                            break;
                        case 2:
                            if (XmlConvert.StrEqual(_ps.chars, _ps.charPos, pos - _ps.charPos, "yes"))
                            {
                                _standalone = true;
                            }
                            else if (XmlConvert.StrEqual(_ps.chars, _ps.charPos, pos - _ps.charPos, "no"))
                            {
                                _standalone = false;
                            }
                            else
                            {
                                Debug.Assert(!isTextDecl);
                                Throw(SR.Xml_InvalidXmlDecl, _ps.LineNo, _ps.LinePos - 1);
                            }
                            if (!isTextDecl)
                            {
                                Debug.Assert(attr != null);
                                attr.SetValue(_ps.chars, _ps.charPos, pos - _ps.charPos);
                            }
                            xmlDeclState = 3;
                            break;
                        default:
                            Debug.Fail($"Unexpected xmlDeclState {xmlDeclState}");
                            break;
                    }
                    sb.Append(chars, _ps.charPos, pos - _ps.charPos);
                    sb.Append(quoteChar);
                    _ps.charPos = pos + 1;
                    continue;
                }
                else if (pos == _ps.charsUsed)
                {
                    if (ReadData() != 0)
                    {
                        goto Continue;
                    }
                    else
                    {
                        Throw(SR.Xml_UnclosedQuote);
                    }
                }
                else
                {
                    Throw(isTextDecl ? SR.Xml_InvalidTextDecl : SR.Xml_InvalidXmlDecl);
                }

            ReadData:
                if (_ps.isEof || ReadData() == 0)
                {
                    Throw(SR.Xml_UnexpectedEOF1);
                }
            }

        NoXmlDecl:
            // no xml declaration
            if (!isTextDecl)
            {
                _parsingFunction = _nextParsingFunction;
            }
            if (_afterResetState)
            {
                // check for invalid encoding switches to default encoding
                Debug.Assert(_ps.encoding != null);
                string encodingName = _ps.encoding.WebName;
                if (encodingName != "utf-8" && encodingName != "utf-16" &&
                    encodingName != "utf-16BE" && !(_ps.encoding is Ucs4Encoding))
                {
                    Throw(SR.Xml_EncodingSwitchAfterResetState, (_ps.encoding.GetByteCount("A") == 1) ? "UTF-8" : "UTF-16");
                }
            }
            if (_ps.decoder is SafeAsciiDecoder)
            {
                SwitchEncodingToUTF8();
            }
            _ps.appendMode = false;
            return false;
        }

        // Parses the document content
        private bool ParseDocumentContent()
        {
            bool mangoQuirks = false;
            while (true)
            {
                bool needMoreChars = false;
                int pos = _ps.charPos;
                Debug.Assert(_ps.chars != null);
                char[] chars = _ps.chars;

                // some tag
                if (chars[pos] == '<')
                {
                    needMoreChars = true;
                    if (_ps.charsUsed - pos < 4) // minimum  "<a/>"
                        goto ReadData;
                    pos++;
                    switch (chars[pos])
                    {
                        // processing instruction
                        case '?':
                            _ps.charPos = pos + 1;
                            if (ParsePI())
                            {
                                return true;
                            }
                            continue;
                        case '!':
                            pos++;
                            if (_ps.charsUsed - pos < 2) // minimum characters expected "--"
                                goto ReadData;
                            // comment
                            if (chars[pos] == '-')
                            {
                                if (chars[pos + 1] == '-')
                                {
                                    _ps.charPos = pos + 2;
                                    if (ParseComment())
                                    {
                                        return true;
                                    }
                                    continue;
                                }
                                else
                                {
                                    ThrowUnexpectedToken(pos + 1, "-");
                                }
                            }
                            // CDATA section
                            else if (chars[pos] == '[')
                            {
                                if (_fragmentType != XmlNodeType.Document)
                                {
                                    pos++;
                                    if (_ps.charsUsed - pos < 6)
                                    {
                                        goto ReadData;
                                    }
                                    if (XmlConvert.StrEqual(chars, pos, 6, "CDATA["))
                                    {
                                        _ps.charPos = pos + 6;
                                        ParseCData();
                                        if (_fragmentType == XmlNodeType.None)
                                        {
                                            _fragmentType = XmlNodeType.Element;
                                        }
                                        return true;
                                    }
                                    else
                                    {
                                        ThrowUnexpectedToken(pos, "CDATA[");
                                    }
                                }
                                else
                                {
                                    Throw(_ps.charPos, SR.Xml_InvalidRootData);
                                }
                            }
                            // DOCTYPE declaration
                            else
                            {
                                if (_fragmentType == XmlNodeType.Document || _fragmentType == XmlNodeType.None)
                                {
                                    _fragmentType = XmlNodeType.Document;
                                    _ps.charPos = pos;
                                    if (ParseDoctypeDecl())
                                    {
                                        return true;
                                    }
                                    continue;
                                }
                                else
                                {
                                    if (ParseUnexpectedToken(pos) == "DOCTYPE")
                                    {
                                        Throw(SR.Xml_BadDTDLocation);
                                    }
                                    else
                                    {
                                        ThrowUnexpectedToken(pos, "<!--", "<[CDATA[");
                                    }
                                }
                            }
                            break;
                        case '/':
                            Throw(pos + 1, SR.Xml_UnexpectedEndTag);
                            break;
                        // document element start tag
                        default:
                            if (_rootElementParsed)
                            {
                                if (_fragmentType == XmlNodeType.Document)
                                {
                                    Throw(pos, SR.Xml_MultipleRoots);
                                }
                                if (_fragmentType == XmlNodeType.None)
                                {
                                    _fragmentType = XmlNodeType.Element;
                                }
                            }
                            _ps.charPos = pos;
                            _rootElementParsed = true;
                            ParseElement();
                            return true;
                    }
                }
                else if (chars[pos] == '&')
                {
                    if (_fragmentType == XmlNodeType.Document)
                    {
                        Throw(pos, SR.Xml_InvalidRootData);
                    }
                    else
                    {
                        if (_fragmentType == XmlNodeType.None)
                        {
                            _fragmentType = XmlNodeType.Element;
                        }
                        switch (HandleEntityReference(false, EntityExpandType.OnlyGeneral, out _))
                        {
                            case EntityType.Unexpanded:
                                if (_parsingFunction == ParsingFunction.EntityReference)
                                {
                                    _parsingFunction = _nextParsingFunction;
                                }
                                ParseEntityReference();
                                return true;
                            case EntityType.CharacterDec:
                            case EntityType.CharacterHex:
                            case EntityType.CharacterNamed:
                                if (ParseText())
                                {
                                    return true;
                                }
                                continue;
                            default:
                                continue;
                        }
                    }
                }
                // end of buffer
                else if (pos == _ps.charsUsed || ((_v1Compat || mangoQuirks) && chars[pos] == 0x0))
                {
                    goto ReadData;
                }
                // something else -> root level whitespace
                else
                {
                    if (_fragmentType == XmlNodeType.Document)
                    {
                        if (ParseRootLevelWhitespace())
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (ParseText())
                        {
                            if (_fragmentType == XmlNodeType.None && _curNode.type == XmlNodeType.Text)
                            {
                                _fragmentType = XmlNodeType.Element;
                            }
                            return true;
                        }
                    }
                    continue;
                }

                Debug.Assert(pos == _ps.charsUsed && !_ps.isEof);

            ReadData:
                // read new characters into the buffer
                if (ReadData() == 0)
                {
                    if (needMoreChars)
                    {
                        Throw(SR.Xml_InvalidRootData);
                    }

                    if (InEntity)
                    {
                        if (HandleEntityEnd(true))
                        {
                            SetupEndEntityNodeInContent();
                            return true;
                        }
                        continue;
                    }
                    Debug.Assert(_index == 0);

                    if (!_rootElementParsed && _fragmentType == XmlNodeType.Document)
                    {
                        ThrowWithoutLineInfo(SR.Xml_MissingRoot);
                    }

                    if (_fragmentType == XmlNodeType.None)
                    {
                        _fragmentType = _rootElementParsed ? XmlNodeType.Document : XmlNodeType.Element;
                    }
                    OnEof();
                    return false;
                }
            }
        }

        // Parses element content
        private bool ParseElementContent()
        {
            while (true)
            {
                int pos = _ps.charPos;
                Debug.Assert(_ps.chars != null);
                char[] chars = _ps.chars;

                switch (chars[pos])
                {
                    // some tag
                    case '<':
                        switch (chars[pos + 1])
                        {
                            // processing instruction
                            case '?':
                                _ps.charPos = pos + 2;
                                if (ParsePI())
                                {
                                    return true;
                                }
                                continue;
                            case '!':
                                pos += 2;
                                if (_ps.charsUsed - pos < 2)
                                    goto ReadData;
                                // comment
                                if (chars[pos] == '-')
                                {
                                    if (chars[pos + 1] == '-')
                                    {
                                        _ps.charPos = pos + 2;
                                        if (ParseComment())
                                        {
                                            return true;
                                        }
                                        continue;
                                    }
                                    else
                                    {
                                        ThrowUnexpectedToken(pos + 1, "-");
                                    }
                                }
                                // CDATA section
                                else if (chars[pos] == '[')
                                {
                                    pos++;
                                    if (_ps.charsUsed - pos < 6)
                                    {
                                        goto ReadData;
                                    }
                                    if (XmlConvert.StrEqual(chars, pos, 6, "CDATA["))
                                    {
                                        _ps.charPos = pos + 6;
                                        ParseCData();
                                        return true;
                                    }
                                    else
                                    {
                                        ThrowUnexpectedToken(pos, "CDATA[");
                                    }
                                }
                                else
                                {
                                    if (ParseUnexpectedToken(pos) == "DOCTYPE")
                                    {
                                        Throw(SR.Xml_BadDTDLocation);
                                    }
                                    else
                                    {
                                        ThrowUnexpectedToken(pos, "<!--", "<[CDATA[");
                                    }
                                }
                                break;
                            // element end tag
                            case '/':
                                _ps.charPos = pos + 2;
                                ParseEndElement();
                                return true;
                            default:
                                // end of buffer
                                if (pos + 1 == _ps.charsUsed)
                                {
                                    goto ReadData;
                                }
                                else
                                {
                                    // element start tag
                                    _ps.charPos = pos + 1;
                                    ParseElement();
                                    return true;
                                }
                        }
                        break;
                    case '&':
                        if (ParseText())
                        {
                            return true;
                        }
                        continue;
                    default:
                        // end of buffer
                        if (pos == _ps.charsUsed)
                        {
                            goto ReadData;
                        }
                        else
                        {
                            // text node, whitespace or entity reference
                            if (ParseText())
                            {
                                return true;
                            }
                            continue;
                        }
                }

            ReadData:
                // read new characters into the buffer
                if (ReadData() == 0)
                {
                    if (_ps.charsUsed - _ps.charPos != 0)
                    {
                        ThrowUnclosedElements();
                    }
                    if (!InEntity)
                    {
                        if (_index == 0 && _fragmentType != XmlNodeType.Document)
                        {
                            OnEof();
                            return false;
                        }
                        ThrowUnclosedElements();
                    }
                    if (HandleEntityEnd(true))
                    {
                        SetupEndEntityNodeInContent();
                        return true;
                    }
                }
            }
        }

        private void ThrowUnclosedElements()
        {
            if (_index == 0 && _curNode.type != XmlNodeType.Element)
            {
                Throw(_ps.charsUsed, SR.Xml_UnexpectedEOF1);
            }
            else
            {
                int i = (_parsingFunction == ParsingFunction.InIncrementalRead) ? _index : _index - 1;
                _stringBuilder.Length = 0;
                for (; i >= 0; i--)
                {
                    NodeData el = _nodes[i];
                    if (el.type != XmlNodeType.Element)
                    {
                        continue;
                    }
                    _stringBuilder.Append(el.GetNameWPrefix(_nameTable));
                    if (i > 0)
                    {
                        _stringBuilder.Append(", ");
                    }
                    else
                    {
                        _stringBuilder.Append('.');
                    }
                }

                Throw(_ps.charsUsed, SR.Xml_UnexpectedEOFInElementContent, _stringBuilder.ToString());
            }
        }

        // Parses the element start tag
        private void ParseElement()
        {
            int pos = _ps.charPos;
            Debug.Assert(_ps.chars != null);
            char[] chars = _ps.chars;
            int colonPos = -1;

            _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);

        // PERF: we intentionally don't call ParseQName here to parse the element name unless a special
        // case occurs (like end of buffer, invalid name char)
        ContinueStartName:
            // check element name start char
            if (XmlCharType.IsStartNCNameSingleChar(chars[pos]))
            {
                pos++;
            }
            else
            {
                goto ParseQNameSlow;
            }

        ContinueName:
            // parse element name
            while (true)
            {
                if (XmlCharType.IsNCNameSingleChar(chars[pos]))
                {
                    pos++;
                }
                else
                {
                    break;
                }
            }

            // colon -> save prefix end position and check next char if it's name start char
            if (chars[pos] == ':')
            {
                if (colonPos != -1)
                {
                    if (_supportNamespaces)
                    {
                        Throw(pos, SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(':', '\0'));
                    }
                    else
                    {
                        pos++;
                        goto ContinueName;
                    }
                }
                else
                {
                    colonPos = pos;
                    pos++;
                    goto ContinueStartName;
                }
            }
            else if (pos + 1 < _ps.charsUsed)
            {
                goto SetElement;
            }

        ParseQNameSlow:
            pos = ParseQName(out colonPos);
            chars = _ps.chars;

        SetElement:
            // push namespace context
            Debug.Assert(_namespaceManager != null);
            _namespaceManager.PushScope();

            // init the NodeData class
            if (colonPos == -1 || !_supportNamespaces)
            {
                _curNode.SetNamedNode(XmlNodeType.Element,
                                      _nameTable.Add(chars, _ps.charPos, pos - _ps.charPos));
            }
            else
            {
                int startPos = _ps.charPos;
                int prefixLen = colonPos - startPos;
                if (prefixLen == _lastPrefix.Length && XmlConvert.StrEqual(chars, startPos, prefixLen, _lastPrefix))
                {
                    _curNode.SetNamedNode(XmlNodeType.Element,
                                          _nameTable.Add(chars, colonPos + 1, pos - colonPos - 1),
                                          _lastPrefix,
                                          null);
                }
                else
                {
                    _curNode.SetNamedNode(XmlNodeType.Element,
                                          _nameTable.Add(chars, colonPos + 1, pos - colonPos - 1),
                                          _nameTable.Add(chars, _ps.charPos, prefixLen),
                                          null);
                    _lastPrefix = _curNode.prefix;
                }
            }

            char ch = chars[pos];
            // whitespace after element name -> there are probably some attributes
            bool isWs = XmlCharType.IsWhiteSpace(ch);
            if (isWs)
            {
                _ps.charPos = pos;
                ParseAttributes();
                return;
            }
            // no attributes
            else
            {
                // non-empty element
                if (ch == '>')
                {
                    _ps.charPos = pos + 1;
                    _parsingFunction = ParsingFunction.MoveToElementContent;
                }
                // empty element
                else if (ch == '/')
                {
                    if (pos + 1 == _ps.charsUsed)
                    {
                        _ps.charPos = pos;
                        if (ReadData() == 0)
                        {
                            Throw(pos, SR.Xml_UnexpectedEOF, ">");
                        }
                        pos = _ps.charPos;
                        chars = _ps.chars;
                    }
                    if (chars[pos + 1] == '>')
                    {
                        _curNode.IsEmptyElement = true;
                        _nextParsingFunction = _parsingFunction;
                        _parsingFunction = ParsingFunction.PopEmptyElementContext;
                        _ps.charPos = pos + 2;
                    }
                    else
                    {
                        ThrowUnexpectedToken(pos, ">");
                    }
                }
                // something else after the element name
                else
                {
                    Throw(pos, SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(chars, _ps.charsUsed, pos));
                }

                // add default attributes & strip spaces in attributes with type other than CDATA
                if (_addDefaultAttributesAndNormalize)
                {
                    AddDefaultAttributesAndNormalize();
                }

                // lookup element namespace
                ElementNamespaceLookup();
            }
        }

        private void AddDefaultAttributesAndNormalize()
        {
            Debug.Assert(_curNode.type == XmlNodeType.Element);
            Debug.Assert(_dtdInfo != null);
            IDtdAttributeListInfo? attlistInfo = _dtdInfo.LookupAttributeList(_curNode.localName, _curNode.prefix);

            if (attlistInfo == null)
            {
                return;
            }

            // fix non-CDATA attribute value
            if (_normalize && attlistInfo.HasNonCDataAttributes)
            {
                // go through the attributes and normalize it if not CDATA type
                for (int i = _index + 1; i < _index + 1 + _attrCount; i++)
                {
                    NodeData attr = _nodes[i];

                    IDtdAttributeInfo? attributeInfo = attlistInfo.LookupAttribute(attr.prefix, attr.localName);
                    if (attributeInfo != null && attributeInfo.IsNonCDataType)
                    {
                        if (DtdValidation && _standalone && attributeInfo.IsDeclaredInExternal)
                        {
                            // VC constraint:
                            // The standalone document declaration must have the value "no" if any external markup declarations
                            // contain declarations of attributes with values subject to normalization, where the attribute appears in
                            // the document with a value which will change as a result of normalization,
                            string oldValue = attr.StringValue;
                            attr.TrimSpacesInValue();

                            if (oldValue != attr.StringValue)
                            {
                                SendValidationEvent(XmlSeverityType.Error, SR.Sch_StandAloneNormalization, attr.GetNameWPrefix(_nameTable), attr.LineNo, attr.LinePos);
                            }
                        }
                        else
                            attr.TrimSpacesInValue();
                    }
                }
            }

            // add default attributes
            IEnumerable<IDtdDefaultAttributeInfo> defaultAttributes = attlistInfo.LookupDefaultAttributes();
            if (defaultAttributes != null)
            {
                int originalAttrCount = _attrCount;
                NodeData[]? nameSortedAttributes = null;

                if (_attrCount >= MaxAttrDuplWalkCount)
                {
                    nameSortedAttributes = new NodeData[_attrCount];
                    Array.Copy(_nodes, _index + 1, nameSortedAttributes, 0, _attrCount);
                    Array.Sort<object>(nameSortedAttributes, DtdDefaultAttributeInfoToNodeDataComparer.Instance);
                }

                foreach (IDtdDefaultAttributeInfo defaultAttributeInfo in defaultAttributes)
                {
                    if (AddDefaultAttributeDtd(defaultAttributeInfo, true, nameSortedAttributes))
                    {
                        if (DtdValidation && _standalone && defaultAttributeInfo.IsDeclaredInExternal)
                        {
                            string prefix = defaultAttributeInfo.Prefix;
                            string qname = (prefix.Length == 0) ? defaultAttributeInfo.LocalName : (prefix + ':' + defaultAttributeInfo.LocalName);
                            SendValidationEvent(XmlSeverityType.Error, SR.Sch_UnSpecifiedDefaultAttributeInExternalStandalone, qname, _curNode.LineNo, _curNode.LinePos);
                        }
                    }
                }

                if (originalAttrCount == 0 && _attrNeedNamespaceLookup)
                {
                    AttributeNamespaceLookup();
                    _attrNeedNamespaceLookup = false;
                }
            }
        }

        // parses the element end tag
        private void ParseEndElement()
        {
            // check if the end tag name equals start tag name
            NodeData startTagNode = _nodes[_index - 1];

            int prefLen = startTagNode.prefix.Length;
            int locLen = startTagNode.localName.Length;

            while (_ps.charsUsed - _ps.charPos < prefLen + locLen + 1)
            {
                if (ReadData() == 0)
                {
                    break;
                }
            }

            int nameLen;
            Debug.Assert(_ps.chars != null);
            char[] chars = _ps.chars;
            if (startTagNode.prefix.Length == 0)
            {
                if (!XmlConvert.StrEqual(chars, _ps.charPos, locLen, startTagNode.localName))
                {
                    ThrowTagMismatch(startTagNode);
                }
                nameLen = locLen;
            }
            else
            {
                int colonPos = _ps.charPos + prefLen;
                if (!XmlConvert.StrEqual(chars, _ps.charPos, prefLen, startTagNode.prefix) ||
                        chars[colonPos] != ':' ||
                        !XmlConvert.StrEqual(chars, colonPos + 1, locLen, startTagNode.localName))
                {
                    ThrowTagMismatch(startTagNode);
                }
                nameLen = locLen + prefLen + 1;
            }

            LineInfo endTagLineInfo = new LineInfo(_ps.lineNo, _ps.LinePos);

            int pos;
            while (true)
            {
                pos = _ps.charPos + nameLen;
                chars = _ps.chars;

                if (pos == _ps.charsUsed)
                {
                    goto ReadData;
                }

                if (XmlCharType.IsNCNameSingleChar(chars[pos]) || (chars[pos] == ':'))
                {
                    ThrowTagMismatch(startTagNode);
                }

                // eat whitespace
                if (chars[pos] != '>')
                {
                    char tmpCh;
                    while (XmlCharType.IsWhiteSpace(tmpCh = chars[pos]))
                    {
                        pos++;
                        switch (tmpCh)
                        {
                            case (char)0xA:
                                OnNewLine(pos);
                                continue;
                            case (char)0xD:
                                if (chars[pos] == (char)0xA)
                                {
                                    pos++;
                                }
                                else if (pos == _ps.charsUsed && !_ps.isEof)
                                {
                                    break;
                                }
                                OnNewLine(pos);
                                continue;
                        }
                    }
                }

                if (chars[pos] == '>')
                {
                    break;
                }
                else if (pos == _ps.charsUsed)
                {
                    goto ReadData;
                }
                else
                {
                    ThrowUnexpectedToken(pos, ">");
                }

                Debug.Fail("We should never get to this point.");

            ReadData:
                if (ReadData() == 0)
                {
                    ThrowUnclosedElements();
                }
            }

            Debug.Assert(_index > 0);
            _index--;
            _curNode = _nodes[_index];

            // set the element data
            Debug.Assert(_curNode == startTagNode);
            startTagNode.lineInfo = endTagLineInfo;
            startTagNode.type = XmlNodeType.EndElement;
            _ps.charPos = pos + 1;

            // set next parsing function
            _nextParsingFunction = (_index > 0) ? _parsingFunction : ParsingFunction.DocumentContent;
            _parsingFunction = ParsingFunction.PopElementContext;
        }

        private void ThrowTagMismatch(NodeData startTag)
        {
            if (startTag.type == XmlNodeType.Element)
            {
                // parse the bad name
                int endPos = ParseQName(out _);

                Debug.Assert(_ps.chars != null);
                string[] args = new string[4];
                args[0] = startTag.GetNameWPrefix(_nameTable);
                args[1] = startTag.lineInfo.lineNo.ToString(CultureInfo.InvariantCulture);
                args[2] = startTag.lineInfo.linePos.ToString(CultureInfo.InvariantCulture);
                args[3] = new string(_ps.chars, _ps.charPos, endPos - _ps.charPos);
                Throw(SR.Xml_TagMismatchEx, args);
            }
            else
            {
                Debug.Assert(startTag.type == XmlNodeType.EntityReference);
                Throw(SR.Xml_UnexpectedEndTag);
            }
        }

        // Reads the attributes
        private void ParseAttributes()
        {
            int pos = _ps.charPos;
            Debug.Assert(_ps.chars != null);
            char[] chars = _ps.chars;
            NodeData? attr = null;

            Debug.Assert(_attrCount == 0);

            while (true)
            {
                // eat whitespace
                int lineNoDelta = 0;
                char tmpch0;
                while (XmlCharType.IsWhiteSpace(tmpch0 = chars[pos]))
                {
                    if (tmpch0 == (char)0xA)
                    {
                        OnNewLine(pos + 1);
                        lineNoDelta++;
                    }
                    else if (tmpch0 == (char)0xD)
                    {
                        if (chars[pos + 1] == (char)0xA)
                        {
                            OnNewLine(pos + 2);
                            lineNoDelta++;
                            pos++;
                        }
                        else if (pos + 1 != _ps.charsUsed)
                        {
                            OnNewLine(pos + 1);
                            lineNoDelta++;
                        }
                        else
                        {
                            _ps.charPos = pos;
                            goto ReadData;
                        }
                    }
                    pos++;
                }

                char tmpch1;
                int startNameCharSize = 0;

                if (XmlCharType.IsStartNCNameSingleChar(tmpch1 = chars[pos]))
                {
                    startNameCharSize = 1;
                }

                if (startNameCharSize == 0)
                {
                    // element end
                    if (tmpch1 == '>')
                    {
                        Debug.Assert(_curNode.type == XmlNodeType.Element);
                        _ps.charPos = pos + 1;
                        _parsingFunction = ParsingFunction.MoveToElementContent;
                        goto End;
                    }
                    // empty element end
                    else if (tmpch1 == '/')
                    {
                        Debug.Assert(_curNode.type == XmlNodeType.Element);
                        if (pos + 1 == _ps.charsUsed)
                        {
                            goto ReadData;
                        }
                        if (chars[pos + 1] == '>')
                        {
                            _ps.charPos = pos + 2;
                            _curNode.IsEmptyElement = true;
                            _nextParsingFunction = _parsingFunction;
                            _parsingFunction = ParsingFunction.PopEmptyElementContext;
                            goto End;
                        }
                        else
                        {
                            ThrowUnexpectedToken(pos + 1, ">");
                        }
                    }
                    else if (pos == _ps.charsUsed)
                    {
                        goto ReadData;
                    }
                    else if (tmpch1 != ':' || _supportNamespaces)
                    {
                        Throw(pos, SR.Xml_BadStartNameChar, XmlException.BuildCharExceptionArgs(chars, _ps.charsUsed, pos));
                    }
                }

                if (pos == _ps.charPos)
                {
                    ThrowExpectingWhitespace(pos);
                }
                _ps.charPos = pos;

                // save attribute name line position
                int attrNameLinePos = _ps.LinePos;

#if DEBUG
                int attrNameLineNo = _ps.LineNo;
#endif

                // parse attribute name
                int colonPos = -1;

                // PERF: we intentionally don't call ParseQName here to parse the element name unless a special
                // case occurs (like end of buffer, invalid name char)
                pos += startNameCharSize; // start name char has already been checked

            // parse attribute name
            ContinueParseName:
                char tmpch2;

                while (true)
                {
                    if (XmlCharType.IsNCNameSingleChar(tmpch2 = chars[pos]))
                    {
                        pos++;
                    }
                    else
                    {
                        break;
                    }
                }

                // colon -> save prefix end position and check next char if it's name start char
                if (tmpch2 == ':')
                {
                    if (colonPos != -1)
                    {
                        if (_supportNamespaces)
                        {
                            Throw(pos, SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(':', '\0'));
                        }
                        else
                        {
                            pos++;
                            goto ContinueParseName;
                        }
                    }
                    else
                    {
                        colonPos = pos;
                        pos++;

                        if (XmlCharType.IsStartNCNameSingleChar(chars[pos]))
                        {
                            pos++;
                            goto ContinueParseName;
                        }
                        // else fallback to full name parsing routine
                        pos = ParseQName(out colonPos);
                        chars = _ps.chars;
                    }
                }
                else if (pos + 1 >= _ps.charsUsed)
                {
                    pos = ParseQName(out colonPos);
                    chars = _ps.chars;
                }

                attr = AddAttribute(pos, colonPos);
                attr.SetLineInfo(_ps.LineNo, attrNameLinePos);

#if DEBUG
                Debug.Assert(attrNameLineNo == _ps.LineNo);
#endif

                // parse equals and quote char;
                if (chars[pos] != '=')
                {
                    _ps.charPos = pos;
                    EatWhitespaces(null);
                    pos = _ps.charPos;
                    if (chars[pos] != '=')
                    {
                        ThrowUnexpectedToken("=");
                    }
                }
                pos++;

                char quoteChar = chars[pos];
                if (quoteChar != '"' && quoteChar != '\'')
                {
                    _ps.charPos = pos;
                    EatWhitespaces(null);
                    pos = _ps.charPos;
                    quoteChar = chars[pos];
                    if (quoteChar != '"' && quoteChar != '\'')
                    {
                        ThrowUnexpectedToken("\"", "'");
                    }
                }
                pos++;
                _ps.charPos = pos;

                attr.quoteChar = quoteChar;
                attr.SetLineInfo2(_ps.LineNo, _ps.LinePos);

                // parse attribute value
                char tmpch3;
                while (XmlCharType.IsAttributeValueChar(tmpch3 = chars[pos]))
                {
                    pos++;
                }

                if (tmpch3 == quoteChar)
                {
#if DEBUG
                    if (_normalize)
                    {
                        string val = new string(chars, _ps.charPos, pos - _ps.charPos);
                        Debug.Assert(val == XmlComplianceUtil.CDataNormalize(val), "The attribute value is not CDATA normalized!");
                    }
#endif
                    attr.SetValue(chars, _ps.charPos, pos - _ps.charPos);
                    pos++;
                    _ps.charPos = pos;
                }
                else
                {
                    ParseAttributeValueSlow(pos, quoteChar, attr);
                    pos = _ps.charPos;
                    chars = _ps.chars;
                }

                // handle special attributes:
                if (attr.prefix.Length == 0)
                {
                    // default namespace declaration
                    if (Ref.Equal(attr.localName, _xmlNs))
                    {
                        OnDefaultNamespaceDecl(attr);
                    }
                }
                else
                {
                    // prefixed namespace declaration
                    if (Ref.Equal(attr.prefix, _xmlNs))
                    {
                        OnNamespaceDecl(attr);
                    }
                    // xml: attribute
                    else if (Ref.Equal(attr.prefix, _xml))
                    {
                        OnXmlReservedAttribute(attr);
                    }
                }
                continue;

            ReadData:
                _ps.lineNo -= lineNoDelta;
                if (ReadData() != 0)
                {
                    pos = _ps.charPos;
                    chars = _ps.chars;
                }
                else
                {
                    ThrowUnclosedElements();
                }
            }

        End:
            if (_addDefaultAttributesAndNormalize)
            {
                AddDefaultAttributesAndNormalize();
            }
            // lookup namespaces: element
            ElementNamespaceLookup();

            // lookup namespaces: attributes
            if (_attrNeedNamespaceLookup)
            {
                AttributeNamespaceLookup();
                _attrNeedNamespaceLookup = false;
            }

            // check duplicate attributes
            if (_attrDuplWalkCount >= MaxAttrDuplWalkCount)
            {
                AttributeDuplCheck();
            }
        }

        private void ElementNamespaceLookup()
        {
            Debug.Assert(_curNode.type == XmlNodeType.Element);
            if (_curNode.prefix.Length == 0)
            {
                _curNode.ns = _xmlContext.defaultNamespace;
            }
            else
            {
                _curNode.ns = LookupNamespace(_curNode);
            }
        }

        private void AttributeNamespaceLookup()
        {
            for (int i = _index + 1; i < _index + _attrCount + 1; i++)
            {
                NodeData at = _nodes[i];
                if (at.type == XmlNodeType.Attribute && at.prefix.Length > 0)
                {
                    at.ns = LookupNamespace(at);
                }
            }
        }

        private void AttributeDuplCheck()
        {
            if (_attrCount < MaxAttrDuplWalkCount)
            {
                for (int i = _index + 1; i < _index + 1 + _attrCount; i++)
                {
                    NodeData attr1 = _nodes[i];
                    for (int j = i + 1; j < _index + 1 + _attrCount; j++)
                    {
                        if (Ref.Equal(attr1.localName, _nodes[j].localName) && Ref.Equal(attr1.ns, _nodes[j].ns))
                        {
                            Throw(SR.Xml_DupAttributeName, _nodes[j].GetNameWPrefix(_nameTable), _nodes[j].LineNo, _nodes[j].LinePos);
                        }
                    }
                }
            }
            else
            {
                if (_attrDuplSortingArray == null || _attrDuplSortingArray.Length < _attrCount)
                {
                    _attrDuplSortingArray = new NodeData[_attrCount];
                }
                Array.Copy(_nodes, _index + 1, _attrDuplSortingArray, 0, _attrCount);
                Array.Sort(_attrDuplSortingArray, 0, _attrCount);

                NodeData attr1 = _attrDuplSortingArray[0];
                for (int i = 1; i < _attrCount; i++)
                {
                    NodeData attr2 = _attrDuplSortingArray[i];
                    if (Ref.Equal(attr1.localName, attr2.localName) && Ref.Equal(attr1.ns, attr2.ns))
                    {
                        Throw(SR.Xml_DupAttributeName, attr2.GetNameWPrefix(_nameTable), attr2.LineNo, attr2.LinePos);
                    }
                    attr1 = attr2;
                }
            }
        }

        private void OnDefaultNamespaceDecl(NodeData attr)
        {
            if (!_supportNamespaces)
            {
                return;
            }

            string ns = _nameTable.Add(attr.StringValue);
            attr.ns = _nameTable.Add(XmlReservedNs.NsXmlNs);

            if (!_curNode.xmlContextPushed)
            {
                PushXmlContext();
            }

            _xmlContext.defaultNamespace = ns;

            AddNamespace(string.Empty, ns, attr);
        }

        private void OnNamespaceDecl(NodeData attr)
        {
            if (!_supportNamespaces)
            {
                return;
            }
            string ns = _nameTable.Add(attr.StringValue);
            if (ns.Length == 0)
            {
                Throw(SR.Xml_BadNamespaceDecl, attr.lineInfo2.lineNo, attr.lineInfo2.linePos - 1);
            }
            AddNamespace(attr.localName, ns, attr);
        }

        private void OnXmlReservedAttribute(NodeData attr)
        {
            switch (attr.localName)
            {
                // xml:space
                case "space":
                    if (!_curNode.xmlContextPushed)
                    {
                        PushXmlContext();
                    }

                    switch (XmlConvert.TrimString(attr.StringValue))
                    {
                        case "preserve":
                            _xmlContext.xmlSpace = XmlSpace.Preserve;
                            break;
                        case "default":
                            _xmlContext.xmlSpace = XmlSpace.Default;
                            break;
                        default:
                            Throw(SR.Xml_InvalidXmlSpace, attr.StringValue, attr.lineInfo.lineNo, attr.lineInfo.linePos);
                            break;
                    }
                    break;
                // xml:lang
                case "lang":
                    if (!_curNode.xmlContextPushed)
                    {
                        PushXmlContext();
                    }

                    _xmlContext.xmlLang = attr.StringValue;
                    break;
            }
        }

        private void ParseAttributeValueSlow(int curPos, char quoteChar, NodeData attr)
        {
            int pos = curPos;
            Debug.Assert(_ps.chars != null);
            char[] chars = _ps.chars;
            int attributeBaseEntityId = _ps.entityId;
            int valueChunkStartPos = 0;
            LineInfo valueChunkLineInfo = new LineInfo(_ps.lineNo, _ps.LinePos);
            NodeData? lastChunk = null;

            Debug.Assert(_stringBuilder.Length == 0);

            while (true)
            {
                // parse the rest of the attribute value
                while (XmlCharType.IsAttributeValueChar(chars[pos]))
                {
                    pos++;
                }

                if (pos - _ps.charPos > 0)
                {
                    _stringBuilder.Append(chars, _ps.charPos, pos - _ps.charPos);
                    _ps.charPos = pos;
                }

                if (chars[pos] == quoteChar && attributeBaseEntityId == _ps.entityId)
                {
                    break;
                }
                else
                {
                    switch (chars[pos])
                    {
                        // eol
                        case (char)0xA:
                            pos++;
                            OnNewLine(pos);
                            if (_normalize)
                            {
                                _stringBuilder.Append((char)0x20);  // CDATA normalization of 0xA
                                _ps.charPos++;
                            }
                            continue;
                        case (char)0xD:
                            if (chars[pos + 1] == (char)0xA)
                            {
                                pos += 2;
                                if (_normalize)
                                {
                                    _stringBuilder.Append(_ps.eolNormalized ? "\u0020\u0020" : "\u0020"); // CDATA normalization of 0xD 0xA
                                    _ps.charPos = pos;
                                }
                            }
                            else if (pos + 1 < _ps.charsUsed || _ps.isEof)
                            {
                                pos++;
                                if (_normalize)
                                {
                                    _stringBuilder.Append((char)0x20);  // CDATA normalization of 0xD and 0xD 0xA
                                    _ps.charPos = pos;
                                }
                            }
                            else
                            {
                                goto ReadData;
                            }
                            OnNewLine(pos);
                            continue;
                        // tab
                        case (char)0x9:
                            pos++;
                            if (_normalize)
                            {
                                _stringBuilder.Append((char)0x20);  // CDATA normalization of 0x9
                                _ps.charPos++;
                            }
                            continue;
                        case '"':
                        case '\'':
                        case '>':
                            pos++;
                            continue;
                        // attribute values cannot contain '<'
                        case '<':
                            Throw(pos, SR.Xml_BadAttributeChar, XmlException.BuildCharExceptionArgs('<', '\0'));
                            break;
                        // entity referece
                        case '&':
                            if (pos - _ps.charPos > 0)
                            {
                                _stringBuilder.Append(chars, _ps.charPos, pos - _ps.charPos);
                            }
                            _ps.charPos = pos;

                            int enclosingEntityId = _ps.entityId;
                            LineInfo entityLineInfo = new LineInfo(_ps.lineNo, _ps.LinePos + 1);
                            switch (HandleEntityReference(true, EntityExpandType.All, out pos))
                            {
                                case EntityType.CharacterDec:
                                case EntityType.CharacterHex:
                                case EntityType.CharacterNamed:
                                    break;
                                case EntityType.Unexpanded:
                                    if (_parsingMode == ParsingMode.Full && _ps.entityId == attributeBaseEntityId)
                                    {
                                        // construct text value chunk
                                        int valueChunkLen = _stringBuilder.Length - valueChunkStartPos;
                                        if (valueChunkLen > 0)
                                        {
                                            NodeData textChunk = new NodeData();
                                            textChunk.lineInfo = valueChunkLineInfo;
                                            textChunk.depth = attr.depth + 1;
                                            textChunk.SetValueNode(XmlNodeType.Text, _stringBuilder.ToString(valueChunkStartPos, valueChunkLen));
                                            AddAttributeChunkToList(attr, textChunk, ref lastChunk);
                                        }

                                        // parse entity name
                                        _ps.charPos++;
                                        string entityName = ParseEntityName();

                                        // construct entity reference chunk
                                        NodeData entityChunk = new NodeData();
                                        entityChunk.lineInfo = entityLineInfo;
                                        entityChunk.depth = attr.depth + 1;
                                        entityChunk.SetNamedNode(XmlNodeType.EntityReference, entityName);
                                        AddAttributeChunkToList(attr, entityChunk, ref lastChunk);

                                        // append entity ref to the attribute value
                                        _stringBuilder.Append('&');
                                        _stringBuilder.Append(entityName);
                                        _stringBuilder.Append(';');

                                        // update info for the next attribute value chunk
                                        valueChunkStartPos = _stringBuilder.Length;
                                        valueChunkLineInfo.Set(_ps.LineNo, _ps.LinePos);

                                        _fullAttrCleanup = true;
                                    }
                                    else
                                    {
                                        _ps.charPos++;
                                        ParseEntityName();
                                    }
                                    pos = _ps.charPos;
                                    break;

                                case EntityType.ExpandedInAttribute:
                                    if (_parsingMode == ParsingMode.Full && enclosingEntityId == attributeBaseEntityId)
                                    {
                                        // construct text value chunk
                                        int valueChunkLen = _stringBuilder.Length - valueChunkStartPos;
                                        if (valueChunkLen > 0)
                                        {
                                            NodeData textChunk = new NodeData();
                                            textChunk.lineInfo = valueChunkLineInfo;
                                            textChunk.depth = attr.depth + 1;
                                            textChunk.SetValueNode(XmlNodeType.Text, _stringBuilder.ToString(valueChunkStartPos, valueChunkLen));
                                            AddAttributeChunkToList(attr, textChunk, ref lastChunk);
                                        }

                                        // construct entity reference chunk
                                        NodeData entityChunk = new NodeData();
                                        entityChunk.lineInfo = entityLineInfo;
                                        entityChunk.depth = attr.depth + 1;
                                        Debug.Assert(_ps.entity != null);
                                        entityChunk.SetNamedNode(XmlNodeType.EntityReference, _ps.entity.Name);
                                        AddAttributeChunkToList(attr, entityChunk, ref lastChunk);

                                        _fullAttrCleanup = true;

                                        // Note: info for the next attribute value chunk will be updated once we
                                        // get out of the expanded entity
                                    }
                                    pos = _ps.charPos;
                                    break;
                                default:
                                    pos = _ps.charPos;
                                    break;
                            }
                            chars = _ps.chars;
                            continue;
                        default:
                            // end of buffer
                            if (pos == _ps.charsUsed)
                            {
                                goto ReadData;
                            }
                            // surrogate chars
                            else
                            {
                                char ch = chars[pos];
                                if (XmlCharType.IsHighSurrogate(ch))
                                {
                                    if (pos + 1 == _ps.charsUsed)
                                    {
                                        goto ReadData;
                                    }
                                    pos++;
                                    if (XmlCharType.IsLowSurrogate(chars[pos]))
                                    {
                                        pos++;
                                        continue;
                                    }
                                }
                                ThrowInvalidChar(chars, _ps.charsUsed, pos);
                                break;
                            }
                    }
                }

            ReadData:
                // read new characters into the buffer
                if (ReadData() == 0)
                {
                    if (_ps.charsUsed - _ps.charPos > 0)
                    {
                        if (_ps.chars[_ps.charPos] != (char)0xD)
                        {
                            Debug.Fail("We should never get to this point.");
                            Throw(SR.Xml_UnexpectedEOF1);
                        }
                        Debug.Assert(_ps.isEof);
                    }
                    else
                    {
                        if (!InEntity)
                        {
                            if (_fragmentType == XmlNodeType.Attribute)
                            {
                                if (attributeBaseEntityId != _ps.entityId)
                                {
                                    Throw(SR.Xml_EntityRefNesting);
                                }
                                break;
                            }
                            Throw(SR.Xml_UnclosedQuote);
                        }
                        if (HandleEntityEnd(true))
                        {
                            Debug.Fail("no EndEntity reporting while parsing attributes");
                            Throw(SR.Xml_InternalError);
                        }
                        // update info for the next attribute value chunk
                        if (attributeBaseEntityId == _ps.entityId)
                        {
                            valueChunkStartPos = _stringBuilder.Length;
                            valueChunkLineInfo.Set(_ps.LineNo, _ps.LinePos);
                        }
                    }
                }

                pos = _ps.charPos;
                chars = _ps.chars;
            }

            if (attr.nextAttrValueChunk != null)
            {
                // construct last text value chunk
                int valueChunkLen = _stringBuilder.Length - valueChunkStartPos;
                if (valueChunkLen > 0)
                {
                    NodeData textChunk = new NodeData();
                    textChunk.lineInfo = valueChunkLineInfo;
                    textChunk.depth = attr.depth + 1;
                    textChunk.SetValueNode(XmlNodeType.Text, _stringBuilder.ToString(valueChunkStartPos, valueChunkLen));
                    AddAttributeChunkToList(attr, textChunk, ref lastChunk);
                }
            }

            _ps.charPos = pos + 1;

            attr.SetValue(_stringBuilder.ToString());
            _stringBuilder.Length = 0;
        }

        private static void AddAttributeChunkToList(NodeData attr, NodeData chunk, ref NodeData? lastChunk)
        {
            if (lastChunk == null)
            {
                Debug.Assert(attr.nextAttrValueChunk == null);
                lastChunk = chunk;
                attr.nextAttrValueChunk = chunk;
            }
            else
            {
                lastChunk.nextAttrValueChunk = chunk;
                lastChunk = chunk;
            }
        }

        // Parses text or whitespace node.
        // Returns true if a node has been parsed and its data set to curNode.
        // Returns false when a whitespace has been parsed and ignored (according to current whitespace handling) or when parsing mode is not Full.
        // Also returns false if there is no text to be parsed.
        private bool ParseText()
        {
            int startPos;
            int endPos;
            int orChars = 0;

            // skip over the text if not in full parsing mode
            if (_parsingMode != ParsingMode.Full)
            {
                while (!ParseText(out _, out _, ref orChars));
                goto IgnoredNode;
            }

            _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);
            Debug.Assert(_stringBuilder.Length == 0);

            // the whole value is in buffer
            if (ParseText(out startPos, out endPos, ref orChars))
            {
                if (endPos - startPos == 0)
                {
                    goto IgnoredNode;
                }

                XmlNodeType nodeType = GetTextNodeType(orChars);
                if (nodeType == XmlNodeType.None)
                {
                    goto IgnoredNode;
                }

                Debug.Assert(endPos - startPos > 0);
                Debug.Assert(_ps.chars != null);
                _curNode.SetValueNode(nodeType, _ps.chars, startPos, endPos - startPos);
                return true;
            }
            // only piece of the value was returned
            else
            {
                // V1 compatibility mode -> cache the whole value
                if (_v1Compat)
                {
                    do
                    {
                        if (endPos - startPos > 0)
                        {
                            _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);
                        }
                    } while (!ParseText(out startPos, out endPos, ref orChars));

                    if (endPos - startPos > 0)
                    {
                        _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);
                    }

                    Debug.Assert(_stringBuilder.Length > 0);

                    XmlNodeType nodeType = GetTextNodeType(orChars);
                    if (nodeType == XmlNodeType.None)
                    {
                        _stringBuilder.Length = 0;
                        goto IgnoredNode;
                    }

                    _curNode.SetValueNode(nodeType, _stringBuilder.ToString());
                    _stringBuilder.Length = 0;
                    return true;
                }
                // V2 reader -> do not cache the whole value yet, read only up to 4kB to decide whether the value is a whitespace
                else
                {
                    bool fullValue;

                    // if it's a partial text value, not a whitespace -> return
                    if (orChars > 0x20)
                    {
                        Debug.Assert(endPos - startPos > 0);
                        Debug.Assert(_ps.chars != null);
                        _curNode.SetValueNode(XmlNodeType.Text, _ps.chars, startPos, endPos - startPos);
                        _nextParsingFunction = _parsingFunction;
                        _parsingFunction = ParsingFunction.PartialTextValue;
                        return true;
                    }

                    // partial whitespace -> read more data (up to 4kB) to decide if it is a whitespace or a text node
                    if (endPos - startPos > 0)
                    {
                        _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);
                    }
                    do
                    {
                        fullValue = ParseText(out startPos, out endPos, ref orChars);
                        if (endPos - startPos > 0)
                        {
                            _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);
                        }
                    } while (!fullValue && orChars <= 0x20 && _stringBuilder.Length < MinWhitespaceLookahedCount);

                    // determine the value node type
                    XmlNodeType nodeType = (_stringBuilder.Length < MinWhitespaceLookahedCount) ? GetTextNodeType(orChars) : XmlNodeType.Text;
                    if (nodeType == XmlNodeType.None)
                    {
                        // ignored whitespace -> skip over the rest of the value unless we already read it all
                        _stringBuilder.Length = 0;
                        if (!fullValue)
                        {
                            while (!ParseText(out _, out _, ref orChars));
                        }
                        goto IgnoredNode;
                    }

                    // set value to curNode
                    _curNode.SetValueNode(nodeType, _stringBuilder.ToString());
                    _stringBuilder.Length = 0;

                    // change parsing state if the full value was not parsed
                    if (!fullValue)
                    {
                        _nextParsingFunction = _parsingFunction;
                        _parsingFunction = ParsingFunction.PartialTextValue;
                    }

                    return true;
                }
            }

        IgnoredNode:

            // ignored whitespace at the end of manually resolved entity
            if (_parsingFunction == ParsingFunction.ReportEndEntity)
            {
                SetupEndEntityNodeInContent();
                _parsingFunction = _nextParsingFunction;
                return true;
            }
            else if (_parsingFunction == ParsingFunction.EntityReference)
            {
                _parsingFunction = _nextNextParsingFunction;
                ParseEntityReference();
                return true;
            }
            return false;
        }

        // Parses a chunk of text starting at ps.charPos.
        //   startPos .... start position of the text chunk that has been parsed (can differ from ps.charPos before the call)
        //   endPos ...... end position of the text chunk that has been parsed (can differ from ps.charPos after the call)
        //   ourOrChars .. all parsed character bigger or equal to 0x20 or-ed (|) into a single int. It can be used for whitespace detection
        //                 (the text has a non-whitespace character if outOrChars > 0x20).
        // Returns true when the whole value has been parsed. Return false when it needs to be called again to get a next chunk of value.
        private bool ParseText(out int startPos, out int endPos, ref int outOrChars)
        {
            Debug.Assert(_ps.chars != null);
            char[] chars = _ps.chars;
            int pos = _ps.charPos;
            int rcount = 0;
            int rpos = -1;
            int orChars = outOrChars;
            char c;

            while (true)
            {
                // parse text content
                while (XmlCharType.IsTextChar(c = chars[pos]))
                {
                    orChars |= (int)c;
                    pos++;
                }

                switch (c)
                {
                    case (char)0x9:
                        pos++;
                        continue;
                    // eol
                    case (char)0xA:
                        pos++;
                        OnNewLine(pos);
                        continue;
                    case (char)0xD:
                        if (chars[pos + 1] == (char)0xA)
                        {
                            if (!_ps.eolNormalized && _parsingMode == ParsingMode.Full)
                            {
                                if (pos - _ps.charPos > 0)
                                {
                                    if (rcount == 0)
                                    {
                                        rcount = 1;
                                        rpos = pos;
                                    }
                                    else
                                    {
                                        ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                                        rpos = pos - rcount;
                                        rcount++;
                                    }
                                }
                                else
                                {
                                    _ps.charPos++;
                                }
                            }
                            pos += 2;
                        }
                        else if (pos + 1 < _ps.charsUsed || _ps.isEof)
                        {
                            if (!_ps.eolNormalized)
                            {
                                chars[pos] = (char)0xA;             // EOL normalization of 0xD
                            }
                            pos++;
                        }
                        else
                        {
                            goto ReadData;
                        }
                        OnNewLine(pos);
                        continue;
                    // some tag
                    case '<':
                        goto ReturnPartialValue;
                    // entity reference
                    case '&':
                        // try to parse char entity inline
                        int charRefEndPos, charCount;
                        EntityType entityType;
                        if ((charRefEndPos = ParseCharRefInline(pos, out charCount, out entityType)) > 0)
                        {
                            if (rcount > 0)
                            {
                                ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                            }
                            rpos = pos - rcount;
                            rcount += (charRefEndPos - pos - charCount);
                            pos = charRefEndPos;

                            if (!XmlCharType.IsWhiteSpace(chars[charRefEndPos - charCount]) ||
                                 (_v1Compat && entityType == EntityType.CharacterDec))
                            {
                                orChars |= 0xFF;
                            }
                        }
                        else
                        {
                            if (pos > _ps.charPos)
                            {
                                goto ReturnPartialValue;
                            }
                            switch (HandleEntityReference(false, EntityExpandType.All, out pos))
                            {
                                case EntityType.Unexpanded:
                                    // make sure we will report EntityReference after the text node
                                    _nextParsingFunction = _parsingFunction;
                                    _parsingFunction = ParsingFunction.EntityReference;
                                    // end the value (returns nothing)
                                    goto NoValue;
                                case EntityType.CharacterDec:
                                    if (!_v1Compat)
                                    {
                                        goto case EntityType.CharacterHex;
                                    }
                                    orChars |= 0xFF;
                                    break;
                                case EntityType.CharacterHex:
                                case EntityType.CharacterNamed:
                                    if (!XmlCharType.IsWhiteSpace(_ps.chars[pos - 1]))
                                    {
                                        orChars |= 0xFF;
                                    }
                                    break;
                                default:
                                    pos = _ps.charPos;
                                    break;
                            }
                            chars = _ps.chars;
                        }
                        continue;
                    case ']':
                        if (_ps.charsUsed - pos < 3 && !_ps.isEof)
                        {
                            goto ReadData;
                        }
                        if (chars[pos + 1] == ']' && chars[pos + 2] == '>')
                        {
                            Throw(pos, SR.Xml_CDATAEndInText);
                        }
                        orChars |= ']';
                        pos++;
                        continue;
                    default:
                        // end of buffer
                        if (pos == _ps.charsUsed)
                        {
                            goto ReadData;
                        }
                        // surrogate chars
                        else
                        {
                            char ch = chars[pos];
                            if (XmlCharType.IsHighSurrogate(ch))
                            {
                                if (pos + 1 == _ps.charsUsed)
                                {
                                    goto ReadData;
                                }
                                pos++;
                                if (XmlCharType.IsLowSurrogate(chars[pos]))
                                {
                                    pos++;
                                    orChars |= ch;
                                    continue;
                                }
                            }
                            int offset = pos - _ps.charPos;
                            if (ZeroEndingStream(pos))
                            {
                                pos = _ps.charPos + offset;
                                goto ReturnPartialValue;
                            }
                            else
                            {
                                ThrowInvalidChar(_ps.chars, _ps.charsUsed, _ps.charPos + offset);
                            }
                            break;
                        }
                }

            ReadData:
                if (pos > _ps.charPos)
                {
                    goto ReturnPartialValue;
                }
                // read new characters into the buffer
                if (ReadData() == 0)
                {
                    if (_ps.charsUsed - _ps.charPos > 0)
                    {
                        if (_ps.chars[_ps.charPos] != (char)0xD && _ps.chars[_ps.charPos] != ']')
                        {
                            Throw(SR.Xml_UnexpectedEOF1);
                        }
                        Debug.Assert(_ps.isEof);
                    }
                    else
                    {
                        if (!InEntity)
                        {
                            // end the value (returns nothing)
                            goto NoValue;
                        }
                        if (HandleEntityEnd(true))
                        {
                            // report EndEntity after the text node
                            _nextParsingFunction = _parsingFunction;
                            _parsingFunction = ParsingFunction.ReportEndEntity;
                            // end the value (returns nothing)
                            goto NoValue;
                        }
                    }
                }
                pos = _ps.charPos;
                chars = _ps.chars;
                continue;
            }
        NoValue:
            startPos = endPos = pos;
            return true;

        ReturnPartialValue:
            if (_parsingMode == ParsingMode.Full && rcount > 0)
            {
                ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
            }
            startPos = _ps.charPos;
            endPos = pos - rcount;
            _ps.charPos = pos;
            outOrChars = orChars;
            return c == '<';
        }

        // When in ParsingState.PartialTextValue, this method parses and caches the rest of the value and stores it in curNode.
        private void FinishPartialValue()
        {
            Debug.Assert(_stringBuilder.Length == 0);
            Debug.Assert(_parsingFunction == ParsingFunction.PartialTextValue ||
                          (_parsingFunction == ParsingFunction.InReadValueChunk && _incReadState == IncrementalReadState.ReadValueChunk_OnPartialValue));

            _curNode.CopyTo(_readValueOffset, _stringBuilder);

            int startPos;
            int endPos;
            int orChars = 0;
            while (!ParseText(out startPos, out endPos, ref orChars))
            {
                _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);
            }
            _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);

            Debug.Assert(_stringBuilder.Length > 0);
            _curNode.SetValue(_stringBuilder.ToString());
            _stringBuilder.Length = 0;
        }

        private void FinishOtherValueIterator()
        {
            switch (_parsingFunction)
            {
                case ParsingFunction.InReadAttributeValue:
                    // do nothing, correct value is already in curNode
                    break;
                case ParsingFunction.InReadValueChunk:
                    if (_incReadState == IncrementalReadState.ReadValueChunk_OnPartialValue)
                    {
                        FinishPartialValue();
                        _incReadState = IncrementalReadState.ReadValueChunk_OnCachedValue;
                    }
                    else
                    {
                        if (_readValueOffset > 0)
                        {
                            _curNode.SetValue(_curNode.StringValue.Substring(_readValueOffset));
                            _readValueOffset = 0;
                        }
                    }
                    break;
                case ParsingFunction.InReadContentAsBinary:
                case ParsingFunction.InReadElementContentAsBinary:
                    switch (_incReadState)
                    {
                        case IncrementalReadState.ReadContentAsBinary_OnPartialValue:
                            FinishPartialValue();
                            _incReadState = IncrementalReadState.ReadContentAsBinary_OnCachedValue;
                            break;
                        case IncrementalReadState.ReadContentAsBinary_OnCachedValue:
                            if (_readValueOffset > 0)
                            {
                                _curNode.SetValue(_curNode.StringValue.Substring(_readValueOffset));
                                _readValueOffset = 0;
                            }
                            break;
                        case IncrementalReadState.ReadContentAsBinary_End:
                            _curNode.SetValue(string.Empty);
                            break;
                    }
                    break;
            }
        }

        // When in ParsingState.PartialTextValue, this method skips over the rest of the partial value.
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private void SkipPartialTextValue()
        {
            Debug.Assert(_parsingFunction == ParsingFunction.PartialTextValue || _parsingFunction == ParsingFunction.InReadValueChunk ||
                          _parsingFunction == ParsingFunction.InReadContentAsBinary || _parsingFunction == ParsingFunction.InReadElementContentAsBinary);
            int orChars = 0;

            _parsingFunction = _nextParsingFunction;
            while (!ParseText(out _, out _, ref orChars));
        }

        private void FinishReadValueChunk()
        {
            Debug.Assert(_parsingFunction == ParsingFunction.InReadValueChunk);

            _readValueOffset = 0;
            if (_incReadState == IncrementalReadState.ReadValueChunk_OnPartialValue)
            {
                Debug.Assert((_index > 0) ? _nextParsingFunction == ParsingFunction.ElementContent : _nextParsingFunction == ParsingFunction.DocumentContent);
                SkipPartialTextValue();
            }
            else
            {
                _parsingFunction = _nextParsingFunction;
                _nextParsingFunction = _nextNextParsingFunction;
            }
        }

        private void FinishReadContentAsBinary()
        {
            Debug.Assert(_parsingFunction == ParsingFunction.InReadContentAsBinary || _parsingFunction == ParsingFunction.InReadElementContentAsBinary);

            _readValueOffset = 0;
            if (_incReadState == IncrementalReadState.ReadContentAsBinary_OnPartialValue)
            {
                Debug.Assert((_index > 0) ? _nextParsingFunction == ParsingFunction.ElementContent : _nextParsingFunction == ParsingFunction.DocumentContent);
                SkipPartialTextValue();
            }
            else
            {
                _parsingFunction = _nextParsingFunction;
                _nextParsingFunction = _nextNextParsingFunction;
            }
            if (_incReadState != IncrementalReadState.ReadContentAsBinary_End)
            {
                while (MoveToNextContentNode(true));
            }
        }

        private void FinishReadElementContentAsBinary()
        {
            FinishReadContentAsBinary();

            if (_curNode.type != XmlNodeType.EndElement)
            {
                Throw(SR.Xml_InvalidNodeType, _curNode.type.ToString());
            }
            // move off the end element
            _outerReader.Read();
        }

        private bool ParseRootLevelWhitespace()
        {
            Debug.Assert(_stringBuilder.Length == 0);

            XmlNodeType nodeType = GetWhitespaceType();

            Debug.Assert(_ps.chars != null);
            if (nodeType == XmlNodeType.None)
            {
                EatWhitespaces(null);
                if (_ps.chars[_ps.charPos] == '<' || _ps.charsUsed - _ps.charPos == 0 || ZeroEndingStream(_ps.charPos))
                {
                    return false;
                }
            }
            else
            {
                _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);
                EatWhitespaces(_stringBuilder);
                if (_ps.chars[_ps.charPos] == '<' || _ps.charsUsed - _ps.charPos == 0 || ZeroEndingStream(_ps.charPos))
                {
                    if (_stringBuilder.Length > 0)
                    {
                        _curNode.SetValueNode(nodeType, _stringBuilder.ToString());
                        _stringBuilder.Length = 0;
                        return true;
                    }
                    return false;
                }
            }

            if (XmlCharType.IsCharData(_ps.chars[_ps.charPos]))
            {
                Throw(SR.Xml_InvalidRootData);
            }
            else
            {
                ThrowInvalidChar(_ps.chars, _ps.charsUsed, _ps.charPos);
            }
            return false;
        }

        private void ParseEntityReference()
        {
            Debug.Assert(_ps.chars != null);
            Debug.Assert(_ps.chars[_ps.charPos] == '&');
            _ps.charPos++;

            _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);
            _curNode.SetNamedNode(XmlNodeType.EntityReference, ParseEntityName());
        }

        private EntityType HandleEntityReference(bool isInAttributeValue, EntityExpandType expandType, out int charRefEndPos)
        {
            Debug.Assert(_ps.chars != null);
            Debug.Assert(_ps.chars[_ps.charPos] == '&');

            if (_ps.charPos + 1 == _ps.charsUsed)
            {
                if (ReadData() == 0)
                {
                    Throw(SR.Xml_UnexpectedEOF1);
                }
            }

            // numeric characters reference
            if (_ps.chars[_ps.charPos + 1] == '#')
            {
                EntityType entityType;
                charRefEndPos = ParseNumericCharRef(expandType != EntityExpandType.OnlyGeneral, null, out entityType);
                Debug.Assert(entityType == EntityType.CharacterDec || entityType == EntityType.CharacterHex);
                return entityType;
            }
            // named reference
            else
            {
                // named character reference
                charRefEndPos = ParseNamedCharRef(expandType != EntityExpandType.OnlyGeneral, null);
                if (charRefEndPos >= 0)
                {
                    return EntityType.CharacterNamed;
                }

                // general entity reference
                // NOTE: XmlValidatingReader compatibility mode: expand all entities in attribute values
                // general entity reference
                if (expandType == EntityExpandType.OnlyCharacter ||
                     (_entityHandling != EntityHandling.ExpandEntities &&
                       (!isInAttributeValue || !_validatingReaderCompatFlag)))
                {
                    return EntityType.Unexpanded;
                }
                int endPos;

                _ps.charPos++;
                int savedLinePos = _ps.LinePos;
                try
                {
                    endPos = ParseName();
                }
                catch (XmlException)
                {
                    Throw(SR.Xml_ErrorParsingEntityName, _ps.LineNo, savedLinePos);
                    return EntityType.Skipped;
                }

                // check ';'
                if (_ps.chars[endPos] != ';')
                {
                    ThrowUnexpectedToken(endPos, ";");
                }

                int entityLinePos = _ps.LinePos;
                string entityName = _nameTable.Add(_ps.chars, _ps.charPos, endPos - _ps.charPos);
                _ps.charPos = endPos + 1;
                charRefEndPos = -1;

                EntityType entType = HandleGeneralEntityReference(entityName, isInAttributeValue, false, entityLinePos);
                _reportedBaseUri = _ps.baseUriStr;
                _reportedEncoding = _ps.encoding;
                return entType;
            }
        }

        // returns true == continue parsing
        // return false == unexpanded external entity, stop parsing and return
        private EntityType HandleGeneralEntityReference(string name, bool isInAttributeValue, bool pushFakeEntityIfNullResolver, int entityStartLinePos)
        {
            IDtdEntityInfo? entity = null;

            if (_dtdInfo == null && _fragmentParserContext != null && _fragmentParserContext.HasDtdInfo && _dtdProcessing == DtdProcessing.Parse)
            {
                ParseDtdFromParserContext();
            }

            if (_dtdInfo == null ||
                 ((entity = _dtdInfo.LookupEntity(name)) == null))
            {
                if (_disableUndeclaredEntityCheck)
                {
                    SchemaEntity schemaEntity = new SchemaEntity(new XmlQualifiedName(name), false);
                    schemaEntity.Text = string.Empty;
                    entity = schemaEntity;
                }
                else
                    Throw(SR.Xml_UndeclaredEntity, name, _ps.LineNo, entityStartLinePos);
            }

            Debug.Assert(entity != null);

            if (entity.IsUnparsedEntity)
            {
                if (_disableUndeclaredEntityCheck)
                {
                    SchemaEntity schemaEntity = new SchemaEntity(new XmlQualifiedName(name), false);
                    schemaEntity.Text = string.Empty;
                    entity = schemaEntity;
                }
                else
                    Throw(SR.Xml_UnparsedEntityRef, name, _ps.LineNo, entityStartLinePos);
            }

            if (_standalone && entity.IsDeclaredInExternal)
            {
                Throw(SR.Xml_ExternalEntityInStandAloneDocument, entity.Name, _ps.LineNo, entityStartLinePos);
            }

            if (entity.IsExternal)
            {
                if (isInAttributeValue)
                {
                    Throw(SR.Xml_ExternalEntityInAttValue, name, _ps.LineNo, entityStartLinePos);
                    return EntityType.Skipped;
                }

                if (_parsingMode == ParsingMode.SkipContent)
                {
                    return EntityType.Skipped;
                }

                if (IsResolverNull)
                {
                    if (pushFakeEntityIfNullResolver)
                    {
                        PushExternalEntity(entity);
                        _curNode.entityId = _ps.entityId;
                        return EntityType.FakeExpanded;
                    }
                    return EntityType.Skipped;
                }
                else
                {
                    PushExternalEntity(entity);
                    _curNode.entityId = _ps.entityId;
                    return (isInAttributeValue && _validatingReaderCompatFlag) ? EntityType.ExpandedInAttribute : EntityType.Expanded;
                }
            }
            else
            {
                if (_parsingMode == ParsingMode.SkipContent)
                {
                    return EntityType.Skipped;
                }

                PushInternalEntity(entity);

                _curNode.entityId = _ps.entityId;
                return (isInAttributeValue && _validatingReaderCompatFlag) ? EntityType.ExpandedInAttribute : EntityType.Expanded;
            }
        }

        private bool InEntity
        {
            get
            {
                return _parsingStatesStackTop >= 0;
            }
        }

        // return true if EndEntity node should be reported. The entity is stored in lastEntity.
        private bool HandleEntityEnd(bool checkEntityNesting)
        {
            if (_parsingStatesStackTop == -1)
            {
                Debug.Fail($"Unexpected parsing states stack top {_parsingStatesStackTop}");
                Throw(SR.Xml_InternalError);
            }

            if (_ps.entityResolvedManually)
            {
                _index--;

                if (checkEntityNesting)
                {
                    if (_ps.entityId != _nodes[_index].entityId)
                    {
                        Throw(SR.Xml_IncompleteEntity);
                    }
                }

                _lastEntity = _ps.entity;  // save last entity for the EndEntity node

                PopEntity();
                return true;
            }
            else
            {
                if (checkEntityNesting)
                {
                    if (_ps.entityId != _nodes[_index].entityId)
                    {
                        Throw(SR.Xml_IncompleteEntity);
                    }
                }

                PopEntity();

                _reportedEncoding = _ps.encoding;
                _reportedBaseUri = _ps.baseUriStr;
                return false;
            }
        }

        private void SetupEndEntityNodeInContent()
        {
            Debug.Assert(_lastEntity != null);

            _reportedEncoding = _ps.encoding;
            _reportedBaseUri = _ps.baseUriStr;

            _curNode = _nodes[_index];
            Debug.Assert(_curNode.depth == _index);
            _curNode.SetNamedNode(XmlNodeType.EndEntity, _lastEntity.Name);
            _curNode.lineInfo.Set(_ps.lineNo, _ps.LinePos - 1);

            if (_index == 0 && _parsingFunction == ParsingFunction.ElementContent)
            {
                _parsingFunction = ParsingFunction.DocumentContent;
            }
        }

        private void SetupEndEntityNodeInAttribute()
        {
            _curNode = _nodes[_index + _attrCount + 1];
            Debug.Assert(_curNode.type == XmlNodeType.EntityReference);
            Debug.Assert(_lastEntity != null);
            Debug.Assert(Ref.Equal(_lastEntity.Name, _curNode.localName));
            _curNode.lineInfo.linePos += _curNode.localName.Length;
            _curNode.type = XmlNodeType.EndEntity;
        }

        private bool ParsePI()
        {
            return ParsePI(null);
        }

        // Parses processing instruction; if piInDtdStringBuilder != null, the processing instruction is in DTD and
        // it will be saved in the passed string builder (target, whitespace & value).
        private bool ParsePI(StringBuilder? piInDtdStringBuilder)
        {
            if (_parsingMode == ParsingMode.Full)
            {
                _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);
            }

            Debug.Assert(_stringBuilder.Length == 0);

            // parse target name
            int nameEndPos = ParseName();
            Debug.Assert(_ps.chars != null);
            string target = _nameTable.Add(_ps.chars, _ps.charPos, nameEndPos - _ps.charPos);

            if (string.Equals(target, "xml", StringComparison.OrdinalIgnoreCase))
            {
                Throw(target.Equals("xml") ? SR.Xml_XmlDeclNotFirst : SR.Xml_InvalidPIName, target);
            }
            _ps.charPos = nameEndPos;

            if (piInDtdStringBuilder == null)
            {
                if (!_ignorePIs && _parsingMode == ParsingMode.Full)
                {
                    _curNode.SetNamedNode(XmlNodeType.ProcessingInstruction, target);
                }
            }
            else
            {
                piInDtdStringBuilder.Append(target);
            }

            // check mandatory whitespace
            char ch = _ps.chars[_ps.charPos];
            Debug.Assert(_ps.charPos < _ps.charsUsed);
            if (EatWhitespaces(piInDtdStringBuilder) == 0)
            {
                if (_ps.charsUsed - _ps.charPos < 2)
                {
                    ReadData();
                }
                if (ch != '?' || _ps.chars[_ps.charPos + 1] != '>')
                {
                    Throw(SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(_ps.chars, _ps.charsUsed, _ps.charPos));
                }
            }

            // scan processing instruction value
            int startPos, endPos;
            if (ParsePIValue(out startPos, out endPos))
            {
                if (piInDtdStringBuilder == null)
                {
                    if (_ignorePIs)
                    {
                        return false;
                    }
                    if (_parsingMode == ParsingMode.Full)
                    {
                        _curNode.SetValue(_ps.chars, startPos, endPos - startPos);
                    }
                }
                else
                {
                    piInDtdStringBuilder.Append(_ps.chars, startPos, endPos - startPos);
                }
            }
            else
            {
                StringBuilder sb;
                if (piInDtdStringBuilder == null)
                {
                    if (_ignorePIs || _parsingMode != ParsingMode.Full)
                    {
                        while (!ParsePIValue(out _, out _));
                        return false;
                    }
                    sb = _stringBuilder;
                    Debug.Assert(_stringBuilder.Length == 0);
                }
                else
                {
                    sb = piInDtdStringBuilder;
                }

                do
                {
                    sb.Append(_ps.chars, startPos, endPos - startPos);
                } while (!ParsePIValue(out startPos, out endPos));
                sb.Append(_ps.chars, startPos, endPos - startPos);

                if (piInDtdStringBuilder == null)
                {
                    _curNode.SetValue(_stringBuilder.ToString());
                    _stringBuilder.Length = 0;
                }
            }

            return true;
        }

        private bool ParsePIValue(out int outStartPos, out int outEndPos)
        {
            // read new characters into the buffer
            if (_ps.charsUsed - _ps.charPos < 2)
            {
                if (ReadData() == 0)
                {
                    Throw(_ps.charsUsed, SR.Xml_UnexpectedEOF, "PI");
                }
            }

            Debug.Assert(_ps.chars != null);
            int pos = _ps.charPos;
            char[] chars = _ps.chars;
            int rcount = 0;
            int rpos = -1;

            while (true)
            {
                char tmpch;
                while (XmlCharType.IsTextChar(tmpch = chars[pos]) && tmpch != '?')
                {
                    pos++;
                }

                switch (chars[pos])
                {
                    // possibly end of PI
                    case '?':
                        if (chars[pos + 1] == '>')
                        {
                            if (rcount > 0)
                            {
                                Debug.Assert(!_ps.eolNormalized);
                                ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                                outEndPos = pos - rcount;
                            }
                            else
                            {
                                outEndPos = pos;
                            }
                            outStartPos = _ps.charPos;
                            _ps.charPos = pos + 2;
                            return true;
                        }
                        else if (pos + 1 == _ps.charsUsed)
                        {
                            goto ReturnPartial;
                        }
                        else
                        {
                            pos++;
                            continue;
                        }
                    // eol
                    case (char)0xA:
                        pos++;
                        OnNewLine(pos);
                        continue;
                    case (char)0xD:
                        if (chars[pos + 1] == (char)0xA)
                        {
                            if (!_ps.eolNormalized && _parsingMode == ParsingMode.Full)
                            {
                                // EOL normalization of 0xD 0xA
                                if (pos - _ps.charPos > 0)
                                {
                                    if (rcount == 0)
                                    {
                                        rcount = 1;
                                        rpos = pos;
                                    }
                                    else
                                    {
                                        ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                                        rpos = pos - rcount;
                                        rcount++;
                                    }
                                }
                                else
                                {
                                    _ps.charPos++;
                                }
                            }
                            pos += 2;
                        }
                        else if (pos + 1 < _ps.charsUsed || _ps.isEof)
                        {
                            if (!_ps.eolNormalized)
                            {
                                chars[pos] = (char)0xA;             // EOL normalization of 0xD
                            }
                            pos++;
                        }
                        else
                        {
                            goto ReturnPartial;
                        }
                        OnNewLine(pos);
                        continue;
                    case '<':
                    case '&':
                    case ']':
                    case (char)0x9:
                        pos++;
                        continue;
                    default:
                        // end of buffer
                        if (pos == _ps.charsUsed)
                        {
                            goto ReturnPartial;
                        }
                        // surrogate characters
                        else
                        {
                            char ch = chars[pos];
                            if (XmlCharType.IsHighSurrogate(ch))
                            {
                                if (pos + 1 == _ps.charsUsed)
                                {
                                    goto ReturnPartial;
                                }
                                pos++;
                                if (XmlCharType.IsLowSurrogate(chars[pos]))
                                {
                                    pos++;
                                    continue;
                                }
                            }
                            ThrowInvalidChar(chars, _ps.charsUsed, pos);
                            break;
                        }
                }
            }

        ReturnPartial:
            if (rcount > 0)
            {
                ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                outEndPos = pos - rcount;
            }
            else
            {
                outEndPos = pos;
            }
            outStartPos = _ps.charPos;
            _ps.charPos = pos;
            return false;
        }

        private bool ParseComment()
        {
            if (_ignoreComments)
            {
                ParsingMode oldParsingMode = _parsingMode;
                _parsingMode = ParsingMode.SkipNode;
                ParseCDataOrComment(XmlNodeType.Comment);
                _parsingMode = oldParsingMode;
                return false;
            }
            else
            {
                ParseCDataOrComment(XmlNodeType.Comment);
                return true;
            }
        }

        private void ParseCData()
        {
            ParseCDataOrComment(XmlNodeType.CDATA);
        }

        // Parses CDATA section or comment
        private void ParseCDataOrComment(XmlNodeType type)
        {
            int startPos, endPos;

            if (_parsingMode == ParsingMode.Full)
            {
                _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);
                Debug.Assert(_stringBuilder.Length == 0);
                if (ParseCDataOrComment(type, out startPos, out endPos))
                {
                    Debug.Assert(_ps.chars != null);
                    _curNode.SetValueNode(type, _ps.chars, startPos, endPos - startPos);
                }
                else
                {
                    do
                    {
                        _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);
                    } while (!ParseCDataOrComment(type, out startPos, out endPos));

                    _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);
                    _curNode.SetValueNode(type, _stringBuilder.ToString());
                    _stringBuilder.Length = 0;
                }
            }
            else
            {
                while (!ParseCDataOrComment(type, out _, out _));
            }
        }

        // Parses a chunk of CDATA section or comment. Returns true when the end of CDATA or comment was reached.
        private bool ParseCDataOrComment(XmlNodeType type, out int outStartPos, out int outEndPos)
        {
            if (_ps.charsUsed - _ps.charPos < 3)
            {
                // read new characters into the buffer
                if (ReadData() == 0)
                {
                    Throw(SR.Xml_UnexpectedEOF, (type == XmlNodeType.Comment) ? "Comment" : "CDATA");
                }
            }

            Debug.Assert(_ps.chars != null);
            int pos = _ps.charPos;
            char[] chars = _ps.chars;
            int rcount = 0;
            int rpos = -1;
            char stopChar = (type == XmlNodeType.Comment) ? '-' : ']';

            while (true)
            {
                char tmpch;
                while (XmlCharType.IsTextChar(tmpch = chars[pos]) && tmpch != stopChar)
                {
                    pos++;
                }

                // possibly end of comment or cdata section
                if (chars[pos] == stopChar)
                {
                    if (chars[pos + 1] == stopChar)
                    {
                        if (chars[pos + 2] == '>')
                        {
                            if (rcount > 0)
                            {
                                Debug.Assert(!_ps.eolNormalized);
                                ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                                outEndPos = pos - rcount;
                            }
                            else
                            {
                                outEndPos = pos;
                            }
                            outStartPos = _ps.charPos;
                            _ps.charPos = pos + 3;
                            return true;
                        }
                        else if (pos + 2 == _ps.charsUsed)
                        {
                            goto ReturnPartial;
                        }
                        else if (type == XmlNodeType.Comment)
                        {
                            Throw(pos, SR.Xml_InvalidCommentChars);
                        }
                    }
                    else if (pos + 1 == _ps.charsUsed)
                    {
                        goto ReturnPartial;
                    }
                    pos++;
                    continue;
                }
                else
                {
                    switch (chars[pos])
                    {
                        // eol
                        case (char)0xA:
                            pos++;
                            OnNewLine(pos);
                            continue;
                        case (char)0xD:
                            if (chars[pos + 1] == (char)0xA)
                            {
                                // EOL normalization of 0xD 0xA - shift the buffer
                                if (!_ps.eolNormalized && _parsingMode == ParsingMode.Full)
                                {
                                    if (pos - _ps.charPos > 0)
                                    {
                                        if (rcount == 0)
                                        {
                                            rcount = 1;
                                            rpos = pos;
                                        }
                                        else
                                        {
                                            ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                                            rpos = pos - rcount;
                                            rcount++;
                                        }
                                    }
                                    else
                                    {
                                        _ps.charPos++;
                                    }
                                }
                                pos += 2;
                            }
                            else if (pos + 1 < _ps.charsUsed || _ps.isEof)
                            {
                                if (!_ps.eolNormalized)
                                {
                                    chars[pos] = (char)0xA;             // EOL normalization of 0xD
                                }
                                pos++;
                            }
                            else
                            {
                                goto ReturnPartial;
                            }
                            OnNewLine(pos);
                            continue;
                        case '<':
                        case '&':
                        case ']':
                        case (char)0x9:
                            pos++;
                            continue;
                        default:
                            // end of buffer
                            if (pos == _ps.charsUsed)
                            {
                                goto ReturnPartial;
                            }
                            // surrogate characters
                            char ch = chars[pos];
                            if (XmlCharType.IsHighSurrogate(ch))
                            {
                                if (pos + 1 == _ps.charsUsed)
                                {
                                    goto ReturnPartial;
                                }
                                pos++;
                                if (XmlCharType.IsLowSurrogate(chars[pos]))
                                {
                                    pos++;
                                    continue;
                                }
                            }
                            ThrowInvalidChar(chars, _ps.charsUsed, pos);
                            break;
                    }
                }

            ReturnPartial:
                if (rcount > 0)
                {
                    ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                    outEndPos = pos - rcount;
                }
                else
                {
                    outEndPos = pos;
                }
                outStartPos = _ps.charPos;

                _ps.charPos = pos;
                return false; // false == parsing of comment or CData section is not finished yet, must be called again
            }
        }

        // Parses DOCTYPE declaration
        private bool ParseDoctypeDecl()
        {
            if (_dtdProcessing == DtdProcessing.Prohibit)
            {
                ThrowWithoutLineInfo(_v1Compat ? SR.Xml_DtdIsProhibited : SR.Xml_DtdIsProhibitedEx);
            }

            // parse 'DOCTYPE'
            while (_ps.charsUsed - _ps.charPos < 8)
            {
                if (ReadData() == 0)
                {
                    Throw(SR.Xml_UnexpectedEOF, "DOCTYPE");
                }
            }

            if (!XmlConvert.StrEqual(_ps.chars, _ps.charPos, 7, "DOCTYPE"))
            {
                ThrowUnexpectedToken((!_rootElementParsed && _dtdInfo == null) ? "DOCTYPE" : "<!--");
            }

            Debug.Assert(_ps.chars != null);
            if (!XmlCharType.IsWhiteSpace(_ps.chars[_ps.charPos + 7]))
            {
                ThrowExpectingWhitespace(_ps.charPos + 7);
            }

            if (_dtdInfo != null)
            {
                Throw(_ps.charPos - 2, SR.Xml_MultipleDTDsProvided);  // position just before <!DOCTYPE
            }

            if (_rootElementParsed)
            {
                Throw(_ps.charPos - 2, SR.Xml_DtdAfterRootElement);
            }

            _ps.charPos += 8;

            EatWhitespaces(null);

            if (_dtdProcessing == DtdProcessing.Parse)
            {
                _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);

                ParseDtd();

                _nextParsingFunction = _parsingFunction;
                _parsingFunction = ParsingFunction.ResetAttributesRootLevel;
                return true;
            }
            // Skip DTD
            else
            {
                Debug.Assert(_dtdProcessing == DtdProcessing.Ignore);

                SkipDtd();
                return false;
            }
        }

        private void ParseDtd()
        {
            IDtdParser dtdParser = DtdParser.Create();

            _dtdInfo = dtdParser.ParseInternalDtd(new DtdParserProxy(this), true);

            if ((_validatingReaderCompatFlag || !_v1Compat) && (_dtdInfo.HasDefaultAttributes || _dtdInfo.HasNonCDataAttributes))
            {
                _addDefaultAttributesAndNormalize = true;
            }

            _curNode.SetNamedNode(XmlNodeType.DocumentType, _dtdInfo.Name.ToString(), string.Empty, null);
            _curNode.SetValue(_dtdInfo.InternalDtdSubset);
        }

        private void SkipDtd()
        {
            // parse dtd name
            int pos = ParseQName(out _);
            _ps.charPos = pos;

            // check whitespace
            EatWhitespaces(null);

            // PUBLIC Id
            Debug.Assert(_ps.chars != null);
            if (_ps.chars[_ps.charPos] == 'P')
            {
                // make sure we have enough characters
                while (_ps.charsUsed - _ps.charPos < 6)
                {
                    if (ReadData() == 0)
                    {
                        Throw(SR.Xml_UnexpectedEOF1);
                    }
                }
                // check 'PUBLIC'
                if (!XmlConvert.StrEqual(_ps.chars, _ps.charPos, 6, "PUBLIC"))
                {
                    ThrowUnexpectedToken("PUBLIC");
                }
                _ps.charPos += 6;

                // check whitespace
                if (EatWhitespaces(null) == 0)
                {
                    ThrowExpectingWhitespace(_ps.charPos);
                }

                // parse PUBLIC value
                SkipPublicOrSystemIdLiteral();

                // check whitespace
                if (EatWhitespaces(null) == 0)
                {
                    ThrowExpectingWhitespace(_ps.charPos);
                }

                // parse SYSTEM value
                SkipPublicOrSystemIdLiteral();

                EatWhitespaces(null);
            }
            else if (_ps.chars[_ps.charPos] == 'S')
            {
                // make sure we have enough characters
                while (_ps.charsUsed - _ps.charPos < 6)
                {
                    if (ReadData() == 0)
                    {
                        Throw(SR.Xml_UnexpectedEOF1);
                    }
                }
                // check 'SYSTEM'
                if (!XmlConvert.StrEqual(_ps.chars, _ps.charPos, 6, "SYSTEM"))
                {
                    ThrowUnexpectedToken("SYSTEM");
                }
                _ps.charPos += 6;

                // check whitespace
                if (EatWhitespaces(null) == 0)
                {
                    ThrowExpectingWhitespace(_ps.charPos);
                }

                // parse SYSTEM value
                SkipPublicOrSystemIdLiteral();

                EatWhitespaces(null);
            }
            else if (_ps.chars[_ps.charPos] != '[' && _ps.chars[_ps.charPos] != '>')
            {
                Throw(SR.Xml_ExpectExternalOrClose);
            }

            // internal DTD
            if (_ps.chars[_ps.charPos] == '[')
            {
                _ps.charPos++;

                SkipUntil(']', true);

                EatWhitespaces(null);
                if (_ps.chars[_ps.charPos] != '>')
                {
                    ThrowUnexpectedToken(">");
                }
            }
            else if (_ps.chars[_ps.charPos] == '>')
            {
                _curNode.SetValue(string.Empty);
            }
            else
            {
                Throw(SR.Xml_ExpectSubOrClose);
            }
            _ps.charPos++;
        }

        private void SkipPublicOrSystemIdLiteral()
        {
            Debug.Assert(_ps.chars != null);
            // check quote char
            char quoteChar = _ps.chars[_ps.charPos];
            if (quoteChar != '"' && quoteChar != '\'')
            {
                ThrowUnexpectedToken("\"", "'");
            }

            _ps.charPos++;
            SkipUntil(quoteChar, false);
        }

        private void SkipUntil(char stopChar, bool recognizeLiterals)
        {
            bool inLiteral = false;
            bool inComment = false;
            bool inPI = false;
            char literalQuote = '"';

            Debug.Assert(_ps.chars != null);
            char[] chars = _ps.chars;
            int pos = _ps.charPos;

            while (true)
            {
                char ch;

                while (XmlCharType.IsAttributeValueChar(ch = chars[pos]) && chars[pos] != stopChar && ch != '-' && ch != '?')
                {
                    pos++;
                }

                // closing stopChar outside of literal and ignore/include sections -> save value & return
                if (ch == stopChar && !inLiteral)
                {
                    _ps.charPos = pos + 1;
                    return;
                }

                // handle the special character
                _ps.charPos = pos;
                switch (ch)
                {
                    // eol
                    case (char)0xA:
                        pos++;
                        OnNewLine(pos);
                        continue;
                    case (char)0xD:
                        if (chars[pos + 1] == (char)0xA)
                        {
                            pos += 2;
                        }
                        else if (pos + 1 < _ps.charsUsed || _ps.isEof)
                        {
                            pos++;
                        }
                        else
                        {
                            goto ReadData;
                        }
                        OnNewLine(pos);
                        continue;

                    // comment, PI
                    case '<':
                        // processing instruction
                        if (chars[pos + 1] == '?')
                        {
                            if (recognizeLiterals && !inLiteral && !inComment)
                            {
                                inPI = true;
                                pos += 2;
                                continue;
                            }
                        }
                        // comment
                        else if (chars[pos + 1] == '!')
                        {
                            if (pos + 3 >= _ps.charsUsed && !_ps.isEof)
                            {
                                goto ReadData;
                            }
                            if (chars[pos + 2] == '-' && chars[pos + 3] == '-')
                            {
                                if (recognizeLiterals && !inLiteral && !inPI)
                                {
                                    inComment = true;
                                    pos += 4;
                                    continue;
                                }
                            }
                        }
                        // need more data
                        else if (pos + 1 >= _ps.charsUsed && !_ps.isEof)
                        {
                            goto ReadData;
                        }
                        pos++;
                        continue;
                    case '-':
                        // end of comment
                        if (inComment)
                        {
                            if (pos + 2 >= _ps.charsUsed && !_ps.isEof)
                            {
                                goto ReadData;
                            }
                            if (chars[pos + 1] == '-' && chars[pos + 2] == '>')
                            {
                                inComment = false;
                                pos += 2;
                                continue;
                            }
                        }
                        pos++;
                        continue;

                    case '?':
                        // end of processing instruction
                        if (inPI)
                        {
                            if (pos + 1 >= _ps.charsUsed && !_ps.isEof)
                            {
                                goto ReadData;
                            }
                            if (chars[pos + 1] == '>')
                            {
                                inPI = false;
                                pos += 1;
                                continue;
                            }
                        }
                        pos++;
                        continue;

                    case (char)0x9:
                    case '>':
                    case ']':
                    case '&':
                        pos++;
                        continue;
                    case '"':
                    case '\'':
                        if (inLiteral)
                        {
                            if (literalQuote == ch)
                            {
                                inLiteral = false;
                            }
                        }
                        else
                        {
                            if (recognizeLiterals && !inComment && !inPI)
                            {
                                inLiteral = true;
                                literalQuote = ch;
                            }
                        }
                        pos++;
                        continue;
                    default:
                        // end of buffer
                        if (pos == _ps.charsUsed)
                        {
                            goto ReadData;
                        }
                        // surrogate chars
                        else
                        {
                            char tmpCh = chars[pos];
                            if (XmlCharType.IsHighSurrogate(tmpCh))
                            {
                                if (pos + 1 == _ps.charsUsed)
                                {
                                    goto ReadData;
                                }
                                pos++;
                                if (XmlCharType.IsLowSurrogate(chars[pos]))
                                {
                                    pos++;
                                    continue;
                                }
                            }
                            ThrowInvalidChar(chars, _ps.charsUsed, pos);
                            break;
                        }
                }

            ReadData:
                // read new characters into the buffer
                if (ReadData() == 0)
                {
                    if (_ps.charsUsed - _ps.charPos > 0)
                    {
                        if (_ps.chars[_ps.charPos] != (char)0xD)
                        {
                            Debug.Fail("We should never get to this point.");
                            Throw(SR.Xml_UnexpectedEOF1);
                        }
                        Debug.Assert(_ps.isEof);
                    }
                    else
                    {
                        Throw(SR.Xml_UnexpectedEOF1);
                    }
                }
                chars = _ps.chars;
                pos = _ps.charPos;
            }
        }

        private int EatWhitespaces(StringBuilder? sb)
        {
            int pos = _ps.charPos;
            int wsCount = 0;
            Debug.Assert(_ps.chars != null);
            char[] chars = _ps.chars;

            while (true)
            {
                while (true)
                {
                    switch (chars[pos])
                    {
                        case (char)0xA:
                            pos++;
                            OnNewLine(pos);
                            continue;
                        case (char)0xD:
                            if (chars[pos + 1] == (char)0xA)
                            {
                                int tmp1 = pos - _ps.charPos;
                                if (sb != null && !_ps.eolNormalized)
                                {
                                    if (tmp1 > 0)
                                    {
                                        sb.Append(chars, _ps.charPos, tmp1);
                                        wsCount += tmp1;
                                    }
                                    _ps.charPos = pos + 1;
                                }
                                pos += 2;
                            }
                            else if (pos + 1 < _ps.charsUsed || _ps.isEof)
                            {
                                if (!_ps.eolNormalized)
                                {
                                    chars[pos] = (char)0xA;             // EOL normalization of 0xD
                                }
                                pos++;
                            }
                            else
                            {
                                goto ReadData;
                            }
                            OnNewLine(pos);
                            continue;
                        case (char)0x9:
                        case (char)0x20:
                            pos++;
                            continue;
                        default:
                            if (pos == _ps.charsUsed)
                            {
                                goto ReadData;
                            }
                            else
                            {
                                int tmp2 = pos - _ps.charPos;
                                if (tmp2 > 0)
                                {
                                    if (sb != null)
                                    {
                                        sb.Append(_ps.chars, _ps.charPos, tmp2);
                                    }
                                    _ps.charPos = pos;
                                    wsCount += tmp2;
                                }
                                return wsCount;
                            }
                    }
                }

            ReadData:
                int tmp3 = pos - _ps.charPos;
                if (tmp3 > 0)
                {
                    if (sb != null)
                    {
                        sb.Append(_ps.chars, _ps.charPos, tmp3);
                    }
                    _ps.charPos = pos;
                    wsCount += tmp3;
                }

                if (ReadData() == 0)
                {
                    if (_ps.charsUsed - _ps.charPos == 0)
                    {
                        return wsCount;
                    }
                    if (_ps.chars[_ps.charPos] != (char)0xD)
                    {
                        Debug.Fail("We should never get to this point.");
                        Throw(SR.Xml_UnexpectedEOF1);
                    }
                    Debug.Assert(_ps.isEof);
                }
                pos = _ps.charPos;
                chars = _ps.chars;
            }
        }

        private int ParseCharRefInline(int startPos, out int charCount, out EntityType entityType)
        {
            Debug.Assert(_ps.chars != null);
            Debug.Assert(_ps.chars[startPos] == '&');
            if (_ps.chars[startPos + 1] == '#')
            {
                return ParseNumericCharRefInline(startPos, true, null, out charCount, out entityType);
            }
            else
            {
                charCount = 1;
                entityType = EntityType.CharacterNamed;
                return ParseNamedCharRefInline(startPos, true, null);
            }
        }

        // Parses numeric character entity reference (e.g. &#32; &#x20;).
        //      - replaces the last one or two character of the entity reference (';' and the character before) with the referenced
        //        character or surrogates pair (if expand == true)
        //      - returns position of the end of the character reference, that is of the character next to the original ';'
        //      - if (expand == true) then ps.charPos is changed to point to the replaced character
        private int ParseNumericCharRef(bool expand, StringBuilder? internalSubsetBuilder, out EntityType entityType)
        {
            while (true)
            {
                int newPos;
                int charCount;
                switch (newPos = ParseNumericCharRefInline(_ps.charPos, expand, internalSubsetBuilder, out charCount, out entityType))
                {
                    case -2:
                        // read new characters in the buffer
                        if (ReadData() == 0)
                        {
                            Throw(SR.Xml_UnexpectedEOF);
                        }

                        Debug.Assert(_ps.chars != null);
                        Debug.Assert(_ps.chars[_ps.charPos] == '&');
                        continue;
                    default:
                        if (expand)
                        {
                            _ps.charPos = newPos - charCount;
                        }
                        return newPos;
                }
            }
        }

        // Parses numeric character entity reference (e.g. &#32; &#x20;).
        // Returns -2 if more data is needed in the buffer
        // Otherwise
        //      - replaces the last one or two character of the entity reference (';' and the character before) with the referenced
        //        character or surrogates pair (if expand == true)
        //      - returns position of the end of the character reference, that is of the character next to the original ';'
        private int ParseNumericCharRefInline(int startPos, bool expand, StringBuilder? internalSubsetBuilder, out int charCount, out EntityType entityType)
        {
            Debug.Assert(_ps.chars != null);
            Debug.Assert(_ps.chars[startPos] == '&' && _ps.chars[startPos + 1] == '#');

            int val;
            int pos;
            char[] chars;

            val = 0;
            string? badDigitExceptionString = null;
            chars = _ps.chars;
            pos = startPos + 2;
            charCount = 0;
            int digitPos = 0;

            try
            {
                if (chars[pos] == 'x')
                {
                    pos++;
                    digitPos = pos;
                    badDigitExceptionString = SR.Xml_BadHexEntity;
                    while (true)
                    {
                        int ch = HexConverter.FromChar(chars[pos]);
                        if (ch == 0xFF)
                            break;
                        val = checked(val * 16 + ch);
                        pos++;
                    }
                    entityType = EntityType.CharacterHex;
                }
                else if (pos < _ps.charsUsed)
                {
                    digitPos = pos;
                    badDigitExceptionString = SR.Xml_BadDecimalEntity;
                    while (chars[pos] >= '0' && chars[pos] <= '9')
                    {
                        val = checked(val * 10 + chars[pos] - '0');
                        pos++;
                    }
                    entityType = EntityType.CharacterDec;
                }
                else
                {
                    // need more data in the buffer
                    entityType = EntityType.Skipped;
                    return -2;
                }
            }
            catch (OverflowException e)
            {
                _ps.charPos = pos;
                entityType = EntityType.Skipped;
                Throw(SR.Xml_CharEntityOverflow, (string?)null, e);
            }

            Debug.Assert(badDigitExceptionString != null);

            if (chars[pos] != ';' || digitPos == pos)
            {
                if (pos == _ps.charsUsed)
                {
                    // need more data in the buffer
                    return -2;
                }
                else
                {
                    Throw(pos, badDigitExceptionString);
                }
            }

            // simple character
            if (val <= char.MaxValue)
            {
                char ch = (char)val;
                if (!XmlCharType.IsCharData(ch) &&
                     ((_v1Compat && _normalize) || (!_v1Compat && _checkCharacters)))
                {
                    Throw((_ps.chars[startPos + 2] == 'x') ? startPos + 3 : startPos + 2, SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(ch, '\0'));
                }

                if (expand)
                {
                    if (internalSubsetBuilder != null)
                    {
                        internalSubsetBuilder.Append(_ps.chars, _ps.charPos, pos - _ps.charPos + 1);
                    }
                    chars[pos] = ch;
                }
                charCount = 1;
                return pos + 1;
            }
            // surrogate
            else
            {
                char low, high;
                XmlCharType.SplitSurrogateChar(val, out low, out high);

                if (_normalize)
                {
                    if (XmlCharType.IsHighSurrogate(high))
                    {
                        if (XmlCharType.IsLowSurrogate(low))
                        {
                            goto Return;
                        }
                    }
                    Throw((_ps.chars[startPos + 2] == 'x') ? startPos + 3 : startPos + 2, SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(high, low));
                }

            Return:
                Debug.Assert(pos > 0);
                if (expand)
                {
                    if (internalSubsetBuilder != null)
                    {
                        internalSubsetBuilder.Append(_ps.chars, _ps.charPos, pos - _ps.charPos + 1);
                    }
                    chars[pos - 1] = (char)high;
                    chars[pos] = (char)low;
                }
                charCount = 2;
                return pos + 1;
            }
        }

        // Parses named character entity reference (&amp; &apos; &lt; &gt; &quot;).
        // Returns -1 if the reference is not a character entity reference.
        // Otherwise
        //      - replaces the last character of the entity reference (';') with the referenced character (if expand == true)
        //      - returns position of the end of the character reference, that is of the character next to the original ';'
        //      - if (expand == true) then ps.charPos is changed to point to the replaced character
        private int ParseNamedCharRef(bool expand, StringBuilder? internalSubsetBuilder)
        {
            while (true)
            {
                int newPos;
                switch (newPos = ParseNamedCharRefInline(_ps.charPos, expand, internalSubsetBuilder))
                {
                    case -1:
                        return -1;
                    case -2:
                        // read new characters in the buffer
                        if (ReadData() == 0)
                        {
                            return -1;
                        }

                        Debug.Assert(_ps.chars != null);
                        Debug.Assert(_ps.chars[_ps.charPos] == '&');
                        continue;
                    default:
                        if (expand)
                        {
                            _ps.charPos = newPos - 1;
                        }
                        return newPos;
                }
            }
        }

        // Parses named character entity reference (&amp; &apos; &lt; &gt; &quot;).
        // Returns -1 if the reference is not a character entity reference.
        // Returns -2 if more data is needed in the buffer
        // Otherwise
        //      - replaces the last character of the entity reference (';') with the referenced character (if expand == true)
        //      - returns position of the end of the character reference, that is of the character next to the original ';'
        private int ParseNamedCharRefInline(int startPos, bool expand, StringBuilder? internalSubsetBuilder)
        {
            Debug.Assert(startPos < _ps.charsUsed);
            Debug.Assert(_ps.chars != null);
            Debug.Assert(_ps.chars[startPos] == '&');
            Debug.Assert(_ps.chars[startPos + 1] != '#');

            int pos = startPos + 1;
            char[] chars = _ps.chars;
            char ch;

            switch (chars[pos])
            {
                // &apos; or &amp;
                case 'a':
                    pos++;
                    // &amp;
                    if (chars[pos] == 'm')
                    {
                        if (_ps.charsUsed - pos >= 3)
                        {
                            if (chars[pos + 1] == 'p' && chars[pos + 2] == ';')
                            {
                                pos += 3;
                                ch = '&';
                                goto FoundCharRef;
                            }
                            else
                            {
                                return -1;
                            }
                        }
                    }
                    // &apos;
                    else if (chars[pos] == 'p')
                    {
                        if (_ps.charsUsed - pos >= 4)
                        {
                            if (chars[pos + 1] == 'o' && chars[pos + 2] == 's' &&
                                    chars[pos + 3] == ';')
                            {
                                pos += 4;
                                ch = '\'';
                                goto FoundCharRef;
                            }
                            else
                            {
                                return -1;
                            }
                        }
                    }
                    else if (pos < _ps.charsUsed)
                    {
                        return -1;
                    }
                    break;
                // &guot;
                case 'q':
                    if (_ps.charsUsed - pos >= 5)
                    {
                        if (chars[pos + 1] == 'u' && chars[pos + 2] == 'o' &&
                                chars[pos + 3] == 't' && chars[pos + 4] == ';')
                        {
                            pos += 5;
                            ch = '"';
                            goto FoundCharRef;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    break;
                // &lt;
                case 'l':
                    if (_ps.charsUsed - pos >= 3)
                    {
                        if (chars[pos + 1] == 't' && chars[pos + 2] == ';')
                        {
                            pos += 3;
                            ch = '<';
                            goto FoundCharRef;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    break;
                // &gt;
                case 'g':
                    if (_ps.charsUsed - pos >= 3)
                    {
                        if (chars[pos + 1] == 't' && chars[pos + 2] == ';')
                        {
                            pos += 3;
                            ch = '>';
                            goto FoundCharRef;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    break;
                default:
                    return -1;
            }

            // need more data in the buffer
            return -2;

        FoundCharRef:
            Debug.Assert(pos > 0);
            if (expand)
            {
                if (internalSubsetBuilder != null)
                {
                    internalSubsetBuilder.Append(_ps.chars, _ps.charPos, pos - _ps.charPos);
                }
                _ps.chars[pos - 1] = ch;
            }
            return pos;
        }

        private int ParseName()
        {
            return ParseQName(false, 0, out _);
        }

        private int ParseQName(out int colonPos)
        {
            return ParseQName(true, 0, out colonPos);
        }

        private int ParseQName(bool isQName, int startOffset, out int colonPos)
        {
            int colonOffset = -1;
            int pos = _ps.charPos + startOffset;

        ContinueStartName:
            Debug.Assert(_ps.chars != null);
            char[] chars = _ps.chars;

            // start name char
            if (XmlCharType.IsStartNCNameSingleChar(chars[pos]))
            {
                pos++;
            }
            else
            {
                if (pos + 1 >= _ps.charsUsed)
                {
                    if (ReadDataInName(ref pos))
                    {
                        goto ContinueStartName;
                    }
                    Throw(pos, SR.Xml_UnexpectedEOF, "Name");
                }
                if (chars[pos] != ':' || _supportNamespaces)
                {
                    Throw(pos, SR.Xml_BadStartNameChar, XmlException.BuildCharExceptionArgs(chars, _ps.charsUsed, pos));
                }
            }

        ContinueName:
            // parse name
            while (true)
            {
                if (XmlCharType.IsNCNameSingleChar(chars[pos]))
                {
                    pos++;
                }
                else
                {
                    break;
                }
            }

            // colon
            if (chars[pos] == ':')
            {
                if (_supportNamespaces)
                {
                    if (colonOffset != -1 || !isQName)
                    {
                        Throw(pos, SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(':', '\0'));
                    }
                    colonOffset = pos - _ps.charPos;
                    pos++;
                    goto ContinueStartName;
                }
                else
                {
                    colonOffset = pos - _ps.charPos;
                    pos++;
                    goto ContinueName;
                }
            }
            // end of buffer
            else if (pos == _ps.charsUsed)
            {
                if (ReadDataInName(ref pos))
                {
                    Debug.Assert(_ps.chars != null);
                    chars = _ps.chars;
                    goto ContinueName;
                }
                Throw(pos, SR.Xml_UnexpectedEOF, "Name");
            }

            // end of name
            colonPos = (colonOffset == -1) ? -1 : _ps.charPos + colonOffset;
            return pos;
        }

        private bool ReadDataInName(ref int pos)
        {
            int offset = pos - _ps.charPos;
            bool newDataRead = (ReadData() != 0);
            pos = _ps.charPos + offset;
            return newDataRead;
        }

        private string ParseEntityName()
        {
            int endPos;
            try
            {
                endPos = ParseName();
            }
            catch (XmlException)
            {
                Throw(SR.Xml_ErrorParsingEntityName);
                return null!;
            }

            Debug.Assert(_ps.chars != null);

            // check ';'
            if (_ps.chars[endPos] != ';')
            {
                Throw(SR.Xml_ErrorParsingEntityName);
            }

            string entityName = _nameTable.Add(_ps.chars, _ps.charPos, endPos - _ps.charPos);
            _ps.charPos = endPos + 1;
            return entityName;
        }

        private NodeData AddNode(int nodeIndex, int nodeDepth)
        {
            Debug.Assert(nodeIndex < _nodes.Length);
            Debug.Assert(_nodes[_nodes.Length - 1] == null);

            NodeData n = _nodes[nodeIndex];
            if (n != null)
            {
                n.depth = nodeDepth;
                return n;
            }
            return AllocNode(nodeIndex, nodeDepth);
        }

        private NodeData AllocNode(int nodeIndex, int nodeDepth)
        {
            Debug.Assert(nodeIndex < _nodes.Length);
            if (nodeIndex >= _nodes.Length - 1)
            {
                NodeData[] newNodes = new NodeData[_nodes.Length * 2];
                Array.Copy(_nodes, newNodes, _nodes.Length);
                _nodes = newNodes;
            }
            Debug.Assert(nodeIndex < _nodes.Length);

            NodeData node = _nodes[nodeIndex];
            if (node == null)
            {
                node = new NodeData();
                _nodes[nodeIndex] = node;
            }
            node.depth = nodeDepth;
            return node;
        }

        private NodeData AddAttributeNoChecks(string name, int attrDepth)
        {
            NodeData newAttr = AddNode(_index + _attrCount + 1, attrDepth);
            newAttr.SetNamedNode(XmlNodeType.Attribute, _nameTable.Add(name));
            _attrCount++;
            return newAttr;
        }

        private NodeData AddAttribute(int endNamePos, int colonPos)
        {
            Debug.Assert(_ps.chars != null);

            // setup attribute name
            if (colonPos == -1 || !_supportNamespaces)
            {
                string localName = _nameTable.Add(_ps.chars, _ps.charPos, endNamePos - _ps.charPos);
                return AddAttribute(localName, string.Empty, localName);
            }
            else
            {
                _attrNeedNamespaceLookup = true;
                int startPos = _ps.charPos;
                int prefixLen = colonPos - startPos;
                if (prefixLen == _lastPrefix.Length && XmlConvert.StrEqual(_ps.chars, startPos, prefixLen, _lastPrefix))
                {
                    return AddAttribute(_nameTable.Add(_ps.chars, colonPos + 1, endNamePos - colonPos - 1),
                                         _lastPrefix,
                                         null);
                }
                else
                {
                    string prefix = _nameTable.Add(_ps.chars, startPos, prefixLen);
                    _lastPrefix = prefix;
                    return AddAttribute(_nameTable.Add(_ps.chars, colonPos + 1, endNamePos - colonPos - 1),
                                         prefix,
                                         null);
                }
            }
        }

        private NodeData AddAttribute(string localName, string prefix, string? nameWPrefix)
        {
            NodeData newAttr = AddNode(_index + _attrCount + 1, _index + 1);

            // set attribute name
            newAttr.SetNamedNode(XmlNodeType.Attribute, localName, prefix, nameWPrefix);

            // pre-check attribute for duplicate: hash by first local name char
            int attrHash = 1 << (localName[0] & 0x1F);
            if ((_attrHashtable & attrHash) == 0)
            {
                _attrHashtable |= attrHash;
            }
            else
            {
                // there are probably 2 attributes beginning with the same letter -> check all previous
                // attributes
                if (_attrDuplWalkCount < MaxAttrDuplWalkCount)
                {
                    _attrDuplWalkCount++;
                    for (int i = _index + 1; i < _index + _attrCount + 1; i++)
                    {
                        NodeData attr = _nodes[i];
                        Debug.Assert(attr.type == XmlNodeType.Attribute);
                        if (Ref.Equal(attr.localName, newAttr.localName))
                        {
                            _attrDuplWalkCount = MaxAttrDuplWalkCount;
                            break;
                        }
                    }
                }
            }

            _attrCount++;
            return newAttr;
        }

        private void PopElementContext()
        {
            Debug.Assert(_namespaceManager != null);
            // pop namespace context
            _namespaceManager.PopScope();

            // pop xml context
            if (_curNode.xmlContextPushed)
            {
                PopXmlContext();
            }
        }

        private void OnNewLine(int pos)
        {
            _ps.lineNo++;
            _ps.lineStartPos = pos - 1;
        }

        private void OnEof()
        {
            Debug.Assert(_ps.isEof);
            _curNode = _nodes[0];
            _curNode.Clear(XmlNodeType.None);
            _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);

            _parsingFunction = ParsingFunction.Eof;
            _readState = ReadState.EndOfFile;

            _reportedEncoding = null;
        }

        private string LookupNamespace(NodeData node)
        {
            Debug.Assert(_namespaceManager != null);
            string? ns = _namespaceManager.LookupNamespace(node.prefix);
            if (ns != null)
            {
                return ns;
            }
            else
            {
                Throw(SR.Xml_UnknownNs, node.prefix, node.LineNo, node.LinePos);
                return null;
            }
        }

        private void AddNamespace(string prefix, string uri, NodeData attr)
        {
            if (uri == XmlReservedNs.NsXmlNs)
            {
                if (Ref.Equal(prefix, _xmlNs))
                {
                    Throw(SR.Xml_XmlnsPrefix, (int)attr.lineInfo2.lineNo, (int)attr.lineInfo2.linePos);
                }
                else
                {
                    Throw(SR.Xml_NamespaceDeclXmlXmlns, prefix, (int)attr.lineInfo2.lineNo, (int)attr.lineInfo2.linePos);
                }
            }
            else if (uri == XmlReservedNs.NsXml)
            {
                if (!Ref.Equal(prefix, _xml) && !_v1Compat)
                {
                    Throw(SR.Xml_NamespaceDeclXmlXmlns, prefix, (int)attr.lineInfo2.lineNo, (int)attr.lineInfo2.linePos);
                }
            }
            if (uri.Length == 0 && prefix.Length > 0)
            {
                Throw(SR.Xml_BadNamespaceDecl, (int)attr.lineInfo.lineNo, (int)attr.lineInfo.linePos);
            }

            Debug.Assert(_namespaceManager != null);

            try
            {
                _namespaceManager.AddNamespace(prefix, uri);
            }
            catch (ArgumentException e)
            {
                ReThrow(e, (int)attr.lineInfo.lineNo, (int)attr.lineInfo.linePos);
            }
#if DEBUG
            if (prefix.Length == 0)
            {
                Debug.Assert(_xmlContext.defaultNamespace == uri);
            }
#endif
        }

        private void ResetAttributes()
        {
            if (_fullAttrCleanup)
            {
                FullAttributeCleanup();
            }

            _curAttrIndex = -1;
            _attrCount = 0;
            _attrHashtable = 0;
            _attrDuplWalkCount = 0;
        }

        private void FullAttributeCleanup()
        {
            for (int i = _index + 1; i < _index + _attrCount + 1; i++)
            {
                NodeData attr = _nodes[i];
                attr.nextAttrValueChunk = null;
                attr.IsDefaultAttribute = false;
            }
            _fullAttrCleanup = false;
        }

        private void PushXmlContext()
        {
            _xmlContext = new XmlContext(_xmlContext);
            _curNode.xmlContextPushed = true;
        }

        private void PopXmlContext()
        {
            Debug.Assert(_curNode.xmlContextPushed);
            _xmlContext = _xmlContext.previousContext!;
            _curNode.xmlContextPushed = false;
        }

        // Returns the whitespace node type according to the current whitespaceHandling setting and xml:space
        private XmlNodeType GetWhitespaceType()
        {
            if (_whitespaceHandling != WhitespaceHandling.None)
            {
                if (_xmlContext.xmlSpace == XmlSpace.Preserve)
                {
                    return XmlNodeType.SignificantWhitespace;
                }

                if (_whitespaceHandling == WhitespaceHandling.All)
                {
                    return XmlNodeType.Whitespace;
                }
            }

            return XmlNodeType.None;
        }

        private XmlNodeType GetTextNodeType(int orChars)
        {
            if (orChars > 0x20)
            {
                return XmlNodeType.Text;
            }
            else
            {
                return GetWhitespaceType();
            }
        }

        // This method resolves and opens an external DTD subset or an external entity based on its SYSTEM or PUBLIC ID.
        // SxS: This method may expose a name if a resource in baseUri (ref) parameter.
        private void PushExternalEntityOrSubset(string? publicId, string? systemId, Uri? baseUri, string? entityName)
        {
            Uri uri;

            Debug.Assert(_xmlResolver != null);

            // First try opening the external reference by PUBLIC Id
            if (!string.IsNullOrEmpty(publicId))
            {
                try
                {
                    uri = _xmlResolver.ResolveUri(baseUri, publicId);
                    if (OpenAndPush(uri))
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    // Intentionally empty - catch all exception related to PUBLIC ID and try opening the entity via the SYSTEM ID
                }
            }

            // Then try SYSTEM Id
            uri = _xmlResolver.ResolveUri(baseUri, systemId);
            try
            {
                if (OpenAndPush(uri))
                {
                    return;
                }

                // resolver returned null, throw exception outside this try-catch
            }
            catch (Exception e)
            {
                if (_v1Compat)
                {
                    throw;
                }

                string innerMessage = e.Message;
                Throw(new XmlException(entityName == null ? SR.Xml_ErrorOpeningExternalDtd : SR.Xml_ErrorOpeningExternalEntity, new string[] { uri.ToString(), innerMessage }, e, 0, 0));
            }

            if (entityName == null)
            {
                ThrowWithoutLineInfo(SR.Xml_CannotResolveExternalSubset, new string?[] { (publicId != null ? publicId : string.Empty), systemId }, null);
            }
            else
            {
                Throw(_dtdProcessing == DtdProcessing.Ignore ? SR.Xml_CannotResolveEntityDtdIgnored : SR.Xml_CannotResolveEntity, entityName);
            }
        }

        // This method opens the URI as a TextReader or Stream, pushes new ParsingStateState on the stack and calls InitStreamInput or InitTextReaderInput.
        // Returns:
        //    - true when everything went ok.
        //    - false when XmlResolver.GetEntity returned null
        // Propagates any exceptions from the XmlResolver indicating when the URI cannot be opened.
        private bool OpenAndPush(Uri uri)
        {
            Debug.Assert(_xmlResolver != null);

            // First try to get the data as a TextReader
            if (_xmlResolver.SupportsType(uri, typeof(TextReader)))
            {
                TextReader? textReader = (TextReader?)_xmlResolver.GetEntity(uri, null, typeof(TextReader));
                if (textReader == null)
                {
                    return false;
                }

                PushParsingState();
                InitTextReaderInput(uri.ToString(), uri, textReader);
            }
            else
            {
                // Then try get it as a Stream
                Debug.Assert(_xmlResolver.SupportsType(uri, typeof(Stream)), "Stream must always be a supported type in XmlResolver");

                Stream? stream = (Stream?)_xmlResolver.GetEntity(uri, null, typeof(Stream));
                if (stream == null)
                {
                    return false;
                }

                PushParsingState();
                InitStreamInput(uri, stream, null);
            }

            return true;
        }

        // returns true if real entity has been pushed, false if fake entity (=empty content entity)
        // SxS: The method neither takes any name of resource directly nor it exposes any resource to the caller.
        // Entity info was created based on source document. It's OK to suppress the SxS warning
        private bool PushExternalEntity(IDtdEntityInfo entity)
        {
            Debug.Assert(entity.IsExternal);

            if (!IsResolverNull)
            {
                Debug.Assert(_xmlResolver != null);
                Uri? entityBaseUri = null;
                if (!string.IsNullOrEmpty(entity.BaseUriString))
                {
                    entityBaseUri = _xmlResolver.ResolveUri(null, entity.BaseUriString);
                }

                PushExternalEntityOrSubset(entity.PublicId, entity.SystemId, entityBaseUri, entity.Name);

                RegisterEntity(entity);

                Debug.Assert(_ps.appendMode);
                int initialPos = _ps.charPos;
                if (_v1Compat)
                {
                    EatWhitespaces(null);
                }

                if (!ParseXmlDeclaration(true))
                {
                    _ps.charPos = initialPos;
                }

                return true;
            }
            else
            {
                Encoding? enc = _ps.encoding;

                PushParsingState();
                InitStringInput(entity.SystemId!, enc, string.Empty);

                RegisterEntity(entity);

                RegisterConsumedCharacters(0, true);

                return false;
            }
        }

        private void PushInternalEntity(IDtdEntityInfo entity)
        {
            Debug.Assert(!entity.IsExternal);

            Encoding? enc = _ps.encoding;

            PushParsingState();

            InitStringInput(entity.DeclaredUriString ?? string.Empty, enc, entity.Text!);

            RegisterEntity(entity);

            _ps.lineNo = entity.LineNumber;
            _ps.lineStartPos = -entity.LinePosition - 1;

            _ps.eolNormalized = true;

            RegisterConsumedCharacters(entity.Text!.Length, true);
        }

        private void PopEntity()
        {
            if (_ps.stream != null)
            {
                _ps.stream.Dispose();
            }

            UnregisterEntity();
            PopParsingState();
            _curNode.entityId = _ps.entityId;
        }

        private void RegisterEntity(IDtdEntityInfo entity)
        {
            // check entity recursion
            if (_currentEntities != null)
            {
                if (_currentEntities.ContainsKey(entity))
                {
                    Throw(entity.IsParameterEntity ? SR.Xml_RecursiveParEntity : SR.Xml_RecursiveGenEntity, entity.Name,
                        _parsingStatesStack![_parsingStatesStackTop].LineNo, _parsingStatesStack[_parsingStatesStackTop].LinePos);
                }
            }

            // save the entity to parsing state & assign it an ID
            _ps.entity = entity;
            _ps.entityId = _nextEntityId++;

            // register entity for recursion checkes
            if (entity != null)
            {
                if (_currentEntities == null)
                {
                    _currentEntities = new Dictionary<IDtdEntityInfo, IDtdEntityInfo>();
                }

                _currentEntities.Add(entity, entity);
            }
        }

        private void UnregisterEntity()
        {
            // remove from recursion check registry
            if (_ps.entity != null)
            {
                _currentEntities!.Remove(_ps.entity);
            }
        }

        private void PushParsingState()
        {
            if (_parsingStatesStack == null)
            {
                _parsingStatesStack = new ParsingState[InitialParsingStatesDepth];
                Debug.Assert(_parsingStatesStackTop == -1);
            }
            else if (_parsingStatesStackTop + 1 == _parsingStatesStack.Length)
            {
                ParsingState[] newParsingStateStack = new ParsingState[_parsingStatesStack.Length * 2];
                Array.Copy(_parsingStatesStack, newParsingStateStack, _parsingStatesStack.Length);
                _parsingStatesStack = newParsingStateStack;
            }
            _parsingStatesStackTop++;
            _parsingStatesStack[_parsingStatesStackTop] = _ps;

            _ps.Clear();
        }

        private void PopParsingState()
        {
            Debug.Assert(_parsingStatesStackTop >= 0);
            Debug.Assert(_parsingStatesStack != null);
            _ps.Close(true);
            _ps = _parsingStatesStack[_parsingStatesStackTop--];
        }

        private void InitIncrementalRead(IncrementalReadDecoder decoder)
        {
            ResetAttributes();

            decoder.Reset();
            _incReadDecoder = decoder;
            _incReadState = IncrementalReadState.Text;
            _incReadDepth = 1;
            _incReadLeftStartPos = _ps.charPos;
            _incReadLeftEndPos = _ps.charPos;
            _incReadLineInfo.Set(_ps.LineNo, _ps.LinePos);

            _parsingFunction = ParsingFunction.InIncrementalRead;
        }

        private int IncrementalRead(Array array, int index, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException((_incReadDecoder is IncrementalReadCharsDecoder) ? "buffer" : nameof(array));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException((_incReadDecoder is IncrementalReadCharsDecoder) ? nameof(count) : "len");
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException((_incReadDecoder is IncrementalReadCharsDecoder) ? nameof(index) : "offset");
            }

            if (array.Length - index < count)
            {
                throw new ArgumentException((_incReadDecoder is IncrementalReadCharsDecoder) ? nameof(count) : "len");
            }

            if (count == 0)
            {
                return 0;
            }

            Debug.Assert(_incReadDecoder != null);
            _curNode.lineInfo = _incReadLineInfo;

            _incReadDecoder.SetNextOutputBuffer(array, index, count);
            IncrementalRead();
            return _incReadDecoder.DecodedCount;
        }

        private int IncrementalRead()
        {
            Debug.Assert(_incReadDecoder != null);
            Debug.Assert(_ps.chars != null);
            int charsDecoded = 0;

        OuterContinue:
            int charsLeft = _incReadLeftEndPos - _incReadLeftStartPos;
            if (charsLeft > 0)
            {
                int count;
                try
                {
                    count = _incReadDecoder.Decode(_ps.chars, _incReadLeftStartPos, charsLeft);
                }
                catch (XmlException e)
                {
                    ReThrow(e, (int)_incReadLineInfo.lineNo, (int)_incReadLineInfo.linePos);
                    return 0;
                }
                if (count < charsLeft)
                {
                    _incReadLeftStartPos += count;
                    _incReadLineInfo.linePos += count; // we have never more then 1 line cached
                    return count;
                }
                else
                {
                    _incReadLeftStartPos = 0;
                    _incReadLeftEndPos = 0;
                    _incReadLineInfo.linePos += count;
                    if (_incReadDecoder.IsFull)
                    {
                        return count;
                    }
                }
            }

            int startPos;
            int pos;

            while (true)
            {
                switch (_incReadState)
                {
                    case IncrementalReadState.Text:
                    case IncrementalReadState.Attributes:
                    case IncrementalReadState.AttributeValue:
                        break;
                    case IncrementalReadState.PI:
                        if (ParsePIValue(out startPos, out pos))
                        {
                            Debug.Assert(XmlConvert.StrEqual(_ps.chars, _ps.charPos - 2, 2, "?>"));
                            _ps.charPos -= 2;
                            _incReadState = IncrementalReadState.Text;
                        }
                        goto Append;
                    case IncrementalReadState.Comment:
                        if (ParseCDataOrComment(XmlNodeType.Comment, out startPos, out pos))
                        {
                            Debug.Assert(XmlConvert.StrEqual(_ps.chars, _ps.charPos - 3, 3, "-->"));
                            _ps.charPos -= 3;
                            _incReadState = IncrementalReadState.Text;
                        }
                        goto Append;
                    case IncrementalReadState.CDATA:
                        if (ParseCDataOrComment(XmlNodeType.CDATA, out startPos, out pos))
                        {
                            Debug.Assert(XmlConvert.StrEqual(_ps.chars, _ps.charPos - 3, 3, "]]>"));
                            _ps.charPos -= 3;
                            _incReadState = IncrementalReadState.Text;
                        }
                        goto Append;
                    case IncrementalReadState.EndElement:
                        _parsingFunction = ParsingFunction.PopElementContext;
                        _nextParsingFunction = (_index > 0 || _fragmentType != XmlNodeType.Document) ? ParsingFunction.ElementContent
                                                                                                    : ParsingFunction.DocumentContent;
                        _outerReader.Read();
                        _incReadState = IncrementalReadState.End;
                        goto case IncrementalReadState.End;
                    case IncrementalReadState.End:
                        return charsDecoded;
                    case IncrementalReadState.ReadData:
                        if (ReadData() == 0)
                        {
                            ThrowUnclosedElements();
                        }
                        _incReadState = IncrementalReadState.Text;
                        break;
                    default:
                        Debug.Fail($"Unexpected read state {_incReadState}");
                        break;
                }
                Debug.Assert(_incReadState == IncrementalReadState.Text ||
                              _incReadState == IncrementalReadState.Attributes ||
                              _incReadState == IncrementalReadState.AttributeValue);

                char[] chars = _ps.chars;
                startPos = _ps.charPos;
                pos = startPos;

                while (true)
                {
                    _incReadLineInfo.Set(_ps.LineNo, _ps.LinePos);

                    char c;
                    if (_incReadState == IncrementalReadState.Attributes)
                    {
                        while (XmlCharType.IsAttributeValueChar(c = chars[pos]) && c != '/')
                        {
                            pos++;
                        }
                    }
                    else
                    {
                        while (XmlCharType.IsAttributeValueChar(chars[pos]))
                        {
                            pos++;
                        }
                    }

                    if (chars[pos] == '&' || chars[pos] == (char)0x9)
                    {
                        pos++;
                        continue;
                    }

                    if (pos - startPos > 0)
                    {
                        goto AppendAndUpdateCharPos;
                    }

                    switch (chars[pos])
                    {
                        // eol
                        case (char)0xA:
                            pos++;
                            OnNewLine(pos);
                            continue;
                        case (char)0xD:
                            if (chars[pos + 1] == (char)0xA)
                            {
                                pos += 2;
                            }
                            else if (pos + 1 < _ps.charsUsed)
                            {
                                pos++;
                            }
                            else
                            {
                                goto ReadData;
                            }
                            OnNewLine(pos);
                            continue;
                        // some tag
                        case '<':
                            if (_incReadState != IncrementalReadState.Text)
                            {
                                pos++;
                                continue;
                            }
                            if (_ps.charsUsed - pos < 2)
                            {
                                goto ReadData;
                            }
                            switch (chars[pos + 1])
                            {
                                // pi
                                case '?':
                                    pos += 2;
                                    _incReadState = IncrementalReadState.PI;
                                    goto AppendAndUpdateCharPos;
                                // comment
                                case '!':
                                    if (_ps.charsUsed - pos < 4)
                                    {
                                        goto ReadData;
                                    }
                                    if (chars[pos + 2] == '-' && chars[pos + 3] == '-')
                                    {
                                        pos += 4;
                                        _incReadState = IncrementalReadState.Comment;
                                        goto AppendAndUpdateCharPos;
                                    }
                                    if (_ps.charsUsed - pos < 9)
                                    {
                                        goto ReadData;
                                    }
                                    if (XmlConvert.StrEqual(chars, pos + 2, 7, "[CDATA["))
                                    {
                                        pos += 9;
                                        _incReadState = IncrementalReadState.CDATA;
                                        goto AppendAndUpdateCharPos;
                                    }
                                    else
                                    {
                                        ; //Throw( );
                                    }
                                    break;
                                // end tag
                                case '/':
                                    {
                                        Debug.Assert(_ps.charPos - pos == 0);
                                        Debug.Assert(_ps.charPos - startPos == 0);

                                        // ParseQName can flush the buffer, so we need to update the startPos, pos and chars after calling it
                                        int endPos = ParseQName(true, 2, out _);
                                        if (XmlConvert.StrEqual(chars, _ps.charPos + 2, endPos - _ps.charPos - 2, _curNode.GetNameWPrefix(_nameTable)) &&
                                            (_ps.chars[endPos] == '>' || XmlCharType.IsWhiteSpace(_ps.chars[endPos])))
                                        {
                                            if (--_incReadDepth > 0)
                                            {
                                                pos = endPos + 1;
                                                continue;
                                            }

                                            _ps.charPos = endPos;
                                            if (XmlCharType.IsWhiteSpace(_ps.chars[endPos]))
                                            {
                                                EatWhitespaces(null);
                                            }
                                            if (_ps.chars[_ps.charPos] != '>')
                                            {
                                                ThrowUnexpectedToken(">");
                                            }
                                            _ps.charPos++;

                                            _incReadState = IncrementalReadState.EndElement;
                                            goto OuterContinue;
                                        }
                                        else
                                        {
                                            pos = endPos;
                                            startPos = _ps.charPos;
                                            chars = _ps.chars;
                                            continue;
                                        }
                                    }
                                // start tag
                                default:
                                    {
                                        Debug.Assert(_ps.charPos - pos == 0);
                                        Debug.Assert(_ps.charPos - startPos == 0);

                                        // ParseQName can flush the buffer, so we need to update the startPos, pos and chars after calling it
                                        int endPos = ParseQName(true, 1, out _);
                                        if (XmlConvert.StrEqual(_ps.chars, _ps.charPos + 1, endPos - _ps.charPos - 1, _curNode.localName) &&
                                            (_ps.chars[endPos] == '>' || _ps.chars[endPos] == '/' || XmlCharType.IsWhiteSpace(_ps.chars[endPos])))
                                        {
                                            _incReadDepth++;
                                            _incReadState = IncrementalReadState.Attributes;
                                            pos = endPos;
                                            goto AppendAndUpdateCharPos;
                                        }
                                        pos = endPos;
                                        startPos = _ps.charPos;
                                        chars = _ps.chars;
                                        continue;
                                    }
                            }
                            break;
                        // end of start tag
                        case '/':
                            if (_incReadState == IncrementalReadState.Attributes)
                            {
                                if (_ps.charsUsed - pos < 2)
                                {
                                    goto ReadData;
                                }
                                if (chars[pos + 1] == '>')
                                {
                                    _incReadState = IncrementalReadState.Text;
                                    _incReadDepth--;
                                }
                            }
                            pos++;
                            continue;
                        // end of start tag
                        case '>':
                            if (_incReadState == IncrementalReadState.Attributes)
                            {
                                _incReadState = IncrementalReadState.Text;
                            }
                            pos++;
                            continue;
                        case '"':
                        case '\'':
                            switch (_incReadState)
                            {
                                case IncrementalReadState.AttributeValue:
                                    if (chars[pos] == _curNode.quoteChar)
                                    {
                                        _incReadState = IncrementalReadState.Attributes;
                                    }
                                    break;
                                case IncrementalReadState.Attributes:
                                    _curNode.quoteChar = chars[pos];
                                    _incReadState = IncrementalReadState.AttributeValue;
                                    break;
                            }
                            pos++;
                            continue;
                        default:
                            // end of buffer
                            if (pos == _ps.charsUsed)
                            {
                                goto ReadData;
                            }
                            // surrogate chars or invalid chars are ignored
                            else
                            {
                                pos++;
                                continue;
                            }
                    }
                }

            ReadData:
                _incReadState = IncrementalReadState.ReadData;

            AppendAndUpdateCharPos:
                _ps.charPos = pos;

            Append:
                // decode characters
                int charsParsed = pos - startPos;
                if (charsParsed > 0)
                {
                    int count;
                    try
                    {
                        count = _incReadDecoder.Decode(_ps.chars, startPos, charsParsed);
                    }
                    catch (XmlException e)
                    {
                        ReThrow(e, (int)_incReadLineInfo.lineNo, (int)_incReadLineInfo.linePos);
                        return 0;
                    }

                    Debug.Assert(count == charsParsed || _incReadDecoder.IsFull, "Check if decoded consumed all characters unless it's full.");
                    charsDecoded += count;
                    if (_incReadDecoder.IsFull)
                    {
                        _incReadLeftStartPos = startPos + count;
                        _incReadLeftEndPos = pos;
                        _incReadLineInfo.linePos += count; // we have never more than 1 line cached
                        return charsDecoded;
                    }
                }
            }
        }

        private void FinishIncrementalRead()
        {
            _incReadDecoder = new IncrementalReadDummyDecoder();
            IncrementalRead();
            Debug.Assert(IncrementalRead() == 0, "Previous call of IncrementalRead should eat up all characters!");
            _incReadDecoder = null;
        }

        private bool ParseFragmentAttribute()
        {
            Debug.Assert(_fragmentType == XmlNodeType.Attribute);

            // if first call then parse the whole attribute value
            if (_curNode.type == XmlNodeType.None)
            {
                _curNode.type = XmlNodeType.Attribute;
                _curAttrIndex = 0;
                ParseAttributeValueSlow(_ps.charPos, ' ', _curNode); // The quote char is intentionally empty (space) because we need to parse ' and " into the attribute value
            }
            else
            {
                _parsingFunction = ParsingFunction.InReadAttributeValue;
            }

            // return attribute value chunk
            if (ReadAttributeValue())
            {
                Debug.Assert(_parsingFunction == ParsingFunction.InReadAttributeValue);
                _parsingFunction = ParsingFunction.FragmentAttribute;
                return true;
            }
            else
            {
                OnEof();
                return false;
            }
        }

        private bool ParseAttributeValueChunk()
        {
            Debug.Assert(_ps.chars != null);

            char[] chars = _ps.chars;
            int pos = _ps.charPos;

            _curNode = AddNode(_index + _attrCount + 1, _index + 2);
            _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);

            if (_emptyEntityInAttributeResolved)
            {
                _curNode.SetValueNode(XmlNodeType.Text, string.Empty);
                _emptyEntityInAttributeResolved = false;
                return true;
            }

            Debug.Assert(_stringBuilder.Length == 0);

            while (true)
            {
                while (XmlCharType.IsAttributeValueChar(chars[pos]))
                    pos++;

                switch (chars[pos])
                {
                    // eol D
                    case (char)0xD:
                        Debug.Assert(_ps.eolNormalized, "Entity replacement text for attribute values should be EOL-normalized!");
                        pos++;
                        continue;
                    // eol A, tab
                    case (char)0xA:
                    case (char)0x9:
                        if (_normalize)
                        {
                            chars[pos] = (char)0x20;  // CDATA normalization of 0xA and 0x9
                        }
                        pos++;
                        continue;
                    case '"':
                    case '\'':
                    case '>':
                        pos++;
                        continue;
                    // attribute values cannot contain '<'
                    case '<':
                        Throw(pos, SR.Xml_BadAttributeChar, XmlException.BuildCharExceptionArgs('<', '\0'));
                        break;
                    // entity reference
                    case '&':
                        if (pos - _ps.charPos > 0)
                        {
                            _stringBuilder.Append(chars, _ps.charPos, pos - _ps.charPos);
                        }

                        _ps.charPos = pos;

                        // expand char entities but not general entities
                        switch (HandleEntityReference(true, EntityExpandType.OnlyCharacter, out pos))
                        {
                            case EntityType.CharacterDec:
                            case EntityType.CharacterHex:
                            case EntityType.CharacterNamed:
                                chars = _ps.chars;
                                if (_normalize && XmlCharType.IsWhiteSpace(chars[_ps.charPos]) && pos - _ps.charPos == 1)
                                {
                                    chars[_ps.charPos] = (char)0x20;  // CDATA normalization of character references in entities
                                }
                                break;
                            case EntityType.Unexpanded:
                                if (_stringBuilder.Length == 0)
                                {
                                    _curNode.lineInfo.linePos++;
                                    _ps.charPos++;
                                    _curNode.SetNamedNode(XmlNodeType.EntityReference, ParseEntityName());
                                    return true;
                                }
                                else
                                {
                                    goto ReturnText;
                                }
                            default:
                                Debug.Fail("We should never get to this point.");
                                break;
                        }
                        chars = _ps.chars;
                        continue;
                    default:
                        // end of buffer
                        if (pos == _ps.charsUsed)
                        {
                            goto ReadData;
                        }
                        // surrogate chars
                        else
                        {
                            char ch = chars[pos];
                            if (XmlCharType.IsHighSurrogate(ch))
                            {
                                if (pos + 1 == _ps.charsUsed)
                                {
                                    goto ReadData;
                                }

                                pos++;
                                if (XmlCharType.IsLowSurrogate(chars[pos]))
                                {
                                    pos++;
                                    continue;
                                }
                            }

                            ThrowInvalidChar(chars, _ps.charsUsed, pos);
                            break;
                        }
                }

            ReadData:
                if (pos - _ps.charPos > 0)
                {
                    _stringBuilder.Append(chars, _ps.charPos, pos - _ps.charPos);
                    _ps.charPos = pos;
                }

                // read new characters into the buffer
                if (ReadData() == 0)
                {
                    if (_stringBuilder.Length > 0)
                    {
                        goto ReturnText;
                    }
                    else
                    {
                        if (HandleEntityEnd(false))
                        {
                            SetupEndEntityNodeInAttribute();
                            return true;
                        }
                        else
                        {
                            Debug.Fail("We should never get to this point.");
                        }
                    }
                }

                pos = _ps.charPos;
                chars = _ps.chars;
            }

        ReturnText:
            if (pos - _ps.charPos > 0)
            {
                _stringBuilder.Append(chars, _ps.charPos, pos - _ps.charPos);
                _ps.charPos = pos;
            }

            _curNode.SetValueNode(XmlNodeType.Text, _stringBuilder.ToString());
            _stringBuilder.Length = 0;
            return true;
        }

        private void ParseXmlDeclarationFragment()
        {
            try
            {
                ParseXmlDeclaration(false);
            }
            catch (XmlException e)
            {
                ReThrow(e, e.LineNumber, e.LinePosition - 6); // 6 == strlen( "<?xml " );
            }
        }

        private void ThrowUnexpectedToken(int pos, string expectedToken)
        {
            ThrowUnexpectedToken(pos, expectedToken, null);
        }

        private void ThrowUnexpectedToken(string expectedToken1)
        {
            ThrowUnexpectedToken(expectedToken1, null);
        }

        private void ThrowUnexpectedToken(int pos, string expectedToken1, string? expectedToken2)
        {
            _ps.charPos = pos;
            ThrowUnexpectedToken(expectedToken1, expectedToken2);
        }

        private void ThrowUnexpectedToken(string expectedToken1, string? expectedToken2)
        {
            string? unexpectedToken = ParseUnexpectedToken();
            if (unexpectedToken == null)
            {
                Throw(SR.Xml_UnexpectedEOF1);
            }

            if (expectedToken2 != null)
            {
                Throw(SR.Xml_UnexpectedTokens2, new string[3] { unexpectedToken, expectedToken1, expectedToken2 });
            }
            else
            {
                Throw(SR.Xml_UnexpectedTokenEx, new string[2] { unexpectedToken, expectedToken1 });
            }
        }

        private string? ParseUnexpectedToken(int pos)
        {
            _ps.charPos = pos;
            return ParseUnexpectedToken();
        }

        private string? ParseUnexpectedToken()
        {
            if (_ps.charPos == _ps.charsUsed)
            {
                return null;
            }

            Debug.Assert(_ps.chars != null);

            if (XmlCharType.IsNCNameSingleChar(_ps.chars[_ps.charPos]))
            {
                int pos = _ps.charPos + 1;
                while (XmlCharType.IsNCNameSingleChar(_ps.chars[pos]))
                {
                    pos++;
                }
                return new string(_ps.chars, _ps.charPos, pos - _ps.charPos);
            }
            else
            {
                Debug.Assert(_ps.charPos < _ps.charsUsed);
                return new string(_ps.chars, _ps.charPos, 1);
            }
        }

        private void ThrowExpectingWhitespace(int pos)
        {
            string? unexpectedToken = ParseUnexpectedToken(pos);

            if (unexpectedToken == null)
            {
                Throw(pos, SR.Xml_UnexpectedEOF1);
            }
            else
            {
                Throw(pos, SR.Xml_ExpectingWhiteSpace, unexpectedToken);
            }
        }

        private int GetIndexOfAttributeWithoutPrefix(string name)
        {
            string? atomizedName = _nameTable.Get(name);
            if (atomizedName == null)
            {
                return -1;
            }

            for (int i = _index + 1; i < _index + _attrCount + 1; i++)
            {
                if (Ref.Equal(_nodes[i].localName, atomizedName) && _nodes[i].prefix.Length == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private int GetIndexOfAttributeWithPrefix(string name)
        {
            name = _nameTable.Add(name);

            if (name == null)
            {
                return -1;
            }

            for (int i = _index + 1; i < _index + _attrCount + 1; i++)
            {
                if (Ref.Equal(_nodes[i].GetNameWPrefix(_nameTable), name))
                {
                    return i;
                }
            }

            return -1;
        }

        // This method is used to enable parsing of zero-terminated streams. The old XmlTextReader implementation used
        // to parse such streams, we this one needs to do that as well.
        // If the last characters decoded from the stream is 0 and the stream is in EOF state, this method will remove
        // the character from the parsing buffer (decrements ps.charsUsed).
        // Note that this method calls ReadData() which may change the value of ps.chars and ps.charPos.
        private bool ZeroEndingStream(int pos)
        {
            Debug.Assert(_ps.chars != null);

            if (_v1Compat && pos == _ps.charsUsed - 1 && _ps.chars[pos] == (char)0 && ReadData() == 0 && _ps.isStreamEof)
            {
                _ps.charsUsed--;
                return true;
            }

            return false;
        }

        private void ParseDtdFromParserContext()
        {
            Debug.Assert(_dtdInfo == null && _fragmentParserContext != null && _fragmentParserContext.HasDtdInfo);

            IDtdParser dtdParser = DtdParser.Create();

            // Parse DTD
            _dtdInfo = dtdParser.ParseFreeFloatingDtd(_fragmentParserContext.BaseURI, _fragmentParserContext.DocTypeName, _fragmentParserContext.PublicId,
                                                     _fragmentParserContext.SystemId, _fragmentParserContext.InternalSubset, new DtdParserProxy(this));

            if ((_validatingReaderCompatFlag || !_v1Compat) && (_dtdInfo.HasDefaultAttributes || _dtdInfo.HasNonCDataAttributes))
            {
                _addDefaultAttributesAndNormalize = true;
            }
        }

        private bool InitReadContentAsBinary()
        {
            Debug.Assert(_parsingFunction != ParsingFunction.InReadContentAsBinary);

            if (_parsingFunction == ParsingFunction.InReadValueChunk)
            {
                throw new InvalidOperationException(SR.Xml_MixingReadValueChunkWithBinary);
            }

            if (_parsingFunction == ParsingFunction.InIncrementalRead)
            {
                throw new InvalidOperationException(SR.Xml_MixingV1StreamingWithV2Binary);
            }

            if (!XmlReader.IsTextualNode(_curNode.type))
            {
                if (!MoveToNextContentNode(false))
                {
                    return false;
                }
            }

            SetupReadContentAsBinaryState(ParsingFunction.InReadContentAsBinary);
            _incReadLineInfo.Set(_curNode.LineNo, _curNode.LinePos);
            return true;
        }

        private bool InitReadElementContentAsBinary()
        {
            Debug.Assert(_parsingFunction != ParsingFunction.InReadElementContentAsBinary);
            Debug.Assert(_curNode.type == XmlNodeType.Element);

            bool isEmpty = _curNode.IsEmptyElement;

            // move to content or off the empty element
            _outerReader.Read();

            if (isEmpty)
            {
                return false;
            }

            // make sure we are on a content node
            if (!MoveToNextContentNode(false))
            {
                if (_curNode.type != XmlNodeType.EndElement)
                {
                    Throw(SR.Xml_InvalidNodeType, _curNode.type.ToString());
                }

                // move off end element
                _outerReader.Read();
                return false;
            }

            SetupReadContentAsBinaryState(ParsingFunction.InReadElementContentAsBinary);
            _incReadLineInfo.Set(_curNode.LineNo, _curNode.LinePos);
            return true;
        }

        private bool MoveToNextContentNode(bool moveIfOnContentNode)
        {
            do
            {
                switch (_curNode.type)
                {
                    case XmlNodeType.Attribute:
                        return !moveIfOnContentNode;
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.CDATA:
                        if (!moveIfOnContentNode)
                        {
                            return true;
                        }
                        break;
                    case XmlNodeType.ProcessingInstruction:
                    case XmlNodeType.Comment:
                    case XmlNodeType.EndEntity:
                        // skip comments, pis and end entity nodes
                        break;
                    case XmlNodeType.EntityReference:
                        _outerReader.ResolveEntity();
                        break;
                    default:
                        return false;
                }
                moveIfOnContentNode = false;
            } while (_outerReader.Read());

            return false;
        }

        private void SetupReadContentAsBinaryState(ParsingFunction inReadBinaryFunction)
        {
            if (_parsingFunction == ParsingFunction.PartialTextValue)
            {
                _incReadState = IncrementalReadState.ReadContentAsBinary_OnPartialValue;
            }
            else
            {
                _incReadState = IncrementalReadState.ReadContentAsBinary_OnCachedValue;
                _nextNextParsingFunction = _nextParsingFunction;
                _nextParsingFunction = _parsingFunction;
            }
            _readValueOffset = 0;
            _parsingFunction = inReadBinaryFunction;
        }

        [MemberNotNull(nameof(_nameTable))]
        private void SetupFromParserContext(XmlParserContext context, XmlReaderSettings settings)
        {
            Debug.Assert(context != null);

            // setup nameTable
            XmlNameTable? nt = settings.NameTable;
            _nameTableFromSettings = (nt != null);

            // get name table from namespace manager in XmlParserContext, if available;
            if (context.NamespaceManager != null)
            {
                // must be the same as XmlReaderSettings.NameTable, or null
                if (nt != null && nt != context.NamespaceManager.NameTable)
                {
                    throw new XmlException(SR.Xml_NametableMismatch);
                }
                // get the namespace manager from context
                _namespaceManager = context.NamespaceManager;
                _xmlContext.defaultNamespace = _namespaceManager.LookupNamespace(string.Empty)!;

                // get the nametable from ns manager
                nt = _namespaceManager.NameTable;

                Debug.Assert(nt != null);
                Debug.Assert(context.NameTable == null || context.NameTable == nt, "This check should have been done in XmlParserContext constructor.");
            }
            // get name table directly from XmlParserContext
            else if (context.NameTable != null)
            {
                // must be the same as XmlReaderSettings.NameTable, or null
                if (nt != null && nt != context.NameTable)
                {
                    throw new XmlException(SR.Xml_NametableMismatch, string.Empty);
                }
                nt = context.NameTable;
            }
            // no nametable provided -> create a new one
            else if (nt == null)
            {
                nt = new NameTable();
                Debug.Assert(_nameTableFromSettings == false);
            }
            _nameTable = nt;

            // make sure we have namespace manager
            if (_namespaceManager == null)
            {
                _namespaceManager = new XmlNamespaceManager(nt);
            }

            // copy xml:space and xml:lang
            _xmlContext.xmlSpace = context.XmlSpace;
            _xmlContext.xmlLang = context.XmlLang;
        }

        //
        // DtdInfo
        //
        internal override IDtdInfo? DtdInfo
        {
            get
            {
                return _dtdInfo;
            }
        }

        internal void SetDtdInfo(IDtdInfo newDtdInfo)
        {
            Debug.Assert(_dtdInfo == null);

            _dtdInfo = newDtdInfo;
            if (_dtdInfo != null)
            {
                if ((_validatingReaderCompatFlag || !_v1Compat) && (_dtdInfo.HasDefaultAttributes || _dtdInfo.HasNonCDataAttributes))
                {
                    _addDefaultAttributesAndNormalize = true;
                }
            }
        }

        //
        // Validation support
        //

        internal IValidationEventHandling? ValidationEventHandling
        {
            set
            {
                _validationEventHandling = value;
            }
        }

        internal OnDefaultAttributeUseDelegate OnDefaultAttributeUse
        {
            set { _onDefaultAttributeUse = value; }
        }

        //
        // Internal properties for XmlValidatingReader
        //
        internal bool XmlValidatingReaderCompatibilityMode
        {
            set
            {
                _validatingReaderCompatFlag = value;

                // Fix for VSWhidbey 516556; These namespaces must be added to the nametable for back compat reasons.
                if (value)
                {
                    _nameTable.Add(XmlReservedNs.NsXs); // Note: this is equal to XmlReservedNs.NsXsd in Everett
                    _nameTable.Add(XmlReservedNs.NsXsi);
                    _nameTable.Add(XmlReservedNs.NsDataType);
                }
            }
        }

        internal XmlNodeType FragmentType
        {
            get
            {
                return _fragmentType;
            }
        }

        internal void ChangeCurrentNodeType(XmlNodeType newNodeType)
        {
            Debug.Assert(_curNode.type == XmlNodeType.Whitespace && newNodeType == XmlNodeType.SignificantWhitespace, "Incorrect node type change!");
            _curNode.type = newNodeType;
        }

        internal XmlResolver? GetResolver()
        {
            if (IsResolverNull)
                return null;
            else
                return _xmlResolver;
        }

        internal object? InternalSchemaType
        {
            get
            {
                return _curNode.schemaType;
            }
            set
            {
                _curNode.schemaType = value;
            }
        }

        internal object? InternalTypedValue
        {
            get
            {
                return _curNode.typedValue;
            }
            set
            {
                _curNode.typedValue = value;
            }
        }

        internal bool StandAlone
        {
            get
            {
                return _standalone;
            }
        }

        internal override XmlNamespaceManager? NamespaceManager
        {
            get
            {
                return _namespaceManager;
            }
        }

        internal bool V1Compat
        {
            get
            {
                return _v1Compat;
            }
        }

        internal ConformanceLevel V1ComformanceLevel
        {
            get
            {
                return _fragmentType == XmlNodeType.Element ? ConformanceLevel.Fragment : ConformanceLevel.Document;
            }
        }

        private bool AddDefaultAttributeDtd(IDtdDefaultAttributeInfo defAttrInfo, bool definedInDtd, NodeData[]? nameSortedNodeData)
        {
            if (defAttrInfo.Prefix.Length > 0)
            {
                _attrNeedNamespaceLookup = true;
            }

            string localName = defAttrInfo.LocalName;
            string prefix = defAttrInfo.Prefix;

            // check for duplicates
            if (nameSortedNodeData != null)
            {
                if (Array.BinarySearch<object>(nameSortedNodeData, defAttrInfo, DtdDefaultAttributeInfoToNodeDataComparer.Instance) >= 0)
                {
                    return false;
                }
            }
            else
            {
                for (int i = _index + 1; i < _index + 1 + _attrCount; i++)
                {
                    if ((object)_nodes[i].localName == (object)localName &&
                        (object)_nodes[i].prefix == (object)prefix)
                    {
                        return false;
                    }
                }
            }

            NodeData attr = AddDefaultAttributeInternal(defAttrInfo.LocalName, null, defAttrInfo.Prefix, defAttrInfo.DefaultValueExpanded,
                                                         defAttrInfo.LineNumber, defAttrInfo.LinePosition,
                                                         defAttrInfo.ValueLineNumber, defAttrInfo.ValueLinePosition, defAttrInfo.IsXmlAttribute);

            Debug.Assert(attr != null);

            if (DtdValidation)
            {
                if (_onDefaultAttributeUse != null)
                {
                    _onDefaultAttributeUse(defAttrInfo, this);
                }

                attr.typedValue = defAttrInfo.DefaultValueTyped;
            }
            return attr != null;
        }

        internal bool AddDefaultAttributeNonDtd(SchemaAttDef attrDef)
        {
            // atomize names - Xsd Validator does not need to have the same nametable
            string localName = _nameTable.Add(attrDef.Name.Name);
            string? prefix = _nameTable.Add(attrDef.Prefix);
            string ns = _nameTable.Add(attrDef.Name.Namespace);

            // atomize namespace - Xsd Validator does not need to have the same nametable
            if (prefix.Length == 0 && ns.Length > 0)
            {
                Debug.Assert(_namespaceManager != null);
                prefix = _namespaceManager.LookupPrefix(ns);

                Debug.Assert(prefix != null);
                if (prefix == null)
                {
                    prefix = string.Empty;
                }
            }

            // find out if the attribute is already there
            for (int i = _index + 1; i < _index + 1 + _attrCount; i++)
            {
                if ((object)_nodes[i].localName == (object)localName &&
                    (((object?)_nodes[i].prefix == (object?)prefix) || ((object?)_nodes[i].ns == (object?)ns && ns != null)))
                {
                    return false;
                }
            }

            // attribute does not exist -> we need to add it
            NodeData attr = AddDefaultAttributeInternal(localName, ns, prefix, attrDef.DefaultValueExpanded,
                                                         attrDef.LineNumber, attrDef.LinePosition,
                                                         attrDef.ValueLineNumber, attrDef.ValueLinePosition, attrDef.Reserved != SchemaAttDef.Reserve.None);
            Debug.Assert(attr != null);

            attr.schemaType = (attrDef.SchemaType == null) ? (object)attrDef.Datatype : (object)attrDef.SchemaType;
            attr.typedValue = attrDef.DefaultValueTyped;
            return true;
        }

        private NodeData AddDefaultAttributeInternal(string localName, string? ns, string prefix, string value,
                                                     int lineNo, int linePos, int valueLineNo, int valueLinePos, bool isXmlAttribute)
        {
            // setup the attribute
            NodeData attr = AddAttribute(localName, prefix, prefix.Length > 0 ? null : localName);
            if (ns != null)
            {
                attr.ns = ns;
            }

            attr.SetValue(value);
            attr.IsDefaultAttribute = true;
            attr.lineInfo.Set(lineNo, linePos);
            attr.lineInfo2.Set(valueLineNo, valueLinePos);

            // handle special attributes:
            if (attr.prefix.Length == 0)
            {
                // default namespace declaration
                if (Ref.Equal(attr.localName, _xmlNs))
                {
                    OnDefaultNamespaceDecl(attr);
                    if (!_attrNeedNamespaceLookup)
                    {
                        // change element default namespace
                        Debug.Assert(_nodes[_index].type == XmlNodeType.Element);
                        if (_nodes[_index].prefix.Length == 0)
                        {
                            _nodes[_index].ns = _xmlContext.defaultNamespace;
                        }
                    }
                }
            }
            else
            {
                // prefixed namespace declaration
                if (Ref.Equal(attr.prefix, _xmlNs))
                {
                    OnNamespaceDecl(attr);
                    if (!_attrNeedNamespaceLookup)
                    {
                        // change namespace of current element and attributes
                        string pref = attr.localName;
                        Debug.Assert(_nodes[_index].type == XmlNodeType.Element);
                        Debug.Assert(_namespaceManager != null);
                        for (int i = _index; i < _index + _attrCount + 1; i++)
                        {
                            if (_nodes[i].prefix.Equals(pref))
                            {
                                _nodes[i].ns = _namespaceManager.LookupNamespace(pref);
                            }
                        }
                    }
                }
                // xml: attribute
                else
                {
                    if (isXmlAttribute)
                    {
                        OnXmlReservedAttribute(attr);
                    }
                }
            }

            _fullAttrCleanup = true;
            return attr;
        }

        internal bool DisableUndeclaredEntityCheck
        {
            set
            {
                _disableUndeclaredEntityCheck = value;
            }
        }

        private int ReadContentAsBinary(byte[] buffer, int index, int count)
        {
            Debug.Assert(_incReadDecoder != null);

            if (_incReadState == IncrementalReadState.ReadContentAsBinary_End)
            {
                return 0;
            }

            _incReadDecoder.SetNextOutputBuffer(buffer, index, count);

            while (true)
            {
                // read what is already cached in curNode
                int charsRead = 0;
                try
                {
                    charsRead = _curNode.CopyToBinary(_incReadDecoder, _readValueOffset);
                }
                // add line info to the exception
                catch (XmlException e)
                {
                    _curNode.AdjustLineInfo(_readValueOffset, _ps.eolNormalized, ref _incReadLineInfo);
                    ReThrow(e, _incReadLineInfo.lineNo, _incReadLineInfo.linePos);
                }
                _readValueOffset += charsRead;

                if (_incReadDecoder.IsFull)
                {
                    return _incReadDecoder.DecodedCount;
                }

                // if on partial value, read the rest of it
                if (_incReadState == IncrementalReadState.ReadContentAsBinary_OnPartialValue)
                {
                    _curNode.SetValue(string.Empty);
                    Debug.Assert(_ps.chars != null);

                    // read next chunk of text
                    bool endOfValue = false;
                    int startPos = 0;
                    int endPos = 0;
                    while (!_incReadDecoder.IsFull && !endOfValue)
                    {
                        int orChars = 0;

                        // store current line info and parse more text
                        _incReadLineInfo.Set(_ps.LineNo, _ps.LinePos);
                        endOfValue = ParseText(out startPos, out endPos, ref orChars);

                        try
                        {
                            charsRead = _incReadDecoder.Decode(_ps.chars, startPos, endPos - startPos);
                        }
                        // add line info to the exception
                        catch (XmlException e)
                        {
                            ReThrow(e, _incReadLineInfo.lineNo, _incReadLineInfo.linePos);
                        }
                        startPos += charsRead;
                    }

                    _incReadState = endOfValue ? IncrementalReadState.ReadContentAsBinary_OnCachedValue : IncrementalReadState.ReadContentAsBinary_OnPartialValue;
                    _readValueOffset = 0;

                    if (_incReadDecoder.IsFull)
                    {
                        _curNode.SetValue(_ps.chars, startPos, endPos - startPos);
                        // adjust line info for the chunk that has been already decoded
                        AdjustLineInfo(_ps.chars, startPos - charsRead, startPos, _ps.eolNormalized, ref _incReadLineInfo);
                        _curNode.SetLineInfo(_incReadLineInfo.lineNo, _incReadLineInfo.linePos);
                        return _incReadDecoder.DecodedCount;
                    }
                }

                // reset to normal state so we can call Read() to move forward
                ParsingFunction tmp = _parsingFunction;
                _parsingFunction = _nextParsingFunction;
                _nextParsingFunction = _nextNextParsingFunction;

                // move to next textual node in the element content; throw on sub elements
                if (!MoveToNextContentNode(true))
                {
                    SetupReadContentAsBinaryState(tmp);
                    _incReadState = IncrementalReadState.ReadContentAsBinary_End;
                    return _incReadDecoder.DecodedCount;
                }

                SetupReadContentAsBinaryState(tmp);
                _incReadLineInfo.Set(_curNode.LineNo, _curNode.LinePos);
            }
        }

        private int ReadElementContentAsBinary(byte[] buffer, int index, int count)
        {
            if (count == 0)
            {
                return 0;
            }

            int decoded = ReadContentAsBinary(buffer, index, count);
            if (decoded > 0)
            {
                return decoded;
            }

            // if 0 bytes returned check if we are on a closing EndElement, throw exception if not
            if (_curNode.type != XmlNodeType.EndElement)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, _curNode.type.ToString(), this as IXmlLineInfo);
            }

            // reset state
            _parsingFunction = _nextParsingFunction;
            _nextParsingFunction = _nextNextParsingFunction;
            Debug.Assert(_parsingFunction != ParsingFunction.InReadElementContentAsBinary);

            // move off the EndElement
            _outerReader.Read();
            return 0;
        }

        private void InitBase64Decoder()
        {
            if (_base64Decoder == null)
            {
                _base64Decoder = new Base64Decoder();
            }
            else
            {
                _base64Decoder.Reset();
            }
            _incReadDecoder = _base64Decoder;
        }

        private void InitBinHexDecoder()
        {
            if (_binHexDecoder == null)
            {
                _binHexDecoder = new BinHexDecoder();
            }
            else
            {
                _binHexDecoder.Reset();
            }
            _incReadDecoder = _binHexDecoder;
        }

        // SxS: URIs are resolved only to be compared. No resource exposure. It's OK to suppress the SxS warning.
        private static bool UriEqual(Uri? uri1, string? uri1Str, string? uri2Str, XmlResolver? resolver)
        {
            if (resolver == null)
            {
                return uri1Str == uri2Str;
            }

            if (uri1 == null)
            {
                uri1 = resolver.ResolveUri(null, uri1Str);
            }

            Uri uri2 = resolver.ResolveUri(null, uri2Str);
            return uri1.Equals(uri2);
        }

        /// <summary>
        /// This method should be called every time the reader is about to consume some number of
        ///   characters from the input. It will count it against the security counters and
        ///   may throw if some of the security limits are exceeded.
        /// </summary>
        /// <param name="characters">Number of characters to be consumed.</param>
        /// <param name="inEntityReference">true if the characters are result of entity expansion.</param>
        private void RegisterConsumedCharacters(long characters, bool inEntityReference)
        {
            Debug.Assert(characters >= 0);
            if (_maxCharactersInDocument > 0)
            {
                long newCharactersInDocument = _charactersInDocument + characters;
                if (newCharactersInDocument < _charactersInDocument)
                {
                    // Integer overflow while counting
                    ThrowWithoutLineInfo(SR.Xml_LimitExceeded, "MaxCharactersInDocument");
                }
                else
                {
                    _charactersInDocument = newCharactersInDocument;
                }
                if (_charactersInDocument > _maxCharactersInDocument)
                {
                    // The limit was exceeded for the total number of characters in the document
                    ThrowWithoutLineInfo(SR.Xml_LimitExceeded, "MaxCharactersInDocument");
                }
            }

            if (_maxCharactersFromEntities > 0 && inEntityReference)
            {
                long newCharactersFromEntities = _charactersFromEntities + characters;
                if (newCharactersFromEntities < _charactersFromEntities)
                {
                    // Integer overflow while counting
                    ThrowWithoutLineInfo(SR.Xml_LimitExceeded, "MaxCharactersFromEntities");
                }
                else
                {
                    _charactersFromEntities = newCharactersFromEntities;
                }
                if (_charactersFromEntities > _maxCharactersFromEntities)
                {
                    // The limit was exceeded for the number of characters from entities
                    ThrowWithoutLineInfo(SR.Xml_LimitExceeded, "MaxCharactersFromEntities");
                }
            }
        }

        internal static void AdjustLineInfo(char[] chars, int startPos, int endPos, bool isNormalized, ref LineInfo lineInfo)
        {
            Debug.Assert(startPos >= 0);
            Debug.Assert(endPos < chars.Length);
            Debug.Assert(startPos <= endPos);

            AdjustLineInfo(chars.AsSpan(startPos, endPos - startPos), isNormalized, ref lineInfo);
        }

        internal static void AdjustLineInfo(string str, int startPos, int endPos, bool isNormalized, ref LineInfo lineInfo)
        {
            Debug.Assert(startPos >= 0);
            Debug.Assert(endPos < str.Length);
            Debug.Assert(startPos <= endPos);

            AdjustLineInfo(str.AsSpan(startPos, endPos - startPos), isNormalized, ref lineInfo);
        }

        private static void AdjustLineInfo(ReadOnlySpan<char> chars, bool isNormalized, ref LineInfo lineInfo)
        {
            int lastNewLinePos = -1;
            for (int i = 0; i < chars.Length; i++)
            {
                switch (chars[i])
                {
                    case '\n':
                        lineInfo.lineNo++;
                        lastNewLinePos = i;
                        break;
                    case '\r':
                        if (isNormalized)
                        {
                            break;
                        }
                        lineInfo.lineNo++;
                        lastNewLinePos = i;
                        int nextIdx = i + 1;
                        if ((uint)nextIdx < (uint)chars.Length && chars[nextIdx] == '\n')
                        {
                            i++;
                            lastNewLinePos++;
                        }
                        break;
                }
            }
            if (lastNewLinePos >= 0)
            {
                lineInfo.linePos = chars.Length - lastNewLinePos;
            }
        }

        // StripSpaces removes spaces at the beginning and at the end of the value and replaces sequences of spaces with a single space
        internal static string StripSpaces(string value)
        {
            int len = value.Length;
            if (len <= 0)
            {
                return string.Empty;
            }

            int startPos = 0;
            StringBuilder? norValue = null;

            while (value[startPos] == 0x20)
            {
                startPos++;
                if (startPos == len)
                {
                    return " ";
                }
            }

            int i;
            for (i = startPos; i < len; i++)
            {
                if (value[i] == 0x20)
                {
                    int j = i + 1;
                    while (j < len && value[j] == 0x20)
                    {
                        j++;
                    }
                    if (j == len)
                    {
                        if (norValue == null)
                        {
                            return value.Substring(startPos, i - startPos);
                        }
                        else
                        {
                            norValue.Append(value, startPos, i - startPos);
                            return norValue.ToString();
                        }
                    }
                    if (j > i + 1)
                    {
                        if (norValue == null)
                        {
                            norValue = new StringBuilder(len);
                        }
                        norValue.Append(value, startPos, i - startPos + 1);
                        startPos = j;
                        i = j - 1;
                    }
                }
            }
            if (norValue == null)
            {
                return (startPos == 0) ? value : value.Substring(startPos, len - startPos);
            }
            else
            {
                if (i > startPos)
                {
                    norValue.Append(value, startPos, i - startPos);
                }
                return norValue.ToString();
            }
        }

        // StripSpaces removes spaces at the beginning and at the end of the value and replaces sequences of spaces with a single space
        internal static void StripSpaces(char[] value, int index, ref int len)
        {
            if (len <= 0)
            {
                return;
            }

            int startPos = index;
            int endPos = index + len;

            while (value[startPos] == 0x20)
            {
                startPos++;
                if (startPos == endPos)
                {
                    len = 1;
                    return;
                }
            }

            int offset = startPos - index;
            int i;
            for (i = startPos; i < endPos; i++)
            {
                char ch;
                if ((ch = value[i]) == 0x20)
                {
                    int j = i + 1;
                    while (j < endPos && value[j] == 0x20)
                    {
                        j++;
                    }
                    if (j == endPos)
                    {
                        offset += (j - i);
                        break;
                    }
                    if (j > i + 1)
                    {
                        offset += (j - i - 1);
                        i = j - 1;
                    }
                }
                value[i - offset] = ch;
            }
            len -= offset;
        }

        internal static void BlockCopyChars(char[] src, int srcOffset, char[] dst, int dstOffset, int count)
        {
            // PERF: Buffer.BlockCopy is faster than Array.Copy
            Buffer.BlockCopy(src, srcOffset * sizeof(char), dst, dstOffset * sizeof(char), count * sizeof(char));
        }

        internal static void BlockCopy(byte[] src, int srcOffset, byte[] dst, int dstOffset, int count)
        {
            Buffer.BlockCopy(src, srcOffset, dst, dstOffset, count);
        }

        static partial void ConvertAbsoluteUnixPathToAbsoluteUri([NotNullIfNotNull("url")] ref string? url, XmlResolver? resolver);
    }
}
