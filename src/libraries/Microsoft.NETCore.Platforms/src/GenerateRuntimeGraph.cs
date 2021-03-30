// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using NuGet.RuntimeModel;

namespace Microsoft.NETCore.Platforms.BuildTasks
{
    public class GenerateRuntimeGraph : BuildTask
    {

        /// <summary>
        /// A set of RuntimeGroups that can be used to generate a runtime graph
        ///   Identity: the base string for the RID, without version architecture, or qualifiers.
        ///   Parent: the base string for the parent of this RID.  This RID will be imported by the baseRID, architecture-specific,
        ///     and qualifier-specific RIDs (with the latter two appending appropriate architecture and qualifiers).
        ///   Versions: A list of strings delimited by semi-colons that represent the versions for this RID.
        ///   TreatVersionsAsCompatible: Default is true.  When true, version-specific RIDs will import the previous
        ///     version-specific RID in the Versions list, with the first version importing the version-less RID.
        ///     When false all version-specific RIDs will import the version-less RID (bypassing previous version-specific RIDs)
        ///   OmitVersionDelimiter: Default is false.  When true no characters will separate the base RID and version (EG: win7).
        ///     When false a '.' will separate the base RID and version (EG: osx.10.12).
        ///   ApplyVersionsToParent: Default is false.  When true, version-specific RIDs will import version-specific Parent RIDs
        ///     similar to is done for architecture and qualifier (see Parent above).
        ///   Architectures: A list of strings delimited by semi-colons that represent the architectures for this RID.
        ///   AdditionalQualifiers: A list of strings delimited by semi-colons that represent the additional qualifiers for this RID.
        ///     Additional qualifers do not stack, each only applies to the qualifier-less RIDs (so as not to cause combinatorial
        ///     exponential growth of RIDs).
        ///
        /// The following options can be used under special circumstances but break the normal precedence rules we try to establish
        /// by generating the RID graph from common logic. These options make it possible to create a RID fallback chain that doesn't
        /// match the rest of the RIDs and therefore is hard for developers/package authors to reason about.
        /// Only use these options for cases where you know what you are doing and have carefully reviewed the resulting RID fallbacks
        /// using the CompatibliltyMap.
        ///   OmitRIDs: A list of strings delimited by semi-colons that represent RIDs calculated from this RuntimeGroup that should
        ///     be omitted from the RuntimeGraph.  These RIDs will not be referenced nor defined.
        ///   OmitRIDDefinitions: A list of strings delimited by semi-colons that represent RIDs calculated from this RuntimeGroup
        ///     that should be omitted from the RuntimeGraph.  These RIDs will not be defined by this RuntimeGroup, but will be
        ///     referenced: useful in case some other RuntimeGroup (or runtime.json template) defines them.
        ///   OmitRIDReferences: A list of strings delimited by semi-colons that represent RIDs calculated from this RuntimeGroup
        ///     that should be omitted from the RuntimeGraph.  These RIDs will be defined but not referenced by this RuntimeGroup.
        /// </summary>
        public ITaskItem[] RuntimeGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Optional source Runtime.json to use as a starting point when merging additional RuntimeGroups
        /// </summary>
        public string SourceRuntimeJson
        {
            get;
            set;
        }

        /// <summary>
        /// Where to write the final runtime.json
        /// </summary>
        public string RuntimeJson
        {
            get;
            set;
        }

        /// <summary>
        /// Optionally, other runtime.jsons which may contain imported RIDs
        /// </summary>
        public string[] ExternalRuntimeJsons
        {
            get;
            set;
        }

        /// <summary>
        /// When defined, specifies the file to write compatibility precedence for each RID in the graph.
        /// </summary>
        public string CompatibilityMap
        {
            get;
            set;
        }


        /// <summary>
        /// True to write the generated runtime.json to RuntimeJson and compatibility map to CompatibilityMap, otherwise files are read and diffed
        /// with generated versions and an error is emitted if they differ.
        /// Setting UpdateRuntimeFiles will overwrite files even when the file is marked ReadOnly.
        /// </summary>
        public bool UpdateRuntimeFiles
        {
            get;
            set;
        }

        /// <summary>
        /// When defined, specifies the file to write a DGML representation of the runtime graph.
        /// </summary>
        public string RuntimeDirectedGraph
        {
            get;
            set;
        }

