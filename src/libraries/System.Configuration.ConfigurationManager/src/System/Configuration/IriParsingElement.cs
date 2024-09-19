// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Configuration
{
    [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
    public sealed class IriParsingElement : ConfigurationElement
    {
        internal const bool EnabledDefaultValue = false;

        private readonly ConfigurationPropertyCollection _properties = new ConfigurationPropertyCollection();

        private readonly ConfigurationProperty _enabled =
            new ConfigurationProperty(CommonConfigurationStrings.Enabled, typeof(bool), EnabledDefaultValue,
                ConfigurationPropertyOptions.None);

        [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
        public IriParsingElement()
        {
            _properties.Add(_enabled);
        }

        protected internal override ConfigurationPropertyCollection Properties
        {
            get
            {
                return _properties;
            }
        }

        [ConfigurationProperty(CommonConfigurationStrings.Enabled, DefaultValue = EnabledDefaultValue)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        public bool Enabled
        {
            get { return (bool)this[_enabled]; }
            set { this[_enabled] = value; }
        }
    }
}
