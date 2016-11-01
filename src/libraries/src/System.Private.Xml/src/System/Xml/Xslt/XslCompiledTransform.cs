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
using System.Collections.Generic;
using System.Linq;

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

    public sealed class XslCompiledTransform
    {
        // Reader settings used when creating XmlReader from inputUri
        private static readonly XmlReaderSettings s_readerSettings = null;

        // Version for GeneratedCodeAttribute
        private const string Version = ThisAssembly.Version;

        static XslCompiledTransform()
        {
            s_readerSettings = new XmlReaderSettings();
        }

        // Options of compilation
        private bool _enableDebug = false;

        // Results of compilation
        private CompilerResults _compilerResults = null;
        private XmlWriterSettings _outputSettings = null;
        private QilExpression _qil = null;

        // Executable command for the compiled stylesheet
        private XmlILCommand _command = null;

        public XslCompiledTransform() { }

        public XslCompiledTransform(bool enableDebug)
        {
            _enableDebug = enableDebug;
        }

        /// <summary>
        /// This function is called on every recompilation to discard all previous results
        /// </summary>
        private void Reset()
        {
            _compilerResults = null;
            _outputSettings = null;
            _qil = null;
            _command = null;
        }

        internal CompilerErrorCollection Errors
        {
            get { return _compilerResults != null ? _compilerResults.Errors : null; }
        }

        /// <summary>
        /// Writer settings specified in the stylesheet
        /// </summary>
        public XmlWriterSettings OutputSettings
        {
            get
            {
                return _outputSettings;
            }
        }

        internal TempFileCollection TemporaryFiles
        {
            get { return _compilerResults != null ? _compilerResults.TempFiles : null; }
        }

        //------------------------------------------------
        // Load methods
        //------------------------------------------------

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Load(XmlReader stylesheet)
        {
            Reset();
            LoadInternal(stylesheet, XsltSettings.Default, XmlNullResolver.Singleton);
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Load(XmlReader stylesheet, XsltSettings settings, XmlResolver stylesheetResolver)
        {
            Reset();
            LoadInternal(stylesheet, settings, stylesheetResolver);
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Load(IXPathNavigable stylesheet)
        {
            Reset();
            LoadInternal(stylesheet, XsltSettings.Default, XmlNullResolver.Singleton);
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Load(IXPathNavigable stylesheet, XsltSettings settings, XmlResolver stylesheetResolver)
        {
            Reset();
            LoadInternal(stylesheet, settings, stylesheetResolver);
        }

        public void Load(string stylesheetUri)
        {
            Reset();
            if (stylesheetUri == null)
            {
                throw new ArgumentNullException(nameof(stylesheetUri));
            }
            LoadInternal(stylesheetUri, XsltSettings.Default, XmlNullResolver.Singleton);
        }

        public void Load(string stylesheetUri, XsltSettings settings, XmlResolver stylesheetResolver)
        {
            Reset();
            if (stylesheetUri == null)
            {
                throw new ArgumentNullException(nameof(stylesheetUri));
            }
            LoadInternal(stylesheetUri, settings, stylesheetResolver);
        }

        private CompilerResults LoadInternal(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver)
        {
            if (stylesheet == null)
            {
                throw new ArgumentNullException(nameof(stylesheet));
            }
            if (settings == null)
            {
                settings = XsltSettings.Default;
            }
            CompileXsltToQil(stylesheet, settings, stylesheetResolver);
            CompilerError error = GetFirstError();
            if (error != null)
            {
                throw new XslLoadException(error);
            }
            if (!settings.CheckOnly)
            {
                CompileQilToMsil(settings);
            }
            return _compilerResults;
        }

        private void CompileXsltToQil(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver)
        {
            //BinCompat TODO: Change arugment back to _enableDebug when all coomented-out dynamic modules and methods setup in XmlILModule is fixed
            _compilerResults = new Compiler(settings, false /*_enableDebug*/, null).Compile(stylesheet, stylesheetResolver, out _qil);
        }

        /// <summary>
        /// Returns the first compiler error except warnings
        /// </summary>
        private CompilerError GetFirstError()
        {
            foreach (CompilerError error in _compilerResults.Errors)
            {
                if (!error.IsWarning)
                {
                    return error;
                }
            }
            return null;
        }

        private void CompileQilToMsil(XsltSettings settings)
        {
            _command = new XmlILGenerator().Generate(_qil, /*typeBuilder:*/null);
            _outputSettings = _command.StaticData.DefaultWriterSettings;
            _qil = null;
        }

        //------------------------------------------------
        // Compile stylesheet to a TypeBuilder
        //------------------------------------------------

        private static volatile ConstructorInfo s_generatedCodeCtor;

        internal static CompilerErrorCollection CompileToType(XmlReader stylesheet, XsltSettings settings, XmlResolver stylesheetResolver, bool debug, TypeBuilder typeBuilder, string scriptAssemblyPath)
        {
            if (stylesheet == null)
                throw new ArgumentNullException(nameof(stylesheet));

            if (typeBuilder == null)
                throw new ArgumentNullException(nameof(typeBuilder));

            if (settings == null)
                settings = XsltSettings.Default;

            if (settings.EnableScript && scriptAssemblyPath == null)
                throw new ArgumentNullException(nameof(scriptAssemblyPath));

            if (scriptAssemblyPath != null)
                scriptAssemblyPath = Path.GetFullPath(scriptAssemblyPath);

            QilExpression qil;
            CompilerErrorCollection errors = new Compiler(settings, debug, scriptAssemblyPath).Compile(stylesheet, stylesheetResolver, out qil).Errors;

            if (!errors.HasErrors)
            {
                // Mark the type with GeneratedCodeAttribute to identify its origin
                if (s_generatedCodeCtor == null)
                    s_generatedCodeCtor = typeof(GeneratedCodeAttribute).GetConstructor(new Type[] { typeof(string), typeof(string) });

                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(s_generatedCodeCtor,
                    new object[] { typeof(XslCompiledTransform).FullName, Version }));

                new XmlILGenerator().Generate(qil, typeBuilder);
            }

            return errors;
        }

        //------------------------------------------------
        // Load compiled stylesheet from a Type
        //------------------------------------------------

        public void Load(Type compiledStylesheet)
        {
            Reset();
            if (compiledStylesheet == null)
                throw new ArgumentNullException(nameof(compiledStylesheet));

            object[] customAttrs = compiledStylesheet.GetCustomAttributes(typeof(GeneratedCodeAttribute), /*inherit:*/false);
            GeneratedCodeAttribute generatedCodeAttr = customAttrs.Length > 0 ? (GeneratedCodeAttribute)customAttrs[0] : null;

            // If GeneratedCodeAttribute is not there, it is not a compiled stylesheet class
            if (generatedCodeAttr != null && generatedCodeAttr.Tool == typeof(XslCompiledTransform).FullName)
            {
                if (new Version(Version).CompareTo(new Version(generatedCodeAttr.Version)) < 0)
                {
                    throw new ArgumentException(SR.Format(SR.Xslt_IncompatibleCompiledStylesheetVersion, generatedCodeAttr.Version, Version), nameof(compiledStylesheet));
                }

                FieldInfo fldData = compiledStylesheet.GetField(XmlQueryStaticData.DataFieldName, BindingFlags.Static | BindingFlags.NonPublic);
                FieldInfo fldTypes = compiledStylesheet.GetField(XmlQueryStaticData.TypesFieldName, BindingFlags.Static | BindingFlags.NonPublic);

                // If private fields are not there, it is not a compiled stylesheet class
                if (fldData != null && fldTypes != null)
                {
                    // Retrieve query static data from the type
                    byte[] queryData = fldData.GetValue(/*this:*/null) as byte[];

                    if (queryData != null)
                    {
                        MethodInfo executeMethod = compiledStylesheet.GetMethod("Execute", BindingFlags.Static | BindingFlags.NonPublic);
                        Type[] earlyBoundTypes = (Type[])fldTypes.GetValue(/*this:*/null);

                        // Load the stylesheet
                        Load(executeMethod, queryData, earlyBoundTypes);
                        return;
                    }
                }
            }

            // Throw an exception if the command was not loaded
            if (_command == null)
                throw new ArgumentException(SR.Format(SR.Xslt_NotCompiledStylesheet, compiledStylesheet.FullName), nameof(compiledStylesheet));
        }

        public void Load(MethodInfo executeMethod, byte[] queryData, Type[] earlyBoundTypes)
        {
            Reset();

            if (executeMethod == null)
                throw new ArgumentNullException(nameof(executeMethod));

            if (queryData == null)
                throw new ArgumentNullException(nameof(queryData));


            DynamicMethod dm = executeMethod as DynamicMethod;
            Delegate delExec = (dm != null) ? dm.CreateDelegate(typeof(ExecuteDelegate)) : executeMethod.CreateDelegate(typeof(ExecuteDelegate));
            _command = new XmlILCommand((ExecuteDelegate)delExec, new XmlQueryStaticData(queryData, earlyBoundTypes));
            _outputSettings = _command.StaticData.DefaultWriterSettings;
        }

        //------------------------------------------------
        // Transform methods which take an IXPathNavigable
        //------------------------------------------------

        public void Transform(IXPathNavigable input, XmlWriter results)
        {
            CheckArguments(input, results);
            Transform(input, (XsltArgumentList)null, results, XmlNullResolver.Singleton);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList arguments, XmlWriter results)
        {
            CheckArguments(input, results);
            Transform(input, arguments, results, XmlNullResolver.Singleton);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList arguments, TextWriter results)
        {
            CheckArguments(input, results);
            using (XmlWriter writer = XmlWriter.Create(results, OutputSettings))
            {
                Transform(input, arguments, writer, XmlNullResolver.Singleton);
                writer.Close();
            }
        }

        public void Transform(IXPathNavigable input, XsltArgumentList arguments, Stream results)
        {
            CheckArguments(input, results);
            using (XmlWriter writer = XmlWriter.Create(results, OutputSettings))
            {
                Transform(input, arguments, writer, XmlNullResolver.Singleton);
                writer.Close();
            }
        }

        //------------------------------------------------
        // Transform methods which take an XmlReader
        //------------------------------------------------

        public void Transform(XmlReader input, XmlWriter results)
        {
            CheckArguments(input, results);
            Transform(input, (XsltArgumentList)null, results, XmlNullResolver.Singleton);
        }

        public void Transform(XmlReader input, XsltArgumentList arguments, XmlWriter results)
        {
            CheckArguments(input, results);
            Transform(input, arguments, results, XmlNullResolver.Singleton);
        }

        public void Transform(XmlReader input, XsltArgumentList arguments, TextWriter results)
        {
            CheckArguments(input, results);
            using (XmlWriter writer = XmlWriter.Create(results, OutputSettings))
            {
                Transform(input, arguments, writer, XmlNullResolver.Singleton);
                writer.Close();
            }
        }

        public void Transform(XmlReader input, XsltArgumentList arguments, Stream results)
        {
            CheckArguments(input, results);
            using (XmlWriter writer = XmlWriter.Create(results, OutputSettings))
            {
                Transform(input, arguments, writer, XmlNullResolver.Singleton);
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
        public void Transform(string inputUri, XmlWriter results)
        {
            CheckArguments(inputUri, results);
            using (XmlReader reader = XmlReader.Create(inputUri, s_readerSettings))
            {
                Transform(reader, (XsltArgumentList)null, results, XmlNullResolver.Singleton);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings")]
        public void Transform(string inputUri, XsltArgumentList arguments, XmlWriter results)
        {
            CheckArguments(inputUri, results);
            using (XmlReader reader = XmlReader.Create(inputUri, s_readerSettings))
            {
                Transform(reader, arguments, results, XmlNullResolver.Singleton);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings")]
        public void Transform(string inputUri, XsltArgumentList arguments, TextWriter results)
        {
            CheckArguments(inputUri, results);
            using (XmlReader reader = XmlReader.Create(inputUri, s_readerSettings))
            using (XmlWriter writer = XmlWriter.Create(results, OutputSettings))
            {
                Transform(reader, arguments, writer, XmlNullResolver.Singleton);
                writer.Close();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings")]
        public void Transform(string inputUri, XsltArgumentList arguments, Stream results)
        {
            CheckArguments(inputUri, results);
            using (XmlReader reader = XmlReader.Create(inputUri, s_readerSettings))
            using (XmlWriter writer = XmlWriter.Create(results, OutputSettings))
            {
                Transform(reader, arguments, writer, XmlNullResolver.Singleton);
                writer.Close();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings")]
        public void Transform(string inputUri, string resultsFile)
        {
            if (inputUri == null)
                throw new ArgumentNullException(nameof(inputUri));

            if (resultsFile == null)
                throw new ArgumentNullException(nameof(resultsFile));

            // SQLBUDT 276415: Prevent wiping out the content of the input file if the output file is the same
            using (XmlReader reader = XmlReader.Create(inputUri, s_readerSettings))
            using (XmlWriter writer = XmlWriter.Create(resultsFile, OutputSettings))
            {
                Transform(reader, (XsltArgumentList)null, writer, XmlNullResolver.Singleton);
                writer.Close();
            }
        }

        //------------------------------------------------
        // Main Transform overloads
        //------------------------------------------------

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Transform(XmlReader input, XsltArgumentList arguments, XmlWriter results, XmlResolver documentResolver)
        {
            CheckArguments(input, results);
            CheckCommand();
            _command.Execute((object)input, documentResolver, arguments, results);
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Transform(IXPathNavigable input, XsltArgumentList arguments, XmlWriter results, XmlResolver documentResolver)
        {
            CheckArguments(input, results);
            CheckCommand();
            _command.Execute((object)input.CreateNavigator(), documentResolver, arguments, results);
        }

        //------------------------------------------------
        // Helper methods
        //------------------------------------------------

        private static void CheckArguments(object input, object results)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (results == null)
                throw new ArgumentNullException(nameof(results));
        }

        private static void CheckArguments(string inputUri, object results)
        {
            if (inputUri == null)
                throw new ArgumentNullException(nameof(inputUri));

            if (results == null)
                throw new ArgumentNullException(nameof(results));
        }

        private void CheckCommand()
        {
            if (_command == null)
            {
                throw new InvalidOperationException(SR.Xslt_NoStylesheetLoaded);
            }
        }

        //------------------------------------------------
        // Test suites entry points
        //------------------------------------------------

        private QilExpression TestCompile(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver)
        {
            Reset();
            CompileXsltToQil(stylesheet, settings, stylesheetResolver);
            return _qil;
        }

        private void TestGenerate(XsltSettings settings)
        {
            Debug.Assert(_qil != null, "You must compile to Qil first");
            CompileQilToMsil(settings);
        }

        private void Transform(string inputUri, XsltArgumentList arguments, XmlWriter results, XmlResolver documentResolver)
        {
            _command.Execute(inputUri, documentResolver, arguments, results);
        }

        internal static void PrintQil(object qil, XmlWriter xw, bool printComments, bool printTypes, bool printLineInfo)
        {
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
