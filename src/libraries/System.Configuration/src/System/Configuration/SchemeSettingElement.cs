// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public sealed class SchemeSettingElement : ConfigurationElement
    {
        private static readonly ConfigurationPropertyCollection s_properties;
        private static readonly ConfigurationProperty s_name;
        private static readonly ConfigurationProperty s_genericUriParserOptions;

        static SchemeSettingElement()
        {
            s_name = new ConfigurationProperty(CommonConfigurationStrings.SchemeName, typeof(string), null,
                ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey);

            s_genericUriParserOptions = new ConfigurationProperty(CommonConfigurationStrings.GenericUriParserOptions,
                typeof(GenericUriParserOptions), GenericUriParserOptions.Default,
                ConfigurationPropertyOptions.IsRequired);

            s_properties = new ConfigurationPropertyCollection();
            s_properties.Add(s_name);
            s_properties.Add(s_genericUriParserOptions);
        }

        [ConfigurationProperty(CommonConfigurationStrings.SchemeName,
            DefaultValue = null, IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)this[s_name]; }
        }

        [ConfigurationProperty(CommonConfigurationStrings.GenericUriParserOptions,
            DefaultValue = ConfigurationPropertyOptions.None, IsRequired = true)]
        public GenericUriParserOptions GenericUriParserOptions
        {
            get { return (GenericUriParserOptions)this[s_genericUriParserOptions]; }
        }

        protected internal override ConfigurationPropertyCollection Properties
        {
            get { return s_properties; }
        }
    }
}
