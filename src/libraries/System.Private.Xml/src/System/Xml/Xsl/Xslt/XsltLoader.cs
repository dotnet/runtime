// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#define XSLT2

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.IO;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using ContextInfo = System.Xml.Xsl.Xslt.XsltInput.ContextInfo;
using F = System.Xml.Xsl.Xslt.AstFactory;
using TypeFactory = System.Xml.Xsl.XmlQueryTypeFactory;
using QName = System.Xml.Xsl.Xslt.XsltInput.DelayedQName;
using XsltAttribute = System.Xml.Xsl.Xslt.XsltInput.XsltAttribute;

namespace System.Xml.Xsl.Xslt
{
    internal sealed class XsltLoader : IErrorHelper
    {
        private Compiler _compiler = null!;
        private XmlResolver _xmlResolver = null!;
        private QueryReaderSettings _readerSettings = null!;
        private KeywordsTable _atoms = null!; // XSLT keywords atomized with QueryReaderSettings.NameTabel
        private XsltInput _input = null!;     // Current input stream
        private Stylesheet? _curStylesheet;   // Current stylesheet
        private Template? _curTemplate;       // Current template

        internal static QilName nullMode = F.QName(string.Empty);

        // Flags which control attribute versioning
        public static int V1Opt = 1;
        public static int V1Req = 2;
        public static int V2Opt = 4;
        public static int V2Req = 8;

        public void Load(Compiler compiler, object stylesheet, XmlResolver? xmlResolver, XmlResolver? origResolver)
        {
            Debug.Assert(compiler != null);
            _compiler = compiler;
            _xmlResolver = xmlResolver ?? XmlResolver.ThrowingResolver;

            XmlReader? reader = stylesheet as XmlReader;
            if (reader != null)
            {
                _readerSettings = new QueryReaderSettings(reader);
                Load(reader);
            }
            else
            {
                // We should take DefaultReaderSettings from Compiler.Settings.DefaultReaderSettings.

                string? uri = stylesheet as string;
                if (uri != null)
                {
                    // If the stylesheet has been provided as a string (URI, really), then we'll bounce
                    // through an XmlResolver to look up its contents. There's a complication here since
                    // the default resolver provided by our caller is likely a throwing resolver, and
                    // a throwing resolver would fail even when attempting to read this URL. We need
                    // to at minimum allow this URL to be read, because the user after all did explicitly
                    // ask us to do so.
                    //
                    // In this case, we'll rely on the 'origResolver' argument, which is the XmlResolver
                    // which was provided *to our caller* before any default substitution took place.
                    // If an explicit resolver was specified, we'll honor it. Otherwise we'll substitute
                    // an XmlUrlResolver for this one read operation. The stored resolver (which is used
                    // for reads beyond the initial stylesheet read) will use a throwing resolver as its
                    // default, as shown at the very top of this method.

                    origResolver ??= new XmlUrlResolver();
                    Uri resolvedUri = origResolver.ResolveUri(null, uri);
                    if (resolvedUri == null)
                    {
                        throw new XslLoadException(SR.Xslt_CantResolve, uri);
                    }

                    _readerSettings = new QueryReaderSettings(new NameTable());
                    using (reader = CreateReader(resolvedUri, origResolver))
                    {
                        Load(reader);
                    }
                }
                else
                {
                    IXPathNavigable? navigable = stylesheet as IXPathNavigable;
                    if (navigable != null)
                    {
                        reader = XPathNavigatorReader.Create(navigable.CreateNavigator()!);
                        _readerSettings = new QueryReaderSettings(reader.NameTable);
                        Load(reader);
                    }
                    else
                    {
                        Debug.Fail("Should never get here");
                    }
                }
            }
            Debug.Assert(compiler.Root != null);
            compiler.StartApplyTemplates = F.ApplyTemplates(nullMode);
            ProcessOutputSettings();
            foreach (AttributeSet attSet in compiler.AttributeSets.Values)
            {
                CheckAttributeSetsDfs(attSet); // Check attribute sets for circular references using dfs marking method
            }
        }

        private void Load(XmlReader reader)
        {
            _atoms = new KeywordsTable(reader.NameTable);
            AtomizeAttributes();
            LoadStylesheet(reader, /*include:*/false);
        }


        private void AtomizeAttributes(XsltAttribute[] attributes)
        {
            for (int i = 0; i < attributes.Length; i++)
            {
                attributes[i].name = _atoms.NameTable.Add(attributes[i].name);
            }
        }
        private void AtomizeAttributes()
        {
            AtomizeAttributes(_stylesheetAttributes);
            AtomizeAttributes(_importIncludeAttributes);
            AtomizeAttributes(_loadStripSpaceAttributes);
            AtomizeAttributes(_outputAttributes);
            AtomizeAttributes(_keyAttributes);
            AtomizeAttributes(_decimalFormatAttributes);
            AtomizeAttributes(_namespaceAliasAttributes);
            AtomizeAttributes(_attributeSetAttributes);
            AtomizeAttributes(_templateAttributes);
            AtomizeAttributes(_scriptAttributes);
            AtomizeAttributes(_assemblyAttributes);
            AtomizeAttributes(_usingAttributes);
            AtomizeAttributes(_applyTemplatesAttributes);
            AtomizeAttributes(_callTemplateAttributes);
            AtomizeAttributes(_copyAttributes);
            AtomizeAttributes(_copyOfAttributes);
            AtomizeAttributes(_ifAttributes);
            AtomizeAttributes(_forEachAttributes);
            AtomizeAttributes(_messageAttributes);
            AtomizeAttributes(_numberAttributes);
            AtomizeAttributes(_valueOfAttributes);
            AtomizeAttributes(_variableAttributes);
            AtomizeAttributes(_paramAttributes);
            AtomizeAttributes(_withParamAttributes);
            AtomizeAttributes(_commentAttributes);
            AtomizeAttributes(_processingInstructionAttributes);
            AtomizeAttributes(_textAttributes);
            AtomizeAttributes(_elementAttributes);
            AtomizeAttributes(_attributeAttributes);
            AtomizeAttributes(_sortAttributes);
#if XSLT2
            AtomizeAttributes(characterMapAttributes);
            AtomizeAttributes(outputCharacterAttributes);
            AtomizeAttributes(functionAttributes);
            AtomizeAttributes(importSchemaAttributes);
            AtomizeAttributes(documentAttributes);
            AtomizeAttributes(analyzeStringAttributes);
            AtomizeAttributes(namespaceAttributes);
            AtomizeAttributes(performSortAttributes);
            AtomizeAttributes(forEachGroupAttributes);
            AtomizeAttributes(sequenceAttributes);
            AtomizeAttributes(resultDocumentAttributes);
#endif
        }

        private bool V1
        {
            get
            {
                Debug.Assert(_compiler.Version != 0, "Version should be already decided at this point");
                return _compiler.Version == 1;
            }
        }
#if XSLT2
        private bool V2 { get { return ! V1; } }
#endif

        // Import/Include XsltInput management

        private readonly HybridDictionary _documentUriInUse = new HybridDictionary();

        private Uri ResolveUri(string relativeUri, string baseUri)
        {
            Uri? resolvedBaseUri = (baseUri.Length != 0) ? _xmlResolver.ResolveUri(null, baseUri) : null;
            Uri resolvedUri = _xmlResolver.ResolveUri(resolvedBaseUri, relativeUri);
            if (resolvedUri == null)
            {
                throw new XslLoadException(SR.Xslt_CantResolve, relativeUri);
            }
            return resolvedUri;
        }

        private XmlReader CreateReader(Uri uri, XmlResolver xmlResolver)
        {
            object? input = xmlResolver.GetEntity(uri, null, null);

            Stream? stream = input as Stream;
            if (stream != null)
            {
                return _readerSettings.CreateReader(stream, uri.ToString());
            }

            XmlReader? reader = input as XmlReader;
            if (reader != null)
            {
                return reader;
            }

            IXPathNavigable? navigable = input as IXPathNavigable;
            if (navigable != null)
            {
                return XPathNavigatorReader.Create(navigable.CreateNavigator()!);
            }

            throw new XslLoadException(SR.Xslt_CannotLoadStylesheet, uri.ToString(), input == null ? "null" : input.GetType().ToString());
        }

        private Stylesheet LoadStylesheet(Uri uri, bool include)
        {
            using (XmlReader reader = CreateReader(uri, _xmlResolver))
            {
                return LoadStylesheet(reader, include);
            }
        }

        private Stylesheet LoadStylesheet(XmlReader reader, bool include)
        {
            string baseUri = reader.BaseURI!;
            Debug.Assert(!_documentUriInUse.Contains(baseUri), "Circular references must be checked while processing xsl:include and xsl:import");
            _documentUriInUse.Add(baseUri, null);
            _compiler.AddModule(baseUri);

            Stylesheet? prevStylesheet = _curStylesheet;
            XsltInput prevInput = _input;
            Stylesheet thisStylesheet = include ? _curStylesheet! : _compiler.CreateStylesheet();

            _input = new XsltInput(reader, _compiler, _atoms);
            _curStylesheet = thisStylesheet;

            try
            {
                LoadDocument();
                if (!include)
                {
                    _compiler.MergeWithStylesheet(_curStylesheet);

                    List<Uri> importHrefs = _curStylesheet.ImportHrefs;
                    _curStylesheet.Imports = new Stylesheet[importHrefs.Count];
                    // Imports should be compiled in the reverse order. Template lookup logic relies on that.
                    for (int i = importHrefs.Count; 0 <= --i;)
                    {
                        _curStylesheet.Imports[i] = LoadStylesheet(importHrefs[i], /*include:*/false);
                    }
                }
            }
            catch (XslLoadException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (!XmlException.IsCatchableException(e))
                {
                    throw;
                }
                // Note that XmlResolver or XmlReader may throw XmlException with SourceUri == null.
                // In that case we report current line information from XsltInput.
                XmlException? ex = e as XmlException;
                ISourceLineInfo lineInfo = (ex != null && ex.SourceUri != null ?
                    new SourceLineInfo(ex.SourceUri, ex.LineNumber, ex.LinePosition, ex.LineNumber, ex.LinePosition) :
                    _input.BuildReaderLineInfo()
                );
                throw new XslLoadException(e, lineInfo);
            }
            finally
            {
                _documentUriInUse.Remove(baseUri);
                _input = prevInput;
                _curStylesheet = prevStylesheet;
            }
            return thisStylesheet;
        }

        private void LoadDocument()
        {
            if (!_input.FindStylesheetElement())
            {
                ReportError(/*[XT_002]*/SR.Xslt_WrongStylesheetElement);
                return;
            }
            Debug.Assert(_input.NodeType == XmlNodeType.Element);
            if (_input.IsXsltNamespace())
            {
                if (
                    _input.IsKeyword(_atoms.Stylesheet) ||
                    _input.IsKeyword(_atoms.Transform)
                )
                {
                    LoadRealStylesheet();
                }
                else
                {
                    ReportError(/*[XT_002]*/SR.Xslt_WrongStylesheetElement);
                    _input.SkipNode();
                }
            }
            else
            {
                LoadSimplifiedStylesheet();
            }
            _input.Finish();
        }

        private void LoadSimplifiedStylesheet()
        {
            Debug.Assert(!_input.IsXsltNamespace());
            Debug.Assert(_curTemplate == null);

            // Prefix will be fixed later in LoadLiteralResultElement()
            _curTemplate = F.Template(/*name:*/null, /*match:*/"/", /*mode:*/nullMode, /*priority:*/double.NaN, _input.XslVersion);

            // This template has mode=null match="/" and no imports
            _input.CanHaveApplyImports = true;
            XslNode lre = LoadLiteralResultElement(/*asStylesheet:*/true);
            if (lre != null)
            {
                SetLineInfo(_curTemplate, lre.SourceLine);

                List<XslNode> content = new List<XslNode>();
                content.Add(lre);
                SetContent(_curTemplate, content);
                if (!_curStylesheet!.AddTemplate(_curTemplate))
                {
                    Debug.Fail("AddTemplate() returned false for simplified stylesheet");
                }
            }
            _curTemplate = null;
        }

        private readonly XsltAttribute[] _stylesheetAttributes = {
            new XsltAttribute("version",                V1Req | V2Req),
            new XsltAttribute("id",                     V1Opt | V2Opt),
            new XsltAttribute("default-validation",     V2Opt),
            new XsltAttribute("input-type-annotations", V2Opt),
        };
        private void LoadRealStylesheet()
        {
            Debug.Assert(_input.IsXsltNamespace() && (_input.IsKeyword(_atoms.Stylesheet) || _input.IsKeyword(_atoms.Transform)));
            ContextInfo ctxInfo = _input.GetAttributes(_stylesheetAttributes);

            ParseValidationAttribute(2, /*defVal:*/true);
            ParseInputTypeAnnotationsAttribute(3);

            QName parentName = _input.ElementName;
            if (_input.MoveToFirstChild())
            {
                bool atTop = true;
                do
                {
                    bool isImport = false;
                    switch (_input.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (_input.IsXsltNamespace())
                            {
                                if (_input.IsKeyword(_atoms.Import))
                                {
                                    if (!atTop)
                                    {
                                        ReportError(/*[XT0200]*/SR.Xslt_NotAtTop, _input.QualifiedName, parentName);
                                        _input.SkipNode();
                                    }
                                    else
                                    {
                                        isImport = true;
                                        LoadImport();
                                    }
                                }
                                else if (_input.IsKeyword(_atoms.Include))
                                {
                                    LoadInclude();
                                }
                                else if (_input.IsKeyword(_atoms.StripSpace))
                                {
                                    LoadStripSpace(ctxInfo.nsList);
                                }
                                else if (_input.IsKeyword(_atoms.PreserveSpace))
                                {
                                    LoadPreserveSpace(ctxInfo.nsList);
                                }
                                else if (_input.IsKeyword(_atoms.Output))
                                {
                                    LoadOutput();
                                }
                                else if (_input.IsKeyword(_atoms.Key))
                                {
                                    LoadKey(ctxInfo.nsList);
                                }
                                else if (_input.IsKeyword(_atoms.DecimalFormat))
                                {
                                    LoadDecimalFormat(ctxInfo.nsList);
                                }
                                else if (_input.IsKeyword(_atoms.NamespaceAlias))
                                {
                                    LoadNamespaceAlias(ctxInfo.nsList);
                                }
                                else if (_input.IsKeyword(_atoms.AttributeSet))
                                {
                                    LoadAttributeSet(ctxInfo.nsList);
                                }
                                else if (_input.IsKeyword(_atoms.Variable))
                                {
                                    LoadGlobalVariableOrParameter(ctxInfo.nsList);
                                }
                                else if (_input.IsKeyword(_atoms.Param))
                                {
                                    LoadGlobalVariableOrParameter(ctxInfo.nsList);
                                }
                                else if (_input.IsKeyword(_atoms.Template))
                                {
                                    LoadTemplate(ctxInfo.nsList);
#if XSLT2
                            } else if (V2 && input.IsKeyword(atoms.CharacterMap)) {
                                LoadCharacterMap(ctxInfo.nsList);
                            } else if (V2 && input.IsKeyword(atoms.Function)) {
                                LoadFunction(ctxInfo.nsList);
                            } else if (V2 && input.IsKeyword(atoms.ImportSchema)) {
                                LoadImportSchema();
#endif
                                }
                                else
                                {
                                    _input.GetVersionAttribute();
                                    if (!_input.ForwardCompatibility)
                                    {
                                        ReportError(/*[XT_003]*/SR.Xslt_UnexpectedElement, _input.QualifiedName, parentName);
                                    }
                                    _input.SkipNode();
                                }
                            }
                            else if (_input.IsNs(_atoms.UrnMsxsl) && _input.IsKeyword(_atoms.Script))
                            {
                                LoadMsScript(ctxInfo.nsList);
                            }
                            else
                            {
                                if (_input.IsNullNamespace())
                                {
                                    ReportError(/*[XT0130]*/SR.Xslt_NullNsAtTopLevel, _input.LocalName);
                                }
                                // Ignoring non-recognized namespace per XSLT spec 2.2
                                _input.SkipNode();
                            }
                            atTop = isImport;
                            break;

                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                            break;
                        default:
                            Debug.Assert(_input.NodeType == XmlNodeType.Text);
                            ReportError(/*[XT0120]*/SR.Xslt_TextNodesNotAllowed, parentName);
                            break;
                    }
                } while (_input.MoveToNextSibling());
            }
        }


