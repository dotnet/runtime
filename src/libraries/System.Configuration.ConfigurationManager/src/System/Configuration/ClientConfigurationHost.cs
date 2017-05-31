// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace System.Configuration
{
    internal sealed class ClientConfigurationHost : DelegatingConfigHost, IInternalConfigClientHost
    {
        internal const string MachineConfigName = "MACHINE";
        internal const string ExeConfigName = "EXE";
        internal const string RoamingUserConfigName = "ROAMING_USER";
        internal const string LocalUserConfigName = "LOCAL_USER";

        internal const string MachineConfigPath = MachineConfigName;
        internal const string ExeConfigPath = MachineConfigPath + "/" + ExeConfigName;
        internal const string RoamingUserConfigPath = ExeConfigPath + "/" + RoamingUserConfigName;
        internal const string LocalUserConfigPath = RoamingUserConfigPath + "/" + LocalUserConfigName;

        private const string MachineConfigFilename = "machine.config";
        private const string MachineConfigSubdirectory = "Config";

        private static readonly object s_version = new object();
        private static volatile string s_machineConfigFilePath;
        private ClientConfigPaths _configPaths; // physical paths to client config files

        private string _exePath; // the physical path to the exe being configured
        private ExeConfigurationFileMap _fileMap; // optional file map
        private bool _initComplete;

        internal ClientConfigurationHost()
        {
            Host = new InternalConfigHost();
        }

        internal ClientConfigPaths ConfigPaths => _configPaths ?? (_configPaths = ClientConfigPaths.GetPaths(_exePath, _initComplete));

        internal static string MachineConfigFilePath
        {
            get
            {
                if (s_machineConfigFilePath == null)
                {
                    string directory = AppDomain.CurrentDomain.BaseDirectory;
                    s_machineConfigFilePath = Path.Combine(Path.Combine(directory, MachineConfigSubdirectory),
                        MachineConfigFilename);
                }

                return s_machineConfigFilePath;
            }
        }

        public override bool HasRoamingConfig
        {
            get
            {
                if (_fileMap != null) return !string.IsNullOrEmpty(_fileMap.RoamingUserConfigFilename);
                else return ConfigPaths.HasRoamingConfig;
            }
        }

        public override bool HasLocalConfig
        {
            get
            {
                if (_fileMap != null) return !string.IsNullOrEmpty(_fileMap.LocalUserConfigFilename);
                else return ConfigPaths.HasLocalConfig;
            }
        }

        public override bool IsAppConfigHttp => !IsFile(GetStreamName(ExeConfigPath));

        public override bool SupportsRefresh => true;

        public override bool SupportsPath => false;

        // Do we support location tags?
        public override bool SupportsLocation => false;

        bool IInternalConfigClientHost.IsExeConfig(string configPath)
        {
            return StringUtil.EqualsIgnoreCase(configPath, ExeConfigPath);
        }

        bool IInternalConfigClientHost.IsRoamingUserConfig(string configPath)
        {
            return StringUtil.EqualsIgnoreCase(configPath, RoamingUserConfigPath);
        }

        bool IInternalConfigClientHost.IsLocalUserConfig(string configPath)
        {
            return StringUtil.EqualsIgnoreCase(configPath, LocalUserConfigPath);
        }

        string IInternalConfigClientHost.GetExeConfigPath()
        {
            return ExeConfigPath;
        }

        string IInternalConfigClientHost.GetRoamingUserConfigPath()
        {
            return RoamingUserConfigPath;
        }

        string IInternalConfigClientHost.GetLocalUserConfigPath()
        {
            return LocalUserConfigPath;
        }

        public override void RefreshConfigPaths()
        {
            // Refresh current config paths.
            if ((_configPaths != null) && !_configPaths.HasEntryAssembly && (_exePath == null))
            {
                ClientConfigPaths.RefreshCurrent();
                _configPaths = null;
            }
        }

        // Return true if the config path is for a user.config file, false otherwise.
        private bool IsUserConfig(string configPath)
        {
            return StringUtil.EqualsIgnoreCase(configPath, RoamingUserConfigPath) ||
                StringUtil.EqualsIgnoreCase(configPath, LocalUserConfigPath);
        }

        public override void Init(IInternalConfigRoot configRoot, params object[] hostInitParams)
        {
            try
            {
                ConfigurationFileMap fileMap = (ConfigurationFileMap)hostInitParams[0];
                _exePath = (string)hostInitParams[1];

                Host.Init(configRoot, hostInitParams);

                // Do not complete initialization in runtime config, to avoid expense of 
                // loading user.config files that may not be required.
                _initComplete = configRoot.IsDesignTime;

                if ((fileMap != null) && !string.IsNullOrEmpty(_exePath))
                    throw ExceptionUtil.UnexpectedError("ClientConfigurationHost::Init");

                if (string.IsNullOrEmpty(_exePath)) _exePath = null;

                // Initialize the fileMap, if provided.
                if (fileMap != null)
                {
                    _fileMap = new ExeConfigurationFileMap();
                    if (!string.IsNullOrEmpty(fileMap.MachineConfigFilename))
                        _fileMap.MachineConfigFilename = Path.GetFullPath(fileMap.MachineConfigFilename);

                    ExeConfigurationFileMap exeFileMap = fileMap as ExeConfigurationFileMap;
                    if (exeFileMap != null)
                    {
                        if (!string.IsNullOrEmpty(exeFileMap.ExeConfigFilename))
                            _fileMap.ExeConfigFilename = Path.GetFullPath(exeFileMap.ExeConfigFilename);

                        if (!string.IsNullOrEmpty(exeFileMap.RoamingUserConfigFilename))
                            _fileMap.RoamingUserConfigFilename = Path.GetFullPath(exeFileMap.RoamingUserConfigFilename);

                        if (!string.IsNullOrEmpty(exeFileMap.LocalUserConfigFilename))
                            _fileMap.LocalUserConfigFilename = Path.GetFullPath(exeFileMap.LocalUserConfigFilename);
                    }
                }
            }
            catch
            {
                throw ExceptionUtil.UnexpectedError("ClientConfigurationHost::Init");
            }
        }

        public override void InitForConfiguration(ref string locationSubPath, out string configPath,
            out string locationConfigPath,
            IInternalConfigRoot configRoot, params object[] hostInitConfigurationParams)
        {
            locationSubPath = null;
            configPath = (string)hostInitConfigurationParams[2];
            locationConfigPath = null;

            Init(configRoot, hostInitConfigurationParams);
        }

        // Delay init if we have not been asked to complete init, and it is a user.config file.
        public override bool IsInitDelayed(IInternalConfigRecord configRecord)
        {
            return !_initComplete && IsUserConfig(configRecord.ConfigPath);
        }

        public override void RequireCompleteInit(IInternalConfigRecord record)
        {
            // Loading information about user.config files is expensive, 
            // so do it just once by locking.
            lock (this)
            {
                if (!_initComplete)
                {
                    // Note that all future requests for config must be complete.
                    _initComplete = true;

                    // Throw out the ConfigPath for this exe.
                    ClientConfigPaths.RefreshCurrent();

                    // Throw out our cached copy.
                    _configPaths = null;

                    // Force loading of user.config file information under lock.
                    ClientConfigPaths configPaths = ConfigPaths;
                }
            }
        }

        public override bool IsConfigRecordRequired(string configPath)
        {
            string configName = ConfigPathUtility.GetName(configPath);
            switch (configName)
            {
                case MachineConfigName:
                case ExeConfigName:
                    return true;
                case RoamingUserConfigName:
                    // Makes the design easier even if we only have an empty Roaming config record.
                    return HasRoamingConfig || HasLocalConfig;
                case LocalUserConfigName:
                    return HasLocalConfig;
                default:
                    // should never get here
                    Debug.Fail("unexpected config name: " + configName);
                    return false;
            }
        }

        public override string GetStreamName(string configPath)
        {
            string configName = ConfigPathUtility.GetName(configPath);
            switch (configName)
            {
                case MachineConfigName:
                    return _fileMap?.MachineConfigFilename ?? MachineConfigFilePath;
                case ExeConfigName:
                    return _fileMap?.ExeConfigFilename ?? ConfigPaths.ApplicationConfigUri;
                case RoamingUserConfigName:
                    return _fileMap?.RoamingUserConfigFilename ?? ConfigPaths.RoamingConfigFilename;
                case LocalUserConfigName:
                    return _fileMap?.LocalUserConfigFilename ?? ConfigPaths.LocalConfigFilename;
                default:
                    // should never get here
                    Debug.Fail("unexpected config name: " + configName);
                    goto case MachineConfigName;
            }
        }

        public override string GetStreamNameForConfigSource(string streamName, string configSource)
        {
            if (IsFile(streamName))
                return Host.GetStreamNameForConfigSource(streamName, configSource);

            int index = streamName.LastIndexOf('/');
            if (index < 0)
                return null;

            string parentUri = streamName.Substring(0, index + 1);
            string result = parentUri + configSource.Replace('\\', '/');

            return result;
        }

        public override object GetStreamVersion(string streamName)
        {
            return IsFile(streamName) ? Host.GetStreamVersion(streamName) : s_version;
        }

        // default impl treats name as a file name
        // null means stream doesn't exist for this name
        public override Stream OpenStreamForRead(string streamName)
        {
            // the streamName can either be a file name, or a URI
            if (IsFile(streamName)) return Host.OpenStreamForRead(streamName);

            if (streamName == null) return null;

            // scheme is http
            WebClient client = new WebClient();

            // Try using default credentials
            try
            {
                client.Credentials = CredentialCache.DefaultCredentials;
            }
            catch { }

            byte[] fileData = null;
            try
            {
                fileData = client.DownloadData(streamName);
            }
            catch { }

            if (fileData == null) return null;

            MemoryStream stream = new MemoryStream(fileData);
            return stream;
        }

        public override Stream OpenStreamForWrite(string streamName, string templateStreamName, ref object writeContext)
        {
            // only support files, not URIs
            if (!IsFile(streamName)) throw ExceptionUtil.UnexpectedError("ClientConfigurationHost::OpenStreamForWrite");

            return Host.OpenStreamForWrite(streamName, templateStreamName, ref writeContext);
        }

        public override void DeleteStream(string streamName)
        {
            // only support files, not URIs
            if (!IsFile(streamName)) throw ExceptionUtil.UnexpectedError("ClientConfigurationHost::Delete");

            Host.DeleteStream(streamName);
        }

        public override bool IsDefinitionAllowed(string configPath, ConfigurationAllowDefinition allowDefinition,
            ConfigurationAllowExeDefinition allowExeDefinition)
        {
            string allowedConfigPath;

            switch (allowExeDefinition)
            {
                case ConfigurationAllowExeDefinition.MachineOnly:
                    allowedConfigPath = MachineConfigPath;
                    break;
                case ConfigurationAllowExeDefinition.MachineToApplication:
                    allowedConfigPath = ExeConfigPath;
                    break;
                case ConfigurationAllowExeDefinition.MachineToRoamingUser:
                    allowedConfigPath = RoamingUserConfigPath;
                    break;
                // MachineToLocalUser does not current have any definition restrictions
                case ConfigurationAllowExeDefinition.MachineToLocalUser:
                    return true;
                default:
                    // If we have extended ConfigurationAllowExeDefinition
                    // make sure to update this switch accordingly
                    throw ExceptionUtil.UnexpectedError("ClientConfigurationHost::IsDefinitionAllowed");
            }

            return configPath.Length <= allowedConfigPath.Length;
        }

        public override void VerifyDefinitionAllowed(string configPath, ConfigurationAllowDefinition allowDefinition,
            ConfigurationAllowExeDefinition allowExeDefinition, IConfigErrorInfo errorInfo)
        {
            if (!IsDefinitionAllowed(configPath, allowDefinition, allowExeDefinition))
            {
                switch (allowExeDefinition)
                {
                    case ConfigurationAllowExeDefinition.MachineOnly:
                        throw new ConfigurationErrorsException(
                            SR.Config_allow_exedefinition_error_machine, errorInfo);
                    case ConfigurationAllowExeDefinition.MachineToApplication:
                        throw new ConfigurationErrorsException(
                            SR.Config_allow_exedefinition_error_application, errorInfo);
                    case ConfigurationAllowExeDefinition.MachineToRoamingUser:
                        throw new ConfigurationErrorsException(
                            SR.Config_allow_exedefinition_error_roaminguser, errorInfo);
                    default:
                        // If we have extended ConfigurationAllowExeDefinition
                        // make sure to update this switch accordingly
                        throw ExceptionUtil.UnexpectedError("ClientConfigurationHost::VerifyDefinitionAllowed");
                }
            }
        }

        // prefetch support
        public override bool PrefetchAll(string configPath, string streamName)
        {
            // If it's a file, we don't need to.  Otherwise (e.g. it's from the web), we'll prefetch everything.
            return !IsFile(streamName);
        }

        public override bool PrefetchSection(string sectionGroupName, string sectionName)
        {
            return sectionGroupName == "system.net";
        }

        public override object CreateDeprecatedConfigContext(string configPath)
        {
            return null;
        }

        public override object
            CreateConfigurationContext(string configPath,
            string locationSubPath)
        {
            return new ExeContext(GetUserLevel(configPath), ConfigPaths.ApplicationUri);
        }

        private ConfigurationUserLevel GetUserLevel(string configPath)
        {
            ConfigurationUserLevel level;

            switch (ConfigPathUtility.GetName(configPath))
            {
                case MachineConfigName:
                    level = ConfigurationUserLevel.None;
                    break;
                case ExeConfigName:
                    level = ConfigurationUserLevel.None;
                    break;
                case LocalUserConfigName:
                    level = ConfigurationUserLevel.PerUserRoamingAndLocal;
                    break;
                case RoamingUserConfigName:
                    level = ConfigurationUserLevel.PerUserRoaming;
                    break;
                default:
                    Debug.Fail("unrecognized configPath " + configPath);
                    level = ConfigurationUserLevel.None;
                    break;
            }

            return level;
        }

        internal static Configuration OpenExeConfiguration(ConfigurationFileMap fileMap, bool isMachine,
            ConfigurationUserLevel userLevel, string exePath)
        {
            // validate userLevel argument
            switch (userLevel)
            {
                case ConfigurationUserLevel.None:
                case ConfigurationUserLevel.PerUserRoaming:
                case ConfigurationUserLevel.PerUserRoamingAndLocal:
                    break;
                default:
                    throw ExceptionUtil.ParameterInvalid(nameof(userLevel));
            }

            // validate fileMap arguments
            if (fileMap != null)
            {
                if (string.IsNullOrEmpty(fileMap.MachineConfigFilename))
                    throw ExceptionUtil.ParameterNullOrEmpty(nameof(fileMap) + "." + nameof(fileMap.MachineConfigFilename));

                ExeConfigurationFileMap exeFileMap = fileMap as ExeConfigurationFileMap;
                if (exeFileMap != null)
                {
                    switch (userLevel)
                    {
                        case ConfigurationUserLevel.None:
                            if (string.IsNullOrEmpty(exeFileMap.ExeConfigFilename))
                                throw ExceptionUtil.ParameterNullOrEmpty(nameof(fileMap) + "." + nameof(exeFileMap.ExeConfigFilename));
                            break;
                        case ConfigurationUserLevel.PerUserRoaming:
                            if (string.IsNullOrEmpty(exeFileMap.RoamingUserConfigFilename))
                                throw ExceptionUtil.ParameterNullOrEmpty(nameof(fileMap) + "." + nameof(exeFileMap.RoamingUserConfigFilename));
                            goto case ConfigurationUserLevel.None;
                        case ConfigurationUserLevel.PerUserRoamingAndLocal:
                            if (string.IsNullOrEmpty(exeFileMap.LocalUserConfigFilename))
                                throw ExceptionUtil.ParameterNullOrEmpty(nameof(fileMap) + "." + nameof(exeFileMap.LocalUserConfigFilename));
                            goto case ConfigurationUserLevel.PerUserRoaming;
                    }
                }
            }

            string configPath = null;
            if (isMachine) configPath = MachineConfigPath;
            else
            {
                switch (userLevel)
                {
                    case ConfigurationUserLevel.None:
                        configPath = ExeConfigPath;
                        break;
                    case ConfigurationUserLevel.PerUserRoaming:
                        configPath = RoamingUserConfigPath;
                        break;
                    case ConfigurationUserLevel.PerUserRoamingAndLocal:
                        configPath = LocalUserConfigPath;
                        break;
                }
            }

            Configuration configuration = new Configuration(null, typeof(ClientConfigurationHost), fileMap, exePath, configPath);

            return configuration;
        }
    }
}