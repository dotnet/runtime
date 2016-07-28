// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Xml.Linq.Tests
{
    public class XHashtableAPI
    {
        [Fact]
        public void NoneNamespaceSameNameElements()
        {
            var a = new XElement(XNamespace.None + "Name");
            var b = new XElement(XNamespace.None + "Name");
            Assert.Same(XNamespace.None, b.Name.Namespace);
            Assert.Same(a.Name.Namespace, b.Name.Namespace);
            Assert.Same(a.Name, b.Name);
        }

        [Fact]
        public void XmlNamespaceDifferentNameElements()
        {
            var a = new XElement(XNamespace.Xml + "NameOne");
            var b = new XElement(XNamespace.Xml + "NameTwo");
            Assert.Same(XNamespace.Xml, b.Name.Namespace);
            Assert.Same(a.Name.Namespace, b.Name.Namespace);
            Assert.NotSame(a.Name, b.Name);
        }

        [Fact]
        public void XmlnsNamespaceSameNameElements()
        {
            var a = new XElement(XNamespace.Xmlns + "Name");
            var b = new XElement(XNamespace.Xmlns + "Name");
            Assert.Same(XNamespace.Xmlns, a.Name.Namespace);
            Assert.Same(a.Name.Namespace, b.Name.Namespace);
            Assert.Same(a.Name, b.Name);
        }

        [Fact]
        public void DefaultNamespaceDifferentNameElements()
        {
            var a = new XElement("NameOne");
            var b = new XElement("NameTwo");
            Assert.Same(a.Name.Namespace, b.Name.Namespace);
            Assert.NotSame(a.Name, b.Name);
        }

        [Fact]
        public void SameNamespaceSameNameAttributes()
        {
            var a = new XAttribute("{Namespace}Name", "a");
            var b = new XAttribute("{Namespace}Name", "b");
            Assert.Same(a.Name.Namespace, b.Name.Namespace);
            Assert.Same(a.Name, b.Name);
        }

        [Fact]
        public void SameNamespaceDifferentNamesElements()
        {
            var a = new XElement("{NameSpace}NameOne");
            var b = new XElement("{NameSpace}NameTwo");
            Assert.Same(a.Name.Namespace, b.Name.Namespace);
            Assert.NotSame(a.Name, b.Name);
        }

        [Fact]
        public void DifferentNamespacesSameNameAttributes()
        {
            XAttribute a = new XAttribute("{NamespaceOne}Name", "a"), b = new XAttribute("{NamespaceTwo}Name", "b");
            Assert.NotSame(a.Name.Namespace, b.Name.Namespace);
            Assert.NotSame(a.Name, b.Name);
        }

        [Fact]
        public void DifferentNamespacesAndNamesElements()
        {
            XElement a = new XElement("{NameSpaceOne}NameOne"), b = new XElement("{NameSpaceTwo}NameTwo");
            Assert.NotSame(a.Name.Namespace, b.Name.Namespace);
            Assert.NotSame(a.Name, b.Name);
        }

        [Fact]
        public void SameDocumentDefaultNamespaceSameNameElements()
        {
            XDocument xDoc = new XDocument(new XElement("Name", new XElement("Name")));
            Assert.Same(
                (xDoc.Nodes().First() as XElement).Name.Namespace,
                (xDoc.Nodes().Last() as XElement).Name.Namespace);
            Assert.Same((xDoc.Nodes().First() as XElement).Name, (xDoc.Nodes().Last() as XElement).Name);
        }

        [Fact]
        public void DifferentDocumentSameNamespaceSameNameElements()
        {
            XDocument xDoc1 = new XDocument(new XElement("{Namespace}Name"));
            XDocument xDoc2 = new XDocument(new XElement("{Namespace}Name"));
            Assert.Same((xDoc1.FirstNode as XElement).Name.Namespace, (xDoc2.FirstNode as XElement).Name.Namespace);
            Assert.Same((xDoc1.FirstNode as XElement).Name, (xDoc2.FirstNode as XElement).Name);
        }

        [Fact]
        public void ImplicitSameName()
        {
            XElement a = new XElement("Name");
            XName xName = "Name";
            Assert.Same(a.Name.Namespace, xName.Namespace);
            Assert.Same(a.Name, xName);
        }

        [Fact]
        public void ImplicitDifferentName()
        {
            XElement a = new XElement("OneName");
            XName xName = "OtherName";
            Assert.Same(a.Name.Namespace, xName.Namespace);
            Assert.NotSame(a.Name, xName);
        }

        [Fact]
        public void ExplicitSameName()
        {
            XElement a = new XElement((XName)"Name");
            XName xName = (XName)"Name";
            Assert.Same(a.Name.Namespace, xName.Namespace);
            Assert.Same(a.Name, xName);
        }

        [Fact]
        public void ExplicitDifferentName()
        {
            XElement a = new XElement((XName)"OneName");
            XName xName = (XName)"OtherName";
            Assert.Same(a.Name.Namespace, xName.Namespace);
            Assert.NotSame(a.Name, xName);
        }

        [Fact]
        public void ExplicitSameHashcodeElements()
        {
            XElement a = new XElement((XName)"{A2WVL}A2WVK");
            XName xName = (XName)"{A2WVK}A2WVL";
            Assert.NotSame(a.Name.Namespace, xName.Namespace);
            Assert.NotSame(a.Name, xName);
        }

        [Fact]
        public void DifferentNameSameHashcodeElements()
        {
            XElement a = new XElement("A2WVL"), b = new XElement("A2WVK");
            Assert.Same(a.Name.Namespace, b.Name.Namespace);
            Assert.NotSame(a.Name, b.Name);
        }

        [Fact]
        public void DifferentNameNoneNamespaceSameHashcodeElements()
        {
            var a = new XElement(XNamespace.None + "A2WVL");
            var b = new XElement(XNamespace.None + "A2WVK");
            Assert.Same(a.Name.Namespace, b.Name.Namespace);
            Assert.NotSame(a.Name, b.Name);
        }

        [Fact]
        public void DifferentNamespaceAndNameSameHashcodeElements()
        {
            XElement a = new XElement("{A2WVL}A2WVK"), b = new XElement("{A2WVK}A2WVL");
            Assert.NotSame(a.Name.Namespace, b.Name.Namespace);
            Assert.NotSame(a.Name, b.Name);
        }
    }
}