        private readonly XsltAttribute[] _importIncludeAttributes = { new XsltAttribute("href", V1Req | V2Req) };
        // SxS: This method reads resource names from source document and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        private void LoadImport()
        {
            _input.GetAttributes(_importIncludeAttributes);

            if (_input.MoveToXsltAttribute(0, "href"))
            {
                // Resolve href right away using the current BaseUri (it might change later)
                Uri uri = ResolveUri(_input.Value, _input.BaseUri!);

                // Check for circular references
                if (_documentUriInUse.Contains(uri.ToString()))
                {
                    ReportError(/*[XT0210]*/SR.Xslt_CircularInclude, _input.Value);
                }
                else
                {
                    _curStylesheet!.ImportHrefs.Add(uri);
                }
            }
            else
            {
                // The error was already reported. Ignore the instruction
            }

            CheckNoContent();
        }

        // SxS: This method reads resource names from source document and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        private void LoadInclude()
        {
            _input.GetAttributes(_importIncludeAttributes);

            if (_input.MoveToXsltAttribute(0, "href"))
            {
                Uri uri = ResolveUri(_input.Value, _input.BaseUri!);

                // Check for circular references
                if (_documentUriInUse.Contains(uri.ToString()))
                {
                    ReportError(/*[XT0180]*/SR.Xslt_CircularInclude, _input.Value);
                }
                else
                {
                    LoadStylesheet(uri, /*include:*/ true);
                }
            }
            else
            {
                // The error was already reported. Ignore the instruction
            }

            CheckNoContent();
        }

        private readonly XsltAttribute[] _loadStripSpaceAttributes = { new XsltAttribute("elements", V1Req | V2Req) };
        private void LoadStripSpace(NsDecl? stylesheetNsList)
        {
            ContextInfo ctxInfo = _input.GetAttributes(_loadStripSpaceAttributes);
            ctxInfo.nsList = MergeNamespaces(ctxInfo.nsList, stylesheetNsList);

            if (_input.MoveToXsltAttribute(0, _atoms.Elements))
            {
                ParseWhitespaceRules(_input.Value, false);
            }
            CheckNoContent();
        }

        private void LoadPreserveSpace(NsDecl? stylesheetNsList)
        {
            ContextInfo ctxInfo = _input.GetAttributes(_loadStripSpaceAttributes);
            ctxInfo.nsList = MergeNamespaces(ctxInfo.nsList, stylesheetNsList);

            if (_input.MoveToXsltAttribute(0, _atoms.Elements))
            {
                ParseWhitespaceRules(_input.Value, true);
            }
            CheckNoContent();
        }

        private readonly XsltAttribute[] _outputAttributes = {
            new XsltAttribute("name",                   V2Opt),
            new XsltAttribute("method",                 V1Opt | V2Opt),
            new XsltAttribute("byte-order-mark",        V2Opt),
            new XsltAttribute("cdata-section-elements", V1Opt | V2Opt),
            new XsltAttribute("doctype-public",         V1Opt | V2Opt),
            new XsltAttribute("doctype-system",         V1Opt | V2Opt),
            new XsltAttribute("encoding",               V1Opt | V2Opt),
            new XsltAttribute("escape-uri-attributes",  V2Opt),
            new XsltAttribute("include-content-type",   V2Opt),
            new XsltAttribute("indent",                 V1Opt | V2Opt),
            new XsltAttribute("media-type",             V1Opt | V2Opt),
            new XsltAttribute("normalization-form",     V2Opt),
            new XsltAttribute("omit-xml-declaration",   V1Opt | V2Opt),
            new XsltAttribute("standalone",             V1Opt | V2Opt),
            new XsltAttribute("undeclare-prefixes",     V2Opt),
            new XsltAttribute("use-character-maps",     V2Opt),
            new XsltAttribute("version",                V1Opt | V2Opt)
        };
        private void LoadOutput()
        {
            _input.GetAttributes(_outputAttributes);

            Output output = _compiler.Output;
            XmlWriterSettings settings = output.Settings;
            int currentPrec = _compiler.CurrentPrecedence;
            TriState triState;

            QilName? name = ParseQNameAttribute(0);
            if (name != null) ReportNYI("xsl:output/@name");

            if (_input.MoveToXsltAttribute(1, "method"))
            {
                if (output.MethodPrec <= currentPrec)
                {
                    _compiler.EnterForwardsCompatible();
                    XmlOutputMethod outputMethod;
                    XmlQualifiedName? method = ParseOutputMethod(_input.Value, out outputMethod);
                    if (_compiler.ExitForwardsCompatible(_input.ForwardCompatibility) && method != null)
                    {
                        if (currentPrec == output.MethodPrec && !output.Method!.Equals(method))
                        {
                            ReportWarning(/*[XT1560]*/SR.Xslt_AttributeRedefinition, "method");
                        }
                        settings.OutputMethod = outputMethod;
                        output.Method = method;
                        output.MethodPrec = currentPrec;
                    }
                }
            }

            TriState byteOrderMask = ParseYesNoAttribute(2, "byte-order-mark");
            if (byteOrderMask != TriState.Unknown) ReportNYI("xsl:output/@byte-order-mark");

            if (_input.MoveToXsltAttribute(3, "cdata-section-elements"))
            {
                // Do not check the import precedence, the effective value is the union of all specified values
                _compiler.EnterForwardsCompatible();
                string[] qnames = XmlConvert.SplitString(_input.Value);
                List<XmlQualifiedName> list = new List<XmlQualifiedName>();
                for (int i = 0; i < qnames.Length; i++)
                {
                    list.Add(ResolveQName(/*ignoreDefaultNs:*/false, qnames[i]));
                }
                if (_compiler.ExitForwardsCompatible(_input.ForwardCompatibility))
                {
                    settings.CDataSectionElements.AddRange(list);
                }
            }

            if (_input.MoveToXsltAttribute(4, "doctype-public"))
            {
                if (output.DocTypePublicPrec <= currentPrec)
                {
                    if (currentPrec == output.DocTypePublicPrec && settings.DocTypePublic != _input.Value)
                    {
                        ReportWarning(/*[XT1560]*/SR.Xslt_AttributeRedefinition, "doctype-public");
                    }
                    settings.DocTypePublic = _input.Value;
                    output.DocTypePublicPrec = currentPrec;
                }
            }

            if (_input.MoveToXsltAttribute(5, "doctype-system"))
            {
                if (output.DocTypeSystemPrec <= currentPrec)
                {
                    if (currentPrec == output.DocTypeSystemPrec && settings.DocTypeSystem != _input.Value)
                    {
                        ReportWarning(/*[XT1560]*/SR.Xslt_AttributeRedefinition, "doctype-system");
                    }
                    settings.DocTypeSystem = _input.Value;
                    output.DocTypeSystemPrec = currentPrec;
                }
            }

            if (_input.MoveToXsltAttribute(6, "encoding"))
            {
                if (output.EncodingPrec <= currentPrec)
                {
                    try
                    {
                        // Encoding.GetEncoding() should never throw NotSupportedException, only ArgumentException
                        Encoding encoding = Encoding.GetEncoding(_input.Value);
                        if (currentPrec == output.EncodingPrec && output.Encoding != _input.Value)
                        {
                            ReportWarning(/*[XT1560]*/SR.Xslt_AttributeRedefinition, "encoding");
                        }
                        settings.Encoding = encoding;
                        output.Encoding = _input.Value;
                        output.EncodingPrec = currentPrec;
                    }
                    catch (ArgumentException)
                    {
                        if (!_input.ForwardCompatibility)
                        {
                            ReportWarning(/*[XT_004]*/SR.Xslt_InvalidEncoding, _input.Value);
                        }
                    }
                }
            }

            bool escapeUriAttributes = ParseYesNoAttribute(7, "escape-uri-attributes") != TriState.False;
            if (!escapeUriAttributes) ReportNYI("xsl:output/@escape-uri-attributes == flase()");

            bool includeContentType = ParseYesNoAttribute(8, "include-content-type") != TriState.False;
            if (!includeContentType) ReportNYI("xsl:output/@include-content-type == flase()");

            triState = ParseYesNoAttribute(9, "indent");
            if (triState != TriState.Unknown)
            {
                if (output.IndentPrec <= currentPrec)
                {
                    bool indent = (triState == TriState.True);
                    if (currentPrec == output.IndentPrec && settings.Indent != indent)
                    {
                        ReportWarning(/*[XT1560]*/SR.Xslt_AttributeRedefinition, "indent");
                    }
                    settings.Indent = indent;
                    output.IndentPrec = currentPrec;
                }
            }

            if (_input.MoveToXsltAttribute(10, "media-type"))
            {
                if (output.MediaTypePrec <= currentPrec)
                {
                    if (currentPrec == output.MediaTypePrec && settings.MediaType != _input.Value)
                    {
                        ReportWarning(/*[XT1560]*/SR.Xslt_AttributeRedefinition, "media-type");
                    }
                    settings.MediaType = _input.Value;
                    output.MediaTypePrec = currentPrec;
                }
            }

            if (_input.MoveToXsltAttribute(11, "normalization-form"))
            {
                ReportNYI("xsl:output/@normalization-form");
            }

            triState = ParseYesNoAttribute(12, "omit-xml-declaration");
            if (triState != TriState.Unknown)
            {
                if (output.OmitXmlDeclarationPrec <= currentPrec)
                {
                    bool omitXmlDeclaration = (triState == TriState.True);
                    if (currentPrec == output.OmitXmlDeclarationPrec && settings.OmitXmlDeclaration != omitXmlDeclaration)
                    {
                        ReportWarning(/*[XT1560]*/SR.Xslt_AttributeRedefinition, "omit-xml-declaration");
                    }
                    settings.OmitXmlDeclaration = omitXmlDeclaration;
                    output.OmitXmlDeclarationPrec = currentPrec;
                }
            }

            triState = ParseYesNoAttribute(13, "standalone");
            if (triState != TriState.Unknown)
            {
                if (output.StandalonePrec <= currentPrec)
                {
                    XmlStandalone standalone = (triState == TriState.True) ? XmlStandalone.Yes : XmlStandalone.No;
                    if (currentPrec == output.StandalonePrec && settings.Standalone != standalone)
                    {
                        ReportWarning(/*[XT1560]*/SR.Xslt_AttributeRedefinition, "standalone");
                    }
                    settings.Standalone = standalone;
                    output.StandalonePrec = currentPrec;
                }
            }

            bool undeclarePrefixes = ParseYesNoAttribute(14, "undeclare-prefixes") == TriState.True;
            if (undeclarePrefixes) ReportNYI("xsl:output/@undeclare-prefixes == true()");

            List<QilName> useCharacterMaps = ParseUseCharacterMaps(15);
            if (useCharacterMaps.Count != 0) ReportNYI("xsl:output/@use-character-maps");

            if (_input.MoveToXsltAttribute(16, "version"))
            {
                if (output.VersionPrec <= currentPrec)
                {
                    if (currentPrec == output.VersionPrec && output.Version != _input.Value)
                    {
                        ReportWarning(/*[XT1560]*/SR.Xslt_AttributeRedefinition, "version");
                    }
                    // BUGBUG: Check that version is a valid nmtoken
                    // ignore version since we support only one version for both xml (1.0) and html (4.0)
                    output.Version = _input.Value;
                    output.VersionPrec = currentPrec;
                }
            }

            CheckNoContent();
        }

        /*
            Default values for method="xml" :   version="1.0"   indent="no"     media-type="text/xml"
            Default values for method="html":   version="4.0"   indent="yes"    media-type="text/html"
            Default values for method="text":                                   media-type="text/plain"
        */
        private void ProcessOutputSettings()
        {
            Output output = _compiler.Output;
            XmlWriterSettings settings = output.Settings;

            // version is ignored, indent="no" by default
            if (settings.OutputMethod == XmlOutputMethod.Html && output.IndentPrec == Output.NeverDeclaredPrec)
            {
                settings.Indent = true;
            }
            if (output.MediaTypePrec == Output.NeverDeclaredPrec)
            {
                settings.MediaType =
                    settings.OutputMethod == XmlOutputMethod.Xml ? "text/xml" :
                    settings.OutputMethod == XmlOutputMethod.Html ? "text/html" :
                    settings.OutputMethod == XmlOutputMethod.Text ? "text/plain" : null;
            }
        }

