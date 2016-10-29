// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <spec>http://devdiv/Documents/Whidbey/CLR/CurrentSpecs/BCL/CodeDom%20Activation.doc</spec>
//------------------------------------------------------------------------------

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Xsl.IlGen;
using System.Xml.Xsl.Runtime;
using System.Runtime.Versioning;

namespace System.Xml.Xsl.Xslt
{

    internal class ScriptClass
    {
        public string ns;
        public CompilerInfo compilerInfo;
        public StringCollection refAssemblies;
        public StringCollection nsImports;
        public CodeTypeDeclaration typeDecl;
        public bool refAssembliesByHref;

        public Dictionary<string, string> scriptUris;

        // These two fields are used to report a compile error when its position is outside
        // of all user code snippets in the generated temporary file
        public string endUri;
        public Location endLoc;

        public ScriptClass(string ns, CompilerInfo compilerInfo)
        {
            this.ns = ns;
            this.compilerInfo = compilerInfo;
            this.refAssemblies = new StringCollection();
            this.nsImports = new StringCollection();
            this.typeDecl = new CodeTypeDeclaration(GenerateUniqueClassName());
            this.refAssembliesByHref = false;
            this.scriptUris = new Dictionary<string, string>(
#if !FEATURE_CASE_SENSITIVE_FILESYSTEM            
                StringComparer.OrdinalIgnoreCase
#endif
            );
        }

        private static long s_scriptClassCounter = 0;

        private static string GenerateUniqueClassName()
        {
            return "Script" + Interlocked.Increment(ref s_scriptClassCounter);
        }

        public void AddScriptBlock(string source, string uriString, int lineNumber, Location end)
        {
            CodeSnippetTypeMember scriptSnippet = new CodeSnippetTypeMember(source);
            string fileName = SourceLineInfo.GetFileName(uriString);
            if (lineNumber > 0)
            {
                scriptSnippet.LinePragma = new CodeLinePragma(fileName, lineNumber);
                scriptUris[fileName] = uriString;
            }
            typeDecl.Members.Add(scriptSnippet);

            this.endUri = uriString;
            this.endLoc = end;
        }

        public ISourceLineInfo EndLineInfo
        {
            get
            {
                return new SourceLineInfo(this.endUri, this.endLoc, this.endLoc);
            }
        }
    }

    internal class Scripts
    {
        private const string ScriptClassesNamespace = "System.Xml.Xsl.CompiledQuery";

        private Compiler _compiler;
        private List<ScriptClass> _scriptClasses = new List<ScriptClass>();
        private Dictionary<string, Type> _nsToType = new Dictionary<string, Type>();
        private XmlExtensionFunctionTable _extFuncs = new XmlExtensionFunctionTable();

        public Scripts(Compiler compiler)
        {
            _compiler = compiler;
        }

        public Dictionary<string, Type> ScriptClasses
        {
            get { return _nsToType; }
        }

        public XmlExtensionFunction ResolveFunction(string name, string ns, int numArgs, IErrorHelper errorHelper)
        {
            Type type;
            if (_nsToType.TryGetValue(ns, out type))
            {
                try
                {
                    return _extFuncs.Bind(name, ns, numArgs, type, XmlQueryRuntime.EarlyBoundFlags);
                }
                catch (XslTransformException e)
                {
                    errorHelper.ReportError(e.Message);
                }
            }
            return null;
        }

        public ScriptClass GetScriptClass(string ns, string language, IErrorHelper errorHelper)
        {
            CompilerInfo compilerInfo;
            try
            {
                compilerInfo = CodeDomProvider.GetCompilerInfo(language);
                Debug.Assert(compilerInfo != null);
            }
            catch (SystemException)
            {
                // There is no CodeDom provider defined for this language
                errorHelper.ReportError(/*[XT_010]*/SR.Xslt_ScriptInvalidLanguage, language);
                return null;
            }

            foreach (ScriptClass scriptClass in _scriptClasses)
            {
                if (ns == scriptClass.ns)
                {
                    // Use object comparison because CompilerInfo.Equals may throw
                    if (compilerInfo != scriptClass.compilerInfo)
                    {
                        errorHelper.ReportError(/*[XT_011]*/SR.Xslt_ScriptMixedLanguages, ns);
                        return null;
                    }
                    return scriptClass;
                }
            }

            ScriptClass newScriptClass = new ScriptClass(ns, compilerInfo);
            newScriptClass.typeDecl.TypeAttributes = TypeAttributes.Public;
            _scriptClasses.Add(newScriptClass);
            return newScriptClass;
        }

        //------------------------------------------------
        // Compilation
        //------------------------------------------------

