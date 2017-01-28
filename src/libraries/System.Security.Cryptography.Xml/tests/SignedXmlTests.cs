using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class SignedXmlTests
    {
        [Fact]
        public void Constructor_Document_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SignedXml((XmlDocument) null)
            );
        }

        [Fact]
        public void Constructor_XmlElement_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SignedXml((XmlElement) null)
            );
        }
    }
}
