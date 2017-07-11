// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;

namespace System.Drawing.Configuration
{
    public sealed class SystemDrawingSection : ConfigurationSection
    {
        private const string BitmapSuffixSectionName = "bitmapSuffix";

        static SystemDrawingSection() => s_properties.Add(s_bitmapSuffix);

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
