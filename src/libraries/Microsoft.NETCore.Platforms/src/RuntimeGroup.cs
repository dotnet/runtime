// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using NuGet.RuntimeModel;

namespace Microsoft.NETCore.Platforms.BuildTasks
{

    internal class RuntimeGroup
    {
        private const string rootRID = "any";
        private const char VersionDelimiter = '.';
        private const char ArchitectureDelimiter = '-';
        private const char QualifierDelimiter = '-';

        public RuntimeGroup(ITaskItem item)
        {
            BaseRID = item.ItemSpec;
            Parent = item.GetString(nameof(Parent));
            Versions = item.GetStrings(nameof(Versions));
            TreatVersionsAsCompatible = item.GetBoolean(nameof(TreatVersionsAsCompatible), true);
            OmitVersionDelimiter = item.GetBoolean(nameof(OmitVersionDelimiter));
            ApplyVersionsToParent = item.GetBoolean(nameof(ApplyVersionsToParent));
            Architectures = item.GetStrings(nameof(Architectures));
            AdditionalQualifiers = item.GetStrings(nameof(AdditionalQualifiers));
            OmitRIDs = new HashSet<string>(item.GetStrings(nameof(OmitRIDs)));
            OmitRIDDefinitions = new HashSet<string>(item.GetStrings(nameof(OmitRIDDefinitions)));
            OmitRIDReferences = new HashSet<string>(item.GetStrings(nameof(OmitRIDReferences)));
        }

        public string BaseRID { get; }
        public string Parent { get; }
        public IEnumerable<string> Versions { get; }
        public bool TreatVersionsAsCompatible { get; }
        public bool OmitVersionDelimiter { get; }
        public bool ApplyVersionsToParent { get; }
        public IEnumerable<string> Architectures { get; }
        public IEnumerable<string> AdditionalQualifiers { get; }
        public ICollection<string> OmitRIDs { get; }
        public ICollection<string> OmitRIDDefinitions { get; }
        public ICollection<string> OmitRIDReferences { get; }

        private class RIDMapping
        {
            public RIDMapping(RID runtimeIdentifier)
            {
                RuntimeIdentifier = runtimeIdentifier;
                Imports = Enumerable.Empty<RID>();
            }

            public RIDMapping(RID runtimeIdentifier, IEnumerable<RID> imports)
            {
                RuntimeIdentifier = runtimeIdentifier;
                Imports = imports;
            }

            public RID RuntimeIdentifier { get; }

            public IEnumerable<RID> Imports { get; }
        }

        private RID CreateRuntime(string baseRid, string version = null, string architecture = null, string qualifier = null)
        {
            return new RID()
            {
                BaseRID = baseRid,
                VersionDelimiter = OmitVersionDelimiter ? string.Empty : VersionDelimiter.ToString(),
                Version = version,
                ArchitectureDelimiter = ArchitectureDelimiter.ToString(),
                Architecture = architecture,
                QualifierDelimiter = QualifierDelimiter.ToString(),
                Qualifier = qualifier
            };
        }

