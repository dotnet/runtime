// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;
using System.Threading;

namespace System.Configuration
{
    internal sealed class ClientConfigurationSystem : IInternalConfigSystem
    {
        private const string SystemDiagnosticsConfigKey = "system.diagnostics";
        private const string SystemNetGroupKey = "system.net/";
        private readonly IInternalConfigHost _configHost;
        private readonly IInternalConfigRoot _configRoot;
        private readonly bool _isAppConfigHttp;
        private IInternalConfigRecord _completeConfigRecord;

        private Exception _initError;
        private bool _isInitInProgress;
        private bool _isMachineConfigInited;
        private bool _isUserConfigInited;
        private IInternalConfigRecord _machineConfigRecord;

        internal ClientConfigurationSystem()
        {
            IConfigSystem configSystem = new ConfigSystem();
            configSystem.Init(typeof(ClientConfigurationHost), null, null);

            _configHost = configSystem.Host;
            _configRoot = configSystem.Root;

            _configRoot.ConfigRemoved += OnConfigRemoved;

            _isAppConfigHttp = ((IInternalConfigHostPaths)_configHost).IsAppConfigHttp;
        }

        object IInternalConfigSystem.GetSection(string sectionName)
        {
            PrepareClientConfigSystem(sectionName);

            // Get the appropriate config record for the section.
            IInternalConfigRecord configRecord = null;
            if (DoesSectionOnlyUseMachineConfig(sectionName))
            {
                if (_isMachineConfigInited) configRecord = _machineConfigRecord;
            }
            else
            {
                if (_isUserConfigInited) configRecord = _completeConfigRecord;
            }

            // Call GetSection(), or return null if no configuration is yet available.
            return configRecord?.GetSection(sectionName);
        }

        void IInternalConfigSystem.RefreshConfig(string sectionName)
        {
            PrepareClientConfigSystem(sectionName);

            if (_isMachineConfigInited) _machineConfigRecord.RefreshSection(sectionName);
        }

        // Supports user config
        bool IInternalConfigSystem.SupportsUserConfig => true;

        // Return true if the section might be used during initialization of the configuration system,
        // and thus lead to deadlock if appropriate measures are not taken.
        private bool IsSectionUsedInInit(string configKey)
        {
            return (configKey == SystemDiagnosticsConfigKey) ||
                (_isAppConfigHttp && configKey.StartsWith(SystemNetGroupKey, StringComparison.Ordinal));
        }

        // Return true if the section should only use the machine configuration and not use the
        // application configuration. This is only true for system.net sections when the configuration
        // file for the application is downloaded via http using System.Net.WebClient.
        private bool DoesSectionOnlyUseMachineConfig(string configKey)
        {
            return _isAppConfigHttp && configKey.StartsWith(SystemNetGroupKey, StringComparison.Ordinal);
        }

        // Ensure that initialization has completed, while handling re-entrancy issues
        // for certain sections that may be used during initialization itself.
        private void EnsureInit(string configKey)
        {
            bool doInit = false;

            lock (this)
            {
                // If the user config is not initialized, then we must either:
                //    a. Perform the initialization ourselves if no other thread is doing it, or
                //    b. Wait for the initialization to complete if we know the section is not used during initialization itself, or
                //    c. Ignore initialization if the section can be used during initialization. Note that GetSection()
                //       returns null is initialization has not completed.
                if (!_isUserConfigInited)
                {
                    if (!_isInitInProgress)
                    {
                        _isInitInProgress = true;
                        doInit = true;
                    }
                    else
                    {
                        if (!IsSectionUsedInInit(configKey))
                        {
                            // Wait for initialization to complete.
                            Monitor.Wait(this);
                        }
                    }
                }
            }

            if (!doInit) return;
            try
            {
                try
                {
                    // Initialize machine configuration.
                    _machineConfigRecord = _configRoot.GetConfigRecord(
                        ClientConfigurationHost.MachineConfigPath);

                    _machineConfigRecord.ThrowIfInitErrors();

                    // Make machine configuration available to system.net sections
                    // when application configuration is downloaded via http.
                    _isMachineConfigInited = true;

                    // If we add System.Net.Configuration we'll need to kick the initialization here
                    // to prevent deadlocks in the networking classes by loading networking config
                    // before making any networking requests.
                    //
                    // Any requests for sections used in initialization during the call to
                    // EnsureConfigLoaded() will be served by _machine.config or will return null.

                    //if (_isAppConfigHttp)
                    //{
                    //}

                    // Now load the rest of configuration
                    var configHostPaths = (IInternalConfigHostPaths)_configHost;
                    configHostPaths.RefreshConfigPaths();

                    string configPath;
                    if (configHostPaths.HasLocalConfig)
                    {
                        configPath = ClientConfigurationHost.LocalUserConfigPath;
                    }
                    else
                    {
                        configPath = configHostPaths.HasRoamingConfig
                            ? ClientConfigurationHost.RoamingUserConfigPath
                            : ClientConfigurationHost.ExeConfigPath;
                    }

                    _completeConfigRecord = _configRoot.GetConfigRecord(configPath);
                    _completeConfigRecord.ThrowIfInitErrors();

                    _isUserConfigInited = true;
                }
                catch (Exception e)
                {
                    _initError =
                        new ConfigurationErrorsException(SR.Config_client_config_init_error, e);
                    throw _initError;
                }
            }
            catch
            {
                ConfigurationManager.SetInitError(_initError);
                _isMachineConfigInited = true;
                _isUserConfigInited = true;
                throw;
            }
            finally
            {
                lock (this)
                {
                    try
                    {
                        // Notify ConfigurationSettings that initialization has fully completed,
                        // even if unsuccessful.
                        ConfigurationManager.CompleteConfigInit();

                        _isInitInProgress = false;
                    }
                    finally
                    {
                        // Wake up all threads waiting for initialization to complete.
                        Monitor.PulseAll(this);
                    }
                }
            }
        }

        private void PrepareClientConfigSystem(string sectionName)
        {
            // Ensure the configuration system is inited for this section.
            if (!_isUserConfigInited) EnsureInit(sectionName);

            // If an error occurred during initialzation, throw it.
            if (_initError != null) throw _initError;
        }

        // If config has been removed because initialization was not complete,
        // fetch a new configuration record. The record will be created and
        // completely initialized as RequireCompleteInit() will have been called
        // on the ClientConfigurationHost before we receive this event.
        private void OnConfigRemoved(object sender, InternalConfigEventArgs e)
        {
            try
            {
                IInternalConfigRecord newConfigRecord = _configRoot.GetConfigRecord(_completeConfigRecord.ConfigPath);
                _completeConfigRecord = newConfigRecord;
                _completeConfigRecord.ThrowIfInitErrors();
            }
            catch (Exception ex)
            {
                _initError = new ConfigurationErrorsException(SR.Config_client_config_init_error, ex);
                ConfigurationManager.SetInitError(_initError);
                throw _initError;
            }
        }
    }
}