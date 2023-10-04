// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using NuGet.RuntimeModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.NETCore.Platforms.BuildTasks
{
    public class RuntimeGroupCollection
    {
        private readonly ICollection<RuntimeGroup> allRuntimeGroups;
        private readonly Dictionary<string, List<RuntimeGroup>> runtimeGroupsByBaseRID;
        private readonly HashSet<RID> knownRIDs;

        public RuntimeGroupCollection(ICollection<RuntimeGroup> runtimeGroups)
        {
            allRuntimeGroups = runtimeGroups;
            runtimeGroupsByBaseRID = runtimeGroups.GroupBy(rg => rg.BaseRID).ToDictionary(g => g.Key, g => new List<RuntimeGroup>(g.AsEnumerable()));

            knownRIDs = new HashSet<RID>(allRuntimeGroups.SelectMany(rg => rg.GetRIDMappings()).Select(mapping => mapping.RuntimeIdentifier));
        }

        /// <summary>
        /// Locate an existing RuntimeGroup to append to.
        /// Existing group must have matching baseRID, then we choose based on closest version,
        /// and prefer matching arch and qualifier.
        /// If no match is found, then a new RID hierarchy is created.
        /// </summary>
        /// <param name="runtimeIdentifier"></param>
        /// <param name="parent"></param>
        public void AddRuntimeIdentifier(string runtimeIdentifier, string parent)
        {
            // don't parse qualifier since we don't use them and they are ambiguous with `-` in base RID
            RID rid = RID.Parse(runtimeIdentifier, noQualifier: true);

            AddRuntimeIdentifier(rid, parent);
        }

        public void AddRuntimeIdentifier(RID rid, string parent)
        {
            // Do nothing if we already know about the RID
            if (knownRIDs.Contains(rid))
            {
                return;
            }

            RuntimeGroup runtimeGroup = null;

            if (runtimeGroupsByBaseRID.TryGetValue(rid.BaseRID, out var candidateRuntimeGroups))
            {
                RuntimeVersion closestVersion = null;

                foreach (var candidate in candidateRuntimeGroups)
                {
                    if (rid.HasVersion)
                    {
                        // Find the closest previous version
                        foreach (var version in candidate.Versions)
                        {
                            // a previous version
                            if (version <= rid.Version)
                            {
                                // haven't yet found a match or this is a closer match
                                if (closestVersion == null || version > closestVersion)
                                {
                                    closestVersion = version;
                                    runtimeGroup = candidate;
                                }
                                else if (version == closestVersion)
                                {
                                    // found a tie in version, examine other fields
                                    considerCandidate();
                                }
                            }
                        }
                    }

                    // if we don't have a version, or if we couldn't find any match, consider other fields
                    if (!rid.HasVersion)
                    {
                        considerCandidate();
                    }

                    // if we don't have a match yet, take this as it matches on baseRID
                    runtimeGroup ??= candidate;

                    void considerCandidate()
                    {
                        // is this a better match?
                        if (!rid.HasArchitecture || candidate.Architectures.Contains(rid.Architecture))
                        {
                            if (!rid.HasQualifier || candidate.AdditionalQualifiers.Contains(rid.Qualifier))
                            {
                                // matched on arch and qualifier.
                                runtimeGroup = candidate;
                            }
                            else if (rid.HasArchitecture && !runtimeGroup.Architectures.Contains(rid.Architecture))
                            {
                                // matched only on arch and existing match doesn't match arch
                                runtimeGroup = candidate;
                            }
                        }
                    }
                }

                Debug.Assert(runtimeGroup != null, "Empty candidates?");
            }
            else
            {
                // This is an unknown base RID, we'll need to add a new group.
                if (string.IsNullOrEmpty(parent))
                {
                    throw new InvalidOperationException($"AdditionalRuntimeIdentifier {rid} was specified, which could not be found in any existing {nameof(RuntimeGroup)}, and no {nameof(parent)} was specified.");
                }

                runtimeGroup = new RuntimeGroup(rid.BaseRID, parent);

                AddRuntimeGroup(runtimeGroup);
            }

            runtimeGroup.ApplyRid(rid);

            // Compute the portion of the RID graph produced from this modified RuntimeGroup
            var ridMappings = runtimeGroup.GetRIDMappings();

            // Record any newly defined RIDs in our set of known RIDs
            foreach (RID definedRID in ridMappings.Select(mapping => mapping.RuntimeIdentifier))
            {
                knownRIDs.Add(definedRID);
            }

            // Make sure that any RID imported is added as well.  This allows users to specify
            // a single new RID and we'll add any new RIDs up the parent chain that might be needed.
            foreach (RID importedRID in ridMappings.SelectMany(mapping => mapping.Imports))
            {
                // This should not introduce any new RuntimeGroups, so we specify parent as null
                AddRuntimeIdentifier(importedRID, null);
            }

        }

        private void AddRuntimeGroup(RuntimeGroup runtimeGroup)
        {
            List<RuntimeGroup> baseRuntimeGroups;

            if (!runtimeGroupsByBaseRID.TryGetValue(runtimeGroup.BaseRID, out baseRuntimeGroups))
            {
                runtimeGroupsByBaseRID[runtimeGroup.BaseRID] = baseRuntimeGroups = new List<RuntimeGroup>();
            }

            baseRuntimeGroups.Add(runtimeGroup);
            allRuntimeGroups.Add(runtimeGroup);
        }

    }
}