        private IEnumerable<RIDMapping> GetRIDMappings()
        {
            // base =>
            //      Parent
            yield return Parent == null ?
                new RIDMapping(CreateRuntime(BaseRID)) :
                new RIDMapping(CreateRuntime(BaseRID), new[] { CreateRuntime(Parent) });

            foreach (var architecture in Architectures)
            {
                // base + arch =>
                //      base,
                //      parent + arch
                var imports = new List<RID>()
                    {
                        CreateRuntime(BaseRID)
                    };

                if (!IsNullOrRoot(Parent))
                {
                    imports.Add(CreateRuntime(Parent, architecture: architecture));
                }

                yield return new RIDMapping(CreateRuntime(BaseRID, architecture: architecture), imports);
            }

            string lastVersion = null;
            foreach (var version in Versions)
            {
                // base + version =>
                //      base + lastVersion,
                //      parent + version (optionally)
                var imports = new List<RID>()
                    {
                        CreateRuntime(BaseRID, version: lastVersion)
                    };

                if (ApplyVersionsToParent)
                {
                    imports.Add(CreateRuntime(Parent, version: version));
                }

                yield return new RIDMapping(CreateRuntime(BaseRID, version: version), imports);

                foreach (var architecture in Architectures)
                {
                    // base + version + architecture =>
                    //      base + version,
                    //      base + lastVersion + architecture,
                    //      parent + version + architecture (optionally)
                    var archImports = new List<RID>()
                        {
                            CreateRuntime(BaseRID, version: version),
                            CreateRuntime(BaseRID, version: lastVersion, architecture: architecture)
                        };

                    if (ApplyVersionsToParent)
                    {
                        archImports.Add(CreateRuntime(Parent, version: version, architecture: architecture));
                    }

                    yield return new RIDMapping(CreateRuntime(BaseRID, version: version, architecture: architecture), archImports);
                }

                if (TreatVersionsAsCompatible)
                {
                    lastVersion = version;
                }
            }

            foreach (var qualifier in AdditionalQualifiers)
            {
                // base + qual =>
                //      base,
                //      parent + qual
                yield return new RIDMapping(CreateRuntime(BaseRID, qualifier: qualifier),
                    new[]
                    {
                            CreateRuntime(BaseRID),
                            IsNullOrRoot(Parent) ? CreateRuntime(qualifier) : CreateRuntime(Parent, qualifier:qualifier)
                    });

                foreach (var architecture in Architectures)
                {
                    // base + arch + qualifier =>
                    //      base + qualifier,
                    //      base + arch
                    //      parent + arch + qualifier
                    var imports = new List<RID>()
                        {
                            CreateRuntime(BaseRID, qualifier: qualifier),
                            CreateRuntime(BaseRID, architecture: architecture)
                        };

                    if (!IsNullOrRoot(Parent))
                    {
                        imports.Add(CreateRuntime(Parent, architecture: architecture, qualifier: qualifier));
                    }

                    yield return new RIDMapping(CreateRuntime(BaseRID, architecture: architecture, qualifier: qualifier), imports);
                }

                lastVersion = null;
                foreach (var version in Versions)
                {
                    // base + version + qualifier =>
                    //      base + version,
                    //      base + lastVersion + qualifier
                    //      parent + version + qualifier (optionally)
                    var imports = new List<RID>()
                        {
                            CreateRuntime(BaseRID, version: version),
                            CreateRuntime(BaseRID, version: lastVersion, qualifier: qualifier)
                        };

                    if (ApplyVersionsToParent)
                    {
                        imports.Add(CreateRuntime(Parent, version: version, qualifier: qualifier));
                    }

                    yield return new RIDMapping(CreateRuntime(BaseRID, version: version, qualifier: qualifier), imports);

                    foreach (var architecture in Architectures)
                    {
                        // base + version + architecture + qualifier =>
                        //      base + version + qualifier,
                        //      base + version + architecture,
                        //      base + version,
                        //      base + lastVersion + architecture + qualifier,
                        //      parent + version + architecture + qualifier (optionally)
                        var archImports = new List<RID>()
                            {
                                CreateRuntime(BaseRID, version: version, qualifier: qualifier),
                                CreateRuntime(BaseRID, version: version, architecture: architecture),
                                CreateRuntime(BaseRID, version: version),
                                CreateRuntime(BaseRID, version: lastVersion, architecture: architecture, qualifier: qualifier)
                            };

                        if (ApplyVersionsToParent)
                        {
                            imports.Add(CreateRuntime(Parent, version: version, architecture: architecture, qualifier: qualifier));
                        }

                        yield return new RIDMapping(CreateRuntime(BaseRID, version: version, architecture: architecture, qualifier: qualifier), archImports);
                    }

                    if (TreatVersionsAsCompatible)
                    {
                        lastVersion = version;
                    }
                }
            }
        }

        private bool IsNullOrRoot(string rid)
        {
            return rid == null || rid == rootRID;
        }


        public IEnumerable<RuntimeDescription> GetRuntimeDescriptions()
        {
            foreach (var mapping in GetRIDMappings())
            {
                var rid = mapping.RuntimeIdentifier.ToString();

                if (OmitRIDs.Contains(rid) || OmitRIDDefinitions.Contains(rid))
                {
                    continue;
                }

                var imports = mapping.Imports
                       .Select(i => i.ToString())
                       .Where(i => !OmitRIDs.Contains(i) && !OmitRIDReferences.Contains(i))
                       .ToArray();

                yield return new RuntimeDescription(rid, imports);
            }
        }

        public RuntimeGraph GetRuntimeGraph()
        {
            return new RuntimeGraph(GetRuntimeDescriptions());
        }
    }
}
