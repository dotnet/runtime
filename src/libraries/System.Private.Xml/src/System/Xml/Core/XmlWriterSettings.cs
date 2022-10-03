// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Xml.Xsl.Runtime;

namespace System.Xml
{
    public enum XmlOutputMethod
    {
        Xml = 0,    // Use Xml 1.0 rules to serialize
        Html = 1,    // Use Html rules specified by Xslt specification to serialize
        Text = 2,    // Only serialize text blocks
        AutoDetect = 3,    // Choose between Xml and Html output methods at runtime (using Xslt rules to do so)
    }

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
        internal static readonly XmlWriterSettings s_defaultWriterSettings = new() { ReadOnly = true };
        private bool _useAsync;
        private Encoding _encoding;
        private bool _omitXmlDecl;
        private NewLineHandling _newLineHandling;
        private string _newLineChars;
        private string _indentChars;
        private bool _newLineOnAttributes;
        private bool _closeOutput;
        private NamespaceHandling _namespaceHandling;
        private ConformanceLevel _conformanceLevel;
        private bool _checkCharacters;
        private bool _writeEndDocumentOnClose;
        private bool _doNotEscapeUriAttributes;
        private bool _mergeCDataSections;
        private string? _mediaType;
        private string? _docTypeSystem;
        private string? _docTypePublic;
        private XmlStandalone _standalone;
        private bool _autoXmlDecl;
        public XmlWriterSettings()
        {
            Initialize();
        }

        public bool Async
        {
            get => _useAsync;
            set
            {
                CheckReadOnly();
                _useAsync = value;
            }
        }

        public Encoding Encoding
        {
            get => _encoding;
            [MemberNotNull(nameof(_encoding))]
            set
            {
                CheckReadOnly();
                _encoding = value;
            }
        }

        // True if an xml declaration should *not* be written.
        public bool OmitXmlDeclaration
        {
            get => _omitXmlDecl;
            set
            {
                CheckReadOnly();
                _omitXmlDecl = value;
            }
        }

        // See NewLineHandling enum for details.
        public NewLineHandling NewLineHandling
        {
            get => _newLineHandling;
            set
            {
                CheckReadOnly();

                if (unchecked((uint)value) > (uint)NewLineHandling.None)
                {
                    ThrowArgumentOutOfRangeException(nameof(value));
                }

                _newLineHandling = value;
            }
        }

        // Line terminator string. By default, this is a carriage return followed by a line feed ("\r\n").
        public string NewLineChars
        {
            get => _newLineChars;
            [MemberNotNull(nameof(_newLineChars))]
            set
            {
                CheckReadOnly();
                ArgumentNullException.ThrowIfNull(value);
                _newLineChars = value;
            }
        }

        // True if output should be indented using rules that are appropriate to the output rules (i.e. Xml, Html, etc).
        public bool Indent
        {
            get => IndentInternal == TriState.True;
            set
            {
                CheckReadOnly();
                IndentInternal = value ? TriState.True : TriState.False;
            }
        }

        // Characters to use when indenting. This is usually tab or some spaces, but can be anything.
        public string IndentChars
        {
            get => _indentChars;
            [MemberNotNull(nameof(_indentChars))]
            set
            {
                CheckReadOnly();
                ArgumentNullException.ThrowIfNull(value);
                _indentChars = value;
            }
        }

        // Whether or not indent attributes on new lines.
        public bool NewLineOnAttributes
        {
            get => _newLineOnAttributes;
            set
            {
                CheckReadOnly();
                _newLineOnAttributes = value;
            }
        }

        // Whether or not the XmlWriter should close the underlying stream or TextWriter when Close is called on the XmlWriter.
        public bool CloseOutput
        {
            get => _closeOutput;
            set
            {
                CheckReadOnly();
                _closeOutput = value;
            }
        }

