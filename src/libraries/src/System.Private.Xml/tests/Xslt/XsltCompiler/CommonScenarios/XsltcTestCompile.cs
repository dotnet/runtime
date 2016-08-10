// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//using Xunit;
//using Xunit.Abstractions;
//using System.IO;
//using System.Xml;
//using System.Xml.Xsl;

// BinCompat TODO: Disabling this for now, as this is loading a dll with no strong name
//namespace System.Xml.Tests
//{
//    //[TestCase(Name = "Compile Time Tests", Desc = "This Testcase maps to test variations described in 'CompileTime Variations Functional Tests'")]
//    public class XsltcTestCompile : XsltcTestCaseBase
//    {
//        private ITestOutputHelper _output;
//        public XsltcTestCompile(ITestOutputHelper output) : base(output)
//        {
//            _output = output;
//        }

//        //[Variation("1", Desc = "Test calling XslCompiledTransform(typeof()) instead of using reflection to load the assembly.", Pri = 1)]
//        [InlineData()]
//        [Theory]
//        public void Var1()
//        {
//            var inputXml = new XmlDocument();
//            inputXml.LoadXml("<foo><bar>Hello, world!</bar></foo>");

//            var xslt = new XslCompiledTransform();
//            xslt.Load(typeof(TestStylesheet));

//            using (var actualStream = new MemoryStream())
//            using (var sw = new StreamWriter(actualStream)
//            {
//                AutoFlush = true
//            })
//            {
//                xslt.Transform(inputXml, null, sw);

//                CompareOutput("<?xml version=\"1.0\" encoding=\"utf-8\"?>Hello foo!", actualStream);

//                return;
//            }
//        }
//    }
//}