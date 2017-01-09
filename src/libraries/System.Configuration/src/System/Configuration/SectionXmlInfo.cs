// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;

namespace System.Configuration
{
    internal sealed class SectionXmlInfo : IConfigErrorInfo
    {
        // the config path of the configuration record where this directive was defined

        internal SectionXmlInfo(
            string configKey, string definitionConfigPath, string targetConfigPath, string subPath,
            string filename, int lineNumber, object streamVersion,
            string rawXml, string configSource, string configSourceStreamName, object configSourceStreamVersion,
            string protectionProviderName, OverrideModeSetting overrideMode, bool skipInChildApps)
        {
            ConfigKey = configKey;
            DefinitionConfigPath = definitionConfigPath;
            TargetConfigPath = targetConfigPath;
            SubPath = subPath;
            Filename = filename;
            LineNumber = lineNumber;
            StreamVersion = streamVersion;
            RawXml = rawXml;
            ConfigSource = configSource;
            ConfigSourceStreamName = configSourceStreamName;
            ProtectionProviderName = protectionProviderName;
            OverrideModeSetting = overrideMode;
            SkipInChildApps = skipInChildApps;
        }

        // other access methods
        internal object StreamVersion { get; set; }

        internal string ConfigSource { get; set; }

        internal string ConfigSourceStreamName { get; set; }

        internal object ConfigSourceStreamVersion
        {
            set { }
        }

        internal string ConfigKey { get; }

        internal string DefinitionConfigPath { get; }

        internal string TargetConfigPath { get; set; }

        internal string SubPath { get; }

        internal string RawXml { get; set; }

        internal string ProtectionProviderName { get; set; }

        internal OverrideModeSetting OverrideModeSetting { get; set; }

        internal bool SkipInChildApps { get; set; }

        public string Filename { get; }

        public int LineNumber { get; set; }
    }
}