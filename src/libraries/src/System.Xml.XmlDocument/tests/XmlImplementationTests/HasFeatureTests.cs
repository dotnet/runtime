// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Xunit;

namespace XmlDocumentTests.XmlImplementationTests
{
    public class HasFeatureTests
    {
        // Feature supported by the XmlImplementation
        private const string SupportedFeature = "XML";

        [Fact]
        public static void HasFeatureReturnsTrueForSupportedFeature()
        {
            var target = new XmlImplementation();
            // verify that it's also case-insensitive
            Assert.True(target.HasFeature(SupportedFeature.ToUpper(), null));
            Assert.True(target.HasFeature(SupportedFeature.ToLower(), null));
        }

        [Fact]
        public static void HasFeatureReturnsFalseForUnsupportedFeature()
        {
            var target = new XmlImplementation();
            Assert.False(target.HasFeature("Unsupported", null));
        }

        [Fact]
        public static void HasFeatureReturnsTrueForSupportedVersion()
        {
            var target = new XmlImplementation();
            Assert.True(target.HasFeature(SupportedFeature, null));
            Assert.True(target.HasFeature(SupportedFeature, "1.0"));
            Assert.True(target.HasFeature(SupportedFeature, "2.0"));
        }

        [Fact]
        public static void HasFeatureReturnsFalseForUnsupportedVersion()
        {
            var target = new XmlImplementation();
            Assert.False(target.HasFeature(SupportedFeature, "1.2"));
        }
    }
}
