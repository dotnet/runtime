// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Text;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_Compile", Desc = "", Priority = 0)]
    public class TC_SchemaSet_Compile : TC_SchemaSetBase
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

        //hook up validaton callback
        private void ValidationCallback(object sender, ValidationEventArgs args)
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

            using (var tempDirectory = new TempDirectory())
            {
                string chamPath = Path.Combine(tempDirectory.Path, "cham.xsd");
                string tempDirectoryPath = tempDirectory.Path[tempDirectory.Path.Length - 1] == Path.DirectorySeparatorChar ?
                    tempDirectory.Path :
                    tempDirectory.Path + Path.DirectorySeparatorChar;

                using (XmlWriter w = XmlWriter.Create(chamPath))
                {
                    using (XmlReader r = XmlReader.Create(new StringReader(cham)))
                        w.WriteNode(r, true);
                }
                XmlSchemaSet ss = new XmlSchemaSet();
                ss.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);

                ss.Add(null, XmlReader.Create(new StringReader(cham)));
                // TempDirectory path must end with a DirectorySeratorChar, otherwise it will throw in the Xml validation.
                var settings = new XmlReaderSettings() { XmlResolver = new XmlUrlResolver() };
                ss.Add(null, XmlReader.Create(new StringReader(main), settings, tempDirectoryPath));
                ss.Compile();

                Assert.Equal(2, ss.Count);
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
                Assert.Equal(0, warningCount);
                Assert.Equal(0, errorCount);
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void FractionDigitsMismatch_Throws()
        {
            string schema = @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:decimal'>
            <xs:fractionDigits value='8'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:fractionDigits value='9'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
";

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(null, XmlReader.Create(new StringReader(schema)));

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());
            Assert.Contains("fractionDigits", ex.Message);
            Assert.DoesNotContain("totalDigits", ex.Message);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void FractionDigitsFacetBaseFixed_Throws()
        {
            string schema = @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:decimal'>
            <xs:fractionDigits value='8' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:fractionDigits value='7'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
";
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(null, XmlReader.Create(new StringReader(schema)));

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());
            Assert.Contains("fixed", ex.Message);
        }
        
        [Fact]
        public void MinLengthLtBaseMinLength_Throws()
        {
            string schema = @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:minLength value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:minLength value='4'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
";

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(null, XmlReader.Create(new StringReader(schema)));

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());
            Assert.Contains("minLength", ex.Message);
        }

        [Fact]
        public void MaxLengthGtBaseMaxLength_Throws()
        {
            string schema = @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:maxLength value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:maxLength value='6'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
";

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(null, XmlReader.Create(new StringReader(schema)));

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());
            Assert.Contains("maxLength", ex.Message);
        }

        #region "Testing presence of minLength or maxLength and Length"

        public static IEnumerable<object[]> MaxMinLengthBaseLength_ThrowsData
        {
            get
            {
                return new List<object[]>()
                {
                    new object[]
                    {  // minLength and length specified in same derivation step.
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:minLength value='5'/>
            <xs:length value='5' />
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // maxLength and length specified in same derivation step.
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:maxLength value='5'/>
            <xs:length value='5' />
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // base type has minLength; derived type has lesser length
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:minLength value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:length value='4'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // base type has maxLength; derived type has greater length
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:maxLength value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:length value='6'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // base type has length; derived type has lesser maxLength
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:length value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:maxLength value='4'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // base type has length; derived type has greater minLength
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:length value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:minLength value='6'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // base type has maxLength; derived type has greater length
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:maxLength value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:length value='6'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(MaxMinLengthBaseLength_ThrowsData))]
        public void MaxMinLengthBaseLength_Throws(string schema)
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(null, XmlReader.Create(new StringReader(schema)));

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());

            Assert.Contains("length", ex.Message);
            Assert.Contains("minLength", ex.Message);
            Assert.Contains("maxLength", ex.Message);
        }

        [Fact]
        public void MinLengthGtMaxLength_Throws()
        {
            string schema = @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:maxLength value='5'/>
            <xs:minLength value='8'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
";
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(null, XmlReader.Create(new StringReader(schema)));

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());

            Assert.Contains("minLength", ex.Message);
            Assert.Contains("maxLength", ex.Message);
        }

        public static IEnumerable<object[]> MaxMinLengthBaseLength_Success_TestData
        {
            get
            {
                return new List<object[]>()
                {
                    new object[]
                    {  // base type has length; derived type has equal maxLength
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:length value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:maxLength value='5'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // base type has length; derived type has greater maxLength
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:length value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:maxLength value='6'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // base type has length; derived type has equal minLength
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:length value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:minLength value='5'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // base type has length; derived type has lesser minLength
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:length value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:minLength value='4'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // base type has minLength; derived type has equal length
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:minLength value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:length value='5'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // base type has minLength; derived type has greater length
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:minLength value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:length value='6'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // base type has maxLength; derived type has equal length
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:maxLength value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:length value='5'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // base type has maxLength; derived type has lesser length
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:maxLength value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:length value='4'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // minLength is equal to maxLength
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:maxLength value='5'/>
            <xs:minLength value='5'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // minLength is less than maxLength
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:maxLength value='8'/>
            <xs:minLength value='5'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(MaxMinLengthBaseLength_Success_TestData))]
        public void MaxMinLengthBaseLength_Test(string schema)
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(null, XmlReader.Create(new StringReader(schema)));

            ss.Compile();

            Assert.True(true);
        }
        #endregion

        [Fact]
        public void LengthGtBaseLength_Throws()
        {
            string schema = @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:length value='5'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:length value='6'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
";

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(null, XmlReader.Create(new StringReader(schema)));

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());

            Assert.Contains("length", ex.Message);
        }

        #region Complex Restricton tests

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void SequenceRestrictsChoiceValid()
        {
            string schema = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' 
		   targetNamespace='urn:gba:sqg' xmlns:ns1='urn:gba:sqg'
		   elementFormDefault='qualified' attributeFormDefault='unqualified'>
    <xs:complexType name='base' abstract='true'>
      <xs:choice minOccurs='2' maxOccurs='unbounded'>
          <xs:element name='a'/>
          <xs:element name='b'/>
          <xs:element name='c'/>
       </xs:choice>
    </xs:complexType>
	<xs:complexType name='derived'>
		<xs:complexContent>
			<xs:restriction base='ns1:base'>
				<xs:sequence>
                    <xs:element name='b'/>
                    <xs:element name='a'/>
					<xs:element name='c'/>
					<xs:element name='c'/>
                    <xs:element name='a'/>
				</xs:sequence>
			</xs:restriction>
		</xs:complexContent>
	</xs:complexType>
	<xs:element name='root' type='ns1:derived'>
	</xs:element>
</xs:schema>
";
            var xr = XmlReader.Create(new StringReader(schema));
            var ss = new XmlSchemaSet();
            ss.Add("urn:gba:sqg", xr);
            ss.Compile();
        }


        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void SequenceRestrictsChoiceComplexButValid()
        {
            string schema = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:ns1='urn:gba:sqg'
           xmlns:xenc='http://www.w3.org/2001/04/xmlenc#'
		   xmlns:ds='http://www.w3.org/2000/09/xmldsig#'
		   xmlns:xi='urn:gba:sqg' targetNamespace='urn:gba:sqg'
		   elementFormDefault='qualified' attributeFormDefault='unqualified'>
	<xs:complexType name='base' abstract='true'>
		<xs:choice maxOccurs='unbounded'>
			<xs:sequence>
				<xs:element name='a' minOccurs='0'/>
				<xs:element name='b' minOccurs='0'/>
			</xs:sequence>
			<xs:sequence>
				<xs:element name='c' minOccurs='0'/>
				<xs:element name='d' minOccurs='0'/>
				<xs:element name='e' minOccurs='0'/>
			</xs:sequence>
		</xs:choice>
	</xs:complexType>
	<xs:complexType name='derived'>
		<xs:complexContent>
			<xs:restriction base='ns1:base'>
				<xs:sequence>
					<xs:element name='c'/>
					<xs:element name='d' minOccurs='0'/>
					<xs:element name='e' minOccurs='0'/>
				</xs:sequence>
			</xs:restriction>
		</xs:complexContent>
	</xs:complexType>
	<xs:element name='root' type='ns1:derived'>
	</xs:element>
</xs:schema>
";
            var xr = XmlReader.Create(new StringReader(schema));
            var ss = new XmlSchemaSet();
            ss.Add("urn:gba:sqg", xr);
            ss.Compile();
        }

        [Fact]
        public void SequenceRestrictsChoiceInvalid()
        {
            // particle "f" in derrived type has no mapping to any particle in the base type.
            string schema = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' 
		   targetNamespace='urn:gba:sqg' xmlns:ns1='urn:gba:sqg'
		   elementFormDefault='qualified' attributeFormDefault='unqualified'>
	<xs:complexType name='base' abstract='true'>
		<xs:choice maxOccurs='unbounded'>
			<xs:sequence>
				<xs:element name='a' minOccurs='0'/>
				<xs:element name='b' minOccurs='0'/>
			</xs:sequence>
			<xs:sequence>
				<xs:element name='c' minOccurs='0'/>
				<xs:element name='d' minOccurs='0'/>
				<xs:element name='e' minOccurs='0'/>
			</xs:sequence>
		</xs:choice>
	</xs:complexType>
	<xs:complexType name='derived'>
		<xs:complexContent>
			<xs:restriction base='ns1:base'>
				<xs:sequence>
					<xs:element name='a'/>
					<xs:element name='c' minOccurs='0'/>
					<xs:element name='e' minOccurs='0'/>
					<xs:element name='f' minOccurs='0'/>
				</xs:sequence>
			</xs:restriction>
		</xs:complexContent>
	</xs:complexType>
	<xs:element name='root' type='ns1:derived'>
	</xs:element>
</xs:schema>
";
            var xr = XmlReader.Create(new StringReader(schema));
            var ss = new XmlSchemaSet();
            ss.Add("urn:gba:sqg", xr);

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());

            Assert.Contains("Invalid particle derivation by restriction", ex.Message);
        }
        #endregion

        #region FacetBaseFixed tests
        public static IEnumerable<object[]> FacetBaseFixed_Throws_TestData
        {
            get{
                return new List<object[]>()
                {
                    new object[]
                    {  // length, derived type has larger value.
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:length value='5' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:length value='6'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // length, derived type has smaller value
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:length value='5' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:length value='4'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // minLength, derived type has larger value.
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:minLength value='5' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:minLength value='6'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // minLength, derived type has smaller value.
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:minLength value='5' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:minLength value='4'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // maxLength, derived type has lower value.
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:maxLength value='5' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:maxLength value='6'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // maxLength, derived type has larger value.
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:maxLength value='5' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:maxLength value='4'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // whiteSpace
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:string'>
            <xs:whiteSpace value='collapse' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:whiteSpace value='replace'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // maxInclusive, derived type with larger value
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:integer'>
            <xs:maxInclusive value='18' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:maxInclusive value='19'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // maxInclusive, derived type with smaller value
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:integer'>
            <xs:maxInclusive value='18' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:maxInclusive value='17'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // maxExclusive, derived type has larger value
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:integer'>
            <xs:maxExclusive value='19' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:maxExclusive value='20'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // maxExclusive, derived type has smaller value
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:integer'>
            <xs:maxExclusive value='19' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:maxExclusive value='18'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // minExclusive, derived type has larger value
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:integer'>
            <xs:minExclusive value='2' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:minExclusive value='3'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // minExclusive, derived type has smaller value
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:integer'>
            <xs:minExclusive value='2' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:minExclusive value='1'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // minInclusive, derived type has larger value
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:integer'>
            <xs:minInclusive value='3' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:minInclusive value='4'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {  // minInclusive, derived type has smaller value
                        @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:integer'>
            <xs:minInclusive value='3' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:minInclusive value='2'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(FacetBaseFixed_Throws_TestData))]
        public void FacetBaseFixed_Throws(string schema)
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(null, XmlReader.Create(new StringReader(schema)));

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());
            Assert.Contains("fixed='true'", ex.Message);
        }
        #endregion

        [Fact]
        public void InvalidAllMax_Throws()
        {
            string schema = @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:complexType name='person'>
        <xs:all maxOccurs='2'>
            <xs:element name='firstname'/>
            <xs:element name='lastname'/>
        </xs:all>
    </xs:complexType>
</xs:schema>
";
            XmlReader xr;
            xr = XmlReader.Create(new StringReader(schema));
            XmlSchemaSet ss = new XmlSchemaSet();

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Add(null, xr));

            Assert.Contains("all", ex.Message);
        }

        [Fact]
        public void InvalidAllElementMax_Throws()
        {
            string schema = @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:complexType name='person'>
        <xs:all>
            <xs:element name='firstname' maxOccurs='2'/>
            <xs:element name='lastname'/>
        </xs:all>
    </xs:complexType>
</xs:schema>
";
            XmlReader xr;
            xr = XmlReader.Create(new StringReader(schema));
            XmlSchemaSet ss = new XmlSchemaSet();

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Add(null, xr));

            Assert.Contains("all", ex.Message);
            Assert.Contains("maxOccurs", ex.Message);
        }

        [Fact]
        public void InvalidExemplar_Throws()
        {
            string schema = @"<?xml version='1.0' encoding='utf-8'?>
<xs:schema elementFormDefault='qualified'
            xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:complexType name = 'personType'>
         <xs:all>
             <xs:element name='firstname'/>
             <xs:element name='lastname'/>
         </xs:all>
     </xs:complexType>
     <xs:element name='person' type='personType' final='#all'/>
     <xs:element name='human' type='personType' substitutionGroup='person'/>
</xs:schema>
";

            XmlReader xr;
            xr = XmlReader.Create(new StringReader(schema));
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(null, xr);

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());

            Assert.Contains("substitutionGroup", ex.Message);
            Assert.Contains("person", ex.Message);
        }

        [Fact]
        public void GroupBaseRestNotEmptiable_Throws()
        {
            string schema = @"<?xml version='1.0' encoding='utf-8'?>
<xs:schema elementFormDefault='qualified'
            xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='component'>
        <xs:restriction base='xs:integer'>
            <xs:minInclusive value='0'/>
            <xs:maxInclusive value='255'/>
        </xs:restriction>
    </xs:simpleType>

    <xs:complexType name='alienColor'>
        <xs:sequence>
            <xs:element name='red' type='component' minOccurs='1' maxOccurs='1'/>
            <xs:element name='green' type='component' minOccurs='1' maxOccurs='1'/>
            <xs:element name='blue' type='component' minOccurs='1' maxOccurs='1'/>
            <xs:element name='ultraviolet' type='component' minOccurs='1' maxOccurs='1'/>
        </xs:sequence>
    </xs:complexType>

    <xs:complexType name='humanColor'>
        <xs:complexContent>
            <xs:restriction base='alienColor'>
                <xs:sequence>
                    <xs:element name='red' type='component' minOccurs='1' maxOccurs='1'/>
                    <xs:element name='green' type='component' minOccurs='1' maxOccurs='1'/>
                    <xs:element name='blue' type='component' minOccurs='1' maxOccurs='1'/>
                </xs:sequence>
            </xs:restriction>
        </xs:complexContent>
    </xs:complexType>
</xs:schema>
";
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(null, XmlReader.Create(new StringReader(schema)));

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());

            Assert.Contains("particle", ex.Message);
        }

        #region test throwing of XmlSchemaException with message Sch_AllRefMinMax
        public static IEnumerable<object[]> AllRefMinMax_Throws_TestData
        {
            get
            {
                return new List<object[]>()
                {
                    new object[]
                    {  // invalid value for minOccurs and maxOccurs
@"<?xml version='1.0' encoding='utf-8'?>
<xs:schema elementFormDefault='qualified'
            xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:group name='foods'>
        <xs:all>
            <xs:element name='bacon' type='xs:integer'/>
            <xs:element name='eggs' type='xs:integer'/>
            <xs:element name='coffee' type='xs:integer'/>
        </xs:all>
    </xs:group>

    <xs:complexType name='breakfast'>
        <xs:group ref='foods' minOccurs='2' maxOccurs='2'/>
    </xs:complexType>
</xs:schema>
"
                    },
                    new object[]
                    {  // maxOccurs too large
@"<?xml version='1.0' encoding='utf-8'?>
<xs:schema elementFormDefault='qualified'
            xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:group name='foods'>
        <xs:all>
            <xs:element name='bacon' type='xs:integer'/>
            <xs:element name='eggs' type='xs:integer'/>
            <xs:element name='coffee' type='xs:integer'/>
        </xs:all>
    </xs:group>

    <xs:complexType name='breakfast'>
        <xs:group ref='foods' maxOccurs='2'/>
    </xs:complexType>
</xs:schema>
"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(AllRefMinMax_Throws_TestData))]
        public void AllRefMinMax_Throws(string schema)
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.Add(null, XmlReader.Create(new StringReader(schema)));

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());

            Assert.Contains("all", ex.Message);
            Assert.Contains("minOccurs", ex.Message);
            Assert.Contains("maxOccurs", ex.Message);
        }
        #endregion

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void TotalDigitsParseValue_Succeeds()
        {
            string schema = @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified'
           xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='foo'>
        <xs:restriction base='xs:decimal'>
            <xs:totalDigits value='8' fixed='true'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='bar'>
        <xs:restriction base='foo'>
            <xs:totalDigits value='8'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
";
            using (StringReader srdr = new StringReader(schema))
            using (XmlReader xmlrdr = XmlReader.Create(srdr))
            {
                XmlSchemaSet ss = new XmlSchemaSet();

                ss.Add(null, xmlrdr);

                // Assert does not throw (Regression test for issue #34426)
                ss.Compile();
            }
        }

        #region tests causing XmlSchemaException with Sch_WhiteSpaceRestriction1
        public static IEnumerable<object[]> WhiteSpaceRestriction1_Throws_TestData
        {
            get
            {
                return new List<object[]>()
                {
                    new object[]
                    {
@"<?xml version='1.0' encoding='utf-8'?>
<xs:schema elementFormDefault='qualified'
            xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='baseType'>
        <xs:restriction base='xs:string'>
            <xs:whiteSpace value='collapse'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='restrictedType'>
        <xs:restriction base='baseType'>
            <xs:whiteSpace value='preserve'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    },
                    new object[]
                    {
@"<?xml version='1.0' encoding='utf-8'?>
<xs:schema elementFormDefault='qualified'
            xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='baseType'>
        <xs:restriction base='xs:string'>
            <xs:whiteSpace value='collapse'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='restrictedType'>
        <xs:restriction base='baseType'>
            <xs:whiteSpace value='replace'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(WhiteSpaceRestriction1_Throws_TestData))]
        public void WhiteSpaceRestriction1_Throws(string schema)
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            using (StringReader sr = new StringReader(schema))
            using (XmlReader xmlrdr = XmlReader.Create(sr))
            {
                ss.Add(null, xmlrdr);
            }

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());

            Assert.Contains("whiteSpace", ex.Message);
            Assert.Contains("collapse", ex.Message);
            Assert.Contains("preserve", ex.Message);
            Assert.Contains("replace", ex.Message);
        }
        #endregion

        #region tests causing XmlSchemaException with Sch_WhiteSpaceRestriction2
        public static IEnumerable<object[]> WhiteSpaceRestriction2_Throws_TestData
        {
            get
            {
                return new List<object[]>()
                {
                    new object[]
                    {
@"<?xml version='1.0' encoding='utf-8'?>
<xs:schema elementFormDefault='qualified'
            xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:simpleType name='baseType'>
        <xs:restriction base='xs:string'>
            <xs:whiteSpace value='replace'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name='restrictedType'>
        <xs:restriction base='baseType'>
            <xs:whiteSpace value='preserve'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(WhiteSpaceRestriction2_Throws_TestData))]
        public void WhiteSpaceRestriction2_Throws(string schema)
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            using (StringReader sr = new StringReader(schema))
            using (XmlReader xmlrdr = XmlReader.Create(sr))
            {
                ss.Add(null, xmlrdr);
            }

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());

            Assert.Contains("whiteSpace", ex.Message);
            Assert.DoesNotContain("collapse", ex.Message);
            Assert.Contains("preserve", ex.Message);
            Assert.Contains("replace", ex.Message);
        }
        #endregion

        #region Attribute Restriction Invalid From WildCard tests

        public static IEnumerable<object[]> AttributeRestrictionInvalidFromWildCard_Throws_TestData
        {
            get
            {
                return new List<object[]>()
                {
                    new object[]
                    {
                        @"<?xml version='1.0' encoding='utf-8'?>
<xs:schema elementFormDefault='qualified'
            xmlns:xs='http://www.w3.org/2001/XMLSchema'>

    <xs:redefine schemaLocation='fake://0'>
        <xs:attributeGroup name='baseGroup'>
            <xs:attribute name='a' type='xs:integer'/>
            <xs:attribute name='b' type='xs:integer'/>
            <xs:attribute name='c' type='xs:integer'/>
            <xs:attribute name='d' type='xs:integer'/>
        </xs:attributeGroup>
    </xs:redefine>
</xs:schema>
"
                    },
                    new object[]
                    {
                        @"<?xml version='1.0' encoding='utf-8'?>
<xs:schema elementFormDefault='qualified'
            xmlns:xs='http://www.w3.org/2001/XMLSchema'
            targetNamespace='http://www.foo.bar'>

    <xs:redefine schemaLocation='fake://1'>
        <xs:attributeGroup name='baseGroup'>
            <xs:attribute name='a' type='xs:integer'/>
            <xs:attribute name='b' type='xs:integer'/>
            <xs:attribute name='c' type='xs:integer'/>
            <xs:attribute name='d' type='xs:integer' form='qualified'/>
        </xs:attributeGroup>
    </xs:redefine>
</xs:schema>
"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(AttributeRestrictionInvalidFromWildCard_Throws_TestData))]
        public void AttributeRestrictionInvalidFromWildCard_Throws(string schema)
        {
            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new FakeXmlResolverAttributeRestriction();
            using (StringReader sr = new StringReader(schema))
            using (XmlReader xmlrdr = XmlReader.Create(sr))
            {
                ss.Add(null, xmlrdr);
            }

            Exception ex = Assert.Throws<XmlSchemaException>(() => ss.Compile());

            Assert.Contains("wildcard", ex.Message);
            Assert.Contains("redefine", ex.Message);
        }

        private class FakeXmlResolverAttributeRestriction : XmlResolver
        {
            public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
            {
                int uriIndex = int.Parse(absoluteUri.Host);
                string[] schema = { @"<?xml version='1.0' encoding='utf-8'?>
<xs:schema elementFormDefault='qualified'
            xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:attributeGroup name='baseGroup'>
        <xs:attribute name='a' type='xs:integer'/>
        <xs:attribute name='b' type='xs:integer'/>
        <xs:attribute name='c' type='xs:integer'/>
    </xs:attributeGroup>
</xs:schema>
",
@"<?xml version='1.0' encoding='utf-8'?>
<xs:schema elementFormDefault='qualified'
            xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:attributeGroup name='baseGroup'>
        <xs:attribute name='a' type='xs:integer'/>
        <xs:attribute name='b' type='xs:integer'/>
        <xs:attribute name='c' type='xs:integer'/>
        <xs:anyAttribute namespace='##local'/>
    </xs:attributeGroup>
</xs:schema>
"
                };

                return new MemoryStream(Encoding.UTF8.GetBytes(schema[uriIndex]));
            }
        }
        #endregion
    }
}
