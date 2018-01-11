// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl
{
    using System.Reflection;
    using System.Diagnostics;
    using System.IO;
    using System.Xml.XPath;
    using System.Xml.Xsl.XsltOld;
    using MS.Internal.Xml.XPath;
    using MS.Internal.Xml.Cache;
    using System.Collections.Generic;
    using System.Xml.Xsl.XsltOld.Debugger;
    using System.Runtime.Versioning;

    public sealed class XslTransform
    {
        private XmlResolver _documentResolver = null;
        private bool _isDocumentResolverSet = false;
        private XmlResolver _DocumentResolver
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
        private Stylesheet _CompiledStylesheet;
        private List<TheQuery> _QueryStore;
        private RootAction _RootAction;

        private IXsltDebugger _debugger;

        public XslTransform() { }

        public XmlResolver XmlResolver
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
        public void Load(XmlReader stylesheet, XmlResolver resolver)
        {
            if (stylesheet == null)
            {
                throw new ArgumentNullException(nameof(stylesheet));
            }
            Load(new XPathDocument(stylesheet, XmlSpace.Preserve), resolver);
        }

        public void Load(IXPathNavigable stylesheet)
        {
            Load(stylesheet, CreateDefaultResolver());
        }
        public void Load(IXPathNavigable stylesheet, XmlResolver resolver)
        {
            if (stylesheet == null)
            {
                throw new ArgumentNullException(nameof(stylesheet));
            }
            Load(stylesheet.CreateNavigator(), resolver);
        }

        public void Load(XPathNavigator stylesheet)
        {
            if (stylesheet == null)
            {
                throw new ArgumentNullException(nameof(stylesheet));
            }
            Load(stylesheet, CreateDefaultResolver());
        }

        public void Load(XPathNavigator stylesheet, XmlResolver resolver)
        {
            if (stylesheet == null)
            {
                throw new ArgumentNullException(nameof(stylesheet));
            }
            Compile(stylesheet, resolver);
        }

        public void Load(string url)
        {
            XmlTextReaderImpl tr = new XmlTextReaderImpl(url);
            Compile(Compiler.LoadDocument(tr).CreateNavigator(), CreateDefaultResolver());
        }

        public void Load(string url, XmlResolver resolver)
        {
            XmlTextReaderImpl tr = new XmlTextReaderImpl(url);
            {
                tr.XmlResolver = resolver;
            }
            Compile(Compiler.LoadDocument(tr).CreateNavigator(), resolver);
        }

        // ------------------------------------ Transform() ------------------------------------ //

        private void CheckCommand()
        {
            if (_CompiledStylesheet == null)
            {
                throw new InvalidOperationException(SR.Xslt_NoStylesheetLoaded);
            }
        }

        public XmlReader Transform(XPathNavigator input, XsltArgumentList args, XmlResolver resolver)
        {
            CheckCommand();
            Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, _debugger);
            return processor.StartReader();
        }

        public XmlReader Transform(XPathNavigator input, XsltArgumentList args)
        {
            return Transform(input, args, _DocumentResolver);
        }

        public void Transform(XPathNavigator input, XsltArgumentList args, XmlWriter output, XmlResolver resolver)
        {
            CheckCommand();
            Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, _debugger);
            processor.Execute(output);
        }

        public void Transform(XPathNavigator input, XsltArgumentList args, XmlWriter output)
        {
            Transform(input, args, output, _DocumentResolver);
        }
        public void Transform(XPathNavigator input, XsltArgumentList args, Stream output, XmlResolver resolver)
        {
            CheckCommand();
            Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, _debugger);
            processor.Execute(output);
        }

        public void Transform(XPathNavigator input, XsltArgumentList args, Stream output)
        {
            Transform(input, args, output, _DocumentResolver);
        }

        public void Transform(XPathNavigator input, XsltArgumentList args, TextWriter output, XmlResolver resolver)
        {
            CheckCommand();
            Processor processor = new Processor(input, args, resolver, _CompiledStylesheet, _QueryStore, _RootAction, _debugger);
            processor.Execute(output);
        }

        public void Transform(XPathNavigator input, XsltArgumentList args, TextWriter output)
        {
            CheckCommand();
            Processor processor = new Processor(input, args, _DocumentResolver, _CompiledStylesheet, _QueryStore, _RootAction, _debugger);
            processor.Execute(output);
        }

        public XmlReader Transform(IXPathNavigable input, XsltArgumentList args, XmlResolver resolver)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            return Transform(input.CreateNavigator(), args, resolver);
        }

        public XmlReader Transform(IXPathNavigable input, XsltArgumentList args)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            return Transform(input.CreateNavigator(), args, _DocumentResolver);
        }
        public void Transform(IXPathNavigable input, XsltArgumentList args, TextWriter output, XmlResolver resolver)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            Transform(input.CreateNavigator(), args, output, resolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList args, TextWriter output)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            Transform(input.CreateNavigator(), args, output, _DocumentResolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList args, Stream output, XmlResolver resolver)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            Transform(input.CreateNavigator(), args, output, resolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList args, Stream output)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            Transform(input.CreateNavigator(), args, output, _DocumentResolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList args, XmlWriter output, XmlResolver resolver)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            Transform(input.CreateNavigator(), args, output, resolver);
        }

        public void Transform(IXPathNavigable input, XsltArgumentList args, XmlWriter output)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            Transform(input.CreateNavigator(), args, output, _DocumentResolver);
        }

        public void Transform(String inputfile, String outputfile, XmlResolver resolver)
        {
            FileStream fs = null;
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

        public void Transform(String inputfile, String outputfile)
        {
            Transform(inputfile, outputfile, _DocumentResolver);
        }

        // Implementation

        private void Compile(XPathNavigator stylesheet, XmlResolver resolver)
        {
            Debug.Assert(stylesheet != null);

            Compiler compiler = (Debugger == null) ? new Compiler() : new DbgCompiler(this.Debugger);
            NavigatorInput input = new NavigatorInput(stylesheet);
            compiler.Compile(input, resolver ?? XmlNullResolver.Singleton);

            Debug.Assert(compiler.CompiledStylesheet != null);
            Debug.Assert(compiler.QueryStore != null);
            Debug.Assert(compiler.QueryStore != null);
            _CompiledStylesheet = compiler.CompiledStylesheet;
            _QueryStore = compiler.QueryStore;
            _RootAction = compiler.RootAction;
        }

        internal IXsltDebugger Debugger
        {
            get { return _debugger; }
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

        private class DebuggerAddapter : IXsltDebugger
        {
            private object _unknownDebugger;
            private MethodInfo _getBltIn;
            private MethodInfo _onCompile;
            private MethodInfo _onExecute;
            public DebuggerAddapter(object unknownDebugger)
            {
                _unknownDebugger = unknownDebugger;
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
                Type unknownType = unknownDebugger.GetType();
                _getBltIn = unknownType.GetMethod("GetBuiltInTemplatesUri", flags);
                _onCompile = unknownType.GetMethod("OnInstructionCompile", flags);
                _onExecute = unknownType.GetMethod("OnInstructionExecute", flags);
            }
            // ------------------ IXsltDebugger ---------------
            public string GetBuiltInTemplatesUri()
            {
                if (_getBltIn == null)
                {
                    return null;
                }
                return (string)_getBltIn.Invoke(_unknownDebugger, new object[] { });
            }
            public void OnInstructionCompile(XPathNavigator styleSheetNavigator)
            {
                if (_onCompile != null)
                {
                    _onCompile.Invoke(_unknownDebugger, new object[] { styleSheetNavigator });
                }
            }
            public void OnInstructionExecute(IXsltProcessor xsltProcessor)
            {
                if (_onExecute != null)
                {
                    _onExecute.Invoke(_unknownDebugger, new object[] { xsltProcessor });
                }
            }
        }
    }
}
