// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;
using System.IO;
using System.Linq;
using Xunit;

namespace System.ConfigurationTests
{
    public class SectionGroupsTests
    {
        [Fact]
        public void RootSectionGroupNotNull()
        {
            using (var temp = new TempConfig(TestData.EmptyConfig))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Assert.NotNull(config.RootSectionGroup);
            }
        }

        public static string EmptySectionGroupConfiguration =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <configSections>
        <sectionGroup name='emptySectionGroup'>
        </sectionGroup>
    </configSections>
</configuration>";

        [Fact]
        public void EmptySectionGroup()
        {
            using (var temp = new TempConfig(EmptySectionGroupConfiguration))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                ConfigurationSectionGroup sectionGroup = config.GetSectionGroup("emptySectionGroup");
                Assert.NotNull(sectionGroup);
                Assert.Empty(sectionGroup.Sections);
                Assert.Empty(sectionGroup.SectionGroups);
            }
        }

        public static string SimpleSectionGroupConfiguration =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <configSections>
        <sectionGroup name='simpleSectionGroup'>
            <section name='fooSection' type='System.Configuration.NameValueSectionHandler, System' />
        </sectionGroup>
    </configSections>
</configuration>";

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/21527", TargetFrameworkMonikers.NetFramework)]
        public void SimpleSectionGroup()
        {
            using (var temp = new TempConfig(SimpleSectionGroupConfiguration))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                ConfigurationSectionGroup sectionGroup = config.GetSectionGroup("simpleSectionGroup");
                Assert.NotNull(sectionGroup);
                Assert.Equal(1, sectionGroup.Sections.Count);
                Assert.Equal("fooSection", sectionGroup.Sections[0].SectionInformation.Name);
                Assert.Equal("System.Configuration.NameValueSectionHandler, System", sectionGroup.Sections[0].SectionInformation.Type);
            }
        }

        private const string SlashNamedSectionConfiguration =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <configSections>
        <sectionGroup name='outer'>
            <sectionGroup name='bad/group' />
            <section name='bad/section' type='System.Configuration.IgnoreSection, System.Configuration.ConfigurationManager' />
        </sectionGroup>
    </configSections>
</configuration>";

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "The package is a partial facade on .NET Framework; the inbox implementation is used.")]
        public void SlashNamedSectionsAndGroupsThrowStoredConfigurationErrors()
        {
            using (var temp = new TempConfig(SlashNamedSectionConfiguration))
            {
                var config = ConfigurationManager.OpenMappedExeConfiguration(
                    new ExeConfigurationFileMap { ExeConfigFilename = temp.ConfigPath },
                    ConfigurationUserLevel.None);

                ConfigurationSectionGroup outerGroup = config.SectionGroups["outer"];
                Assert.NotNull(outerGroup);

                ConfigurationSectionGroupCollection sectionGroups = outerGroup.SectionGroups;
                int slashSectionGroupIndex = Enumerable.Range(0, sectionGroups.Count).Single(i => sectionGroups.GetKey(i) == "bad/group");
                Assert.Throws<ConfigurationErrorsException>(() => sectionGroups["bad/group"]);
                Assert.Throws<ConfigurationErrorsException>(() => sectionGroups[slashSectionGroupIndex]);
                Assert.Throws<ConfigurationErrorsException>(() => sectionGroups.Cast<ConfigurationSectionGroup>().ToArray());
                Assert.Throws<ConfigurationErrorsException>(() => sectionGroups.CopyTo(new ConfigurationSectionGroup[sectionGroups.Count], 0));

                ConfigurationSectionCollection sections = outerGroup.Sections;
                int slashSectionIndex = Enumerable.Range(0, sections.Count).Single(i => sections.GetKey(i) == "bad/section");
                Assert.Throws<ConfigurationErrorsException>(() => sections["bad/section"]);
                Assert.Throws<ConfigurationErrorsException>(() => sections[slashSectionIndex]);
                Assert.Throws<ConfigurationErrorsException>(() => sections.Cast<ConfigurationSection>().ToArray());
                Assert.Throws<ConfigurationErrorsException>(() => sections.CopyTo(new ConfigurationSection[sections.Count], 0));
            }
        }

        private const string NestedSectionGroupConfiguration =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <configSections>
        <sectionGroup name='outer'>
            <sectionGroup name='inner' />
            <section name='leaf' type='System.Configuration.IgnoreSection, System.Configuration.ConfigurationManager' />
        </sectionGroup>
    </configSections>
</configuration>";

        [Fact]
        public void DescendantPathLookupIsRejected()
        {
            using (var temp = new TempConfig(NestedSectionGroupConfiguration))
            {
                var config = ConfigurationManager.OpenMappedExeConfiguration(
                    new ExeConfigurationFileMap { ExeConfigFilename = temp.ConfigPath },
                    ConfigurationUserLevel.None);

                Assert.Null(config.SectionGroups["outer/inner"]);

                ConfigurationSectionGroup outerGroup = config.SectionGroups["outer"];
                Assert.NotNull(outerGroup);
                Assert.Null(outerGroup.Sections["leaf/child"]);
            }
        }
    }
}
