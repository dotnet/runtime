// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.XPath;
using System.Runtime.Versioning;

namespace System.Xml.Xsl.Xslt
{
    using TypeFactory = XmlQueryTypeFactory;
#if DEBUG && FEATURE_COMPILED_XSL
    using XmlILTrace = System.Xml.Xsl.IlGen.XmlILTrace;
#endif

    internal enum XslVersion
    {
        Version10 = 0,
        ForwardsCompatible = 1,
        Current = Version10,
    }

    // RootLevel is underdeveloped consept currently. I plane to move here more collections from Compiler.
    // Compiler is like a stylesheet in some sense. it has a lot of properties of stylesheet. Instead of 
    // inhereting from Styleseet (or StylesheetLevel) I desided to agregate special subclass of StylesheetLevel.
    // One more reason to for this design is to normolize apply-templates and apply-imports to one concept:
    // apply-templates is apply-imports(compiler.Root).
    // For now I don't create new files for these new classes to simplify integrations WebData <-> WebData_xsl
    internal class RootLevel : StylesheetLevel
    {
        public RootLevel(Stylesheet principal)
        {
            base.Imports = new Stylesheet[] { principal };
        }
    }

    internal class Compiler
    {
        public XsltSettings Settings;
        public bool IsDebug;
        public string ScriptAssemblyPath;
        public int Version;                // 0 - Auto; 1 - XSLT 1.0; 2 - XSLT 2.0
        public string inputTypeAnnotations;   // null - "unspecified"; "preserve"; "strip"

        public CompilerErrorCollection CompilerErrorColl;        // Results of the compilation
        public int CurrentPrecedence = 0;  // Decreases by 1 with each import
        public XslNode StartApplyTemplates;
        public RootLevel Root;
        public Scripts Scripts;
        public Output Output = new Output();
        public List<VarPar> ExternalPars = new List<VarPar>();
        public List<VarPar> GlobalVars = new List<VarPar>();
        public List<WhitespaceRule> WhitespaceRules = new List<WhitespaceRule>();
        public DecimalFormats DecimalFormats = new DecimalFormats();
        public Keys Keys = new Keys();
        public List<ProtoTemplate> AllTemplates = new List<ProtoTemplate>();

        public Dictionary<QilName, VarPar> AllGlobalVarPars = new Dictionary<QilName, VarPar>();
        public Dictionary<QilName, Template> NamedTemplates = new Dictionary<QilName, Template>();
        public Dictionary<QilName, AttributeSet> AttributeSets = new Dictionary<QilName, AttributeSet>();
        public Dictionary<string, NsAlias> NsAliases = new Dictionary<string, NsAlias>();

        private Dictionary<string, int> _moduleOrder = new Dictionary<string, int>();

        public Compiler(XsltSettings settings, bool debug, string scriptAssemblyPath)
        {
            Debug.Assert(CompilerErrorColl == null, "Compiler cannot be reused");

            Settings = settings;
            IsDebug = settings.IncludeDebugInformation | debug;
            ScriptAssemblyPath = scriptAssemblyPath;

            CompilerErrorColl = new CompilerErrorCollection();
            Scripts = new Scripts(this);
        }

        public CompilerErrorCollection Compile(object stylesheet, XmlResolver xmlResolver, out QilExpression qil)
        {
            Debug.Assert(stylesheet != null);
            Debug.Assert(Root == null, "Compiler cannot be reused");

            new XsltLoader().Load(this, stylesheet, xmlResolver);
            qil = QilGenerator.CompileStylesheet(this);
            SortErrors();
            return CompilerErrorColl;
        }

        public Stylesheet CreateStylesheet()
        {
            Stylesheet sheet = new Stylesheet(this, CurrentPrecedence);
            if (CurrentPrecedence-- == 0)
            {
                Root = new RootLevel(sheet);
            }
            return sheet;
        }

        public void AddModule(string baseUri)
        {
            if (!_moduleOrder.ContainsKey(baseUri))
            {
                _moduleOrder[baseUri] = _moduleOrder.Count;
            }
        }

