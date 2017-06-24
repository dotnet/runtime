// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Xml;
using System.Configuration;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    internal class ConfigurationHandler : IConfigurationSectionHandler
    {
        public virtual object Create(object parent, object configContext, XmlNode section)
        {
            ConfigSettings configSettings = null;
            bool foundDebugging = false;
            System.Enum debugLevelEnum = (System.Enum)GlobalConfig.DefaultDebugLevel;
            string debugLogFile = null;

            foreach (XmlNode child in section.ChildNodes)
            {
                switch (child.Name)
                {
#if DEBUG
                    case "Debugging":
                        if (foundDebugging)
                            throw new ConfigurationErrorsException(
                                                String.Format(
                                                    CultureInfo.CurrentCulture,
                                                    SR.ConfigHandlerConfigSectionsUnique,
                                                    "Debugging"));

                        RemoveEnumAttribute(child, "Debugging", "debugLevel", typeof(DebugLevel), ref debugLevelEnum);
                        RemoveStringAttribute(child, "Debugging", "debugLogFile", out debugLogFile);

                        foundDebugging = true;
                        break;
#endif

                    default:
                        throw new ConfigurationErrorsException(
                                            String.Format(
                                                CultureInfo.CurrentCulture,
                                                SR.ConfigHandlerUnknownConfigSection,
                                                child.Name));
                }
            }

            if (foundDebugging)
                configSettings = new ConfigSettings((DebugLevel)debugLevelEnum, debugLogFile);
            else
                configSettings = new ConfigSettings();

            // We need to always return an object so if we haven't read the debug section just create a default object.
            return (configSettings);
        }

#if DEBUG
        private void RemoveEnumAttribute(XmlNode node, string sectionName, string attributeName, Type enumType, ref System.Enum value)
        {
            XmlNode attribute = node.Attributes.RemoveNamedItem(attributeName);
            if (null != attribute)
            {
                try
                {
                    // case-insensitive, for ease of use
                    value = (System.Enum)System.Enum.Parse(enumType, attribute.Value, true);
                }
                catch (ArgumentException)
                {
                    throw new ConfigurationErrorsException(
                                        String.Format(
                                                CultureInfo.CurrentCulture,
                                                SR.ConfigHandlerInvalidEnumAttribute,
                                                attributeName,
                                                sectionName));
                }
            }
        }

        private void RemoveStringAttribute(XmlNode node, string sectionName, string attributeName, out string value)
        {
            value = null;
            XmlNode attribute = node.Attributes.RemoveNamedItem(attributeName);
            if (null != attribute)
            {
                value = attribute.Value as string;

                if (value == null)
                    throw new ConfigurationErrorsException(
                                        String.Format(
                                                CultureInfo.CurrentCulture,
                                                SR.ConfigHandlerInvalidStringAttribute,
                                                attributeName,
                                                sectionName));

                // Treat empty string the same as no string
                if (value.Length == 0)
                    value = null;
            }
        }
#endif
    }

    internal class ConfigSettings
    {
        public ConfigSettings(DebugLevel debugLevel, string debugLogFile)
        {
            _debugLevel = debugLevel;
            _debugLogFile = debugLogFile;
        }

        public ConfigSettings() : this(GlobalConfig.DefaultDebugLevel, null)
        {
        }

        public DebugLevel DebugLevel
        {
            get { return _debugLevel; }
        }

        public string DebugLogFile
        {
            get { return _debugLogFile; }
        }

        private DebugLevel _debugLevel = GlobalConfig.DefaultDebugLevel;
        private string _debugLogFile = null;
    }
}