        private void CheckUseAttrubuteSetInList(IList<XslNode> list)
        {
            foreach (XslNode xslNode in list)
            {
                switch (xslNode.NodeType)
                {
                    case XslNodeType.UseAttributeSet:
                        AttributeSet? usedAttSet;
                        if (_compiler.AttributeSets.TryGetValue(xslNode.Name!, out usedAttSet))
                        {
                            CheckAttributeSetsDfs(usedAttSet);
                        }
                        else
                        {
                            // The error will be reported in QilGenerator while compiling this attribute set.
                        }
                        break;
                    case XslNodeType.List:
                        CheckUseAttrubuteSetInList(xslNode.Content);
                        break;
                }
            }
        }

        private void CheckAttributeSetsDfs(AttributeSet attSet)
        {
            Debug.Assert(attSet != null);
            switch (attSet.CycleCheck)
            {
                case CycleCheck.NotStarted:
                    attSet.CycleCheck = CycleCheck.Processing;
                    CheckUseAttrubuteSetInList(attSet.Content);
                    attSet.CycleCheck = CycleCheck.Completed;
                    break;
                case CycleCheck.Completed:
                    break;
                default:
                    Debug.Assert(attSet.CycleCheck == CycleCheck.Processing);
                    Debug.Assert(attSet.Content[0].SourceLine != null);
                    _compiler.ReportError(/*[XT0720]*/attSet.Content[0].SourceLine!, SR.Xslt_CircularAttributeSet, attSet.Name!.QualifiedName);
                    break;
            }
        }

        private readonly XsltAttribute[] _keyAttributes = {
            new XsltAttribute("name",      V1Req | V2Req),
            new XsltAttribute("match",     V1Req | V2Req),
            new XsltAttribute("use",       V1Req | V2Opt),
            new XsltAttribute("collation", V2Opt)
        };
        private void LoadKey(NsDecl? stylesheetNsList)
        {
            ContextInfo ctxInfo = _input.GetAttributes(_keyAttributes);
            ctxInfo.nsList = MergeNamespaces(ctxInfo.nsList, stylesheetNsList);

            QilName keyName = ParseQNameAttribute(0)!;
            string? match = ParseStringAttribute(1, "match");
            string? use = ParseStringAttribute(2, "use");
            ParseCollationAttribute(3);

            _input.MoveToElement();

            List<XslNode>? content;

            if (V1)
            {
                if (use == null)
                {
                    _input.SkipNode();
                }
                else
                {
                    CheckNoContent();
                }
            }
            else
            {
                content = LoadInstructions();
                // Load the end tag only if the content is not empty
                if (content.Count != 0)
                {
                    content = LoadEndTag(content);
                }
                if ((use == null) == (content.Count == 0))
                {
                    ReportError(/*[XTSE1205]*/SR.Xslt_KeyCntUse);
                }
                else
                {
                    if (use == null) ReportNYI("xsl:key[count(@use) = 0]");
                }
            }

            Key key = (Key)SetInfo(F.Key(keyName, match, use, _input.XslVersion), null, ctxInfo);

            if (_compiler.Keys.Contains(keyName))
            {
                // Add to the list of previous definitions
                _compiler.Keys[keyName].Add(key);
            }
            else
            {
                // First definition of key with that name
                List<Key> defList = new List<Key>();
                defList.Add(key);
                _compiler.Keys.Add(defList);
            }
        }

        private readonly XsltAttribute[] _decimalFormatAttributes = {
            new XsltAttribute("name",               V1Opt | V2Opt),
            new XsltAttribute("infinity",           V1Opt | V2Opt),
            new XsltAttribute("NaN",                V1Opt | V2Opt),
            new XsltAttribute("decimal-separator",  V1Opt | V2Opt),
            new XsltAttribute("grouping-separator", V1Opt | V2Opt),
            new XsltAttribute("percent",            V1Opt | V2Opt),
            new XsltAttribute("per-mille",          V1Opt | V2Opt),
            new XsltAttribute("zero-digit",         V1Opt | V2Opt),
            new XsltAttribute("digit",              V1Opt | V2Opt),
            new XsltAttribute("pattern-separator",  V1Opt | V2Opt),
            new XsltAttribute("minus-sign",         V1Opt | V2Opt)
        };
        private void LoadDecimalFormat(NsDecl? stylesheetNsList)
        {
            const int NumCharAttrs = 8, NumSignAttrs = 7;
            ContextInfo ctxInfo = _input.GetAttributes(_decimalFormatAttributes);
            ctxInfo.nsList = MergeNamespaces(ctxInfo.nsList, stylesheetNsList);

            XmlQualifiedName name;
            if (_input.MoveToXsltAttribute(0, "name"))
            {
                _compiler.EnterForwardsCompatible();
                name = ResolveQName(/*ignoreDefaultNs:*/true, _input.Value);
                if (!_compiler.ExitForwardsCompatible(_input.ForwardCompatibility))
                {
                    name = new XmlQualifiedName();
                }
            }
            else
            {
                // Use name="" for the default decimal-format
                name = new XmlQualifiedName();
            }

            string infinity = DecimalFormatDecl.Default.InfinitySymbol;
            if (_input.MoveToXsltAttribute(1, "infinity"))
            {
                infinity = _input.Value;
            }

            string nan = DecimalFormatDecl.Default.NanSymbol;
            if (_input.MoveToXsltAttribute(2, "NaN"))
            {
                nan = _input.Value;
            }

            char[] DefaultValues = DecimalFormatDecl.Default.Characters;
            Span<char> characters = stackalloc char[NumCharAttrs];
            Debug.Assert(NumCharAttrs == DefaultValues.Length);

            for (int idx = 0; idx < NumCharAttrs; idx++)
            {
                characters[idx] = ParseCharAttribute(3 + idx, _decimalFormatAttributes[3 + idx].name, DefaultValues[idx]);
            }

            // Check all NumSignAttrs signs are distinct
            for (int i = 0; i < NumSignAttrs; i++)
            {
                for (int j = i + 1; j < NumSignAttrs; j++)
                {
                    if (characters[i] == characters[j])
                    {
                        // Try move to second attribute and if it is missing to first.
                        bool dummy = _input.MoveToXsltAttribute(3 + j, _decimalFormatAttributes[3 + j].name) || _input.MoveToXsltAttribute(3 + i, _decimalFormatAttributes[3 + i].name);
                        Debug.Assert(dummy, "One of the attrs should have lineInfo. If both are default they can't conflict.");
                        ReportError(/*[XT1300]*/SR.Xslt_DecimalFormatSignsNotDistinct, _decimalFormatAttributes[3 + i].name, _decimalFormatAttributes[3 + j].name);
                        break;
                    }
                }
            }

            if (_compiler.DecimalFormats.Contains(name))
            {
                // Check all attributes have the same values
                DecimalFormatDecl format = _compiler.DecimalFormats[name];
                _input.MoveToXsltAttribute(1, "infinity");
                CheckError(infinity != format.InfinitySymbol, /*[XT1290]*/SR.Xslt_DecimalFormatRedefined, "infinity", infinity);
                _input.MoveToXsltAttribute(2, "NaN");
                CheckError(nan != format.NanSymbol, /*[XT1290]*/SR.Xslt_DecimalFormatRedefined, "NaN", nan);
                for (int idx = 0; idx < NumCharAttrs; idx++)
                {
                    _input.MoveToXsltAttribute(3 + idx, _decimalFormatAttributes[3 + idx].name);
                    CheckError(characters[idx] != format.Characters[idx], /*[XT1290]*/SR.Xslt_DecimalFormatRedefined, _decimalFormatAttributes[3 + idx].name, char.ToString(characters[idx]));
                }
                Debug.Assert(name.Equals(format.Name));
            }
            else
            {
                // Add format to the global collection
                DecimalFormatDecl format = new DecimalFormatDecl(name, infinity, nan, new string(characters));
                _compiler.DecimalFormats.Add(format);
            }
            CheckNoContent();
        }

        private readonly XsltAttribute[] _namespaceAliasAttributes = {
            new XsltAttribute("stylesheet-prefix", V1Req | V2Req),
            new XsltAttribute("result-prefix",     V1Req | V2Req)
        };
        private void LoadNamespaceAlias(NsDecl? stylesheetNsList)
        {
            ContextInfo ctxInfo = _input.GetAttributes(_namespaceAliasAttributes);
            ctxInfo.nsList = MergeNamespaces(ctxInfo.nsList, stylesheetNsList);

            string? stylesheetNsUri = null;
            string? resultPrefix = null;
            string? resultNsUri = null;

            if (_input.MoveToXsltAttribute(0, "stylesheet-prefix"))
            {
                if (_input.Value.Length == 0)
                {
                    ReportError(/*[XT_005]*/SR.Xslt_EmptyNsAlias, "stylesheet-prefix");
                }
                else
                {
                    stylesheetNsUri = _input.LookupXmlNamespace(_input.Value == "#default" ? string.Empty : _input.Value);
                }
            }

            if (_input.MoveToXsltAttribute(1, "result-prefix"))
            {
                if (_input.Value.Length == 0)
                {
                    ReportError(/*[XT_005]*/SR.Xslt_EmptyNsAlias, "result-prefix");
                }
                else
                {
                    resultPrefix = _input.Value == "#default" ? string.Empty : _input.Value;
                    resultNsUri = _input.LookupXmlNamespace(resultPrefix);
                }
            }

            CheckNoContent();

            if (stylesheetNsUri == null || resultNsUri == null)
            {
                // At least one of attributes is missing or invalid
                return;
            }
            if (_compiler.SetNsAlias(stylesheetNsUri, resultNsUri, resultPrefix, _curStylesheet!.ImportPrecedence))
            {
                // Namespace alias redefinition
                _input.MoveToElement();
                ReportWarning(/*[XT0810]*/SR.Xslt_DupNsAlias, stylesheetNsUri);
            }
        }

        private readonly XsltAttribute[] _attributeSetAttributes = {
            new XsltAttribute("name",               V1Req | V2Req),
            new XsltAttribute("use-attribute-sets", V1Opt | V2Opt)
        };
        private void LoadAttributeSet(NsDecl? stylesheetNsList)
        {
            ContextInfo ctxInfo = _input.GetAttributes(_attributeSetAttributes);
            ctxInfo.nsList = MergeNamespaces(ctxInfo.nsList, stylesheetNsList);

            QilName? setName = ParseQNameAttribute(0);
            Debug.Assert(setName != null, "Required attribute always != null");

            AttributeSet? set;
            if (!_curStylesheet!.AttributeSets!.TryGetValue(setName, out set))
            {
                set = F.AttributeSet(setName);
                // First definition for setName within this stylesheet
                _curStylesheet.AttributeSets[setName] = set;
                if (!_compiler.AttributeSets.ContainsKey(setName))
                {
                    // First definition for setName overall, adding it to the list here
                    // to ensure stable order of prototemplate functions in QilExpression
                    _compiler.AllTemplates.Add(set);
                }
            }

            List<XslNode> content = new List<XslNode>();
            if (_input.MoveToXsltAttribute(1, "use-attribute-sets"))
            {
                AddUseAttributeSets(content);
            }

            QName parentName = _input.ElementName;
            if (_input.MoveToFirstChild())
            {
                do
                {
                    switch (_input.NodeType)
                    {
                        case XmlNodeType.Element:
                            // Only xsl:attribute's are allowed here
                            if (_input.IsXsltKeyword(_atoms.Attribute))
                            {
                                AddInstruction(content, XslAttribute());
                            }
                            else
                            {
                                ReportError(/*[XT_006]*/SR.Xslt_UnexpectedElement, _input.QualifiedName, parentName);
                                _input.SkipNode();
                            }
                            break;
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                            break;
                        default:
                            Debug.Assert(_input.NodeType == XmlNodeType.Text);
                            ReportError(/*[XT_006]*/SR.Xslt_TextNodesNotAllowed, parentName);
                            break;
                    }
                } while (_input.MoveToNextSibling());
            }
            set.AddContent(SetInfo(F.List(), LoadEndTag(content), ctxInfo));
        }

        private void LoadGlobalVariableOrParameter(NsDecl? stylesheetNsList)
        {
            Debug.Assert(_curTemplate == null);
            Debug.Assert(_input.CanHaveApplyImports == false);
            VarPar var = XslVarPar();
            // Preserving namespaces to parse content later
            var.Namespaces = MergeNamespaces(var.Namespaces, stylesheetNsList);
            CheckError(!_curStylesheet!.AddVarPar(var), /*[XT0630]*/SR.Xslt_DupGlobalVariable, var.Name!.QualifiedName);
        }

