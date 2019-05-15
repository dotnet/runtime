// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration.Test;

namespace Microsoft.Extensions.Configuration.Ini.Test
{
    public class ConfigurationProviderIniTest : ConfigurationProviderTestBase
    {
        protected override (IConfigurationProvider Provider, Action Initializer) LoadThroughProvider(
            TestSection testConfig)
        {
            var iniBuilder = new StringBuilder();
            SectionToIni(iniBuilder, "", testConfig);

            var provider = new IniConfigurationProvider(
                new IniConfigurationSource
                {
                    Optional = true
                });

            var ini = iniBuilder.ToString();
            return (provider, () => provider.Load(TestStreamHelpers.StringToStream(ini)));
        }

        private void SectionToIni(StringBuilder iniBuilder, string sectionName, TestSection section)
        {
            foreach (var tuple in section.Values.SelectMany(e => e.Value.Expand(e.Key)))
            {
                iniBuilder.AppendLine($"{tuple.Key}={tuple.Value}");
            }

            foreach (var tuple in section.Sections)
            {
                iniBuilder.AppendLine($"[{sectionName}{tuple.Key}]");
                SectionToIni(iniBuilder, tuple.Key + ":", tuple.Section);
            }
        }
    }
}
