using Xunit;
using Xunit.Abstractions;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_Count", Desc = "", Priority = 0)]
    public class TC_SchemaSet_Count
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_Count(ITestOutputHelper output)
        {
            _output = output;
        }


        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v1 - Count on empty collection", Priority = 1)]
        [InlineData()]
        [Theory]
        public void v1()
        {
            XmlSchemaSet sc = new XmlSchemaSet();

            CError.Compare(sc.Count, 0, "Count");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v2 - Add two schemas for same ns and Count")]
        [InlineData()]
        [Theory]
        public void v2()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.Add("xsdauthor", TestData._XsdNoNs);
            sc.Add("xsdauthor", TestData._XsdAuthor);

            CError.Compare(sc.Count, 2, "Count");

            return;
        }

        //-----------------------------------------------------------------------------------
        //[Variation(Desc = "v3 - Add two schemas for diff ns with imports/includes and Count")]
        [InlineData()]
        [Theory]
        public void v3()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.Add("xsdauthor", TestData._XsdAuthor);
            sc.Add(null, TestData._Root + "xsdbookexternal.xsd");

            CError.Compare(sc.Count, 2, "Count");

            return;
        }
    }
}