        //: http://www.w3.org/TR/xslt#section-Defining-Template-Rules
        private readonly XsltAttribute[] _templateAttributes = {
            new XsltAttribute("match",    V1Opt | V2Opt),
            new XsltAttribute("name",     V1Opt | V2Opt),
            new XsltAttribute("priority", V1Opt | V2Opt),
            new XsltAttribute("mode",     V1Opt | V2Opt),
            new XsltAttribute("as",       V2Opt)
        };
        private void LoadTemplate(NsDecl? stylesheetNsList)
        {
            Debug.Assert(_curTemplate == null);
            ContextInfo ctxInfo = _input.GetAttributes(_templateAttributes);
            ctxInfo.nsList = MergeNamespaces(ctxInfo.nsList, stylesheetNsList);

            string? match = ParseStringAttribute(0, "match");
            QilName? name = ParseQNameAttribute(1);
            double priority = double.NaN;
            if (_input.MoveToXsltAttribute(2, "priority"))
            {
                priority = XPathConvert.StringToDouble(_input.Value);
                if (double.IsNaN(priority) && !_input.ForwardCompatibility)
                {
                    ReportError(/*[XT0530]*/SR.Xslt_InvalidAttrValue, "priority", _input.Value);
                }
            }
            QilName mode = V1 ? ParseModeAttribute(3) : ParseModeListAttribute(3);

            if (match == null)
            {
                CheckError(!_input.AttributeExists(1, "name"), /*[XT_007]*/SR.Xslt_BothMatchNameAbsent);
                CheckError(_input.AttributeExists(3, "mode"), /*[XT_008]*/SR.Xslt_ModeWithoutMatch);
                mode = nullMode;
                if (_input.AttributeExists(2, "priority"))
                {
                    if (V1)
                    {
                        ReportWarning(/*[XT_008]*/SR.Xslt_PriorityWithoutMatch);
                    }
                    else
                    {
                        ReportError(/*[XT_008]*/SR.Xslt_PriorityWithoutMatch);
                    }
                }
            }

            if (_input.MoveToXsltAttribute(4, "as"))
            {
                ReportNYI("xsl:template/@as");
            }

            _curTemplate = F.Template(name, match, mode, priority, _input.XslVersion);

            // Template without match considered to not have mode and can't call xsl:apply-imports
            _input.CanHaveApplyImports = (match != null);

            SetInfo(_curTemplate,
                LoadEndTag(LoadInstructions(InstructionFlags.AllowParam)), ctxInfo
            );

            if (!_curStylesheet!.AddTemplate(_curTemplate))
            {
                ReportError(/*[XT0660]*/SR.Xslt_DupTemplateName, _curTemplate.Name!.QualifiedName);
            }
            _curTemplate = null;
        }

#if XSLT2
        //: http://www.w3.org/TR/xslt20/#element-character-map
        XsltAttribute[] characterMapAttributes = {
            new XsltAttribute("name"            , V2Req),
            new XsltAttribute("use-character-maps", V2Opt)
        };
        XsltAttribute[] outputCharacterAttributes = {
            new XsltAttribute("character", V2Req),
            new XsltAttribute("string"   , V2Req)
        };
        private void LoadCharacterMap(NsDecl stylesheetNsList) {
            ContextInfo ctxInfo = input.GetAttributes(characterMapAttributes);
            ctxInfo.nsList = MergeNamespaces(ctxInfo.nsList, stylesheetNsList);

            QilName name = ParseQNameAttribute(0);
            List<QilName> useCharacterMaps = ParseUseCharacterMaps(1);

            ReportNYI("xsl:character-map");

            QName parentName = input.ElementName;
            if (input.MoveToFirstChild()) {
                do {
                    switch (input.NodeType) {
                    case XmlNodeType.Element:
                        // Only xsl:output-character are allowed here
                        if (input.IsXsltKeyword(atoms.OutputCharacter)) {
                            input.GetAttributes(outputCharacterAttributes);
                            ReportNYI("xsl:output-character");
                            char ch  = ParseCharAttribute(0, "character", /*defVal:*/(char)0);
                            string s = ParseStringAttribute(1, "string");
                            CheckNoContent();
                        } else {
                            ReportError(/*[XT_006]*/SR.Xslt_UnexpectedElement, input.QualifiedName, parentName);
                            input.SkipNode();
                        }
                        break;
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        break;
                    default:
                        Debug.Assert(input.NodeType == XmlNodeType.Text);
                        ReportError(/*[XT_006]*/SR.Xslt_TextNodesNotAllowed, parentName);
                        break;
                    }
                } while (input.MoveToNextSibling());
            }
        }

        //: http://www.w3.org/TR/xslt20/#stylesheet-functions
        XsltAttribute[] functionAttributes = {
            new XsltAttribute("name"    , V2Req),
            new XsltAttribute("as"      , V2Opt),
            new XsltAttribute("override", V2Opt)
        };
        private void LoadFunction(NsDecl stylesheetNsList) {
            Debug.Assert(curTemplate == null);
            ContextInfo ctxInfo = input.GetAttributes(functionAttributes);
            ctxInfo.nsList = MergeNamespaces(ctxInfo.nsList, stylesheetNsList);

            QilName name  = ParseQNameAttribute(0);
            string asType = ParseStringAttribute(1, "as");
            bool over     = ParseYesNoAttribute(2, "override") == TriState.True;

            ReportNYI("xsl:function");

            Debug.Assert(input.CanHaveApplyImports == false);

            curFunction = new Object();
            LoadInstructions(InstructionFlags.AllowParam);
            curFunction = null;
        }

        //: http://www.w3.org/TR/xslt20/#element-import-schema
        XsltAttribute[] importSchemaAttributes = {
            new XsltAttribute("namespace"     , V2Opt),
            new XsltAttribute("schema-location", V2Opt)
        };
        private void LoadImportSchema() {
            ContextInfo ctxInfo = input.GetAttributes(importSchemaAttributes);
            ReportError(/*[XTSE1650]*/SR.Xslt_SchemaDeclaration, input.ElementName);
            input.SkipNode();
        }
#endif

        private readonly XsltAttribute[] _scriptAttributes = {
            new XsltAttribute("implements-prefix", V1Req | V2Req),
            new XsltAttribute("language",          V1Opt | V2Opt)
        };
        private void LoadMsScript(NsDecl? stylesheetNsList)
        {
            ContextInfo ctxInfo = _input.GetAttributes(_scriptAttributes);
            ctxInfo.nsList = MergeNamespaces(ctxInfo.nsList, stylesheetNsList);


            string? scriptNs = null;
            if (_input.MoveToXsltAttribute(0, "implements-prefix"))
            {
                if (_input.Value.Length == 0)
                {
                    ReportError(/*[XT_009]*/SR.Xslt_EmptyAttrValue, "implements-prefix", _input.Value);
                }
                else
                {
                    scriptNs = _input.LookupXmlNamespace(_input.Value);
                    if (scriptNs == XmlReservedNs.NsXslt)
                    {
                        ReportError(/*[XT_036]*/SR.Xslt_ScriptXsltNamespace);
                        scriptNs = null;
                    }
                }
            }

            scriptNs ??= _compiler.CreatePhantomNamespace();
            ParseStringAttribute(1, "language");

            if (!_compiler.Settings.EnableScript)
            {
                _compiler.Scripts.ScriptClasses[scriptNs] = null;
                _input.SkipNode();
                return;
            }

            throw new PlatformNotSupportedException(SR.CompilingScriptsNotSupported); // Not adding any scripts as script compilation is not available
        }

        private readonly XsltAttribute[] _assemblyAttributes = {
            new XsltAttribute("name", V1Opt | V2Opt),
            new XsltAttribute("href", V1Opt | V2Opt)
        };

        private readonly XsltAttribute[] _usingAttributes = {
            new XsltAttribute("namespace", V1Req | V2Req)
        };

        // ----------------- Template level methods --------------------------
        // Each instruction in AST tree has nsdecl list attuched to it.
        // Load*() methods do this treek. Xsl*() methods rely on LoadOneInstruction() to do this.

        private enum InstructionFlags
        {
            None = 0x00,
            AllowParam = 0x01,
            AllowSort = 0x02,
            AllowFallback = 0x04,
        }

        private List<XslNode> LoadInstructions()
        {
            return LoadInstructions(new List<XslNode>(), InstructionFlags.None);
        }

        private List<XslNode> LoadInstructions(InstructionFlags flags)
        {
            return LoadInstructions(new List<XslNode>(), flags);
        }

        private List<XslNode> LoadInstructions(List<XslNode> content)
        {
            return LoadInstructions(content, InstructionFlags.None);
        }

        private const int MAX_LOADINSTRUCTIONS_DEPTH = 1024;
        private int _loadInstructionsDepth;
        private List<XslNode> LoadInstructions(List<XslNode> content, InstructionFlags flags)
        {
            if (++_loadInstructionsDepth > MAX_LOADINSTRUCTIONS_DEPTH)
            {
                if (LocalAppContextSwitches.LimitXPathComplexity)
                {
                    throw XslLoadException.Create(SR.Xslt_InputTooComplex);
                }
            }
            QName parentName = _input.ElementName;
            if (_input.MoveToFirstChild())
            {
                bool atTop = true;
                int sortNumber = 0;
                XslNode? result;

                do
                {
                    switch (_input.NodeType)
                    {
                        case XmlNodeType.Element:
                            string nspace = _input.NamespaceUri;
                            string name = _input.LocalName;
                            if (nspace == _atoms.UriXsl)
                            {
                                InstructionFlags instrFlag = (
                                    Ref.Equal(name, _atoms.Param) ? InstructionFlags.AllowParam :
                                    Ref.Equal(name, _atoms.Sort) ? InstructionFlags.AllowSort :
                                    /*else */ InstructionFlags.None
                                );
                                if (instrFlag != InstructionFlags.None)
                                {
                                    string? error = (
                                        (flags & instrFlag) == 0 ? /*[XT_013]*/SR.Xslt_UnexpectedElement :
                                        !atTop ? /*[XT_014]*/SR.Xslt_NotAtTop :
                                        /*else*/ null
                                    );
                                    if (error != null)
                                    {
                                        ReportError(error, _input.QualifiedName, parentName);
                                        atTop = false;
                                        _input.SkipNode();
                                        continue;
                                    }
                                }
                                else
                                {
                                    atTop = false;
                                }
                                result = (
                                    Ref.Equal(name, _atoms.ApplyImports) ? XslApplyImports() :
                                    Ref.Equal(name, _atoms.ApplyTemplates) ? XslApplyTemplates() :
                                    Ref.Equal(name, _atoms.CallTemplate) ? XslCallTemplate() :
                                    Ref.Equal(name, _atoms.Copy) ? XslCopy() :
                                    Ref.Equal(name, _atoms.CopyOf) ? XslCopyOf() :
                                    Ref.Equal(name, _atoms.Fallback) ? XslFallback() :
                                    Ref.Equal(name, _atoms.If) ? XslIf() :
                                    Ref.Equal(name, _atoms.Choose) ? XslChoose() :
                                    Ref.Equal(name, _atoms.ForEach) ? XslForEach() :
                                    Ref.Equal(name, _atoms.Message) ? XslMessage() :
                                    Ref.Equal(name, _atoms.Number) ? XslNumber() :
                                    Ref.Equal(name, _atoms.ValueOf) ? XslValueOf() :
                                    Ref.Equal(name, _atoms.Comment) ? XslComment() :
                                    Ref.Equal(name, _atoms.ProcessingInstruction) ? XslProcessingInstruction() :
                                    Ref.Equal(name, _atoms.Text) ? XslText() :
                                    Ref.Equal(name, _atoms.Element) ? XslElement() :
                                    Ref.Equal(name, _atoms.Attribute) ? XslAttribute() :
                                    Ref.Equal(name, _atoms.Variable) ? XslVarPar() :
                                    Ref.Equal(name, _atoms.Param) ? XslVarPar() :
                                    Ref.Equal(name, _atoms.Sort) ? XslSort(sortNumber++) :
#if XSLT2
                                V2 && Ref.Equal(name, atoms.AnalyzeString  ) ? XslAnalyzeString() :
                                V2 && Ref.Equal(name, "namespace"      ) ? XslNamespace() :
                                V2 && Ref.Equal(name, atoms.PerformSort    ) ? XslPerformSort() :
                                V2 && Ref.Equal(name, atoms.Document       ) ? XslDocument() :
                                V2 && Ref.Equal(name, atoms.ForEachGroup   ) ? XslForEachGroup() :
                                V2 && Ref.Equal(name, atoms.NextMatch      ) ? XslNextMatch() :
                                V2 && Ref.Equal(name, atoms.Sequence       ) ? XslSequence() :
                                V2 && Ref.Equal(name, atoms.ResultDocument ) ? XslResultDocument() :
#endif
                                    /*default:*/                                   LoadUnknownXsltInstruction(parentName)
                                );
                            }
                            else
                            {
                                atTop = false;
                                result = LoadLiteralResultElement(/*asStylesheet:*/false);
                            }
                            break;
                        case XmlNodeType.SignificantWhitespace:
                            result = SetLineInfo(F.Text(_input.Value), _input.BuildLineInfo());
                            break;
                        case XmlNodeType.Whitespace:
                            continue;
                        default:
                            Debug.Assert(_input.NodeType == XmlNodeType.Text);
                            atTop = false;
                            goto case XmlNodeType.SignificantWhitespace;
                    }
                    AddInstruction(content, result);
                } while (_input.MoveToNextSibling());
            }
            --_loadInstructionsDepth;
            return content;
        }

        private List<XslNode> LoadWithParams(InstructionFlags flags)
        {
            QName parentName = _input.ElementName;
            List<XslNode> content = new List<XslNode>();
            /* Process children */
            if (_input.MoveToFirstChild())
            {
                int sortNumber = 0;
                do
                {
                    switch (_input.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (_input.IsXsltKeyword(_atoms.WithParam))
                            {
                                XslNode withParam = XslVarPar();
                                CheckWithParam(content, withParam);
                                AddInstruction(content, withParam);
                            }
                            else if (flags == InstructionFlags.AllowSort && _input.IsXsltKeyword(_atoms.Sort))
                            {
                                AddInstruction(content, XslSort(sortNumber++));
                            }
                            else if (flags == InstructionFlags.AllowFallback && _input.IsXsltKeyword(_atoms.Fallback))
                            {
                                XslFallback();
                            }
                            else
                            {
                                ReportError(/*[XT_016]*/SR.Xslt_UnexpectedElement, _input.QualifiedName, parentName);
                                _input.SkipNode();
                            }
                            break;
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                            break;
                        default:
                            Debug.Assert(_input.NodeType == XmlNodeType.Text);
                            ReportError(/*[XT_016]*/SR.Xslt_TextNodesNotAllowed, parentName);
                            break;
                    }
                } while (_input.MoveToNextSibling());
            }
            return content;
        }

        // http://www.w3.org/TR/xslt#apply-imports
        private XslNode? XslApplyImports()
        {
            ContextInfo ctxInfo = _input.GetAttributes();
            if (!_input.CanHaveApplyImports)
            {
                ReportError(/*[XT_015]*/SR.Xslt_InvalidApplyImports);
                _input.SkipNode();
                return null;
            }

            List<XslNode>? content = LoadWithParams(InstructionFlags.None);

            ctxInfo.SaveExtendedLineInfo(_input);

            if (V1)
            {
                if (content.Count != 0)
                {
                    ISourceLineInfo contentInfo = content[0].SourceLine!;
                    if (!_input.ForwardCompatibility)
                    {
                        _compiler.ReportError(contentInfo, /*[XT0260]*/SR.Xslt_NotEmptyContents, _atoms.ApplyImports);
                    }
                    else
                    {
                        return SetInfo(F.Error(XslLoadException.CreateMessage(contentInfo, /*[XT0260]*/SR.Xslt_NotEmptyContents, _atoms.ApplyImports)), null, ctxInfo);
                    }
                }
                content = null;
            }
            else
            {
                if (content.Count != 0) ReportNYI("xsl:apply-imports/xsl:with-param");
                content = null;
            }

            return SetInfo(F.ApplyImports(/*Mode:*/_curTemplate!.Mode, _curStylesheet, _input.XslVersion), content, ctxInfo);
        }