        public void ApplyNsAliases(ref string prefix, ref string nsUri)
        {
            NsAlias alias;
            if (NsAliases.TryGetValue(nsUri, out alias))
            {
                nsUri = alias.ResultNsUri;
                prefix = alias.ResultPrefix;
            }
        }

        // Returns true in case of redefinition
        public bool SetNsAlias(string ssheetNsUri, string resultNsUri, string resultPrefix, int importPrecedence)
        {
            NsAlias oldNsAlias;
            if (NsAliases.TryGetValue(ssheetNsUri, out oldNsAlias))
            {
                // Namespace alias for this stylesheet namespace URI has already been defined
                Debug.Assert(importPrecedence <= oldNsAlias.ImportPrecedence, "Stylesheets must be processed in the order of decreasing import precedence");
                if (importPrecedence < oldNsAlias.ImportPrecedence || resultNsUri == oldNsAlias.ResultNsUri)
                {
                    // Either the identical definition or lower precedence - ignore it
                    return false;
                }
                // Recover by choosing the declaration that occurs later in the stylesheet
            }
            NsAliases[ssheetNsUri] = new NsAlias(resultNsUri, resultPrefix, importPrecedence);
            return oldNsAlias != null;
        }

        private void MergeWhitespaceRules(Stylesheet sheet)
        {
            for (int idx = 0; idx <= 2; idx++)
            {
                sheet.WhitespaceRules[idx].Reverse();
                this.WhitespaceRules.AddRange(sheet.WhitespaceRules[idx]);
            }
            sheet.WhitespaceRules = null;
        }

        private void MergeAttributeSets(Stylesheet sheet)
        {
            foreach (QilName attSetName in sheet.AttributeSets.Keys)
            {
                AttributeSet attSet;
                if (!this.AttributeSets.TryGetValue(attSetName, out attSet))
                {
                    this.AttributeSets[attSetName] = sheet.AttributeSets[attSetName];
                }
                else
                {
                    // Lower import precedence - insert before all previous definitions
                    attSet.MergeContent(sheet.AttributeSets[attSetName]);
                }
            }
            sheet.AttributeSets = null;
        }

        private void MergeGlobalVarPars(Stylesheet sheet)
        {
            foreach (VarPar var in sheet.GlobalVarPars)
            {
                Debug.Assert(var.NodeType == XslNodeType.Variable || var.NodeType == XslNodeType.Param);
                if (!AllGlobalVarPars.ContainsKey(var.Name))
                {
                    if (var.NodeType == XslNodeType.Variable)
                    {
                        GlobalVars.Add(var);
                    }
                    else
                    {
                        ExternalPars.Add(var);
                    }
                    AllGlobalVarPars[var.Name] = var;
                }
            }
            sheet.GlobalVarPars = null;
        }

        public void MergeWithStylesheet(Stylesheet sheet)
        {
            MergeWhitespaceRules(sheet);
            MergeAttributeSets(sheet);
            MergeGlobalVarPars(sheet);
        }

        public static string ConstructQName(string prefix, string localName)
        {
            if (prefix.Length == 0)
            {
                return localName;
            }
            else
            {
                return prefix + ':' + localName;
            }
        }

        public bool ParseQName(string qname, out string prefix, out string localName, IErrorHelper errorHelper)
        {
            Debug.Assert(qname != null);
            try
            {
                ValidateNames.ParseQNameThrow(qname, out prefix, out localName);
                return true;
            }
            catch (XmlException e)
            {
                errorHelper.ReportError(/*[XT_042]*/e.Message, null);
                prefix = PhantomNCName;
                localName = PhantomNCName;
                return false;
            }
        }

        public bool ParseNameTest(string nameTest, out string prefix, out string localName, IErrorHelper errorHelper)
        {
            Debug.Assert(nameTest != null);
            try
            {
                ValidateNames.ParseNameTestThrow(nameTest, out prefix, out localName);
                return true;
            }
            catch (XmlException e)
            {
                errorHelper.ReportError(/*[XT_043]*/e.Message, null);
                prefix = PhantomNCName;
                localName = PhantomNCName;
                return false;
            }
        }

