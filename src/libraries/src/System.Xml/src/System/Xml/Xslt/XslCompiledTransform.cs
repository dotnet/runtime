// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <spec>http://webdata/xml/specs/XslCompiledTransform.xml</spec>
//------------------------------------------------------------------------------

using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.Xslt;
using System.Runtime.Versioning;
using System.Xml.XmlConfiguration;

namespace System.Xml.Xsl
{
#if ! HIDE_XSL

    //----------------------------------------------------------------------------------------------------
    //  Clarification on null values in this API:
    //      stylesheet, stylesheetUri   - cannot be null
    //      settings                    - if null, XsltSettings.Default will be used
    //      stylesheetResolver          - if null, XmlNullResolver will be used for includes/imports.
    //                                    However, if the principal stylesheet is given by its URI, that
    //                                    URI will be resolved using XmlUrlResolver (for compatibility
    //                                    with XslTransform and XmlReader).
    //      typeBuilder                 - cannot be null
    //      scriptAssemblyPath          - can be null only if scripts are disabled
    //      compiledStylesheet          - cannot be null
    //      executeMethod, queryData    - cannot be null
    //      earlyBoundTypes             - null means no script types
    //      documentResolver            - if null, XmlNullResolver will be used
    //      input, inputUri             - cannot be null
    //      arguments                   - null means no arguments
    //      results, resultsFile        - cannot be null
    //----------------------------------------------------------------------------------------------------

    public sealed class XslCompiledTransform {
        // Reader settings used when creating XmlReader from inputUri
        private static readonly XmlReaderSettings ReaderSettings = null;

        // Version for GeneratedCodeAttribute
        private const string Version = ThisAssembly.Version;

        static XslCompiledTransform() {
            ReaderSettings = new XmlReaderSettings();
        }

        // Options of compilation
        private bool                enableDebug     = false;

        // Results of compilation
        private CompilerResults     compilerResults = null;
        private XmlWriterSettings   outputSettings  = null;
        private QilExpression       qil             = null;

        // Executable command for the compiled stylesheet
        private XmlILCommand        command         = null;

        public XslCompiledTransform() {}

        public XslCompiledTransform(bool enableDebug) {
            this.enableDebug = enableDebug;
        }

        /// <summary>
        /// This function is called on every recompilation to discard all previous results
        /// </summary>
        private void Reset() {
            this.compilerResults = null;
            this.outputSettings  = null;
            this.qil             = null;
            this.command         = null;
        }

        internal CompilerErrorCollection Errors {
            get { return this.compilerResults != null ? this.compilerResults.Errors : null; }
        }

        /// <summary>
        /// Writer settings specified in the stylesheet
        /// </summary>
        public XmlWriterSettings OutputSettings {
            get {
                return this.outputSettings; 
            }
        }

        public TempFileCollection TemporaryFiles {
            get { return this.compilerResults != null ? this.compilerResults.TempFiles : null; }
        }

