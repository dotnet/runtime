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
        public override void Combine_before_other_provider()
        {
            // Disabled test due to XML handling of empty section.
        }

        public override void Combine_after_other_provider()
        {
            // Disabled test due to XML handling of empty section.
        }

        public override void Has_debug_view()
        {
            var configRoot = BuildConfigRoot(LoadThroughProvider(TestSection.TestConfig));
            var providerTag = configRoot.Providers.Single().ToString();

            var expected =
                $@"Key1=Value1 ({providerTag})
Section1:
  Key2=Value12 ({providerTag})
  Section2:
    Key3=Value123 ({providerTag})
    Key3a:
      0=ArrayValue0 ({providerTag})
        Name=0 ({providerTag})
      1=ArrayValue1 ({providerTag})
        Name=1 ({providerTag})
      2=ArrayValue2 ({providerTag})
        Name=2 ({providerTag})
Section3:
  Section4:
    Key4=Value344 ({providerTag})
";

            AssertDebugView(configRoot, expected);
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
                if (tuple.Value.AsArray == null)
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
