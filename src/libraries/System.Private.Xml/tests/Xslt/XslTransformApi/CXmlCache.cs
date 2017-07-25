// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit.Abstractions;
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;

public enum NodeFlags
{
    None = 0,
    EmptyElement = 1,
    HasValue = 2,
    SingleQuote = 4,
    DefaultAttribute = 8,
    UnparsedEntities = 16,
    IsWhitespace = 32,
    DocumentRoot = 64,
    AttributeTextNode = 128,
    MixedContent = 256,
    Indent = 512
}

abstract public class CXmlBase
{
    protected XmlNodeType _nType;
    protected string _strName;
    protected string _strLocalName;
    protected string _strPrefix;
    protected string _strNamespace;
    internal int _nDepth;
    internal NodeFlags _eFlags = NodeFlags.None;
    internal CXmlBase _rNextNode = null;
    internal CXmlBase _rParentNode = null;
    internal CXmlBase _rFirstChildNode = null;
    internal CXmlBase _rLastChildNode = null;
    internal int _nChildNodes = 0;

    //
    // Constructors
    //
    public CXmlBase(string strPrefix, string strName, string strLocalName, XmlNodeType NodeType, string strNamespace)
    {
        _strPrefix = strPrefix;
        _strName = strName;
        _strLocalName = strLocalName;
        _nType = NodeType;
        _strNamespace = strNamespace;
    }

    public CXmlBase(string strPrefix, string strName, XmlNodeType NodeType, string strNamespace)
        : this(strPrefix, strName, strName, NodeType, strNamespace)
    { }

    public CXmlBase(string strPrefix, string strName, XmlNodeType NodeType)
        : this(strPrefix, strName, strName, NodeType, "")
    { }

    public CXmlBase(string strName, XmlNodeType NodeType)
        : this("", strName, strName, NodeType, "")
    { }

    //
    // Virtual Methods and Properties
    //
    abstract public void Write(XmlWriter rXmlWriter);

    abstract public void WriteXml(TextWriter rTW);

    abstract public string Value
    { get; }

    //
    // Public Methods and Properties
    //
    public string Name
    {
        get { return _strName; }
    }

    public string LocalName
    {
        get { return _strLocalName; }
    }

    public string Prefix
    {
        get { return _strPrefix; }
    }

    public string Namespace
    {
        get { return _strNamespace; }
    }

    public int Depth
    {
        get { return _nDepth; }
    }

    public XmlNodeType NodeType
    {
        get { return _nType; }
    }

    public NodeFlags Flags
    {
        get { return _eFlags; }
    }

    public int ChildNodeCount
    {
        get { return _nChildNodes; }
    }

    public void InsertNode(CXmlBase rNode)
    {
        if (_rFirstChildNode == null)
        {
            _rFirstChildNode = _rLastChildNode = rNode;
        }
        else
        {
            _rLastChildNode._rNextNode = rNode;
            _rLastChildNode = rNode;
        }

        if ((this._eFlags & NodeFlags.IsWhitespace) == 0)
            _nChildNodes++;

        rNode._rParentNode = this;
    }

    //
    // Internal Methods and Properties
    //
    internal CXmlBase _Child(int n)
    {
        int i;
        int j;
        CXmlBase rChild = _rFirstChildNode;

        for (i = 0, j = 0; rChild != null; i++, rChild = rChild._rNextNode)
        {
            if ((rChild._eFlags & NodeFlags.IsWhitespace) == 0)
            {
                if (j++ == n) break;
            }
        }

        return rChild;
    }

    internal CXmlBase _Child(string str)
    {
        CXmlBase rChild;

        for (rChild = _rFirstChildNode; rChild != null; rChild = rChild._rNextNode)
            if (rChild.Name == str) break;

        return rChild;
    }
}

public class CXmlAttribute : CXmlBase
{
    //
    // Constructor
    //
    public CXmlAttribute(XmlReader rXmlReader)
        : base(rXmlReader.Prefix, rXmlReader.Name, rXmlReader.LocalName, rXmlReader.NodeType, rXmlReader.NamespaceURI)
    {
        if (rXmlReader.IsDefault)
            _eFlags |= NodeFlags.DefaultAttribute;

        if (rXmlReader.QuoteChar == '\'')
            _eFlags |= NodeFlags.SingleQuote;
    }

