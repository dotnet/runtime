// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration.Internal;
using System.Reflection;
using System.Xml;

namespace System.Configuration
{
    internal sealed class RuntimeConfigurationRecord : BaseConfigurationRecord
    {
        private static readonly SimpleBitVector32 s_runtimeClassFlags = new SimpleBitVector32(
            ClassSupportsChangeNotifications
            | ClassSupportsRefresh
            | ClassSupportsImpersonation
            | ClassSupportsRestrictedPermissions
            | ClassSupportsDelayedInit);

        private RuntimeConfigurationRecord() { }

        protected override SimpleBitVector32 ClassFlags => s_runtimeClassFlags;

        internal static IInternalConfigRecord Create(
            InternalConfigRoot configRoot,
            IInternalConfigRecord parent,
            string configPath)
        {
            RuntimeConfigurationRecord configRecord = new RuntimeConfigurationRecord();
            configRecord.Init(configRoot, (BaseConfigurationRecord)parent, configPath, null);
            return configRecord;
        }

        // Create the factory that will evaluate configuration
        protected override object CreateSectionFactory(FactoryRecord factoryRecord)
        {
            return new RuntimeConfigurationFactory(this, factoryRecord);
        }

        // parentConfig contains the config that we'd merge with.
        protected override object CreateSection(bool inputIsTrusted, FactoryRecord factoryRecord,
            SectionRecord sectionRecord, object parentConfig, ConfigXmlReader reader)
        {
            // Get the factory used to create a section.
            RuntimeConfigurationFactory factory = (RuntimeConfigurationFactory)factoryRecord.Factory;

            // Use the factory to create a section.
            object config = factory.CreateSection(inputIsTrusted, this, factoryRecord, sectionRecord, parentConfig,
                reader);

            return config;
        }

        protected override object UseParentResult(string configKey, object parentResult, SectionRecord sectionRecord)
        {
            return parentResult;
        }

        protected override object GetRuntimeObject(object result)
        {
            object runtimeObject;
            ConfigurationSection section = result as ConfigurationSection;
            if (section == null) runtimeObject = result;
            else
            {
                // Call into config section while impersonating process or UNC identity
                // so that the section could read files from disk if needed
                try
                {
                    runtimeObject = section.GetRuntimeObject();
                }
                catch (Exception e)
                {
                    throw new ConfigurationErrorsException(
                        SR.Format(SR.Config_exception_in_config_section_handler,
                            section.SectionInformation.SectionName), e);
                }
            }

            return runtimeObject;
        }

        private sealed class RuntimeConfigurationFactory
        {
            private ConstructorInfo _sectionCtor;
            private IConfigurationSectionHandler _sectionHandler;

            internal RuntimeConfigurationFactory(RuntimeConfigurationRecord configRecord, FactoryRecord factoryRecord)
            {
                Init(configRecord, factoryRecord);
            }

            private void Init(RuntimeConfigurationRecord configRecord, FactoryRecord factoryRecord)
            {
                // Get the type of the factory
                Type type = TypeUtil.GetType(configRecord.Host, factoryRecord.FactoryTypeName,
                    true);

                // If the type is a ConfigurationSection, that's the type.
                if (typeof(ConfigurationSection).IsAssignableFrom(type))
                {
                    _sectionCtor = TypeUtil.GetConstructor(type, typeof(ConfigurationSection),
                        true);
                }
                else
                {
                    // Note: in v1, IConfigurationSectionHandler is in effect a factory that has a Create method
                    // that creates the real section object.

                    // throws if type does not implement IConfigurationSectionHandler
                    TypeUtil.VerifyAssignableType(typeof(IConfigurationSectionHandler), type, true);

                    // Create an instance of the handler
                    _sectionHandler =
                        (IConfigurationSectionHandler)TypeUtil.CreateInstance(type);
                }
            }

            // Throw an exception if an attribute within a legacy section is one of our
            // reserved locking attributes. We do not want admins to think they can lock
            // an attribute or element within a legacy section.
            private static void CheckForLockAttributes(string sectionName, XmlNode xmlNode)
            {
                XmlAttributeCollection attributes = xmlNode.Attributes;
                if (attributes != null)
                {
                    foreach (XmlAttribute attribute in attributes)
                        if (ConfigurationElement.IsLockAttributeName(attribute.Name))
                        {
                            throw new ConfigurationErrorsException(
                                SR.Format(SR.Config_element_locking_not_supported, sectionName), attribute);
                        }
                }

                foreach (XmlNode child in xmlNode.ChildNodes)
                    if (xmlNode.NodeType == XmlNodeType.Element) CheckForLockAttributes(sectionName, child);
            }

            internal object CreateSection(bool inputIsTrusted, RuntimeConfigurationRecord configRecord,
                FactoryRecord factoryRecord, SectionRecord sectionRecord, object parentConfig, ConfigXmlReader reader)
            {
                object config;

                if (_sectionCtor != null)
                {
                    ConfigurationSection configSection =
                        (ConfigurationSection)_sectionCtor.Invoke(null);

                    configSection.SectionInformation.SetRuntimeConfigurationInformation(configRecord, factoryRecord,
                        sectionRecord);

                    configSection.CallInit();

                    ConfigurationSection parentSection = (ConfigurationSection)parentConfig;
                    configSection.Reset(parentSection);

                    if (reader != null) configSection.DeserializeSection(reader);

                    // throw if there are any cached errors
                    ConfigurationErrorsException errors = configSection.GetErrors();
                    if (errors != null) throw errors;

                    // don't allow changes to sections at runtime
                    configSection.SetReadOnly();

                    // reset the modified bit
                    configSection.ResetModified();

                    config = configSection;
                }
                else
                {
                    if (reader != null)
                    {
                        XmlNode xmlNode = ErrorInfoXmlDocument.CreateSectionXmlNode(reader);

                        CheckForLockAttributes(factoryRecord.ConfigKey, xmlNode);

                        // In v1, our old section handler expects a context that contains the virtualPath from the configPath
                        object configContext = configRecord.Host.CreateDeprecatedConfigContext(configRecord.ConfigPath);

                        config = _sectionHandler.Create(parentConfig, configContext, xmlNode);
                    }
                    else config = null;
                }

                return config;
            }
        }
    }
}