        // Conformance
        // See ConformanceLevel enum for details.
        public ConformanceLevel ConformanceLevel
        {
            get => _conformanceLevel;
            set
            {
                CheckReadOnly();

                if (unchecked((uint)value) > (uint)ConformanceLevel.Document)
                {
                    ThrowArgumentOutOfRangeException(nameof(value));
                }
                _conformanceLevel = value;
            }
        }

        // Whether or not to check content characters that they are valid XML characters.
        public bool CheckCharacters
        {
            get => _checkCharacters;
            set
            {
                CheckReadOnly();
                _checkCharacters = value;
            }
        }

        // Whether or not to remove duplicate namespace declarations
        public NamespaceHandling NamespaceHandling
        {
            get => _namespaceHandling;
            set
            {
                CheckReadOnly();
                if (unchecked((uint)value) > (uint)(NamespaceHandling.OmitDuplicates))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _namespaceHandling = value;
            }
        }

        //Whether or not to auto complete end-element when close/dispose
        public bool WriteEndDocumentOnClose
        {
            get => _writeEndDocumentOnClose;
            set
            {
                CheckReadOnly();
                _writeEndDocumentOnClose = value;
            }
        }

        // Specifies the method (Html, Xml, etc.) that will be used to serialize the result tree.
        public XmlOutputMethod OutputMethod { get; internal set; }

        public void Reset()
        {
            CheckReadOnly();
            Initialize();
        }

        // Deep clone all settings (except read-only, which is always set to false).  The original and new objects
        // can now be set independently of each other.
        public XmlWriterSettings Clone()
        {
            XmlWriterSettings clonedSettings = (XmlWriterSettings)MemberwiseClone();

            // Deep clone shared settings that are not immutable
            clonedSettings.CDataSectionElements = new List<XmlQualifiedName>(CDataSectionElements);
            clonedSettings.ReadOnly = false;

            return clonedSettings;
        }

        // Set of XmlQualifiedNames that identify any elements that need to have text children wrapped in CData sections.
        internal List<XmlQualifiedName> CDataSectionElements { get; private set; } = new();

        // Used in Html writer to disable encoding of uri attributes
        public bool DoNotEscapeUriAttributes
        {
            get => _doNotEscapeUriAttributes;
            set
            {
                CheckReadOnly();
                _doNotEscapeUriAttributes = value;
            }
        }

        internal bool MergeCDataSections
        {
            get => _mergeCDataSections;
            set
            {
                CheckReadOnly();
                _mergeCDataSections = value;
            }
        }

        // Used in Html writer when writing Meta element.  Null denotes the default media type.
        internal string? MediaType
        {
            get => _mediaType;
            set
            {
                CheckReadOnly();
                _mediaType = value;
            }
        }

        // System Id in doc-type declaration.  Null denotes the absence of the system Id.
        internal string? DocTypeSystem
        {
            get => _docTypeSystem;
            set
            {
                CheckReadOnly();
                _docTypeSystem = value;
            }
        }

        // Public Id in doc-type declaration.  Null denotes the absence of the public Id.
        internal string? DocTypePublic
        {
            get => _docTypePublic;
            set
            {
                CheckReadOnly();
                _docTypePublic = value;
            }
        }

        // Yes for standalone="yes", No for standalone="no", and Omit for no standalone.
        internal XmlStandalone Standalone
        {
            get => _standalone;
            set
            {
                CheckReadOnly();
                _standalone = value;
            }
        }

        // True if an xml declaration should automatically be output (no need to call WriteStartDocument)
        internal bool AutoXmlDeclaration
        {
            get => _autoXmlDecl;
            set
            {
                CheckReadOnly();
                _autoXmlDecl = value;
            }
        }

