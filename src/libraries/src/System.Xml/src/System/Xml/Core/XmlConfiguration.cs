// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Configuration;
using System.Globalization;
using System.Xml;

namespace System.Xml.XmlConfiguration
{
    internal static class XmlConfigurationString
    {
        internal const string XmlReaderSectionName = "xmlReader";
        internal const string XsltSectionName = "xslt";

        internal const string ProhibitDefaultResolverName = "prohibitDefaultResolver";
        internal const string LimitXPathComplexityName = "limitXPathComplexity";
        internal const string EnableMemberAccessForXslCompiledTransformName = "enableMemberAccessForXslCompiledTransform";

        internal const string XmlConfigurationSectionName = "system.xml";

        internal static string XmlReaderSectionPath = string.Format(CultureInfo.InvariantCulture, @"{0}/{1}", XmlConfigurationSectionName, XmlReaderSectionName);
        internal static string XsltSectionPath = string.Format(CultureInfo.InvariantCulture, @"{0}/{1}", XmlConfigurationSectionName, XsltSectionName);
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed class XmlReaderSection : ConfigurationSection
    {
        public string ProhibitDefaultResolverString
        {
            get { return (string)this[XmlConfigurationString.ProhibitDefaultResolverName]; }
            set { this[XmlConfigurationString.ProhibitDefaultResolverName] = value; }
        }

        private bool _ProhibitDefaultResolver
        {
            get
            {
                string value = ProhibitDefaultResolverString;
                bool result;
                XmlConvert.TryToBoolean(value, out result);
                return result;
            }
        }

        internal static XmlResolver CreateDefaultResolver()
        {
            if (LocalAppContextSwitches.ProhibitDefaultUrlResolver)
                return null;
            else
                return new XmlUrlResolver();
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed class XsltConfigSection : ConfigurationSection
    {
        public string ProhibitDefaultResolverString
        {
            get { return (string)this[XmlConfigurationString.ProhibitDefaultResolverName]; }
            set { this[XmlConfigurationString.ProhibitDefaultResolverName] = value; }
        }

        private bool _ProhibitDefaultResolver
        {
            get
            {
                string value = ProhibitDefaultResolverString;
                bool result;
                XmlConvert.TryToBoolean(value, out result);
                return result;
            }
        }

        internal static XmlResolver CreateDefaultResolver()
        {
            if (LocalAppContextSwitches.s_ProhibitDefaultUrlResolver)
                return XmlNullResolver.Singleton;
            else
                return new XmlUrlResolver();
        }

        internal string LimitXPathComplexityString
        {
            get { return (string)this[XmlConfigurationString.LimitXPathComplexityName]; }
            set { this[XmlConfigurationString.LimitXPathComplexityName] = value; }
        }

        private bool _LimitXPathComplexity
        {
            get
            {
                string value = LimitXPathComplexityString;
                bool result = true;
                XmlConvert.TryToBoolean(value, out result);
                return result;
            }
        }

        internal string EnableMemberAccessForXslCompiledTransformString
        {
            get { return (string)this[XmlConfigurationString.EnableMemberAccessForXslCompiledTransformName]; }
            set { this[XmlConfigurationString.EnableMemberAccessForXslCompiledTransformName] = value; }
        }

        private bool _EnableMemberAccessForXslCompiledTransform
        {
            get
            {
                string value = EnableMemberAccessForXslCompiledTransformString;
                bool result = false;
                XmlConvert.TryToBoolean(value, out result);
                return result;
            }
        }
    }
}