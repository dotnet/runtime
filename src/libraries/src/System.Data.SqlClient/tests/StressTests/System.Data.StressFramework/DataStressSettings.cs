// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Stress.Data
{
    /// <summary>
    /// Loads dataStressSettings section from Stress.Data.Framework.dll.config (App.config in source tree)
    /// </summary>
    public class DataStressSettings
    {

        internal static readonly string s_configFileName = "StressTest.config";

        // use Instance to access the settings
        private DataStressSettings()
        {
        }

        private bool Initialized { get; set; }

        private DataStressConfigurationSection _dataStressSettings = new DataStressConfigurationSection();

        // list of sources read from the config file
        private Dictionary<string, DataSource> _sources = new Dictionary<string, DataSource>(StringComparer.CurrentCultureIgnoreCase);
        public ErrorHandlingAction ActionOnProductError
        {
            get;
            private set;
        }
        public ErrorHandlingAction ActionOnTestError
        {
            get;
            private set;
        }
        public ErrorHandlingAction ActionOnProgrammingError
        {
            get;
            private set;
        }

        public int NumberOfConnectionPools
        {
            get;
            private set;
        }

        // singleton instance, lazy evaluation
        private static DataStressSettings s_instance = new DataStressSettings();
        public static DataStressSettings Instance
        {
            get
            {
                if (!s_instance.Initialized)
                {
                    lock (s_instance)
                    {
                        if (!s_instance.Initialized)
                        {
                            s_instance.Load();
                        }
                    }
                }
                return s_instance;
            }
        }

        #region Configuration file handlers

        private class DataStressConfigurationSection
        {
            private List<DataSourceElement> _sources = new List<DataSourceElement>();
            private ErrorHandlingPolicyElement _errorHandlingPolicy = new ErrorHandlingPolicyElement();
            private ConnectionPoolPolicyElement _connectionPoolPolicy = new ConnectionPoolPolicyElement();

            StressConfigReader reader = new StressConfigReader(s_configFileName);

            public List<DataSourceElement> Sources
            {
                get
                {
                    if(_sources.Count == 0)
                    {
                        reader.Load();
                        _sources = reader.Sources;
                    }
                    return _sources;
                }
            }

            public ErrorHandlingPolicyElement ErroHandlingPolicy
            {
                get
                {
                    return _errorHandlingPolicy;
                }
            }

            public ConnectionPoolPolicyElement ConnectionPoolPolicy
            {
                get
                {
                    return _connectionPoolPolicy;
                }
            }
        }


        internal class DataSourceElement
        {
            private string _name;
            private string _type;
            private bool _isDefault = false;

            public readonly Dictionary<string, string> SourceProperties = new Dictionary<string, string>();


            public DataSourceElement(string ds_name,
                                    string ds_type,
                                    string ds_server,
                                    string ds_datasource,
                                    string ds_database,
                                    string ds_user,
                                    string ds_password,
                                    bool ds_isDefault = false,
                                    bool ds_winAuth = false,
                                    bool ds_isLocal = false,
                                    string ds_dbFile = null,
                                    bool disableMultiSubnetFailoverSetup = true,
                                    bool disableNamedPipes = true)
            {
                _name = ds_name;
                _type = ds_type;
                _isDefault = ds_isDefault;

                if (ds_server != null)
                {
                    SourceProperties.Add("server", ds_server);
                }
                if (ds_datasource != null)
                {
                    SourceProperties.Add("dataSource", ds_datasource);
                }
                if (ds_database != null)
                {
                    SourceProperties.Add("database", ds_database);
                }
                if (ds_user != null)
                {
                    SourceProperties.Add("user", ds_user);
                }
                if (ds_password != null)
                {
                    SourceProperties.Add("password", ds_password);
                }

                SourceProperties.Add("supportsWindowsAuthentication", ds_winAuth.ToString());
                SourceProperties.Add("islocal", ds_isLocal.ToString());

                SourceProperties.Add("DisableMultiSubnetFailoverSetup", disableMultiSubnetFailoverSetup.ToString());

                SourceProperties.Add("DisableNamedPipes", disableNamedPipes.ToString());


                if (ds_dbFile != null)
                {
                    SourceProperties.Add("databaseFile", ds_dbFile);
                }
            }

            public string Name
            {
                get { return _name; }
            }

            public string Type
            {
                get { return _type; }
            }

            public bool IsDefault
            {
                get { return _isDefault; }
            }
        }

        private class ErrorHandlingPolicyElement
        {
            private string _onProductError = "debugBreak";
            private string _onTestError = "throwException";
            private string _onProgrammingError = "debugBreak";

            public string OnProductError
            {
                get
                {
                    return _onProductError;
                }
            }

            public string OnTestError
            {
                get
                {
                    return _onTestError;
                }
            }

            public string OnProgrammingError
            {
                get
                {
                    return _onProgrammingError;
                }
            }
        }

        private class ConnectionPoolPolicyElement
        {
            private int _numberOfPools = 10;

            public int NumberOfPools
            {
                get
                {
                    return _numberOfPools;
                }
            }
        }

        #endregion

        /// <summary>
        /// loads the configuration data from the app config file (Stress.Data.Framework.dll.config) and initializes the Sources collection
        /// </summary>
        private void Load()
        {
            // Parse <sources>
            foreach (DataSourceElement sourceElement in _dataStressSettings.Sources)
            {
                // if Parse raises exception, check that the type attribute is set to the relevant the SourceType enumeration value name
                DataSourceType sourceType = (DataSourceType)Enum.Parse(typeof(DataSourceType), sourceElement.Type, true);

                DataSource newSource = DataSource.Create(sourceElement.Name, sourceType, sourceElement.IsDefault, sourceElement.SourceProperties);
                _sources.Add(newSource.Name, newSource);
            }

            // Parse <errorhandlingpolicy>
            // if Parse raises exception, check that the action attribute is set to a valid ActionOnProductBugFound enumeration value name
            this.ActionOnProductError = (ErrorHandlingAction)Enum.Parse(typeof(ErrorHandlingAction), _dataStressSettings.ErroHandlingPolicy.OnProductError, true);
            this.ActionOnTestError = (ErrorHandlingAction)Enum.Parse(typeof(ErrorHandlingAction), _dataStressSettings.ErroHandlingPolicy.OnTestError, true);
            this.ActionOnProgrammingError = (ErrorHandlingAction)Enum.Parse(typeof(ErrorHandlingAction), _dataStressSettings.ErroHandlingPolicy.OnProgrammingError, true);

            // Parse <connectionPoolPolicy>
            this.NumberOfConnectionPools = _dataStressSettings.ConnectionPoolPolicy.NumberOfPools;

            this.Initialized = true;
        }


        /// <summary>
        /// use this method to retrieve the source data by its name (represented with 'name' attribute in config file)
        /// </summary>
        /// <param name="name">case-sensitive name</param>
        public DataSource GetSourceByName(string name)
        {
            return _sources[name];
        }

        /// <summary>
        /// Use this method to retrieve the default source associated with the type specified.
        /// The type of the node is specified with 'type' attribute on the sources file - see DataSourceType enum for list of supported types.
        /// If there is a source node with isDefault=true, this node is returned (first one found in config file).
        /// Otherwise, first source node from type specified is returned.
        /// </summary>
        public DataSource GetDefaultSourceByType(DataSourceType type)
        {
            DataSource defaultSource = null;
            foreach (DataSource source in _sources.Values)
            {
                if (source.Type == type)
                {
                    if (defaultSource == null)
                    {
                        // use the first found source, if default is not set
                        defaultSource = source;
                    }
                    else if (source.IsDefault)
                    {
                        defaultSource = source;
                        break;
                    }
                }
            }
            return defaultSource;
        }
    }
}