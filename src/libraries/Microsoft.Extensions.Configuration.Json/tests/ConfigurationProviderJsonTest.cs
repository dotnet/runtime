// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Test;

namespace Microsoft.Extensions.Configuration
{
    public class ConfigurationProviderJsonTest : ConfigurationProviderTestBase
    {
        public override void Load_from_single_provider_with_duplicates_throws()
        {
            // JSON provider doesn't throw for duplicate values with the same case
            AssertConfig(BuildConfigRoot(LoadThroughProvider(TestSection.DuplicatesTestConfig)));
        }

        protected override (IConfigurationProvider Provider, Action Initializer) LoadThroughProvider(TestSection testConfig)
        {
            var jsonBuilder = new StringBuilder();
            SectionToJson(jsonBuilder, testConfig);

            var provider = new JsonConfigurationProvider(
                new JsonConfigurationSource
                {
                    Optional = true
                });

            var json = jsonBuilder.ToString();

            return (provider, () => provider.Load(TestStreamHelpers.StringToStream(json)));
        }

        private void SectionToJson(StringBuilder jsonBuilder, TestSection section)
        {
            jsonBuilder.AppendLine("{");

            foreach (var tuple in section.Values)
            {
                jsonBuilder.AppendLine(tuple.Value.AsArray != null
                    ? $"'{tuple.Key}': [{string.Join(", ", tuple.Value.AsArray.Select(v => $"'{v}'"))}],"
                    : $"'{tuple.Key}': '{tuple.Value.AsString}',");
            }

            foreach (var tuple in section.Sections)
            {
                jsonBuilder.Append($"'{tuple.Key}': ");
                SectionToJson(jsonBuilder, tuple.Section);
            }

            jsonBuilder.AppendLine("},");
        }
    }
}
