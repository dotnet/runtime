// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Configuration
{
    /// <summary>
    /// Holds the configuration file mapping for machine.config. It is the base class for
    /// ExeConfigurationFileMap and WebConfigurationFileMap.
    /// </summary>
    public class ConfigurationFileMap : ICloneable
    {
        // This used to be two fields: one containing the filename and the other containing
        // a Boolean dictating whether a security check needed to take place. Such a pattern wasn't thread-safe
        // and could be circumvented by malicious callers. Using a single reference field is guaranteed atomic
        // read and write across all platforms, so the race condition is eliminated.
        private Func<string> _getFilenameThunk;

        public ConfigurationFileMap()
        {
            _getFilenameThunk = GetFilenameFromMachineConfigFilePath;
        }

        public ConfigurationFileMap(string machineConfigFilename)
        {
            if (string.IsNullOrEmpty(machineConfigFilename))
                throw new ArgumentNullException(nameof(machineConfigFilename));

            if (!File.Exists(machineConfigFilename))
                throw new ArgumentException(SR.Format(SR.Machine_config_file_not_found, machineConfigFilename),
                    nameof(machineConfigFilename));

            MachineConfigFilename = machineConfigFilename;
        }

        // copy ctor used only for cloning
        private ConfigurationFileMap(ConfigurationFileMap other)
        {
            _getFilenameThunk = other._getFilenameThunk;
        }

        public string MachineConfigFilename
        {
            get { return _getFilenameThunk(); }
            set { _getFilenameThunk = () => value; }
        }

        public virtual object Clone()
        {
            return new ConfigurationFileMap(this);
        }

        private static string GetFilenameFromMachineConfigFilePath()
        {
            return ClientConfigurationHost.MachineConfigFilePath;
        }

        internal bool IsMachinePathDefault => _getFilenameThunk == GetFilenameFromMachineConfigFilePath;
    }
}