        //------------------------------------------------
        // Load methods
        //------------------------------------------------

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Load(XmlReader stylesheet) {
            Reset();
            LoadInternal(stylesheet, XsltSettings.Default, XsltConfigSection.CreateDefaultResolver());
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Load(XmlReader stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) {
            Reset();
            LoadInternal(stylesheet, settings, stylesheetResolver);
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Load(IXPathNavigable stylesheet) {
            Reset();
            LoadInternal(stylesheet, XsltSettings.Default, XsltConfigSection.CreateDefaultResolver());
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Load(IXPathNavigable stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) {
            Reset();
            LoadInternal(stylesheet, settings, stylesheetResolver);
        }
        
        public void Load(string stylesheetUri) {
            Reset();
            if (stylesheetUri == null) {
                throw new ArgumentNullException("stylesheetUri");
            }
            LoadInternal(stylesheetUri, XsltSettings.Default, XsltConfigSection.CreateDefaultResolver());
        }

        public void Load(string stylesheetUri, XsltSettings settings, XmlResolver stylesheetResolver) {
            Reset();
            if (stylesheetUri == null) {
                throw new ArgumentNullException("stylesheetUri");
            }
            LoadInternal(stylesheetUri, settings, stylesheetResolver);
        }

        private CompilerResults LoadInternal(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) {
            if (stylesheet == null) {
                throw new ArgumentNullException("stylesheet");
            }
            if (settings == null) {
                settings = XsltSettings.Default;
            }
            CompileXsltToQil(stylesheet, settings, stylesheetResolver);
            CompilerError error = GetFirstError();
            if (error != null) {
                throw new XslLoadException(error);
            }
            if (!settings.CheckOnly) {
                CompileQilToMsil(settings);
            }
            return this.compilerResults;
        }

        private void CompileXsltToQil(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) {
            this.compilerResults = new Compiler(settings, this.enableDebug, null).Compile(stylesheet, stylesheetResolver, out this.qil);
        }

        /// <summary>
        /// Returns the first compiler error except warnings
        /// </summary>
        private CompilerError GetFirstError() {
            foreach (CompilerError error in compilerResults.Errors) {
                if (!error.IsWarning) {
                    return error;
                }
            }
            return null;
        }

        private void CompileQilToMsil(XsltSettings settings) {
            this.command = new XmlILGenerator().Generate(this.qil, /*typeBuilder:*/null);
            this.outputSettings = this.command.StaticData.DefaultWriterSettings;
            this.qil = null;
        }

        //------------------------------------------------
        // Compile stylesheet to a TypeBuilder
        //------------------------------------------------

        private static volatile ConstructorInfo GeneratedCodeCtor;
       
        public static CompilerErrorCollection CompileToType(XmlReader stylesheet, XsltSettings settings, XmlResolver stylesheetResolver, bool debug, TypeBuilder typeBuilder, string scriptAssemblyPath) {
            if (stylesheet == null)
                throw new ArgumentNullException("stylesheet");

            if (typeBuilder == null)
                throw new ArgumentNullException("typeBuilder");

            if (settings == null)
                settings = XsltSettings.Default;

            if (settings.EnableScript && scriptAssemblyPath == null)
                throw new ArgumentNullException("scriptAssemblyPath");

            if (scriptAssemblyPath != null)
                scriptAssemblyPath = Path.GetFullPath(scriptAssemblyPath);

            QilExpression qil;
            CompilerErrorCollection errors = new Compiler(settings, debug, scriptAssemblyPath).Compile(stylesheet, stylesheetResolver, out qil).Errors;

            if (!errors.HasErrors) {
                // Mark the type with GeneratedCodeAttribute to identify its origin
                if (GeneratedCodeCtor == null)
                    GeneratedCodeCtor = typeof(GeneratedCodeAttribute).GetConstructor(new Type[] { typeof(string), typeof(string) });

                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(GeneratedCodeCtor,
                    new object[] { typeof(XslCompiledTransform).FullName, Version }));

                new XmlILGenerator().Generate(qil, typeBuilder);
            }

            return errors;
        }

        //------------------------------------------------
        // Load compiled stylesheet from a Type
        //------------------------------------------------

        public void Load(Type compiledStylesheet) {
            Reset();
            if (compiledStylesheet == null)
                throw new ArgumentNullException("compiledStylesheet");

            object[] customAttrs = compiledStylesheet.GetCustomAttributes(typeof(GeneratedCodeAttribute), /*inherit:*/false);
            GeneratedCodeAttribute generatedCodeAttr = customAttrs.Length > 0 ? (GeneratedCodeAttribute)customAttrs[0] : null;

            // If GeneratedCodeAttribute is not there, it is not a compiled stylesheet class
            if (generatedCodeAttr != null && generatedCodeAttr.Tool == typeof(XslCompiledTransform).FullName) {

                if(new Version(Version).CompareTo(new Version(generatedCodeAttr.Version)) < 0) {
                    throw new ArgumentException(string.Format(Res.Xslt_IncompatibleCompiledStylesheetVersion, generatedCodeAttr.Version, Version), "compiledStylesheet");
                }

                FieldInfo fldData  = compiledStylesheet.GetField(XmlQueryStaticData.DataFieldName,  BindingFlags.Static | BindingFlags.NonPublic);
                FieldInfo fldTypes = compiledStylesheet.GetField(XmlQueryStaticData.TypesFieldName, BindingFlags.Static | BindingFlags.NonPublic);

                // If private fields are not there, it is not a compiled stylesheet class
                if (fldData != null && fldTypes != null) {
                    /*if (System.Xml.XmlConfiguration.XsltConfigSection.EnableMemberAccessForXslCompiledTransform)
                    {
                        // Need MemberAccess reflection permission to access a private data field and create a delegate
                        new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();
                    }*/

                    // Retrieve query static data from the type
                    byte[] queryData = fldData.GetValue(/*this:*/null) as byte[];

                    if (queryData != null) {
                        MethodInfo executeMethod = compiledStylesheet.GetMethod("Execute", BindingFlags.Static | BindingFlags.NonPublic);
                        Type[] earlyBoundTypes = (Type[])fldTypes.GetValue(/*this:*/null);

                        // Load the stylesheet
                        Load(executeMethod, queryData, earlyBoundTypes);                        
                        return;
                    }
                }
            }

            // Throw an exception if the command was not loaded
            if (this.command == null)
                throw new ArgumentException(string.Format(Res.Xslt_NotCompiledStylesheet, compiledStylesheet.FullName), "compiledStylesheet");
        }

        public void Load(MethodInfo executeMethod, byte[] queryData, Type[] earlyBoundTypes) {
            Reset();

            if (executeMethod == null)
                throw new ArgumentNullException("executeMethod");

            if (queryData == null)
                throw new ArgumentNullException("queryData");

            // earlyBoundTypes may be null

            /*if (!System.Xml.XmlConfiguration.XsltConfigSection.EnableMemberAccessForXslCompiledTransform)
            {
                // make sure we have permission to create the delegate if the type is not visible.
                // If the declaring type is null we cannot check for visibility.
                // NOTE: a DynamicMethod will always have a DeclaringType == null. DynamicMethods will do demand on their own if skipVisibility is true. 
                if (executeMethod.DeclaringType != null && !executeMethod.DeclaringType.IsVisible)
                {
                    new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
                }
            }*/

            DynamicMethod dm = executeMethod as DynamicMethod;
            Delegate delExec = (dm != null) ? dm.CreateDelegate(typeof(ExecuteDelegate)) : Delegate.CreateDelegate(typeof(ExecuteDelegate), executeMethod);
            this.command = new XmlILCommand((ExecuteDelegate)delExec, new XmlQueryStaticData(queryData, earlyBoundTypes));
            this.outputSettings = this.command.StaticData.DefaultWriterSettings;
        }

        //------------------------------------------------
        // Transform methods which take an IXPathNavigable
        //------------------------------------------------

        public void Transform(IXPathNavigable input, XmlWriter results) {
            CheckArguments(input, results);
            Transform(input, (XsltArgumentList)null, results, XsltConfigSection.CreateDefaultResolver());
        }

        public void Transform(IXPathNavigable input, XsltArgumentList arguments, XmlWriter results) {
            CheckArguments(input, results);
            Transform(input, arguments, results, XsltConfigSection.CreateDefaultResolver());
        }

        public void Transform(IXPathNavigable input, XsltArgumentList arguments, TextWriter results) {
            CheckArguments(input, results);
            using (XmlWriter writer = XmlWriter.Create(results, OutputSettings)) {
                Transform(input, arguments, writer, XsltConfigSection.CreateDefaultResolver());
                writer.Close();
            }
        }

        public void Transform(IXPathNavigable input, XsltArgumentList arguments, Stream results) {
            CheckArguments(input, results);
            using (XmlWriter writer = XmlWriter.Create(results, OutputSettings)) {
                Transform(input, arguments, writer, XsltConfigSection.CreateDefaultResolver());
                writer.Close();
            }
        }

        //------------------------------------------------
        // Transform methods which take an XmlReader
        //------------------------------------------------

        public void Transform(XmlReader input, XmlWriter results) {
            CheckArguments(input, results);
            Transform(input, (XsltArgumentList)null, results, XsltConfigSection.CreateDefaultResolver());
        }

        public void Transform(XmlReader input, XsltArgumentList arguments, XmlWriter results) {
            CheckArguments(input, results);
            Transform(input, arguments, results, XsltConfigSection.CreateDefaultResolver());
        }

        public void Transform(XmlReader input, XsltArgumentList arguments, TextWriter results) {
            CheckArguments(input, results);
            using (XmlWriter writer = XmlWriter.Create(results, OutputSettings)) {
                Transform(input, arguments, writer, XsltConfigSection.CreateDefaultResolver());
                writer.Close();
            }
        }

        public void Transform(XmlReader input, XsltArgumentList arguments, Stream results) {
            CheckArguments(input, results);
            using (XmlWriter writer = XmlWriter.Create(results, OutputSettings)) {
                Transform(input, arguments, writer, XsltConfigSection.CreateDefaultResolver());
                writer.Close();
            }
        }

        //------------------------------------------------
        // Transform methods which take a uri
        // SxS Note: Annotations should propagate to the caller to have him either check that 
        // the passed URIs are SxS safe or decide that they don't have to be SxS safe and 
        // suppress the message. 
        //------------------------------------------------

        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings")]
        public void Transform(string inputUri, XmlWriter results) {
            CheckArguments(inputUri, results);
            using (XmlReader reader = XmlReader.Create(inputUri, ReaderSettings)) {
                Transform(reader, (XsltArgumentList)null, results, XsltConfigSection.CreateDefaultResolver());
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings")]
        public void Transform(string inputUri, XsltArgumentList arguments, XmlWriter results) {
            CheckArguments(inputUri, results);
            using (XmlReader reader = XmlReader.Create(inputUri, ReaderSettings)) {
                Transform(reader, arguments, results, XsltConfigSection.CreateDefaultResolver());
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings")]
        public void Transform(string inputUri, XsltArgumentList arguments, TextWriter results) {
            CheckArguments(inputUri, results);
            using (XmlReader reader = XmlReader.Create(inputUri, ReaderSettings))
            using (XmlWriter writer = XmlWriter.Create(results, OutputSettings)) {
                Transform(reader, arguments, writer, XsltConfigSection.CreateDefaultResolver());
                writer.Close();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings")]
        public void Transform(string inputUri, XsltArgumentList arguments, Stream results) {
            CheckArguments(inputUri, results);
            using (XmlReader reader = XmlReader.Create(inputUri, ReaderSettings))
            using (XmlWriter writer = XmlWriter.Create(results, OutputSettings)) {
                Transform(reader, arguments, writer, XsltConfigSection.CreateDefaultResolver());
                writer.Close();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings")]
        public void Transform(string inputUri, string resultsFile) {
            if (inputUri == null)
                throw new ArgumentNullException("inputUri");

            if (resultsFile == null)
                throw new ArgumentNullException("resultsFile");

            // SQLBUDT 276415: Prevent wiping out the content of the input file if the output file is the same
            using (XmlReader reader = XmlReader.Create(inputUri, ReaderSettings))
            using (XmlWriter writer = XmlWriter.Create(resultsFile, OutputSettings)) {
                Transform(reader, (XsltArgumentList)null, writer, XsltConfigSection.CreateDefaultResolver());
                writer.Close();
            }
        }

        //------------------------------------------------
        // Main Transform overloads
        //------------------------------------------------

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Transform(XmlReader input, XsltArgumentList arguments, XmlWriter results, XmlResolver documentResolver) {
            CheckArguments(input, results);
            CheckCommand();
            this.command.Execute((object)input, documentResolver, arguments, results);
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Transform(IXPathNavigable input, XsltArgumentList arguments, XmlWriter results, XmlResolver documentResolver) {
            CheckArguments(input, results);
            CheckCommand();
            this.command.Execute((object)input.CreateNavigator(), documentResolver, arguments, results);
        }

        //------------------------------------------------
        // Helper methods
        //------------------------------------------------

        private static void CheckArguments(object input, object results) {
            if (input == null)
                throw new ArgumentNullException("input");

            if (results == null)
                throw new ArgumentNullException("results");
        }

        private static void CheckArguments(string inputUri, object results) {
            if (inputUri == null)
                throw new ArgumentNullException("inputUri");

            if (results == null)
                throw new ArgumentNullException("results");
        }

        private void CheckCommand() {
            if (this.command == null) {
                throw new InvalidOperationException(string.Format(Res.Xslt_NoStylesheetLoaded));
            }
        }

        //------------------------------------------------
        // Test suites entry points
        //------------------------------------------------

        private QilExpression TestCompile(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) {
            Reset();
            CompileXsltToQil(stylesheet, settings, stylesheetResolver);
            return qil;
        }

        private void TestGenerate(XsltSettings settings) {
            Debug.Assert(qil != null, "You must compile to Qil first");
            CompileQilToMsil(settings);
        }

        private void Transform(string inputUri, XsltArgumentList arguments, XmlWriter results, XmlResolver documentResolver) {
            command.Execute(inputUri, documentResolver, arguments, results);
        }

        internal static void PrintQil(object qil, XmlWriter xw, bool printComments, bool printTypes, bool printLineInfo) {
            QilExpression qilExpr = (QilExpression)qil;
            QilXmlWriter.Options options = QilXmlWriter.Options.None;
            QilValidationVisitor.Validate(qilExpr);
            if (printComments) options |= QilXmlWriter.Options.Annotations;
            if (printTypes) options |= QilXmlWriter.Options.TypeInfo;
            if (printLineInfo) options |= QilXmlWriter.Options.LineInfo;
            QilXmlWriter qw = new QilXmlWriter(xw, options);
            qw.ToXml(qilExpr);
            xw.Flush();
        }
    }
#endif // ! HIDE_XSL
}
