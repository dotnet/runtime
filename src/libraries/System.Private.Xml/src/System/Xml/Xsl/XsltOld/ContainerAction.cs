// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Globalization;
    using System.Xml;
    using System.Xml.XPath;
    using System.Xml.Xsl.Runtime;
    using MS.Internal.Xml.XPath;
    using System.Collections;
    using System.Runtime.Versioning;
    using System.Diagnostics.CodeAnalysis;

    internal sealed class NamespaceInfo
    {
        internal string? prefix;
        internal string? nameSpace;
        internal int stylesheetId;

        internal NamespaceInfo(string? prefix, string? nameSpace, int stylesheetId)
        {
            this.prefix = prefix;
            this.nameSpace = nameSpace;
            this.stylesheetId = stylesheetId;
        }
    }

    internal class ContainerAction : CompiledAction
    {
        internal ArrayList? containedActions;
        internal CopyCodeAction? lastCopyCodeAction; // non null if last action is CopyCodeAction;

        private int _maxid;

        // Local execution states
        protected const int ProcessingChildren = 1;

        internal override void Compile(Compiler compiler)
        {
            throw new NotImplementedException();
        }

        internal static void CompileStylesheetAttributes(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;
            string element = input.LocalName;
            string? badAttribute = null;
            string? version = null;

            if (input.MoveToFirstAttribute())
            {
                do
                {
                    string nspace = input.NamespaceURI;
                    string name = input.LocalName;

                    if (nspace.Length != 0) continue;

                    if (Ref.Equal(name, input.Atoms.Version))
                    {
                        version = input.Value;
                        if (1 <= XmlConvert.ToXPathDouble(version))
                        {
                            compiler.ForwardCompatibility = (version != "1.0");
                        }
                        else
                        {
                            // XmlConvert.ToXPathDouble(version) an be NaN!
                            if (!compiler.ForwardCompatibility)
                            {
                                throw XsltException.Create(SR.Xslt_InvalidAttrValue, "version", version);
                            }
                        }
                    }
                    else if (Ref.Equal(name, input.Atoms.ExtensionElementPrefixes))
                    {
                        compiler.InsertExtensionNamespace(input.Value);
                    }
                    else if (Ref.Equal(name, input.Atoms.ExcludeResultPrefixes))
                    {
                        compiler.InsertExcludedNamespace(input.Value);
                    }
                    else if (Ref.Equal(name, input.Atoms.Id))
                    {
                        // Do nothing here.
                    }
                    else
                    {
                        // We can have version attribute later. For now remember this attribute and continue
                        badAttribute = name;
                    }
                }
                while (input.MoveToNextAttribute());
                input.ToParent();
            }

            if (version == null)
            {
                throw XsltException.Create(SR.Xslt_MissingAttribute, "version");
            }

            if (badAttribute != null && !compiler.ForwardCompatibility)
            {
                throw XsltException.Create(SR.Xslt_InvalidAttribute, badAttribute, element);
            }
        }

        internal static void CompileSingleTemplate(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;

            //
            // find mandatory version attribute and launch compilation of single template
            //

            string? version = null;

            if (input.MoveToFirstAttribute())
            {
                do
                {
                    string nspace = input.NamespaceURI;
                    string name = input.LocalName;

                    if (Ref.Equal(nspace, input.Atoms.UriXsl) &&
                        Ref.Equal(name, input.Atoms.Version))
                    {
                        version = input.Value;
                    }
                }
                while (input.MoveToNextAttribute());
                input.ToParent();
            }

            if (version == null)
            {
                if (Ref.Equal(input.LocalName, input.Atoms.Stylesheet) &&
                    input.NamespaceURI == XmlReservedNs.NsWdXsl)
                {
                    throw XsltException.Create(SR.Xslt_WdXslNamespace);
                }
                throw XsltException.Create(SR.Xslt_WrongStylesheetElement);
            }

            compiler.AddTemplate(compiler.CreateSingleTemplateAction());
        }

        /*
         * CompileTopLevelElements
         */
        protected void CompileDocument(Compiler compiler, bool inInclude)
        {
            NavigatorInput input = compiler.Input;

            // SkipToElement :
            while (input.NodeType != XPathNodeType.Element)
            {
                if (!compiler.Advance())
                {
                    throw XsltException.Create(SR.Xslt_WrongStylesheetElement);
                }
            }

            Debug.Assert(compiler.Input.NodeType == XPathNodeType.Element);
            if (Ref.Equal(input.NamespaceURI, input.Atoms.UriXsl))
            {
                if (
                    !Ref.Equal(input.LocalName, input.Atoms.Stylesheet) &&
                    !Ref.Equal(input.LocalName, input.Atoms.Transform)
                )
                {
                    throw XsltException.Create(SR.Xslt_WrongStylesheetElement);
                }
                compiler.PushNamespaceScope();
                CompileStylesheetAttributes(compiler);
                CompileTopLevelElements(compiler);
                if (!inInclude)
                {
                    CompileImports(compiler);
                }
            }
            else
            {
                // single template
                compiler.PushLiteralScope();
                CompileSingleTemplate(compiler);
            }

            compiler.PopScope();
        }

        internal Stylesheet CompileImport(Compiler compiler, Uri uri, int id)
        {
            NavigatorInput input = compiler.ResolveDocument(uri);
            compiler.PushInputDocument(input);

            try
            {
                compiler.PushStylesheet(new Stylesheet());
                compiler.Stylesheetid = id;
                CompileDocument(compiler, /*inInclude*/ false);
            }
            catch (XsltCompileException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new XsltCompileException(e, input.BaseURI, input.LineNumber, input.LinePosition);
            }
            finally
            {
                compiler.PopInputDocument();
            }
            return compiler.PopStylesheet();
        }

        private void CompileImports(Compiler compiler)
        {
            ArrayList imports = compiler.CompiledStylesheet!.Imports;
            // We can't reverce imports order. Template lookup relyes on it after compilation
            int saveStylesheetId = compiler.Stylesheetid;
            for (int i = imports.Count - 1; 0 <= i; i--)
            {   // Imports should be compiled in reverse order
                Uri? uri = imports[i] as Uri;
                Debug.Assert(uri != null);
                imports[i] = CompileImport(compiler, uri, ++_maxid);
            }
            compiler.Stylesheetid = saveStylesheetId;
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        private void CompileInclude(Compiler compiler)
        {
            Uri uri = compiler.ResolveUri(compiler.GetSingleAttribute(compiler.Input.Atoms.Href));
            string resolved = uri.ToString();
            if (compiler.IsCircularReference(resolved))
            {
                throw XsltException.Create(SR.Xslt_CircularInclude, resolved);
            }

            NavigatorInput input = compiler.ResolveDocument(uri);
            compiler.PushInputDocument(input);

            try
            {
                CompileDocument(compiler, /*inInclude*/ true);
            }
            catch (XsltCompileException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new XsltCompileException(e, input.BaseURI, input.LineNumber, input.LinePosition);
            }
            finally
            {
                compiler.PopInputDocument();
            }
            CheckEmpty(compiler);
        }

        internal static void CompileNamespaceAlias(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;
            string element = input.LocalName;
            string? namespace1 = null, namespace2 = null;
            string? prefix1 = null, prefix2 = null;
            if (input.MoveToFirstAttribute())
            {
                do
                {
                    string nspace = input.NamespaceURI;
                    string name = input.LocalName;

                    if (nspace.Length != 0) continue;

                    if (Ref.Equal(name, input.Atoms.StylesheetPrefix))
                    {
                        prefix1 = input.Value;
                        namespace1 = compiler.GetNsAlias(ref prefix1);
                    }
                    else if (Ref.Equal(name, input.Atoms.ResultPrefix))
                    {
                        prefix2 = input.Value;
                        namespace2 = compiler.GetNsAlias(ref prefix2);
                    }
                    else
                    {
                        if (!compiler.ForwardCompatibility)
                        {
                            throw XsltException.Create(SR.Xslt_InvalidAttribute, name, element);
                        }
                    }
                }
                while (input.MoveToNextAttribute());
                input.ToParent();
            }

            CheckRequiredAttribute(compiler, namespace1, "stylesheet-prefix");
            CheckRequiredAttribute(compiler, namespace2, "result-prefix");
            CheckEmpty(compiler);

            //String[] resultarray = { prefix2, namespace2 };
            compiler.AddNamespaceAlias(namespace1!, new NamespaceInfo(prefix2, namespace2, compiler.Stylesheetid));
        }

        internal static void CompileKey(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;
            string element = input.LocalName;
            int MatchKey = Compiler.InvalidQueryKey;
            int UseKey = Compiler.InvalidQueryKey;

            XmlQualifiedName? Name = null;
            if (input.MoveToFirstAttribute())
            {
                do
                {
                    string nspace = input.NamespaceURI;
                    string name = input.LocalName;
                    string value = input.Value;

                    if (nspace.Length != 0) continue;

                    if (Ref.Equal(name, input.Atoms.Name))
                    {
                        Name = compiler.CreateXPathQName(value);
                    }
                    else if (Ref.Equal(name, input.Atoms.Match))
                    {
                        MatchKey = compiler.AddQuery(value, /*allowVars:*/false, /*allowKey*/false, /*pattern*/true);
                    }
                    else if (Ref.Equal(name, input.Atoms.Use))
                    {
                        UseKey = compiler.AddQuery(value, /*allowVars:*/false, /*allowKey*/false, /*pattern*/false);
                    }
                    else
                    {
                        if (!compiler.ForwardCompatibility)
                        {
                            throw XsltException.Create(SR.Xslt_InvalidAttribute, name, element);
                        }
                    }
                }
                while (input.MoveToNextAttribute());
                input.ToParent();
            }

            CheckRequiredAttribute(compiler, MatchKey != Compiler.InvalidQueryKey, "match");
            CheckRequiredAttribute(compiler, UseKey != Compiler.InvalidQueryKey, "use");
            CheckRequiredAttribute(compiler, Name != null, "name");
            // It is a breaking change to check for emptiness, SQLBUDT 324364
            //CheckEmpty(compiler);

            compiler.InsertKey(Name!, MatchKey, UseKey);
        }

        protected static void CompileDecimalFormat(Compiler compiler)
        {
            NumberFormatInfo info = new NumberFormatInfo();
            DecimalFormat format = new DecimalFormat(info, '#', '0', ';');
            XmlQualifiedName? Name = null;
            NavigatorInput input = compiler.Input;
            if (input.MoveToFirstAttribute())
            {
                do
                {
                    if (input.Prefix.Length != 0) continue;

                    string name = input.LocalName;
                    string value = input.Value;

                    if (Ref.Equal(name, input.Atoms.Name))
                    {
                        Name = compiler.CreateXPathQName(value);
                    }
                    else if (Ref.Equal(name, input.Atoms.DecimalSeparator))
                    {
                        info.NumberDecimalSeparator = value;
                    }
                    else if (Ref.Equal(name, input.Atoms.GroupingSeparator))
                    {
                        info.NumberGroupSeparator = value;
                    }
                    else if (Ref.Equal(name, input.Atoms.Infinity))
                    {
                        info.PositiveInfinitySymbol = value;
                    }
                    else if (Ref.Equal(name, input.Atoms.MinusSign))
                    {
                        info.NegativeSign = value;
                    }
                    else if (Ref.Equal(name, input.Atoms.NaN))
                    {
                        info.NaNSymbol = value;
                    }
                    else if (Ref.Equal(name, input.Atoms.Percent))
                    {
                        info.PercentSymbol = value;
                    }
                    else if (Ref.Equal(name, input.Atoms.PerMille))
                    {
                        info.PerMilleSymbol = value;
                    }
                    else if (Ref.Equal(name, input.Atoms.Digit))
                    {
                        if (CheckAttribute(value.Length == 1, compiler))
                        {
                            format.digit = value[0];
                        }
                    }
                    else if (Ref.Equal(name, input.Atoms.ZeroDigit))
                    {
                        if (CheckAttribute(value.Length == 1, compiler))
                        {
                            format.zeroDigit = value[0];
                        }
                    }
                    else if (Ref.Equal(name, input.Atoms.PatternSeparator))
                    {
                        if (CheckAttribute(value.Length == 1, compiler))
                        {
                            format.patternSeparator = value[0];
                        }
                    }
                }
                while (input.MoveToNextAttribute());
                input.ToParent();
            }
            info.NegativeInfinitySymbol = string.Concat(info.NegativeSign, info.PositiveInfinitySymbol);
            if (Name == null)
            {
                Name = new XmlQualifiedName();
            }
            compiler.AddDecimalFormat(Name, format);
            CheckEmpty(compiler);
        }

        internal static bool CheckAttribute(bool valid, Compiler compiler)
        {
            if (!valid)
            {
                if (!compiler.ForwardCompatibility)
                {
                    throw XsltException.Create(SR.Xslt_InvalidAttrValue, compiler.Input.LocalName, compiler.Input.Value);
                }
                return false;
            }
            return true;
        }

        protected static void CompileSpace(Compiler compiler, bool preserve)
        {
            string value = compiler.GetSingleAttribute(compiler.Input.Atoms.Elements);
            string[] elements = XmlConvert.SplitString(value);
            for (int i = 0; i < elements.Length; i++)
            {
                double defaultPriority = NameTest(elements[i]);
                compiler.CompiledStylesheet!.AddSpace(compiler, elements[i], defaultPriority, preserve);
            }
            CheckEmpty(compiler);
        }

        private static double NameTest(string name)
        {
            if (name == "*")
            {
                return -0.5;
            }
            int idx = name.Length - 2;
            if (0 <= idx && name[idx] == ':' && name[idx + 1] == '*')
            {
                if (!PrefixQName.ValidatePrefix(name.Substring(0, idx)))
                {
                    throw XsltException.Create(SR.Xslt_InvalidAttrValue, "elements", name);
                }
                return -0.25;
            }
            else
            {
                PrefixQName.ParseQualifiedName(name, out _, out _);
                return 0;
            }
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        protected void CompileTopLevelElements(Compiler compiler)
        {
            // Navigator positioned at parent root, need to move to child and then back
            if (compiler.Recurse() == false)
            {
                return;
            }

            NavigatorInput input = compiler.Input;
            bool notFirstElement = false;
            do
            {
                switch (input.NodeType)
                {
                    case XPathNodeType.Element:
                        string name = input.LocalName;
                        string nspace = input.NamespaceURI;

                        if (Ref.Equal(nspace, input.Atoms.UriXsl))
                        {
                            if (Ref.Equal(name, input.Atoms.Import))
                            {
                                if (notFirstElement)
                                {
                                    throw XsltException.Create(SR.Xslt_NotFirstImport);
                                }
                                // We should compile imports in reverse order after all toplevel elements.
                                // remember it now and return to it in CompileImpoorts();
                                Uri uri = compiler.ResolveUri(compiler.GetSingleAttribute(compiler.Input.Atoms.Href));
                                string resolved = uri.ToString();
                                if (compiler.IsCircularReference(resolved))
                                {
                                    throw XsltException.Create(SR.Xslt_CircularInclude, resolved);
                                }
                                compiler.CompiledStylesheet!.Imports.Add(uri);
                                CheckEmpty(compiler);
                            }
                            else if (Ref.Equal(name, input.Atoms.Include))
                            {
                                notFirstElement = true;
                                CompileInclude(compiler);
                            }
                            else
                            {
                                notFirstElement = true;
                                compiler.PushNamespaceScope();
                                if (Ref.Equal(name, input.Atoms.StripSpace))
                                {
                                    CompileSpace(compiler, false);
                                }
                                else if (Ref.Equal(name, input.Atoms.PreserveSpace))
                                {
                                    CompileSpace(compiler, true);
                                }
                                else if (Ref.Equal(name, input.Atoms.Output))
                                {
                                    CompileOutput(compiler);
                                }
                                else if (Ref.Equal(name, input.Atoms.Key))
                                {
                                    CompileKey(compiler);
                                }
                                else if (Ref.Equal(name, input.Atoms.DecimalFormat))
                                {
                                    CompileDecimalFormat(compiler);
                                }
                                else if (Ref.Equal(name, input.Atoms.NamespaceAlias))
                                {
                                    CompileNamespaceAlias(compiler);
                                }
                                else if (Ref.Equal(name, input.Atoms.AttributeSet))
                                {
                                    compiler.AddAttributeSet(compiler.CreateAttributeSetAction());
                                }
                                else if (Ref.Equal(name, input.Atoms.Variable))
                                {
                                    VariableAction? action = compiler.CreateVariableAction(VariableType.GlobalVariable);
                                    if (action != null)
                                    {
                                        AddAction(action);
                                    }
                                }
                                else if (Ref.Equal(name, input.Atoms.Param))
                                {
                                    VariableAction? action = compiler.CreateVariableAction(VariableType.GlobalParameter);
                                    if (action != null)
                                    {
                                        AddAction(action);
                                    }
                                }
                                else if (Ref.Equal(name, input.Atoms.Template))
                                {
                                    compiler.AddTemplate(compiler.CreateTemplateAction());
                                }
                                else
                                {
                                    if (!compiler.ForwardCompatibility)
                                    {
                                        throw compiler.UnexpectedKeyword();
                                    }
                                }
                                compiler.PopScope();
                            }
                        }
                        else if (nspace == input.Atoms.UrnMsxsl && name == input.Atoms.Script)
                        {
                            AddScript(compiler);
                        }
                        else
                        {
                            if (nspace.Length == 0)
                            {
                                throw XsltException.Create(SR.Xslt_NullNsAtTopLevel, input.Name);
                            }
                            // Ignoring non-recognized namespace per XSLT spec 2.2
                        }
                        break;

                    case XPathNodeType.ProcessingInstruction:
                    case XPathNodeType.Comment:
                    case XPathNodeType.Whitespace:
                    case XPathNodeType.SignificantWhitespace:
                        break;

                    default:
                        throw XsltException.Create(SR.Xslt_InvalidContents, "stylesheet");
                }
            }
            while (compiler.Advance());

            compiler.ToParent();
        }

        protected void CompileTemplate(Compiler compiler)
        {
            do
            {
                CompileOnceTemplate(compiler);
            }
            while (compiler.Advance());
        }

        protected void CompileOnceTemplate(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;

            if (input.NodeType == XPathNodeType.Element)
            {
                string nspace = input.NamespaceURI;

                if (Ref.Equal(nspace, input.Atoms.UriXsl))
                {
                    compiler.PushNamespaceScope();
                    CompileInstruction(compiler);
                    compiler.PopScope();
                }
                else
                {
                    compiler.PushLiteralScope();
                    compiler.InsertExtensionNamespace();
                    if (compiler.IsExtensionNamespace(nspace))
                    {
                        AddAction(compiler.CreateNewInstructionAction());
                    }
                    else
                    {
                        CompileLiteral(compiler);
                    }
                    compiler.PopScope();
                }
            }
            else
            {
                CompileLiteral(compiler);
            }
        }

        private void CompileInstruction(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;
            CompiledAction? action;

            Debug.Assert(Ref.Equal(input.NamespaceURI, input.Atoms.UriXsl));

            string name = input.LocalName;

            if (Ref.Equal(name, input.Atoms.ApplyImports))
            {
                action = compiler.CreateApplyImportsAction();
            }
            else if (Ref.Equal(name, input.Atoms.ApplyTemplates))
            {
                action = compiler.CreateApplyTemplatesAction();
            }
            else if (Ref.Equal(name, input.Atoms.Attribute))
            {
                action = compiler.CreateAttributeAction();
            }
            else if (Ref.Equal(name, input.Atoms.CallTemplate))
            {
                action = compiler.CreateCallTemplateAction();
            }
            else if (Ref.Equal(name, input.Atoms.Choose))
            {
                action = compiler.CreateChooseAction();
            }
            else if (Ref.Equal(name, input.Atoms.Comment))
            {
                action = compiler.CreateCommentAction();
            }
            else if (Ref.Equal(name, input.Atoms.Copy))
            {
                action = compiler.CreateCopyAction();
            }
            else if (Ref.Equal(name, input.Atoms.CopyOf))
            {
                action = compiler.CreateCopyOfAction();
            }
            else if (Ref.Equal(name, input.Atoms.Element))
            {
                action = compiler.CreateElementAction();
            }
            else if (Ref.Equal(name, input.Atoms.Fallback))
            {
                return;
            }
            else if (Ref.Equal(name, input.Atoms.ForEach))
            {
                action = compiler.CreateForEachAction();
            }
            else if (Ref.Equal(name, input.Atoms.If))
            {
                action = compiler.CreateIfAction(IfAction.ConditionType.ConditionIf);
            }
            else if (Ref.Equal(name, input.Atoms.Message))
            {
                action = compiler.CreateMessageAction();
            }
            else if (Ref.Equal(name, input.Atoms.Number))
            {
                action = compiler.CreateNumberAction();
            }
            else if (Ref.Equal(name, input.Atoms.ProcessingInstruction))
            {
                action = compiler.CreateProcessingInstructionAction();
            }
            else if (Ref.Equal(name, input.Atoms.Text))
            {
                action = compiler.CreateTextAction();
            }
            else if (Ref.Equal(name, input.Atoms.ValueOf))
            {
                action = compiler.CreateValueOfAction();
            }
            else if (Ref.Equal(name, input.Atoms.Variable))
            {
                action = compiler.CreateVariableAction(VariableType.LocalVariable);
            }
            else
            {
                if (compiler.ForwardCompatibility)
                    action = compiler.CreateNewInstructionAction();
                else
                    throw compiler.UnexpectedKeyword();
            }

            Debug.Assert(action != null);

            AddAction(action);
        }

        private void CompileLiteral(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;

            switch (input.NodeType)
            {
                case XPathNodeType.Element:
                    this.AddEvent(compiler.CreateBeginEvent());
                    CompileLiteralAttributesAndNamespaces(compiler);

                    if (compiler.Recurse())
                    {
                        CompileTemplate(compiler);
                        compiler.ToParent();
                    }

                    this.AddEvent(new EndEvent(XPathNodeType.Element));
                    break;

                case XPathNodeType.Text:
                case XPathNodeType.SignificantWhitespace:
                    this.AddEvent(compiler.CreateTextEvent());
                    break;
                case XPathNodeType.Whitespace:
                case XPathNodeType.ProcessingInstruction:
                case XPathNodeType.Comment:
                    break;

                default:
                    Debug.Fail("Unexpected node type.");
                    break;
            }
        }

        private void CompileLiteralAttributesAndNamespaces(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;

            if (input.Navigator.MoveToAttribute("use-attribute-sets", input.Atoms.UriXsl))
            {
                AddAction(compiler.CreateUseAttributeSetsAction());
                input.Navigator.MoveToParent();
            }
            compiler.InsertExcludedNamespace();

            if (input.MoveToFirstNamespace())
            {
                do
                {
                    string uri = input.Value;

                    if (uri == XmlReservedNs.NsXslt)
                    {
                        continue;
                    }
                    if (
                        compiler.IsExcludedNamespace(uri) ||
                        compiler.IsExtensionNamespace(uri) ||
                        compiler.IsNamespaceAlias(uri)
                    )
                    {
                        continue;
                    }
                    this.AddEvent(new NamespaceEvent(input));
                }
                while (input.MoveToNextNamespace());
                input.ToParent();
            }

            if (input.MoveToFirstAttribute())
            {
                do
                {
                    // Skip everything from Xslt namespace
                    if (Ref.Equal(input.NamespaceURI, input.Atoms.UriXsl))
                    {
                        continue;
                    }

                    // Add attribute events
                    this.AddEvent(compiler.CreateBeginEvent());
                    this.AddEvents(compiler.CompileAvt(input.Value));
                    this.AddEvent(new EndEvent(XPathNodeType.Attribute));
                }
                while (input.MoveToNextAttribute());
                input.ToParent();
            }
        }

        private void CompileOutput(Compiler compiler)
        {
            Debug.Assert((object)this == (object)compiler.RootAction);
            compiler.RootAction.Output.Compile(compiler);
        }

        internal void AddAction(Action? action)
        {
            if (this.containedActions == null)
            {
                this.containedActions = new ArrayList();
            }
            this.containedActions.Add(action);
            lastCopyCodeAction = null;
        }

        [MemberNotNull(nameof(lastCopyCodeAction))]
        private void EnsureCopyCodeAction()
        {
            if (lastCopyCodeAction == null)
            {
                CopyCodeAction copyCode = new CopyCodeAction();
                AddAction(copyCode);
                lastCopyCodeAction = copyCode;
            }
        }

        protected void AddEvent(Event copyEvent)
        {
            EnsureCopyCodeAction();
            lastCopyCodeAction.AddEvent(copyEvent);
        }

        protected void AddEvents(ArrayList copyEvents)
        {
            EnsureCopyCodeAction();
            lastCopyCodeAction.AddEvents(copyEvents);
        }

        private static void AddScript(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;

            ScriptingLanguage lang = ScriptingLanguage.JScript;
            string? implementsNamespace = null;
            if (input.MoveToFirstAttribute())
            {
                do
                {
                    if (input.LocalName == input.Atoms.Language)
                    {
                        string langName = input.Value;
                        if (
                            string.Equals(langName, "jscript", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(langName, "javascript", StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            lang = ScriptingLanguage.JScript;
                        }
                        else if (
                          string.Equals(langName, "c#", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(langName, "csharp", StringComparison.OrdinalIgnoreCase)
                      )
                        {
                            lang = ScriptingLanguage.CSharp;
                        }
                        else if (
                            string.Equals(langName, "vb", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(langName, "visualbasic", StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            lang = ScriptingLanguage.VisualBasic;
                        }
                        else
                        {
                            throw XsltException.Create(SR.Xslt_ScriptInvalidLanguage, langName);
                        }
                    }
                    else if (input.LocalName == input.Atoms.ImplementsPrefix)
                    {
                        if (!PrefixQName.ValidatePrefix(input.Value))
                        {
                            throw XsltException.Create(SR.Xslt_InvalidAttrValue, input.LocalName, input.Value);
                        }
                        implementsNamespace = compiler.ResolveXmlNamespace(input.Value);
                    }
                }
                while (input.MoveToNextAttribute());
                input.ToParent();
            }
            if (implementsNamespace == null)
            {
                throw XsltException.Create(SR.Xslt_MissingAttribute, input.Atoms.ImplementsPrefix);
            }
            if (!input.Recurse() || input.NodeType != XPathNodeType.Text)
            {
                throw XsltException.Create(SR.Xslt_ScriptEmpty);
            }
            compiler.AddScript(input.Value, lang, implementsNamespace, input.BaseURI, input.LineNumber);
            input.ToParent();
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);

            switch (frame.State)
            {
                case Initialized:
                    if (this.containedActions != null && this.containedActions.Count > 0)
                    {
                        processor.PushActionFrame(frame);
                        frame.State = ProcessingChildren;
                    }
                    else
                    {
                        frame.Finished();
                    }
                    break;                              // Allow children to run

                case ProcessingChildren:
                    frame.Finished();
                    break;

                default:
                    Debug.Fail("Invalid Container action execution state");
                    break;
            }
        }

        internal Action? GetAction(int actionIndex)
        {
            Debug.Assert(actionIndex == 0 || this.containedActions != null);

            if (this.containedActions != null && actionIndex < this.containedActions.Count)
            {
                return (Action)this.containedActions[actionIndex]!;
            }
            else
            {
                return null;
            }
        }

        internal void CheckDuplicateParams(XmlQualifiedName name)
        {
            if (this.containedActions != null)
            {
                foreach (CompiledAction action in this.containedActions)
                {
                    WithParamAction? param = action as WithParamAction;
                    if (param != null && param.Name == name)
                    {
                        throw XsltException.Create(SR.Xslt_DuplicateWithParam, name.ToString());
                    }
                }
            }
        }

        internal override void ReplaceNamespaceAlias(Compiler compiler)
        {
            if (this.containedActions == null)
            {
                return;
            }
            for (int i = 0; i < this.containedActions.Count; i++)
            {
                ((Action)this.containedActions[i]!).ReplaceNamespaceAlias(compiler);
            }
        }
    }
}
