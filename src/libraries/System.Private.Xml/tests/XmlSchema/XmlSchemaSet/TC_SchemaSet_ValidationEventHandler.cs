// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.Collections;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_ValidationEventHandler", Desc = "")]
    public class TC_SchemaSet_ValidationEventHandler : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_ValidationEventHandler(ITestOutputHelper output)
        {
            _output = output;
        }


        private ArrayList _ValidationErrors = new ArrayList();

        public void ValidationCallBack(object sender, ValidationEventArgs args)
        {
            _output.WriteLine("***{0}", args.Message);
            _ValidationErrors.Add(args);
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v1 - ValidationEventHandler, add invalid XDR schema")]
        [InlineData()]
        [Theory]
        public void v1()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
            _ValidationErrors.Clear();
            sc.Add("xdrerror", TestData._XdrError);

            CError.Compare(sc.Count, 0, "sc count");
            CError.Compare(_ValidationErrors.Count, 1, "err count");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v2 - ValidationEventHandler, add invalid XSD schema", Priority = 0)]
        [InlineData()]
        [Theory]
        public void v2()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
            _ValidationErrors.Clear();
            sc.Add("xdrerror", TestData._XsdError);

            CError.Compare(sc.Count, 0, "sc count");
            CError.Compare(_ValidationErrors.Count, 1, "err count");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v3 - ValidationEventArgs accuracy")]
        [InlineData()]
        [Theory]
        public void v3()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
            _ValidationErrors.Clear();
            sc.Add("xsderror", TestData._XsdError);
            sc.Add("xsderror", TestData._XsdError2);

            // should only be one error before compile()
            CError.Compare(_ValidationErrors.Count, 1, "err count");
            sc.Compile();
            // should be two errors before compile()
            CError.Compare(_ValidationErrors.Count, 2, "err count");
            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v4 - Add(XmlSchema) and verify handler is called")]
        [InlineData()]
        [Theory]
        public void v4()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
            _ValidationErrors.Clear();
            sc.Add(XmlSchema.Read(new XmlTextReader(TestData._XsdError2), null));

            CError.Compare(sc.Count, 1, "sc count");
            // schema should still be added and the error cound should be 0
            CError.Compare(_ValidationErrors.Count, 0, "err count");
            sc.Compile();
            // schema should still be added and the error cound should be 1
            CError.Compare(_ValidationErrors.Count, 1, "err count");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v5 - Add(XmlSchema) and verify handler is called")]
        [InlineData()]
        [Theory]
        public void v5()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
            _ValidationErrors.Clear();
            sc.Add("xsderror", new XmlTextReader(TestData._XsdError2));

            CError.Compare(sc.Count, 1, "sc count");

            // schema should still be added and the error cound should be 0
            CError.Compare(_ValidationErrors.Count, 0, "err count");
            sc.Compile();
            // schema should still be added and the error cound should be 1
            CError.Compare(_ValidationErrors.Count, 1, "err count");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v6 - Add(URL) and verify handler is called")]
        [InlineData()]
        [Theory]
        public void v6()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
            _ValidationErrors.Clear();
            sc.Add("xsderror", TestData._XsdError2);

            CError.Compare(sc.Count, 1, "sc count");
            // schema should still be added and the error cound should be 0
            CError.Compare(_ValidationErrors.Count, 0, "err count");
            sc.Compile();
            // schema should still be added and the error cound should be 1
            CError.Compare(_ValidationErrors.Count, 1, "err count");

            return;
        }

        //[Variation(Desc = "v7 - remove ValidationEventHandler")]
        [InlineData()]
        [Theory]
        public void v7()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
            _ValidationErrors.Clear();
            sc.Add("xsderror", TestData._XsdError);
            sc.Add("xsderror", TestData._XsdError2);

            // should only be one error before compile()
            CError.Compare(_ValidationErrors.Count, 1, "err count");
            sc.Compile();
            // should be two errors before compile()
            CError.Compare(_ValidationErrors.Count, 2, "err count");

            sc.ValidationEventHandler -= new ValidationEventHandler(ValidationCallBack);
            try
            {
                sc.Add("xsderror", TestData._XsdError);
                sc.Add("xsderror", TestData._XsdError2);
            }
            catch (XmlSchemaException e)
            {
                _output.WriteLine(e.Message);
            }
            CError.Compare(_ValidationErrors.Count, 2, "err count");

            return;
        }
    }
}