        public override bool Execute()
        {
            if (RuntimeGroups != null && RuntimeGroups.Any() && RuntimeJson == null)
            {
                Log.LogError($"{nameof(RuntimeJson)} argument must be specified when {nameof(RuntimeGroups)} is specified.");
                return false;
            }

            RuntimeGraph runtimeGraph;
            if (!string.IsNullOrEmpty(SourceRuntimeJson))
            {
                if (!File.Exists(SourceRuntimeJson))
                {
                    Log.LogError($"{nameof(SourceRuntimeJson)} did not exist at {SourceRuntimeJson}.");
                    return false;
                }

                runtimeGraph = JsonRuntimeFormat.ReadRuntimeGraph(SourceRuntimeJson);
            }
            else
            {
                runtimeGraph = new RuntimeGraph();
            }

            foreach (var runtimeGroup in RuntimeGroups.NullAsEmpty().Select(i => new RuntimeGroup(i)))
            {
                runtimeGraph = SafeMerge(runtimeGraph, runtimeGroup);
            }

            Dictionary<string, string> externalRids = new Dictionary<string, string>();
            if (ExternalRuntimeJsons != null)
            {
                foreach (var externalRuntimeJson in ExternalRuntimeJsons)
                {
                    RuntimeGraph externalRuntimeGraph = JsonRuntimeFormat.ReadRuntimeGraph(externalRuntimeJson);

                    foreach (var runtime in externalRuntimeGraph.Runtimes.Keys)
                    {
                        // don't check for duplicates, we merely care what is external
                        externalRids.Add(runtime, externalRuntimeJson);
                    }
                }
            }

            ValidateImports(runtimeGraph, externalRids);

            if (!string.IsNullOrEmpty(RuntimeJson))
            {
                if (UpdateRuntimeFiles)
                {
                    EnsureWritable(RuntimeJson);
                    WriteRuntimeGraph(RuntimeJson, runtimeGraph);

                }
                else
                {
                    // validate that existing file matches generated file
                    if (!File.Exists(RuntimeJson))
                    {
                        Log.LogError($"{nameof(RuntimeJson)} did not exist at {RuntimeJson} and {nameof(UpdateRuntimeFiles)} was not specified.");
                    }
                    else
                    {
                        var existingRuntimeGraph = JsonRuntimeFormat.ReadRuntimeGraph(RuntimeJson);

                        if (!existingRuntimeGraph.Equals(runtimeGraph))
                        {
                            Log.LogError($"The generated {nameof(RuntimeJson)} differs from {RuntimeJson} and {nameof(UpdateRuntimeFiles)} was not specified.  Please specify {nameof(UpdateRuntimeFiles)}=true to commit the changes.");
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(CompatibilityMap))
            {
                var compatibilityMap = GetCompatibilityMap(runtimeGraph);
                if (UpdateRuntimeFiles)
                {
                    EnsureWritable(CompatibilityMap);
                    WriteCompatibilityMap(compatibilityMap, CompatibilityMap);
                }
                else
                {
                    // validate that existing file matches generated file
                    if (!File.Exists(CompatibilityMap))
                    {
                        Log.LogError($"{nameof(CompatibilityMap)} did not exist at {CompatibilityMap} and {nameof(UpdateRuntimeFiles)} was not specified.");
                    }
                    else
                    {
                        var existingCompatibilityMap = ReadCompatibilityMap(CompatibilityMap);

                        if (!CompatibilityMapEquals(existingCompatibilityMap, compatibilityMap))
                        {
                            Log.LogError($"The generated {nameof(CompatibilityMap)} differs from {CompatibilityMap} and {nameof(UpdateRuntimeFiles)} was not specified.  Please specify {nameof(UpdateRuntimeFiles)}=true to commit the changes.");
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(RuntimeDirectedGraph))
            {
                WriteRuntimeGraph(runtimeGraph, RuntimeDirectedGraph);
            }

            return !Log.HasLoggedErrors;
        }

        private void EnsureWritable(string file)
        {
            if (File.Exists(file))
            {
                var existingAttributes = File.GetAttributes(file);

                if ((existingAttributes & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(file, existingAttributes &= ~FileAttributes.ReadOnly);
                }
            }
        }

        public static void WriteRuntimeGraph(string filePath, RuntimeGraph runtimeGraph)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            using (var textWriter = new StreamWriter(fileStream))
            using (var jsonWriter = new JsonTextWriter(textWriter))
            using (var writer = new JsonObjectWriter(jsonWriter))
            {
                jsonWriter.Formatting = Formatting.Indented;

                // workaround https://github.com/NuGet/Home/issues/9532
                writer.WriteObjectStart();

                JsonRuntimeFormat.WriteRuntimeGraph(writer, runtimeGraph);

                writer.WriteObjectEnd();
            }
        }

        private RuntimeGraph SafeMerge(RuntimeGraph existingGraph, RuntimeGroup runtimeGroup)
        {
            var runtimeGraph = runtimeGroup.GetRuntimeGraph();

            foreach (var existingRuntimeDescription in existingGraph.Runtimes.Values)
            {
                RuntimeDescription newRuntimeDescription;

                if (runtimeGraph.Runtimes.TryGetValue(existingRuntimeDescription.RuntimeIdentifier, out newRuntimeDescription))
                {
                    // overlapping RID, ensure that the imports match (same ordering and content)
                    if (!existingRuntimeDescription.InheritedRuntimes.SequenceEqual(newRuntimeDescription.InheritedRuntimes))
                    {
                        Log.LogError($"RuntimeGroup {runtimeGroup.BaseRID} defines RID {newRuntimeDescription.RuntimeIdentifier} with imports {string.Join(";", newRuntimeDescription.InheritedRuntimes)} which differ from existing imports {string.Join(";", existingRuntimeDescription.InheritedRuntimes)}.  You may avoid this by specifying {nameof(RuntimeGroup.OmitRIDDefinitions)} metadata with {newRuntimeDescription.RuntimeIdentifier}.");
                    }
                }
            }

            return RuntimeGraph.Merge(existingGraph, runtimeGraph);
        }

        private void ValidateImports(RuntimeGraph runtimeGraph, IDictionary<string, string> externalRIDs)
        {
            foreach (var runtimeDescription in runtimeGraph.Runtimes.Values)
            {
                string externalRuntimeJson;

                if (externalRIDs.TryGetValue(runtimeDescription.RuntimeIdentifier, out externalRuntimeJson))
                {
                    Log.LogError($"Runtime {runtimeDescription.RuntimeIdentifier} is defined in both this RuntimeGraph and {externalRuntimeJson}.");
                }

                foreach (var import in runtimeDescription.InheritedRuntimes)
                {
                    if (!runtimeGraph.Runtimes.ContainsKey(import) && !externalRIDs.ContainsKey(import))
                    {
                        Log.LogError($"Runtime {runtimeDescription.RuntimeIdentifier} imports {import} which is not defined.");
                    }
                }
            }
        }

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

            private class RID
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

        private static IDictionary<string, IEnumerable<string>> GetCompatibilityMap(RuntimeGraph graph)
        {
            Dictionary<string, IEnumerable<string>> compatibilityMap = new Dictionary<string, IEnumerable<string>>();

            foreach (var rid in graph.Runtimes.Keys.OrderBy(rid => rid, StringComparer.Ordinal))
            {
                compatibilityMap.Add(rid, graph.ExpandRuntime(rid));
            }

            return compatibilityMap;
        }

        private static IDictionary<string, IEnumerable<string>> ReadCompatibilityMap(string mapFile)
        {
            var serializer = new JsonSerializer();
            using (var file = File.OpenText(mapFile))
            using (var jsonTextReader = new JsonTextReader(file))
            {
                return serializer.Deserialize<IDictionary<string, IEnumerable<string>>>(jsonTextReader);
            }
        }

        private static void WriteCompatibilityMap(IDictionary<string, IEnumerable<string>> compatibilityMap, string mapFile)
        {
            var serializer = new JsonSerializer()
            {
                Formatting = Formatting.Indented,
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
            };

            string directory = Path.GetDirectoryName(mapFile);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var file = File.CreateText(mapFile))
            {
                serializer.Serialize(file, compatibilityMap);
            }
        }

        private static bool CompatibilityMapEquals(IDictionary<string, IEnumerable<string>> left, IDictionary<string, IEnumerable<string>> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            foreach (var leftPair in left)
            {
                IEnumerable<string> rightValue;

                if (!right.TryGetValue(leftPair.Key, out rightValue))
                {
                    return false;
                }

                if (!rightValue.SequenceEqual(leftPair.Value))
                {
                    return false;
                }
            }

            return true;
        }

        private static XNamespace s_dgmlns = @"http://schemas.microsoft.com/vs/2009/dgml";
        private static void WriteRuntimeGraph(RuntimeGraph graph, string dependencyGraphFilePath)
        {

            var doc = new XDocument(new XElement(s_dgmlns + "DirectedGraph"));
            var nodesElement = new XElement(s_dgmlns + "Nodes");
            var linksElement = new XElement(s_dgmlns + "Links");
            doc.Root.Add(nodesElement);
            doc.Root.Add(linksElement);

            var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var runtimeDescription in graph.Runtimes.Values)
            {
                nodesElement.Add(new XElement(s_dgmlns + "Node",
                    new XAttribute("Id", runtimeDescription.RuntimeIdentifier)));

                foreach (var import in runtimeDescription.InheritedRuntimes)
                {
                    linksElement.Add(new XElement(s_dgmlns + "Link",
                        new XAttribute("Source", runtimeDescription.RuntimeIdentifier),
                        new XAttribute("Target", import)));
                }
            }

            using (var file = File.Create(dependencyGraphFilePath))
            {
                doc.Save(file);
            }
        }
    }
}
