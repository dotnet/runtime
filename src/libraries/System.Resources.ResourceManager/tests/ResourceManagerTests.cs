// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;

namespace System.Resources.Tests
{
    public static class ResourceManagerTests
    {
        public static IEnumerable<object[]> EnglishResourceData()
        {
            yield return new object[] { "One", "Value-One" };
            yield return new object[] { "Two", "Value-Two" };
            yield return new object[] { "Three", "Value-Three" };
            yield return new object[] { "Empty", "" };
            yield return new object[] { "InvalidKeyName", null };
        }

        [Theory]
        [MemberData(nameof(EnglishResourceData))]
        public static void GetString_Basic(string key, string expectedValue)
        {
            ResourceManager resourceManager = new ResourceManager("System.Resources.Tests.Resources.TestResx", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            string actual = resourceManager.GetString(key);
            Assert.Equal(expectedValue, actual);
        }

        [Theory]
        [MemberData(nameof(EnglishResourceData))]
        public static void GetString_FromResourceType(string key, string expectedValue)
        {
            Type resourceType = typeof(Resources.TestResx);
            ResourceManager resourceManager = new ResourceManager(resourceType);
            string actual = resourceManager.GetString(key);
            Assert.Equal(expectedValue, actual);
        }
    }
}
