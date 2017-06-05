// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Configuration
{
    using System.Configuration;

    /// <include file='doc\SystemDrawingSection.uex' path='docs/doc[@for="SystemDrawingSection"]/*' />
    /// <devdoc>
    /// A configuration section with a "bitmapSuffix" string value that specifies the suffix to be
    /// appended to bitmaps that are loaded through ToolboxBitmapAttribute and similar attributes.
    /// </devdoc>
    public sealed class SystemDrawingSection : ConfigurationSection
    {
        private const string BitmapSuffixSectionName = "bitmapSuffix";

        static SystemDrawingSection()
        {
            s_properties.Add(s_bitmapSuffix);
        }

        [ConfigurationProperty(BitmapSuffixSectionName)]
        public string BitmapSuffix
        {
            get { return (string)this[s_bitmapSuffix]; }
            set { this[s_bitmapSuffix] = value; }
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get { return s_properties; }
        }

        private static readonly ConfigurationPropertyCollection s_properties = new ConfigurationPropertyCollection();

        private static readonly ConfigurationProperty s_bitmapSuffix =
            new ConfigurationProperty(BitmapSuffixSectionName, typeof(string), null, ConfigurationPropertyOptions.None);
    }
}
