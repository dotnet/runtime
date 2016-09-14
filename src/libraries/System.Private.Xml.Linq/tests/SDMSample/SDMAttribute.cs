// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace XDocumentTests.SDMSample
{
    public class SDM_Attribute
    {
        [Fact]
        public void AttributeConstructor()
        {
            string value = "bar";

            // Name/value constructor.
            Assert.Throws<ArgumentNullException>(() => new XAttribute(null, value));
            Assert.Throws<ArgumentNullException>(() => new XAttribute("foo", null));

            // Codepaths for special-casing xmlns namespace
            XName name = XName.Get("xmlns", string.Empty);
            XAttribute att1 = new XAttribute(name, value);
            Assert.Equal("xmlns", att1.Name.ToString());
            Assert.Equal(value, att1.Value);

            name = XName.Get("xmlns", "namespacename");
            att1 = new XAttribute(name, value);
            Assert.Equal("{namespacename}xmlns", att1.Name.ToString());
            Assert.Equal(value, att1.Value);

            name = XName.Get("foo", string.Empty);
            att1 = new XAttribute(name, value);
            Assert.Equal("foo", att1.Name.ToString());
            Assert.Equal(value, att1.Value);

            name = XName.Get("foo", "namespacename");
            att1 = new XAttribute(name, value);
            Assert.Equal("{namespacename}foo", att1.Name.ToString());
            Assert.Equal(value, att1.Value);

            // Copy constructor.
            Assert.Throws<ArgumentNullException>(() => new XAttribute((XAttribute)null));

            XAttribute att2 = new XAttribute(att1);
            Assert.Equal(att1.Name.ToString(), att2.Name.ToString());
            Assert.Equal(att1.Value, att2.Value);
        }

        /// <summary>
        /// Tests EmptySequence on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeEmptySequence()
        {
            Assert.Empty(XAttribute.EmptySequence);
            Assert.Empty(XAttribute.EmptySequence);
        }

        /// <summary>
        /// Tests IsNamespaceDeclaration on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeIsNamespaceDeclaration()
        {
            XAttribute att1 = new XAttribute("{http://bogus}name", "value");
            XAttribute att2 = new XAttribute("{http://www.w3.org/2000/xmlns/}name", "value");

            Assert.False(att1.IsNamespaceDeclaration);
            Assert.True(att2.IsNamespaceDeclaration);
        }

        /// <summary>
        /// Tests Parent on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeParent()
        {
            XAttribute a = new XAttribute("att-name", "value");
            XElement e = new XElement("elem-name");

            Assert.Null(a.Parent);

            e.Add(a);
            Assert.Equal(e, a.Parent);

            e.RemoveAttributes();
            Assert.Null(a.Parent);
        }

        /// <summary>
        /// Validate behavior of the Value property on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeValue()
        {
            XAttribute a = new XAttribute("foo", 10m);

            Assert.Equal("10", a.Value);
            Assert.Throws<ArgumentNullException>(() => a.Value = null);

            a.Value = "100";
            Assert.Equal("100", a.Value);
        }

        /// <summary>
        /// Validates the behavior of the Remove method on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeRemove()
        {
            XElement e = new XElement("element");
            XAttribute a = new XAttribute("attribute", "value");

            // Can't remove when no parent.
            Assert.Throws<InvalidOperationException>(() => a.Remove());

            e.Add(a);
            Assert.Equal(1, e.Attributes().Count());

            a.Remove();
            Assert.Empty(e.Attributes());
        }

        /// <summary>
        /// Validates the explicit string conversion operator on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeExplicitToString()
        {
            XAttribute e2 = new XAttribute("x", string.Empty);
            XAttribute e3 = new XAttribute("x", 10.0);

            Assert.Null((string)((XAttribute)null));
            Assert.Equal(string.Empty, (string)e2);
            Assert.Equal("10", (string)e3);
        }

        /// <summary>
        /// Validates the explicit boolean conversion operator on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeExplicitToBoolean()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (bool)((XAttribute)null));

            // Test various values.
            XAttribute e1 = new XAttribute("x", string.Empty);
            XAttribute e2 = new XAttribute("x", "bogus");
            XAttribute e3 = new XAttribute("x", "true");
            XAttribute e4 = new XAttribute("x", "false");
            XAttribute e5 = new XAttribute("x", "0");
            XAttribute e6 = new XAttribute("x", "1");

            Assert.Throws<FormatException>(() => (bool)e1);
            Assert.Throws<FormatException>(() => (bool)e2);

            Assert.True((bool)e3);
            Assert.False((bool)e4);
            Assert.False((bool)e5);
            Assert.True((bool)e6);
        }

        /// <summary>
        /// Validates the explicit int32 conversion operator on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeExplicitToInt32()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (int)((XAttribute)null));

            // Test various values.
            XAttribute e1 = new XAttribute("x", string.Empty);
            XAttribute e2 = new XAttribute("x", "bogus");
            XAttribute e3 = new XAttribute("x", "2147483648");
            XAttribute e4 = new XAttribute("x", "5");

            Assert.Throws<FormatException>(() => (int)e1);
            Assert.Throws<FormatException>(() => (int)e2);
            Assert.Throws<OverflowException>(() => (int)e3);

            Assert.Equal(5, (int)e4);
        }

        /// <summary>
        /// Validates the explicit uint32 conversion operator on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeExplicitToUInt32()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (uint)((XAttribute)null));

            // Test various values.
            XAttribute e1 = new XAttribute("x", string.Empty);
            XAttribute e2 = new XAttribute("x", "bogus");
            XAttribute e3 = new XAttribute("x", "4294967296");
            XAttribute e4 = new XAttribute("x", "5");

            Assert.Throws<FormatException>(() => (uint)e1);
            Assert.Throws<FormatException>(() => (uint)e2);
            Assert.Throws<OverflowException>(() => (uint)e3);

            Assert.Equal(5u, (uint)e4);
        }

        /// <summary>
        /// Validates the explicit int64 conversion operator on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeExplicitToInt64()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (long)((XAttribute)null));

            // Test various values.
            XAttribute e1 = new XAttribute("x", string.Empty);
            XAttribute e2 = new XAttribute("x", "bogus");
            XAttribute e3 = new XAttribute("x", "18446744073709551616");
            XAttribute e4 = new XAttribute("x", "5");

            Assert.Throws<FormatException>(() => (long)e1);
            Assert.Throws<FormatException>(() => (long)e2);
            Assert.Throws<OverflowException>(() => (long)e3);

            Assert.Equal(5L, (long)e4);
        }

        /// <summary>
        /// Validates the explicit uint64 conversion operator on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeExplicitToUInt64()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (ulong)((XAttribute)null));

            // Test various values.
            XAttribute e1 = new XAttribute("x", string.Empty);
            XAttribute e2 = new XAttribute("x", "bogus");
            XAttribute e3 = new XAttribute("x", "18446744073709551616");
            XAttribute e4 = new XAttribute("x", "5");

            Assert.Throws<FormatException>(() => (ulong)e1);
            Assert.Throws<FormatException>(() => (ulong)e2);
            Assert.Throws<OverflowException>(() => (ulong)e3);

            Assert.Equal(5UL, (ulong)e4);
        }

        /// <summary>
        /// Validates the explicit float conversion operator on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeExplicitToFloat()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (float)((XAttribute)null));

            // Test various values.
            XAttribute e1 = new XAttribute("x", string.Empty);
            XAttribute e2 = new XAttribute("x", "bogus");
            XAttribute e3 = new XAttribute("x", "5e+500");
            XAttribute e4 = new XAttribute("x", "5.0");

            Assert.Throws<FormatException>(() => (float)e1);
            Assert.Throws<FormatException>(() => (float)e2);
            Assert.Throws<OverflowException>(() => (float)e3);

            Assert.Equal(5.0f, (float)e4);
        }

        /// <summary>
        /// Validates the explicit double conversion operator on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeExplicitToDouble()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (double)((XAttribute)null));

            // Test various values.
            XAttribute e1 = new XAttribute("x", string.Empty);
            XAttribute e2 = new XAttribute("x", "bogus");
            XAttribute e3 = new XAttribute("x", "5e+5000");
            XAttribute e4 = new XAttribute("x", "5.0");

            Assert.Throws<FormatException>(() => (double)e1);
            Assert.Throws<FormatException>(() => (double)e2);
            Assert.Throws<OverflowException>(() => (double)e3);

            Assert.Equal(5.0, (double)e4);
        }

        /// <summary>
        /// Validates the explicit decimal conversion operator on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeExplicitToDecimal()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (decimal)((XAttribute)null));

            // Test various values.
            XAttribute e1 = new XAttribute("x", string.Empty);
            XAttribute e2 = new XAttribute("x", "bogus");
            XAttribute e3 = new XAttribute("x", "111111111111111111111111111111111111111111111111");
            XAttribute e4 = new XAttribute("x", "5.0");

            Assert.Throws<FormatException>(() => (decimal)e1);
            Assert.Throws<FormatException>(() => (decimal)e2);
            Assert.Throws<OverflowException>(() => (decimal)e3);

            Assert.Equal(5.0m, (decimal)e4);
        }

        /// <summary>
        /// Validates the explicit DateTime conversion operator on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeExplicitToDateTime()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (DateTime)((XAttribute)null));

            // Test various values.
            XAttribute e1 = new XAttribute("x", string.Empty);
            XAttribute e2 = new XAttribute("x", "bogus");
            XAttribute e3 = new XAttribute("x", "1968-01-07");

            Assert.Throws<FormatException>(() => (DateTime)e1);
            Assert.Throws<FormatException>(() => (DateTime)e2);

            Assert.Equal(new DateTime(1968, 1, 7), (DateTime)e3);
        }

        /// <summary>
        /// Validates the explicit TimeSpan conversion operator on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeExplicitToTimeSpan()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (TimeSpan)((XAttribute)null));

            // Test various values.
            XAttribute e1 = new XAttribute("x", string.Empty);
            XAttribute e2 = new XAttribute("x", "bogus");
            XAttribute e3 = new XAttribute("x", "PT1H2M3S");

            Assert.Throws<FormatException>(() => (TimeSpan)e1);
            Assert.Throws<FormatException>(() => (TimeSpan)e2);

            Assert.Equal(new TimeSpan(1, 2, 3), (TimeSpan)e3);
        }

        /// <summary>
        /// Validates the explicit guid conversion operator on XAttribute.
        /// </summary>
        [Fact]
        public void AttributeExplicitToGuid()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (Guid)((XAttribute)null));

            string guid = "2b67e9fb-97ad-4258-8590-8bc8c2d32df5";

            // Test various values.
            XAttribute e1 = new XAttribute("x", string.Empty);
            XAttribute e2 = new XAttribute("x", "bogus");
            XAttribute e3 = new XAttribute("x", guid);

            Assert.Throws<FormatException>(() => (Guid)e1);
            Assert.Throws<FormatException>(() => (Guid)e2);

            Assert.Equal(new Guid(guid), (Guid)e3);
        }

        /// <summary>
        /// Validates the explicit conversion operators on XAttribute
        /// for nullable value types.
        /// </summary>
        [Fact]
        public void AttributeExplicitToNullables()
        {
            string guid = "cd8d69ed-fef9-4283-aaf4-216463e4496f";

            Assert.Equal(true, (bool?)new XAttribute("x", true));
            Assert.Equal(5, (int?)new XAttribute("x", 5));
            Assert.Equal(5u, (uint?)new XAttribute("x", 5));
            Assert.Equal(5L, (long?)new XAttribute("x", 5));
            Assert.Equal(5uL, (ulong?)new XAttribute("x", 5));
            Assert.Equal(5.0f, (float?)new XAttribute("x", 5));
            Assert.Equal(5.0, (double?)new XAttribute("x", 5));
            Assert.Equal(5.0m, (decimal?)new XAttribute("x", 5));
            Assert.Equal(new DateTime(1968, 1, 7), (DateTime?)new XAttribute("x", "1968-01-07"));
            Assert.Equal(new TimeSpan(1, 2, 3), (TimeSpan?)new XAttribute("x", "PT1H2M3S"));
            Assert.Equal(new Guid(guid), (Guid?)new XAttribute("x", guid));

            Assert.Null((bool?)((XAttribute)null));
            Assert.Null((int?)((XAttribute)null));
            Assert.Null((uint?)((XAttribute)null));
            Assert.Null((long?)((XAttribute)null));
            Assert.Null((ulong?)((XAttribute)null));
            Assert.Null((float?)((XAttribute)null));
            Assert.Null((double?)((XAttribute)null));
            Assert.Null((decimal?)((XAttribute)null));
            Assert.Null((DateTime?)((XAttribute)null));
            Assert.Null((TimeSpan?)((XAttribute)null));
            Assert.Null((Guid?)((XAttribute)null));
        }
    }
}