    //
    // Public Methods and Properties (Override)
    //
    override public void Write(XmlWriter rXmlWriter)
    {
        CXmlBase rNode;

        if ((this._eFlags & NodeFlags.DefaultAttribute) == 0)
        {
            if (rXmlWriter is XmlTextWriter)
                ((XmlTextWriter)rXmlWriter).QuoteChar = this.Quote;

            rXmlWriter.WriteStartAttribute(this.Prefix, this.LocalName, this.Namespace);

            for (rNode = this._rFirstChildNode; rNode != null; rNode = rNode._rNextNode)
            {
                rNode.Write(rXmlWriter);
            }

            rXmlWriter.WriteEndAttribute();
        }
    }

    override public void WriteXml(TextWriter rTW)
    {
        if ((this._eFlags & NodeFlags.DefaultAttribute) == 0)
        {
            CXmlBase rNode;

            rTW.Write(' ' + this.Name + '=' + this.Quote);
            for (rNode = this._rFirstChildNode; rNode != null; rNode = rNode._rNextNode)
            {
                rNode.WriteXml(rTW);
            }
            rTW.Write(this.Quote);
        }
    }

    //
    // Public Methods and Properties
    //
    override public string Value
    {
        get
        {
            CXmlNode rNode;
            string strValue = string.Empty;

            for (rNode = (CXmlNode)this._rFirstChildNode; rNode != null; rNode = rNode.NextNode)
                strValue += rNode.Value;

            return strValue;
        }
    }

    public CXmlAttribute NextAttribute
    {
        get { return (CXmlAttribute)this._rNextNode; }
    }

    public char Quote
    {
        get { return ((base._eFlags & NodeFlags.SingleQuote) != 0 ? '\'' : '"'); }
        set { if (value == '\'') base._eFlags |= NodeFlags.SingleQuote; else base._eFlags &= ~NodeFlags.SingleQuote; }
    }

    public CXmlNode FirstChild
    {
        get { return (CXmlNode)base._rFirstChildNode; }
    }

    public CXmlNode Child(int n)
    {
        return (CXmlNode)base._Child(n);
    }

    public CXmlNode Child(string str)
    {
        return (CXmlNode)base._Child(str);
    }
}

public class CXmlNode : CXmlBase
{
    internal string _strValue = null;
    private CXmlAttribute _rFirstAttribute = null;
    private CXmlAttribute _rLastAttribute = null;
    private int _nAttributeCount = 0;

    //
    // Constructors
    //
    public CXmlNode(string strPrefix, string strName, XmlNodeType NodeType)
        : base(strPrefix, strName, NodeType)
    { }

    public CXmlNode(XmlReader rXmlReader)
        : base(rXmlReader.Prefix, rXmlReader.Name, rXmlReader.LocalName, rXmlReader.NodeType, rXmlReader.NamespaceURI)
    {
        _eFlags |= CXmlCache._eDefaultFlags;

        if (NodeType == XmlNodeType.Whitespace ||
            NodeType == XmlNodeType.SignificantWhitespace)
        {
            _eFlags |= NodeFlags.IsWhitespace;
        }

        if (rXmlReader.IsEmptyElement)
        {
            _eFlags |= NodeFlags.EmptyElement;
        }

        if (rXmlReader.HasValue)
        {
            _eFlags |= NodeFlags.HasValue;
            _strValue = rXmlReader.Value;
        }
    }