        // If TriState.Unknown, then Indent property was not explicitly set.  In this case, the AutoDetect output
        // method will default to Indent=true for Html and Indent=false for Xml.
        internal TriState IndentInternal { get; set; }
        private bool IsQuerySpecific => CDataSectionElements.Count != 0 || _docTypePublic != null || _docTypeSystem != null || _standalone == XmlStandalone.Yes;
        internal XmlWriter CreateWriter(string outputFileName)
        {
            ArgumentNullException.ThrowIfNull(outputFileName);

            // need to clone the settigns so that we can set CloseOutput to true to make sure the stream gets closed in the end
            XmlWriterSettings newSettings = this;
            if (!newSettings.CloseOutput)
            {
                newSettings = newSettings.Clone();
                newSettings.CloseOutput = true;
            }

            FileStream? fs = null;
            try
            {
                // open file stream
                fs = new FileStream(outputFileName, FileMode.Create, FileAccess.Write, FileShare.Read, 0x1000, _useAsync);

                // create writer
                return newSettings.CreateWriter(fs);
            }
            catch
            {
                fs?.Dispose();
                throw;
            }
        }

        internal XmlWriter CreateWriter(Stream output)
        {
            ArgumentNullException.ThrowIfNull(output);

            XmlWriter writer;

            // create raw writer
            Debug.Assert(Encoding.UTF8.WebName == "utf-8");
            if (Encoding.WebName == "utf-8")
            { // Encoding.CodePage is not supported in Silverlight
                // create raw UTF-8 writer
                switch (OutputMethod)
                {
                    case XmlOutputMethod.Xml:
                        writer = Indent ? new XmlUtf8RawTextWriterIndent(output, this) : new XmlUtf8RawTextWriter(output, this);
                        break;
                    case XmlOutputMethod.Html:
                        writer = Indent ? new HtmlUtf8RawTextWriterIndent(output, this) : new HtmlUtf8RawTextWriter(output, this);
                        break;
                    case XmlOutputMethod.Text:
                        writer = new TextUtf8RawTextWriter(output, this);
                        break;
                    case XmlOutputMethod.AutoDetect:
                        writer = new XmlAutoDetectWriter(output, this);
                        break;
                    default:
                        Debug.Fail("Invalid XmlOutputMethod setting.");
                        return null!;
                }
            }
            else
            {
                // Otherwise, create a general-purpose writer than can do any encoding
                switch (OutputMethod)
                {
                    case XmlOutputMethod.Xml:
                        writer = Indent ? new XmlEncodedRawTextWriterIndent(output, this) : new XmlEncodedRawTextWriter(output, this);
                        break;
                    case XmlOutputMethod.Html:
                        writer = Indent ? new HtmlEncodedRawTextWriterIndent(output, this) : new HtmlEncodedRawTextWriter(output, this);
                        break;
                    case XmlOutputMethod.Text:
                        writer = new TextEncodedRawTextWriter(output, this);
                        break;
                    case XmlOutputMethod.AutoDetect:
                        writer = new XmlAutoDetectWriter(output, this);
                        break;
                    default:
                        Debug.Fail("Invalid XmlOutputMethod setting.");
                        return null!;
                }
            }

            // Wrap with Xslt/XQuery specific writer if needed;
            // XmlOutputMethod.AutoDetect writer does this lazily when it creates the underlying Xml or Html writer.
            if (OutputMethod != XmlOutputMethod.AutoDetect)
            {
                if (IsQuerySpecific)
                {
                    // Create QueryOutputWriter if CData sections or DocType need to be tracked
                    writer = new QueryOutputWriter((XmlRawWriter)writer, this);
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
            ArgumentNullException.ThrowIfNull(output);

            XmlWriter writer;

            // create raw writer
            switch (OutputMethod)
            {
                case XmlOutputMethod.Xml:
                    writer = Indent ? new XmlEncodedRawTextWriterIndent(output, this) : new XmlEncodedRawTextWriter(output, this);
                    break;
                case XmlOutputMethod.Html:
                    writer = Indent ? new HtmlEncodedRawTextWriterIndent(output, this) : new HtmlEncodedRawTextWriter(output, this);
                    break;
                case XmlOutputMethod.Text:
                    writer = new TextEncodedRawTextWriter(output, this);
                    break;
                case XmlOutputMethod.AutoDetect:
                    writer = new XmlAutoDetectWriter(output, this);
                    break;
                default:
                    Debug.Fail("Invalid XmlOutputMethod setting.");
                    return null!;
            }

            // XmlOutputMethod.AutoDetect writer does this lazily when it creates the underlying Xml or Html writer.
            if (OutputMethod != XmlOutputMethod.AutoDetect)
            {
                if (IsQuerySpecific)
                {
                    // Create QueryOutputWriter if CData sections or DocType need to be tracked
                    writer = new QueryOutputWriter((XmlRawWriter)writer, this);
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

        internal XmlWriter CreateWriter(XmlWriter output)
        {
            ArgumentNullException.ThrowIfNull(output);

            return AddConformanceWrapper(output);
        }


        internal bool ReadOnly { get; set; }
        private void CheckReadOnly([CallerMemberName]string? propertyName = null)
        {
            if (ReadOnly)
            {
                throw new XmlException(SR.Xml_ReadOnlyProperty, $"{GetType().Name}.{propertyName}");
            }
        }

        [MemberNotNull(nameof(_encoding))]
        [MemberNotNull(nameof(_newLineChars))]
        [MemberNotNull(nameof(_indentChars))]
        private void Initialize()
        {
            _encoding = Encoding.UTF8;
            _omitXmlDecl = false;
            _newLineHandling = NewLineHandling.Replace;
            _newLineChars = Environment.NewLine; // "\r\n" on Windows, "\n" on Unix
            IndentInternal = TriState.Unknown;
            _indentChars = "  ";
            _newLineOnAttributes = false;
            _closeOutput = false;
            _namespaceHandling = NamespaceHandling.Default;
            _conformanceLevel = ConformanceLevel.Document;
            _checkCharacters = true;
            _writeEndDocumentOnClose = true;
            OutputMethod = XmlOutputMethod.Xml;
            CDataSectionElements.Clear();
            _mergeCDataSections = false;
            _mediaType = null;
            _docTypeSystem = null;
            _docTypePublic = null;
            _standalone = XmlStandalone.Omit;
            _doNotEscapeUriAttributes = false;
            _useAsync = false;
            ReadOnly = false;
        }

        private XmlWriter AddConformanceWrapper(XmlWriter baseWriter)
        {
            ConformanceLevel confLevel = ConformanceLevel.Auto;
            XmlWriterSettings? baseWriterSettings = baseWriter.Settings;
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
                    confLevel = ConformanceLevel;
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

            if (this.IsQuerySpecific && (baseWriterSettings == null || !baseWriterSettings.IsQuerySpecific))
            {
                // Create QueryOutputWriterV1 if CData sections or DocType need to be tracked
                writer = new QueryOutputWriterV1(writer, this);
            }

            return writer;
        }

        /// <summary>
        /// Serialize the object to BinaryWriter.
        /// </summary>
        internal void GetObjectData(XmlQueryDataWriter writer)
        {
            // Encoding encoding;
            // NOTE: For Encoding we serialize only CodePage, and ignore EncoderFallback/DecoderFallback.
            // It suffices for XSLT purposes, but not in the general case.
            Debug.Assert(Encoding.Equals(Encoding.GetEncoding(Encoding.CodePage)), "Cannot serialize encoding correctly");
            writer.Write(Encoding.CodePage);
            // bool omitXmlDecl;
            writer.Write(OmitXmlDeclaration);
            // NewLineHandling newLineHandling;
            writer.Write((sbyte)NewLineHandling);
            // string newLineChars;
            writer.WriteStringQ(NewLineChars);
            // TriState indent;
            writer.Write((sbyte)IndentInternal);
            // string indentChars;
            writer.WriteStringQ(IndentChars);
            // bool newLineOnAttributes;
            writer.Write(NewLineOnAttributes);
            // bool closeOutput;
            writer.Write(CloseOutput);
            // ConformanceLevel conformanceLevel;
            writer.Write((sbyte)ConformanceLevel);
            // bool checkCharacters;
            writer.Write(CheckCharacters);
            // XmlOutputMethod outputMethod;
            writer.Write((sbyte)OutputMethod);
            // List<XmlQualifiedName> cdataSections;
            writer.Write(CDataSectionElements.Count);
            foreach (XmlQualifiedName qName in CDataSectionElements)
            {
                writer.Write(qName.Name);
                writer.Write(qName.Namespace);
            }
            // bool mergeCDataSections;
            writer.Write(_mergeCDataSections);
            // string mediaType;
            writer.WriteStringQ(_mediaType);
            // string docTypeSystem;
            writer.WriteStringQ(_docTypeSystem);
            // string docTypePublic;
            writer.WriteStringQ(_docTypePublic);
            // XmlStandalone standalone;
            writer.Write((sbyte)_standalone);
            // bool autoXmlDecl;
            writer.Write(_autoXmlDecl);
            // bool isReadOnly;
            writer.Write(ReadOnly);
        }

        /// <summary>
        /// Deserialize the object from BinaryReader.
        /// </summary>
        internal XmlWriterSettings(XmlQueryDataReader reader)
        {
            // Encoding encoding;
            Encoding = Encoding.GetEncoding(reader.ReadInt32());
            // bool omitXmlDecl;
            OmitXmlDeclaration = reader.ReadBoolean();
            // NewLineHandling newLineHandling;
            NewLineHandling = (NewLineHandling)reader.ReadSByte(0, (sbyte)NewLineHandling.None);
            // string newLineChars;
            NewLineChars = reader.ReadStringQ()!;
            // TriState indent;
            IndentInternal = (TriState)reader.ReadSByte((sbyte)TriState.Unknown, (sbyte)TriState.True);
            // string indentChars;
            IndentChars = reader.ReadStringQ()!;
            // bool newLineOnAttributes;
            NewLineOnAttributes = reader.ReadBoolean();
            // bool closeOutput;
            CloseOutput = reader.ReadBoolean();
            // ConformanceLevel conformanceLevel;
            ConformanceLevel = (ConformanceLevel)reader.ReadSByte(0, (sbyte)ConformanceLevel.Document);
            // bool checkCharacters;
            CheckCharacters = reader.ReadBoolean();
            // XmlOutputMethod outputMethod;
            OutputMethod = (XmlOutputMethod)reader.ReadSByte(0, (sbyte)XmlOutputMethod.AutoDetect);
            // List<XmlQualifiedName> cdataSections;
            int length = reader.ReadInt32();
            CDataSectionElements = new List<XmlQualifiedName>(length);
            for (int idx = 0; idx < length; idx++)
            {
                CDataSectionElements.Add(new XmlQualifiedName(reader.ReadString(), reader.ReadString()));
            }
            // bool mergeCDataSections;
            _mergeCDataSections = reader.ReadBoolean();
            // string mediaType;
            _mediaType = reader.ReadStringQ();
            // string docTypeSystem;
            _docTypeSystem = reader.ReadStringQ();
            // string docTypePublic;
            _docTypePublic = reader.ReadStringQ();
            // XmlStandalone standalone;
            Standalone = (XmlStandalone)reader.ReadSByte(0, (sbyte)XmlStandalone.No);
            // bool autoXmlDecl;
            _autoXmlDecl = reader.ReadBoolean();
            // bool isReadOnly;
            ReadOnly = reader.ReadBoolean();
        }

        [DoesNotReturn]
        [StackTraceHidden]
        private static void ThrowArgumentOutOfRangeException(string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }
    }
}
