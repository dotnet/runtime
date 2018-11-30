// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration.Test;

namespace Microsoft.Extensions.Configuration.Xml.Test
{
    public class ConfigurationProviderXmlTest : ConfigurationProviderTestBase
    {
        public override void Combine_after_other_provider()
        {
            // Disabled test due to XML handling of empty section.
        }

        protected override (IConfigurationProvider Provider, Action Initializer) LoadThroughProvider(TestSection testConfig)
        {
            var xmlBuilder = new StringBuilder();
            SectionToXml(xmlBuilder, "settings", testConfig);

            var provider = new XmlConfigurationProvider(
                new XmlConfigurationSource
                {
                    Optional = true
                });

            var xml = xmlBuilder.ToString();
            return (provider, () => provider.Load(TestStreamHelpers.StringToStream(xml)));
        }

        private void SectionToXml(StringBuilder xmlBuilder, string sectionName, TestSection section)
        {
            xmlBuilder.AppendLine($"<{sectionName}>");

            foreach (var tuple in section.Values)
            {
                if (tuple.Value.AsString != null)
                {
                    xmlBuilder.AppendLine($"<{tuple.Key}>{tuple.Value.AsString}</{tuple.Key}>");
                }
                else
                {
                    for (var i = 0; i < tuple.Value.AsArray.Length; i++)
                    {
                        xmlBuilder.AppendLine($"<{tuple.Key} Name=\"{i}\">{tuple.Value.AsArray[i]}</{tuple.Key}>");
                    }
                }
            }

            foreach (var tuple in section.Sections)
            {
                SectionToXml(xmlBuilder, tuple.Key, tuple.Section);
            }

            xmlBuilder.AppendLine($"</{sectionName}>");
        }
    }
}
