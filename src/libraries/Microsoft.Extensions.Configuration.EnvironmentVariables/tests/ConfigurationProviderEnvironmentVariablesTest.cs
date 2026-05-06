// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration.Test;

namespace Microsoft.Extensions.Configuration.EnvironmentVariables.Test
{
    public class ConfigurationProviderEnvironmentVariablesTest : ConfigurationProviderTestBase
    {
        protected override (IConfigurationProvider Provider, Action Initializer) LoadThroughProvider(
            TestSection testConfig)
        {
            var values = new List<KeyValuePair<string, string>>();
            SectionToValues(testConfig, "", values);

            var provider = new EnvironmentVariablesConfigurationProvider(null);

            return (provider, () => provider.Load(new Hashtable(values.ToDictionary(e => e.Key, e => e.Value))));
        }

        public override void Load_from_single_provider_with_differing_case_duplicates_throws()
        {
            AssertConfig(BuildConfigRoot(LoadThroughProvider(TestSection.DuplicatesDifferentCaseTestConfig)));
        }

        public override void Null_values_are_included_in_the_config()
        {
            AssertConfig(BuildConfigRoot(LoadThroughProvider(TestSection.NullsTestConfig)), expectNulls: true);
        }
    }
}