        public void ValidatePiName(string name, IErrorHelper errorHelper)
        {
            Debug.Assert(name != null);
            try
            {
                ValidateNames.ValidateNameThrow(
                    /*prefix:*/string.Empty, /*localName:*/name, /*ns:*/string.Empty,
                    XPathNodeType.ProcessingInstruction, ValidateNames.Flags.AllExceptPrefixMapping
                );
            }
            catch (XmlException e)
            {
                errorHelper.ReportError(/*[XT_044]*/e.Message, null);
            }
        }

        public readonly string PhantomNCName = "error";
        private int _phantomNsCounter = 0;

        public string CreatePhantomNamespace()
        {
            // Prepend invalid XmlChar to ensure this name would not clash with any namespace name in the stylesheet
            return "\0namespace" + _phantomNsCounter++;
        }

        public bool IsPhantomNamespace(string namespaceName)
        {
            return namespaceName.Length > 0 && namespaceName[0] == '\0';
        }

        public bool IsPhantomName(QilName qname)
        {
            string nsUri = qname.NamespaceUri;
            return nsUri.Length > 0 && nsUri[0] == '\0';
        }

        // -------------------------------- Error Handling --------------------------------

        private int ErrorCount
        {
            get
            {
                return CompilerErrorColl.Count;
            }
            set
            {
                Debug.Assert(value <= ErrorCount);
                for (int idx = ErrorCount - 1; idx >= value; idx--)
                {
                    CompilerErrorColl.RemoveAt(idx);
                }
            }
        }

        private int _savedErrorCount = -1;

        public void EnterForwardsCompatible()
        {
            Debug.Assert(_savedErrorCount == -1, "Nested EnterForwardsCompatible calls");
            _savedErrorCount = ErrorCount;
        }

        // Returns true if no errors were suppressed
        public bool ExitForwardsCompatible(bool fwdCompat)
        {
            Debug.Assert(_savedErrorCount != -1, "ExitForwardsCompatible without EnterForwardsCompatible");
            if (fwdCompat && ErrorCount > _savedErrorCount)
            {
                ErrorCount = _savedErrorCount;
                Debug.Assert((_savedErrorCount = -1) < 0);
                return false;
            }
            Debug.Assert((_savedErrorCount = -1) < 0);
            return true;
        }

        public CompilerError CreateError(ISourceLineInfo lineInfo, string res, params string[] args)
        {
            AddModule(lineInfo.Uri);
            return new CompilerError(
                lineInfo.Uri, lineInfo.Start.Line, lineInfo.Start.Pos, /*errorNumber:*/string.Empty,
                /*errorText:*/XslTransformException.CreateMessage(res, args)
            );
        }

        public void ReportError(ISourceLineInfo lineInfo, string res, params string[] args)
        {
            CompilerError error = CreateError(lineInfo, res, args);
            CompilerErrorColl.Add(error);
        }

        public void ReportWarning(ISourceLineInfo lineInfo, string res, params string[] args)
        {
            int warningLevel = 1;
            if (0 <= Settings.WarningLevel && Settings.WarningLevel < warningLevel)
            {
                // Ignore warning
                return;
            }
            CompilerError error = CreateError(lineInfo, res, args);
            if (Settings.TreatWarningsAsErrors)
            {
                error.ErrorText = XslTransformException.CreateMessage(SR.Xslt_WarningAsError, error.ErrorText);
                CompilerErrorColl.Add(error);
            }
            else
            {
                error.IsWarning = true;
                CompilerErrorColl.Add(error);
            }
        }

