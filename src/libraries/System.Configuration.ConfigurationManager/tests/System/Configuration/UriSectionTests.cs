// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class UriSectionTests
    {
        public static string UriSectionConfiguration =
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

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "dotnet/corefx #19341")]
        public void UriSectionIdnIriParsing()
        {
            using (var temp = new TempConfig(UriSectionConfiguration))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                UriSection uriSection = (UriSection)config.GetSection("uri");
                Assert.Equal(UriIdnScope.All, uriSection.Idn.Enabled);
                Assert.Equal(true, uriSection.IriParsing.Enabled);
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "dotnet/corefx #19341")]
        public void UriSectionSchemeSettings()
        {
            using (var temp = new TempConfig(UriSectionConfiguration))
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