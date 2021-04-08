// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.NETCore.Platforms.BuildTasks
{
    internal class RID
    {
        public string BaseRID { get; set; }
        public string VersionDelimiter { get; set; }
        public string Version { get; set; }
        public string ArchitectureDelimiter { get; set; }
        public string Architecture { get; set; }
        public string QualifierDelimiter { get; set; }
        public string Qualifier { get; set; }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(BaseRID);

            if (HasVersion())
            {
                builder.Append(VersionDelimiter);
                builder.Append(Version);
            }

            if (HasArchitecture())
            {
                builder.Append(ArchitectureDelimiter);
                builder.Append(Architecture);
            }

            if (HasQualifier())
            {
                builder.Append(QualifierDelimiter);
                builder.Append(Qualifier);
            }

            return builder.ToString();
        }

        public bool HasVersion()
        {
            return Version != null;
        }

        public bool HasArchitecture()
        {
            return Architecture != null;
        }

        public bool HasQualifier()
        {
            return Qualifier != null;
        }
    }

}
