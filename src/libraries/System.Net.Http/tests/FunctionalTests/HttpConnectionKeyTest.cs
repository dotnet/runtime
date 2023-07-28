// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class HttpConnectionKeyTest
    {
        public static IEnumerable<object[]> KeyComponents()
        {
            yield return new object[] { "Https", "localhost", 80, "localhost-ssl", new Uri("http://localhost"), "domain1/userA", false};
            yield return new object[] { "Http", "localhost1", 80, "localhost-ssl", new Uri("http://localhost"), "domain1/userA", false };
            yield return new object[] { "Http", "localhost", 81, "localhost-ssl", new Uri("http://localhost"), "domain1/userA", false };
            yield return new object[] { "Http", "localhost", 80, "localhost-ssl1", new Uri("http://localhost"), "domain1/userA", false };
            yield return new object[] { "Http", "localhost", 80, "localhost-ssl", new Uri("http://localhost1"), "domain1/userA", false };
            yield return new object[] { "Http", "localhost", 80, "localhost-ssl", new Uri("http://localhost"), "domain1/userB", false };
            yield return new object[] { "Http", "localhost", 80, "localhost-ssl", new Uri("http://localhost"), "domain1/userA", true };
        }

        [Theory, MemberData(nameof(KeyComponents))]
        public void Equals_DifferentParameters_ReturnsTrueIfAllEqual(string kindString, string host, int port, string sslHostName, Uri proxyUri, string identity, bool expected)
        {
            Type connectionKindType = Type.GetType("System.Net.Http.HttpConnectionKind, System.Net.Http");
            Type keyType = Type.GetType("System.Net.Http.HttpConnectionPoolManager+HttpConnectionKey, System.Net.Http");
            object referenceKey = Activator.CreateInstance(keyType, Enum.Parse(connectionKindType, "Http"), "localhost", 80, "localhost-ssl", new Uri("http://localhost"), "domain1/userA");
            object actualKey = Activator.CreateInstance(keyType, Enum.Parse(connectionKindType, kindString), host, port, sslHostName, proxyUri, identity);
            Assert.Equal(expected, referenceKey.Equals(actualKey));
        }
    }
}
