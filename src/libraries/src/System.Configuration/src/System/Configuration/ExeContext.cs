// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public sealed class ExeContext
    {
        internal ExeContext(ConfigurationUserLevel userContext,
            string exePath)
        {
            UserLevel = userContext;
            ExePath = exePath;
        }

        // The ConfigurationUserLevel that we are running within.
        //
        // Note: ConfigurationUserLevel.None will be set for machine.config
        //       and the applicationconfig file.  Use IsMachineConfig in
        //       ConfigurationContext, to determine the difference.
        public ConfigurationUserLevel UserLevel { get; }

        public string ExePath { get; }
    }
}