    //
    // Public Methods and Properties (Override)
    //
    override public void Write(XmlWriter rXmlWriter)
    {
        CXmlBase rNode;
        CXmlAttribute rAttribute;
        string DocTypePublic = null;
        string DocTypeSystem = null;

        switch (this.NodeType)
        {
            case XmlNodeType.CDATA:
                rXmlWriter.WriteCData(this._strValue);
                break;

            case XmlNodeType.Comment:
                rXmlWriter.WriteComment(this._strValue);
                break;

            case XmlNodeType.DocumentType:
                for (rAttribute = _rFirstAttribute; rAttribute != null; rAttribute = rAttribute.NextAttribute)
                {
                    if (rAttribute.Name == "PUBLIC")
                    { DocTypePublic = rAttribute.Value; }
                    if (rAttribute.Name == "SYSTEM")
                    { DocTypeSystem = rAttribute.Value; }
                }
                rXmlWriter.WriteDocType(this.Name, DocTypePublic, DocTypeSystem, this._strValue);
                break;

            case XmlNodeType.EntityReference:
                rXmlWriter.WriteEntityRef(this.Name);
                break;

            case XmlNodeType.ProcessingInstruction:
                rXmlWriter.WriteProcessingInstruction(this.Name, this._strValue);
                break;

            case XmlNodeType.Text:
                if (this.Name == string.Empty)
                {
                    if ((this.Flags & NodeFlags.UnparsedEntities) == 0)
                    {
                        rXmlWriter.WriteString(this._strValue);
                    }
                    else
                    {
                        rXmlWriter.WriteRaw(this._strValue.ToCharArray(), 0, this._strValue.Length);
                    }
                }
                else
                {
                    if (this._strName[0] == '#')
                        rXmlWriter.WriteCharEntity(this._strValue[0]);
                    else
                        rXmlWriter.WriteEntityRef(this.Name);
                }
                break;

            case XmlNodeType.Whitespace:
            case XmlNodeType.SignificantWhitespace:
                if ((this._rParentNode._eFlags & NodeFlags.DocumentRoot) != 0)
                    rXmlWriter.WriteRaw(this._strValue.ToCharArray(), 0, this._strValue.Length);
                else
                    rXmlWriter.WriteString(this._strValue);
                break;

            case XmlNodeType.Element:
                rXmlWriter.WriteStartElement(this.Prefix, this.LocalName, null);

                for (rAttribute = _rFirstAttribute; rAttribute != null; rAttribute = rAttribute.NextAttribute)
                {
                    rAttribute.Write(rXmlWriter);
                }

                if ((this.Flags & NodeFlags.EmptyElement) == 0)
                    rXmlWriter.WriteString(string.Empty);

                for (rNode = base._rFirstChildNode; rNode != null; rNode = rNode._rNextNode)
                {
                    rNode.Write(rXmlWriter);
                }

                // Should only produce empty tag if the original document used empty tag
                if ((this.Flags & NodeFlags.EmptyElement) == 0)
                    rXmlWriter.WriteFullEndElement();
                else
                    rXmlWriter.WriteEndElement();

                break;

            case XmlNodeType.XmlDeclaration:
                rXmlWriter.WriteRaw("<?xml " + this._strValue + "?>");
                break;

            default:
                throw (new Exception("Node.Write: Unhandled node type " + this.NodeType.ToString()));
        }
    }

    override public void WriteXml(TextWriter rTW)
    {
        String strXml;
        CXmlAttribute rAttribute;
        CXmlBase rNode;

        switch (this._nType)
        {
            case XmlNodeType.Text:
                if (this._strName == "")
                {
                    rTW.Write(this._strValue);
                }
                else
                {
                    if (this._strName.StartsWith("#"))
                    {
                        rTW.Write("&" + Convert.ToString(Convert.ToInt32(this._strValue[0])) + ";");
                    }
                    else
                    {
                        rTW.Write("&" + this.Name + ";");
                    }
                }
                break;

            case XmlNodeType.Whitespace:
            case XmlNodeType.SignificantWhitespace:
            case XmlNodeType.DocumentType:
                rTW.Write(this._strValue);
                break;

            case XmlNodeType.Element:
                strXml = this.Name;
                rTW.Write('<' + strXml);

                //Put in all the Attributes
                for (rAttribute = _rFirstAttribute; rAttribute != null; rAttribute = rAttribute.NextAttribute)
                {
                    rAttribute.WriteXml(rTW);
                }

                //If there is children, put those in, otherwise close the tag.
                if ((base._eFlags & NodeFlags.EmptyElement) == 0)
                {
                    rTW.Write('>');

                    for (rNode = base._rFirstChildNode; rNode != null; rNode = rNode._rNextNode)
                    {
                        rNode.WriteXml(rTW);
                    }

                    rTW.Write("</" + strXml + ">");
                }
                else
                {
                    rTW.Write(" />");
                }

                break;

            case XmlNodeType.EntityReference:
                rTW.Write("&" + this._strName + ";");
                break;

            case XmlNodeType.Notation:
                rTW.Write("<!NOTATION " + this._strValue + ">");
                break;

            case XmlNodeType.CDATA:
                rTW.Write("<![CDATA[" + this._strValue + "]]>");
                break;

            case XmlNodeType.XmlDeclaration:
            case XmlNodeType.ProcessingInstruction:
                rTW.Write("<?" + this._strName + " " + this._strValue + "?>");
                break;

            case XmlNodeType.Comment:
                rTW.Write("<!--" + this._strValue + "-->");
                break;

            default:
                throw (new Exception("Unhandled NodeType " + this._nType.ToString()));
        }
    }

