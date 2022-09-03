// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable SYSLIB0047 // XmlSecureResolver is obsolete

namespace System.Xml.Tests
{
    public class XmlSecureResolverTests
    {
        [Fact]
        public void GetEntity_ThrowsXmlException()
        {
            PoisonedXmlResolver innerResolver = new PoisonedXmlResolver();
            XmlSecureResolver outerResolver = new XmlSecureResolver(innerResolver, "some-url");
            Uri absoluteUri = new Uri("https://dot.net/");
            Type typeToReturn = typeof(Stream);

            Assert.Throws<XmlException>(() => outerResolver.GetEntity(absoluteUri, "role", typeToReturn));
            Assert.False(innerResolver.WasAnyApiInvoked);
        }

        [Fact]
        public void GetEntityAsync_ThrowsXmlException()
        {
            PoisonedXmlResolver innerResolver = new PoisonedXmlResolver();
            XmlSecureResolver outerResolver = new XmlSecureResolver(innerResolver, "some-url");
            Uri absoluteUri = new Uri("https://dot.net/");
            Type typeToReturn = typeof(Stream);

            Assert.Throws<XmlException>(() => (object)outerResolver.GetEntityAsync(absoluteUri, "role", typeToReturn));
            Assert.False(innerResolver.WasAnyApiInvoked);
        }

        [Fact]
        public void Instance_HasNoState()
        {
            // This is a safety check to ensure we're not keeping the inner resolver in an instance field,
            // since we don't want to risk invoking it.

            FieldInfo[] allDeclaredInstanceFields = typeof(XmlSecureResolver).GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.Empty(allDeclaredInstanceFields);
        }

        private sealed class PoisonedXmlResolver : XmlResolver
        {
            public bool WasAnyApiInvoked { get; private set; }

            public override ICredentials Credentials
            {
                set
                {
                    WasAnyApiInvoked = true;
                    throw new NotImplementedException();
                }
            }

            public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
            {
                WasAnyApiInvoked = true;
                throw new NotImplementedException();
            }

            public override Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn)
            {
                WasAnyApiInvoked = true;
                throw new NotImplementedException();
            }

            public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
            {
                WasAnyApiInvoked = true;
                throw new NotImplementedException();
            }

            public override bool SupportsType(Uri absoluteUri, Type? type)
            {
                WasAnyApiInvoked = true;
                throw new NotImplementedException();
            }
        }
    }
}
