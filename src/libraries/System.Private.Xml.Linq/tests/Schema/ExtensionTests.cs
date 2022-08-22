// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// ExtensionsTest.cs - Unit Test Cases for Extensions.cs class under
// System.Xml.Schema namespace found in System.Xml.Linq assembly
// (System.Xml.Linq.dll)
//
// Author:
//      Stefan Prutianu (stefanprutianu@yahoo.com)
//
// (C) Stefan Prutianu
//

using System;
using System.Xml;
using System.IO;
using Network =  System.Net;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;
using ExtensionsClass = System.Xml.Schema.Extensions;
using Xunit;

namespace CoreXml.Test.XLinq
{
    public class SchemaExtensionsTests
    {
        public static string xsdString;
        public static XmlSchemaSet schemaSet;
        public static string xmlString;
        public static XDocument xmlDocument;
        public static bool validationSucceeded;

        // initialize values for tests
        public SchemaExtensionsTests()
        {
            xsdString = @"<?xml version='1.0'?>
                   <xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                      <xs:element name='note'>
                       <xs:complexType>
                     <xs:sequence>
                       <xs:element name='to' type='xs:string'/>
                       <xs:element name='from' type='xs:string'/>
                       <xs:element name='heading' type='xs:string'/>
                       <xs:element name='ps' type='xs:string'
                        maxOccurs='1' minOccurs = '0' default='Bye!'/>
                       <xs:element name='body'>
                          <xs:simpleType>
                         <xs:restriction base='xs:string'>
                            <xs:minLength value='5'/>
                            <xs:maxLength value='30'/>
                         </xs:restriction>
                          </xs:simpleType>
                       </xs:element>
                     </xs:sequence>
                     <xs:attribute name='subject' type='xs:string'
                       default='mono'/>
                     <xs:attribute name='date' type='xs:date'
                       use='required'/>
                       </xs:complexType>
                     </xs:element>
                   </xs:schema>";
            schemaSet = new XmlSchemaSet();
            schemaSet.Add("", XmlReader.Create(new StringReader(xsdString)));

            xmlString = @"<?xml version='1.0'?>
                      <note date ='2010-05-26'>
                     <to>Tove</to>
                     <from>Jani</from>
                     <heading>Reminder</heading>
                     <body>Don't forget to call me!</body>
                      </note>";
            xmlDocument = XDocument.Load(new StringReader(xmlString));
            validationSucceeded = false;

            /*
             * Use this method to load the above data from disk
             * Comment the above code when using this method.
             * Also the arguments for the following tests: XAttributeSuccessValidate
             * XAttributeFailValidate, XAttributeThrowExceptionValidate, XElementSuccessValidate
             * XElementFailValidate,XElementThrowExceptionValidate, XAttributeGetSchemaInfo
             * XElementGetSchemaInfo may need to be changed.
             */
            //LoadOutsideDocuments("c:\\note.xsd", "c:\\note.xml");
        }

        // this gets called when a validation error occurs
        private void TestValidationHandler(object sender, ValidationEventArgs e)
        {
            validationSucceeded = false;
        }

        // test successful validation
        [Fact]
        public void XDocumentSuccessValidate()
        {
            validationSucceeded = true;
            ExtensionsClass.Validate(xmlDocument, schemaSet,
                new ValidationEventHandler(TestValidationHandler));
            Assert.True(validationSucceeded);
        }

        // test failed validation
        [Fact]
        public void XDocumentFailValidate()
        {
            string elementName = "AlteringElementName";
            object elementValue = "AlteringElementValue";

            // alter XML document to invalidate
            var newElement = new XElement(elementName, elementValue);
            xmlDocument.Root.Add(newElement);

            validationSucceeded = true;
            ExtensionsClass.Validate(xmlDocument, schemaSet,
                new ValidationEventHandler(TestValidationHandler));
            Assert.False(validationSucceeded);
        }

        /*
         * test if exception is thrown when xml document does not
         * conform to the xml schema
         */
        [Fact]
        public void XDocumentThrowExceptionValidate()
        {
            string elementName = "AlteringElementName";
            object elementValue = "AlteringElementValue";

            // alter XML document to invalidate
            var newElement = new XElement(elementName, elementValue);
            xmlDocument.Root.Add(newElement);

            Assert.Throws<XmlSchemaValidationException>(() => ExtensionsClass.Validate(xmlDocument, schemaSet, null));
        }

