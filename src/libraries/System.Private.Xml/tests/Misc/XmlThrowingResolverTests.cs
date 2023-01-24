// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Xml.Tests
{
    public class XmlThrowingResolverTests
    {
        [Fact]
        public void PropertyAccessor_ReturnsSingleton()
        {
            XmlResolver resolver1 = XmlResolver.ThrowingResolver;
            Assert.NotNull(resolver1);

            XmlResolver resolver2 = XmlResolver.ThrowingResolver;
            Assert.Same(resolver1, resolver2);
            Assert.Equal(resolver1, resolver2); // default comparer should also say they're equal
        }

        [Fact]
        public void GetEntity_ThrowsXmlException()
        {
            XmlResolver resolver = XmlResolver.ThrowingResolver;
            Uri absoluteUri = new Uri("https://dot.net/");
            Type typeToReturn = typeof(Stream);

            Assert.Throws<XmlException>(() => resolver.GetEntity(absoluteUri, "role", typeToReturn));
        }

        [Fact]
        public void GetEntityAsync_ThrowsXmlException()
        {
            XmlResolver resolver = XmlResolver.ThrowingResolver;
            Uri absoluteUri = new Uri("https://dot.net/");
            Type typeToReturn = typeof(Stream);

            Assert.Throws<XmlException>(() => (object)resolver.GetEntityAsync(absoluteUri, "role", typeToReturn));
        }
    }
}
