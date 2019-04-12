// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions.Configuration.CommandLine.Test
{
    public class ConfigurationProviderCommandLineTest : ConfigurationProviderTestBase
    {
        protected override (IConfigurationProvider Provider, Action Initializer) LoadThroughProvider(
            TestSection testConfig)
        {
            var args = new List<string>();
            SectionToArgs(args, "", testConfig);

            var provider = new CommandLineConfigurationProvider(args);

            return (provider, () => { });
        }

        private void SectionToArgs(List<string> args, string sectionName, TestSection section)
        {
            foreach (var tuple in section.Values.SelectMany(e => e.Value.Expand(e.Key)))
            {
                args.Add($"--{sectionName}{tuple.Key}={tuple.Value}");
            }

            foreach (var tuple in section.Sections)
            {
                SectionToArgs(args, sectionName + tuple.Key + ":", tuple.Section);
            }
        }

        [Fact]
        public override void Load_from_single_provider_with_duplicates_throws()
        {
            AssertConfig(BuildConfigRoot(LoadThroughProvider(TestSection.DuplicatesTestConfig)));
        }

        [Fact]
        public override void Load_from_single_provider_with_differing_case_duplicates_throws()
        {
            AssertConfig(BuildConfigRoot(LoadThroughProvider(TestSection.DuplicatesDifferentCaseTestConfig)));
        }
    }
}
