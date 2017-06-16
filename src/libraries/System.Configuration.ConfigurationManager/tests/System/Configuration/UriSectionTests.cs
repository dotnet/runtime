// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class UriSectionTests
    {
        // In core we don't include framework configuration objects in the implicit
        // machine.config as they don't have any function.
        public static string UriSectionConfiguration_Core =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <configSections>
        <section name='uri' type='System.Configuration.UriSection, System' />
    </configSections>
    <uri>
        <idn enabled='All' />
        <iriParsing enabled='true' />
        <schemeSettings>
            <add name='ftp' genericUriParserOptions='DontCompressPath' />
        </schemeSettings>
    </uri>
</configuration>";

        public static string UriSectionConfiguration_NetFX =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <uri>
        <idn enabled='All' />
        <iriParsing enabled='true' />
        <schemeSettings>
            <add name='ftp' genericUriParserOptions='DontCompressPath' />
        </schemeSettings>
    </uri>
</configuration>";

        [Fact]
        [ActiveIssue(21000, TargetFrameworkMonikers.UapAot)]
        public void UriSectionIdnIriParsing()
        {
            using (var temp = new TempConfig(PlatformDetection.IsFullFramework ? UriSectionConfiguration_NetFX : UriSectionConfiguration_Core))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                UriSection uriSection = (UriSection)config.GetSection("uri");
                Assert.Equal(UriIdnScope.All, uriSection.Idn.Enabled);
                Assert.Equal(true, uriSection.IriParsing.Enabled);
            }
        }

        [Fact]
        [ActiveIssue(21000, TargetFrameworkMonikers.UapAot)]
        public void UriSectionSchemeSettings()
        {
            using (var temp = new TempConfig(PlatformDetection.IsFullFramework ? UriSectionConfiguration_NetFX : UriSectionConfiguration_Core))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                UriSection uriSection = (UriSection)config.GetSection("uri");
                Assert.Equal(1, uriSection.SchemeSettings.Count);
                SchemeSettingElement schemeSettingElement = uriSection.SchemeSettings[0];
                Assert.Equal("ftp", schemeSettingElement.Name);
                Assert.Equal(GenericUriParserOptions.DontCompressPath, schemeSettingElement.GenericUriParserOptions);
            }
        }
    }
}