    //
    // Public Methods and Properties
    //
    public string NodeValue
    {
        get { return _strValue; }
    }

    override public string Value
    {
        get
        {
            string strValue = "";
            CXmlNode rChild;

            if ((this._eFlags & NodeFlags.HasValue) != 0)
            {
                char chEnt;
                int nIndexAmp = 0;
                int nIndexSem = 0;

                if ((this._eFlags & NodeFlags.UnparsedEntities) == 0)
                    return this._strValue;

                strValue = this._strValue;

                while ((nIndexAmp = strValue.IndexOf('&', nIndexAmp)) != -1)
                {
                    nIndexSem = strValue.IndexOf(';', nIndexAmp);
                    chEnt = ResolveCharEntity(strValue.Substring(nIndexAmp + 1, nIndexSem - nIndexAmp - 1));
                    if (chEnt != char.MinValue)
                    {
                        strValue = strValue.Substring(0, nIndexAmp) + chEnt + strValue.Substring(nIndexSem + 1);
                        nIndexAmp++;
                    }
                    else
                        nIndexAmp = nIndexSem;
                }
                return strValue;
            }

            for (rChild = (CXmlNode)this._rFirstChildNode; rChild != null; rChild = (CXmlNode)rChild._rNextNode)
            {
                strValue = strValue + rChild.Value;
            }

            return strValue;
        }
    }

    public CXmlNode NextNode
    {
        get
        {
            CXmlBase rNode = this._rNextNode;

            while (rNode != null &&
                   (rNode.Flags & NodeFlags.IsWhitespace) != 0)
                rNode = rNode._rNextNode;
            return (CXmlNode)rNode;
        }
    }

    public CXmlNode FirstChild
    {
        get
        {
            CXmlBase rNode = this._rFirstChildNode;

            while (rNode != null &&
                   (rNode.Flags & NodeFlags.IsWhitespace) != 0)
                rNode = rNode._rNextNode;
            return (CXmlNode)rNode;
        }
    }

    public CXmlNode Child(int n)
    {
        int i;
        CXmlNode rChild;

        i = 0;
        for (rChild = FirstChild; rChild != null; rChild = rChild.NextNode)
            if (i++ == n) break;

        return rChild;
    }

    public CXmlNode Child(string str)
    {
        return (CXmlNode)base._Child(str);
    }

    public int Type
    {
        get { return Convert.ToInt32(base._nType); }
    }

    public CXmlAttribute FirstAttribute
    {
        get { return _rFirstAttribute; }
    }

    public int AttributeCount
    {
        get { return _nAttributeCount; }
    }

    public CXmlAttribute Attribute(int n)
    {
        int i;
        CXmlAttribute rAttribute;

        i = 0;
        for (rAttribute = _rFirstAttribute; rAttribute != null; rAttribute = rAttribute.NextAttribute)
            if (i++ == n) break;
        return rAttribute;
    }

    public CXmlAttribute Attribute(string str)
    {
        CXmlAttribute rAttribute;

        for (rAttribute = _rFirstAttribute; rAttribute != null; rAttribute = rAttribute.NextAttribute)
        {
            if (rAttribute.Name == str) break;
        }

        return rAttribute;
    }

    public void AddAttribute(CXmlAttribute rAttribute)
    {
        if (_rFirstAttribute == null)
        {
            _rFirstAttribute = rAttribute;
        }
        else
        {
            _rLastAttribute._rNextNode = rAttribute;
        }
        _rLastAttribute = rAttribute;
        _nAttributeCount++;
    }

    private char ResolveCharEntity(string strName)
    {
        if (strName[0] == '#')
            if (strName[1] == 'x')
                return Convert.ToChar(Convert.ToInt32(strName.Substring(2), 16));
            else
                return Convert.ToChar(Convert.ToInt32(strName.Substring(1)));
        if (strName == "lt")
            return '<';
        if (strName == "gt")
            return '>';
        if (strName == "amp")
            return '&';
        if (strName == "apos")
            return '\'';
        if (strName == "quot")
            return '"';

        return char.MinValue;
    }
}

public class CXmlCache
{
    //CXmlCache Properties
    private bool _fTrace = false;

    private bool _fThrow = true;
    private bool _fReadNode = true;
    private int _hr = 0;
    private Encoding _eEncoding = System.Text.Encoding.UTF8;
    private string _strParseError = "";

