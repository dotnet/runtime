// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class SyndicationElementExtensionCollectionTests
    {
        [Fact]
        public void Add_Invoke_ReturnsExpected()
        {
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            elementExtensions.Add(new ExtensionObject { Value = 10 });
            elementExtensions.Add(new SyndicationElementExtension(new ExtensionObject { Value = 11 }));
            using (var stringReader = new StringReader(@"<SyndicationElementExtensionCollectionTests.ExtensionObject xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Value>12</Value></SyndicationElementExtensionCollectionTests.ExtensionObject>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                elementExtensions.Add(reader);
            }
            elementExtensions.Add(new ExtensionObject { Value = 13 }, new DataContractSerializer(typeof(ExtensionObject)));
            elementExtensions.Add(new ExtensionObject { Value = 14 }, (DataContractSerializer)null);
            elementExtensions.Add(new ExtensionObject { Value = 15 }, new XmlSerializer(typeof(ExtensionObject)));
            elementExtensions.Add(new ExtensionObject { Value = 16 }, (XmlSerializer)null);
            elementExtensions.Add("outerName", "outerNamespace", new ExtensionObject { Value = 17 });
            elementExtensions.Add("outerName", "outerNamespace", new ExtensionObject { Value = 18 }, new DataContractSerializer(typeof(ExtensionObject)));
            elementExtensions.Add("outerName", "outerNamespace", new ExtensionObject { Value = 19 }, null);

            Assert.Equal(10, elementExtensions.Count);

            using (XmlReader reader = elementExtensions.GetReaderAtElementExtensions())
            {
                Assert.Equal(@"<SyndicationElementExtensionCollectionTests.ExtensionObject xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Value>10</Value></SyndicationElementExtensionCollectionTests.ExtensionObject>", reader.ReadOuterXml());
            }
        }

        [Fact]
        public void Add_NullDataContractExtension_ThrowsArgumentNullException()
        {
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            AssertExtensions.Throws<ArgumentNullException>("dataContractExtension", () => elementExtensions.Add((object)null));
            AssertExtensions.Throws<ArgumentNullException>("dataContractExtension", () => elementExtensions.Add(null, new DataContractSerializer(typeof(ExtensionObject))));
            AssertExtensions.Throws<ArgumentNullException>("dataContractExtension", () => elementExtensions.Add("outerName", "outerNamespace", null));
            AssertExtensions.Throws<ArgumentNullException>("dataContractExtension", () => elementExtensions.Add("outerName", "outerNamespace", null, new DataContractSerializer(typeof(ExtensionObject))));
        }

        [Fact]
        public void Add_NullXmlSerializerExtension_ThrowsArgumentNullException()
        {
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            AssertExtensions.Throws<ArgumentNullException>("xmlSerializerExtension", () => elementExtensions.Add(null, new XmlSerializer(typeof(ExtensionObject))));
        }

        [Fact]
        public void Add_NullReader_ThrowsArgumentNullException()
        {
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            AssertExtensions.Throws<ArgumentNullException>("xmlReader", () => elementExtensions.Add(null));
        }

        [Fact]
        public void Clear_Invoke_Success()
        {
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            elementExtensions.Add(new ExtensionObject { Value = 10 });

            elementExtensions.Clear();
            Assert.Equal(0, elementExtensions.Count);

            elementExtensions.Clear();
            Assert.Equal(0, elementExtensions.Count);
        }

        [Fact]
        public void Insert_Invoke_Success()
        {
            var extension = new SyndicationElementExtension(new ExtensionObject { Value = 10 });
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;

            elementExtensions.Insert(0, extension);
            Assert.Same(extension, elementExtensions[0]);
        }

        [Fact]
        public void Insert_Null_ThrowsArgumentNullException()
        {
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            AssertExtensions.Throws<ArgumentNullException>("item", () => elementExtensions.Insert(0, null));
        }

        [Fact]
        public void ItemSet_Get_ReturnsExpected()
        {
            var extension = new SyndicationElementExtension(new ExtensionObject { Value = 10 });
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            elementExtensions.Add(new ExtensionObject { Value = 9 });

            elementExtensions[0] = extension;
            Assert.Same(extension, elementExtensions[0]);
        }

        [Fact]
        public void ItemSet_Null_ThrowsArgumentNullException()
        {
            var extension = new SyndicationElementExtension(new ExtensionObject { Value = 10 });
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            elementExtensions.Add(new ExtensionObject { Value = 9 });

            AssertExtensions.Throws<ArgumentNullException>("item", () => elementExtensions[0] = null);
        }

        [Fact]
        public void Remove_Invoke_Success()
        {
            var extension = new SyndicationElementExtension(new ExtensionObject { Value = 10 });
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            elementExtensions.Add(extension);

            elementExtensions.RemoveAt(0);
            Assert.Equal(0, elementExtensions.Count);
        }

        [Fact]
        public void GetReaderAtElementExtensions_InvokeMultipleTimes_ReturnsNewReader()
        {
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            Assert.NotSame(elementExtensions.GetReaderAtElementExtensions(), elementExtensions.GetReaderAtElementExtensions());
        }

        [Fact]
        public void ReadElementExtensions_Invoke_ReturnsExpected()
        {
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            elementExtensions.Add(new ExtensionObject { Value = 10 });
            elementExtensions.Add(new ExtensionObject { Value = 11 });
            elementExtensions.Add(new OtherExtensionObject { Value = 11 });
            elementExtensions.Add(new Inner.OtherExtensionObject { Value = 11 });

            Collection<ExtensionObject> results = elementExtensions.ReadElementExtensions<ExtensionObject>("SyndicationElementExtensionCollectionTests.ExtensionObject", "http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests");
            Assert.Equal(2, results.Count);
            Assert.Equal(10, results[0].Value);
            Assert.Equal(11, results[1].Value);

            results = elementExtensions.ReadElementExtensions<ExtensionObject>("SyndicationElementExtensionCollectionTests.ExtensionObject", "http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests", new DataContractSerializer(typeof(ExtensionObject)));
            Assert.Equal(2, results.Count);
            Assert.Equal(10, results[0].Value);
            Assert.Equal(11, results[1].Value);

            results = elementExtensions.ReadElementExtensions<ExtensionObject>("SyndicationElementExtensionCollectionTests.ExtensionObject", "http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests", new XmlSerializer(typeof(ExtensionObject)));
            Assert.Equal(2, results.Count);
            Assert.Equal(10, results[0].Value);
            Assert.Equal(11, results[1].Value);
        }

        [Theory]
        [InlineData("NoSuchObject", null)]
        [InlineData("SyndicationElementExtensionCollectionTests.ExtensionObject", "")]
        [InlineData("SyndicationElementExtensionCollectionTests.ExtensionObject", "http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Testss")]
        [InlineData("SyndicationElementExtensionCollectionTests.extensionobject", "http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests")]
        [InlineData("SyndicationElementExtensionCollectionTests.ExtensionObject", "http://schemas.datacontract.org/2004/07/system.servicemodel.syndication.tests")]
        [InlineData("SyndicationElementExtensionCollectionTests.ExtensionObject", "System.ServiceModel.Syndication.Tests")]
        public void ReadElementExtensions_NoSuchObject_ReturnsEmpty(string name, string ns)
        {
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            elementExtensions.Add(new ExtensionObject { Value = 10 });
            elementExtensions.Add(new ExtensionObject { Value = 11 });

            Assert.Empty(elementExtensions.ReadElementExtensions<ExtensionObject>(name, ns));
            Assert.Empty(elementExtensions.ReadElementExtensions<ExtensionObject>(name, ns, new DataContractSerializer(typeof(ExtensionObject))));
            Assert.Empty(elementExtensions.ReadElementExtensions<ExtensionObject>(name, ns, new XmlSerializer(typeof(ExtensionObject))));
        }

        [Fact]
        public void ReadElementExtensions_Empty_ReturnsEmpty()
        {
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            Assert.Empty(elementExtensions.ReadElementExtensions<ExtensionObject>("ExtensionName", null));
            Assert.Empty(elementExtensions.ReadElementExtensions<ExtensionObject>("ExtensionName", null, new DataContractSerializer(typeof(ExtensionObject))));
            Assert.Empty(elementExtensions.ReadElementExtensions<ExtensionObject>("ExtensionName", null, new XmlSerializer(typeof(ExtensionObject))));
        }

        [Fact]
        public void ReadElementExtensions_NullSerializer_ThrowsArgumentNullException()
        {
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            AssertExtensions.Throws<ArgumentNullException>("serializer", () => elementExtensions.ReadElementExtensions<SyndicationElementExtension>("extensionName", "extensionNamespace", (XmlObjectSerializer)null));
            AssertExtensions.Throws<ArgumentNullException>("serializer", () => elementExtensions.ReadElementExtensions<SyndicationElementExtension>("extensionName", "extensionNamespace", (XmlSerializer)null));
        }

        [Fact]
        public void ReadElementExtensions_EmptyExtensionName_ThrowsArgumentException()
        {
            SyndicationElementExtensionCollection elementExtensions = new SyndicationCategory().ElementExtensions;
            AssertExtensions.Throws<ArgumentException>(null, () => elementExtensions.ReadElementExtensions<SyndicationElementExtension>("", "extensionNamespace"));
            AssertExtensions.Throws<ArgumentException>(null, () => elementExtensions.ReadElementExtensions<SyndicationElementExtension>("", "extensionNamespace", new DataContractSerializer(typeof(ExtensionObject))));
            AssertExtensions.Throws<ArgumentException>(null, () => elementExtensions.ReadElementExtensions<SyndicationElementExtension>("", "extensionNamespace", new XmlSerializer(typeof(ExtensionObject))));
        }

        [Fact]
        public void Clone_NoExtensions_ReturnsExpected()
        {
            var category = new SyndicationCategory();
            SyndicationElementExtensionCollection elementExtensions = category.ElementExtensions;

            SyndicationElementExtensionCollection clone = category.Clone().ElementExtensions;
            Assert.Empty(clone);
        }

        [Fact]
        public void Clone_WithExtensions_ReturnsExpected()
        {
            var category = new SyndicationCategory();

            SyndicationElementExtensionCollection elementExtensions = category.ElementExtensions;
            elementExtensions.Add(new ExtensionObject { Value = 10 });

            SyndicationElementExtensionCollection clone = category.Clone().ElementExtensions;
            Assert.Equal(1, clone.Count);

            elementExtensions.Clear();
            Assert.Equal(1, clone.Count);
        }

        [DataContract]
        public class ExtensionObject
        {
            [DataMember]
            public int Value { get; set; }
        }

        [DataContract]
        public class OtherExtensionObject
        {
            [DataMember]
            public int Value { get; set; }
        }
    }
}

namespace System.ServiceModel.Syndication.Tests.Inner
{
    [DataContract]
    public class OtherExtensionObject
    {
        [DataMember]
        public int Value { get; set; }
    }
}
