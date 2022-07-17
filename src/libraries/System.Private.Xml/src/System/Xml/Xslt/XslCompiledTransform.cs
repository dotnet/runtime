// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// <spec>http://webdata/xml/specs/XslCompiledTransform.xml</spec>
//------------------------------------------------------------------------------

using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.Xslt;

namespace System.Xml.Xsl
{
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
        // Version for GeneratedCodeAttribute
        private static readonly Version? s_version = typeof(XslCompiledTransform).Assembly.GetName().Version;

        // Options of compilation
        private readonly bool _enableDebug;

        // Results of compilation
        private CompilerErrorCollection? _compilerErrorColl;
        private QilExpression? _qil;

        // Executable command for the compiled stylesheet
        private XmlILCommand? _command;

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
            _compilerErrorColl = null;
            OutputSettings = null;
            _qil = null;
            _command = null;
        }

        /// <summary>
        /// Writer settings specified in the stylesheet
        /// </summary>
        public XmlWriterSettings? OutputSettings { get; private set; }

        //------------------------------------------------
        // Load methods
        //------------------------------------------------

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Load(XmlReader stylesheet)
        {
            Reset();
            LoadInternal(stylesheet, XsltSettings.Default, CreateDefaultResolver());
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Load(XmlReader stylesheet, XsltSettings? settings, XmlResolver? stylesheetResolver)
        {
            Reset();
            LoadInternal(stylesheet, settings, stylesheetResolver);
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Load(IXPathNavigable stylesheet)
        {
            Reset();
            LoadInternal(stylesheet, XsltSettings.Default, CreateDefaultResolver());
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Load(IXPathNavigable stylesheet, XsltSettings? settings, XmlResolver? stylesheetResolver)
        {
            Reset();
            LoadInternal(stylesheet, settings, stylesheetResolver);
        }

        public void Load(string stylesheetUri)
        {
            Reset();
            ArgumentNullException.ThrowIfNull(stylesheetUri);
            LoadInternal(stylesheetUri, XsltSettings.Default, CreateDefaultResolver());
        }

        public void Load(string stylesheetUri, XsltSettings? settings, XmlResolver? stylesheetResolver)
        {
            Reset();
            ArgumentNullException.ThrowIfNull(stylesheetUri);
            LoadInternal(stylesheetUri, settings, stylesheetResolver);
        }

        private void LoadInternal(object stylesheet, XsltSettings? settings, XmlResolver? stylesheetResolver)
        {
            ArgumentNullException.ThrowIfNull(stylesheet);

            settings ??= XsltSettings.Default;
            CompileXsltToQil(stylesheet, settings, stylesheetResolver);
            CompilerError? error = GetFirstError();
            if (error != null)
            {
                throw new XslLoadException(error);
            }
            if (!settings.CheckOnly)
            {
                CompileQilToMsil(settings);
            }
        }

        [MemberNotNull(nameof(_compilerErrorColl))]
        [MemberNotNull(nameof(_qil))]
        private void CompileXsltToQil(object stylesheet, XsltSettings settings, XmlResolver? stylesheetResolver)
        {
            _compilerErrorColl = new Compiler(settings, _enableDebug, null).Compile(stylesheet, stylesheetResolver, out _qil);
        }

        /// <summary>
        /// Returns the first compiler error except warnings
        /// </summary>
        private CompilerError? GetFirstError()
        {
            foreach (CompilerError error in _compilerErrorColl!)
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
            _command = new XmlILGenerator().Generate(_qil!, null)!;
            OutputSettings = _command.StaticData.DefaultWriterSettings;
            _qil = null;
        }

        //------------------------------------------------
        // Load compiled stylesheet from a Type
        //------------------------------------------------
        [RequiresUnreferencedCode("This method will get fields and types from the assembly of the passed in compiledStylesheet and call their constructors which cannot be statically analyzed")]
        public void Load(Type compiledStylesheet)
        {
            Reset();
            ArgumentNullException.ThrowIfNull(compiledStylesheet);
            object[] customAttrs = compiledStylesheet.GetCustomAttributes(typeof(GeneratedCodeAttribute), false);
            GeneratedCodeAttribute? generatedCodeAttr = customAttrs.Length > 0 ? (GeneratedCodeAttribute)customAttrs[0] : null;

            // If GeneratedCodeAttribute is not there, it is not a compiled stylesheet class
            if (generatedCodeAttr != null && generatedCodeAttr.Tool == typeof(XslCompiledTransform).FullName)
            {
                if (s_version < Version.Parse(generatedCodeAttr.Version!))
                {
                    throw new ArgumentException(SR.Format(SR.Xslt_IncompatibleCompiledStylesheetVersion, generatedCodeAttr.Version, s_version), nameof(compiledStylesheet));
                }

                FieldInfo? fldData = compiledStylesheet.GetField(XmlQueryStaticData.DataFieldName, BindingFlags.Static | BindingFlags.NonPublic);
                FieldInfo? fldTypes = compiledStylesheet.GetField(XmlQueryStaticData.TypesFieldName, BindingFlags.Static | BindingFlags.NonPublic);

                // If private fields are not there, it is not a compiled stylesheet class
                if (fldData != null && fldTypes != null)
                {
                    // Retrieve query static data from the type
                    byte[]? queryData = fldData.GetValue(/*this:*/null) as byte[];

                    if (queryData != null)
                    {
                        MethodInfo? executeMethod = compiledStylesheet.GetMethod("Execute", BindingFlags.Static | BindingFlags.NonPublic);
                        Type[]? earlyBoundTypes = (Type[]?)fldTypes.GetValue(null);

                        // Load the stylesheet
                        Load(executeMethod!, queryData, earlyBoundTypes);
                        return;
                    }
                }
            }

            // Throw an exception if the command was not loaded
            if (_command == null)
            {
                throw new ArgumentException(SR.Format(SR.Xslt_NotCompiledStylesheet, compiledStylesheet.FullName), nameof(compiledStylesheet));
            }
        }

        [RequiresUnreferencedCode("This method will call into constructors of the earlyBoundTypes array which cannot be statically analyzed.")]
        public void Load(MethodInfo executeMethod, byte[] queryData, Type[]? earlyBoundTypes)
        {
            Reset();
            ArgumentNullException.ThrowIfNull(executeMethod);
            ArgumentNullException.ThrowIfNull(queryData);

            Delegate delExec = executeMethod is DynamicMethod dm
                ? dm.CreateDelegate(typeof(ExecuteDelegate))
                : executeMethod.CreateDelegate(typeof(ExecuteDelegate));

            _command = new XmlILCommand((ExecuteDelegate)delExec, new XmlQueryStaticData(queryData, earlyBoundTypes));
            OutputSettings = _command.StaticData.DefaultWriterSettings;
        }

        //------------------------------------------------
        // Transform methods which take an IXPathNavigable
        //------------------------------------------------

        public void Transform(IXPathNavigable input, XmlWriter results)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(results);

            Transform(input, null, results, CreateDefaultResolver());
        }

        public void Transform(IXPathNavigable input, XsltArgumentList? arguments, XmlWriter results)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(results);

            Transform(input, arguments, results, CreateDefaultResolver());
        }

        public void Transform(IXPathNavigable input, XsltArgumentList? arguments, TextWriter results)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(results);

            using XmlWriter writer = XmlWriter.Create(results, OutputSettings);
            Transform(input, arguments, writer, CreateDefaultResolver());
        }

        public void Transform(IXPathNavigable input, XsltArgumentList? arguments, Stream results)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(results);

            using XmlWriter writer = XmlWriter.Create(results, OutputSettings);
            Transform(input, arguments, writer, CreateDefaultResolver());
        }

        //------------------------------------------------
        // Transform methods which take an XmlReader
        //------------------------------------------------

        public void Transform(XmlReader input, XmlWriter results)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(results);

            Transform(input, null, results, CreateDefaultResolver());
        }

        public void Transform(XmlReader input, XsltArgumentList? arguments, XmlWriter results)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(results);

            Transform(input, arguments, results, CreateDefaultResolver());
        }

        public void Transform(XmlReader input, XsltArgumentList? arguments, TextWriter results)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(results);

            using XmlWriter writer = XmlWriter.Create(results, OutputSettings);
            Transform(input, arguments, writer, CreateDefaultResolver());
        }

        public void Transform(XmlReader input, XsltArgumentList? arguments, Stream results)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(results);

            using XmlWriter writer = XmlWriter.Create(results, OutputSettings);
            Transform(input, arguments, writer, CreateDefaultResolver());
        }

        //------------------------------------------------
        // Transform methods which take a uri
        // SxS Note: Annotations should propagate to the caller to have them either check that
        // the passed URIs are SxS safe or decide that they don't have to be SxS safe and
        // suppress the message.
        //------------------------------------------------

        public void Transform(string inputUri, XmlWriter results)
        {
            ArgumentNullException.ThrowIfNull(inputUri);
            ArgumentNullException.ThrowIfNull(results);

            using XmlReader reader = XmlReader.Create(inputUri);
            Transform(reader, null, results, CreateDefaultResolver());
        }

        public void Transform(string inputUri, XsltArgumentList? arguments, XmlWriter results)
        {
            ArgumentNullException.ThrowIfNull(inputUri);
            ArgumentNullException.ThrowIfNull(results);

            using XmlReader reader = XmlReader.Create(inputUri);
            Transform(reader, arguments, results, CreateDefaultResolver());
        }

        public void Transform(string inputUri, XsltArgumentList? arguments, TextWriter results)
        {
            ArgumentNullException.ThrowIfNull(inputUri);
            ArgumentNullException.ThrowIfNull(results);

            using XmlReader reader = XmlReader.Create(inputUri);
            using XmlWriter writer = XmlWriter.Create(results, OutputSettings);
            Transform(reader, arguments, writer, CreateDefaultResolver());
        }

        public void Transform(string inputUri, XsltArgumentList? arguments, Stream results)
        {
            ArgumentNullException.ThrowIfNull(inputUri);
            ArgumentNullException.ThrowIfNull(results);

            using XmlReader reader = XmlReader.Create(inputUri);
            using XmlWriter writer = XmlWriter.Create(results, OutputSettings);
            Transform(reader, arguments, writer, CreateDefaultResolver());
        }

        public void Transform(string inputUri, string resultsFile)
        {
            ArgumentNullException.ThrowIfNull(inputUri);
            ArgumentNullException.ThrowIfNull(resultsFile);

            // SQLBUDT 276415: Prevent wiping out the content of the input file if the output file is the same
            using XmlReader reader = XmlReader.Create(inputUri);
            using XmlWriter writer = XmlWriter.Create(resultsFile, OutputSettings);
            Transform(reader, null, writer, CreateDefaultResolver());
        }

        //------------------------------------------------
        // Main Transform overloads
        //------------------------------------------------

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Transform(XmlReader input, XsltArgumentList? arguments, XmlWriter results, XmlResolver? documentResolver)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(results);

            CheckCommand();
            _command.Execute(input, documentResolver, arguments, results);
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void Transform(IXPathNavigable input, XsltArgumentList? arguments, XmlWriter results, XmlResolver? documentResolver)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(results);

            CheckCommand();
            _command.Execute(input.CreateNavigator()!, documentResolver, arguments, results);
        }

        [MemberNotNull(nameof(_command))]
        private void CheckCommand()
        {
            if (_command == null)
            {
                throw new InvalidOperationException(SR.Xslt_NoStylesheetLoaded);
            }
        }

        private static XmlResolver CreateDefaultResolver()
        {
            if (LocalAppContextSwitches.AllowDefaultResolver)
            {
                return new XmlUrlResolver();
            }

            return XmlNullResolver.Singleton;
        }
    }
}
