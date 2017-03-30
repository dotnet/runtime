// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.IO;
using System.Xml;
using Microsoft.Test.ModuleCore;
using XmlCoreTest.Common;

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
    protected XmlNodeType pnType;
    protected string pstrName;
    protected string pstrLocalName;
    protected string pstrPrefix;
    protected string pstrNamespace;
    internal int pnDepth;
    internal NodeFlags peFlags = NodeFlags.None;
    internal CXmlBase prNextNode = null;
    internal CXmlBase prParentNode = null;
    internal CXmlBase prFirstChildNode = null;
    internal CXmlBase prLastChildNode = null;
    internal int pnChildNodes = 0;

    //
    // Constructors
    //
    public CXmlBase(string strPrefix, string strName, string strLocalName, XmlNodeType NodeType, string strNamespace)
    {
        pstrPrefix = strPrefix;
        pstrName = strName;
        pstrLocalName = strLocalName;
        pnType = NodeType;
        pstrNamespace = strNamespace;
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

    abstract public string Xml { get; }

    abstract public void WriteXml(TextWriter rTW);

    abstract public string Value { get; }

    //
    // Public Methods and Properties
    //
    public string Name
    {
        get { return pstrName; }
    }

    public string LocalName
    {
        get { return pstrLocalName; }
    }

    public string Prefix
    {
        get { return pstrPrefix; }
    }

    public string Namespace
    {
        get { return pstrNamespace; }
    }

    public int Depth
    {
        get { return pnDepth; }
    }

    public XmlNodeType NodeType
    {
        get { return pnType; }
    }

    public NodeFlags Flags
    {
        get { return peFlags; }
    }

    public int ChildNodeCount
    {
        get { return pnChildNodes; }
    }

    public void InsertNode(CXmlBase rNode)
    {
        if (prFirstChildNode == null)
        {
            prFirstChildNode = prLastChildNode = rNode;
        }
        else
        {
            prLastChildNode.prNextNode = rNode;
            prLastChildNode = rNode;
        }

        if ((this.peFlags & NodeFlags.IsWhitespace) == 0)
            pnChildNodes++;

        rNode.prParentNode = this;
    }

    //
    // Internal Methods and Properties
    //
    internal CXmlBase _Child(int n)
    {
        int i;
        int j;
        CXmlBase rChild = prFirstChildNode;

        for (i = 0, j = 0; rChild != null; i++, rChild = rChild.prNextNode)
        {
            if ((rChild.peFlags & NodeFlags.IsWhitespace) == 0)
            {
                if (j++ == n) break;
            }
        }

        return rChild;
    }

    internal CXmlBase _Child(string str)
    {
        CXmlBase rChild;

        for (rChild = prFirstChildNode; rChild != null; rChild = rChild.prNextNode)
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
            peFlags |= NodeFlags.DefaultAttribute;
    }

    //
    // Public Methods and Properties (Override)
    //
    override public void Write(XmlWriter rXmlWriter)
    {
        CXmlBase rNode;

        if ((this.peFlags & NodeFlags.DefaultAttribute) == 0)
        {
            rXmlWriter.WriteStartAttribute(this.Prefix, this.LocalName, this.Namespace);

            for (rNode = this.prFirstChildNode; rNode != null; rNode = rNode.prNextNode)
            {
                rNode.Write(rXmlWriter);
            }

            rXmlWriter.WriteEndAttribute();
        }
    }

    override public string Xml
    {
        get
        {
            CXmlCache._rBufferWriter.Dispose();
            WriteXml(CXmlCache._rBufferWriter);
            return CXmlCache._rBufferWriter.ToString();
        }
    }

    override public void WriteXml(TextWriter rTW)
    {
        if ((this.peFlags & NodeFlags.DefaultAttribute) == 0)
        {
            CXmlBase rNode;

            rTW.Write(' ' + this.Name + '=' + this.Quote);
            for (rNode = this.prFirstChildNode; rNode != null; rNode = rNode.prNextNode)
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

            for (rNode = (CXmlNode)this.prFirstChildNode; rNode != null; rNode = rNode.NextNode)
                strValue += rNode.Value;

            return strValue;
        }
    }

    public CXmlAttribute NextAttribute
    {
        get { return (CXmlAttribute)this.prNextNode; }
    }

    public char Quote
    {
        get { return ((base.peFlags & NodeFlags.SingleQuote) != 0 ? '\'' : '"'); }
        set { if (value == '\'') base.peFlags |= NodeFlags.SingleQuote; else base.peFlags &= ~NodeFlags.SingleQuote; }
    }

    public CXmlNode FirstChild
    {
        get { return (CXmlNode)base.prFirstChildNode; }
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
        peFlags |= CXmlCache._eDefaultFlags;

        if (NodeType == XmlNodeType.Whitespace ||
            NodeType == XmlNodeType.SignificantWhitespace)
        {
            peFlags |= NodeFlags.IsWhitespace;
        }

        if (rXmlReader.IsEmptyElement)
        {
            peFlags |= NodeFlags.EmptyElement;
        }

        if (rXmlReader.HasValue)
        {
            peFlags |= NodeFlags.HasValue;
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
                rXmlWriter.WriteCData(_strValue);
                break;

            case XmlNodeType.Comment:
                rXmlWriter.WriteComment(_strValue);
                break;

            case XmlNodeType.DocumentType:
                for (rAttribute = _rFirstAttribute; rAttribute != null; rAttribute = rAttribute.NextAttribute)
                {
                    if (rAttribute.Name == "PUBLIC") { DocTypePublic = rAttribute.Value; }
                    if (rAttribute.Name == "SYSTEM") { DocTypeSystem = rAttribute.Value; }
                }
                rXmlWriter.WriteDocType(this.Name, DocTypePublic, DocTypeSystem, _strValue);
                break;

            case XmlNodeType.EntityReference:
                rXmlWriter.WriteEntityRef(this.Name);
                break;

            case XmlNodeType.ProcessingInstruction:
                rXmlWriter.WriteProcessingInstruction(this.Name, _strValue);
                break;

            case XmlNodeType.Text:
                if (this.Name == string.Empty)
                {
                    if ((this.Flags & NodeFlags.UnparsedEntities) == 0)
                    {
                        rXmlWriter.WriteString(_strValue);
                    }
                    else
                    {
                        rXmlWriter.WriteRaw(_strValue.ToCharArray(), 0, _strValue.Length);
                    }
                }
                else
                {
                    if (this.pstrName[0] == '#')
                        rXmlWriter.WriteCharEntity(_strValue[0]);
                    else
                        rXmlWriter.WriteEntityRef(this.Name);
                }
                break;

            case XmlNodeType.Whitespace:
            case XmlNodeType.SignificantWhitespace:
                if ((this.prParentNode.peFlags & NodeFlags.DocumentRoot) != 0)
                    rXmlWriter.WriteRaw(_strValue.ToCharArray(), 0, _strValue.Length);
                else
                    rXmlWriter.WriteString(_strValue);
                break;

            case XmlNodeType.Element:
                rXmlWriter.WriteStartElement(this.Prefix, this.LocalName, null);

                for (rAttribute = _rFirstAttribute; rAttribute != null; rAttribute = rAttribute.NextAttribute)
                {
                    rAttribute.Write(rXmlWriter);
                }

                if ((this.Flags & NodeFlags.EmptyElement) == 0)
                    rXmlWriter.WriteString(string.Empty);

                for (rNode = base.prFirstChildNode; rNode != null; rNode = rNode.prNextNode)
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
                rXmlWriter.WriteRaw("<?xml " + _strValue + "?>");
                break;

            default:
                throw (new Exception("Node.Write: Unhandled node type " + this.NodeType.ToString()));
        }
    }

    override public string Xml
    {
        get
        {
            CXmlCache._rBufferWriter.Dispose();
            WriteXml(CXmlCache._rBufferWriter);
            return CXmlCache._rBufferWriter.ToString();
        }
    }

    override public void WriteXml(TextWriter rTW)
    {
        String strXml;
        CXmlAttribute rAttribute;
        CXmlBase rNode;

        switch (this.pnType)
        {
            case XmlNodeType.Text:
                if (this.pstrName == "")
                {
                    rTW.Write(_strValue);
                }
                else
                {
                    if (this.pstrName.StartsWith("#"))
                    {
                        rTW.Write("&" + Convert.ToString(Convert.ToInt32(_strValue[0])) + ";");
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
                rTW.Write(_strValue);
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
                if ((base.peFlags & NodeFlags.EmptyElement) == 0)
                {
                    rTW.Write('>');

                    for (rNode = base.prFirstChildNode; rNode != null; rNode = rNode.prNextNode)
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
                rTW.Write("&" + this.pstrName + ";");
                break;

            case XmlNodeType.Notation:
                rTW.Write("<!NOTATION " + _strValue + ">");
                break;

            case XmlNodeType.CDATA:
                rTW.Write("<![CDATA[" + _strValue + "]]>");
                break;

            case XmlNodeType.XmlDeclaration:
            case XmlNodeType.ProcessingInstruction:
                rTW.Write("<?" + this.pstrName + " " + _strValue + "?>");
                break;

            case XmlNodeType.Comment:
                rTW.Write("<!--" + _strValue + "-->");
                break;

            default:
                throw (new Exception("Unhandled NodeType " + this.pnType.ToString()));
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

            if ((this.peFlags & NodeFlags.HasValue) != 0)
            {
                char chEnt;
                int nIndexAmp = 0;
                int nIndexSem = 0;

                if ((this.peFlags & NodeFlags.UnparsedEntities) == 0)
                    return _strValue;

                strValue = _strValue;

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

            for (rChild = (CXmlNode)this.prFirstChildNode; rChild != null; rChild = (CXmlNode)rChild.prNextNode)
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
            CXmlBase rNode = this.prNextNode;

            while (rNode != null &&
                   (rNode.Flags & NodeFlags.IsWhitespace) != 0)
                rNode = rNode.prNextNode;
            return (CXmlNode)rNode;
        }
    }

    public CXmlNode FirstChild
    {
        get
        {
            CXmlBase rNode = this.prFirstChildNode;

            while (rNode != null &&
                   (rNode.Flags & NodeFlags.IsWhitespace) != 0)
                rNode = rNode.prNextNode;
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
        get { return Convert.ToInt32(base.pnType); }
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
            _rLastAttribute.prNextNode = rAttribute;
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
    private bool _fNamespaces = true;

    private bool _fValidationCallback = false;
    private bool _fExpandAttributeValues = false;

    //Internal stuff
    protected XmlReader prXmlReader = null;
    protected CXmlNode prDocumentRootNode;
    protected CXmlNode prRootNode = null;
    internal static NodeFlags _eDefaultFlags = NodeFlags.None;
    internal static BufferWriter _rBufferWriter = new BufferWriter();

    //
    // Constructor
    //
    public CXmlCache() { }

    //
    // Public Methods and Properties
    //
    public virtual bool Load(XmlReader rXmlReader)
    {
        //Hook up your reader as my reader
        prXmlReader = rXmlReader;

        //Process the Document
        try
        {
            prDocumentRootNode = new CXmlNode("", "", XmlNodeType.Element);
            prDocumentRootNode.peFlags = NodeFlags.DocumentRoot | NodeFlags.Indent;
            Process(prDocumentRootNode);
            for (prRootNode = prDocumentRootNode.FirstChild; prRootNode != null && prRootNode.NodeType != XmlNodeType.Element; prRootNode = prRootNode.NextNode) ;
        }
        catch (Exception e)
        {
            //Unhook your reader
            prXmlReader = null;

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
        prXmlReader = null;

        return true;
    }

    public bool Load(string strFileName)
    {
        XmlReader rXmlTextReader;
        bool fRet;

        rXmlTextReader = XmlReader.Create(FilePathUtil.getStream(strFileName));
        fRet = Load(rXmlTextReader);
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
        XmlWriter rXmlTextWriter = null;

        try
        {
            rXmlTextWriter = XmlWriter.Create(FilePathUtil.getStream(strName));

            for (rNode = prDocumentRootNode.prFirstChildNode; rNode != null; rNode = rNode.prNextNode)
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

    virtual public string Xml
    {
        get
        {
            _rBufferWriter.Dispose();
            WriteXml(_rBufferWriter);
            return _rBufferWriter.ToString();
        }
    }

    public void WriteXml(TextWriter rTW)
    {
        CXmlBase rNode;

        //Spit out the document
        for (rNode = prDocumentRootNode.prFirstChildNode; rNode != null; rNode = rNode.prNextNode)
            rNode.WriteXml(rTW);
    }

    public CXmlNode RootNode
    {
        get { return prRootNode; }
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
                TestLog.Write(" ");
            TestLog.WriteLine(str);
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
                if (!prXmlReader.Read())
                {
                    DebugTrace("Read() == false");
                    return;
                }
            }
            else
            {
                if (!prXmlReader.ReadAttributeValue())
                {
                    DebugTrace("ReadAttributeValue() == false");
                    return;
                }
            }

            DebugTrace(prXmlReader);

            //We also want to pop if we get an EndElement or EndEntity
            if (prXmlReader.NodeType == XmlNodeType.EndElement ||
                 prXmlReader.NodeType == XmlNodeType.EndEntity)
            {
                DebugTrace("NodeType == EndElement or EndEntity");
                return;
            }

            rNewNode = GetNewNode(prXmlReader);
            rNewNode.pnDepth = prXmlReader.Depth;

            // Test for MixedContent and set Indent if necessary
            if ((rParentNode.Flags & NodeFlags.MixedContent) != 0)
            {
                rNewNode.peFlags |= NodeFlags.MixedContent;
                // Indent is off for all new nodes
            }
            else
            {
                rNewNode.peFlags |= NodeFlags.Indent;		// Turn on Indent for current Node
            }

            // Set all Depth 0 nodes to No Mixed Content and Indent True
            if (prXmlReader.Depth == 0)
            {
                rNewNode.peFlags |= NodeFlags.Indent;			// Turn on Indent
                rNewNode.peFlags &= ~NodeFlags.MixedContent;	// Turn off MixedContent
            }

            rParentNode.InsertNode(rNewNode);


            //Do some special stuff based on NodeType
            switch (prXmlReader.NodeType)
            {
                case XmlNodeType.Element:
                    if (prXmlReader.MoveToFirstAttribute())
                    {
                        do
                        {
                            CXmlAttribute rNewAttribute = new CXmlAttribute(prXmlReader);
                            rNewNode.AddAttribute(rNewAttribute);

                            if (_fExpandAttributeValues)
                            {
                                DebugTrace("Attribute: " + prXmlReader.Name);
                                _fReadNode = false;
                                Process(rNewAttribute);
                                _fReadNode = true;
                            }
                            else
                            {
                                CXmlNode rValueNode = new CXmlNode("", "", XmlNodeType.Text);
                                rValueNode.peFlags = _eDefaultFlags | NodeFlags.HasValue;

                                rValueNode._strValue = prXmlReader.Value;

                                DebugTrace("  Value=" + rValueNode.Value, prXmlReader.Depth + 1);

                                rNewAttribute.InsertNode(rValueNode);
                            }
                        } while (prXmlReader.MoveToNextAttribute());
                    }

                    if ((rNewNode.Flags & NodeFlags.EmptyElement) == 0)
                        Process(rNewNode);

                    break;

                case XmlNodeType.XmlDeclaration:
                    string strValue = rNewNode.NodeValue;
                    int nPos = strValue.IndexOf("encoding");
                    if (nPos != -1)
                    {
                        int nEnd;

                        nPos = strValue.IndexOf("=", nPos);         //Find the = sign
                        nEnd = strValue.IndexOf("\"", nPos) + 1;    //Find the next " character
                        nPos = strValue.IndexOf("'", nPos) + 1;     //Find the next ' character
                        if (nEnd == 0 || (nPos < nEnd && nPos > 0)) //Pick the one that's closer to the = sign
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
                    break;

                case XmlNodeType.ProcessingInstruction:
                    break;

                case XmlNodeType.Text:
                    if (!_fReadNode)
                    {
                        rNewNode.peFlags = _eDefaultFlags | NodeFlags.AttributeTextNode;
                    }
                    rNewNode.peFlags |= NodeFlags.MixedContent;		// turn on Mixed Content for current node
                    rNewNode.peFlags &= ~NodeFlags.Indent;			// turn off Indent for current node
                    rParentNode.peFlags |= NodeFlags.MixedContent;	// turn on Mixed Content for Parent Node
                    break;

                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.CDATA:
                    rNewNode.peFlags |= NodeFlags.MixedContent;		// turn on Mixed Content for current node
                    rNewNode.peFlags &= ~NodeFlags.Indent;			// turn off Indent for current node
                    rParentNode.peFlags |= NodeFlags.MixedContent;	// turn on Mixed Content for Parent Node
                    break;

                case XmlNodeType.Comment:
                case XmlNodeType.Notation:
                    break;

                case XmlNodeType.DocumentType:
                    if (prXmlReader.MoveToFirstAttribute())
                    {
                        do
                        {
                            CXmlAttribute rNewAttribute = new CXmlAttribute(prXmlReader);
                            rNewNode.AddAttribute(rNewAttribute);

                            CXmlNode rValueNode = new CXmlNode(prXmlReader);
                            rValueNode._strValue = prXmlReader.Value;
                            rNewAttribute.InsertNode(rValueNode);
                        } while (prXmlReader.MoveToNextAttribute());
                    }

                    break;

                default:
                    TestLog.WriteLine("UNHANDLED TYPE, " + prXmlReader.NodeType.ToString() + " IN Process()!");
                    break;
            }
        }
    }

    protected virtual CXmlNode GetNewNode(XmlReader rXmlReader)
    {
        return new CXmlNode(rXmlReader);
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

    public new void Dispose()
    {
        _nPosition = 0;
        _dResult = 0;
    }
}

public class BufferWriter : TextWriter
{
    private int _nBufferSize = 0;
    private int _nBufferUsed = 0;
    private int _nBufferGrow = 1024;
    private Char[] _rgchBuffer = null;
    private Encoding _encoding;

    // --------------------------------------------------------------------------------------------------
    //    Constructor
    // --------------------------------------------------------------------------------------------------
    public BufferWriter()
    {
        _encoding = Encoding.UTF8;
    }

    // --------------------------------------------------------------------------------------------------
    //    Properties
    // --------------------------------------------------------------------------------------------------
    override public string ToString()
    {
        return new String(_rgchBuffer, 0, _nBufferUsed);
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
        if (_nBufferUsed == _nBufferSize)
        {
            Char[] rgchTemp = new Char[_nBufferSize + _nBufferGrow];
            for (_nBufferUsed = 0; _nBufferUsed < _nBufferSize; _nBufferUsed++)
                rgchTemp[_nBufferUsed] = _rgchBuffer[_nBufferUsed];
            _rgchBuffer = rgchTemp;
            _nBufferSize += _nBufferGrow;
            if (_nBufferGrow < (1024 * 1024))
                _nBufferGrow *= 2;
        }
        _rgchBuffer[_nBufferUsed++] = ch;
    }

    public new void Dispose()
    {
        //Set nBufferUsed to 0, so we start writing from the beginning of the buffer.
        _nBufferUsed = 0;
    }
}