    //XmlReader Properties
#pragma warning disable 0618
    private ValidationType _eValidationMode = ValidationType.Auto;
#pragma warning restore 0618

    private WhitespaceHandling _eWhitespaceMode = WhitespaceHandling.None;
    private EntityHandling _eEntityMode = EntityHandling.ExpandEntities;
    private bool _fNamespaces = true;

    private bool _fValidationCallback = false;
    private bool _fExpandAttributeValues = false;

    //Internal stuff
    protected XmlReader _rXmlReader = null;

    protected CXmlNode _rDocumentRootNode;
    protected CXmlNode _rRootNode = null;
    internal static NodeFlags _eDefaultFlags = NodeFlags.None;

    private ITestOutputHelper _output;
    public CXmlCache(ITestOutputHelper output)
    {
        _output = output;
    }

    //
    // Constructor
    //
    public CXmlCache()
    {
    }

    //
    // Public Methods and Properties
    //
    public virtual bool Load(XmlReader rXmlReader)
    {
        //Hook up your reader as my reader
        _rXmlReader = rXmlReader;

        if (rXmlReader is XmlTextReader)
        {
            _eWhitespaceMode = ((XmlTextReader)rXmlReader).WhitespaceHandling;
            _fNamespaces = ((XmlTextReader)rXmlReader).Namespaces;
            _eValidationMode = ValidationType.None;
        }
#pragma warning disable 0618
        if (rXmlReader is XmlValidatingReader)
        {
            if (((XmlValidatingReader)rXmlReader).Reader is XmlTextReader)
            {
                _eWhitespaceMode = ((XmlTextReader)((XmlValidatingReader)rXmlReader).Reader).WhitespaceHandling;
            }
            else
            {
                _eWhitespaceMode = WhitespaceHandling.None;
            }
            _fNamespaces = ((XmlValidatingReader)rXmlReader).Namespaces;
            _eValidationMode = ((XmlValidatingReader)rXmlReader).ValidationType;
            _eEntityMode = ((XmlValidatingReader)rXmlReader).EntityHandling;
        }
#pragma warning restore 0618

        DebugTrace("Setting ValidationMode=" + _eValidationMode.ToString());
        DebugTrace("Setting EntityMode=" + _eEntityMode.ToString());
        DebugTrace("Setting WhitespaceMode=" + _eWhitespaceMode.ToString());

        //Process the Document
        try
        {
            _rDocumentRootNode = new CXmlNode("", "", XmlNodeType.Element);
            _rDocumentRootNode._eFlags = NodeFlags.DocumentRoot | NodeFlags.Indent;
            Process(_rDocumentRootNode);
            for (_rRootNode = _rDocumentRootNode.FirstChild; _rRootNode != null && _rRootNode.NodeType != XmlNodeType.Element; _rRootNode = _rRootNode.NextNode) ;
        }
        catch (Exception e)
        {
            //Unhook your reader
            _rXmlReader = null;

            _strParseError = e.ToString();

            if (_fThrow)
            {
                throw (e);
            }

            if (_hr == 0)
                _hr = -1;

            return false;
        }

        //Unhook your reader
        _rXmlReader = null;

        return true;
    }

    public bool Load(string strFileName)
    {
#pragma warning disable 0618
        XmlTextReader rXmlTextReader;
        XmlValidatingReader rXmlValidatingReader;
        bool fRet;

        rXmlTextReader = new XmlTextReader(strFileName);
        rXmlTextReader.WhitespaceHandling = _eWhitespaceMode;
        rXmlTextReader.Namespaces = _fNamespaces;

        _eEncoding = rXmlTextReader.Encoding;

        rXmlValidatingReader = new XmlValidatingReader(rXmlTextReader);
        rXmlValidatingReader.ValidationType = _eValidationMode;
        rXmlValidatingReader.EntityHandling = _eEntityMode;
#pragma warning restore 0618

        if (_fValidationCallback)
            rXmlValidatingReader.ValidationEventHandler += new ValidationEventHandler(this.ValidationCallback);

        try
        {
            fRet = Load((XmlReader)rXmlValidatingReader);
        }
        catch (Exception e)
        {
            fRet = false;
            rXmlValidatingReader.Dispose();
            rXmlTextReader.Dispose();

            if (_strParseError == string.Empty)
                _strParseError = e.ToString();

            if (_fThrow)
                throw (e);
        }

        rXmlValidatingReader.Dispose();
        rXmlTextReader.Dispose();
        return fRet;
    }

