// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public enum EREADER_TYPE
    {
        UNICODE = 0,
        UTF8,
        BIGENDIAN,
        BYTE,
        GENERIC,
        STRING_ONLY,
        BIG_ELEMENT_SIZE,
        JUNK,
        INVALID_NAMESPACE,
        XMLNAMESPACE_TEST,
        XMLLANG_TEST,
        XMLSPACE_TEST,
        WELLFORMED_DTD,
        NONWELLFORMED_DTD,
        INVWELLFORMED_DTD,
        VALID_DTD,
        INVALID_DTD,
        INVALID_SCHEMA,
        XMLSCHEMA,
        XSLT_COPY,
        WHITESPACE_TEST,
        BASE64_TEST,
        BINHEX_TEST,
        CONSTRUCTOR,
        LINENUMBER,
        LBNORMALIZATION,
        SCHEMATYPE,
        BINARY
    };

    ////////////////////////////////////////////////////////////////
    // CDataReader
    //
    ////////////////////////////////////////////////////////////////
    public class CDataReader
    {
        private const string ST_XML = "xml";

        public const int STATUS_PASSED = 1;
        public const int STATUS_FAILED = 0;

        private EREADER_TYPE _eType;
        private string _strSource;
        private object _oInternal;

        // CDataReader constructor
        internal bool _iswrapping = false;

        public CDataReader(object objInternal, EREADER_TYPE eReaderType, string strSource)
        {
            _eType = eReaderType;
            _strSource = strSource;
            _oInternal = objInternal;
        }

        public virtual XmlReader Internal
        {
            get { return (XmlReader)_oInternal; }
            set { _oInternal = value; }
        }

        public virtual EREADER_TYPE ReaderType
        {
            get { return _eType; }
        }


        /// <summary>
        /// The following methods just wrap the XmlReader methods by explicitly calling XmlReader methods from CDataReader.
        /// </summary>

        public virtual XmlNodeType NodeType
        {
            get { return Internal.NodeType; }
        }

        public void CheckWrappingReader()
        {
            if (_iswrapping)
            {
                XmlCustomReader customReader = _oInternal as XmlCustomReader;
                if (!customReader.IsCalled)
                    throw new CTestFailedException("Custom Extended reader not called");
            }
        }

        public void ResetWrappingReader()
        {
            if (_iswrapping)
            {
                ((XmlCustomReader)_oInternal).IsCalled = true;
            }
        }

        public virtual string Name
        {
            get { return Internal.Name; }
        }

        public virtual string LocalName
        {
            get { return Internal.LocalName; }
        }

        public virtual string NamespaceURI
        {
            get { return Internal.NamespaceURI; }
        }

        public virtual string Prefix
        {
            get { return Internal.Prefix; }
        }

        public virtual bool HasValue
        {
            get { return Internal.HasValue; }
        }

        public virtual string Value
        {
            get { return Internal.Value; }
        }

        public virtual Type ValueType
        {
            get { return Internal.ValueType; }
        }

        public virtual int Depth
        {
            get { return Internal.Depth; }
        }

        public virtual string BaseURI
        {
            get { return Internal.BaseURI; }
        }

        public virtual bool IsEmptyElement
        {
            get { return Internal.IsEmptyElement; }
        }

        public virtual bool IsDefault
        {
            get { return Internal.IsDefault; }
        }

        public virtual XmlSpace XmlSpace
        {
            get { return Internal.XmlSpace; }
        }

        public virtual string XmlLang
        {
            get { return Internal.XmlLang; }
        }

        public virtual int AttributeCount
        {
            get { return Internal.AttributeCount; }
        }

        public virtual bool HasAttributes
        {
            get { return Internal.HasAttributes; }
        }

        public virtual bool CanResolveEntity
        {
            get { return Internal.CanResolveEntity; }
        }

        public string GetAttribute(string name)
        {
            ResetWrappingReader();
            string s = Internal.GetAttribute(name);
            CheckWrappingReader();
            return s;
        }

        public string GetAttribute(string name, string namespaceURI)
        {
            ResetWrappingReader();
            string s = Internal.GetAttribute(name, namespaceURI);
            CheckWrappingReader();
            return s;
        }

        public string GetAttribute(int i)
        {
            ResetWrappingReader();
            string s = Internal.GetAttribute(i);
            CheckWrappingReader();
            return s;
        }

        public string this[string name]
        {
            get
            {
                ResetWrappingReader();
                string s = Internal[name];
                CheckWrappingReader();
                return s;
            }
        }

        public string this[string name, string namespaceURI]
        {
            get
            {
                ResetWrappingReader();
                string s = Internal[name, namespaceURI];
                CheckWrappingReader();
                return s;
            }
        }

        public string this[int i]
        {
            get
            {
                ResetWrappingReader();
                string s = Internal[i];
                CheckWrappingReader();
                return s;
            }
        }

        public bool MoveToAttribute(string name)
        {
            ResetWrappingReader();
            bool x = Internal.MoveToAttribute(name);
            CheckWrappingReader();
            return x;
        }

        public bool MoveToAttribute(string name, string namespaceURI)
        {
            bool x = Internal.MoveToAttribute(name, namespaceURI);
            return x;
        }

        public void MoveToAttribute(int i)
        {
            ResetWrappingReader();
            Internal.MoveToAttribute(i);
            CheckWrappingReader();
        }

        public bool MoveToFirstAttribute()
        {
            ResetWrappingReader();
            bool x = Internal.MoveToFirstAttribute();
            CheckWrappingReader();
            return x;
        }

        public bool MoveToNextAttribute()
        {
            ResetWrappingReader();
            bool x = Internal.MoveToNextAttribute();
            CheckWrappingReader();
            return x;
        }

        public bool MoveToElement()
        {
            ResetWrappingReader();
            bool x = Internal.MoveToElement();
            CheckWrappingReader();
            return x;
        }

        public virtual bool Read()
        {
            ResetWrappingReader();
            bool x = Internal.Read();
            CheckWrappingReader();
            return x;
        }

        public virtual bool EOF
        {
            get
            {
                ResetWrappingReader();
                bool x = Internal.EOF;
                CheckWrappingReader();
                return x;
            }
        }

        public virtual void Close()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            if (Internal != null)
            {
                IDisposable disposableObj = Internal as IDisposable;
                if (disposableObj != null)
                    disposableObj.Dispose();
            }
        }

        public virtual ReadState ReadState
        {
            get
            {
                ResetWrappingReader();
                ReadState rs = Internal.ReadState;
                CheckWrappingReader();
                return rs;
            }
        }

        public virtual void Skip()
        {
            ResetWrappingReader();
            Internal.Skip();
            CheckWrappingReader();
        }

        public string ReadInnerXml()
        {
            ResetWrappingReader();
            string s = Internal.ReadInnerXml();
            CheckWrappingReader();
            return s;
        }

        public string ReadOuterXml()
        {
            ResetWrappingReader();
            string s = Internal.ReadOuterXml();
            CheckWrappingReader();
            return s;
        }

        public XmlNodeType MoveToContent()
        {
            ResetWrappingReader();
            XmlNodeType s = Internal.MoveToContent();
            CheckWrappingReader();
            return s;
        }

        public bool IsStartElement()
        {
            ResetWrappingReader();
            bool x = Internal.IsStartElement();
            CheckWrappingReader();
            return x;
        }

        public bool IsStartElement(string name)
        {
            ResetWrappingReader();
            bool x = Internal.IsStartElement(name);
            CheckWrappingReader();
            return x;
        }

        public bool IsStartElement(string name, string ns)
        {
            ResetWrappingReader();
            bool x = Internal.IsStartElement(name, ns);
            CheckWrappingReader();
            return x;
        }

        public void ReadStartElement()
        {
            ResetWrappingReader();
            Internal.ReadStartElement();
            CheckWrappingReader();
        }

        public void ReadStartElement(string name)
        {
            ResetWrappingReader();
            Internal.ReadStartElement(name);
            CheckWrappingReader();
        }

        public void ReadStartElement(string name, string ns)
        {
            ResetWrappingReader();
            Internal.ReadStartElement(name, ns);
            CheckWrappingReader();
        }

        public void ReadEndElement()
        {
            ResetWrappingReader();
            Internal.ReadEndElement();
            CheckWrappingReader();
        }

        public virtual XmlNameTable NameTable
        {
            get
            {
                ResetWrappingReader();
                XmlNameTable n = Internal.NameTable;
                CheckWrappingReader();
                return n;
            }
        }

        public string LookupNamespace(string prefix)
        {
            ResetWrappingReader();
            string s = Internal.LookupNamespace(prefix);
            CheckWrappingReader();
            return s;
        }

        public void ResolveEntity()
        {
            ResetWrappingReader();
            Internal.ResolveEntity();
            CheckWrappingReader();
        }

        public bool ReadAttributeValue()
        {
            ResetWrappingReader();
            bool x = Internal.ReadAttributeValue();
            CheckWrappingReader();
            return x;
        }

        public XmlReader ReadSubtree()
        {
            ResetWrappingReader();
            XmlReader x = Internal.ReadSubtree();
            CheckWrappingReader();
            return x;
        }

        public bool ReadToDescendant(string name)
        {
            ResetWrappingReader();
            bool x = Internal.ReadToDescendant(name);
            CheckWrappingReader();
            return x;
        }

        public bool ReadToDescendant(string localName, string ns)
        {
            ResetWrappingReader();
            bool x = Internal.ReadToDescendant(localName, ns);
            CheckWrappingReader();
            return x;
        }

        public bool ReadToFollowing(string name)
        {
            ResetWrappingReader();
            bool x = Internal.ReadToFollowing(name);
            CheckWrappingReader();
            return x;
        }

        public bool ReadToFollowing(string localName, string ns)
        {
            ResetWrappingReader();
            bool x = Internal.ReadToFollowing(localName, ns);
            CheckWrappingReader();
            return x;
        }

        public bool ReadToNextSibling(string name)
        {
            ResetWrappingReader();
            bool x = Internal.ReadToNextSibling(name);
            CheckWrappingReader();
            return x;
        }

        public bool ReadToNextSibling(string localName, string ns)
        {
            ResetWrappingReader();
            bool x = Internal.ReadToNextSibling(localName, ns);
            CheckWrappingReader();
            return x;
        }

        public XmlReaderSettings Settings
        {
            get
            {
                ResetWrappingReader();
                XmlReaderSettings x = Internal.Settings;
                CheckWrappingReader();
                return x;
            }
        }

        public virtual object ReadElementContentAsObject()
        {
            ResetWrappingReader();
            object x = Internal.ReadElementContentAsObject();
            CheckWrappingReader();
            return x;
        }

        public virtual bool ReadElementContentAsBoolean()
        {
            ResetWrappingReader();
            bool x = Internal.ReadElementContentAsBoolean();
            CheckWrappingReader();
            return x;
        }

        public virtual double ReadElementContentAsDouble()
        {
            ResetWrappingReader();
            double x = Internal.ReadElementContentAsDouble();
            CheckWrappingReader();
            return x;
        }

        public virtual int ReadElementContentAsInt()
        {
            ResetWrappingReader();
            int x = Internal.ReadElementContentAsInt();
            CheckWrappingReader();
            return x;
        }

        public virtual long ReadElementContentAsLong()
        {
            ResetWrappingReader();
            long x = Internal.ReadElementContentAsLong();
            CheckWrappingReader();
            return x;
        }

        public virtual float ReadElementContentAsFloat()
        {
            ResetWrappingReader();
            float x = Internal.ReadElementContentAsFloat();
            CheckWrappingReader();
            return x;
        }

        public virtual decimal ReadElementContentAsDecimal()
        {
            ResetWrappingReader();
            decimal x = Internal.ReadElementContentAsDecimal();
            CheckWrappingReader();
            return x;
        }

        public virtual string ReadElementContentAsString()
        {
            ResetWrappingReader();
            string x = Internal.ReadElementContentAsString();
            CheckWrappingReader();
            return x;
        }

        //Overloads
        public virtual object ReadElementContentAsObject(string localName, string namespaceUri)
        {
            ResetWrappingReader();
            object x = Internal.ReadElementContentAsObject(localName, namespaceUri);
            CheckWrappingReader();
            return x;
        }

        public virtual object ReadElementContentAs(System.Type type, IXmlNamespaceResolver resolver)
        {
            ResetWrappingReader();
            object x = Internal.ReadElementContentAs(type, resolver);
            CheckWrappingReader();
            return x;
        }

        public virtual object ReadElementContentAs(System.Type type, IXmlNamespaceResolver resolver, string localName, string namespaceUri)
        {
            ResetWrappingReader();
            object x = Internal.ReadElementContentAs(type, resolver, localName, namespaceUri);
            CheckWrappingReader();
            return x;
        }

        public virtual bool ReadElementContentAsBoolean(string localName, string namespaceUri)
        {
            ResetWrappingReader();
            bool x = Internal.ReadElementContentAsBoolean(localName, namespaceUri);
            CheckWrappingReader();
            return x;
        }

        public virtual double ReadElementContentAsDouble(string localName, string namespaceUri)
        {
            ResetWrappingReader();
            double x = Internal.ReadElementContentAsDouble(localName, namespaceUri);
            CheckWrappingReader();
            return x;
        }

        public virtual int ReadElementContentAsInt(string localName, string namespaceUri)
        {
            ResetWrappingReader();
            int x = Internal.ReadElementContentAsInt(localName, namespaceUri);
            CheckWrappingReader();
            return x;
        }

        public virtual long ReadElementContentAsLong(string localName, string namespaceUri)
        {
            ResetWrappingReader();
            long x = Internal.ReadElementContentAsLong(localName, namespaceUri);
            CheckWrappingReader();
            return x;
        }

        public virtual decimal ReadElementContentAsDecimal(string localName, string namespaceUri)
        {
            ResetWrappingReader();
            decimal x = Internal.ReadElementContentAsDecimal(localName, namespaceUri);
            CheckWrappingReader();
            return x;
        }

        public virtual float ReadElementContentAsFloat(string localName, string namespaceUri)
        {
            ResetWrappingReader();
            float x = Internal.ReadElementContentAsFloat(localName, namespaceUri);
            CheckWrappingReader();
            return x;
        }

        public virtual string ReadElementContentAsString(string localName, string namespaceUri)
        {
            ResetWrappingReader();
            string x = Internal.ReadElementContentAsString(localName, namespaceUri);
            CheckWrappingReader();
            return x;
        }


        //Non-Element calls.
        public virtual object ReadContentAs(System.Type type, IXmlNamespaceResolver resolver)
        {
            ResetWrappingReader();
            object x = Internal.ReadContentAs(type, resolver);
            CheckWrappingReader();
            return x;
        }

        public virtual bool ReadContentAsBoolean()
        {
            ResetWrappingReader();
            bool x = Internal.ReadContentAsBoolean();
            CheckWrappingReader();
            return x;
        }

        public virtual DateTimeOffset ReadContentAsDateTimeOffset()
        {
            ResetWrappingReader();
            DateTimeOffset x = Internal.ReadContentAsDateTimeOffset();
            CheckWrappingReader();
            return x;
        }

        public virtual double ReadContentAsDouble()
        {
            ResetWrappingReader();
            double x = Internal.ReadContentAsDouble();
            CheckWrappingReader();
            return x;
        }

        public virtual int ReadContentAsInt()
        {
            ResetWrappingReader();
            int x = Internal.ReadContentAsInt();
            CheckWrappingReader();
            return x;
        }

        public virtual long ReadContentAsLong()
        {
            ResetWrappingReader();
            long x = Internal.ReadContentAsLong();
            CheckWrappingReader();
            return x;
        }

        public virtual string ReadContentAsString()
        {
            ResetWrappingReader();
            string x = Internal.ReadContentAsString();
            CheckWrappingReader();
            return x;
        }

        public virtual decimal ReadContentAsDecimal()
        {
            ResetWrappingReader();
            decimal x = Internal.ReadContentAsDecimal();
            CheckWrappingReader();
            return x;
        }

        public virtual float ReadContentAsFloat()
        {
            ResetWrappingReader();
            float x = Internal.ReadContentAsFloat();
            CheckWrappingReader();
            return x;
        }

        public virtual object ReadContentAsObject()
        {
            ResetWrappingReader();
            object x = Internal.ReadContentAsObject();
            CheckWrappingReader();
            return x;
        }

        public virtual int LineNumber
        {
            get
            {
                IXmlLineInfo li = (IXmlLineInfo)Internal;

                if (li.HasLineInfo())
                    return li.LineNumber;
                else
                    throw new XmlException("Line Number information is not available");
            }
        }

        public virtual int LinePosition
        {
            get
            {
                IXmlLineInfo li = (IXmlLineInfo)Internal;
                if (li.HasLineInfo())
                {
                    return li.LinePosition;
                }
                else
                {
                    throw new Exception("LinePosition not supported in " + Internal.GetType());
                }
            }
        }

        public virtual bool Namespaces
        {
            set
            {
            }
            get
            {
                return false;
            }
        }

        public bool CanReadBinaryContent
        {
            get
            {
                ResetWrappingReader();
                bool x = Internal.CanReadBinaryContent;
                CheckWrappingReader();
                return x;
            }
        }

        public int ReadContentAsBase64(byte[] array, int offset, int len)
        {
            ResetWrappingReader();
            int x = Internal.ReadContentAsBase64(array, offset, len);
            CheckWrappingReader();
            return x;
        }

        public int ReadContentAsBinHex(byte[] array, int offset, int len)
        {
            ResetWrappingReader();
            int x = Internal.ReadContentAsBinHex(array, offset, len);
            CheckWrappingReader();
            return x;
        }

        public int ReadElementContentAsBase64(byte[] array, int offset, int len)
        {
            ResetWrappingReader();
            int x = Internal.ReadElementContentAsBase64(array, offset, len);
            CheckWrappingReader();
            return x;
        }

        public int ReadElementContentAsBinHex(byte[] array, int offset, int len)
        {
            ResetWrappingReader();
            int x = Internal.ReadElementContentAsBinHex(array, offset, len);
            CheckWrappingReader();
            return x;
        }

        public bool CanReadValueChunk
        {
            get
            {
                ResetWrappingReader();
                bool x = Internal.CanReadValueChunk;
                CheckWrappingReader();
                return x;
            }
        }

        public int ReadValueChunk(char[] buffer, int offset, int count)
        {
            ResetWrappingReader();
            int x = Internal.ReadValueChunk(buffer, offset, count);
            CheckWrappingReader();
            return x;
        }

        public void ResetState()
        {
        }

        ////////////////////////////////////////////////////////////////
        // Dump ALL properties
        //
        ////////////////////////////////////////////////////////////////
        public void DumpAll()
        {
            while (Read())
            {
                CError.Write("NodeType  = " + NodeType + "\t|\t");
                CError.Write("NodeName  = " + Name + "\t|\t");
                CError.Write("NodeLocalName  = " + LocalName + "\t|\t");
                CError.Write("NodeNamespace  = " + NamespaceURI + "\t|\t");
                CError.Write("NodePrefix  = " + Prefix + "\t|\t");
                CError.Write("NodeHasValue  = " + (HasValue).ToString() + "\t|\t");
                CError.Write("NodeValue = " + Value + "\t|\t");
                CError.Write("NodeDepth = " + Depth + "\t|\t");
                CError.Write("IsEmptyElement = " + IsEmptyElement.ToString() + "\t|\t");
                CError.Write("IsDefault = " + IsDefault.ToString() + "\t|\t");
                CError.Write("XmlSpace = " + XmlSpace + "\t|\t");
                CError.Write("XmlLang = " + XmlLang + "\t|\t");

                CError.Write("AttributeCount = " + AttributeCount + "\t|\t");
                CError.Write("HasAttributes = " + HasAttributes.ToString() + "\t|\t");

                CError.Write("EOF = " + EOF.ToString() + "\t|\t");
                CError.Write("ReadState = " + ReadState.ToString() + "\t|\t");
                CError.WriteLine();
            }
        }

        public void DumpOneNode()
        {
            CError.Write("NodeType  = " + NodeType + "\t|\t");
            CError.Write("NodeName  = " + Name + "\t|\t");
            CError.Write("NodeLocalName  = " + LocalName + "\t|\t");
            CError.Write("NodeNamespace  = " + NamespaceURI + "\t|\t");
            CError.Write("NodePrefix  = " + Prefix + "\t|\t");
            CError.Write("NodeHasValue  = " + (HasValue).ToString() + "\t|\t");
            CError.Write("NodeValue = " + Value + "\t|\t");
            CError.Write("NodeDepth = " + Depth + "\t|\t");
            CError.Write("IsEmptyElement = " + (IsEmptyElement).ToString() + "\t|\t");
            CError.Write("IsDefault = " + (IsDefault).ToString() + "\t|\t");
            CError.Write("XmlSpace = " + XmlSpace + "\t|\t");
            CError.Write("XmlLang = " + XmlLang + "\t|\t");

            CError.Write("AttributeCount = " + AttributeCount + "\t|\t");
            CError.Write("HasAttributes = " + (HasAttributes).ToString() + "\t|\t");

            CError.Write("EOF = " + (EOF).ToString() + "\t|\t");
            CError.Write("ReadState = " + (ReadState).ToString() + "\t|\t");

            if (AttributeCount > 0)
            {
                CError.WriteLine();
                for (int i = 0; i < AttributeCount; i++)
                {
                    CError.Write("GetAttribute(" + i + ")= " + GetAttribute(i) + "\t|\t");
                }
            }

            CError.WriteLine();
        }

        private void DumpChars(string strActValue)
        {
            byte c;
            int i;

            for (i = 0; i < strActValue.Length; i++)
            {
                c = Convert.ToByte(strActValue[i]);
                CError.WriteLine("Char[" + i.ToString() + "] : " + c.ToString());
            }
        }

        public int FindNodeType(XmlNodeType _nodetype)
        {
            if (NodeType == _nodetype)
                return STATUS_PASSED;

            while (Read())
            {
                if (NodeType == XmlNodeType.EntityReference)
                {
                    if (CanResolveEntity)
                        ResolveEntity();
                }

                if (NodeType == XmlNodeType.ProcessingInstruction && NodeType == XmlNodeType.XmlDeclaration)
                {
                    if (string.Compare(Name, 0, ST_XML, 0, 3) != 0)
                        return STATUS_PASSED;
                }

                if (NodeType == _nodetype)
                {
                    return STATUS_PASSED;
                }

                if (NodeType == XmlNodeType.Element && (_nodetype == XmlNodeType.Attribute))
                {
                    if (MoveToFirstAttribute())
                    {
                        return STATUS_PASSED;
                    }
                }
            }
            return STATUS_FAILED;
        }

        ///////////////////////////////////////
        // PositionOnElement
        ///////////////////////////////////////
        public void PositionOnElement(string strElementName)
        {
            CError.WriteLine("Seeking Element : " + strElementName);

            if (NodeType == XmlNodeType.Element && Name == strElementName)
                return;

            while (Read())
            {
                if (NodeType == XmlNodeType.Element && Name == strElementName)
                    break;
            }

            if (EOF)
            {
                throw new CTestException(CTestBase.TEST_FAIL, "Couldn't find element '" + strElementName + "'");
            }
        }

        //////////////////////////////////////////
        // PositionOnNodeType
        //////////////////////////////////////////
        public void PositionOnNodeType(XmlNodeType nodeType)
        {
            CError.WriteLine("Seeking Nodetype : " + nodeType.ToString());

            if (NodeType == nodeType)
                return;

            while (Read() && NodeType != nodeType)
            {
                if (NodeType == XmlNodeType.EntityReference)
                {
                    if (CanResolveEntity)
                        ResolveEntity();
                }
                if (nodeType == XmlNodeType.ProcessingInstruction && NodeType == XmlNodeType.XmlDeclaration)
                {
                    if (string.Compare(Name, 0, ST_XML, 0, 3) != 0)
                        return;
                }
                if (NodeType == XmlNodeType.Element && nodeType == XmlNodeType.Attribute)
                {
                    if (MoveToFirstAttribute())
                    {
                        return;
                    }
                }
            }
            if (EOF)
            {
                throw new CTestException(CTestBase.TEST_FAIL, "Couldn't find XmlNodeType " + nodeType);
            }
        }

        ///////////////////////
        // VerifyNode
        ///////////////////////
        public bool VerifyNode(XmlNodeType eExpNodeType, string strExpName, string strExpValue)
        {
            bool bPassed = true;

            if (NodeType != eExpNodeType)
            {
                CError.WriteLine("NodeType doesn't match");
                CError.WriteLine("    Expected NodeType: " + eExpNodeType);
                CError.WriteLine("    Actual NodeType: " + NodeType);
                bPassed = false;
            }
            if (Name != strExpName)
            {
                CError.WriteLine("Name doesn't match:");
                CError.WriteLine("    Expected Name: '" + strExpName + "'");
                CError.WriteLine("    Actual Name: '" + Name + "'");

                bPassed = false;
            }
            if (Value != strExpValue)
            {
                CError.WriteLine("Value doesn't match:");
                CError.WriteLine("    Expected Value: '" + strExpValue + "'");
                CError.WriteLine("    Actual Value: '" + Value + "'");

                bPassed = false;
            }

            if (bPassed)
            {
                CError.WriteLine("Passed");
            }

            return bPassed;
        }

        ///////////////////////
        // CompareNode
        ///////////////////////
        public void CompareNode(XmlNodeType eExpNodeType, string strExpName, string strExpValue)
        {
            bool bNode = VerifyNode(eExpNodeType, strExpName, strExpValue);
            CError.Compare(bNode, "VerifyNode failed");
        }
    }


    #region CustomReader
    /// <summary>
    /// CustomReader which wraps Factory created reader.
    /// </summary>

    public class CustomReader : XmlReader, IXmlLineInfo
    {
        private XmlReader _tr = null;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_tr != null)
                _tr.Dispose();
        }

        public int LinePosition
        {
            get
            {
                return ((IXmlLineInfo)_tr).LinePosition;
            }
        }

        public int LineNumber
        {
            get
            {
                return ((IXmlLineInfo)_tr).LineNumber;
            }
        }

        public bool HasLineInfo()
        {
            return ((IXmlLineInfo)_tr).HasLineInfo();
        }

        public CustomReader(string filename)
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = DtdProcessing.Ignore;
            _tr = ReaderHelper.Create(filename, rs);
        }

        public CustomReader(TextReader txtReader, bool isFragment)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Ignore;
            if (!isFragment)
                _tr = ReaderHelper.Create(txtReader, settings, (string)null);
            else
            {
                settings.ConformanceLevel = ConformanceLevel.Fragment;
                _tr = ReaderHelper.Create(txtReader, settings, (string)null);
            }
        }

        public CustomReader(string url, bool isFragment)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Ignore;
            if (!isFragment)
                _tr = ReaderHelper.Create(url, settings);
            else
            {
                settings.ConformanceLevel = ConformanceLevel.Fragment;
                _tr = ReaderHelper.Create(url, settings);
            }
        }

        public CustomReader(Stream stream)
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = DtdProcessing.Ignore;
            _tr = ReaderHelper.Create(stream, rs, (string)null);
        }

        public override int Depth
        {
            get
            {
                return _tr.Depth;
            }
        }

        public override string Value
        {
            get
            {
                return _tr.Value;
            }
        }

        public override bool MoveToElement()
        {
            return _tr.MoveToElement();
        }

        public override string LocalName
        {
            get
            {
                return _tr.LocalName;
            }
        }

        public override XmlNodeType NodeType
        {
            get
            {
                return _tr.NodeType;
            }
        }

        public override bool MoveToNextAttribute()
        {
            return _tr.MoveToNextAttribute();
        }

        public override bool MoveToFirstAttribute()
        {
            return _tr.MoveToFirstAttribute();
        }

        public override string LookupNamespace(string prefix)
        {
            return _tr.LookupNamespace(prefix);
        }

        public override bool EOF
        {
            get
            {
                return _tr.EOF;
            }
        }

        public override bool HasValue
        {
            get
            {
                return _tr.HasValue;
            }
        }

        public override string NamespaceURI
        {
            get
            {
                return _tr.NamespaceURI;
            }
        }

        public override bool Read()
        {
            return _tr.Read();
        }

        public override XmlNameTable NameTable
        {
            get
            {
                return _tr.NameTable;
            }
        }

        public override bool CanResolveEntity
        {
            get
            {
                return _tr.CanResolveEntity;
            }
        }

        public override void ResolveEntity()
        {
            _tr.ResolveEntity();
        }

        public override string GetAttribute(string name, string namespaceURI)
        {
            return _tr.GetAttribute(name, namespaceURI);
        }

        public override string GetAttribute(string name)
        {
            return _tr.GetAttribute(name);
        }

        public override string GetAttribute(int i)
        {
            return _tr.GetAttribute(i);
        }

        public override string BaseURI
        {
            get
            {
                return _tr.BaseURI;
            }
        }

        public override bool ReadAttributeValue()
        {
            return _tr.ReadAttributeValue();
        }

        public override string Prefix
        {
            get
            {
                return _tr.Prefix;
            }
        }

        public override bool MoveToAttribute(string name, string ns)
        {
            return _tr.MoveToAttribute(name, ns);
        }

        public override bool MoveToAttribute(string name)
        {
            return _tr.MoveToAttribute(name);
        }

        public override int AttributeCount
        {
            get
            {
                return _tr.AttributeCount;
            }
        }

        public override ReadState ReadState
        {
            get
            {
                return _tr.ReadState;
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
                return _tr.IsEmptyElement;
            }
        }
    }
    #endregion
}
