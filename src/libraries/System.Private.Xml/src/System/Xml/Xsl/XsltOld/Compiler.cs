// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.XsltOld.Debugger;
using MS.Internal.Xml.XPath;
using KeywordsTable = System.Xml.Xsl.Xslt.KeywordsTable;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class Sort
    {
        internal int select;
        internal string? lang;
        internal XmlDataType dataType;
        internal XmlSortOrder order;
        internal XmlCaseOrder caseOrder;

        public Sort(int sortkey, string? xmllang, XmlDataType datatype, XmlSortOrder xmlorder, XmlCaseOrder xmlcaseorder)
        {
            select = sortkey;
            lang = xmllang;
            dataType = datatype;
            order = xmlorder;
            caseOrder = xmlcaseorder;
        }
    }

    internal enum ScriptingLanguage
    {
        JScript,
        VisualBasic,
        CSharp
    }

    internal sealed class Compiler
    {
        internal const int InvalidQueryKey = -1;

        internal const double RootPriority = 0.5;

        // cached StringBuilder for AVT parseing
        internal StringBuilder AvtStringBuilder = new StringBuilder();

        private int _stylesheetid; // Root stylesheet has id=0. We are using this in CompileImports to compile BuiltIns
        private InputScope? _rootScope;

        //
        // Object members
        private XmlResolver? _xmlResolver;

        //
        // Template being currently compiled
        private TemplateBaseAction? _currentTemplate;
        private XmlQualifiedName? _currentMode;
        private Hashtable? _globalNamespaceAliasTable;

        //
        // Current import stack
        private Stack<Stylesheet>? _stylesheets;

        private readonly HybridDictionary _documentURIs = new HybridDictionary();

        // import/include documents, who is here has its URI in this.documentURIs
        private NavigatorInput _input = null!;

        // Atom table & InputScopeManager - cached top of the this.input stack
        private KeywordsTable? _atoms;
        private InputScopeManager _scopeManager = null!;

        //
        // Compiled stylesheet state
        internal Stylesheet? stylesheet;
        internal Stylesheet? rootStylesheet;
        private RootAction _rootAction = null!;
        private List<TheQuery>? _queryStore;
        private readonly QueryBuilder _queryBuilder = new QueryBuilder();
        private int _rtfCount;

        // Used to load Built In templates
        public static XmlQualifiedName BuiltInMode = new XmlQualifiedName("*", string.Empty);

        internal KeywordsTable Atoms
        {
            get
            {
                Debug.Assert(_atoms != null);
                return _atoms;
            }
        }

        internal int Stylesheetid
        {
            get { return _stylesheetid; }
            set { _stylesheetid = value; }
        }

        internal NavigatorInput? Document
        {
            get { return _input; }
        }

        internal NavigatorInput Input
        {
            get { return _input; }
        }

        internal bool Advance()
        {
            Debug.Assert(Document != null);
            return Document.Advance();
        }

        internal bool Recurse()
        {
            Debug.Assert(Document != null);
            return Document.Recurse();
        }

        internal bool ToParent()
        {
            Debug.Assert(Document != null);
            return Document.ToParent();
        }

        internal Stylesheet? CompiledStylesheet
        {
            get { return this.stylesheet; }
        }

        internal RootAction RootAction
        {
            get { return _rootAction; }
            set
            {
                Debug.Assert(_rootAction == null);
                _rootAction = value;
                Debug.Assert(_currentTemplate == null);
                _currentTemplate = _rootAction;
            }
        }

        internal List<TheQuery>? QueryStore
        {
            get { return _queryStore; }
        }

        internal string GetUnicRtfId()
        {
            _rtfCount++;
            return _rtfCount.ToString(CultureInfo.InvariantCulture);
        }

        //
        // The World of Compile
        //
        internal void Compile(NavigatorInput input, XmlResolver xmlResolver)
        {
            Debug.Assert(input != null);
            Debug.Assert(xmlResolver != null);
            Debug.Assert(_input == null && _atoms == null);
            _xmlResolver = xmlResolver;

            PushInputDocument(input);
            _rootScope = _scopeManager.PushScope();
            _queryStore = new List<TheQuery>();

            try
            {
                this.rootStylesheet = new Stylesheet();
                PushStylesheet(this.rootStylesheet);

                Debug.Assert(_input != null && _atoms != null);

                try
                {
                    this.CreateRootAction();
                }
                catch (XsltCompileException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new XsltCompileException(e, this.Input!.BaseURI, this.Input.LineNumber, this.Input.LinePosition);
                }

                this.stylesheet!.ProcessTemplates();
                _rootAction.PorcessAttributeSets(this.rootStylesheet);
                this.stylesheet.SortWhiteSpace();

                if (_globalNamespaceAliasTable != null)
                {
                    this.stylesheet.ReplaceNamespaceAlias(this);
                    _rootAction.ReplaceNamespaceAlias(this);
                }
            }
            finally
            {
                PopInputDocument();
            }

            Debug.Assert(_rootAction != null);
            Debug.Assert(this.stylesheet != null);
            Debug.Assert(_queryStore != null);
            Debug.Assert(_input == null && _atoms == null);
        }

        //
        // Input scope compiler's support
        //

        internal bool ForwardCompatibility
        {
            get { return _scopeManager.CurrentScope!.ForwardCompatibility; }
            set { _scopeManager.CurrentScope!.ForwardCompatibility = value; }
        }

        internal bool CanHaveApplyImports
        {
            get { return _scopeManager.CurrentScope!.CanHaveApplyImports; }
            set { _scopeManager.CurrentScope!.CanHaveApplyImports = value; }
        }

        internal void InsertExtensionNamespace(string value)
        {
            string[]? nsList = ResolvePrefixes(value);
            if (nsList != null)
            {
                _scopeManager.InsertExtensionNamespaces(nsList);
            }
        }

        internal void InsertExcludedNamespace(string value)
        {
            string[]? nsList = ResolvePrefixes(value);
            if (nsList != null)
            {
                _scopeManager.InsertExcludedNamespaces(nsList);
            }
        }

        internal void InsertExtensionNamespace()
        {
            InsertExtensionNamespace(Input!.Navigator.GetAttribute(Input.Atoms.ExtensionElementPrefixes, Input.Atoms.UriXsl));
        }

        internal void InsertExcludedNamespace()
        {
            InsertExcludedNamespace(Input!.Navigator.GetAttribute(Input.Atoms.ExcludeResultPrefixes, Input.Atoms.UriXsl));
        }

        internal bool IsExtensionNamespace(string nspace)
        {
            return _scopeManager.IsExtensionNamespace(nspace);
        }

        internal bool IsExcludedNamespace(string nspace)
        {
            return _scopeManager.IsExcludedNamespace(nspace);
        }

        internal void PushLiteralScope()
        {
            PushNamespaceScope();
            string value = Input!.Navigator.GetAttribute(Atoms.Version, Atoms.UriXsl);
            if (value.Length != 0)
            {
                ForwardCompatibility = (value != "1.0");
            }
        }

        internal void PushNamespaceScope()
        {
            _scopeManager.PushScope();
            NavigatorInput input = Input!;

            if (input.MoveToFirstNamespace())
            {
                do
                {
                    _scopeManager.PushNamespace(input.LocalName, input.Value);
                }
                while (input.MoveToNextNamespace());
                input.ToParent();
            }
        }

        internal void PopScope()
        {
            _scopeManager.PopScope();
        }

        internal InputScopeManager CloneScopeManager()
        {
            return _scopeManager.Clone();
        }

        //
        // Variable support
        //

        internal int InsertVariable(VariableAction variable)
        {
            InputScope varScope;
            if (variable.IsGlobal)
            {
                Debug.Assert(_rootAction != null);
                varScope = _rootScope!;
            }
            else
            {
                Debug.Assert(_currentTemplate != null);
                Debug.Assert(variable.VarType == VariableType.LocalVariable || variable.VarType == VariableType.LocalParameter || variable.VarType == VariableType.WithParameter);
                varScope = _scopeManager.VariableScope;
            }

            VariableAction? oldVar = varScope.ResolveVariable(variable.Name!);
            if (oldVar != null)
            {
                // Other variable with this name is visible in this scope
                if (oldVar.IsGlobal)
                {
                    if (variable.IsGlobal)
                    {
                        // Global Vars replace each other base on import presidens odred
                        if (variable.Stylesheetid == oldVar.Stylesheetid)
                        {
                            // Both vars are in the same stylesheet
                            throw XsltException.Create(SR.Xslt_DupVarName, variable.NameStr);
                        }
                        else if (variable.Stylesheetid < oldVar.Stylesheetid)
                        {
                            // newly defined var is more importent
                            varScope.InsertVariable(variable);
                            return oldVar.VarKey;
                        }
                        else
                        {
                            // we egnore new variable
                            return InvalidQueryKey; // We didn't add this var, so doesn't matter what VarKey we return;
                        }
                    }
                    else
                    {
                        // local variable can shadow global
                    }
                }
                else
                {
                    // Local variable never can be "shadowed"
                    throw XsltException.Create(SR.Xslt_DupVarName, variable.NameStr);
                }
            }

            varScope.InsertVariable(variable);
            return _currentTemplate!.AllocateVariableSlot();
        }

        internal void AddNamespaceAlias(string StylesheetURI, NamespaceInfo AliasInfo)
        {
            if (_globalNamespaceAliasTable == null)
            {
                _globalNamespaceAliasTable = new Hashtable();
            }
            NamespaceInfo? duplicate = _globalNamespaceAliasTable[StylesheetURI] as NamespaceInfo;
            if (duplicate == null || AliasInfo.stylesheetId <= duplicate.stylesheetId)
            {
                _globalNamespaceAliasTable[StylesheetURI] = AliasInfo;
            }
        }

        internal bool IsNamespaceAlias(string StylesheetURI)
        {
            if (_globalNamespaceAliasTable == null)
            {
                return false;
            }
            return _globalNamespaceAliasTable.Contains(StylesheetURI);
        }

        internal NamespaceInfo? FindNamespaceAlias(string StylesheetURI)
        {
            if (_globalNamespaceAliasTable != null)
            {
                return (NamespaceInfo?)_globalNamespaceAliasTable[StylesheetURI];
            }
            return null;
        }

        internal string ResolveXmlNamespace(string prefix)
        {
            return _scopeManager.ResolveXmlNamespace(prefix);
        }

        internal string ResolveXPathNamespace(string prefix)
        {
            return _scopeManager.ResolveXPathNamespace(prefix);
        }

        internal string DefaultNamespace
        {
            get { return _scopeManager.DefaultNamespace; }
        }

        internal void InsertKey(XmlQualifiedName name, int MatchKey, int UseKey)
        {
            _rootAction.InsertKey(name, MatchKey, UseKey);
        }

        internal void AddDecimalFormat(XmlQualifiedName name, DecimalFormat formatinfo)
        {
            _rootAction.AddDecimalFormat(name, formatinfo);
        }

        //
        // Attribute parsing support
        //

        // This function is for processing optional attributes only.  In case of error
        // the value is ignored iff forwards-compatible mode is on.
        private string[]? ResolvePrefixes(string tokens)
        {
            if (tokens == null || tokens.Length == 0)
            {
                return null;
            }
            string[] nsList = XmlConvert.SplitString(tokens);

            try
            {
                for (int idx = 0; idx < nsList.Length; idx++)
                {
                    string prefix = nsList[idx];
                    nsList[idx] = _scopeManager.ResolveXmlNamespace(prefix == "#default" ? string.Empty : prefix);
                }
            }
            catch (XsltException)
            {
                // The only exception here might be SR.Xslt_InvalidPrefix
                if (!ForwardCompatibility)
                {
                    // Rethrow the exception if we're not in forwards-compatible mode
                    throw;
                }
                // Ignore the whole list in forwards-compatible mode
                return null;
            }
            return nsList;
        }

        internal bool GetYesNo(string value)
        {
            Debug.Assert(value != null);
            Debug.Assert((object)value == (object)Input!.Value); // this is always true. Why we passing value to this function.
            if (value == "yes")
            {
                return true;
            }
            if (value == "no")
            {
                return false;
            }
            throw XsltException.Create(SR.Xslt_InvalidAttrValue, Input.LocalName, value);
        }

        internal string GetSingleAttribute(string attributeAtom)
        {
            NavigatorInput input = Input!;
            string element = input.LocalName;
            string? value = null;

            if (input.MoveToFirstAttribute())
            {
                do
                {
                    string nspace = input.NamespaceURI;
                    string name = input.LocalName;

                    if (nspace.Length != 0) continue;

                    if (Ref.Equal(name, attributeAtom))
                    {
                        value = input.Value;
                    }
                    else
                    {
                        if (!this.ForwardCompatibility)
                        {
                            throw XsltException.Create(SR.Xslt_InvalidAttribute, name, element);
                        }
                    }
                }
                while (input.MoveToNextAttribute());
                input.ToParent();
            }

            if (value == null)
            {
                throw XsltException.Create(SR.Xslt_MissingAttribute, attributeAtom);
            }
            return value;
        }

        internal XmlQualifiedName CreateXPathQName(string qname)
        {
            string prefix, local;
            PrefixQName.ParseQualifiedName(qname, out prefix, out local);

            return new XmlQualifiedName(local, _scopeManager.ResolveXPathNamespace(prefix));
        }

        internal XmlQualifiedName CreateXmlQName(string qname)
        {
            string prefix, local;
            PrefixQName.ParseQualifiedName(qname, out prefix, out local);

            return new XmlQualifiedName(local, _scopeManager.ResolveXmlNamespace(prefix));
        }

        //
        // Input documents management
        //

        internal static XPathDocument LoadDocument(XmlTextReaderImpl reader)
        {
            reader.EntityHandling = EntityHandling.ExpandEntities;
            reader.XmlValidatingReaderCompatibilityMode = true;
            try
            {
                return new XPathDocument(reader, XmlSpace.Preserve);
            }
            finally
            {
                reader.Close();
            }
        }

        private void AddDocumentURI(string href)
        {
            Debug.Assert(!_documentURIs.Contains(href), "Circular references must be checked while processing xsl:include and xsl:import");
            _documentURIs.Add(href, null);
        }

        private void RemoveDocumentURI(string href)
        {
            Debug.Assert(_documentURIs.Contains(href), "Attempt to remove href that was not added");
            _documentURIs.Remove(href);
        }

        internal bool IsCircularReference(string href)
        {
            return _documentURIs.Contains(href);
        }

        internal Uri ResolveUri(string relativeUri)
        {
            Debug.Assert(_xmlResolver != null);
            string baseUri = this.Input!.BaseURI;
            Uri uri = _xmlResolver.ResolveUri((baseUri.Length != 0) ? _xmlResolver.ResolveUri(null, baseUri) : null, relativeUri);
            if (uri == null)
            {
                throw XsltException.Create(SR.Xslt_CantResolve, relativeUri);
            }
            return uri;
        }

        internal NavigatorInput ResolveDocument(Uri absoluteUri)
        {
            Debug.Assert(_xmlResolver != null);
            object? input = _xmlResolver.GetEntity(absoluteUri, null, null);
            string resolved = absoluteUri.ToString();

            if (input is Stream)
            {
                XmlTextReaderImpl tr = new XmlTextReaderImpl(resolved, (Stream)input);
                {
                    tr.XmlResolver = _xmlResolver;
                }
                // reader is closed by Compiler.LoadDocument()
                return new NavigatorInput(Compiler.LoadDocument(tr).CreateNavigator(), resolved, _rootScope);
            }
            else if (input is XPathNavigator)
            {
                return new NavigatorInput((XPathNavigator)input, resolved, _rootScope);
            }
            else
            {
                throw XsltException.Create(SR.Xslt_CantResolve, resolved);
            }
        }

        internal void PushInputDocument(NavigatorInput newInput)
        {
            Debug.Assert(newInput != null);
            string inputUri = newInput.Href;

            AddDocumentURI(inputUri);

            newInput.Next = _input;
            _input = newInput;
            _atoms = _input.Atoms;
            _scopeManager = _input.InputScopeManager;
        }

        internal void PopInputDocument()
        {
            Debug.Assert(_input != null);
            Debug.Assert(_input.Atoms == _atoms);

            NavigatorInput lastInput = _input;

            _input = lastInput.Next!;
            lastInput.Next = null;

            if (_input != null)
            {
                _atoms = _input.Atoms;
                _scopeManager = _input.InputScopeManager;
            }
            else
            {
                _atoms = null;
                _scopeManager = null!;
            }

            RemoveDocumentURI(lastInput.Href);
            lastInput.Close();
        }

        //
        // Stylesheet management
        //

        internal void PushStylesheet(Stylesheet stylesheet)
        {
            _stylesheets ??= new Stack<Stylesheet>();
            _stylesheets.Push(stylesheet);
            this.stylesheet = stylesheet;
        }

        internal Stylesheet PopStylesheet()
        {
            Debug.Assert(this.stylesheet == _stylesheets!.Peek());
            Stylesheet stylesheet = _stylesheets.Pop();
            this.stylesheet = _stylesheets.Peek();
            return stylesheet;
        }

        //
        // Attribute-Set management
        //

        internal void AddAttributeSet(AttributeSetAction attributeSet)
        {
            Debug.Assert(this.stylesheet == _stylesheets!.Peek());
            this.stylesheet.AddAttributeSet(attributeSet);
        }

        //
        // Template management
        //

        internal void AddTemplate(TemplateAction template)
        {
            Debug.Assert(this.stylesheet == _stylesheets!.Peek());
            this.stylesheet.AddTemplate(template);
        }

        internal void BeginTemplate(TemplateAction template)
        {
            Debug.Assert(_currentTemplate != null);
            _currentTemplate = template;
            _currentMode = template.Mode;
            this.CanHaveApplyImports = template.MatchKey != Compiler.InvalidQueryKey;
        }

        internal void EndTemplate()
        {
            Debug.Assert(_currentTemplate != null);
            _currentTemplate = _rootAction;
        }

        internal XmlQualifiedName? CurrentMode
        {
            get { return _currentMode; }
        }

        //
        // Query management
        //
        internal int AddQuery(string xpathQuery)
        {
            return AddQuery(xpathQuery, /*allowVars:*/true, /*allowKey*/true, false);
        }

        internal int AddQuery(string xpathQuery, bool allowVar, bool allowKey, bool isPattern)
        {
            Debug.Assert(_queryStore != null);

            CompiledXpathExpr expr;
            try
            {
                expr = new CompiledXpathExpr(
                    (isPattern
                        ? _queryBuilder.BuildPatternQuery(xpathQuery, allowVar, allowKey)
                        : _queryBuilder.Build(xpathQuery, allowVar, allowKey)
                    ),
                    xpathQuery,
                    false
                );
            }
            catch (XPathException e)
            {
                if (!ForwardCompatibility)
                {
                    throw XsltException.Create(SR.Xslt_InvalidXPath, new string[] { xpathQuery }, e);
                }
                expr = new ErrorXPathExpression(xpathQuery, this.Input!.BaseURI, this.Input.LineNumber, this.Input.LinePosition);
            }
            _queryStore.Add(new TheQuery(expr, _scopeManager));
            return _queryStore.Count - 1;
        }

        internal int AddStringQuery(string xpathQuery)
        {
            string modifiedQuery = XmlCharType.IsOnlyWhitespace(xpathQuery) ? xpathQuery : $"string({xpathQuery})";
            return AddQuery(modifiedQuery);
        }

        internal int AddBooleanQuery(string xpathQuery)
        {
            string modifiedQuery = XmlCharType.IsOnlyWhitespace(xpathQuery) ? xpathQuery : $"boolean({xpathQuery})";
            return AddQuery(modifiedQuery);
        }

        //
        // Script support
        //
        private readonly Hashtable[] _typeDeclsByLang = new Hashtable[] { new Hashtable(), new Hashtable(), new Hashtable() };

        internal void AddScript(string source, ScriptingLanguage lang, string ns, string fileName, int lineNumber)
        {
            ValidateExtensionNamespace(ns);

            for (ScriptingLanguage langTmp = ScriptingLanguage.JScript; langTmp <= ScriptingLanguage.CSharp; langTmp++)
            {
                Hashtable typeDecls = _typeDeclsByLang[(int)langTmp];
                if (lang == langTmp)
                {
                    throw new PlatformNotSupportedException(SR.CompilingScriptsNotSupported);
                }
                else if (typeDecls.Contains(ns))
                {
                    throw XsltException.Create(SR.Xslt_ScriptMixedLanguages, ns);
                }
            }
        }

        private static void ValidateExtensionNamespace(string nsUri)
        {
            if (nsUri.Length == 0 || nsUri == XmlReservedNs.NsXslt)
            {
                throw XsltException.Create(SR.Xslt_InvalidExtensionNamespace);
            }
            XmlConvert.ToUri(nsUri);
        }

        public string GetNsAlias(ref string prefix)
        {
            Debug.Assert(
                Ref.Equal(_input!.LocalName, _input.Atoms.StylesheetPrefix) ||
                Ref.Equal(_input.LocalName, _input.Atoms.ResultPrefix)
            );
            if (prefix == "#default")
            {
                prefix = string.Empty;
                return this.DefaultNamespace;
            }
            else
            {
                if (!PrefixQName.ValidatePrefix(prefix))
                {
                    throw XsltException.Create(SR.Xslt_InvalidAttrValue, _input.LocalName, prefix);
                }
                return this.ResolveXPathNamespace(prefix);
            }
        }

        // AVT's compilation.
        // CompileAvt() returns ArrayList of TextEvent & AvtEvent

        private static void getTextLex(string avt, ref int start, StringBuilder lex)
        {
            Debug.Assert(avt.Length != start, "Empty string not supposed here");
            Debug.Assert(lex.Length == 0, "Builder shoud to be reset here");
            int avtLength = avt.Length;
            int i;
            for (i = start; i < avtLength; i++)
            {
                char ch = avt[i];
                if (ch == '{')
                {
                    if (i + 1 < avtLength && avt[i + 1] == '{')
                    { // "{{"
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }
                else if (ch == '}')
                {
                    if (i + 1 < avtLength && avt[i + 1] == '}')
                    { // "}}"
                        i++;
                    }
                    else
                    {
                        throw XsltException.Create(SR.Xslt_SingleRightAvt, avt);
                    }
                }
                lex.Append(ch);
            }
            start = i;
        }

        private static void getXPathLex(string avt, ref int start, StringBuilder lex)
        {
            // AVT parser states
            const int InExp = 0;      // Inside AVT expression
            const int InLiteralA = 1;      // Inside literal in expression, apostrophe delimited
            const int InLiteralQ = 2;      // Inside literal in expression, quote delimited
            Debug.Assert(avt.Length != start, "Empty string not supposed here");
            Debug.Assert(lex.Length == 0, "Builder shoud to be reset here");
            Debug.Assert(avt[start] == '{', "We calling getXPathLex() only after we realy meet {");
            int avtLength = avt.Length;
            int state = InExp;
            for (int i = start + 1; i < avtLength; i++)
            {
                char ch = avt[i];
                switch (state)
                {
                    case InExp:
                        switch (ch)
                        {
                            case '{':
                                throw XsltException.Create(SR.Xslt_NestedAvt, avt);
                            case '}':
                                i++; // include '}'
                                if (i == start + 2)
                                { // empty XPathExpresion
                                    throw XsltException.Create(SR.Xslt_EmptyAvtExpr, avt);
                                }
                                lex.Append(avt, start + 1, i - start - 2); // avt without {}
                                start = i;
                                return;
                            case '\'':
                                state = InLiteralA;
                                break;
                            case '"':
                                state = InLiteralQ;
                                break;
                        }
                        break;
                    case InLiteralA:
                        if (ch == '\'')
                        {
                            state = InExp;
                        }
                        break;
                    case InLiteralQ:
                        if (ch == '"')
                        {
                            state = InExp;
                        }
                        break;
                }
            }

            // if we meet end of string before } we have an error
            throw XsltException.Create(state == InExp ? SR.Xslt_OpenBracesAvt : SR.Xslt_OpenLiteralAvt, avt);
        }

        private static bool GetNextAvtLex(string avt, ref int start, StringBuilder lex, out bool isAvt)
        {
            Debug.Assert(start <= avt.Length);
#if DEBUG
            int saveStart = start;
#endif
            isAvt = false;
            if (start == avt.Length)
            {
                return false;
            }
            lex.Length = 0;
            getTextLex(avt, ref start, lex);
            if (lex.Length == 0)
            {
                isAvt = true;
                getXPathLex(avt, ref start, lex);
            }
#if DEBUG
            Debug.Assert(saveStart < start, "We have to read something. Otherwise it's dead loop.");
#endif
            return true;
        }

        internal ArrayList CompileAvt(string avtText, out bool constant)
        {
            Debug.Assert(avtText != null);
            ArrayList list = new ArrayList();

            constant = true;
            /* Parse input.Value as AVT */
            {
                int pos = 0;
                bool isAvt;
                while (GetNextAvtLex(avtText, ref pos, this.AvtStringBuilder, out isAvt))
                {
                    string lex = this.AvtStringBuilder.ToString();
                    if (isAvt)
                    {
                        list.Add(new AvtEvent(this.AddStringQuery(lex)));
                        constant = false;
                    }
                    else
                    {
                        list.Add(new TextEvent(lex));
                    }
                }
            }
            Debug.Assert(!constant || list.Count <= 1, "We can't have more then 1 text event if now {avt} found");
            return list;
        }

        internal ArrayList CompileAvt(string avtText)
        {
            return CompileAvt(avtText, out _);
        }

        // Compiler is a class factory for some actions:
        // CompilerDbg override all this methods:

        public ApplyImportsAction CreateApplyImportsAction()
        {
            ApplyImportsAction action = new ApplyImportsAction();
            action.Compile(this);
            return action;
        }

        public ApplyTemplatesAction CreateApplyTemplatesAction()
        {
            ApplyTemplatesAction action = new ApplyTemplatesAction();
            action.Compile(this);
            return action;
        }

        public AttributeAction CreateAttributeAction()
        {
            AttributeAction action = new AttributeAction();
            action.Compile(this);
            return action;
        }

        public AttributeSetAction CreateAttributeSetAction()
        {
            AttributeSetAction action = new AttributeSetAction();
            action.Compile(this);
            return action;
        }

        public CallTemplateAction CreateCallTemplateAction()
        {
            CallTemplateAction action = new CallTemplateAction();
            action.Compile(this);
            return action;
        }

        public ChooseAction CreateChooseAction()
        {//!!! don't need to be here
            ChooseAction action = new ChooseAction();
            action.Compile(this);
            return action;
        }

        public CommentAction CreateCommentAction()
        {
            CommentAction action = new CommentAction();
            action.Compile(this);
            return action;
        }

        public CopyAction CreateCopyAction()
        {
            CopyAction action = new CopyAction();
            action.Compile(this);
            return action;
        }

        public CopyOfAction CreateCopyOfAction()
        {
            CopyOfAction action = new CopyOfAction();
            action.Compile(this);
            return action;
        }

        public ElementAction CreateElementAction()
        {
            ElementAction action = new ElementAction();
            action.Compile(this);
            return action;
        }

        public ForEachAction CreateForEachAction()
        {
            ForEachAction action = new ForEachAction();
            action.Compile(this);
            return action;
        }

        public IfAction CreateIfAction(IfAction.ConditionType type)
        {
            IfAction action = new IfAction(type);
            action.Compile(this);
            return action;
        }

        public MessageAction CreateMessageAction()
        {
            MessageAction action = new MessageAction();
            action.Compile(this);
            return action;
        }

        public NewInstructionAction CreateNewInstructionAction()
        {
            NewInstructionAction action = new NewInstructionAction();
            action.Compile(this);
            return action;
        }

        public NumberAction CreateNumberAction()
        {
            NumberAction action = new NumberAction();
            action.Compile(this);
            return action;
        }

        public ProcessingInstructionAction CreateProcessingInstructionAction()
        {
            ProcessingInstructionAction action = new ProcessingInstructionAction();
            action.Compile(this);
            return action;
        }

        public void CreateRootAction()
        {
            this.RootAction = new RootAction();
            this.RootAction.Compile(this);
        }

        public SortAction CreateSortAction()
        {
            SortAction action = new SortAction();
            action.Compile(this);
            return action;
        }

        public TemplateAction CreateTemplateAction()
        {
            TemplateAction action = new TemplateAction();
            action.Compile(this);
            return action;
        }

        public TemplateAction CreateSingleTemplateAction()
        {
            TemplateAction action = new TemplateAction();
            action.CompileSingle(this);
            return action;
        }

        public TextAction CreateTextAction()
        {
            TextAction action = new TextAction();
            action.Compile(this);
            return action;
        }

        public UseAttributeSetsAction CreateUseAttributeSetsAction()
        {
            UseAttributeSetsAction action = new UseAttributeSetsAction();
            action.Compile(this);
            return action;
        }

        public ValueOfAction CreateValueOfAction()
        {
            ValueOfAction action = new ValueOfAction();
            action.Compile(this);
            return action;
        }

        public VariableAction? CreateVariableAction(VariableType type)
        {
            VariableAction action = new VariableAction(type);
            action.Compile(this);
            if (action.VarKey != InvalidQueryKey)
            {
                return action;
            }
            else
            {
                return null;
            }
        }

        public WithParamAction CreateWithParamAction()
        {
            WithParamAction action = new WithParamAction();
            action.Compile(this);
            return action;
        }

        // Compiler is a class factory for some events:
        // CompilerDbg override all this methods:

        public BeginEvent CreateBeginEvent()
        {
            return new BeginEvent(this);
        }

        public TextEvent CreateTextEvent()
        {
            return new TextEvent(this);
        }

        public XsltException UnexpectedKeyword()
        {
            XPathNavigator nav = this.Input!.Navigator.Clone();
            string thisName = nav.Name;
            nav.MoveToParent();
            string parentName = nav.Name;
            return XsltException.Create(SR.Xslt_UnexpectedKeyword, thisName, parentName);
        }

        internal sealed class ErrorXPathExpression : CompiledXpathExpr
        {
            private readonly string _baseUri;
            private readonly int _lineNumber, _linePosition;
            public ErrorXPathExpression(string expression, string baseUri, int lineNumber, int linePosition)
                : base(null!, expression, false)
            {
                _baseUri = baseUri;
                _lineNumber = lineNumber;
                _linePosition = linePosition;
            }

            public override XPathExpression Clone() { return this; }
            public override void CheckErrors()
            {
                throw new XsltException(SR.Xslt_InvalidXPath, new string[] { Expression }, _baseUri, _lineNumber, _linePosition, null);
            }
        }
    }
}
