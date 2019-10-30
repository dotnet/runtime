// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public sealed class ExeConfigurationFileMap : ConfigurationFileMap
    {
        public ExeConfigurationFileMap()
        {
            ExeConfigFilename = string.Empty;
            RoamingUserConfigFilename = string.Empty;
            LocalUserConfigFilename = string.Empty;
        }

        public ExeConfigurationFileMap(string machineConfigFileName)
            : base(machineConfigFileName)
        {
            ExeConfigFilename = string.Empty;
            RoamingUserConfigFilename = string.Empty;
            LocalUserConfigFilename = string.Empty;
        }

        private ExeConfigurationFileMap(string machineConfigFileName, string exeConfigFilename,
            string roamingUserConfigFilename, string localUserConfigFilename)
            : base(machineConfigFileName)
        {
            ExeConfigFilename = exeConfigFilename;
            RoamingUserConfigFilename = roamingUserConfigFilename;
            LocalUserConfigFilename = localUserConfigFilename;
        }

        public string ExeConfigFilename { get; set; }

        public string RoamingUserConfigFilename { get; set; }

        public string LocalUserConfigFilename { get; set; }

        public override object Clone()
        {
            return new ExeConfigurationFileMap(MachineConfigFilename, ExeConfigFilename, RoamingUserConfigFilename,
                LocalUserConfigFilename);
        }
    }
}