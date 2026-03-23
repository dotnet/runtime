// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class CanonicalXmlEntityReferenceTest
    {
        [Fact]
        public void EntityReferenceInCanonicalization()
        {
            // This test exercises CanonicalXmlEntityReference by using a transform
            // that internally creates a CanonicalXmlDocument and loads XML into it.
            // When an XmlNodeReader reads from a document containing entity references,
            // the target document's CreateEntityReference method is called.

            string xml = @"<!DOCTYPE doc [
<!ENTITY test ""TestValue"">
]>
<root>&test;</root>";

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            // Use C14N transform which internally uses CanonicalXmlDocument
            XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
            transform.LoadInput(doc);
            
            Stream output = (Stream)transform.GetOutput();
            string result = new StreamReader(output, Encoding.UTF8).ReadToEnd();
            
            // Entity should be expanded in canonical form
            Assert.Equal("<root>TestValue</root>", result);
        }

        [Fact]
        public void EntityReferenceWithXmlNodeList()
        {
            // Test with XmlNodeList input which triggers different code path in CanonicalXml
            // When using XmlNodeList, nodes not in the list are not included, which may affect
            // entity reference expansion in the canonical output
            string xml = @"<!DOCTYPE doc [
<!ENTITY test ""Hello"">
]>
<root><child>&test;</child></root>";

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            XmlNodeList nodeList = doc.SelectNodes("//child")!;

            XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
            transform.LoadInput(nodeList);
            
            Stream output = (Stream)transform.GetOutput();
            string result = new StreamReader(output, Encoding.UTF8).ReadToEnd();
            
            // Only the child element should be in the result (entity not expanded since only child element is in XPath)
            Assert.Equal("<child></child>", result);
        }

        [Fact]
        public void EntityReferenceWithCommentsIncluded()
        {
            // Test with includeComments = true
            string xml = @"<!DOCTYPE doc [
<!ENTITY ent ""EntityContent"">
]>
<root><!--comment-->&ent;</root>";

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            XmlDsigC14NWithCommentsTransform transform = new XmlDsigC14NWithCommentsTransform();
            transform.LoadInput(doc);
            
            Stream output = (Stream)transform.GetOutput();
            string result = new StreamReader(output, Encoding.UTF8).ReadToEnd();
            
            // Both comment and expanded entity should be in output
            Assert.Contains("<!--comment-->", result);
            Assert.Contains("EntityContent", result);
        }

        [Fact]
        public void EntityReferenceInExclusiveCanonicalization()
        {
            // Test with Exclusive C14N transform
            string xml = @"<!DOCTYPE doc [
<!ENTITY test ""ExclusiveTest"">
]>
<root xmlns=""http://example.com"">&test;</root>";

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
            transform.LoadInput(doc);
            
            Stream output = (Stream)transform.GetOutput();
            string result = new StreamReader(output, Encoding.UTF8).ReadToEnd();
            
            // Entity should be expanded in canonical form
            Assert.Contains("ExclusiveTest", result);
            Assert.Contains("http://example.com", result);
        }

        [Fact]
        public void EntityReferenceWithHash()
        {
            // Test the WriteHash code path by using GetDigestedOutput
            string xml = @"<!DOCTYPE doc [
<!ENTITY test ""HashTest"">
]>
<root>&test;</root>";

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
            transform.LoadInput(doc);
            
            using (SHA256 hash = SHA256.Create())
            {
                byte[] digest = transform.GetDigestedOutput(hash);
                
                // Should produce a valid hash
                Assert.NotNull(digest);
                Assert.Equal(32, digest.Length); // SHA256 produces 32 bytes
            }
        }

        [Fact]
        public void MultipleEntityReferences()
        {
            // Test with multiple entity references
            string xml = @"<!DOCTYPE doc [
<!ENTITY ent1 ""First"">
<!ENTITY ent2 ""Second"">
]>
<root>&ent1; and &ent2;</root>";

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
            transform.LoadInput(doc);
            
            Stream output = (Stream)transform.GetOutput();
            string result = new StreamReader(output, Encoding.UTF8).ReadToEnd();
            
            // Both entities should be expanded
            Assert.Contains("First", result);
            Assert.Contains("Second", result);
            Assert.Contains("and", result);
        }
    }
}
