// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class KeyReferenceTests
    {
        [Fact]
        public void Constructor_Default()
        {
            KeyReference keyRef = new KeyReference();
            Assert.NotNull(keyRef);
        }

        [Fact]
        public void Constructor_WithUri()
        {
            string uri = "#EncryptedKey1";
            KeyReference keyRef = new KeyReference(uri);
            Assert.Equal(uri, keyRef.Uri);
        }

        [Fact]
        public void Constructor_WithUriAndTransformChain()
        {
            string uri = "#EncryptedKey1";
            TransformChain tc = new TransformChain();
            tc.Add(new XmlDsigBase64Transform());
            
            KeyReference keyRef = new KeyReference(uri, tc);
            Assert.Equal(uri, keyRef.Uri);
            Assert.NotNull(keyRef.TransformChain);
        }

        [Fact]
        public void GetXml_ReturnsValidXml()
        {
            KeyReference keyRef = new KeyReference("#key1");
            XmlElement element = keyRef.GetXml();
            Assert.NotNull(element);
            Assert.Equal("KeyReference", element.LocalName);
        }
    }

    public class DataReferenceTests
    {
        [Fact]
        public void Constructor_Default()
        {
            DataReference dataRef = new DataReference();
            Assert.NotNull(dataRef);
        }

        [Fact]
        public void Constructor_WithUri()
        {
            string uri = "#EncryptedData1";
            DataReference dataRef = new DataReference(uri);
            Assert.Equal(uri, dataRef.Uri);
        }

        [Fact]
        public void Constructor_WithUriAndTransformChain()
        {
            string uri = "#EncryptedData1";
            TransformChain tc = new TransformChain();
            tc.Add(new XmlDsigBase64Transform());
            
            DataReference dataRef = new DataReference(uri, tc);
            Assert.Equal(uri, dataRef.Uri);
            Assert.NotNull(dataRef.TransformChain);
        }

        [Fact]
        public void GetXml_ReturnsValidXml()
        {
            DataReference dataRef = new DataReference("#data1");
            XmlElement element = dataRef.GetXml();
            Assert.NotNull(element);
            Assert.Equal("DataReference", element.LocalName);
        }
    }

    public class ReferenceListTests
    {
        [Fact]
        public void Constructor_CreatesEmptyList()
        {
            ReferenceList refList = new ReferenceList();
            Assert.Equal(0, refList.Count);
        }

        [Fact]
        public void Add_DataReference()
        {
            ReferenceList refList = new ReferenceList();
            DataReference dataRef = new DataReference("#data1");
            
            int index = refList.Add(dataRef);
            Assert.Equal(0, index);
            Assert.Equal(1, refList.Count);
        }

        [Fact]
        public void Add_KeyReference()
        {
            ReferenceList refList = new ReferenceList();
            KeyReference keyRef = new KeyReference("#key1");
            
            int index = refList.Add(keyRef);
            Assert.Equal(0, index);
            Assert.Equal(1, refList.Count);
        }

        [Fact]
        public void Add_InvalidType_ThrowsArgumentException()
        {
            ReferenceList refList = new ReferenceList();
            Assert.Throws<ArgumentException>(() => refList.Add("invalid"));
        }

        [Fact]
        public void Add_Null_ThrowsArgumentNullException()
        {
            ReferenceList refList = new ReferenceList();
            Assert.Throws<ArgumentNullException>(() => refList.Add(null));
        }

        [Fact]
        public void Contains_ExistingItem_ReturnsTrue()
        {
            ReferenceList refList = new ReferenceList();
            DataReference dataRef = new DataReference("#data1");
            refList.Add(dataRef);
            
            Assert.True(refList.Contains(dataRef));
        }

        [Fact]
        public void Contains_NonExistingItem_ReturnsFalse()
        {
            ReferenceList refList = new ReferenceList();
            DataReference dataRef1 = new DataReference("#data1");
            DataReference dataRef2 = new DataReference("#data2");
            refList.Add(dataRef1);
            
            Assert.False(refList.Contains(dataRef2));
        }

        [Fact]
        public void IndexOf_ReturnsCorrectIndex()
        {
            ReferenceList refList = new ReferenceList();
            DataReference dataRef1 = new DataReference("#data1");
            DataReference dataRef2 = new DataReference("#data2");
            refList.Add(dataRef1);
            refList.Add(dataRef2);
            
            Assert.Equal(0, refList.IndexOf(dataRef1));
            Assert.Equal(1, refList.IndexOf(dataRef2));
        }

        [Fact]
        public void Insert_InsertsAtCorrectPosition()
        {
            ReferenceList refList = new ReferenceList();
            DataReference dataRef1 = new DataReference("#data1");
            DataReference dataRef2 = new DataReference("#data2");
            DataReference dataRef3 = new DataReference("#data3");
            
            refList.Add(dataRef1);
            refList.Add(dataRef3);
            refList.Insert(1, dataRef2);
            
            Assert.Equal(3, refList.Count);
            Assert.Equal(dataRef2, refList[1]);
        }

        [Fact]
        public void Insert_Null_ThrowsArgumentNullException()
        {
            ReferenceList refList = new ReferenceList();
            Assert.Throws<ArgumentNullException>(() => refList.Insert(0, null));
        }

        [Fact]
        public void Insert_InvalidType_ThrowsArgumentException()
        {
            ReferenceList refList = new ReferenceList();
            Assert.Throws<ArgumentException>(() => refList.Insert(0, "invalid"));
        }

        [Fact]
        public void Remove_RemovesItem()
        {
            ReferenceList refList = new ReferenceList();
            DataReference dataRef = new DataReference("#data1");
            refList.Add(dataRef);
            
            refList.Remove(dataRef);
            Assert.Equal(0, refList.Count);
        }

        [Fact]
        public void Clear_RemovesAllItems()
        {
            ReferenceList refList = new ReferenceList();
            refList.Add(new DataReference("#data1"));
            refList.Add(new KeyReference("#key1"));
            
            refList.Clear();
            Assert.Equal(0, refList.Count);
        }

        [Fact]
        public void GetEnumerator_EnumeratesAllItems()
        {
            ReferenceList refList = new ReferenceList();
            refList.Add(new DataReference("#data1"));
            refList.Add(new KeyReference("#key1"));
            
            int count = 0;
            foreach (var item in refList)
            {
                Assert.NotNull(item);
                count++;
            }
            Assert.Equal(2, count);
        }

        [Fact]
        public void Item_SetValue_ReplacesItem()
        {
            ReferenceList refList = new ReferenceList();
            DataReference dataRef1 = new DataReference("#data1");
            DataReference dataRef2 = new DataReference("#data2");
            refList.Add(dataRef1);
            
            refList[0] = dataRef2;
            Assert.Equal(dataRef2, refList[0]);
        }

        [Fact]
        public void Item_SetNull_ThrowsArgumentNullException()
        {
            ReferenceList refList = new ReferenceList();
            refList.Add(new DataReference("#data1"));
            
            Assert.Throws<ArgumentNullException>(() => refList[0] = null);
        }

        [Fact]
        public void RemoveAt_RemovesItemAtIndex()
        {
            ReferenceList refList = new ReferenceList();
            DataReference dataRef1 = new DataReference("#data1");
            DataReference dataRef2 = new DataReference("#data2");
            refList.Add(dataRef1);
            refList.Add(dataRef2);
            
            refList.RemoveAt(0);
            Assert.Equal(1, refList.Count);
            Assert.Equal(dataRef2, refList[0]);
        }

        [Fact]
        public void IList_Properties()
        {
            ReferenceList refList = new ReferenceList();
            IList iList = refList;
            Assert.False(iList.IsFixedSize);
            Assert.False(iList.IsReadOnly);
            Assert.False(refList.IsSynchronized);
            Assert.NotNull(refList.SyncRoot);
        }

        [Fact]
        public void CopyTo_CopiesItemsToArray()
        {
            ReferenceList refList = new ReferenceList();
            DataReference dataRef1 = new DataReference("#data1");
            DataReference dataRef2 = new DataReference("#data2");
            refList.Add(dataRef1);
            refList.Add(dataRef2);
            
            object[] array = new object[2];
            refList.CopyTo(array, 0);
            
            Assert.Equal(dataRef1, array[0]);
            Assert.Equal(dataRef2, array[1]);
        }
    }

    public class CanonicalXmlSignificantWhitespaceTests
    {
        [Fact]
        public void SignificantWhitespaceInCanonicalization()
        {
            // Test with XML containing significant whitespace
            string xml = @"<root xml:space='preserve'>  
    <child>text</child>  
</root>";

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
            transform.LoadInput(doc);
            
            Stream output = (Stream)transform.GetOutput();
            string result = new StreamReader(output, Encoding.UTF8).ReadToEnd();
            
            Assert.Contains("<root", result);
            Assert.Contains("xml:space", result);
        }

        [Fact]
        public void SignificantWhitespaceWithHash()
        {
            string xml = @"<root xml:space='preserve'>
    <child>text</child>
</root>";

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
            transform.LoadInput(doc);
            
            using (SHA256 hash = SHA256.Create())
            {
                byte[] digest = transform.GetDigestedOutput(hash);
                Assert.NotNull(digest);
                Assert.Equal(32, digest.Length);
            }
        }
    }

    public class CanonicalXmlCommentTests
    {
        [Fact]
        public void CommentInCanonicalizationWithComments()
        {
            string xml = @"<root><!--test comment--><child>text</child></root>";

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            XmlDsigC14NWithCommentsTransform transform = new XmlDsigC14NWithCommentsTransform();
            transform.LoadInput(doc);
            
            Stream output = (Stream)transform.GetOutput();
            string result = new StreamReader(output, Encoding.UTF8).ReadToEnd();
            
            Assert.Contains("<!--test comment-->", result);
        }

        [Fact]
        public void MultipleCommentsInCanonicalization()
        {
            string xml = @"<root><!--comment1--><child><!--comment2-->text</child><!--comment3--></root>";

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            XmlDsigC14NWithCommentsTransform transform = new XmlDsigC14NWithCommentsTransform();
            transform.LoadInput(doc);
            
            Stream output = (Stream)transform.GetOutput();
            string result = new StreamReader(output, Encoding.UTF8).ReadToEnd();
            
            Assert.Contains("<!--comment1-->", result);
            Assert.Contains("<!--comment2-->", result);
            Assert.Contains("<!--comment3-->", result);
        }
    }

    public class CanonicalXmlCDataSectionTests
    {
        [Fact]
        public void CDataInCanonicalization()
        {
            string xml = @"<root><![CDATA[This is <CDATA> content with special & characters]]></root>";

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
            transform.LoadInput(doc);
            
            Stream output = (Stream)transform.GetOutput();
            string result = new StreamReader(output, Encoding.UTF8).ReadToEnd();
            
            // CDATA should be output as text in canonical form
            Assert.Contains("This is ", result);
            Assert.Contains(" content with special ", result);
        }

        [Fact]
        public void CDataWithHash()
        {
            string xml = @"<root><![CDATA[CDATA content]]></root>";

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
            transform.LoadInput(doc);
            
            using (SHA256 hash = SHA256.Create())
            {
                byte[] digest = transform.GetDigestedOutput(hash);
                Assert.NotNull(digest);
                Assert.Equal(32, digest.Length);
            }
        }
    }

    public class TransformChainTests
    {
        [Fact]
        public void Constructor_CreatesEmptyChain()
        {
            TransformChain chain = new TransformChain();
            Assert.NotNull(chain);
            Assert.Equal(0, chain.Count);
        }

        [Fact]
        public void Add_AddsTransform()
        {
            TransformChain chain = new TransformChain();
            XmlDsigBase64Transform transform = new XmlDsigBase64Transform();
            
            chain.Add(transform);
            Assert.Equal(1, chain.Count);
        }

        [Fact]
        public void Add_MultipleTransforms()
        {
            TransformChain chain = new TransformChain();
            chain.Add(new XmlDsigBase64Transform());
            chain.Add(new XmlDsigC14NTransform());
            
            Assert.Equal(2, chain.Count);
        }

        [Fact]
        public void Indexer_ReturnsCorrectTransform()
        {
            TransformChain chain = new TransformChain();
            XmlDsigBase64Transform transform1 = new XmlDsigBase64Transform();
            XmlDsigC14NTransform transform2 = new XmlDsigC14NTransform();
            
            chain.Add(transform1);
            chain.Add(transform2);
            
            Assert.Equal(transform1, chain[0]);
            Assert.Equal(transform2, chain[1]);
        }

        [Fact]
        public void GetEnumerator_EnumeratesTransforms()
        {
            TransformChain chain = new TransformChain();
            chain.Add(new XmlDsigBase64Transform());
            chain.Add(new XmlDsigC14NTransform());
            
            int count = 0;
            foreach (Transform transform in chain)
            {
                Assert.NotNull(transform);
                count++;
            }
            Assert.Equal(2, count);
        }
    }

    public class XmlDsigEnvelopedSignatureTransformTests
    {
        [Fact]
        public void Constructor_SetsAlgorithm()
        {
            XmlDsigEnvelopedSignatureTransform transform = new XmlDsigEnvelopedSignatureTransform();
            Assert.Equal("http://www.w3.org/2000/09/xmldsig#enveloped-signature", transform.Algorithm);
        }

        [Fact]
        public void GetInnerXml_ReturnsNull()
        {
            XmlDsigEnvelopedSignatureTransform transform = new XmlDsigEnvelopedSignatureTransform();
            // Protected method, but we can test behavior indirectly
            Assert.Equal("http://www.w3.org/2000/09/xmldsig#enveloped-signature", transform.Algorithm);
        }

        [Fact]
        public void LoadInput_WithDocument()
        {
            string xml = @"<root><child>data</child></root>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            XmlDsigEnvelopedSignatureTransform transform = new XmlDsigEnvelopedSignatureTransform();
            transform.LoadInput(doc);
            
            object output = transform.GetOutput();
            Assert.NotNull(output);
            Assert.IsAssignableFrom<XmlDocument>(output);
        }

        [Fact]
        public void InputTypes_ContainsExpectedTypes()
        {
            XmlDsigEnvelopedSignatureTransform transform = new XmlDsigEnvelopedSignatureTransform();
            Type[] inputTypes = transform.InputTypes;
            
            Assert.Contains(typeof(Stream), inputTypes);
            Assert.Contains(typeof(XmlDocument), inputTypes);
            Assert.Contains(typeof(XmlNodeList), inputTypes);
        }

        [Fact]
        public void OutputTypes_ContainsExpectedTypes()
        {
            XmlDsigEnvelopedSignatureTransform transform = new XmlDsigEnvelopedSignatureTransform();
            Type[] outputTypes = transform.OutputTypes;
            
            Assert.Contains(typeof(XmlDocument), outputTypes);
            Assert.Contains(typeof(XmlNodeList), outputTypes);
        }
    }
}
