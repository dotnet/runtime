// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Configuration
{
    [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
    public sealed class SchemeSettingElement : ConfigurationElement
    {
        private static readonly ConfigurationProperty s_name = new ConfigurationProperty(CommonConfigurationStrings.SchemeName, typeof(string), null,
                ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey);
        private static readonly ConfigurationProperty s_genericUriParserOptions = new ConfigurationProperty(CommonConfigurationStrings.GenericUriParserOptions,
                typeof(GenericUriParserOptions), GenericUriParserOptions.Default,
                ConfigurationPropertyOptions.IsRequired);
        private static readonly ConfigurationPropertyCollection s_properties = new ConfigurationPropertyCollection() { s_name, s_genericUriParserOptions };

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        [ConfigurationProperty(CommonConfigurationStrings.SchemeName,
            DefaultValue = null, IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)this[s_name]; }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
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
