// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    // Information about a stream used in configuration
    internal class StreamInfo
    {
        // the configSource directive that generated this stream, null for a full config file

        internal StreamInfo(string sectionName, string configSource, string streamName)
        {
            SectionName = sectionName;
            ConfigSource = configSource;
            StreamName = streamName;
        }

        private StreamInfo() { }

        internal string SectionName { get; private set; }

        internal string ConfigSource { get; private set; }

        internal string StreamName { get; private set; }

        internal bool IsMonitored { get; set; }

        internal object Version { get; set; }

        internal StreamInfo Clone()
        {
            StreamInfo clone = new StreamInfo
            {
                SectionName = SectionName,
                ConfigSource = ConfigSource,
                StreamName = StreamName,
                IsMonitored = IsMonitored,
                Version = Version
            };


            return clone;
        }
    }
}