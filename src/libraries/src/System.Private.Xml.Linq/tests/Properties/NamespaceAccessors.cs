// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Linq.Tests
{
    public class NamespaceAccessors
    {
        // GetDefaultNamespace ()
        //  ~ No namespace def
        //  ~ Default namespace
        //      ~ in scope
        //      ~ local
        //      ~ redefined
        //      ~ undefined
        //  ~ sanity - DML: adding/removing xmlns attribute

        [Theory]
        [InlineData("<A><B xmlns='nsa'/></A>", "nsa")]
        [InlineData("<A xmlns='nsa'><B/></A>", "nsa")]
        [InlineData("<A xmlns='nsb'><B xmlns='nsa'/></A>", "nsa")]
        [InlineData("<A xmlns='nsb'><B xmlns=''/></A>", "")]
        [InlineData("<A><B/></A>", "")]
        [InlineData("<A><C xmlns='nsc'/><B/></A>", "")]
        public void GetDefaultNamespace(string xml, string defNSString)
        {
            XNamespace defNS = defNSString == null ? null : XNamespace.Get(defNSString);
            XElement e = XElement.Parse(xml);
            XNamespace df = ((XElement)e.LastNode).GetDefaultNamespace();
            Assert.Same(defNS, df);
            Assert.True(defNS.Equals(df));
        }

        [Fact]
        public void GetDefaultNamespaceDMLSanity()
        {
            XElement e = new XElement(
                "A",
                new XAttribute("xmlns", "nsA"),
                new XElement("B", new XAttribute("xmlns", "nsB1")),
                new XElement("B", new XAttribute("xmlns", "nsB2")));

            XElement x = e.LastNode as XElement;
            Assert.Equal(XNamespace.Get("nsB2"), x.GetDefaultNamespace());

            x.FirstAttribute.Value = "nsX1";
            Assert.Equal(XNamespace.Get("nsX1"), x.GetDefaultNamespace());

            x.FirstAttribute.Remove();
            Assert.Equal(XNamespace.Get("nsA"), x.GetDefaultNamespace());

            x.Parent.FirstAttribute.Value = "nsA1";
            Assert.Equal(XNamespace.Get("nsA1"), x.GetDefaultNamespace());

            x.Add(new XAttribute("xmlns", "nsX2"));
            Assert.Equal(XNamespace.Get("nsX2"), x.GetDefaultNamespace());
        }

        // GetNamespaceOfPrefix ()
        //  ~ Not declared prefix
        //      ~ Not existing
        //      ~ Existing but not in the context
        //  ~ Declared
        //      ~ used, not used
        //      ~ Locally
        //      ~ On parent
        //          ~ Definition
        //          ~ Redefinition
        //  ~ Null, String.Empty

        [Theory]
        [InlineData("<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", "p", "nsc")]
        [InlineData("<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", "P", null)]
        [InlineData("<a:A xmlns:a='a'><C xmlns:p='nsc'/><B/></a:A>", "p", "nsc")]
        [InlineData("<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S/></p:C><B/></A>", "p", "nsc")]
        [InlineData("<A xmlns:p='nsc'><C xmlns:a='a'/><B/></A>", "p", "nsc")]
        [InlineData("<p:A xmlns:p='nsc' xmlns:a='a'><C/><B/></p:A>", "p", "nsc")]
        [InlineData("<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "p", "nsc")]
        [InlineData("<a:A xmlns:a='a' xmlns:p='redefme'><C xmlns:p='nsc'/><B/></a:A>", "p", "nsc")]
        [InlineData("<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "X", null)]
        [InlineData("<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "xml", "http://www.w3.org/XML/1998/namespace")]
        [InlineData("<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "xmlns", "http://www.w3.org/2000/xmlns/")]
        [InlineData("<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", "X", null)]
        public void NamespaceForPrefix(string xml, string prefix, string ns)
        {
            XNamespace NS = ns == null ? null : XNamespace.Get(ns);
            XElement e = XElement.Parse(xml);
            XNamespace df = (e.FirstNode as XElement).GetNamespaceOfPrefix(prefix);
            Assert.Same(NS, df);
            if (NS != null)
            {
                Assert.True(NS.Equals(df));
            }
        }

        [Theory]
        [InlineData("<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", null, null, typeof(ArgumentNullException))]
        [InlineData("<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", "", null, typeof(ArgumentException))]
        public void NamespaceForPrefixNull(string xml, string prefix, string ns, Type expectedException)
        {
            XElement e = XElement.Parse(xml);
            Assert.Throws(expectedException, () => (e.FirstNode as XElement).GetNamespaceOfPrefix(prefix));
        }

        [Fact]
        public void NamespaceForPrefixDMLSanity()
        {
            XElement e = new XElement(
                "A",
                new XAttribute(XNamespace.Xmlns + "b", "nsA"),
                new XElement("{nsA}B", new XAttribute(XNamespace.Xmlns + "b", "nsB1")),
                new XElement("{nsB2}B", new XAttribute(XNamespace.Xmlns + "b", "nsB2")));

            XElement b1 = e.FirstNode as XElement;
            XElement b2 = e.LastNode as XElement;

            Assert.Equal("nsB2", b2.GetNamespaceOfPrefix("b").NamespaceName);

            b2.Attribute(XNamespace.Xmlns + "b").Value = "nsNEW";
            Assert.Equal("nsNEW", b2.GetNamespaceOfPrefix("b").NamespaceName);

            b2.Attribute(XNamespace.Xmlns + "b").Remove();
            Assert.Equal("nsA", b2.GetNamespaceOfPrefix("b").NamespaceName);

            b2.Add(new XAttribute(XNamespace.Xmlns + "p", "nsP"));
            Assert.Equal("nsP", b2.GetNamespaceOfPrefix("p").NamespaceName);

            b2.Add(new XAttribute(XNamespace.Xmlns + "b", "nsADD"));
            Assert.Equal("nsADD", b2.GetNamespaceOfPrefix("b").NamespaceName);
        }


        // GetPrefixForNamespace ()
        //  ~ Not declared namespace
        //  ~ Default namespace
        //  ~ Namespace defined
        //      ~ locally
        //      ~ on parent (in scope)
        //      ~ redefined
        //  ~ Null, String.Empty

        [Theory]
        [InlineData("<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", "p", "nsc")]
        [InlineData("<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", null, "NSC")]
        [InlineData("<a:A xmlns:a='a'><C xmlns:p='nsc'/><B/></a:A>", "p", "nsc")]
        [InlineData("<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S/></p:C><B/></A>", "p", "nsc")]
        [InlineData("<A xmlns:p='nsc'><C xmlns:a='a'/><B/></A>", "p", "nsc")]
        [InlineData("<p:A xmlns:p='nsc' xmlns:a='a'><C/><B/></p:A>", "p", "nsc")]
        [InlineData("<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "p", "nsc")]
        [InlineData("<a:A xmlns:a='a' xmlns:p='redefme'><C xmlns:p='nsc'/><B/></a:A>", "p", "nsc")]
        [InlineData("<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", null, "x")]
        [InlineData("<A xmlns='x'><p:C xmlns:p='nsc'/><B/></A>", null, "")]
        [InlineData("<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", null, "nonexisting")]
        [InlineData("<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "xml", "http://www.w3.org/XML/1998/namespace")]
        [InlineData("<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'/><B/></A>", "xmlns", "http://www.w3.org/2000/xmlns/")]
        [InlineData("<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", null, "nonexisting")]
        public void PrefixOfNamespace(string xml, string prefix, string ns)
        {
            XElement e = XElement.Parse(xml);
            string pref = (e.FirstNode as XElement).GetPrefixOfNamespace(XNamespace.Get(ns));
            Assert.Equal(prefix, pref);
        }

        [Theory]
        [InlineData("<A xmlns='x' xmlns:p='redefme'><p:C xmlns:p='nsc'><X:Sub xmlns:X='xx'/></p:C><B/></A>", null, null)]
        public void PrefixOfNamespaceNull(string xml, string prefix, string ns)
        {
            XElement e = XElement.Parse(xml);
            Assert.Throws<ArgumentNullException>(() => (e.FirstNode as XElement).GetPrefixOfNamespace(ns));
        }

        [Fact]
        public void PrefixOfNamespaceDMLSanity()
        {
            XElement e = new XElement(
                "A",
                new XAttribute(XNamespace.Xmlns + "p", "nsA"),
                new XElement("{nsA}B", new XAttribute(XNamespace.Xmlns + "b", "nsA")),
                new XElement("{nsB2}B", new XAttribute(XNamespace.Xmlns + "b", "nsA")));

            XElement b1 = e.FirstNode as XElement;
            XElement b2 = e.LastNode as XElement;

            Assert.Equal("b", b2.GetPrefixOfNamespace("nsA")); // original value

            b2.Attribute(XNamespace.Xmlns + "b").Value = "nsNEW";
            Assert.Equal("b", b2.GetPrefixOfNamespace("nsNEW")); // value change

            b2.Attribute(XNamespace.Xmlns + "b").Remove();
            Assert.Equal("p", b2.GetPrefixOfNamespace("nsA")); // removed value, take one from parent

            b2.Add(new XAttribute(XNamespace.Xmlns + "nn", "nsQ"));
            Assert.Equal("nn", b2.GetPrefixOfNamespace("nsQ")); // New add - brand new

            b2.Add(new XAttribute(XNamespace.Xmlns + "n1", "nsA"));
            Assert.Equal("n1", b2.GetPrefixOfNamespace("nsA")); // New add - redefine
        }
    }
}
