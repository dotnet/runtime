// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace XDocumentTests.SDMSample
{
    public class SDM_XName
    {
        /// <summary>
        /// Gets an XML qualified name for an XName, for interop.
        /// </summary>
        /// <param name="name">XName.</param>
        /// <returns>XmlQualifiedName.</returns>
        internal static XmlQualifiedName GetQName(XName name)
        {
            return new XmlQualifiedName(name.LocalName, name.Namespace.NamespaceName);
        }

        /// <summary>
        /// Tests trying to use an invalid name with XName.Get.
        /// </summary>
        [Fact]
        public void NameGetInvalid()
        {
            Assert.Throws<ArgumentNullException>(() => XName.Get(null));
            Assert.Throws<ArgumentNullException>(() => XName.Get(null, "foo"));
            Assert.Throws<ArgumentNullException>(() => XName.Get(string.Empty, "foo"));
            AssertExtensions.Throws<ArgumentException>(null, () => XName.Get(string.Empty));
            AssertExtensions.Throws<ArgumentException>(null, () => XName.Get("{}"));
            AssertExtensions.Throws<ArgumentException>(null, () => XName.Get("{foo}"));
        }

        /// <summary>
        /// Tests the operators on XName.
        /// </summary>
        [Fact]
        public void NameOperators()
        {
            // Implicit conversion from string.
            XName name = (XName)(string)null;
            Assert.Null(name);

            name = (XName)"foo";
            Assert.Equal("", name.Namespace.NamespaceName);
            Assert.Equal("foo", name.LocalName);

            name = (XName)"{bar}foo";
            Assert.Equal("bar", name.Namespace.NamespaceName);
            Assert.Equal("foo", name.LocalName);

            // Conversion to XmlQualifiedName
            XmlQualifiedName qname = GetQName(name);
            Assert.Equal("bar", qname.Namespace);
            Assert.Equal("foo", qname.Name);

            // Equality, which should be based on reference equality.
            XName ns1 = (XName)"foo";
            XName ns2 = (XName)"foo";
            XName ns3 = (XName)"bar";
            XName ns4 = null;

            Assert.NotNull(ns1);
            Assert.NotNull(ns2);
            Assert.NotNull(ns3);

            Assert.Same(ns1, ns2);
            Assert.NotSame(ns1, ns3);
            Assert.NotSame(ns2, ns3);

            Assert.True(ns1 == ns2); // equal
            Assert.False(ns1 == ns3); // not equal
            Assert.False(ns1 == ns4); // not equal

            Assert.False(ns1 != ns2); // false
            Assert.True(ns1 != ns3); // true
            Assert.True(ns1 != ns4); // true
        }

        /// <summary>
        /// Tests trying to use an invalid name with XNamespace.Get.
        /// </summary>
        [Fact]
        public void NamespaceGetNull()
        {
            Assert.Throws<ArgumentNullException>(() => XNamespace.Get(null));
        }

        /// <summary>
        /// Tests the operators on XNamespace.
        /// </summary>
        [Fact]
        public void NamespaceOperators()
        {
            // Implicit conversion from string.
            XNamespace ns = (XNamespace)(string)null;
            Assert.Null(ns);

            ns = (XNamespace)"foo";
            Assert.Equal("foo", ns.NamespaceName);

            // Operator +
            XName name;
            Assert.Throws<ArgumentNullException>(() => (XNamespace)null + "localname");
            Assert.Throws<ArgumentNullException>(() => ns + (string)null);

            name = ns + "localname";
            Assert.Equal("localname", name.LocalName);
            Assert.Equal("foo", name.Namespace.NamespaceName);

            // Equality, which should be based on reference equality.
            XNamespace ns1 = (XNamespace)"foo";
            XNamespace ns2 = (XNamespace)"foo";
            XNamespace ns3 = (XNamespace)"bar";
            XNamespace ns4 = null;

            Assert.NotNull(ns1);
            Assert.NotNull(ns2);
            Assert.NotNull(ns3);

            Assert.Same(ns1, ns2);
            Assert.True(ns1.Equals(ns2));

            Assert.NotSame(ns1, ns3);
            Assert.False(ns1.Equals(ns3));

            Assert.NotSame(ns2, ns3);
            Assert.False(ns2.Equals(ns3));

            Assert.True(ns1 == ns2); // equal
            Assert.False(ns1 == ns3); // not equal
            Assert.False(ns1 == ns4); // not equal

            Assert.False(ns1 != ns2); // false
            Assert.True(ns1 != ns3); // true
            Assert.True(ns1 != ns4); // true
        }
    }
}