    public void Save(string strName)
    {
        Save(strName, false, _eEncoding);
    }

    public void Save(string strName, bool fOverWrite)
    {
        Save(strName, fOverWrite, _eEncoding);
    }

    public void Save(string strName, bool fOverWrite, System.Text.Encoding Encoding)
    {
        CXmlBase rNode;
        XmlTextWriter rXmlTextWriter = null;

        try
        {
            if (fOverWrite)
            {
                File.Delete(strName);
            }

            rXmlTextWriter = new XmlTextWriter(strName, Encoding);
            rXmlTextWriter.Namespaces = _fNamespaces;

            for (rNode = _rDocumentRootNode._rFirstChildNode; rNode != null; rNode = rNode._rNextNode)
            {
                rNode.Write(rXmlTextWriter);
            }
            rXmlTextWriter.Dispose();
        }
        catch (Exception e)
        {
            DebugTrace(e.ToString());
            if (rXmlTextWriter != null)
                rXmlTextWriter.Dispose();
            throw (e);
        }
    }

    public void WriteXml(TextWriter rTW)
    {
        CXmlBase rNode;

        //Spit out the document
        for (rNode = _rDocumentRootNode._rFirstChildNode; rNode != null; rNode = rNode._rNextNode)
            rNode.WriteXml(rTW);
    }

    public CXmlNode RootNode
    {
        get { return _rRootNode; }
    }

    public string ParseError
    {
        get { return _strParseError; }
    }

    public int ParseErrorCode
    {
        get { return _hr; }
    }

    //
    // XmlReader Properties
    //
    public string EntityMode
    {
        set
        {
            if (value == "ExpandEntities")
                _eEntityMode = EntityHandling.ExpandEntities;
            else if (value == "ExpandCharEntities")
                _eEntityMode = EntityHandling.ExpandCharEntities;
            else
                throw (new Exception("Invalid Entity mode."));
        }
        get { return _eEntityMode.ToString(); }
    }

    public string ValidationMode
    {
        set
        {
#pragma warning disable 0618
            if (value == "None")
                _eValidationMode = ValidationType.None;
            else if (value == "DTD")
                _eValidationMode = ValidationType.DTD;
            else if (value == "XDR")
                _eValidationMode = ValidationType.XDR;
            else if (value == "Schema")
                _eValidationMode = ValidationType.Schema;
            else if (value == "Auto")
                _eValidationMode = ValidationType.Auto;
            else
                throw (new Exception("Invalid Validation mode."));
#pragma warning restore 0618
        }
        get { return _eValidationMode.ToString(); }
    }

    public string WhitespaceMode
    {
        set
        {
            if (value == "All")
                _eWhitespaceMode = WhitespaceHandling.All;
            else if (value == "Significant")
                _eWhitespaceMode = WhitespaceHandling.Significant;
            else if (value == "None")
                _eWhitespaceMode = WhitespaceHandling.None;
            else
                throw (new Exception("Invalid Whitespace mode."));
        }
        get { return _eWhitespaceMode.ToString(); }
    }

    public bool Namespaces
    {
        set { _fNamespaces = value; }
        get { return _fNamespaces; }
    }

    public bool UseValidationCallback
    {
        set { _fValidationCallback = value; }
        get { return _fValidationCallback; }
    }

    public bool ExpandAttributeValues
    {
        set { _fExpandAttributeValues = value; }
        get { return _fExpandAttributeValues; }
    }

    //
    // Internal Properties
    //
    public bool Throw
    {
        get { return _fThrow; }
        set { _fThrow = value; }
    }

    public bool Trace
    {
        set { _fTrace = value; }
        get { return _fTrace; }
    }

    //
    //Private Methods
    //
    private void DebugTrace(string str)
    {
        DebugTrace(str, 0);
    }

    private void DebugTrace(string str, int nDepth)
    {
        if (_fTrace)
        {
            int i;

            for (i = 0; i < nDepth; i++)
                _output.WriteLine(" ");
            _output.WriteLine(str);
        }
    }

    private void DebugTrace(XmlReader rXmlReader)
    {
        if (_fTrace)
        {
            string str;

            str = rXmlReader.NodeType.ToString() + ", Depth=" + rXmlReader.Depth + " Name=";
            if (rXmlReader.Prefix != "")
            {
                str += rXmlReader.Prefix + ":";
            }
            str += rXmlReader.LocalName;

            if (rXmlReader.HasValue)
                str += " Value=" + rXmlReader.Value;

            if (rXmlReader.NodeType == XmlNodeType.Attribute)
                str += " QuoteChar=" + rXmlReader.QuoteChar;

            DebugTrace(str, rXmlReader.Depth);
        }
    }

