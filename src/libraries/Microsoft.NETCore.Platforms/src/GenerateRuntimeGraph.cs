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
        /// Additional runtime identifiers to add to the graph.
        /// </summary>
        public string[] AdditionalRuntimeIdentifiers
        {
            get;
            set;
        }

        /// <summary>
        /// Parent RID to use for any unknown AdditionalRuntimeIdentifer.
        /// </summary>
        public string AdditionalRuntimeIdentifierParent
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

            List<RuntimeGroup> runtimeGroups = RuntimeGroups.NullAsEmpty().Select(i => new RuntimeGroup(i)).ToList();

            AddRuntimeIdentifiers(runtimeGroups);

            foreach (var runtimeGroup in runtimeGroups)
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

        private static void EnsureWritable(string file)
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

        private void AddRuntimeIdentifiers(ICollection<RuntimeGroup> runtimeGroups)
        {
            if (AdditionalRuntimeIdentifiers == null || AdditionalRuntimeIdentifiers.Length == 0)
            {
                return;
            }

            RuntimeGroupCollection runtimeGroupCollection = new RuntimeGroupCollection(runtimeGroups);

            foreach (string additionalRuntimeIdentifier in AdditionalRuntimeIdentifiers)
            {
                runtimeGroupCollection.AddRuntimeIdentifier(additionalRuntimeIdentifier, AdditionalRuntimeIdentifierParent);
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
