// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml.XPath;
    using System.Xml.Xsl.XsltOld;
    using System.Xml.Xsl.XsltOld.Debugger;

    public sealed class XslTransform
    {
        private XmlResolver? _documentResolver;
        private bool _isDocumentResolverSet;
        private XmlResolver? _DocumentResolver
        {
            get
            {
                if (_isDocumentResolverSet)
                    return _documentResolver;
                else
                {
                    return CreateDefaultResolver();
                }
            }
        }


        //
        // Compiled stylesheet state
        //
        private Stylesheet? _CompiledStylesheet;
        private List<TheQuery>? _QueryStore;
        private RootAction? _RootAction;

        public XslTransform() { }

        public XmlResolver? XmlResolver
        {
            set
            {
                _documentResolver = value;
                _isDocumentResolverSet = true;
            }
        }

        public void Load(XmlReader stylesheet)
        {
            Load(stylesheet, CreateDefaultResolver());
        }
        public void Load(XmlReader stylesheet, XmlResolver? resolver)
        {
            ArgumentNullException.ThrowIfNull(stylesheet);

            Load(new XPathDocument(stylesheet, XmlSpace.Preserve), resolver);
        }

        public void Load(IXPathNavigable stylesheet)
        {
            Load(stylesheet, CreateDefaultResolver());
        }
        public void Load(IXPathNavigable stylesheet, XmlResolver? resolver)
        {
            ArgumentNullException.ThrowIfNull(stylesheet);

            Load(stylesheet.CreateNavigator()!, resolver);
        }

        public void Load(XPathNavigator stylesheet)
        {
            ArgumentNullException.ThrowIfNull(stylesheet);

            Load(stylesheet, CreateDefaultResolver());
        }

        public void Load(XPathNavigator stylesheet, XmlResolver? resolver)
        {
            ArgumentNullException.ThrowIfNull(stylesheet);

            Compile(stylesheet, resolver);
        }

        public void Load([StringSyntax(StringSyntaxAttribute.Uri)] string url)
        {
            XmlTextReaderImpl tr = new XmlTextReaderImpl(url);
            Compile(Compiler.LoadDocument(tr).CreateNavigator(), CreateDefaultResolver());
        }

        public void Load([StringSyntax(StringSyntaxAttribute.Uri)] string url, XmlResolver? resolver)
        {
            XmlTextReaderImpl tr = new XmlTextReaderImpl(url);
            {
                tr.XmlResolver = resolver;
            }
            Compile(Compiler.LoadDocument(tr).CreateNavigator(), resolver);
        }

        // ------------------------------------ Transform() ------------------------------------ //
        [MemberNotNull(nameof(_CompiledStylesheet))]
        [MemberNotNull(nameof(_QueryStore))]
        [MemberNotNull(nameof(_RootAction))]
        private void CheckCommand()
        {
            if (_CompiledStylesheet == null)
            {
                throw new InvalidOperationException(SR.Xslt_NoStylesheetLoaded);
            }

            Debug.Assert(_QueryStore != null);
            Debug.Assert(_RootAction != null);
        }

        public XmlReader Transform(XPathNavigator input, XsltArgumentList? args, XmlResolver? resolver)
        {
            CheckCommand();
            Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, null);
            return processor.StartReader();
        }

        public XmlReader Transform(XPathNavigator input, XsltArgumentList? args)
        {
            return Transform(input, args, _DocumentResolver);
        }

        public void Transform(XPathNavigator input, XsltArgumentList? args, XmlWriter output, XmlResolver? resolver)
        {
            CheckCommand();
            Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, null);
            processor.Execute(output);
        }

        public void Transform(XPathNavigator input, XsltArgumentList? args, XmlWriter output)
        {
            Transform(input, args, output, _DocumentResolver);
        }
        public void Transform(XPathNavigator input, XsltArgumentList? args, Stream output, XmlResolver? resolver)
        {
            CheckCommand();
            Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, null);
            processor.Execute(output);
        }

        public void Transform(XPathNavigator input, XsltArgumentList? args, Stream output)
        {
            Transform(input, args, output, _DocumentResolver);
        }

        public void Transform(XPathNavigator input, XsltArgumentList? args, TextWriter output, XmlResolver? resolver)
        {
            CheckCommand();
            Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, null);
            processor.Execute(output);
        }

        public void Transform(XPathNavigator input, XsltArgumentList? args, TextWriter output)
        {
            CheckCommand();
            Processor processor = new Processor(input, args, _DocumentResolver, _CompiledStylesheet, _QueryStore, _RootAction, null);
            processor.Execute(output);
        }

        public XmlReader Transform(IXPathNavigable input, XsltArgumentList? args, XmlResolver? resolver)
        {
            ArgumentNullException.ThrowIfNull(input);

            return Transform(input.CreateNavigator()!, args, resolver);
        }

        public XmlReader Transform(IXPathNavigable input, XsltArgumentList? args)
        {
            ArgumentNullException.ThrowIfNull(input);

            return Transform(input.CreateNavigator()!, args, _DocumentResolver);
        }
        public void Transform(IXPathNavigable input, XsltArgumentList? args, TextWriter output, XmlResolver? resolver)
        {
            ArgumentNullException.ThrowIfNull(input);

            Transform(input.CreateNavigator()!, args, output, resolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList? args, TextWriter output)
        {
            ArgumentNullException.ThrowIfNull(input);

            Transform(input.CreateNavigator()!, args, output, _DocumentResolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList? args, Stream output, XmlResolver? resolver)
        {
            ArgumentNullException.ThrowIfNull(input);

            Transform(input.CreateNavigator()!, args, output, resolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList? args, Stream output)
        {
            ArgumentNullException.ThrowIfNull(input);

            Transform(input.CreateNavigator()!, args, output, _DocumentResolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList? args, XmlWriter output, XmlResolver? resolver)
        {
            ArgumentNullException.ThrowIfNull(input);

            Transform(input.CreateNavigator()!, args, output, resolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList? args, XmlWriter output)
        {
            ArgumentNullException.ThrowIfNull(input);

            Transform(input.CreateNavigator()!, args, output, _DocumentResolver);
        }

        public void Transform(string inputfile, string outputfile, XmlResolver? resolver)
        {
            FileStream? fs = null;
            try
            {
                // We should read doc before creating output file in case they are the same
                XPathDocument doc = new XPathDocument(inputfile);
                fs = new FileStream(outputfile, FileMode.Create, FileAccess.ReadWrite);
                Transform(doc, /*args:*/null, fs, resolver);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Dispose();
                }
            }
        }

        public void Transform(string inputfile, string outputfile)
        {
            Transform(inputfile, outputfile, _DocumentResolver);
        }

        // Implementation

        private void Compile(XPathNavigator stylesheet, XmlResolver? resolver)
        {
            Debug.Assert(stylesheet != null);

            Compiler compiler = new Compiler();
            NavigatorInput input = new NavigatorInput(stylesheet);
            compiler.Compile(input, resolver ?? XmlNullResolver.Singleton);

            Debug.Assert(compiler.CompiledStylesheet != null);
            Debug.Assert(compiler.QueryStore != null);
            Debug.Assert(compiler.RootAction != null);
            _CompiledStylesheet = compiler.CompiledStylesheet;
            _QueryStore = compiler.QueryStore;
            _RootAction = compiler.RootAction;
        }

        private static XmlResolver CreateDefaultResolver()
        {
            if (LocalAppContextSwitches.AllowDefaultResolver)
            {
                return new XmlUrlResolver();
            }
            else
            {
                return XmlNullResolver.Singleton;
            }
        }
    }
}
