// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using Xunit;

namespace System.Resources.Tests
{
    public static partial class ResourceManagerTests
    {
        [Fact]
        public static void HeaderVersionNumber()
        {
            Assert.Equal(1, ResourceManager.HeaderVersionNumber);
        }

        [Fact]
        public static void MagicNumber()
        {
            Assert.Equal(unchecked((int)0xBEEFCACE), ResourceManager.MagicNumber);
        }

        [Fact]
        public static void UsingResourceSet()
        {
            var resourceManager = new ResourceManager("System.Resources.Tests.Resources.TestResx", typeof(ResourceManagerTests).GetTypeInfo().Assembly, typeof(ResourceSet));
            Assert.Equal(typeof(ResourceSet), resourceManager.ResourceSetType);
        }

        [Fact]
        public static void BaseName()
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            Assert.Equal("System.Resources.Tests.Resources.TestResx", manager.BaseName);
        }

        [Theory]
        [MemberData(nameof(EnglishResourceData))]
        public static void IgnoreCase(string key, string expectedValue)
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            var culture = new CultureInfo("en-US");
            Assert.False(manager.IgnoreCase);
            Assert.Equal(expectedValue, manager.GetString(key, culture));
            Assert.Null(manager.GetString(key.ToLower(), culture));
            manager.IgnoreCase = true;
            Assert.Equal(expectedValue, manager.GetString(key, culture));
            Assert.Equal(expectedValue, manager.GetString(key.ToLower(), culture));
        }

        public static IEnumerable<object[]> EnglishNonStringResourceData()
        {
            yield return new object[] { "Int", 42 };
            yield return new object[] { "Float", 3.14159 };
            yield return new object[] { "Bytes", new byte[] { 41, 42, 43, 44, 192, 168, 1, 1 } };
            yield return new object[] { "InvalidKeyName", null };
        }

        [Theory]
        [MemberData(nameof(EnglishNonStringResourceData))]
        [ActiveIssue(12565, TestPlatforms.AnyUnix)]
        public static void GetObject(string key, object expectedValue)
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx.netstandard17", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            Assert.Equal(expectedValue, manager.GetObject(key));
            Assert.Equal(expectedValue, manager.GetObject(key, new CultureInfo("en-US")));
        }

        [Theory]
        [MemberData(nameof(EnglishResourceData))]
        public static void GetResourceSet_Strings(string key, string expectedValue)
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            var culture = new CultureInfo("en-US");
            ResourceSet set = manager.GetResourceSet(culture, true, true);
            Assert.Equal(expectedValue, set.GetString(key));
        }

        [Theory]
        [MemberData(nameof(EnglishNonStringResourceData))]
        [ActiveIssue(12565, TestPlatforms.AnyUnix)]
        public static void GetResourceSet_NonStrings(string key, object expectedValue)
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx.netstandard17", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            var culture = new CultureInfo("en-US");
            ResourceSet set = manager.GetResourceSet(culture, true, true);
            Assert.Equal(expectedValue, set.GetObject(key));
        }

        [Fact]
        [ActiveIssue(12565, TestPlatforms.AnyUnix)]
        public static void GetStream()
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx.netstandard17", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            var culture = new CultureInfo("en-US");
            var expectedBytes = new byte[] { 41, 42, 43, 44, 192, 168, 1, 1 };
            using (Stream stream = manager.GetStream("ByteStream"))
            {
                foreach (byte b in expectedBytes)
                {
                    Assert.Equal(b, stream.ReadByte());
                }
            }
            using (Stream stream = manager.GetStream("ByteStream", culture))
            {
                foreach (byte b in expectedBytes)
                {
                    Assert.Equal(b, stream.ReadByte());
                }
            }
        }
    }
}
