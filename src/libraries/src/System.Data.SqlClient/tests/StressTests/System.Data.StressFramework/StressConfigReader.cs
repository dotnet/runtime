// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using static Stress.Data.DataStressSettings;

namespace Stress.Data
{
    /// <summary>
    /// Reads the configuration from a configuration file and provides the configuration
    /// </summary>
    internal class StressConfigReader
    {
        private string _configFilePath;
        private const string dataStressSettings = "dataStressSettings";
        private const string sourcePath = "//dataStressSettings/sources/source";
        internal List<DataSourceElement> Sources
        {
            get; private set;
        }

        public StressConfigReader(string configFilePath)
        {
            this._configFilePath = configFilePath;
        }

        internal void Load()
        {
            XmlReader reader = null;
            try { 
                Sources = new List<DataSourceElement>();
                reader = CreateReader();

                XPathDocument xpathDocument = new XPathDocument(reader);

                XPathNavigator navigator = xpathDocument.CreateNavigator();

                XPathNodeIterator sourceIterator = navigator.Select(sourcePath);

                foreach (XPathNavigator sourceNavigator in sourceIterator)
                {
                    string nsUri = sourceNavigator.NamespaceURI;
                    string sourceName = sourceNavigator.GetAttribute("name", nsUri);
                    string sourceType = sourceNavigator.GetAttribute("type", nsUri);
                    bool isDefault;
                    isDefault = bool.TryParse(sourceNavigator.GetAttribute("isDefault", nsUri), out isDefault) ? isDefault : false;
                    string dataSource = sourceNavigator.GetAttribute("dataSource", nsUri);
                    string user = sourceNavigator.GetAttribute("user", nsUri);
                    string password = sourceNavigator.GetAttribute("password", nsUri);
                    string database = sourceNavigator.GetAttribute("database", nsUri);
                    bool supportsWindowsAuthentication;
                    supportsWindowsAuthentication = bool.TryParse(sourceNavigator.GetAttribute("supportsWindowsAuthentication", nsUri), out supportsWindowsAuthentication) ? supportsWindowsAuthentication : false;
                    bool isLocal;
                    isLocal = bool.TryParse(sourceNavigator.GetAttribute("isLocal", nsUri), out isLocal) ? isLocal : false;
                    bool disableMultiSubnetFailover;
                    disableMultiSubnetFailover = bool.TryParse(sourceNavigator.GetAttribute("disableMultiSubnetFailover", nsUri), out disableMultiSubnetFailover) ? disableMultiSubnetFailover : false;
                    bool disableNamedPipes;
                    disableMultiSubnetFailover = bool.TryParse(sourceNavigator.GetAttribute("disableNamedPipes", nsUri), out disableNamedPipes) ? disableNamedPipes : false;

                    DataSourceElement element = new DataSourceElement(sourceName, sourceType, null, dataSource, database, user, password, ds_isDefault: isDefault, ds_isLocal: isLocal, disableMultiSubnetFailoverSetup: disableMultiSubnetFailover, disableNamedPipes: disableNamedPipes);
                    Sources.Add(element);
                }
            }
            finally
            {
                reader.Dispose();
            }
        }

        private XmlReader CreateReader()
        {
            FileStream configurationStream = new FileStream(this._configFilePath, FileMode.Open);
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Prohibit;
            XmlReader reader = XmlReader.Create(configurationStream, settings);
            return reader;
        }
    }
}