        public void CompileScripts()
        {
            List<ScriptClass> scriptsForLang = new List<ScriptClass>();

            for (int i = 0; i < _scriptClasses.Count; i++)
            {
                // If the script is already compiled, skip it
                if (_scriptClasses[i] == null)
                    continue;

                // Group together scripts with the same CompilerInfo
                CompilerInfo compilerInfo = _scriptClasses[i].compilerInfo;
                scriptsForLang.Clear();

                for (int j = i; j < _scriptClasses.Count; j++)
                {
                    // Use object comparison because CompilerInfo.Equals may throw
                    if (_scriptClasses[j] != null && _scriptClasses[j].compilerInfo == compilerInfo)
                    {
                        scriptsForLang.Add(_scriptClasses[j]);
                        _scriptClasses[j] = null;
                    }
                }

                Assembly assembly = CompileAssembly(scriptsForLang);

                if (assembly != null)
                {
                    foreach (ScriptClass script in scriptsForLang)
                    {
                        Type clrType = assembly.GetType(ScriptClassesNamespace + '.' + script.typeDecl.Name);
                        if (clrType != null)
                        {
                            _nsToType.Add(script.ns, clrType);
                        }
                    }
                }
            }
        }

        // Namespaces we always import when compiling
        private static readonly string[] s_defaultNamespaces = new string[] {
            "System",
            "System.Collections",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Xml",
            "System.Xml.Xsl",
            "System.Xml.XPath",
        };

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        private Assembly CompileAssembly(List<ScriptClass> scriptsForLang)
        {
            TempFileCollection allTempFiles = _compiler.CompilerResults.TempFiles;
            CompilerErrorCollection allErrors = _compiler.CompilerResults.Errors;
            ScriptClass lastScript = scriptsForLang[scriptsForLang.Count - 1];
            CodeDomProvider provider;
            bool isVB = false;

            try
            {
                provider = lastScript.compilerInfo.CreateProvider();
            }
            catch (SystemException e)
            {
                // The CodeDom provider type could not be located, or some error in machine.config
                allErrors.Add(_compiler.CreateError(lastScript.EndLineInfo, /*[XT_041]*/SR.Xslt_ScriptCompileException, e.Message));
                return null;
            }

#if !FEATURE_PAL // visualbasic
            isVB = provider is Microsoft.VisualBasic.VBCodeProvider;
#endif // !FEATURE_PAL

            CodeCompileUnit[] codeUnits = new CodeCompileUnit[scriptsForLang.Count];
            CompilerParameters compilParams = lastScript.compilerInfo.CreateDefaultCompilerParameters();

            // REVIEW: You cannot say just "System.Xml", because JScript compiler loads it from
            // the Framework folder, while other compilers load it from GAC, thus we would have
            // two distinct copies of System.Xml in the memory
            compilParams.ReferencedAssemblies.Add(typeof(SR).GetTypeInfo().Assembly.GetName().ToString());
            compilParams.ReferencedAssemblies.Add("System.dll");
            if (isVB)
            {
                compilParams.ReferencedAssemblies.Add("Microsoft.VisualBasic.dll");
            }

            bool refAssembliesByHref = false;

            for (int idx = 0; idx < scriptsForLang.Count; idx++)
            {
                ScriptClass script = scriptsForLang[idx];
                CodeNamespace scriptNs = new CodeNamespace(ScriptClassesNamespace);

                // Add imported namespaces
                foreach (string ns in s_defaultNamespaces)
                {
                    scriptNs.Imports.Add(new CodeNamespaceImport(ns));
                }
                if (isVB)
                {
                    scriptNs.Imports.Add(new CodeNamespaceImport("Microsoft.VisualBasic"));
                }
                foreach (string ns in script.nsImports)
                {
                    scriptNs.Imports.Add(new CodeNamespaceImport(ns));
                }

                scriptNs.Types.Add(script.typeDecl);

                CodeCompileUnit unit = new CodeCompileUnit();
                {
                    unit.Namespaces.Add(scriptNs);

                    if (isVB)
                    {
                        // This settings have sense for Visual Basic only. In future releases we may allow to specify
                        // them explicitly in the msxsl:script element.
                        unit.UserData["AllowLateBound"] = true;   // Allow variables to be declared untyped
                        unit.UserData["RequireVariableDeclaration"] = false;  // Allow variables to be undeclared
                    }
                }

                codeUnits[idx] = unit;
                foreach (string name in script.refAssemblies)
                {
                    compilParams.ReferencedAssemblies.Add(name);
                }

                refAssembliesByHref |= script.refAssembliesByHref;
            }

            XsltSettings settings = _compiler.Settings;
            compilParams.WarningLevel = settings.WarningLevel >= 0 ? settings.WarningLevel : compilParams.WarningLevel;
            compilParams.TreatWarningsAsErrors = settings.TreatWarningsAsErrors;
            compilParams.IncludeDebugInformation = _compiler.IsDebug;

            string asmPath = _compiler.ScriptAssemblyPath;
            if (asmPath != null && scriptsForLang.Count < _scriptClasses.Count)
            {
                asmPath = Path.ChangeExtension(asmPath, "." + GetLanguageName(lastScript.compilerInfo) + Path.GetExtension(asmPath));
            }
            compilParams.OutputAssembly = asmPath;

            string tempDir = (settings.TempFiles != null) ? settings.TempFiles.TempDir : null;
            compilParams.TempFiles = new TempFileCollection(tempDir);

            // We need only .dll and .pdb, but there is no way to specify that
            bool keepFiles = (_compiler.IsDebug && asmPath == null);
#if DEBUG
            keepFiles = keepFiles || XmlILTrace.IsEnabled;
#endif
            keepFiles = keepFiles && !settings.CheckOnly;


            compilParams.TempFiles.KeepFiles = keepFiles;

            // If GenerateInMemory == true, then CodeDom loads the compiled assembly using Assembly.Load(byte[])
            // instead of Assembly.Load(AssemblyName).  That means the assembly will be loaded in the anonymous
            // context (http://blogs.msdn.com/suzcook/archive/2003/05/29/57143.aspx), and its dependencies can only
            // be loaded from the Load context or using AssemblyResolve event.  However we want to use the LoadFrom
            // context to preload all dependencies specified by <ms:assembly href="uri-reference"/>, so we turn off
            // GenerateInMemory here.
            compilParams.GenerateInMemory = (asmPath == null && !_compiler.IsDebug && !refAssembliesByHref) || settings.CheckOnly;

            CompilerResults results;

            try
            {
                results = provider.CompileAssemblyFromDom(compilParams, codeUnits);
            }
            catch (Exception e)
            {
                if (e.HResult == HResults.EFail)
                {
                    // Compiler might have created temporary files
                    results = new CompilerResults(compilParams.TempFiles);
                    results.Errors.Add(_compiler.CreateError(lastScript.EndLineInfo, /*[XT_041]*/SR.Xslt_ScriptCompileException, e.Message));
                }
                else
                {
                    throw;
                }
            }

            if (!settings.CheckOnly)
            {
                foreach (string fileName in results.TempFiles)
                {
                    allTempFiles.AddFile(fileName, allTempFiles.KeepFiles);
                }
            }

            foreach (CompilerError error in results.Errors)
            {
                FixErrorPosition(error, scriptsForLang);
                _compiler.AddModule(error.FileName);
            }

            allErrors.AddRange(results.Errors);
            return results.Errors.HasErrors ? null : results.CompiledAssembly;
        }

