// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using System.Speech.Recognition;
using System.Speech.Internal.SrgsParser;
using System.Text;
using Microsoft.CSharp;
using Microsoft.VisualBasic;

#pragma warning disable 56507 // check for null or empty strings

namespace System.Speech.Internal.SrgsCompiler
{
    /// <summary>
    /// Summary description for CustomGrammar.
    /// </summary>
    internal class CustomGrammar
    {
        #region Constructors

        internal CustomGrammar()
        {
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Creates the assembly and makes sure the it is valid.
        /// Creates the constructor for the grammar
        /// </summary>
        internal string CreateAssembly(int iCfg, string outputFile, CultureInfo culture)
        {
            // Temporary file for the IL
            // Limitation to the ICompiler interfaces!
            string code = null;
            FileHelper.DeleteTemporaryFile(outputFile);

            try
            {
                CreateAssembly(outputFile, false, null);

                // Check the validity of the code only on individual grammars
                CheckValidAssembly(iCfg, ExtractCodeGenerated(outputFile));

                // Regenerate the code with the constructors added
                code = GenerateCode(true, culture);
            }
            finally
            {
                FileHelper.DeleteTemporaryFile(outputFile);
            }
            return code;
        }

        /// <summary>
        /// Creates the assembly and makes sure the it is valid.
        /// Get the IL and PDB for the embeded code for this grammar
        /// </summary>
        /// <param name="il"></param>
        /// <param name="pdb"></param>
        internal void CreateAssembly(out byte[] il, out byte[] pdb)
        {
            // Temporary file for the IL
            // Limitation to the ICompiler interfaces!
            string outputFile;
            using (FileStream outputStream = FileHelper.CreateAndOpenTemporaryFile(out outputFile, extension: "dll", fileOptions: FileOptions.DeleteOnClose))
            {
                // do nothing - allow the file to be created, closed and deleted;
                // we only care about the name, which we will give to the compiler.
                // Two processes might collide, sending the same name to the compiler,
                // but the chances of that are remote, and there's no way to avoid it.
            }

            try
            {
                CreateAssembly(outputFile, _fDebugScript, null);

                il = ExtractCodeGenerated(outputFile);
                pdb = null;
                if (_fDebugScript)
                {
                    string pdbFile = outputFile.Substring(0, outputFile.LastIndexOf('.')) + ".pdb";
                    pdb = ExtractCodeGenerated(pdbFile);
                    FileHelper.DeleteTemporaryFile(pdbFile);
                }

                // Check the validity of the code only on individual grammars
                CheckValidAssembly(0, il);
            }
            finally
            {
                FileHelper.DeleteTemporaryFile(outputFile);
            }
        }

        /// <summary>
        /// Create a DLL with the CFG as a resource
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cfgResources"></param>
        internal void CreateAssembly(string path, List<CustomGrammar.CfgResource> cfgResources)
        {
            CreateAssembly(path, _fDebugScript, cfgResources);
        }

        /// <summary>
        /// Add the scripts defined in 'cg' to the set of scripts defined in 'cgCombined'.
        /// Build the union of teh codebehind files and assembly references
        /// </summary>
        /// <param name="cg"></param>
        /// <param name="innerCode"></param>
        internal void Combine(CustomGrammar cg, string innerCode)
        {
            if (_rules.Count == 0)
            {
                _language = cg._language;
            }
            else
            {
                if (_language != cg._language)
                {
                    XmlParser.ThrowSrgsException(SRID.IncompatibleLanguageProperties);
                }
            }

            if (_namespace == null)
            {
                _namespace = cg._namespace;
            }
            else
            {
                if (_namespace != cg._namespace)
                {
                    XmlParser.ThrowSrgsException(SRID.IncompatibleNamespaceProperties);
                }
            }

            _fDebugScript |= cg._fDebugScript;

            foreach (string codebehind in cg._codebehind)
            {
                if (!_codebehind.Contains(codebehind))
                {
                    _codebehind.Add(codebehind);
                }
            }

            foreach (string assemblyReferences in cg._assemblyReferences)
            {
                if (!_assemblyReferences.Contains(assemblyReferences))
                {
                    _assemblyReferences.Add(assemblyReferences);
                }
            }

            foreach (string importNamespaces in cg._importNamespaces)
            {
                if (!_importNamespaces.Contains(importNamespaces))
                {
                    _importNamespaces.Add(importNamespaces);
                }
            }

            _keyFile = cg._keyFile;

            _types.AddRange(cg._types);
            foreach (Rule rule in cg._rules)
            {
                if (_types.Contains(rule.Name))
                {
                    XmlParser.ThrowSrgsException(SRID.RuleDefinedMultipleTimes2, rule.Name);
                }
            }

            // Combine all the scripts
            _script.Append(innerCode);
        }

        #endregion

        #region Internal Properties

        internal bool HasScript
        {
            get
            {
                bool has_script = _script.Length > 0 || _codebehind.Count > 0;
                if (!has_script)
                {
                    foreach (Rule rule in _rules)
                    {
                        if (rule.Script.Length > 0)
                        {
                            has_script = true;
                            break;
                        }
                    }
                }
                return has_script;
            }
        }

        #endregion

        #region Internal Types

        internal class CfgResource
        {
            internal string name;
            internal byte[] data;
        }

        #endregion

        #region Internal Fields

        // 'C#', 'VB' or 'JScript'
        internal string _language = "C#";

        // namespace for the class wrapping the inline code
        internal string _namespace;

        // namespace for the class wrapping the inline code
        internal List<Rule> _rules = new();

        // code behind dll
        internal Collection<string> _codebehind = new();

        // if set generates #line statements
        internal bool _fDebugScript;

        // List of assemby references to import
        internal Collection<string> _assemblyReferences = new();

        // List of namespaces to import
        internal Collection<string> _importNamespaces = new();

        // Key file for the strong name
        internal string _keyFile;

        // CFG scripts definition
        internal Collection<ScriptRef> _scriptRefs = new();

        // inline script
        internal List<string> _types = new();

        // inline script
        internal StringBuilder _script = new();

        #endregion

        #region Private Methods

        private void CreateAssembly(string outputFile, bool debug, List<CfgResource> cfgResources)
        {
            if (_language == null)
            {
                XmlParser.ThrowSrgsException(SRID.NoLanguageSet);
            }

            // Get the scrip to compile
            string sourceCode = GenerateCode(false, null);

            // Script could end up in a file.
            string scriptFile = null;

            // List of files to compile; embedded script + code behind file
            string[] files = null;

            try
            {
                // Add an extra file for the embedded script if code behind files are available
                if (_codebehind.Count > 0)
                {
                    int cFiles = _codebehind.Count + (sourceCode != null ? 1 : 0);
                    files = new string[cFiles];

                    for (int i = 0; i < _codebehind.Count; i++)
                    {
                        files[i] = _codebehind[i];
                    }

                    if (sourceCode != null)
                    {
                        using (FileStream scriptStream = FileHelper.CreateAndOpenTemporaryFile(out scriptFile))
                        {
                            files[files.Length - 1] = scriptFile;
                            // Write the script in a temporary file
                            using (StreamWriter sw = new(scriptStream))
                            {
                                sw.Write(sourceCode);
                            }
                        }
                    }
                }

                // Compile the code files to [outputFile].dll and [outputFile.pdb
                CompileScript(outputFile, debug, sourceCode, files, cfgResources);
            }
            finally
            {
                FileHelper.DeleteTemporaryFile(scriptFile);
            }
        }

        private void CompileScript(string outputFile, bool debug, string code, string[] codeFiles, List<CfgResource> cfgResouces)
        {
            //string pdbFile = debug ? outputFile.Substring (0, outputFile.LastIndexOf ('.')) + ".pdb" : null;

            using (CodeDomProvider codeDomProvider = CodeProvider())
            {
                CompilerParameters parameters = GetCompilerParameters(outputFile, cfgResouces, debug, _assemblyReferences, _keyFile);

                CompilerResults results;
                if (codeFiles != null)
                {
                    // Compile the set of source files
                    results = codeDomProvider.CompileAssemblyFromFile(parameters, codeFiles);
                }
                else
                {
                    // Compile the set of source files
                    results = codeDomProvider.CompileAssemblyFromSource(parameters, code);
                }

                if (results.Errors.Count > 0)
                {
                    ThrowCompilationErrors(results);
                }

                if (results.NativeCompilerReturnValue != 0)
                {
                    XmlParser.ThrowSrgsException(SRID.UnexpectedError, results.NativeCompilerReturnValue);
                }
            }
        }

        private CodeDomProvider CodeProvider()
        {
            CodeDomProvider codeDomProvider = null;
            switch (_language)
            {
                case "C#":
                    codeDomProvider = CreateCSharpCompiler();
                    break;

                case "VB.Net":
                    codeDomProvider = CreateVBCompiler();
                    break;

                default:
                    XmlParser.ThrowSrgsException(SRID.UnsupportedLanguage, _language);
                    break;
            }
            return codeDomProvider;
        }

        private string GenerateCode(bool classDefinitionOnly, CultureInfo culture)
        {
            string script = string.Empty;

            // Select an instance of a compiler and wrap the script code with the
            // class definition
            switch (_language)
            {
                case "C#":
                    script = WrapScriptCSharp(classDefinitionOnly, culture);
                    break;

                case "VB.Net":
                    script = WrapScriptVB(classDefinitionOnly, culture);
                    break;

                default:
                    XmlParser.ThrowSrgsException(SRID.UnsupportedLanguage, _language);
                    break;
            }

            return script;
        }

        private string WrapScriptCSharp(bool classDefinitionOnly, CultureInfo culture)
        {
            StringBuilder sbClasses = new();

            // Combine all the classes into a single text
            foreach (Rule rule in _rules)
            {
                if (rule.Script != null)
                {
                    WrapClassCSharp(sbClasses, rule.Name, rule.BaseClass, culture, rule.Script.ToString(), rule.Constructors.ToString());
                }
            }

            // Add the global scripts
            if (_script.Length > 0)
            {
                sbClasses.Append(_script);
            }

            // Add the using and name space definition
            return sbClasses.Length > 0 ? !classDefinitionOnly ? WrapScriptOuterCSharp(sbClasses.ToString()) : sbClasses.ToString() : null;
        }

        private string WrapScriptVB(bool classDefinitionOnly, CultureInfo culture)
        {
            StringBuilder sbClasses = new();

            // Combine all the classes into a single text
            foreach (Rule rule in _rules)
            {
                if (rule.Script != null)
                {
                    WrapClassVB(sbClasses, rule.Name, rule.BaseClass, culture, rule.Script.ToString(), rule.Constructors.ToString());
                }
            }

            // Add the global scripts
            if (_script.Length > 0)
            {
                sbClasses.Append(_script);
            }

            // Add the using and name space definition
            return sbClasses.Length > 0 ? !classDefinitionOnly ? WrapScriptOuterVB(sbClasses.ToString()) : sbClasses.ToString() : null;
        }

        /// <summary>
        /// The CSharp assembly is loaded on the first call to the CSharpCodeProvider.
        /// Keeps this routine outside of CreateAssembly to avoid loading both the
        /// CSharp compiler, the VB compiler and JSccript compiler.
        /// </summary>
        /// <returns></returns>
        private static CodeDomProvider CreateCSharpCompiler()
        {
            return new CSharpCodeProvider();
        }

        private string WrapScriptOuterCSharp(string innerCode)
        {
            // Add the using and name space definition
            if (!string.IsNullOrEmpty(innerCode))
            {
                // quick estimate for the string builder size
                int cNamespacesStrings = 0;
                foreach (string importNamespace in _importNamespaces)
                {
                    cNamespacesStrings += importNamespace.Length;
                }

                // Find the local namespace, System.Speech or Microsoft.SpeechServer
                SRID srid = SRID.ArrayOfNullIllegal;
                string speechNamespace = srid.GetType().Namespace;

                // Add the using
                string usingStatements = string.Format(CultureInfo.InvariantCulture, "#line 1 \"{0}\"\nusing System;\nusing System.Collections.Generic;\nusing System.Diagnostics;\nusing {1};\nusing {1}.Recognition;\nusing {1}.Recognition.SrgsGrammar;\n", _preambuleMarker, speechNamespace);
                StringBuilder sbWhole = new(_script.Length + usingStatements.Length + 200);

                sbWhole.Append(usingStatements);
                foreach (string importNamespace in _importNamespaces)
                {
                    sbWhole.Append("using ");
                    sbWhole.Append(importNamespace);
                    sbWhole.Append(";\n");
                }

                // Add the namespace definition
                if (_namespace != null)
                {
                    sbWhole.Append("namespace ");
                    sbWhole.Append(_namespace);
                    sbWhole.Append("\n{\n");
                }

                // Add all the classes
                sbWhole.Append(innerCode);

                // close the namespace if any
                if (_namespace != null)
                {
                    sbWhole.Append("}\n");
                }
                return sbWhole.ToString();
            }
            else
            {
                return null;
            }
        }

        private static void WrapClassCSharp(StringBuilder sb, string classname, string baseclass, CultureInfo culture, string script, string constructor)
        {
            // Add the class definition
            sb.Append("public partial class ");
            sb.Append(classname);
            sb.Append(" : ");
            sb.Append(!string.IsNullOrEmpty(baseclass) ? baseclass : "Grammar");
            sb.Append(" \n {\n");

            // Only append the Association table for STG files.
            if (culture != null)
            {
                // Append the association class to CFGs
                sb.Append("[DebuggerBrowsable (DebuggerBrowsableState.Never)]public static string __cultureId = \"");
                sb.Append(culture.LCID.ToString(CultureInfo.InvariantCulture));
                sb.Append("\";\n");
            }

            // The constructor if any
            sb.Append(constructor);

            // Add the user script
            sb.Append(script);

            // override the propert IsStg to set it to tru;
            sb.Append("override protected bool IsStg { get { return true; }}\n\n");

            // close the class
            sb.Append("\n}\n");
        }

        /// <summary>
        /// The VB assembly is loaded on the first call to the CSharpCodeProvider.
        /// Keeps this routine outside of CreateAssembly to avoid loading both the
        /// CSharp compiler, the VB compiler and JSccript compiler.
        /// </summary>
        /// <returns></returns>
        private static CodeDomProvider CreateVBCompiler()
        {
            return new VBCodeProvider();
        }

        private string WrapScriptOuterVB(string innerCode)
        {
            // Add the using and name space definition
            if (!string.IsNullOrEmpty(innerCode))
            {
                // quick estimate for the string builder size
                int cNamespacesStrings = 0;
                foreach (string importNamespace in _importNamespaces)
                {
                    cNamespacesStrings += importNamespace.Length;
                }

                // Find the local namespace, System.Speech or Microsoft.SpeechServer
                SRID srid = SRID.ArrayOfNullIllegal;
                string speechNamespace = srid.GetType().Namespace;

                // Add the using
                string usingStatements = string.Format(CultureInfo.InvariantCulture, "#ExternalSource (\"{0}\", 1)\nImports System\nImports System.Collections.Generic\nImports System.Diagnostics\nImports {1}\nImports {1}.Recognition\nImports {1}.Recognition.SrgsGrammar\n", _preambuleMarker, speechNamespace);
                StringBuilder sbWhole = new(_script.Length + usingStatements.Length + 200);

                sbWhole.Append(usingStatements);
                foreach (string importNamespace in _importNamespaces)
                {
                    sbWhole.Append("Imports ");
                    sbWhole.Append(importNamespace);
                    sbWhole.Append('\n');
                }

                // Add the namespace definition
                if (_namespace != null)
                {
                    sbWhole.Append("Namespace ");
                    sbWhole.Append(_namespace);
                    sbWhole.Append('\n');
                }

                sbWhole.Append("#End ExternalSource\n");

                // Add all the classes
                sbWhole.Append(innerCode);

                // close the namespace if any
                if (_namespace != null)
                {
                    sbWhole.Append("End Namespace\n");
                }
                return sbWhole.ToString();
            }
            else
            {
                return null;
            }
        }

        private static void WrapClassVB(StringBuilder sb, string classname, string baseclass, CultureInfo culture, string script, string constructor)
        {
            // Add the class definition
            sb.Append("Public Partial class ");
            sb.Append(classname);
            sb.Append("\n Inherits ");
            sb.Append(!string.IsNullOrEmpty(baseclass) ? baseclass : "Grammar");
            sb.Append(" \n");

            // Only append the Association table for STG files.
            if (culture != null)
            {
                // Append the association class to CFGs
                sb.Append("<DebuggerBrowsable (DebuggerBrowsableState.Never)>Public Shared __cultureId as String = \"");
                sb.Append(culture.LCID.ToString(CultureInfo.InvariantCulture));
                sb.Append("\"\n");
            }

            // The constructor if any
            sb.Append(constructor);

            // Add the user script
            sb.Append(script);

            // override the propert IsStg to set it to tru;
            sb.Append("Protected Overrides ReadOnly Property IsStg() As Boolean\nGet\nReturn True\nEnd Get\nEnd Property\n");

            // close the class
            sb.Append("\nEnd Class\n");
        }

        private static void ThrowCompilationErrors(CompilerResults results)
        {
            StringBuilder sbErrors = new();
            foreach (CompilerError error in results.Errors)
            {
                if (sbErrors.Length > 0)
                {
                    sbErrors.Append('\n');
                }
                if (error.FileName.IndexOf(_preambuleMarker, StringComparison.Ordinal) == -1)
                {
                    sbErrors.Append(error.FileName);
                    sbErrors.Append('(');
                    sbErrors.Append(error.Line);
                    sbErrors.Append(',');
                    sbErrors.Append(error.Column);
                    sbErrors.Append("): ");
                }

                sbErrors.Append("error ");
                sbErrors.Append(error.ErrorNumber);
                sbErrors.Append(": ");
                sbErrors.Append(error.ErrorText);
            }
            XmlParser.ThrowSrgsException(SRID.GrammarCompilerError, sbErrors.ToString());
        }

        private static CompilerParameters GetCompilerParameters(string outputFile, List<CfgResource> cfgResources, bool debug, Collection<string> assemblyReferences, string keyfile)
        {
            CompilerParameters parameters = new();
            StringBuilder compilerOptions = new();

            // Get the compiler to use
            parameters.GenerateInMemory = false;
            parameters.OutputAssembly = outputFile;

            // Set the debug flag
            if (parameters.IncludeDebugInformation = debug)
            {
                // Add the debug FLAG
                compilerOptions.Append("/define:DEBUG ");
            }

            // Set the key file if any
            if (keyfile != null)
            {
                // Add the keyfile flag
                compilerOptions.Append("/keyfile:");
                compilerOptions.Append(keyfile);
            }
            parameters.CompilerOptions = compilerOptions.ToString();

            // add all the referenced dll
            parameters.ReferencedAssemblies.Add("System.dll");

            // add the assembly for System.Speech
            Assembly assembly = Assembly.GetExecutingAssembly();
            parameters.ReferencedAssemblies.Add(assembly.Location);

            foreach (string assemblyReference in assemblyReferences)
            {
                parameters.ReferencedAssemblies.Add(assemblyReference);
            }

            // add the cfgs as resources if any
            if (cfgResources != null)
            {
                foreach (CfgResource cfgResource in cfgResources)
                {
                    using (FileStream fs = new(cfgResource.name, FileMode.Create, FileAccess.Write))
                    {
                        using (BinaryWriter sw = new(fs))
                        {
                            sw.Write(cfgResource.data, 0, cfgResource.data.Length);
                            parameters.EmbeddedResources.Add(cfgResource.name);
                        }
                    }
                }
            }
            return parameters;
        }

        private void CheckValidAssembly(int iCfg, byte[] il)
        {
            // Check all methods referenced in the rule; availability, public and arguments
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            AppDomain appDomain = null;
            try
            {
                appDomain = AppDomain.CreateDomain("Loading Domain");
                // AppDomainCompilerProxy proxy = (AppDomainCompilerProxy) appDomain.CreateInstanceFromAndUnwrap (executingAssembly.GetName ().CodeBase, "System.Speech.Internal.SrgsCompiler.AppDomainCompilerProxy");
                AppDomainCompilerProxy proxy = new();

                // Marshalling between App domains prevents to use complex types as they cannot
                // be marhalled accross app domain boundaries. Use 3 arrays instead
                int count = _scriptRefs.Count;
                string[] rules = new string[count];
                string[] methods = new string[count];
                int[] methodScripts = new int[count];

                for (int i = 0; i < count; i++)
                {
                    ScriptRef scriptRef = _scriptRefs[i];
                    rules[i] = scriptRef._rule;
                    methods[i] = scriptRef._sMethod;
                    methodScripts[i] = (int)scriptRef._method;
                }

                // Marshalling of all parameters must be achieved
                Exception e = proxy.CheckAssembly(il, iCfg, _language, _namespace, rules, methods, methodScripts);

                // Throw the error if any
                if (e != null)
                {
                    throw e;
                }

                // Get the constructors and the types
                AssociateConstructorsWithRules(proxy, rules, _rules, iCfg, _language);
            }
            finally
            {
                if (appDomain != null)
                {
                    AppDomain.Unload(appDomain);
                    appDomain = null;
                }
            }
        }

        private static void AssociateConstructorsWithRules(AppDomainCompilerProxy proxy, string[] names, List<Rule> rules, int iCfg, string language)
        {
            string[] constructors = proxy.Constructors();

            // Build the constructors for the
            foreach (Rule rule in rules)
            {
                int i = 0;
                for (; i < names.Length && (i = Array.IndexOf(names, rule.Name, i)) >= 0; i++)
                {
                    if (constructors[i] != null)
                    {
                        rule.Constructors.Append(constructors[i]);
                    }
                }
                if (rule.Constructors.Length == 0)
                {
                    rule.Constructors.Append(proxy.GenerateConstructor(iCfg, Array.Empty<ParameterInfo>(), language, rule.Name));
                }
            }
        }

        private static byte[] ExtractCodeGenerated(string path)
        {
            byte[] data = null;
            if (!string.IsNullOrEmpty(path))
            {
                // return the memory blob with the IL for .Net Semantics
                using (FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    data = Helpers.ReadStreamToByteArray(fs, (int)fs.Length);
                }
            }
            return data;
        }
        #endregion

        private const string _preambuleMarker = "<Does Not Exist>";
    }
}
