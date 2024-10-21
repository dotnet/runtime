// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace System.Drawing.Configuration
{
    [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
    public sealed class SystemDrawingSection : ConfigurationSection
    {
        private const string BitmapSuffixSectionName = "bitmapSuffix";

        static SystemDrawingSection() => s_properties.Add(s_bitmapSuffix);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        [ConfigurationProperty(BitmapSuffixSectionName)]
        public string BitmapSuffix
        {
            get => (string)this[s_bitmapSuffix];
            set => this[s_bitmapSuffix] = value;
        }

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

        private static readonly ConfigurationPropertyCollection s_properties = new ConfigurationPropertyCollection();

        private static readonly ConfigurationProperty s_bitmapSuffix =
            new ConfigurationProperty(BitmapSuffixSectionName, typeof(string), null, ConfigurationPropertyOptions.None);
    }
}