        private int _assemblyCounter = 0;

        private string GetLanguageName(CompilerInfo compilerInfo)
        {
            Regex alphaNumeric = new Regex("^[0-9a-zA-Z]+$");
            foreach (string name in compilerInfo.GetLanguages())
            {
                if (alphaNumeric.IsMatch(name))
                    return name;
            }
            return "script" + (++_assemblyCounter).ToString(CultureInfo.InvariantCulture);
        }

        // The position of a compile error may be outside of all user code snippets (for example, in case of
        // unclosed '{'). In that case filename would be the name of the temporary file, and not the name
        // of the stylesheet file. Exposing the path of the temporary file is considered to be a security issue,
        // so here we check that filename is amongst user files.
        private static void FixErrorPosition(CompilerError error, List<ScriptClass> scriptsForLang)
        {
            string fileName = error.FileName;
            string uri;

            foreach (ScriptClass script in scriptsForLang)
            {
                // We assume that CodeDom provider returns absolute paths (VSWhidbey 289665).
                // Note that casing may be different.
                if (script.scriptUris.TryGetValue(fileName, out uri))
                {
                    // The error position is within one of user stylesheets, its URI may be reported
                    error.FileName = uri;
                    return;
                }
            }

            // Error is outside user code snippeets, we should hide filename for security reasons.
            // Return filename and position of the end of the last script block for the given class.
            int idx, scriptNumber;
            ScriptClass errScript = scriptsForLang[scriptsForLang.Count - 1];

            // Normally temporary source files are named according to the scheme "<random name>.<script number>.
            // <language extension>". Try to extract the middle part to find the relevant script class. In case
            // of a non-standard CodeDomProvider, use the last script class.
            fileName = Path.GetFileNameWithoutExtension(fileName);
            if ((idx = fileName.LastIndexOf('.')) >= 0)
                if (int.TryParse(fileName.Substring(idx + 1), NumberStyles.None, NumberFormatInfo.InvariantInfo, out scriptNumber))
                    if ((uint)scriptNumber < scriptsForLang.Count)
                    {
                        errScript = scriptsForLang[scriptNumber];
                    }

            error.FileName = errScript.endUri;
            error.Line = errScript.endLoc.Line;
            error.Column = errScript.endLoc.Pos;
        }
    }
}
