using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlImplementationTests
{
    public class HasFeatureTests
    {
        [Fact]
        public static void TestForFeatureSome()
        {
            var imp = new XmlImplementation();

            Assert.False(imp.HasFeature("some", "1.0"));
            Assert.False(imp.HasFeature("some", "2.0"));
            Assert.False(imp.HasFeature("some", "3.0"));

        }
    }
}