        // http://www.w3.org/TR/xslt#section-Applying-Template-Rules
        private readonly XsltAttribute[] _applyTemplatesAttributes = {
            new XsltAttribute("select", V1Opt | V2Opt),
            new XsltAttribute("mode",   V1Opt | V2Opt)
        };
        private XslNode XslApplyTemplates()
        {
            ContextInfo ctxInfo = _input.GetAttributes(_applyTemplatesAttributes);

            string select = ParseStringAttribute(0, "select") ?? "node()";
            QilName mode = ParseModeAttribute(1);

            List<XslNode> content = LoadWithParams(InstructionFlags.AllowSort);
            ctxInfo.SaveExtendedLineInfo(_input);
            return SetInfo(F.ApplyTemplates(mode, select, ctxInfo, _input.XslVersion),
                content, ctxInfo
            );
        }

        // http://www.w3.org/TR/xslt#named-templates
        // http://www.w3.org/TR/xslt#element-call-template
        private readonly XsltAttribute[] _callTemplateAttributes = {
            new XsltAttribute("name", V1Req | V2Req)
        };
        private XslNode XslCallTemplate()
        {
            ContextInfo ctxInfo = _input.GetAttributes(_callTemplateAttributes);
            QilName? name = ParseQNameAttribute(0);

            List<XslNode> content = LoadWithParams(InstructionFlags.None);
            ctxInfo.SaveExtendedLineInfo(_input);
            return SetInfo(F.CallTemplate(name, ctxInfo), content, ctxInfo);
        }

        // http://www.w3.org/TR/xslt#copying
        // http://www.w3.org/TR/xslt20/#element-copy
        private readonly XsltAttribute[] _copyAttributes = {
            new XsltAttribute("copy-namespaces",    V2Opt),
            new XsltAttribute("inherit-namespaces", V2Opt),
            new XsltAttribute("use-attribute-sets", V1Opt | V2Opt),
            new XsltAttribute("type",               V2Opt),
            new XsltAttribute("validation",         V2Opt)
        };
        private XslNode XslCopy()
        {
            ContextInfo ctxInfo = _input.GetAttributes(_copyAttributes);

            bool copyNamespaces = ParseYesNoAttribute(0, "copy-namespaces") != TriState.False;
            bool inheritNamespaces = ParseYesNoAttribute(1, "inherit-namespaces") != TriState.False;
            if (!copyNamespaces) ReportNYI("xsl:copy[@copy-namespaces    = 'no']");
            if (!inheritNamespaces) ReportNYI("xsl:copy[@inherit-namespaces = 'no']");

            List<XslNode> content = new List<XslNode>();
            if (_input.MoveToXsltAttribute(2, "use-attribute-sets"))
            {
                AddUseAttributeSets(content);
            }

            ParseTypeAttribute(3);
            ParseValidationAttribute(4, /*defVal:*/false);

            return SetInfo(F.Copy(), LoadEndTag(LoadInstructions(content)), ctxInfo);
        }

        private readonly XsltAttribute[] _copyOfAttributes = {
            new XsltAttribute("select",          V1Req | V2Req),
            new XsltAttribute("copy-namespaces", V2Opt),
            new XsltAttribute("type",            V2Opt),
            new XsltAttribute("validation",      V2Opt)
        };
        private XslNode XslCopyOf()
        {
            ContextInfo ctxInfo = _input.GetAttributes(_copyOfAttributes);
            string? select = ParseStringAttribute(0, "select");
            bool copyNamespaces = ParseYesNoAttribute(1, "copy-namespaces") != TriState.False;
            if (!copyNamespaces) ReportNYI("xsl:copy-of[@copy-namespaces    = 'no']");

            ParseTypeAttribute(2);
            ParseValidationAttribute(3, /*defVal:*/false);

            CheckNoContent();
            return SetInfo(F.CopyOf(select, _input.XslVersion), null, ctxInfo);
        }

        // http://www.w3.org/TR/xslt#fallback
        // See LoadFallbacks() for real fallback implementation
        private XslNode? XslFallback()
        {
            _input.GetAttributes();
            _input.SkipNode();
            return null;
        }

        private readonly XsltAttribute[] _ifAttributes = {
            new XsltAttribute("test", V1Req | V2Req)
        };
        private XslNode XslIf()
        {
            ContextInfo ctxInfo = _input.GetAttributes(_ifAttributes);
            string? test = ParseStringAttribute(0, "test");

            return SetInfo(F.If(test, _input.XslVersion), LoadInstructions(), ctxInfo);
        }

        private XslNode XslChoose()
        {
            ContextInfo ctxInfo = _input.GetAttributes();

            List<XslNode> content = new List<XslNode>();
            bool otherwise = false;
            bool when = false;

            QName parentName = _input.ElementName;
            if (_input.MoveToFirstChild())
            {
                do
                {
                    switch (_input.NodeType)
                    {
                        case XmlNodeType.Element:
                            XslNode? node = null;
                            if (Ref.Equal(_input.NamespaceUri, _atoms.UriXsl))
                            {
                                if (Ref.Equal(_input.LocalName, _atoms.When))
                                {
                                    if (otherwise)
                                    {
                                        ReportError(/*[XT_018]*/SR.Xslt_WhenAfterOtherwise);
                                        _input.SkipNode();
                                        continue;
                                    }
                                    else
                                    {
                                        when = true;
                                        node = XslIf();
                                    }
                                }
                                else if (Ref.Equal(_input.LocalName, _atoms.Otherwise))
                                {
                                    if (otherwise)
                                    {
                                        ReportError(/*[XT_019]*/SR.Xslt_DupOtherwise);
                                        _input.SkipNode();
                                        continue;
                                    }
                                    else
                                    {
                                        otherwise = true;
                                        node = XslOtherwise();
                                    }
                                }
                            }
                            if (node == null)
                            {
                                ReportError(/*[XT_020]*/SR.Xslt_UnexpectedElement, _input.QualifiedName, parentName);
                                _input.SkipNode();
                                continue;
                            }
                            AddInstruction(content, node);
                            break;
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                            break;
                        default:
                            Debug.Assert(_input.NodeType == XmlNodeType.Text);
                            ReportError(/*[XT_020]*/SR.Xslt_TextNodesNotAllowed, parentName);
                            break;
                    }
                } while (_input.MoveToNextSibling());
            }
            CheckError(!when, /*[XT_021]*/SR.Xslt_NoWhen);
            return SetInfo(F.Choose(), content, ctxInfo);
        }

        private XslNode XslOtherwise()
        {
            ContextInfo ctxInfo = _input.GetAttributes();
            return SetInfo(F.Otherwise(), LoadInstructions(), ctxInfo);
        }

        private readonly XsltAttribute[] _forEachAttributes = {
            new XsltAttribute("select", V1Req | V2Req)
        };
        private XslNode XslForEach()
        {
            ContextInfo ctxInfo = _input.GetAttributes(_forEachAttributes);

            string? select = ParseStringAttribute(0, "select");
            // The current template rule becomes null, so we must not allow xsl:apply-import's within this element
            _input.CanHaveApplyImports = false;
            List<XslNode> content = LoadInstructions(InstructionFlags.AllowSort);
            ctxInfo.SaveExtendedLineInfo(_input);

            return SetInfo(F.ForEach(select, ctxInfo, _input.XslVersion),
                content, ctxInfo
            );
        }

        // http://www.w3.org/TR/xslt#message
        // http://www.w3.org/TR/xslt20/#element-message
        private readonly XsltAttribute[] _messageAttributes = {
            new XsltAttribute("select",    V2Opt),
            new XsltAttribute("terminate", V1Opt | V2Opt)
        };
        private XslNode XslMessage()
        {
            ContextInfo ctxInfo = _input.GetAttributes(_messageAttributes);

            string? select = ParseStringAttribute(0, "select");
            bool terminate = ParseYesNoAttribute(1, /*attName:*/"terminate") == TriState.True;

            List<XslNode> content = LoadInstructions();
            if (content.Count != 0)
            {
                content = LoadEndTag(content);
            }
            if (select != null)
            {
                content.Insert(0, F.CopyOf(select, _input.XslVersion));
            }

            return SetInfo(F.Message(terminate), content, ctxInfo);
        }

        // http://www.w3.org/TR/xslt#number
        // http://www.w3.org/TR/xslt20/#element-number
        private readonly XsltAttribute[] _numberAttributes = {
            new XsltAttribute("value",              V1Opt | V2Opt),
            new XsltAttribute("select",             V2Opt),
            new XsltAttribute("level",              V1Opt | V2Opt),
            new XsltAttribute("count",              V1Opt | V2Opt),
            new XsltAttribute("from",               V1Opt | V2Opt),
            new XsltAttribute("format",             V1Opt | V2Opt),
            new XsltAttribute("lang",               V1Opt | V2Opt),
            new XsltAttribute("letter-value",       V1Opt | V2Opt),
            new XsltAttribute("ordinal",            V2Opt),
            new XsltAttribute("grouping-separator", V1Opt | V2Opt),
            new XsltAttribute("grouping-size",      V1Opt | V2Opt)
        };
        private XslNode XslNumber()
        {
            ContextInfo ctxInfo = _input.GetAttributes(_numberAttributes);

            string? value = ParseStringAttribute(0, "value");
            string? select = ParseStringAttribute(1, "select");
            if (select != null) ReportNYI("xsl:number/@select");
            NumberLevel level = NumberLevel.Single;
            if (_input.MoveToXsltAttribute(2, "level"))
            {
                switch (_input.Value)
                {
                    case "single": level = NumberLevel.Single; break;
                    case "multiple": level = NumberLevel.Multiple; break;
                    case "any": level = NumberLevel.Any; break;
                    default:
                        if (!_input.ForwardCompatibility)
                        {
                            ReportError(/*[XT_022]*/SR.Xslt_InvalidAttrValue, "level", _input.Value);
                        }
                        break;
                }
            }
            string? count = ParseStringAttribute(3, "count");
            string? from = ParseStringAttribute(4, "from");
            string? format = ParseStringAttribute(5, "format");
            string? lang = ParseStringAttribute(6, "lang");
            string? letterValue = ParseStringAttribute(7, "letter-value");
            string? ordinal = ParseStringAttribute(8, "ordinal");
            if (!string.IsNullOrEmpty(ordinal)) ReportNYI("xsl:number/@ordinal");
            string? groupingSeparator = ParseStringAttribute(9, "grouping-separator");
            string? groupingSize = ParseStringAttribute(10, "grouping-size");

            // Default values for xsl:number :  level="single"  format="1"
            format ??= "1";

            CheckNoContent();
            return SetInfo(
                F.Number(level, count, from, value,
                    format, lang, letterValue, groupingSeparator, groupingSize,
                    _input.XslVersion
                ),
                null, ctxInfo
            );
        }

        // http://www.w3.org/TR/xslt#value-of
        private readonly XsltAttribute[] _valueOfAttributes = {
            new XsltAttribute("select",                  V1Req | V2Opt),
            new XsltAttribute("separator",               V2Opt),
            new XsltAttribute("disable-output-escaping", V1Opt | V2Opt)
        };
        private XslNode XslValueOf()
        {
            ContextInfo ctxInfo = _input.GetAttributes(_valueOfAttributes);

            string? select = ParseStringAttribute(0, "select");
            string? separator = ParseStringAttribute(1, "separator");
            bool doe = ParseYesNoAttribute(2, /*attName:*/"disable-output-escaping") == TriState.True;

            if (separator != null)
            {
                ReportNYI("xsl:value-of/@separator");
            }

            List<XslNode>? content;

            if (V1)
            {
                if (select == null)
                {
                    _input.SkipNode();
                    return SetInfo(F.Error(XslLoadException.CreateMessage(ctxInfo.lineInfo, SR.Xslt_MissingAttribute, "select")), null, ctxInfo);
                }
                CheckNoContent();
            }
            else
            {
                content = LoadContent(select != null);
                CheckError(select == null && content.Count == 0, /*[???]*/SR.Xslt_NoSelectNoContent, _input.ElementName);
                if (content.Count != 0)
                {
                    ReportNYI("xsl:value-of/*");
                }
            }

            return SetInfo(F.XslNode(doe ? XslNodeType.ValueOfDoe : XslNodeType.ValueOf, null, select, _input.XslVersion),
                null, ctxInfo
            );
        }

        //                    required tunnel select
        // variable              -        -      +
        // with-param            -        +      +
        // stylesheet/param      +        -      +
        // template/param        +        +      +
        // function/param        -        -      -
        // xsl:variable     http://www.w3.org/TR/xslt#local-variables
        // xsl:param        http://www.w3.org/TR/xslt#element-param
        // xsl:with-param   http://www.w3.org/TR/xslt#element-with-param
        private readonly XsltAttribute[] _variableAttributes = {
            new XsltAttribute("name",     V1Req | V2Req),
            new XsltAttribute("select",   V1Opt | V2Opt),
            new XsltAttribute("as",       V2Opt),
            new XsltAttribute("required", 0),
            new XsltAttribute("tunnel",   0)
        };
        private readonly XsltAttribute[] _paramAttributes = {
            new XsltAttribute("name",     V1Req | V2Req),
            new XsltAttribute("select",   V1Opt | V2Opt),
            new XsltAttribute("as",       V2Opt),
            new XsltAttribute("required", V2Opt),
            new XsltAttribute("tunnel",   V2Opt)
        };
        private readonly XsltAttribute[] _withParamAttributes = {
            new XsltAttribute("name",    V1Req | V2Req),
            new XsltAttribute("select",  V1Opt | V2Opt),
            new XsltAttribute("as",      V2Opt),
            new XsltAttribute("required", 0),
            new XsltAttribute("tunnel",   V2Opt)
        };
        private VarPar XslVarPar()
        {
            string localName = _input.LocalName;
            XslNodeType nodeType = (
                Ref.Equal(localName, _atoms.Variable) ? XslNodeType.Variable :
                Ref.Equal(localName, _atoms.Param) ? XslNodeType.Param :
                Ref.Equal(localName, _atoms.WithParam) ? XslNodeType.WithParam :
                XslNodeType.Unknown
            );
            Debug.Assert(nodeType != XslNodeType.Unknown);
            ContextInfo ctxInfo = _input.GetAttributes(
                nodeType == XslNodeType.Variable ? _variableAttributes :
                nodeType == XslNodeType.Param ? _paramAttributes :
                /*default:*/                       _withParamAttributes
            );

            QilName? name = ParseQNameAttribute(0);
            string? select = ParseStringAttribute(1, "select");
            string? asType = ParseStringAttribute(2, "as");
            TriState required = ParseYesNoAttribute(3, "required");
            if (required == TriState.True) ReportNYI("xsl:param/@required == true()");

            if (asType != null)
            {
                ReportNYI("xsl:param/@as");
            }

            TriState tunnel = ParseYesNoAttribute(4, "tunnel");
            if (tunnel != TriState.Unknown)
            {
                if (nodeType == XslNodeType.Param && _curTemplate == null)
                {
                    if (!_input.ForwardCompatibility)
                    {
                        ReportError(/*[???]*/SR.Xslt_NonTemplateTunnel, name!.ToString());
                    }
                }
                else
                {
                    if (tunnel == TriState.True) ReportNYI("xsl:param/@tunnel == true()");
                }
            }

            List<XslNode> content = LoadContent(select != null);
            CheckError((required == TriState.True) && (select != null || content.Count != 0), /*[???]*/SR.Xslt_RequiredAndSelect, name!.ToString());

            VarPar result = F.VarPar(nodeType, name, select, _input.XslVersion);
            SetInfo(result, content, ctxInfo);
            return result;
        }