        /*
         * test xml validation populating the XML tree with
         * the post-schema-validation infoset(PSVI)
         */
        [Fact]
        public void XDocumentAddSchemaInfoValidate()
        {
            // no. of elements before validation
            IEnumerable<XElement> elements = xmlDocument.Elements();
            IEnumerator<XElement> elementsEnumerator = elements.GetEnumerator();
            int beforeNoOfElements = 0;
            int beforeNoOfAttributes = 0;
            while (elementsEnumerator.MoveNext())
            {
                beforeNoOfElements++;
                if (!elementsEnumerator.Current.HasAttributes)
                    continue;

                IEnumerable<XAttribute> attributes = elementsEnumerator.Current.Attributes();
                IEnumerator<XAttribute> attributesEnumerator = attributes.GetEnumerator();
                while (attributesEnumerator.MoveNext())
                    beforeNoOfAttributes++;
            }

            // populate XML with PSVI
            validationSucceeded = true;
            ExtensionsClass.Validate(xmlDocument, schemaSet,
                new ValidationEventHandler(TestValidationHandler), true);
            Assert.True(validationSucceeded);

            // no. of elements after validation
            elements = xmlDocument.Elements();
            elementsEnumerator = elements.GetEnumerator();
            int afterNoOfElements = 0;
            int afterNoOfAttributes = 0;
            while (elementsEnumerator.MoveNext())
            {
                afterNoOfElements++;
                if (!elementsEnumerator.Current.HasAttributes)
                    continue;

                IEnumerable<XAttribute> attributes = elementsEnumerator.Current.Attributes();
                IEnumerator<XAttribute> attributesEnumerator = attributes.GetEnumerator();
                while (attributesEnumerator.MoveNext())
                    afterNoOfAttributes++;
            }

            Assert.True(afterNoOfAttributes >= beforeNoOfAttributes, "XDocumentAddSchemaInfoValidate, wrong newAttributes value.");
            Assert.True(afterNoOfElements >= beforeNoOfElements, "XDocumentAddSchemaInfoValidate, wrong newElements value.");
        }

        /*
         * test xml validation without populating the XML tree with
         * the post-schema-validation infoset(PSVI).
         */
        [Fact]
        public void XDocumentNoSchemaInfoValidate()
        {
            // no. of elements before validation
            IEnumerable<XElement> elements = xmlDocument.Elements();
            IEnumerator<XElement> elementsEnumerator = elements.GetEnumerator();
            int beforeNoOfElements = 0;
            int beforeNoOfAttributes = 0;
            while (elementsEnumerator.MoveNext())
            {
                beforeNoOfElements++;
                if (!elementsEnumerator.Current.HasAttributes)
                    continue;

                IEnumerable<XAttribute> attributes = elementsEnumerator.Current.Attributes();
                IEnumerator<XAttribute> attributesEnumerator = attributes.GetEnumerator();
                while (attributesEnumerator.MoveNext())
                   beforeNoOfAttributes++;
            }

            // don't populate XML with PSVI
            validationSucceeded = true;
            ExtensionsClass.Validate(xmlDocument, schemaSet,
                new ValidationEventHandler(TestValidationHandler), false);
            Assert.True(validationSucceeded);

            // no. of elements after validation
            elements = xmlDocument.Elements();
            elementsEnumerator = elements.GetEnumerator();
            int afterNoOfElements = 0;
            int afterNoOfAttributes = 0;
            while (elementsEnumerator.MoveNext())
            {
                afterNoOfElements++;
                if (!elementsEnumerator.Current.HasAttributes)
                    continue;

                IEnumerable<XAttribute> attributes = elementsEnumerator.Current.Attributes();
                IEnumerator<XAttribute> attributesEnumerator = attributes.GetEnumerator();
                while (attributesEnumerator.MoveNext())
                    afterNoOfAttributes++;
            }

            Assert.Equal(afterNoOfAttributes, beforeNoOfAttributes);
            Assert.Equal(afterNoOfElements, beforeNoOfElements);
        }

        // attribute validation succeeds after change
        [Fact]
        public void XAttributeSuccessValidate()
        {
            string elementName = "note";
            string attributeName = "date";
            object attributeValue = "2010-05-27";

            // validate the entire document
            validationSucceeded = true;
            ExtensionsClass.Validate(xmlDocument, schemaSet,
                new ValidationEventHandler(TestValidationHandler), true);
            Assert.True(validationSucceeded);

            // change and re-validate attribute value
            XAttribute date = xmlDocument.Element(elementName).Attribute(attributeName);
            date.SetValue(attributeValue);
            ExtensionsClass.Validate(date, date.GetSchemaInfo().SchemaAttribute,schemaSet,
                new ValidationEventHandler(TestValidationHandler));
            Assert.True(validationSucceeded);
        }

        // attribute validation fails after change
        [Fact]
        public void XAttributeFailValidate()
        {
            string elementName = "note";
            string attributeName = "date";
            object attributeValue = "2010-12-32";

            // validate the entire document
            validationSucceeded = true;
            ExtensionsClass.Validate(xmlDocument, schemaSet,
                new ValidationEventHandler(TestValidationHandler),true);
            Assert.True(validationSucceeded);

            // change and re-validate attribute value
            XAttribute date = xmlDocument.Element(elementName).Attribute(attributeName);
            date.SetValue(attributeValue);
            ExtensionsClass.Validate(date, date.GetSchemaInfo().SchemaAttribute, schemaSet,
                new ValidationEventHandler(TestValidationHandler));
            Assert.False(validationSucceeded);
        }

