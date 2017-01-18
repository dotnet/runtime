// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    public class CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public CXmlSchemaValidatorTestCase(ITestOutputHelper output)
        {
            _output = output;
        }

        private string m_TestData = Path.Combine(FilePathUtil.GetTestDataPath(), "XmlSchemaValidatorAPI");

        public string TestData
        {
            get
            {
                return m_TestData;
            }
        }

        // Schemas Filenames

        protected const string XSDFILE_200_DEF_ATTRIBUTES = "200DefAttributes.xsd";
        protected const string XSDFILE_IDENTITY_CONSTRAINS = "IdentityConstrains.xsd";
        protected const string XSDFILE_GET_EXPECTED_ATTRIBUTES = "GetExpectedAttributes.xsd";
        protected const string XSDFILE_GET_EXPECTED_PARTICLES = "GetExpectedParticles.xsd";
        protected const string XSDFILE_NO_TARGET_NAMESPACE = "NoTargetNamespaceCollection.xsd";
        protected const string XSDFILE_TARGET_NAMESPACE = "TargetNamespaceCollection.xsd";
        protected const string XSDFILE_PARTIAL_VALIDATION = "PartialValidation.xsd";
        protected const string XSDFILE_VALIDATE_ATTRIBUTE = "ValidateAttribute.xsd";
        protected const string XSDFILE_VALIDATE_END_ELEMENT = "ValidateEndElement.xsd";
        protected const string XSDFILE_VALIDATE_TEXT = "ValidateText.xsd";

        protected const XmlSchemaValidationFlags AllFlags = XmlSchemaValidationFlags.ReportValidationWarnings |
                                                             XmlSchemaValidationFlags.ProcessInlineSchema |
                                                             XmlSchemaValidationFlags.ProcessSchemaLocation |
                                                             XmlSchemaValidationFlags.ProcessIdentityConstraints;

        // ========== CreateValidator ==========

        protected XmlSchemaValidator CreateValidator(XmlSchemaSet schemas, XmlSchemaValidationFlags flags)
        {
            return new XmlSchemaValidator(schemas.NameTable, schemas, new XmlNamespaceManager(schemas.NameTable), flags);
        }

        protected XmlSchemaValidator CreateValidator(XmlSchemaSet schemas, IXmlNamespaceResolver nsRes, XmlSchemaValidationFlags flags)
        {
            return new XmlSchemaValidator(schemas.NameTable, schemas, nsRes, flags);
        }

        protected XmlSchemaValidator CreateValidator(XmlSchemaSet schemas)
        {
            return CreateValidator(schemas, AllFlags);
        }

        protected XmlSchemaValidator CreateValidator(string xsdFilename, string targetNamespace, XmlSchemaValidationFlags flags)
        {
            string path = xsdFilename;
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.XmlResolver = new XmlUrlResolver();

            if (!path.StartsWith("file://") && !path.StartsWith("http://"))
                path = Path.Combine(this.TestData, path);

            schemas.Add(targetNamespace, path);
            schemas.Compile();
            return CreateValidator(schemas, flags);
        }

        protected XmlSchemaValidator CreateValidator(string xsdFilename, string targetNamespace)
        {
            return CreateValidator(xsdFilename, targetNamespace, AllFlags);
        }

        protected XmlSchemaValidator CreateValidator(string xsdFilename)
        {
            return CreateValidator(xsdFilename, "", AllFlags);
        }

        // ========== CreateSchemaSet ===========

        protected XmlSchemaSet CreateSchemaSet(string targetNamespace, string XsdContent)
        {
            StringReader s = new StringReader(XsdContent);
            XmlSchemaSet schemas = new XmlSchemaSet();

            schemas.Add(targetNamespace, XmlReader.Create(s));

            return schemas;
        }

        protected XmlSchemaSet CreateSchemaSetFromXml(string XmlContent)
        {
            XmlSchemaInference infer = new XmlSchemaInference();
            StringReader s = new StringReader(XmlContent);

            return infer.InferSchema(XmlReader.Create(s));
        }

        // ========== Getters ==========

        private string stringGetterContent;

        private string stringGetterHandle()
        {
            return stringGetterContent;
        }

        public XmlValueGetter StringGetter(string resStr)
        {
            stringGetterContent = resStr;
            return new XmlValueGetter(stringGetterHandle);
        }

        public bool bWarningCallback = false;
        public bool bErrorCallback = false;
        public int errorCount = 0;
        public int warningCount = 0;
        public bool WarningInnerExceptionSet = false;
        public bool ErrorInnerExceptionSet = false;

        public void Initialize()
        {
            bWarningCallback = bErrorCallback = false;
            errorCount = warningCount = 0;
            WarningInnerExceptionSet = ErrorInnerExceptionSet = false;
        }

        public void ValidationCallback(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
            {
                _output.WriteLine("WARNING: ");
                bWarningCallback = true;
                warningCount++;
                WarningInnerExceptionSet = (args.Exception.InnerException != null);
                _output.WriteLine("\nInnerExceptionSet : " + WarningInnerExceptionSet + "\n");
            }
            else if (args.Severity == XmlSeverityType.Error)
            {
                _output.WriteLine("ERROR: ");
                bErrorCallback = true;
                errorCount++;
                ErrorInnerExceptionSet = (args.Exception.InnerException != null);
                _output.WriteLine("\nInnerExceptionSet : " + ErrorInnerExceptionSet + "\n");
            }
            _output.WriteLine(args.Message);
        }

        public void ValidateWithSchemaInfo(XmlSchemaSet ss)
        {
            string ns = "http://tempuri.org/XMLSchema.xsd";
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(ss.NameTable);
            XmlSchemaValidator val = new XmlSchemaValidator(ss.NameTable, ss, nsmgr, XmlSchemaValidationFlags.ProcessIdentityConstraints);
            val.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            val.Initialize();
            XmlSchemaInfo si = new XmlSchemaInfo();
            val.ValidateElement("root", ns, si);
            val.ValidateEndOfAttributes(si);
            val.ValidateElement("B", ns, si);
            val.ValidateEndOfAttributes(si);

            val.ValidateElement("part", ns, si);
            val.ValidateEndOfAttributes(si);
            val.ValidateText("1");
            val.ValidateEndElement(si);

            val.ValidateElement("part", ns, si);
            val.ValidateEndOfAttributes(si);
            val.ValidateEndElement(si, "1");

            val.ValidateElement("part", ns, si);
            val.ValidateEndOfAttributes(si);
            val.ValidateText("1");
            val.ValidateEndElement(si);

            val.ValidateEndElement(si);
            val.ValidateEndElement(si);

            Assert.Equal(warningCount, 0);
            Assert.Equal(errorCount, 0);
        }

        public void ValidateWithXmlReader(XmlSchemaSet schemas, string xml, string xsd)
        {
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(new NameTable());
            XmlSchemaValidationFlags validationFlags = XmlSchemaValidationFlags.ProcessIdentityConstraints |
            XmlSchemaValidationFlags.AllowXmlAttributes;
            XmlSchemaValidator validator = new XmlSchemaValidator(namespaceManager.NameTable, schemas, namespaceManager, validationFlags);
            validator.Initialize();
            using (XmlReader r = XmlReader.Create(xsd))
            {
                while (r.Read())
                {
                    switch (r.NodeType)
                    {
                        case XmlNodeType.Element:
                            namespaceManager.PushScope();
                            if (r.MoveToFirstAttribute())
                            {
                                do
                                {
                                    if (r.NamespaceURI == "http://www.w3.org/2000/xmlns/")
                                    {
                                        namespaceManager.AddNamespace(r.LocalName, r.Value);
                                    }
                                } while (r.MoveToNextAttribute());
                                r.MoveToElement();
                            }
                            validator.ValidateElement(r.LocalName, r.NamespaceURI, null, null, null, null, null);
                            if (r.MoveToFirstAttribute())
                            {
                                do
                                {
                                    if (r.NamespaceURI != "http://www.w3.org/2000/xmlns/")
                                    {
                                        validator.ValidateAttribute(r.LocalName, r.NamespaceURI, r.Value, null);
                                    }
                                } while (r.MoveToNextAttribute());
                                r.MoveToElement();
                            }
                            validator.ValidateEndOfAttributes(null);
                            if (r.IsEmptyElement) goto case XmlNodeType.EndElement;
                            break;

                        case XmlNodeType.EndElement:
                            validator.ValidateEndElement(null);
                            namespaceManager.PopScope();
                            break;

                        case XmlNodeType.Text:
                            validator.ValidateText(r.Value);
                            break;

                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.Whitespace:
                            validator.ValidateWhitespace(r.Value);
                            break;

                        default:
                            break;
                    }
                }
                validator.EndValidation();
            }
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            rs.ValidationType = ValidationType.Schema;
            rs.Schemas.XmlResolver = new XmlUrlResolver();
            rs.Schemas.Add(null, XmlReader.Create(xsd));

            using (XmlReader r = XmlReader.Create(xml, rs))
            {
                while (r.Read()) ;
            }
            Assert.Equal(warningCount, 0);
            Assert.Equal(errorCount, 0);
        }

        public void ValidateSchemaSet(XmlSchemaSet ss, int schCount, bool isCompiled, int countGT, int countGE, int countGA, string str)
        {
            _output.WriteLine(str);
            Assert.Equal(ss.Count, schCount);
            Assert.Equal(ss.IsCompiled, isCompiled);
            Assert.Equal(ss.GlobalTypes.Count, countGT);
            Assert.Equal(ss.GlobalElements.Count, countGE);
            Assert.Equal(ss.GlobalAttributes.Count, countGA);
        }
    }
}