        // http://www.w3.org/TR/xslt#section-Creating-Comments
        // http://www.w3.org/TR/xslt20/#element-comment
        private readonly XsltAttribute[] _commentAttributes = {
            new XsltAttribute("select", V2Opt)
        };
        private XslNode XslComment()
        {
            ContextInfo ctxInfo = _input.GetAttributes(_commentAttributes);
            string? select = ParseStringAttribute(0, "select");
            if (select != null) ReportNYI("xsl:comment/@select");

            return SetInfo(F.Comment(), LoadContent(select != null), ctxInfo);
        }

        private List<XslNode> LoadContent(bool hasSelect)
        {
            QName parentName = _input.ElementName;
            List<XslNode> content = LoadInstructions();
            CheckError(hasSelect && content.Count != 0, /*[XT0620]*/SR.Xslt_ElementCntSel, parentName);
            // Load the end tag only if the content is not empty
            if (content.Count != 0)
            {
                content = LoadEndTag(content);
            }
            return content;
        }

        // http://www.w3.org/TR/xslt#section-Creating-Processing-Instructions
        // http://www.w3.org/TR/xslt20/#element-processing-instruction
        private readonly XsltAttribute[] _processingInstructionAttributes = {
            new XsltAttribute("name",   V1Req | V2Req),
            new XsltAttribute("select", V2Opt)
        };
        private XslNode XslProcessingInstruction()
        {
            ContextInfo ctxInfo = _input.GetAttributes(_processingInstructionAttributes);
            string name = ParseNCNameAttribute(0);
            string? select = ParseStringAttribute(1, "select");
            if (select != null) ReportNYI("xsl:processing-instruction/@select");

            return SetInfo(F.PI(name, _input.XslVersion), LoadContent(select != null), ctxInfo);
        }

        // http://www.w3.org/TR/xslt#section-Creating-Text
        private readonly XsltAttribute[] _textAttributes = {
            new XsltAttribute("disable-output-escaping", V1Opt | V2Opt)
        };
        private XslNode XslText()
        {
            ContextInfo ctxInfo = _input.GetAttributes(_textAttributes);

            bool doe = ParseYesNoAttribute(0, /*attName:*/ "disable-output-escaping") == TriState.True;
            SerializationHints hints = doe ? SerializationHints.DisableOutputEscaping : SerializationHints.None;

            // We are not using StringBuilder here because in most cases there will be just one text node.
            List<XslNode> content = new List<XslNode>();

            QName parentName = _input.ElementName;
            if (_input.MoveToFirstChild())
            {
                do
                {
                    switch (_input.NodeType)
                    {
                        case XmlNodeType.Text:
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                            // xsl:text may contain multiple child text nodes separated by comments and PIs, which are ignored by XsltInput
                            content.Add(F.Text(_input.Value, hints));
                            break;
                        default:
                            Debug.Assert(_input.NodeType == XmlNodeType.Element);
                            ReportError(/*[XT_023]*/SR.Xslt_UnexpectedElement, _input.QualifiedName, parentName);
                            _input.SkipNode();
                            break;
                    }
                } while (_input.MoveToNextSibling());
            }

            // Empty xsl:text elements will be ignored
            return SetInfo(F.List(), content, ctxInfo);
        }

        // http://www.w3.org/TR/xslt#section-Creating-Elements-with-xsl:element
        // http://www.w3.org/TR/xslt20/#element-element
        private readonly XsltAttribute[] _elementAttributes = {
            new XsltAttribute("name",               V1Req | V2Req),
            new XsltAttribute("namespace",          V1Opt | V2Opt),
            new XsltAttribute("inherit-namespaces", V2Opt),
            new XsltAttribute("use-attribute-sets", V1Opt | V2Opt),
            new XsltAttribute("type",               V2Opt),
            new XsltAttribute("validation",         V2Opt)
        };
        private XslNode XslElement()
        {
            ContextInfo ctxInfo = _input.GetAttributes(_elementAttributes);

            string name = ParseNCNameAttribute(0);
            string? ns = ParseStringAttribute(1, "namespace");
            CheckError(ns == XmlReservedNs.NsXmlNs, /*[XT_024]*/SR.Xslt_ReservedNS, ns);

            bool inheritNamespaces = ParseYesNoAttribute(2, "inherit-namespaces") != TriState.False;
            if (!inheritNamespaces) ReportNYI("xsl:copy[@inherit-namespaces = 'no']");

            ParseTypeAttribute(4);
            ParseValidationAttribute(5, /*defVal:*/false);

            List<XslNode> content = new List<XslNode>();
            if (_input.MoveToXsltAttribute(3, "use-attribute-sets"))
            {
                AddUseAttributeSets(content);
            }
            return SetInfo(F.Element(name, ns, _input.XslVersion),
                LoadEndTag(LoadInstructions(content)), ctxInfo
            );
        }

        // http://www.w3.org/TR/xslt#creating-attributes
        // http://www.w3.org/TR/xslt20#creating-attributes
        private readonly XsltAttribute[] _attributeAttributes = {
            new XsltAttribute("name",       V1Req | V2Req),
            new XsltAttribute("namespace",  V1Opt | V2Opt),
            new XsltAttribute("select",     V2Opt),
            new XsltAttribute("separator",  V2Opt),
            new XsltAttribute("type",       V2Opt),
            new XsltAttribute("validation", V2Opt)
        };
        private XslNode XslAttribute()
        {
            ContextInfo ctxInfo = _input.GetAttributes(_attributeAttributes);

            string name = ParseNCNameAttribute(0);
            string? ns = ParseStringAttribute(1, "namespace");
            CheckError(ns == XmlReservedNs.NsXmlNs, /*[XT_024]*/SR.Xslt_ReservedNS, ns);

            string? select = ParseStringAttribute(2, "select");
            if (select != null) ReportNYI("xsl:attribute/@select");
            string? separator = ParseStringAttribute(3, "separator");
            if (separator != null) ReportNYI("xsl:attribute/@separator");

            ParseTypeAttribute(4);
            ParseValidationAttribute(5, /*defVal:*/false);

            return SetInfo(F.Attribute(name, ns, _input.XslVersion), LoadContent(select != null), ctxInfo);
        }

        // http://www.w3.org/TR/xslt#sorting
        // http://www.w3.org/TR/xslt20/#element-sort
        private readonly XsltAttribute[] _sortAttributes = {
            new XsltAttribute("select",     V1Opt | V2Opt),
            new XsltAttribute("lang",       V1Opt | V2Opt),
            new XsltAttribute("order",      V1Opt | V2Opt),
            new XsltAttribute("collation",  V1Opt | V2Opt),
            new XsltAttribute("stable",     V1Opt | V2Opt),
            new XsltAttribute("case-order", V1Opt | V2Opt),
            new XsltAttribute("data-type",  V1Opt | V2Opt)
        };
        private XslNode XslSort(int sortNumber)
        {
            ContextInfo ctxInfo = _input.GetAttributes(_sortAttributes);

            string? select = ParseStringAttribute(0, "select");
            string? lang = ParseStringAttribute(1, "lang");
            string? order = ParseStringAttribute(2, "order");
            ParseCollationAttribute(3);
            TriState stable = ParseYesNoAttribute(4, "stable");
            string? caseOrder = ParseStringAttribute(5, "case-order");
            string? dataType = ParseStringAttribute(6, "data-type");

            if (stable != TriState.Unknown)
            {
                CheckError(sortNumber != 0, SR.Xslt_SortStable);
            }

            List<XslNode>? content;
            if (V1)
            {
                CheckNoContent();
            }
            else
            {
                content = LoadContent(select != null);
                if (content.Count != 0)
                {
                    ReportNYI("xsl:sort/*");
                }
            }

            select ??= ".";

            return SetInfo(F.Sort(select, lang, dataType, order, caseOrder, _input.XslVersion),
                null, ctxInfo
            );
        }

#if XSLT2
        // http://www.w3.org/TR/xslt20/#element-document
        XsltAttribute[] documentAttributes = {
            new XsltAttribute("type"      , V2Opt),
            new XsltAttribute("validation", V2Opt)
        };
        private XslNode XslDocument() {
            ContextInfo ctxInfo = input.GetAttributes(documentAttributes);

            ParseTypeAttribute(0);
            ParseValidationAttribute(1, /*defVal:*/false);

            ReportNYI("xsl:document");

            List<XslNode> content = LoadEndTag(LoadInstructions());

            return null;
        }

        // http://www.w3.org/TR/xslt20/#element-analyze-string
        XsltAttribute[] analyzeStringAttributes = {
            new XsltAttribute("select", V2Req),
            new XsltAttribute("regex" , V2Req),
            new XsltAttribute("flags" , V2Opt)
        };
        private XslNode XslAnalyzeString() {
            ContextInfo ctxInfo = input.GetAttributes(analyzeStringAttributes);

            string select = ParseStringAttribute(0, "select");
            string regex  = ParseStringAttribute(1, "regex" );
            string flags  = ParseStringAttribute(2, "flags" ) ?? "";

            ReportNYI("xsl:analyze-string");

            XslNode matching = null;
            XslNode nonMatching = null;
            QName parentName = input.ElementName;
            if (input.MoveToFirstChild()) {
                do {
                    switch (input.NodeType) {
                    case XmlNodeType.Element:
                        if (input.IsXsltKeyword(atoms.MatchingSubstring)) {
                            ContextInfo ctxInfoChld = input.GetAttributes();
                            CheckError(nonMatching != null, /*[???]*/SR.Xslt_AnalyzeStringChildOrder);
                            CheckError(matching    != null, /*[???]*/SR.Xslt_AnalyzeStringDupChild, atoms.MatchingSubstring);
                            // The current template rule becomes null, so we must not allow xsl:apply-import's within this element
                            input.CanHaveApplyImports = false;
                            matching = SetInfo(F.List(), LoadInstructions(), ctxInfoChld);
                        } else if (input.IsXsltKeyword(atoms.NonMatchingSubstring)) {
                            ContextInfo ctxInfoChld = input.GetAttributes();
                            CheckError(nonMatching != null, /*[???]*/SR.Xslt_AnalyzeStringDupChild, atoms.NonMatchingSubstring);
                            input.CanHaveApplyImports = false;
                            nonMatching = SetInfo(F.List(), LoadInstructions(), ctxInfoChld);
                        } else if (input.IsXsltKeyword(atoms.Fallback)) {
                            XslFallback();
                        } else {
                            ReportError(/*[XT_017]*/SR.Xslt_UnexpectedElement, input.QualifiedName, parentName);
                            input.SkipNode();
                        }
                        break;
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        break;
                    default:
                        Debug.Assert(input.NodeType == XmlNodeType.Text);
                        ReportError(/*[XT_017]*/SR.Xslt_TextNodesNotAllowed, parentName);
                        break;
                    }
                } while (input.MoveToNextSibling());
            }
            CheckError(matching == nonMatching, /*[XTSE1130]*/SR.Xslt_AnalyzeStringEmpty);

            ctxInfo.SaveExtendedLineInfo(input);

            return null;
        }

        // http://www.w3.org/TR/xslt20/#element-namespace
        XsltAttribute[] namespaceAttributes = {
            new XsltAttribute("name"  , V2Req),
            new XsltAttribute("select", V2Opt)
        };
        private XslNode XslNamespace() {
            ContextInfo ctxInfo = input.GetAttributes(namespaceAttributes);
            string name = ParseNCNameAttribute(0);
            string select= ParseStringAttribute(1, "select");

            List<XslNode> content = LoadContent(select != null);
            CheckError(select == null && content.Count == 0, /*[???]*/SR.Xslt_NoSelectNoContent, input.ElementName);

            ReportNYI("xsl:namespace");

            return null;
        }

        // http://www.w3.org/TR/xslt20/#element-perform-sort
        XsltAttribute[] performSortAttributes = {
            new XsltAttribute("select", V2Opt)
        };
        private XslNode XslPerformSort() {
            ContextInfo ctxInfo = input.GetAttributes(performSortAttributes);
            string select = ParseStringAttribute(0, "select");

            List<XslNode> content = LoadInstructions(InstructionFlags.AllowSort);
            ctxInfo.SaveExtendedLineInfo(input);

            if (select != null) {
                foreach (XslNode node in content) {
                    if (node.NodeType != XslNodeType.Sort) {
                        ReportError(SR.Xslt_PerformSortCntSel);
                        break;
                    }
                }
            }

            ReportNYI("xsl:perform-sort");
            return null;
        }

        // http://www.w3.org/TR/xslt20/#element-for-each-group
        XsltAttribute[] forEachGroupAttributes = {
            new XsltAttribute("select"             , V2Req),
            new XsltAttribute("group-by"           , V2Opt),
            new XsltAttribute("group-adjacent"     , V2Opt),
            new XsltAttribute("group-starting-with", V2Opt),
            new XsltAttribute("group-ending-with"  , V2Opt),
            new XsltAttribute("collation"          , V2Opt)
        };
        private XslNode XslForEachGroup() {
            ContextInfo ctxInfo = input.GetAttributes(forEachGroupAttributes);

            string select            = ParseStringAttribute(   0, "select"             );
            string groupBy           = ParseStringAttribute(   1, "group-by"           );
            string groupAdjacent     = ParseStringAttribute(   2, "group-adjacent"     );
            string groupStartingWith = ParseStringAttribute(   3, "group-starting-with");
            string groupEndingWith   = ParseStringAttribute(   4, "group-ending-with"  );
            string collation         = ParseCollationAttribute(5);

            ReportNYI("xsl:for-each-group");

            // The current template rule becomes null, so we must not allow xsl:apply-import's within this element
            input.CanHaveApplyImports = false;
            List<XslNode> content = LoadInstructions(InstructionFlags.AllowSort);
            ctxInfo.SaveExtendedLineInfo(input);

            return null;
        }

