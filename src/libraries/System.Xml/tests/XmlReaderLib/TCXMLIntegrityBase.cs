// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCXMLIntegrityBase : CDataReaderTestCase
    {
        // Type is System.Xml.Tests.TCXMLIntegrityBase
        // Test Case
        public override void AddChildren()
        {
            // for function GetXmlReaderNodeType
            {
                this.AddChild(new CVariation(GetXmlReaderNodeType) { Attribute = new Variation("NodeType") });
            }


            // for function GetXmlReaderName
            {
                this.AddChild(new CVariation(GetXmlReaderName) { Attribute = new Variation("Name") });
            }


            // for function GetXmlReaderLocalName
            {
                this.AddChild(new CVariation(GetXmlReaderLocalName) { Attribute = new Variation("LocalName") });
            }


            // for function Namespace
            {
                this.AddChild(new CVariation(Namespace) { Attribute = new Variation("NamespaceURI") });
            }


            // for function Prefix
            {
                this.AddChild(new CVariation(Prefix) { Attribute = new Variation("Prefix") });
            }


            // for function HasValue
            {
                this.AddChild(new CVariation(HasValue) { Attribute = new Variation("HasValue") });
            }


            // for function GetXmlReaderValue
            {
                this.AddChild(new CVariation(GetXmlReaderValue) { Attribute = new Variation("Value") });
            }


            // for function GetDepth
            {
                this.AddChild(new CVariation(GetDepth) { Attribute = new Variation("Depth") });
            }


            // for function GetBaseURI
            {
                this.AddChild(new CVariation(GetBaseURI) { Attribute = new Variation("BaseURI") });
            }


            // for function IsEmptyElement
            {
                this.AddChild(new CVariation(IsEmptyElement) { Attribute = new Variation("IsEmptyElement") });
            }


            // for function IsDefault
            {
                this.AddChild(new CVariation(IsDefault) { Attribute = new Variation("IsDefault") });
            }


            // for function GetXmlSpace
            {
                this.AddChild(new CVariation(GetXmlSpace) { Attribute = new Variation("XmlSpace") });
            }


            // for function GetXmlLang
            {
                this.AddChild(new CVariation(GetXmlLang) { Attribute = new Variation("XmlLang") });
            }


            // for function AttributeCount
            {
                this.AddChild(new CVariation(AttributeCount) { Attribute = new Variation("AttributeCount") });
            }


            // for function HasAttribute
            {
                this.AddChild(new CVariation(HasAttribute) { Attribute = new Variation("HasAttributes") });
            }


            // for function GetAttributeName
            {
                this.AddChild(new CVariation(GetAttributeName) { Attribute = new Variation("GetAttributes(name)") });
            }


            // for function GetAttributeEmptyName
            {
                this.AddChild(new CVariation(GetAttributeEmptyName) { Attribute = new Variation("GetAttribute(String.Empty)") });
            }


            // for function GetAttributeNameNamespace
            {
                this.AddChild(new CVariation(GetAttributeNameNamespace) { Attribute = new Variation("GetAttribute(name,ns)") });
            }


            // for function GetAttributeEmptyNameNamespace
            {
                this.AddChild(new CVariation(GetAttributeEmptyNameNamespace) { Attribute = new Variation("GetAttribute(String.Empty, String.Empty)") });
            }


            // for function GetAttributeOrdinal
            {
                this.AddChild(new CVariation(GetAttributeOrdinal) { Attribute = new Variation("GetAttribute(i)") });
            }


            // for function HelperThisOrdinal
            {
                this.AddChild(new CVariation(HelperThisOrdinal) { Attribute = new Variation("this[i]") });
            }


            // for function HelperThisName
            {
                this.AddChild(new CVariation(HelperThisName) { Attribute = new Variation("this[name]") });
            }


            // for function HelperThisNameNamespace
            {
                this.AddChild(new CVariation(HelperThisNameNamespace) { Attribute = new Variation("this[name,namespace]") });
            }


            // for function MoveToAttributeName
            {
                this.AddChild(new CVariation(MoveToAttributeName) { Attribute = new Variation("MoveToAttribute(name)") });
            }


            // for function MoveToAttributeNameNamespace
            {
                this.AddChild(new CVariation(MoveToAttributeNameNamespace) { Attribute = new Variation("MoveToAttributeNameNamespace(name,ns)") });
            }


            // for function MoveToAttributeOrdinal
            {
                this.AddChild(new CVariation(MoveToAttributeOrdinal) { Attribute = new Variation("MoveToAttribute(i)") });
            }


            // for function MoveToFirstAttribute
            {
                this.AddChild(new CVariation(MoveToFirstAttribute) { Attribute = new Variation("MoveToFirstAttribute()") });
            }


            // for function MoveToNextAttribute
            {
                this.AddChild(new CVariation(MoveToNextAttribute) { Attribute = new Variation("MoveToNextAttribute()") });
            }


            // for function MoveToElement
            {
                this.AddChild(new CVariation(MoveToElement) { Attribute = new Variation("MoveToElement()") });
            }


            // for function ReadTestAfterClose
            {
                this.AddChild(new CVariation(ReadTestAfterClose) { Attribute = new Variation("Read") });
            }


            // for function GetEOF
            {
                this.AddChild(new CVariation(GetEOF) { Attribute = new Variation("GetEOF") });
            }


            // for function GetReadState
            {
                this.AddChild(new CVariation(GetReadState) { Attribute = new Variation("GetReadState") });
            }


            // for function XMLSkip
            {
                this.AddChild(new CVariation(XMLSkip) { Attribute = new Variation("Skip") });
            }


            // for function TestNameTable
            {
                this.AddChild(new CVariation(TestNameTable) { Attribute = new Variation("NameTable") });
            }


            // for function ReadInnerXmlTestAfterClose
            {
                this.AddChild(new CVariation(ReadInnerXmlTestAfterClose) { Attribute = new Variation("ReadInnerXml") });
            }


            // for function TestReadOuterXml
            {
                this.AddChild(new CVariation(TestReadOuterXml) { Attribute = new Variation("ReadOuterXml") });
            }


            // for function TestMoveToContent
            {
                this.AddChild(new CVariation(TestMoveToContent) { Attribute = new Variation("MoveToContent") });
            }


            // for function TestIsStartElement
            {
                this.AddChild(new CVariation(TestIsStartElement) { Attribute = new Variation("IsStartElement") });
            }


            // for function TestIsStartElementName
            {
                this.AddChild(new CVariation(TestIsStartElementName) { Attribute = new Variation("IsStartElement(name)") });
            }


            // for function TestIsStartElementName2
            {
                this.AddChild(new CVariation(TestIsStartElementName2) { Attribute = new Variation("IsStartElement(String.Empty)") });
            }


            // for function TestIsStartElementNameNs
            {
                this.AddChild(new CVariation(TestIsStartElementNameNs) { Attribute = new Variation("IsStartElement(name, ns)") });
            }


            // for function TestIsStartElementNameNs2
            {
                this.AddChild(new CVariation(TestIsStartElementNameNs2) { Attribute = new Variation("IsStartElement(String.Empty,String.Empty)") });
            }


            // for function TestReadStartElement
            {
                this.AddChild(new CVariation(TestReadStartElement) { Attribute = new Variation("ReadStartElement") });
            }


            // for function TestReadStartElementName
            {
                this.AddChild(new CVariation(TestReadStartElementName) { Attribute = new Variation("ReadStartElement(name)") });
            }


            // for function TestReadStartElementName2
            {
                this.AddChild(new CVariation(TestReadStartElementName2) { Attribute = new Variation("ReadStartElement(String.Empty)") });
            }


            // for function TestReadStartElementNameNs
            {
                this.AddChild(new CVariation(TestReadStartElementNameNs) { Attribute = new Variation("ReadStartElement(name, ns)") });
            }


            // for function TestReadStartElementNameNs2
            {
                this.AddChild(new CVariation(TestReadStartElementNameNs2) { Attribute = new Variation("ReadStartElement(String.Empty,String.Empty)") });
            }


            // for function TestReadEndElement
            {
                this.AddChild(new CVariation(TestReadEndElement) { Attribute = new Variation("ReadEndElement") });
            }


            // for function LookupNamespace
            {
                this.AddChild(new CVariation(LookupNamespace) { Attribute = new Variation("LookupNamespace") });
            }


            // for function ResolveEntity
            {
                this.AddChild(new CVariation(ResolveEntity) { Attribute = new Variation("ResolveEntity") });
            }


            // for function ReadAttributeValue
            {
                this.AddChild(new CVariation(ReadAttributeValue) { Attribute = new Variation("ReadAttributeValue") });
            }


            // for function CloseTest
            {
                this.AddChild(new CVariation(CloseTest) { Attribute = new Variation("Close") });
            }
        }
    }
}
