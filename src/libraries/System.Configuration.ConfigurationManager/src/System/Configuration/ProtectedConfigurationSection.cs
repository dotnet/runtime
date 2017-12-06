// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.Globalization;
using System.Xml;

namespace System.Configuration
{
    public sealed class ProtectedConfigurationSection : ConfigurationSection
    {
        private const string EncryptedSectionTemplate = "<{0} {1}=\"{2}\"> {3} </{0}>";

        private static readonly ConfigurationPropertyCollection s_properties;

        private static readonly ConfigurationProperty s_propProviders =
            new ConfigurationProperty("providers",
                typeof(ProtectedProviderSettings),
                new ProtectedProviderSettings(),
                ConfigurationPropertyOptions.None);

        private static readonly ConfigurationProperty s_propDefaultProvider =
            new ConfigurationProperty("defaultProvider",
                type: typeof(string),
                defaultValue: "RsaProtectedConfigurationProvider",
                typeConverter: null,
                validator: ConfigurationProperty.s_nonEmptyStringValidator,
                options: ConfigurationPropertyOptions.None);

        static ProtectedConfigurationSection()
        {
            // Property initialization
            s_properties = new ConfigurationPropertyCollection { s_propProviders, s_propDefaultProvider };
        }

        public ProtectedConfigurationSection(){}

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

        private ProtectedProviderSettings ProtectedProviders => (ProtectedProviderSettings)base[s_propProviders];

        [ConfigurationProperty("providers")]
        public ProviderSettingsCollection Providers => ProtectedProviders.Providers;

        [ConfigurationProperty("defaultProvider", DefaultValue = "RsaProtectedConfigurationProvider")]
        public string DefaultProvider
        {
            get { return (string)base[s_propDefaultProvider]; }
            set { base[s_propDefaultProvider] = value; }
        }

        internal ProtectedConfigurationProvider GetProviderFromName(string providerName)
        {
            ProviderSettings ps = Providers[providerName];

            if (ps == null)
                throw new ArgumentException(string.Format(SR.ProtectedConfigurationProvider_not_found, providerName), nameof(providerName));

            return InstantiateProvider(ps);
        }

        internal ProtectedConfigurationProviderCollection GetAllProviders()
        {
            ProtectedConfigurationProviderCollection coll = new ProtectedConfigurationProviderCollection();
            foreach (ProviderSettings ps in Providers)
                coll.Add(InstantiateProvider(ps));
            return coll;
        }

        private ProtectedConfigurationProvider CreateAndInitializeProviderWithAssert(Type t, ProviderSettings pn)
        {
            ProtectedConfigurationProvider provider =
                (ProtectedConfigurationProvider)TypeUtil.CreateInstance(t);
            NameValueCollection pars = pn.Parameters;
            NameValueCollection cloneParams = new NameValueCollection(pars.Count);

            foreach (string key in pars) cloneParams[key] = pars[key];

            provider.Initialize(pn.Name, cloneParams);
            return provider;
        }

        private ProtectedConfigurationProvider InstantiateProvider(ProviderSettings pn)
        {
            Type t = TypeUtil.GetType(pn.Type, true);
            if (!typeof(ProtectedConfigurationProvider).IsAssignableFrom(t))
                throw new ArgumentException(SR.WrongType_of_Protected_provider, nameof(pn));

            return CreateAndInitializeProviderWithAssert(t, pn);
        }

        internal static string DecryptSection(string encryptedXml, ProtectedConfigurationProvider provider)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(encryptedXml);
            XmlNode resultNode = provider.Decrypt(doc.DocumentElement);
            return resultNode.OuterXml;
        }

        internal static string FormatEncryptedSection(string encryptedXml, string sectionName, string providerName)
        {
            return string.Format(CultureInfo.InvariantCulture, EncryptedSectionTemplate,
                sectionName, // The section to encrypt
                BaseConfigurationRecord.ProtectionProviderAttibute, // protectionProvider keyword
                providerName, // The provider name
                encryptedXml // the encrypted xml
                );
        }

        internal static string EncryptSection(string clearXml, ProtectedConfigurationProvider provider)
        {
            XmlDocument xmlDocument = new XmlDocument { PreserveWhitespace = true };
            xmlDocument.LoadXml(clearXml);
            XmlNode encNode = provider.Encrypt(xmlDocument.DocumentElement);
            return encNode.OuterXml;
        }
    }
}