        // http://www.w3.org/TR/xslt20/#element-next-match
        private XslNode XslNextMatch() {
            ContextInfo ctxInfo = input.GetAttributes();

            // We need to do this dynamic any way:
            //if (!input.CanHaveApplyImports) {
            //    ReportError(/*[XT_015]*/SR.Xslt_InvalidApplyImports);
            //    input.SkipNode();
            //    return null;
            //}

            ReportNYI("xsl:next-match");

            List<XslNode> content = LoadWithParams(InstructionFlags.AllowFallback);
            ctxInfo.SaveExtendedLineInfo(input);

            return null;
        }

        // http://www.w3.org/TR/xslt20/#element-sequence
        XsltAttribute[] sequenceAttributes = {
            new XsltAttribute("select", V2Req)
        };
        private XslNode XslSequence() {
            ContextInfo ctxInfo = input.GetAttributes(sequenceAttributes);
            string select = ParseStringAttribute(0, "select");
            ReportNYI("xsl:sequence");

            QName parentName = input.ElementName;
            if (input.MoveToFirstChild()) {
                do {
                    switch (input.NodeType) {
                    case XmlNodeType.Element:
                        if (input.IsXsltKeyword(atoms.Fallback)) {
                            XslFallback();
                        } else {
                            ReportError(/*[XT_017]*/SR.Xslt_UnexpectedElement, input.QualifiedName, parentName);
                            input.SkipNode();
                        }
                        break;
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        break;
                    default:
                        Debug.Assert(input.NodeType == XmlNodeType.Text);
                        ReportError(/*[XT_017]*/SR.Xslt_TextNodesNotAllowed, parentName);
                        break;
                    }
                } while (input.MoveToNextSibling());
            }
            return null;
        }

        // http://www.w3.org/TR/xslt20/#element-result-document
        XsltAttribute[] resultDocumentAttributes = {
            new XsltAttribute("format"                , V2Opt), // 0
            new XsltAttribute("href"                  , V2Opt), // 1
            new XsltAttribute("validation"            , V2Opt), // 2
            new XsltAttribute("type"                  , V2Opt), // 3
            new XsltAttribute("name"                  , V2Opt), // 4
            new XsltAttribute("method"                , V2Opt), // 5
            new XsltAttribute("byte-order-mark"       , V2Opt), // 6
            new XsltAttribute("cdata-section-elements", V2Opt), // 7
            new XsltAttribute("doctype-public"        , V2Opt), // 8
            new XsltAttribute("doctype-system"        , V2Opt), // 9
            new XsltAttribute("encoding"              , V2Opt), // 10
            new XsltAttribute("escape-uri-attributes" , V2Opt), // 11
            new XsltAttribute("include-content-type"  , V2Opt), // 12
            new XsltAttribute("indent"                , V2Opt), // 13
            new XsltAttribute("media-type"            , V2Opt), // 14
            new XsltAttribute("normalization-form"    , V2Opt), // 15
            new XsltAttribute("omit-xml-declaration"  , V2Opt), // 16
            new XsltAttribute("standalone"            , V2Opt), // 17
            new XsltAttribute("undeclare-prefixes"    , V2Opt), // 18
            new XsltAttribute("use-character-maps"    , V2Opt), // 19
            new XsltAttribute("output-version"        , V2Opt)  // 20
        };
        private XslNode XslResultDocument() {
            ContextInfo ctxInfo = input.GetAttributes(resultDocumentAttributes);

            string format                  = ParseStringAttribute(0 , "format");
            XmlWriterSettings settings = new XmlWriterSettings(); // we should use attFormat to determing settings
            string href                    = ParseStringAttribute(1 , "href");
            ParseValidationAttribute(2, /*defVal:*/false);
            ParseTypeAttribute(3);
            QilName  name                  = ParseQNameAttribute( 4);
            TriState byteOrderMask         = ParseYesNoAttribute( 6 , "byte-order-mark");
            string   docTypePublic         = ParseStringAttribute(8 , "doctype-public");
            string   docTypeSystem         = ParseStringAttribute(9 , "doctype-system");
            bool     escapeUriAttributes   = ParseYesNoAttribute( 11, "escape-uri-attributes") != TriState.False;
            bool     includeContentType    = ParseYesNoAttribute( 12, "include-content-type") != TriState.False;
            settings.Indent                = ParseYesNoAttribute( 13, "indent") == TriState.True;
            string   mediaType             = ParseStringAttribute(14, "media-type");
            string   normalizationForm     = ParseStringAttribute(15, "normalization-form");
            settings.OmitXmlDeclaration    = ParseYesNoAttribute( 16, "omit-xml-declaration") == TriState.True;
            settings.Standalone            = ParseYesNoAttribute( 17, "standalone"        ) == TriState.True ? XmlStandalone.Yes : XmlStandalone.No;
            bool undeclarePrefixes         = ParseYesNoAttribute( 18, "undeclare-prefixes") == TriState.True;
            List<QilName> useCharacterMaps = ParseUseCharacterMaps(19);
            string   outputVersion         = ParseStringAttribute(20, "output-version");

            ReportNYI("xsl:result-document");

            if (format != null) ReportNYI("xsl:result-document/@format");

            href ??= string.Empty;
            // attHref is a BaseUri of new output tree. It should be resolved relative to "base output URI"


            if (input.MoveToXsltAttribute(5, "method")) {
                compiler.EnterForwardsCompatible();
                XmlOutputMethod   outputMethod;
                ParseOutputMethod(input.Value, out outputMethod);
                if (compiler.ExitForwardsCompatible(input.ForwardCompatibility)) {
                    settings.OutputMethod = outputMethod;
                }
            }
            if (input.MoveToXsltAttribute(7, "cdata-section-elements")) {
                // Do not check the import precedence, the effective value is the union of all specified values
                compiler.EnterForwardsCompatible();
                string[] qnames = XmlConvert.SplitString(input.Value);
                List<XmlQualifiedName> list = new List<XmlQualifiedName>();
                for (int i = 0; i < qnames.Length; i++) {
                    list.Add(ResolveQName(/*ignoreDefaultNs:*/false, qnames[i]));
                }
                if (compiler.ExitForwardsCompatible(input.ForwardCompatibility)) {
                    foreach (XmlQualifiedName qname in list) {
                        settings.CDataSectionElements.Add(qname);
                    }
                }
            }
            if (input.MoveToXsltAttribute(10, "encoding")) {
                try {
                    // Encoding.GetEncoding() should never throw NotSupportedException, only ArgumentException
                    settings.Encoding = Encoding.GetEncoding(input.Value);
                } catch (ArgumentException) {
                    if (!input.ForwardCompatibility) {
                        ReportWarning(/*[XT_004]*/SR.Xslt_InvalidEncoding, input.Value);
                    }
                }
            }

            if (byteOrderMask != TriState.Unknown) ReportNYI("xsl:result-document/@byte-order-mark");
            if (!escapeUriAttributes) ReportNYI("xsl:result-document/@escape-uri-attributes == flase()");
            if (!includeContentType) ReportNYI("xsl:output/@include-content-type == flase()");
            if (normalizationForm != null) ReportNYI("xsl:result-document/@normalization-form");
            if (undeclarePrefixes) ReportNYI("xsl:result-document/@undeclare-prefixes == true()");

            if (docTypePublic != null) {
                settings.DocTypePublic = docTypePublic;
            }

            if (docTypeSystem != null) {
                settings.DocTypeSystem = docTypeSystem;
            }
            if (mediaType != null) {
                settings.MediaType = mediaType;
            }

            if (useCharacterMaps != null) ReportNYI("xsl:result-document/@use-character-maps");

            if (outputVersion != null) {
                // BUGBUG: Check that version is a valid nmtoken
                // ignore version since we support only one version for both xml (1.0) and html (4.0)
                ReportNYI("xsl:result-document/@output-version");
            }

            LoadInstructions();
            return null;
        }
#endif

        // http://www.w3.org/TR/xslt#literal-result-element
        private XslNode LoadLiteralResultElement(bool asStylesheet)
        {
            Debug.Assert(_input.NodeType == XmlNodeType.Element);
            string prefix = _input.Prefix;
            string name = _input.LocalName;
            string nsUri = _input.NamespaceUri;

            ContextInfo ctxInfo = _input.GetLiteralAttributes(asStylesheet);

            if (_input.IsExtensionNamespace(nsUri))
            {
                // This is not a literal result element, so drop all attributes we have collected
                return SetInfo(F.List(), LoadFallbacks(name), ctxInfo);
            }

            List<XslNode> content = new List<XslNode>();

            for (int i = 1; _input.MoveToLiteralAttribute(i); i++)
            {
                if (_input.IsXsltNamespace() && _input.IsKeyword(_atoms.UseAttributeSets))
                {
                    AddUseAttributeSets(content);
                }
            }

            for (int i = 1; _input.MoveToLiteralAttribute(i); i++)
            {
                if (!_input.IsXsltNamespace())
                {
                    XslNode att = F.LiteralAttribute(F.QName(_input.LocalName, _input.NamespaceUri, _input.Prefix), _input.Value, _input.XslVersion);
                    // QilGenerator takes care of AVTs, and needs line info
                    AddInstruction(content, SetLineInfo(att, ctxInfo.lineInfo));
                }
                else
                {
                    // ignore all other xslt attributes. See XslInput.GetLiteralAttributes()
                }
            }

            content = LoadEndTag(LoadInstructions(content));
            return SetInfo(F.LiteralElement(F.QName(name, nsUri, prefix)), content, ctxInfo);
        }

        private void CheckWithParam(List<XslNode> content, XslNode withParam)
        {
            Debug.Assert(content != null && withParam != null);
            Debug.Assert(withParam.NodeType == XslNodeType.WithParam);
            foreach (XslNode node in content)
            {
                if (node.NodeType == XslNodeType.WithParam && node.Name!.Equals(withParam.Name))
                {
                    ReportError(/*[XT0670]*/SR.Xslt_DuplicateWithParam, withParam.Name!.QualifiedName);
                    break;
                }
            }
        }

        private static void AddInstruction(List<XslNode> content, XslNode? instruction)
        {
            Debug.Assert(content != null);
            if (instruction != null)
            {
                content.Add(instruction);
            }
        }

        private List<XslNode> LoadEndTag(List<XslNode> content)
        {
            Debug.Assert(content != null);
            if (_compiler.IsDebug && !_input.IsEmptyElement)
            {
                AddInstruction(content, SetLineInfo(F.Nop(), _input.BuildLineInfo()));
            }
            return content;
        }

        private XslNode? LoadUnknownXsltInstruction(string parentName)
        {
            _input.GetVersionAttribute();
            if (!_input.ForwardCompatibility)
            {
                ReportError(/*[XT_026]*/SR.Xslt_UnexpectedElement, _input.QualifiedName, parentName);
                _input.SkipNode();
                return null;
            }
            else
            {
                ContextInfo ctxInfo = _input.GetAttributes();
                List<XslNode> fallbacks = LoadFallbacks(_input.LocalName);
                return SetInfo(F.List(), fallbacks, ctxInfo);
            }
        }

        private List<XslNode> LoadFallbacks(string instrName)
        {
            _input.MoveToElement();
            ISourceLineInfo extElmLineInfo = _input.BuildNameLineInfo();
            List<XslNode> fallbacksArray = new List<XslNode>();
            /* Process children */
            if (_input.MoveToFirstChild())
            {
                do
                {
                    if (_input.IsXsltKeyword(_atoms.Fallback))
                    {
                        ContextInfo ctxInfo = _input.GetAttributes();
                        fallbacksArray.Add(SetInfo(F.List(), LoadInstructions(), ctxInfo));
                    }
                    else
                    {
                        _input.SkipNode();
                    }
                } while (_input.MoveToNextSibling());
            }

            // Generate runtime error if there is no fallbacks
            if (fallbacksArray.Count == 0)
            {
                fallbacksArray.Add(
                    F.Error(XslLoadException.CreateMessage(extElmLineInfo, SR.Xslt_UnknownExtensionElement, instrName))
                );
            }
            return fallbacksArray;
        }

        // ------------------ little helper methods ---------------------

        // Suppresses errors if FCB is enabled
        private QilName ParseModeAttribute(int attNum)
        {
            //Debug.Assert(
            //    input.IsXsltKeyword(atoms.ApplyTemplates) ||
            //    input.IsXsltKeyword(atoms.Template) && V1
            //);
            if (!_input.MoveToXsltAttribute(attNum, "mode"))
            {
                return nullMode;
            }
            // mode is always optional attribute
            _compiler.EnterForwardsCompatible();
            string qname = _input.Value;
            QilName mode;
            if (!V1 && qname == "#default")
            {
                mode = nullMode;
            }
            else if (!V1 && qname == "#current")
            {
                ReportNYI("xsl:apply-templates[@mode='#current']");
                mode = nullMode;
            }
            else if (!V1 && qname == "#all")
            {
                ReportError(SR.Xslt_ModeListAll);
                mode = nullMode;
            }
            else
            {
                mode = CreateXPathQName(qname);
            }
            if (!_compiler.ExitForwardsCompatible(_input.ForwardCompatibility))
            {
                mode = nullMode;
            }
            return mode;
        }

        // Parse mode when it is list. V1: -, V2: xsl:template
        // Suppresses errors if FCB is enabled
        private QilName ParseModeListAttribute(int attNum)
        {
            //Debug.Assert(input.IsXsltKeyword(atoms.Template) && !V1);
            if (!_input.MoveToXsltAttribute(attNum, "mode"))
            {
                return nullMode;
            }

            string modeList = _input.Value;
            if (modeList == "#all")
            {
                ReportNYI("xsl:template[@mode='#all']");
                return nullMode;
            }
            else
            {
                string[] list = XmlConvert.SplitString(modeList);
                List<QilName> modes = new List<QilName>(list.Length);

                _compiler.EnterForwardsCompatible();  // mode is always optional attribute

                if (list.Length == 0)
                {
                    ReportError(SR.Xslt_ModeListEmpty);
                }
                else
                {
                    foreach (string qname in list)
                    {
                        QilName mode;
                        if (qname == "#default")
                        {
                            mode = nullMode;
                        }
                        else if (qname == "#current")
                        {
                            ReportNYI("xsl:apply-templates[@mode='#current']");
                            break;
                        }
                        else if (qname == "#all")
                        {
                            ReportError(SR.Xslt_ModeListAll);
                            break;
                        }
                        else
                        {
                            mode = CreateXPathQName(qname);
                        }
                        bool dup = false;
                        foreach (QilName m in modes)
                        {
                            dup |= m.Equals(mode);
                        }
                        if (dup)
                        {
                            ReportError(SR.Xslt_ModeListDup, qname);
                        }
                        else
                        {
                            modes.Add(mode);
                        }
                    }
                }

                if (!_compiler.ExitForwardsCompatible(_input.ForwardCompatibility))
                {
                    modes.Clear();
                    modes.Add(nullMode);
                }
                if (1 < modes.Count)
                {
                    ReportNYI("Multiple modes");
                    return nullMode;
                }
                if (modes.Count == 0)
                {
                    return nullMode;
                }
                return modes[0];
            }
        }

