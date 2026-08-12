// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;
using System.IO;
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

        public static string InvalidSectionAndSectionGroupNamesConfiguration =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <configSections>
        <sectionGroup name='outer'>
            <section name='inner/invalid' type='System.Configuration.NameValueSectionHandler, System' />
            <sectionGroup name='innerGroup/invalid' type='Missing.SectionGroup, Missing.Assembly' />
        </sectionGroup>
    </configSections>
</configuration>";

        [Fact]
        public void InvalidSectionNameContainingPathSeparatorThrowsOnAllAccessors()
        {
            using (var temp = new TempConfig(InvalidSectionAndSectionGroupNamesConfiguration))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                ConfigurationSectionCollection sections = config.GetSectionGroup("outer").Sections;

                Assert.Throws<ConfigurationErrorsException>(() => sections["inner/invalid"]);
                Assert.Throws<ConfigurationErrorsException>(() => sections[0]);
                Assert.Throws<ConfigurationErrorsException>(() => sections.GetEnumerator().MoveNext());
                Assert.Throws<ConfigurationErrorsException>(() => sections.CopyTo(new ConfigurationSection[sections.Count], 0));
            }
        }

        [Fact]
        public void InvalidSectionGroupNameContainingPathSeparatorThrowsOnAllAccessors()
        {
            using (var temp = new TempConfig(InvalidSectionAndSectionGroupNamesConfiguration))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                ConfigurationSectionGroupCollection sectionGroups = config.GetSectionGroup("outer").SectionGroups;

                Assert.Throws<ConfigurationErrorsException>(() => sectionGroups["innerGroup/invalid"]);
                Assert.Throws<ConfigurationErrorsException>(() => sectionGroups[0]);
                Assert.Throws<ConfigurationErrorsException>(() => sectionGroups.GetEnumerator().MoveNext());
                Assert.Throws<ConfigurationErrorsException>(() => sectionGroups.CopyTo(new ConfigurationSectionGroup[sectionGroups.Count], 0));
            }
        }

        public static string DescendantSectionAndSectionGroupConfiguration =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <configSections>
        <sectionGroup name='outer'>
            <section name='inner' type='System.Configuration.NameValueSectionHandler, System' />
            <sectionGroup name='innerGroup' />
        </sectionGroup>
    </configSections>
</configuration>";

        [Fact]
        public void DescendantsAreNotAccessibleFromParentCollections()
        {
            using (var temp = new TempConfig(DescendantSectionAndSectionGroupConfiguration))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);

                Assert.Null(config.RootSectionGroup.Sections["outer/inner"]);
                Assert.Null(config.RootSectionGroup.SectionGroups["outer/innerGroup"]);
            }
        }
    }
}
