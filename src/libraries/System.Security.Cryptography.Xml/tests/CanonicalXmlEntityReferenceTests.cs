using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class CanonicalXmlEntityReferenceTests
    {
        [Fact]
        public void Write_WritesExpectedOutput()
        {
            var xmlDocument = new XmlDocument();
            var entityReference = new CanonicalXmlEntityReference("entity", xmlDocument, true);
            var stringBuilder = new StringBuilder();
            var anc = new AncestralNamespaceContextManager();

            entityReference.Write(stringBuilder, DocPosition.InsideRootElement, anc);

            Assert.Equal("<!ENTITY entity SYSTEM \"entity\">", stringBuilder.ToString());
        }

        [Fact]
        public void WriteHash_WritesExpectedHash()
        {
            var xmlDocument = new XmlDocument();
            var entityReference = new CanonicalXmlEntityReference("entity", xmlDocument, true);
            var hashAlgorithm = SHA256.Create();
            var anc = new AncestralNamespaceContextManager();

            entityReference.WriteHash(hashAlgorithm, DocPosition.InsideRootElement, anc);

            var expectedHash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes("<!ENTITY entity SYSTEM \"entity\">"));
            Assert.Equal(expectedHash, hashAlgorithm.Hash);
        }

        [Fact]
        public void IsInNodeSet_GetSet_ReturnsExpected()
        {
            var xmlDocument = new XmlDocument();
            var entityReference = new CanonicalXmlEntityReference("entity", xmlDocument, true);

            Assert.True(entityReference.IsInNodeSet);

            entityReference.IsInNodeSet = false;
            Assert.False(entityReference.IsInNodeSet);
        }
    }
}