        /*
         * test if exception is thrown when xml document does not
         * conform to the xml schema after attribute value change
         */
        [Fact]
        public void XAttributeThrowExceptionValidate()
        {
            string elementName = "note";
            string attributeName =  "date";
            object attributeValue =  "2010-12-32";

            // validate the entire document
            validationSucceeded = true;
            ExtensionsClass.Validate(xmlDocument, schemaSet,
                new ValidationEventHandler(TestValidationHandler),true);
            Assert.True(validationSucceeded);

            // change and re-validate attribute value
            XAttribute date = xmlDocument.Element(elementName).Attribute(attributeName);
            date.SetValue(attributeValue);
            Assert.Throws<XmlSchemaValidationException>(() => ExtensionsClass.Validate(date, date.GetSchemaInfo().SchemaAttribute, schemaSet, null));
        }

        // element validation succeeds after change
        [Fact]
        public void XElementSuccessValidate()
        {
            string parentElementName = "note";
            string childElementName = "body";
            object childElementValue = "Please call me!";

            // validate the entire document
            validationSucceeded = true;
            ExtensionsClass.Validate(xmlDocument, schemaSet,
                new ValidationEventHandler(TestValidationHandler), true);
            Assert.True(validationSucceeded);

            // alter element
            XElement root = xmlDocument.Element(parentElementName);
            root.SetElementValue(childElementName, childElementValue);

            ExtensionsClass.Validate(root, root.GetSchemaInfo().SchemaElement, schemaSet,
                new ValidationEventHandler(TestValidationHandler));
            Assert.True(validationSucceeded);
        }

        // element validation fails after change
        [Fact]
        public void XElementFailValidate()
        {
            string parentElementName = "note";
            string childElementName = "body";
            object childElementValue = "Don't forget to call me! Please!";

            // validate the entire document
            validationSucceeded = true;
            ExtensionsClass.Validate(xmlDocument, schemaSet,
                new ValidationEventHandler(TestValidationHandler), true);
            Assert.True(validationSucceeded);

            // alter element
            XElement root = xmlDocument.Element(parentElementName);
            root.SetElementValue(childElementName, childElementValue);

            ExtensionsClass.Validate(root, root.GetSchemaInfo().SchemaElement, schemaSet,
                new ValidationEventHandler(TestValidationHandler));
            Assert.False(validationSucceeded);

        }

        /*
         * test if exception is thrown when xml document does not
         * conform to the xml schema after element value change
         */
        [Fact]
        public void XElementThrowExceptionValidate()
        {
            string parentElementName = "note" ;
            string childElementName = "body";
            object childElementValue = "Don't forget to call me! Please!";

            // validate the entire document
            validationSucceeded = true;
            ExtensionsClass.Validate(xmlDocument, schemaSet,
                new ValidationEventHandler(TestValidationHandler), true);
            Assert.True(validationSucceeded);

            // alter element
            XElement root = xmlDocument.Element(parentElementName);
            root.SetElementValue(childElementName, childElementValue);

            Assert.Throws<XmlSchemaValidationException>(() => ExtensionsClass.Validate(root, root.GetSchemaInfo().SchemaElement, schemaSet, null));
        }

        // test attribute schema info
        [Fact]
        public void XAttributeGetSchemaInfo()
        {
            string elementName =  "note";
            string attributeName = "date";

            // validate the entire document
            validationSucceeded = true;
            ExtensionsClass.Validate(xmlDocument, schemaSet,
                new ValidationEventHandler(TestValidationHandler), true);
            Assert.True(validationSucceeded);

            // validate attribute
            XAttribute date = xmlDocument.Element(elementName).Attribute(attributeName);
            ExtensionsClass.Validate(date, date.GetSchemaInfo().SchemaAttribute, schemaSet,
                new ValidationEventHandler(TestValidationHandler));
            Assert.True(validationSucceeded);

            IXmlSchemaInfo schemaInfo =  ExtensionsClass.GetSchemaInfo(date);
            Assert.NotNull(schemaInfo);
        }

        // test element schema info
        [Fact]
        public void XElementGetSchemaInfo()
        {
            string elementName = "body";

            // validate the entire document
            validationSucceeded = true;
            ExtensionsClass.Validate(xmlDocument, schemaSet,
                new ValidationEventHandler(TestValidationHandler), true);
            Assert.True(validationSucceeded);

            // validate element
            XElement body = xmlDocument.Root.Element(elementName);
            ExtensionsClass.Validate(body, body.GetSchemaInfo().SchemaElement, schemaSet,
                new ValidationEventHandler(TestValidationHandler));
            Assert.True(validationSucceeded);

            IXmlSchemaInfo schemaInfo = ExtensionsClass.GetSchemaInfo(body);
            Assert.NotNull(schemaInfo);
        }
    }
}
