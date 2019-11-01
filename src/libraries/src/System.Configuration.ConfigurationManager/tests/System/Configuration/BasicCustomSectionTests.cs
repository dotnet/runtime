// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using System.IO;
using Xunit;

namespace System.ConfigurationTests
{
    public class BasicCustomSectionTests
    {
        public class SimpleCustomSection : ConfigurationSection
        {
            [ConfigurationProperty("test")]
            public string Test
            {
                get { return (string)this["test"]; }
                set { this["test"] = value; }
            }
        }

        public static string SimpleCustomData =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <configSections>
        <section name='simpleCustomSection' type='System.ConfigurationTests.BasicCustomSectionTests+SimpleCustomSection, System.Configuration.ConfigurationManager.Tests' />
    </configSections>
    <simpleCustomSection />
</configuration>";

        [Fact]
        public void SimpleCustomSectionExists()
        {
            using (var temp = new TempConfig(SimpleCustomData))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                SimpleCustomSection section = config.GetSection("simpleCustomSection") as SimpleCustomSection;
                Assert.NotNull(section);
                Assert.Equal(string.Empty, section.Test);
            }
        }

        public static string SimpleCustomDataWithValue =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <configSections>
        <section name='simpleCustomSection' type='System.ConfigurationTests.BasicCustomSectionTests+SimpleCustomSection, System.Configuration.ConfigurationManager.Tests' />
    </configSections>
    <simpleCustomSection test='Foo' />
</configuration>";

        [Fact]
        public void SimpleCustomSectionWithData()
        {
            using (var temp = new TempConfig(SimpleCustomDataWithValue))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                SimpleCustomSection section = config.GetSection("simpleCustomSection") as SimpleCustomSection;
                Assert.NotNull(section);
                Assert.Equal("Foo", section.Test);
            }
        }

        public class SimpleCustomSectionRequiredValue : ConfigurationSection
        {
            [ConfigurationProperty("test", IsRequired = true)]
            public string Test
            {
                get { return (string)this["test"]; }
                set { this["test"] = value; }
            }
        }

        public static string MissingRequiredData =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <configSections>
        <section name='simpleCustomSectionRequiredValue' type='System.ConfigurationTests.BasicCustomSectionTests+SimpleCustomSectionRequiredValue, System.Configuration.ConfigurationManager.Tests' />
    </configSections>
    <simpleCustomSectionRequiredValue />
</configuration>";

        [Fact]
        public void SimpleCustomSectionMissingRequired()
        {
            using (var temp = new TempConfig(MissingRequiredData))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);

                Assert.Throws<ConfigurationErrorsException>(() => config.GetSection("simpleCustomSectionRequiredValue") as SimpleCustomSectionRequiredValue);
            }
        }

        public class SimpleCustomSectionDefaultValue: ConfigurationSection
        {
            [ConfigurationProperty("test", DefaultValue = "Bar" )]
            public string Test
            {
                get { return (string)this["test"]; }
                set { this["test"] = value; }
            }
        }

        public static string DefaultAppliedData =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <configSections>
        <section name='simpleCustomSectionDefaultValue' type='System.ConfigurationTests.BasicCustomSectionTests+SimpleCustomSectionDefaultValue, System.Configuration.ConfigurationManager.Tests' />
    </configSections>
    <simpleCustomSectionDefaultValue />
</configuration>";

        [Fact]
        public void SimpleCustomSectionWithDefault()
        {
            using (var temp = new TempConfig(DefaultAppliedData))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                SimpleCustomSectionDefaultValue section = config.GetSection("simpleCustomSectionDefaultValue") as SimpleCustomSectionDefaultValue;
                Assert.NotNull(section);
                Assert.Equal("Bar", section.Test);
            }
        }

        public class SimpleDefaultCollectionSection : ConfigurationSection
        {
            [ConfigurationProperty("", IsDefaultCollection = true)]
            public KeyValueConfigurationCollection Settings => (KeyValueConfigurationCollection)base[""];
        }

        public static string SimpleDefaultCollection =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <configSections>
        <section name='simpleDefaultCollectionSection' type='System.ConfigurationTests.BasicCustomSectionTests+SimpleDefaultCollectionSection, System.Configuration.ConfigurationManager.Tests' />
    </configSections>
    <simpleDefaultCollectionSection>
        <add key='Fruit' value='Pear' />
        <add key='Nut' value='Cashew' />
    </simpleDefaultCollectionSection>
</configuration>";

        [Fact]
        public void CustomSectionDefaultCollection()
        {
            using (var temp = new TempConfig(SimpleDefaultCollection))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                SimpleDefaultCollectionSection section = config.GetSection("simpleDefaultCollectionSection") as SimpleDefaultCollectionSection;
                Assert.NotNull(section);

                Assert.Equal(2, section.Settings.Count);
                Assert.Equal("Pear", section.Settings["Fruit"].Value);
                Assert.Equal("Cashew", section.Settings["Nut"].Value);
            }
        }

        public class SimpleCollectionSection : ConfigurationSection
        {
            [ConfigurationProperty("foods", IsDefaultCollection = true)]
            public KeyValueConfigurationCollection Foods => (KeyValueConfigurationCollection)base["foods"];
        }

        public static string SimpleCollection =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <configSections>
        <section name='simpleCollectionSection' type='System.ConfigurationTests.BasicCustomSectionTests+SimpleCollectionSection, System.Configuration.ConfigurationManager.Tests' />
    </configSections>
    <simpleCollectionSection>
        <foods>
            <add key='Fruit' value='Pear' />
            <add key='Nut' value='Cashew' />
        </foods>
    </simpleCollectionSection>
</configuration>";

        [Fact]
        public void CustomSectionCollection()
        {
            using (var temp = new TempConfig(SimpleCollection))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                SimpleCollectionSection section = config.GetSection("simpleCollectionSection") as SimpleCollectionSection;
                Assert.NotNull(section);

                Assert.Equal(2, section.Foods.Count);
                Assert.Equal("Pear", section.Foods["Fruit"].Value);
                Assert.Equal("Cashew", section.Foods["Nut"].Value);
            }
        }
    }
}
