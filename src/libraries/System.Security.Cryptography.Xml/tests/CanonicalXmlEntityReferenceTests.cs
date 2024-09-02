using System.Text;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public static class CanonicalXmlEntityReferenceTests
    {
        [Fact]
        public static void Write_WritesExpectedOutput()
        {
            var xmlDocument = new XmlDocument();
            var entityReference = new CanonicalXmlEntityReference("entity", xmlDocument, true);
            var stringBuilder = new StringBuilder();
            var anc = new AncestralNamespaceContextManager();

            entityReference.Write(stringBuilder, DocPosition.InsideRootElement, anc);

            Assert.Equal("<!ENTITY entity SYSTEM \"entity\">", stringBuilder.ToString());
        }

        [Fact]
        public static void WriteHash_WritesExpectedHash()
        {
            var xmlDocument = new XmlDocument();
            var entityReference = new CanonicalXmlEntityReference("entity", xmlDocument, true);
            using SHA256 hashAlgorithm = SHA256.Create();
            var anc = new AncestralNamespaceContextManager();

            entityReference.WriteHash(hashAlgorithm, DocPosition.InsideRootElement, anc);

            byte[] expectedHash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes("<!ENTITY entity SYSTEM \"entity\">"));
            Assert.Equal(expectedHash, hashAlgorithm.Hash);
        }

        [Fact]
        public static void IsInNodeSet_GetSet_ReturnsExpected()
        {
            var xmlDocument = new XmlDocument();
            var entityReference = new CanonicalXmlEntityReference("entity", xmlDocument, true);

            Assert.True(entityReference.IsInNodeSet);

            entityReference.IsInNodeSet = false;
            Assert.False(entityReference.IsInNodeSet);
        }
    }
}
