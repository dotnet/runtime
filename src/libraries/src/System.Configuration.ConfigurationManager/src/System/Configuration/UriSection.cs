// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public sealed class UriSection : ConfigurationSection
    {
        private static readonly ConfigurationPropertyCollection _properties = new ConfigurationPropertyCollection();

        private static readonly ConfigurationProperty _idn = new ConfigurationProperty(CommonConfigurationStrings.Idn,
            typeof(IdnElement), null, ConfigurationPropertyOptions.None);

        private static readonly ConfigurationProperty _iriParsing = new ConfigurationProperty(
            CommonConfigurationStrings.IriParsing, typeof(IriParsingElement), null, ConfigurationPropertyOptions.None);

        private static readonly ConfigurationProperty _schemeSettings =
            new ConfigurationProperty(CommonConfigurationStrings.SchemeSettings,
            typeof(SchemeSettingElementCollection), null, ConfigurationPropertyOptions.None);

        static UriSection()
        {
            _properties.Add(_idn);
            _properties.Add(_iriParsing);
            _properties.Add(_schemeSettings);
        }

        [ConfigurationProperty(CommonConfigurationStrings.Idn)]
        public IdnElement Idn => (IdnElement)this[_idn];

        [ConfigurationProperty(CommonConfigurationStrings.IriParsing)]
        public IriParsingElement IriParsing => (IriParsingElement)this[_iriParsing];

        [ConfigurationProperty(CommonConfigurationStrings.SchemeSettings)]
        public SchemeSettingElementCollection SchemeSettings => (SchemeSettingElementCollection)this[_schemeSettings];

        protected internal override ConfigurationPropertyCollection Properties => _properties;
    }
}
