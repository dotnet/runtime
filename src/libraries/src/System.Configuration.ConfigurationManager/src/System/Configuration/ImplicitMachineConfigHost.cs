// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;
using System.IO;
using System.Text;

namespace System.Configuration
{
    internal class ImplicitMachineConfigHost : DelegatingConfigHost
    {
        private string _machineStreamName;
        private ConfigurationFileMap _fileMap;

        internal ImplicitMachineConfigHost(IInternalConfigHost host)
        {
            // Delegate to the host provided.
            Host = host;
        }

        public override void InitForConfiguration(ref string locationSubPath, out string configPath,
            out string locationConfigPath, IInternalConfigRoot configRoot, params object[] hostInitConfigurationParams)
        {
            // Stash the filemap so we can see if the machine config was explicitly specified
            _fileMap = (ConfigurationFileMap)hostInitConfigurationParams[0];
            base.InitForConfiguration(ref locationSubPath, out configPath, out locationConfigPath, configRoot, hostInitConfigurationParams);
        }

        public override void Init(IInternalConfigRoot configRoot, params object[] hostInitParams)
        {
            // Stash the filemap so we can see if the machine config was explicitly specified
            _fileMap = (ConfigurationFileMap)hostInitParams[0];
            base.Init(configRoot, hostInitParams);
        }

        public override string GetStreamName(string configPath)
        {
            string name = base.GetStreamName(configPath);

            if (ConfigPathUtility.GetName(configPath) == ClientConfigurationHost.MachineConfigName
                && (_fileMap?.IsMachinePathDefault ?? true))
            {
                // The machine config was asked for and wasn't explicitly
                // specified, stash the "default" machine.config path
                _machineStreamName = name;
            }

            return name;
        }

        public override Stream OpenStreamForRead(string streamName)
        {
            Stream stream = base.OpenStreamForRead(streamName);

            if (stream == null && streamName == _machineStreamName)
            {
                // We only want to inject if we aren't able to load
                stream = new MemoryStream(Encoding.UTF8.GetBytes(s_implicitMachineConfig));
            }

            return stream;
        }

        private static string s_implicitMachineConfig =
@"<configuration>
    <configSections>
        <section name='appSettings' type='System.Configuration.AppSettingsSection, System.Configuration.ConfigurationManager' restartOnExternalChanges='false' requirePermission='false' />
        <section name='connectionStrings' type='System.Configuration.ConnectionStringsSection, System.Configuration.ConfigurationManager' requirePermission='false' />
        <section name='mscorlib' type='System.Configuration.IgnoreSection, System.Configuration.ConfigurationManager' allowLocation='false' />
        <section name='runtime' type='System.Configuration.IgnoreSection, System.Configuration.ConfigurationManager' allowLocation='false' />
        <section name='assemblyBinding' type='System.Configuration.IgnoreSection, System.Configuration.ConfigurationManager' allowLocation='false' />
        <section name='satelliteassemblies' type='System.Configuration.IgnoreSection, System.Configuration.ConfigurationManager' allowLocation='false' />
        <section name='startup' type='System.Configuration.IgnoreSection, System.Configuration.ConfigurationManager' allowLocation='false' />
    </configSections>
    <configProtectedData defaultProvider='RsaProtectedConfigurationProvider'>
        <providers>
            <add name = 'RsaProtectedConfigurationProvider' type='System.Configuration.RsaProtectedConfigurationProvider, System.Configuration.ConfigurationManager' description='Uses RsaCryptoServiceProvider to encrypt and decrypt' keyContainerName='NetFrameworkConfigurationKey' cspProviderName='' useMachineContainer='true' useOAEP='false' />
            <add name = 'DataProtectionConfigurationProvider' type='System.Configuration.DpapiProtectedConfigurationProvider, System.Configuration.ConfigurationManager' description='Uses CryptProtectData and CryptUnProtectData Windows APIs to encrypt and decrypt' useMachineProtection='true' keyEntropy='' />
        </providers>
    </configProtectedData>
    <connectionStrings>
        <add name = 'LocalSqlServer' connectionString='data source=.\SQLEXPRESS;Integrated Security=SSPI;AttachDBFilename=|DataDirectory|aspnetdb.mdf;User Instance=true' providerName='System.Data.SqlClient' />
    </connectionStrings>
</configuration>";
    }
}