        private string? ParseCollationAttribute(int attNum)
        {
            if (_input.MoveToXsltAttribute(attNum, "collation"))
            {
                ReportNYI("@collation");
            }
            return null;
        }

        // Does not suppress errors
        private bool ResolveQName(bool ignoreDefaultNs, string qname, out string localName, out string namespaceName, out string prefix)
        {
            if (qname == null)
            {
                // That means stylesheet is incorrect
                prefix = _compiler.PhantomNCName;
                localName = _compiler.PhantomNCName;
                namespaceName = _compiler.CreatePhantomNamespace();
                return false;
            }
            if (!_compiler.ParseQName(qname, out prefix, out localName, (IErrorHelper)this))
            {
                namespaceName = _compiler.CreatePhantomNamespace();
                return false;
            }
            if (ignoreDefaultNs && prefix.Length == 0)
            {
                namespaceName = string.Empty;
            }
            else
            {
                namespaceName = _input.LookupXmlNamespace(prefix)!;
                if (namespaceName == null)
                {
                    namespaceName = _compiler.CreatePhantomNamespace();
                    return false;
                }
            }
            return true;
        }

        // Does not suppress errors
        private QilName? ParseQNameAttribute(int attNum)
        {
            bool required = _input.IsRequiredAttribute(attNum);
            QilName? result = null;
            if (!required)
            {
                _compiler.EnterForwardsCompatible();
            }
            if (_input.MoveToXsltAttribute(attNum, "name"))
            {
                string prefix, localName, namespaceName;
                if (ResolveQName(/*ignoreDefaultNs:*/true, _input.Value, out localName, out namespaceName, out prefix))
                {
                    result = F.QName(localName, namespaceName, prefix);
                }
            }
            if (!required)
            {
                _compiler.ExitForwardsCompatible(_input.ForwardCompatibility);
            }
            if (result == null && required)
            {
                result = F.QName(_compiler.PhantomNCName, _compiler.CreatePhantomNamespace(), _compiler.PhantomNCName);
            }
            return result;
        }

        private string ParseNCNameAttribute(int attNum)
        {
            Debug.Assert(_input.IsRequiredAttribute(attNum), "It happened that @name as NCName is always required attribute");
            if (_input.MoveToXsltAttribute(attNum, "name"))
            {
                return _input.Value;
            }
            return _compiler.PhantomNCName;
        }

        // Does not suppress errors
        private QilName CreateXPathQName(string qname)
        {
            string prefix, localName, namespaceName;
            ResolveQName(/*ignoreDefaultNs:*/true, qname, out localName, out namespaceName, out prefix);
            return F.QName(localName, namespaceName, prefix);
        }

        // Does not suppress errors
        private XmlQualifiedName ResolveQName(bool ignoreDefaultNs, string qname)
        {
            string localName, namespaceName;
            ResolveQName(ignoreDefaultNs, qname, out localName, out namespaceName, out _);
            return new XmlQualifiedName(localName, namespaceName);
        }

        // Does not suppress errors
        private void ParseWhitespaceRules(string elements, bool preserveSpace)
        {
            if (!string.IsNullOrEmpty(elements))
            {
                string[] tokens = XmlConvert.SplitString(elements);
                for (int i = 0; i < tokens.Length; i++)
                {
                    string? prefix, localName, namespaceName;
                    if (!_compiler.ParseNameTest(tokens[i], out prefix, out localName, (IErrorHelper)this))
                    {
                        namespaceName = _compiler.CreatePhantomNamespace();
                    }
                    else if (string.IsNullOrEmpty(prefix))
                    {
                        namespaceName = prefix;
                    }
                    else
                    {
                        namespaceName = _input.LookupXmlNamespace(prefix) ?? _compiler.CreatePhantomNamespace();
                    }
                    int index = (
                        (localName == null ? 1 : 0) +
                        (namespaceName == null ? 1 : 0)
                    );
                    _curStylesheet!.AddWhitespaceRule(index, new WhitespaceRule(localName, namespaceName, preserveSpace));
                }
            }
        }

        // Does not suppress errors.  In case of error, null is returned.
        private XmlQualifiedName? ParseOutputMethod(string attValue, out XmlOutputMethod method)
        {
            string prefix, localName, namespaceName;
            ResolveQName(/*ignoreDefaultNs:*/true, attValue, out localName, out namespaceName, out prefix);
            method = XmlOutputMethod.AutoDetect;

            if (Compiler.IsPhantomNamespace(namespaceName))
            {
                return null;
            }
            else if (prefix.Length == 0)
            {
                switch (localName)
                {
                    case "xml": method = XmlOutputMethod.Xml; break;
                    case "html": method = XmlOutputMethod.Html; break;
                    case "text": method = XmlOutputMethod.Text; break;
                    default:
                        ReportError(/*[XT1570]*/SR.Xslt_InvalidAttrValue, nameof(method), attValue);
                        return null;
                }
            }
            else
            {
                if (!_input.ForwardCompatibility)
                {
                    ReportWarning(/*[XT1570]*/SR.Xslt_InvalidMethod, attValue);
                }
            }
            return new XmlQualifiedName(localName, namespaceName);
        }

        // Suppresses errors if FCB is enabled
        private void AddUseAttributeSets(List<XslNode> list)
        {
            Debug.Assert(_input.LocalName == "use-attribute-sets", "we are positioned on this attribute");
            Debug.Assert(list != null && list.Count == 0, "It happened that we always add use-attribute-sets first. Otherwise we can't call list.Clear()");

            _compiler.EnterForwardsCompatible();
            foreach (string qname in XmlConvert.SplitString(_input.Value))
            {
                AddInstruction(list, SetLineInfo(F.UseAttributeSet(CreateXPathQName(qname)), _input.BuildLineInfo()));
            }
            if (!_compiler.ExitForwardsCompatible(_input.ForwardCompatibility))
            {
                // There were errors in the list, ignore the whole list
                list.Clear();
            }
        }

        private List<QilName> ParseUseCharacterMaps(int attNum)
        {
            List<QilName> useCharacterMaps = new List<QilName>();
            if (_input.MoveToXsltAttribute(attNum, "use-character-maps"))
            {
                _compiler.EnterForwardsCompatible();
                foreach (string qname in XmlConvert.SplitString(_input.Value))
                {
                    useCharacterMaps.Add(CreateXPathQName(qname));
                }
                if (!_compiler.ExitForwardsCompatible(_input.ForwardCompatibility))
                {
                    useCharacterMaps.Clear(); // There were errors in the list, ignore the whole list
                }
            }
            return useCharacterMaps;
        }

        private string? ParseStringAttribute(int attNum, string attName)
        {
            if (_input.MoveToXsltAttribute(attNum, attName))
            {
                return _input.Value;
            }
            return null;
        }

        private char ParseCharAttribute(int attNum, string attName, char defVal)
        {
            if (_input.MoveToXsltAttribute(attNum, attName))
            {
                if (_input.Value.Length == 1)
                {
                    return _input.Value[0];
                }
                else
                {
                    if (_input.IsRequiredAttribute(attNum) || !_input.ForwardCompatibility)
                    {
                        ReportError(/*[XT_029]*/SR.Xslt_CharAttribute, attName);
                    }
                }
            }
            return defVal;
        }

        // Suppresses errors if FCB is enabled
        private TriState ParseYesNoAttribute(int attNum, string attName)
        {
            Debug.Assert(!_input.IsRequiredAttribute(attNum), "All Yes/No attributes are optional.");
            if (_input.MoveToXsltAttribute(attNum, attName))
            {
                switch (_input.Value)
                {
                    case "yes": return TriState.True;
                    case "no": return TriState.False;
                    default:
                        if (!_input.ForwardCompatibility)
                        {
                            ReportError(/*[XT_028]*/SR.Xslt_BistateAttribute, attName, "yes", "no");
                        }
                        break;
                }
            }
            return TriState.Unknown;
        }

        private void ParseTypeAttribute(int attNum)
        {
            Debug.Assert(!_input.IsRequiredAttribute(attNum), "All 'type' attributes are optional.");
            if (_input.MoveToXsltAttribute(attNum, "type"))
            {
                CheckError(true, /*[???]*/SR.Xslt_SchemaAttribute, "type");
            }
        }

        private void ParseValidationAttribute(int attNum, bool defVal)
        {
            Debug.Assert(!_input.IsRequiredAttribute(attNum), "All 'validation' attributes are optional.");
            string attributeName = defVal ? _atoms.DefaultValidation : "validation";
            if (_input.MoveToXsltAttribute(attNum, attributeName))
            {
                string value = _input.Value;
                if (value == "strip")
                {
                    // no error
                }
                else if (
                  value == "preserve" ||
                  value == "strict" && !defVal ||
                  value == "lax" && !defVal
              )
                {
                    ReportError(/*[???]*/SR.Xslt_SchemaAttributeValue, attributeName, value);
                }
                else if (!_input.ForwardCompatibility)
                {
                    ReportError(/*[???]*/SR.Xslt_InvalidAttrValue, attributeName, value);
                }
            }
        }

        private void ParseInputTypeAnnotationsAttribute(int attNum)
        {
            Debug.Assert(!_input.IsRequiredAttribute(attNum), "All 'input-type-validation' attributes are optional.");
            if (_input.MoveToXsltAttribute(attNum, "input-type-annotations"))
            {
                string value = _input.Value;
                switch (value)
                {
                    case "unspecified":
                        break;
                    case "strip":
                    case "preserve":
                        if (_compiler.inputTypeAnnotations == null)
                        {
                            _compiler.inputTypeAnnotations = value;
                        }
                        else
                        {
                            CheckError(_compiler.inputTypeAnnotations != value, /*[XTSE0265]*/SR.Xslt_InputTypeAnnotations);
                        }
                        break;
                    default:
                        if (!_input.ForwardCompatibility)
                        {
                            ReportError(/*[???]*/SR.Xslt_InvalidAttrValue, "input-type-annotations", value);
                        }
                        break;
                }
            }
        }

        private void CheckNoContent()
        {
            _input.MoveToElement();
            QName parentName = _input.ElementName;
            ISourceLineInfo? errorLineInfo = SkipEmptyContent();

            if (errorLineInfo != null)
            {
                _compiler.ReportError(errorLineInfo, /*[XT0260]*/SR.Xslt_NotEmptyContents, parentName);
            }
        }

        // Returns ISourceLineInfo of the first violating (non-whitespace) node, or null otherwise
        private ISourceLineInfo? SkipEmptyContent()
        {
            ISourceLineInfo? result = null;

            // Really EMPTY means no content at all, but for the sake of compatibility with MSXML we allow whitespace
            if (_input.MoveToFirstChild())
            {
                do
                {
                    // NOTE: XmlNodeType.SignificantWhitespace is not allowed here
                    if (_input.NodeType != XmlNodeType.Whitespace)
                    {
                        result ??= _input.BuildNameLineInfo();
                        _input.SkipNode();
                    }
                } while (_input.MoveToNextSibling());
            }
            return result;
        }

        private static XslNode SetLineInfo(XslNode node, ISourceLineInfo? lineInfo)
        {
            Debug.Assert(node != null);
            node.SourceLine = lineInfo;
            return node;
        }

        private static void SetContent(XslNode node, List<XslNode>? content)
        {
            Debug.Assert(node != null);
            if (content != null && content.Count == 0)
            {
                content = null; // Actualy we can reuse this ArrayList.
            }
            node.SetContent(content);
        }

        internal static XslNode SetInfo(XslNode to, List<XslNode>? content, ContextInfo info)
        {
            Debug.Assert(to != null);
            to.Namespaces = info.nsList;
            SetContent(to, content);
            SetLineInfo(to, info.lineInfo);
            return to;
        }

        // NOTE! We inverting namespace order that is irelevant for namespace of the same node, but
        // for included styleseets we don't keep stylesheet as a node and adding it's namespaces to
        // each toplevel element by MergeNamespaces().
        // Namespaces of stylesheet can be overridden in template and to make this works correclety we
        // should attache them after NsDec of top level elements.
        // Toplevel element almost never contais NsDecl and in practice node duplication will not happened, but if they have
        // we should copy NsDecls of stylesheet locally in toplevel elements.
        private static NsDecl? MergeNamespaces(NsDecl? thisList, NsDecl? parentList)
        {
            if (parentList == null)
            {
                return thisList;
            }
            if (thisList == null)
            {
                return parentList;
            }
            // Clone all nodes and attache them to nodes of thisList;
            while (parentList != null)
            {
                bool duplicate = false;
                for (NsDecl? tmp = thisList; tmp != null; tmp = tmp.Prev)
                {
                    if (Ref.Equal(tmp.Prefix, parentList.Prefix) && (
                        tmp.Prefix != null ||           // Namespace declaration
                        tmp.NsUri == parentList.NsUri   // Extension or excluded namespace
                    ))
                    {
                        duplicate = true;
                        break;
                    }
                }
                if (!duplicate)
                {
                    thisList = new NsDecl(thisList, parentList.Prefix, parentList.NsUri);
                }
                parentList = parentList.Prev;
            }
            return thisList;
        }

        // -------------------------------- IErrorHelper --------------------------------

        public void ReportError(string res, params string?[]? args)
        {
            _compiler.ReportError(_input.BuildNameLineInfo(), res, args);
        }

        public void ReportWarning(string res, params string?[]? args)
        {
            _compiler.ReportWarning(_input.BuildNameLineInfo(), res, args);
        }

        private void ReportNYI(string? arg)
        {
            if (!_input.ForwardCompatibility)
            {
                ReportError(SR.Xslt_NotYetImplemented, arg);
            }
        }

        public void CheckError(bool cond, string res, params string?[]? args)
        {
            if (cond)
            {
                _compiler.ReportError(_input.BuildNameLineInfo(), res, args);
            }
        }
    }
}
