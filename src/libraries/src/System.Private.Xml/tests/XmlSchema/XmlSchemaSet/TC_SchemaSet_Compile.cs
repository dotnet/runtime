// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_Compile", Desc = "", Priority = 0)]
    public class TC_SchemaSet_Compile
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_Compile(ITestOutputHelper output)
        {
            _output = output;
        }


        public bool bWarningCallback;
        public bool bErrorCallback;
        public int errorCount;
        public int warningCount;
        public bool WarningInnerExceptionSet = false;
        public bool ErrorInnerExceptionSet = false;

        public void Initialize()
        {
            bWarningCallback = bErrorCallback = false;
            errorCount = warningCount = 0;
            WarningInnerExceptionSet = ErrorInnerExceptionSet = false;
        }

        //hook up validaton callback
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

            _output.WriteLine(args.Message); // Print the error to the screen.
        }

        [Fact]
        //[Variation(Desc = "v1 - Compile on empty collection")]
        public void v1()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.Compile();
            return;
        }

        [Fact]
        //[Variation(Desc = "v2 - Compile after error in Add")]
        public void v2()
        {
            XmlSchemaSet sc = new XmlSchemaSet();

            try
            {
                sc.Add(null, Path.Combine(TestData._Root, "schema1.xdr"));
            }
            catch (XmlSchemaException)
            {
                sc.Compile();
                // GLOBALIZATION
                return;
            }
            Assert.True(false);
        }

        [Fact]
        //[Variation(Desc = "TFS_470021 Unexpected local particle qualified name when chameleon schema is added to set")]
        public void TFS_470021()
        {
            string cham = @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema id='a0'
                  elementFormDefault='qualified'
                  xmlns:xs='http://www.w3.org/2001/XMLSchema'>
  <xs:complexType name='ctseq1_a'>
    <xs:sequence>
      <xs:element name='foo'/>
    </xs:sequence>
    <xs:attribute name='abt0' type='xs:string'/>
  </xs:complexType>
  <xs:element name='gect1_a' type ='ctseq1_a'/>
</xs:schema>";
            string main = @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema id='m0'
                  targetNamespace='http://tempuri.org/chameleon1'
                  elementFormDefault='qualified'
                  xmlns='http://tempuri.org/chameleon1'
                  xmlns:mstns='http://tempuri.org/chameleon1'
                  xmlns:xs='http://www.w3.org/2001/XMLSchema'>
  <xs:include schemaLocation='cham.xsd' />

  <xs:element name='root'>
    <xs:complexType>
      <xs:sequence maxOccurs='unbounded'>
        <xs:any namespace='##any' processContents='lax'/>
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>";
            using (XmlWriter w = XmlWriter.Create("cham.xsd"))
            {
                using (XmlReader r = XmlReader.Create(new StringReader(cham)))
                    w.WriteNode(r, true);
            }
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);

            ss.Add(null, XmlReader.Create(new StringReader(cham)));
            ss.Add(null, XmlReader.Create(new StringReader(main)));
            ss.Compile();

            Assert.Equal(ss.Count, 2);
            foreach (XmlSchemaElement e in ss.GlobalElements.Values)
            {
                _output.WriteLine(e.QualifiedName.ToString());
                XmlSchemaComplexType type = e.ElementSchemaType as XmlSchemaComplexType;
                XmlSchemaSequence seq = type.ContentTypeParticle as XmlSchemaSequence;
                foreach (XmlSchemaObject child in seq.Items)
                {
                    if (child is XmlSchemaElement)
                        _output.WriteLine("\t" + (child as XmlSchemaElement).QualifiedName);
                }
            }
            Assert.Equal(warningCount, 0);
            Assert.Equal(errorCount, 0);
            return;
        }
    }
}