        private void SortErrors()
        {
            CompilerErrorCollection errorColl = this.CompilerErrorColl;
            if (errorColl.Count > 1)
            {
                CompilerError[] errors = new CompilerError[errorColl.Count];
                errorColl.CopyTo(errors, 0);
                Array.Sort<CompilerError>(errors, new CompilerErrorComparer(_moduleOrder));
                errorColl.Clear();
                errorColl.AddRange(errors);
            }
        }

        private class CompilerErrorComparer : IComparer<CompilerError>
        {
            private Dictionary<string, int> _moduleOrder;

            public CompilerErrorComparer(Dictionary<string, int> moduleOrder)
            {
                _moduleOrder = moduleOrder;
            }

            public int Compare(CompilerError x, CompilerError y)
            {
                if ((object)x == (object)y)
                    return 0;

                if (x == null)
                    return -1;

                if (y == null)
                    return 1;

                int result = _moduleOrder[x.FileName].CompareTo(_moduleOrder[y.FileName]);
                if (result != 0)
                    return result;

                result = x.Line.CompareTo(y.Line);
                if (result != 0)
                    return result;

                result = x.Column.CompareTo(y.Column);
                if (result != 0)
                    return result;

                result = x.IsWarning.CompareTo(y.IsWarning);
                if (result != 0)
                    return result;

                result = string.CompareOrdinal(x.ErrorNumber, y.ErrorNumber);
                if (result != 0)
                    return result;

                return string.CompareOrdinal(x.ErrorText, y.ErrorText);
            }
        }
    }

    internal class Output
    {
        public XmlWriterSettings Settings;
        public string Version;
        public string Encoding;
        public XmlQualifiedName Method;

        // All the xsl:output elements occurring in a stylesheet are merged into a single effective xsl:output element.
        // We store the import precedence of each attribute value to catch redefinitions with the same import precedence.
        public const int NeverDeclaredPrec = int.MinValue;
        public int MethodPrec = NeverDeclaredPrec;
        public int VersionPrec = NeverDeclaredPrec;
        public int EncodingPrec = NeverDeclaredPrec;
        public int OmitXmlDeclarationPrec = NeverDeclaredPrec;
        public int StandalonePrec = NeverDeclaredPrec;
        public int DocTypePublicPrec = NeverDeclaredPrec;
        public int DocTypeSystemPrec = NeverDeclaredPrec;
        public int IndentPrec = NeverDeclaredPrec;
        public int MediaTypePrec = NeverDeclaredPrec;

        public Output()
        {
            Settings = new XmlWriterSettings();
            Settings.OutputMethod = XmlOutputMethod.AutoDetect;
            Settings.AutoXmlDeclaration = true;
            Settings.ConformanceLevel = ConformanceLevel.Auto;
            Settings.MergeCDataSections = true;
        }
    }

    internal class DecimalFormats : KeyedCollection<XmlQualifiedName, DecimalFormatDecl>
    {
        protected override XmlQualifiedName GetKeyForItem(DecimalFormatDecl format)
        {
            return format.Name;
        }
    }

    internal class DecimalFormatDecl
    {
        public readonly XmlQualifiedName Name;
        public readonly string InfinitySymbol;
        public readonly string NanSymbol;
        public readonly char[] Characters;

        public static DecimalFormatDecl Default = new DecimalFormatDecl(new XmlQualifiedName(), "Infinity", "NaN", ".,%\u20300#;-");

        public DecimalFormatDecl(XmlQualifiedName name, string infinitySymbol, string nanSymbol, string characters)
        {
            Debug.Assert(characters.Length == 8);
            this.Name = name;
            this.InfinitySymbol = infinitySymbol;
            this.NanSymbol = nanSymbol;
            this.Characters = characters.ToCharArray();
        }
    }

    internal class NsAlias
    {
        public readonly string ResultNsUri;
        public readonly string ResultPrefix;
        public readonly int ImportPrecedence;

        public NsAlias(string resultNsUri, string resultPrefix, int importPrecedence)
        {
            this.ResultNsUri = resultNsUri;
            this.ResultPrefix = resultPrefix;
            this.ImportPrecedence = importPrecedence;
        }
    }
}
