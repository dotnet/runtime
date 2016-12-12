/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    ConfigurationHandler.cs

Abstract:

    Implements config file parsing.

History:

    18-Aug-2004    MattRim     Derived from SearchWaitHandler from S.DS,
                               written by WeiqingT

--*/

using System;
using System.Collections;
using System.Diagnostics;
using System.Xml;
using System.Configuration;
using System.Globalization;


namespace System.DirectoryServices.AccountManagement
{    
    class ConfigurationHandler :IConfigurationSectionHandler
    {
        public virtual object Create(object parent, object configContext, XmlNode section)
        {        

            ConfigSettings configSettings = null;
            bool foundDebugging = false;
            System.Enum debugLevelEnum = (System.Enum) GlobalConfig.DefaultDebugLevel;
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
                                                    StringResources.ConfigHandlerConfigSectionsUnique,
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
                                                StringResources.ConfigHandlerUnknownConfigSection,
                                                child.Name));
                }
                
            }

            if ( foundDebugging )
                configSettings = new ConfigSettings((DebugLevel) debugLevelEnum, debugLogFile);                        
            else
                configSettings = new ConfigSettings();

            // We need to always return an object so if we haven't read the debug section just create a default object.
            return (configSettings);
        }      

#if DEBUG
        void RemoveEnumAttribute(XmlNode node, string sectionName, string attributeName, Type enumType, ref System.Enum value)
        {
            XmlNode attribute = node.Attributes.RemoveNamedItem(attributeName);
            if (null != attribute) 
            {
                try
                {
                    // case-insensitive, for ease of use
                    value = (System.Enum) System.Enum.Parse(enumType, attribute.Value, true);                
                }
                catch (ArgumentException)
                {
                    throw new ConfigurationErrorsException(
                                        String.Format(
                                                CultureInfo.CurrentCulture,                                                
                                                StringResources.ConfigHandlerInvalidEnumAttribute,
                                                attributeName,
                                                sectionName));                                                
                }
            }
        }   


        void RemoveStringAttribute(XmlNode node, string sectionName, string attributeName, out string value)
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
                                                StringResources.ConfigHandlerInvalidStringAttribute,
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
            this.debugLevel = debugLevel;
            this.debugLogFile = debugLogFile;
        }
        
        public ConfigSettings() : this(GlobalConfig.DefaultDebugLevel, null)
        {
        
        }
        
        public DebugLevel DebugLevel
        {
            get {return this.debugLevel;}
        }

        public string DebugLogFile
        {
            get {return this.debugLogFile;}
        }

        DebugLevel debugLevel = GlobalConfig.DefaultDebugLevel;
        string debugLogFile = null;
    }
}
