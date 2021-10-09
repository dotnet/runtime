// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;

namespace System.Configuration
{
    public sealed class IdnElement : ConfigurationElement
    {
        internal const UriIdnScope EnabledDefaultValue = UriIdnScope.None;

        private readonly ConfigurationPropertyCollection _properties = new ConfigurationPropertyCollection();

        private readonly ConfigurationProperty _enabled =
            new ConfigurationProperty(CommonConfigurationStrings.Enabled, typeof(UriIdnScope),
                EnabledDefaultValue, new UriIdnScopeTypeConverter(), null, ConfigurationPropertyOptions.None);

        public IdnElement()
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
        public UriIdnScope Enabled
        {
            get { return (UriIdnScope)this[_enabled]; }
            set { this[_enabled] = value; }
        }

        private sealed class UriIdnScopeTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string))
                {
                    return true;
                }
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string s)
                {
                    switch (s.ToLowerInvariant())
                    {
                        case "all":
                            return UriIdnScope.All;
                        case "none":
                            return UriIdnScope.None;
                        case "allexceptintranet":
                            return UriIdnScope.AllExceptIntranet;
                    }
                }

                return base.ConvertFrom(context, culture, value);
            }
        }
    }
}