    protected void Process(CXmlBase rParentNode)
    {
        CXmlNode rNewNode;

        while (true)
        {
            //We want to pop if Read() returns false, aka EOF
            if (_fReadNode)
            {
                if (!_rXmlReader.Read())
                {
                    DebugTrace("Read() == false");
                    return;
                }
            }
            else
            {
                if (!_rXmlReader.ReadAttributeValue())
                {
                    DebugTrace("ReadAttributeValue() == false");
                    return;
                }
            }

            DebugTrace(_rXmlReader);

            //We also want to pop if we get an EndElement or EndEntity
            if (_rXmlReader.NodeType == XmlNodeType.EndElement ||
                 _rXmlReader.NodeType == XmlNodeType.EndEntity)
            {
                DebugTrace("NodeType == EndElement or EndEntity");
                return;
            }

            rNewNode = GetNewNode(_rXmlReader);
            rNewNode._nDepth = _rXmlReader.Depth;

            // Test for MixedContent and set Indent if necessary
            if ((rParentNode.Flags & NodeFlags.MixedContent) != 0)
            {
                rNewNode._eFlags |= NodeFlags.MixedContent;
                // Indent is off for all new nodes
            }
            else
            {
                rNewNode._eFlags |= NodeFlags.Indent;		// Turn on Indent for current Node
            }

            // Set all Depth 0 nodes to No Mixed Content and Indent True
            if (_rXmlReader.Depth == 0)
            {
                rNewNode._eFlags |= NodeFlags.Indent;			// Turn on Indent
                rNewNode._eFlags &= ~NodeFlags.MixedContent;	// Turn off MixedContent
            }

            rParentNode.InsertNode(rNewNode);

            //Do some special stuff based on NodeType
            switch (_rXmlReader.NodeType)
            {
                case XmlNodeType.EntityReference:
                    if (_eValidationMode == ValidationType.DTD)
                    {
                        _rXmlReader.ResolveEntity();
                        Process(rNewNode);
                    }
                    break;

                case XmlNodeType.Element:
                    if (_rXmlReader.MoveToFirstAttribute())
                    {
                        do
                        {
                            CXmlAttribute rNewAttribute = new CXmlAttribute(_rXmlReader);
                            rNewNode.AddAttribute(rNewAttribute);

                            if (_fExpandAttributeValues)
                            {
                                DebugTrace("Attribute: " + _rXmlReader.Name);
                                _fReadNode = false;
                                Process(rNewAttribute);
                                _fReadNode = true;
                            }
                            else
                            {
                                CXmlNode rValueNode = new CXmlNode("", "", XmlNodeType.Text);
                                rValueNode._eFlags = _eDefaultFlags | NodeFlags.HasValue;

                                rValueNode._strValue = _rXmlReader.Value;

                                DebugTrace("  Value=" + rValueNode.Value, _rXmlReader.Depth + 1);

                                rNewAttribute.InsertNode(rValueNode);
                            }
                        } while (_rXmlReader.MoveToNextAttribute());
                    }

                    if ((rNewNode.Flags & NodeFlags.EmptyElement) == 0)
                        Process(rNewNode);

                    break;

                case XmlNodeType.XmlDeclaration:
                    if (_rXmlReader is XmlTextReader)
                    {
                        _eEncoding = ((XmlTextReader)_rXmlReader).Encoding;
                    }
#pragma warning disable 0618
                    else if (_rXmlReader is XmlValidatingReader)
                    {
                        _eEncoding = ((XmlValidatingReader)_rXmlReader).Encoding;
                    }
#pragma warning restore 0618
                    else
                    {
                        string strValue = rNewNode.NodeValue;
                        int nPos = strValue.IndexOf("encoding");
                        if (nPos != -1)
                        {
                            int nEnd;

                            nPos = strValue.IndexOf("=", nPos);			//Find the = sign
                            nEnd = strValue.IndexOf("\"", nPos) + 1;	//Find the next " character
                            nPos = strValue.IndexOf("'", nPos) + 1;		//Find the next ' character
                            if (nEnd == 0 || (nPos < nEnd && nPos > 0))	//Pick the one that's closer to the = sign
                            {
                                nEnd = strValue.IndexOf("'", nPos);
                            }
                            else
                            {
                                nPos = nEnd;
                                nEnd = strValue.IndexOf("\"", nPos);
                            }
                            string sEncodeName = strValue.Substring(nPos, nEnd - nPos);
                            DebugTrace("XMLDecl contains encoding " + sEncodeName);
                            if (sEncodeName.ToUpper() == "UCS-2")
                            {
                                sEncodeName = "unicode";
                            }
                            _eEncoding = System.Text.Encoding.GetEncoding(sEncodeName);
                        }
                    }
                    break;

                case XmlNodeType.ProcessingInstruction:
                    break;

                case XmlNodeType.Text:
                    if (!_fReadNode)
                    {
                        rNewNode._eFlags = _eDefaultFlags | NodeFlags.AttributeTextNode;
                    }
                    rNewNode._eFlags |= NodeFlags.MixedContent;		// turn on Mixed Content for current node
                    rNewNode._eFlags &= ~NodeFlags.Indent;			// turn off Indent for current node
                    rParentNode._eFlags |= NodeFlags.MixedContent;	// turn on Mixed Content for Parent Node
                    break;

                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.CDATA:
                    rNewNode._eFlags |= NodeFlags.MixedContent;		// turn on Mixed Content for current node
                    rNewNode._eFlags &= ~NodeFlags.Indent;			// turn off Indent for current node
                    rParentNode._eFlags |= NodeFlags.MixedContent;	// turn on Mixed Content for Parent Node
                    break;

                case XmlNodeType.Comment:
                case XmlNodeType.Notation:
                    break;

                case XmlNodeType.DocumentType:
                    if (_rXmlReader.MoveToFirstAttribute())
                    {
                        do
                        {
                            CXmlAttribute rNewAttribute = new CXmlAttribute(_rXmlReader);
                            rNewNode.AddAttribute(rNewAttribute);

                            CXmlNode rValueNode = new CXmlNode(_rXmlReader);
                            rValueNode._strValue = _rXmlReader.Value;
                            rNewAttribute.InsertNode(rValueNode);
                        } while (_rXmlReader.MoveToNextAttribute());
                    }

                    break;

                default:
                    _output.WriteLine("UNHANDLED TYPE, " + _rXmlReader.NodeType.ToString() + " IN Process()!");
                    break;
            }
        }
    }

