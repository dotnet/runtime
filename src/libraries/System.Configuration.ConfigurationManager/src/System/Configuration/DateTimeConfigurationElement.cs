// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Configuration
{
    [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
    internal sealed class DateTimeConfigurationElement : ConfigurationElement
    {
        private static readonly ConfigurationProperty s_propValue =
            new ConfigurationProperty("value", typeof(DateTime), DateTime.MinValue, ConfigurationPropertyOptions.IsKey);

        private static readonly ConfigurationPropertyCollection s_properties = new ConfigurationPropertyCollection { s_propValue };

        private readonly DateTime _initValue;

        private bool _needsInit;

        public DateTimeConfigurationElement() { }

        public DateTimeConfigurationElement(DateTime value)
        {
            _needsInit = true;
            _initValue = value;
        }

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        [ConfigurationProperty("value", IsKey = true)]
        public DateTime Value
        {
            get { return (DateTime)base[s_propValue]; }
            set { base[s_propValue] = value; }
        }

        protected internal override void Init()
        {
            base.Init();

            // We cannot initialize configuration properties in the constructor,
            // because Properties is an overridable virtual property that
            // hence may not be available in the constructor.
            if (_needsInit)
            {
                _needsInit = false;
                Value = _initValue;
            }
        }
    }
}
