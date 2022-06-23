// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;
    using System.Text;
    using System.Collections;

    internal sealed class XsltOutput : CompiledAction
    {
        internal enum OutputMethod
        {
            Xml,
            Html,
            Text,
            Other,
            Unknown,
        }

        private OutputMethod _method = OutputMethod.Unknown;
        private int _methodSId = int.MaxValue;
        private Encoding _encoding = System.Text.Encoding.UTF8;
        private int _encodingSId = int.MaxValue;
        private string? _version;
        private int _versionSId = int.MaxValue;
        private bool _omitXmlDecl;
        private int _omitXmlDeclSId = int.MaxValue;
        private bool _standalone;
        private int _standaloneSId = int.MaxValue;
        private string? _doctypePublic;
        private int _doctypePublicSId = int.MaxValue;
        private string? _doctypeSystem;
        private int _doctypeSystemSId = int.MaxValue;
        private bool _indent;
        private int _indentSId = int.MaxValue;
        private string _mediaType = "text/html";
        private int _mediaTypeSId = int.MaxValue;
        private Hashtable? _cdataElements;

        internal OutputMethod Method
        {
            get { return _method; }
        }

        internal bool OmitXmlDeclaration
        {
            get { return _omitXmlDecl; }
        }

        internal bool HasStandalone
        {
            get { return _standaloneSId != int.MaxValue; }
        }

        internal bool Standalone
        {
            get { return _standalone; }
        }

        internal string? DoctypePublic
        {
            get { return _doctypePublic; }
        }

        internal string? DoctypeSystem
        {
            get { return _doctypeSystem; }
        }

        internal Hashtable? CDataElements
        {
            get { return _cdataElements; }
        }

        internal bool Indent
        {
            get { return _indent; }
        }

        internal Encoding Encoding
        {
            get { return _encoding; }
        }

        internal string MediaType
        {
            get { return _mediaType; }
        }

        internal XsltOutput CreateDerivedOutput(OutputMethod method)
        {
            XsltOutput output = (XsltOutput)MemberwiseClone();
            output._method = method;
            if (method == OutputMethod.Html && _indentSId == int.MaxValue)
            { // HTML output and Ident wasn't specified
                output._indent = true;
            }
            return output;
        }

        internal override void Compile(Compiler compiler)
        {
            CompileAttributes(compiler);
            CheckEmpty(compiler);
        }

        internal override bool CompileAttribute(Compiler compiler)
        {
            string name = compiler.Input.LocalName;
            string value = compiler.Input.Value;

            if (Ref.Equal(name, compiler.Atoms.Method))
            {
                if (compiler.Stylesheetid <= _methodSId)
                {
                    _method = ParseOutputMethod(value, compiler);
                    _methodSId = compiler.Stylesheetid;
                    if (_indentSId == int.MaxValue)
                    {
                        _indent = (_method == OutputMethod.Html);
                    }
                }
            }
            else if (Ref.Equal(name, compiler.Atoms.Version))
            {
                if (compiler.Stylesheetid <= _versionSId)
                {
                    _version = value;
                    _versionSId = compiler.Stylesheetid;
                }
            }
            else if (Ref.Equal(name, compiler.Atoms.Encoding))
            {
                if (compiler.Stylesheetid <= _encodingSId)
                {
                    try
                    {
                        _encoding = System.Text.Encoding.GetEncoding(value);
                        _encodingSId = compiler.Stylesheetid;
                    }
                    catch (System.NotSupportedException) { }
                    catch (System.ArgumentException) { }
                    Debug.Assert(_encoding != null);
                }
            }
            else if (Ref.Equal(name, compiler.Atoms.OmitXmlDeclaration))
            {
                if (compiler.Stylesheetid <= _omitXmlDeclSId)
                {
                    _omitXmlDecl = compiler.GetYesNo(value);
                    _omitXmlDeclSId = compiler.Stylesheetid;
                }
            }
            else if (Ref.Equal(name, compiler.Atoms.Standalone))
            {
                if (compiler.Stylesheetid <= _standaloneSId)
                {
                    _standalone = compiler.GetYesNo(value);
                    _standaloneSId = compiler.Stylesheetid;
                }
            }
            else if (Ref.Equal(name, compiler.Atoms.DocTypePublic))
            {
                if (compiler.Stylesheetid <= _doctypePublicSId)
                {
                    _doctypePublic = value;
                    _doctypePublicSId = compiler.Stylesheetid;
                }
            }
            else if (Ref.Equal(name, compiler.Atoms.DocTypeSystem))
            {
                if (compiler.Stylesheetid <= _doctypeSystemSId)
                {
                    _doctypeSystem = value;
                    _doctypeSystemSId = compiler.Stylesheetid;
                }
            }
            else if (Ref.Equal(name, compiler.Atoms.Indent))
            {
                if (compiler.Stylesheetid <= _indentSId)
                {
                    _indent = compiler.GetYesNo(value);
                    _indentSId = compiler.Stylesheetid;
                }
            }
            else if (Ref.Equal(name, compiler.Atoms.MediaType))
            {
                if (compiler.Stylesheetid <= _mediaTypeSId)
                {
                    _mediaType = value;
                    _mediaTypeSId = compiler.Stylesheetid;
                }
            }
            else if (Ref.Equal(name, compiler.Atoms.CDataSectionElements))
            {
                string[] qnames = XmlConvert.SplitString(value);

                if (_cdataElements == null)
                {
                    _cdataElements = new Hashtable(qnames.Length);
                }

                for (int i = 0; i < qnames.Length; i++)
                {
                    XmlQualifiedName qname = compiler.CreateXmlQName(qnames[i]);
                    _cdataElements[qname] = qname;
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Fail("Override invoked");
        }

        private static OutputMethod ParseOutputMethod(string value, Compiler compiler)
        {
            XmlQualifiedName method = compiler.CreateXPathQName(value);
            if (method.Namespace.Length != 0)
            {
                return OutputMethod.Other;
            }
            switch (method.Name)
            {
                case "xml":
                    return OutputMethod.Xml;
                case "html":
                    return OutputMethod.Html;
                case "text":
                    return OutputMethod.Text;
                default:
                    if (compiler.ForwardCompatibility)
                    {
                        return OutputMethod.Unknown;
                    }
                    throw XsltException.Create(SR.Xslt_InvalidAttrValue, "method", value);
            }
        }
    }
}