    protected virtual CXmlNode GetNewNode(XmlReader rXmlReader)
    {
        return new CXmlNode(rXmlReader);
    }

    private void ValidationCallback(object sender, ValidationEventArgs args)
    {
        //  commented by ROCHOA -- don't know where ValidationEventArgs comes from
        //	_hr = Convert.ToInt16(args.ErrorCode);
        throw (new Exception("[" + Convert.ToString(_hr) + "] " + args.Message));
    }
}

public class ChecksumWriter : TextWriter
{
    private int _nPosition = 0;
    private Decimal _dResult = 0;
    private Encoding _encoding;

    // --------------------------------------------------------------------------------------------------
    //    Constructor
    // --------------------------------------------------------------------------------------------------
    public ChecksumWriter()
    {
        _encoding = Encoding.UTF8;
    }

    // --------------------------------------------------------------------------------------------------
    //    Properties
    // --------------------------------------------------------------------------------------------------
    public Decimal CheckSum
    {
        get { return _dResult; }
    }

    public override Encoding Encoding
    {
        get { return _encoding; }
    }

    // --------------------------------------------------------------------------------------------------
    //    Public methods
    // --------------------------------------------------------------------------------------------------
    override public void Write(String str)
    {
        int i;
        int m;

        m = str.Length;
        for (i = 0; i < m; i++)
        {
            Write(str[i]);
        }
    }

    override public void Write(Char[] rgch)
    {
        int i;
        int m;

        m = rgch.Length;
        for (i = 0; i < m; i++)
        {
            Write(rgch[i]);
        }
    }

    override public void Write(Char[] rgch, Int32 iOffset, Int32 iCount)
    {
        int i;
        int m;

        m = iOffset + iCount;
        for (i = iOffset; i < m; i++)
        {
            Write(rgch[i]);
        }
    }

    override public void Write(Char ch)
    {
        _dResult += Math.Round((Decimal)(ch / (_nPosition + 1.0)), 10);
        _nPosition++;
    }

    override public void Close()
    {
        _nPosition = 0;
        _dResult = 0;
    }
}
