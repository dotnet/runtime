// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Microsoft.Build.Evaluation;
using Xunit;

namespace ILLink.Tasks.Tests
{
    public class ILinkTargetsTests
    {
        private static string TargetsFilePath =>
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "build",
                "Microsoft.NET.ILLink.targets");

        private static string EvaluateProperty(string propertyName, Dictionary<string, string> globalProperties)
        {
            string projectXml = $"""
                <Project>
                  <Import Project="{TargetsFilePath}" />
                </Project>
                """;

            using var reader = XmlReader.Create(new StringReader(projectXml));
            using var collection = new ProjectCollection();
            var project = collection.LoadProject(reader, globalProperties, toolsVersion: null);
            return project.GetPropertyValue(propertyName);
        }

        [Theory]
        [InlineData("Release", "false")]
        [InlineData("Debug", "")]
        public void StartupHookSupport_IsDisabledOnlyForNonDebug(string configuration, string expectedValue)
        {
            var properties = new Dictionary<string, string>
            {
                ["PublishTrimmed"] = "true",
                ["Configuration"] = configuration,
            };

            Assert.Equal(expectedValue, EvaluateProperty("StartupHookSupport", properties));
        }

        [Fact]
        public void StartupHookSupport_IsNotSetWhenPublishTrimmedIsFalse()
        {
            var properties = new Dictionary<string, string>
            {
                ["PublishTrimmed"] = "false",
                ["Configuration"] = "Release",
            };

            Assert.Equal("", EvaluateProperty("StartupHookSupport", properties));
        }

        [Fact]
        public void StartupHookSupport_RespectsExplicitValue()
        {
            var properties = new Dictionary<string, string>
            {
                ["PublishTrimmed"] = "true",
                ["Configuration"] = "Release",
                ["StartupHookSupport"] = "true",
            };

            Assert.Equal("true", EvaluateProperty("StartupHookSupport", properties));
        }

        [Theory]
        [InlineData("Release")]
        [InlineData("Debug")]
        public void MetadataUpdaterSupport_IsNotSetByILLinkTargets(string configuration)
        {
            var properties = new Dictionary<string, string>
            {
                ["PublishTrimmed"] = "true",
                ["Configuration"] = configuration,
            };

            // MetadataUpdaterSupport should not be set by Microsoft.NET.ILLink.targets;
            // it is handled by the SDK (Microsoft.NET.Sdk.targets) which only disables
            // it for non-Debug builds.
            Assert.Equal("", EvaluateProperty("MetadataUpdaterSupport", properties));
        }
    }
}
