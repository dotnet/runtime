// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using ILCompiler.IBC;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    [Flags]
    public enum MethodProfilingDataFlags
    {
        // Important: update toolbox\ibcmerge\ibcmerge.cs if you change these
        ReadMethodCode = 0,  // 0x00001  // Also means the method was executed
        ReadMethodDesc = 1,  // 0x00002
        RunOnceMethod = 2,  // 0x00004
        RunNeverMethod = 3,  // 0x00008
                             //  MethodStoredDataAccess        = 4,  // 0x00010  // obsolete
        WriteMethodDesc = 5,  // 0x00020
                              //  ReadFCallHash                 = 6,  // 0x00040  // obsolete
        ReadGCInfo = 7,  // 0x00080
        CommonReadGCInfo = 8,  // 0x00100
                               //  ReadMethodDefRidMap           = 9,  // 0x00200  // obsolete
        ReadCerMethodList = 10, // 0x00400
        ReadMethodPrecode = 11, // 0x00800
        WriteMethodPrecode = 12, // 0x01000
        ExcludeHotMethodCode = 13, // 0x02000  // Hot method should be excluded from the ReadyToRun image
        ExcludeColdMethodCode = 14, // 0x04000  // Cold method should be excluded from the ReadyToRun image
        DisableInlining = 15, // 0x08000  // Disable inlining of this method in optimized AOT native code
    }

    public class MethodProfileData
    {
        public MethodProfileData(MethodDesc method, MethodProfilingDataFlags flags, double exclusiveWeight, Dictionary<MethodDesc, int> callWeights, uint scenarioMask)
        {
            Method = method;
            Flags = flags;
            ScenarioMask = scenarioMask;
            ExclusiveWeight = exclusiveWeight;
            CallWeights = callWeights;
        }

        public readonly MethodDesc Method;
        public readonly MethodProfilingDataFlags Flags;
        public readonly uint ScenarioMask;
        public readonly double ExclusiveWeight;
        public readonly Dictionary<MethodDesc, int> CallWeights;
    }

    public abstract class ProfileData
    {
        public abstract bool PartialNGen { get; }
        public abstract MethodProfileData GetMethodProfileData(MethodDesc m);
        public abstract IEnumerable<MethodProfileData> GetAllMethodProfileData();
        public abstract byte[] GetMethodBlockCount(MethodDesc m);
    }

    public class EmptyProfileData : ProfileData
    {
        private static readonly EmptyProfileData s_singleton = new EmptyProfileData();

        private EmptyProfileData()
        {
        }

        public override bool PartialNGen => false;

        public static EmptyProfileData Singleton => s_singleton;

        public override MethodProfileData GetMethodProfileData(MethodDesc m)
        {
            return null;
        }

        public override IEnumerable<MethodProfileData> GetAllMethodProfileData()
        {
            return Array.Empty<MethodProfileData>();
        }

        public override byte[] GetMethodBlockCount(MethodDesc m)
        {
            return null;
        }
    }


    public class ProfileDataManager
    {
        private readonly IBCProfileParser _ibcParser;
        private readonly List<ProfileData> _inputData = new List<ProfileData>();
        private readonly Dictionary<MethodDesc, MethodProfileData> _mergedProfileData = new Dictionary<MethodDesc, MethodProfileData>();
        private readonly Dictionary<ModuleDesc, HashSet<MethodDesc>> _placedProfileMethods = new Dictionary<ModuleDesc, HashSet<MethodDesc>>();
        private readonly HashSet<MethodDesc> _placedProfileMethodsAll = new HashSet<MethodDesc>();
        private readonly bool _partialNGen;
        private readonly ReadyToRunCompilationModuleGroupBase _compilationGroup;

        public ProfileDataManager(Logger logger,
                                  IEnumerable<ModuleDesc> possibleReferenceModules,
                                  IEnumerable<ModuleDesc> inputModules,
                                  IEnumerable<ModuleDesc> versionBubbleModules,
                                  ModuleDesc nonLocalGenericsHome,
                                  IReadOnlyList<string> mibcFiles,
                                  CompilerTypeSystemContext context,
                                  ReadyToRunCompilationModuleGroupBase compilationGroup)
        {
            _ibcParser = new IBCProfileParser(logger, possibleReferenceModules);
            _compilationGroup = compilationGroup;
            HashSet<ModuleDesc> versionBubble = new HashSet<ModuleDesc>(versionBubbleModules);

            {
                // Parse MIbc Data

                string onlyParseItemsDefinedInAssembly = nonLocalGenericsHome == null ? inputModules.First().Assembly.GetName().Name : null;
                HashSet<string> versionBubbleModuleStrings = new HashSet<string>();
                foreach (ModuleDesc versionBubbleModule in versionBubble)
                {
                    versionBubbleModuleStrings.Add(versionBubbleModule.Assembly.GetName().Name);
                }

                foreach (string file in mibcFiles)
                {
                    _inputData.Add(MIbcProfileParser.ParseMIbcFile(context, file, versionBubbleModuleStrings, onlyParseItemsDefinedInAssembly));
                }
            }

            {
                // Parse Ibc data
                foreach (var module in inputModules)
                {
                    _inputData.Add(_ibcParser.ParseIBCDataFromModule((EcmaModule)module));
                    _placedProfileMethods.Add(module, new HashSet<MethodDesc>());
                }
            }

            // Merge all data together
            foreach (ProfileData profileData in _inputData)
            {
                MergeProfileData(ref _partialNGen, _mergedProfileData, profileData);
            }

            // With the merged data find the set of methods to be placed within this module
            foreach (var profileData in _mergedProfileData)
            {
                // If the method is not excluded from processing
                if (!profileData.Value.Flags.HasFlag(MethodProfilingDataFlags.ExcludeHotMethodCode) &&
                    !profileData.Value.Flags.HasFlag(MethodProfilingDataFlags.ExcludeColdMethodCode))
                {
                    // Check for methods which are defined within the version bubble, and only rely on other modules within the bubble
                    if (!_compilationGroup.VersionsWithMethodBody(profileData.Key))
                        continue; // Method not contained within version bubble

                    if (_compilationGroup.ContainsType(profileData.Key.OwningType) &&
                        (profileData.Key.OwningType is MetadataType declaringType))
                    {
                        // In this case the method is placed in its natural home (which is the defining module of the method)
                        _placedProfileMethods[declaringType.Module].Add(profileData.Key);
                        _placedProfileMethodsAll.Add(profileData.Key);
                    }
                    else
                    {
                        // If the defining module is not within the input set, if the nonLocalGenericsHome is provided, place it there
                        if ((nonLocalGenericsHome != null) && (profileData.Key.GetTypicalMethodDefinition() != profileData.Key))
                        {
                            _placedProfileMethods[nonLocalGenericsHome].Add(profileData.Key);
                            _placedProfileMethodsAll.Add(profileData.Key);
                        }
                    }
                }
            }
        }

        private void MergeProfileData(ref bool partialNgen, Dictionary<MethodDesc, MethodProfileData> mergedProfileData, ProfileData profileData)
        {
            if (profileData.PartialNGen)
                partialNgen = true;

            foreach (MethodProfileData data in profileData.GetAllMethodProfileData())
            {
                MethodProfileData dataToMerge;
                if (mergedProfileData.TryGetValue(data.Method, out dataToMerge))
                {
                    var mergedCallWeights = data.CallWeights;
                    if (mergedCallWeights == null)
                    {
                        mergedCallWeights = dataToMerge.CallWeights;
                    }
                    else if (dataToMerge.CallWeights != null)
                    {
                        mergedCallWeights = new Dictionary<MethodDesc, int>(data.CallWeights);
                        foreach (var entry in dataToMerge.CallWeights)
                        {
                            if (mergedCallWeights.TryGetValue(entry.Key, out var initialWeight))
                            {
                                mergedCallWeights[entry.Key] = initialWeight + entry.Value;
                            }
                            else
                            {
                                mergedCallWeights[entry.Key] = entry.Value;
                            }
                        }
                    }
                    mergedProfileData[data.Method] = new MethodProfileData(data.Method, dataToMerge.Flags | data.Flags, data.ExclusiveWeight + dataToMerge.ExclusiveWeight, mergedCallWeights, dataToMerge.ScenarioMask | data.ScenarioMask);
                }
                else
                {
                    mergedProfileData.Add(data.Method, data);
                }
            }
        }

        /// <summary>
        /// Get the defining module for a method which is entirely defined within the version bubble
        /// If a module is a generic which has interaction modules outside of the version bubble, return null.
        /// </summary>
        private ModuleDesc GetDefiningModuleForMethodWithinVersionBubble(MethodDesc method, HashSet<ModuleDesc> versionBubble)
        {
            if (_compilationGroup.VersionsWithMethodBody(method) && (method.OwningType is MetadataType metadataType))
            {
                return metadataType.Module;
            }

            return null;
        }

        public IEnumerable<MethodDesc> GetMethodsForModuleDesc(ModuleDesc moduleDesc)
        {
            if (_placedProfileMethods.TryGetValue(moduleDesc, out var precomputedProfileData))
                return precomputedProfileData.ToArray();

            return Array.Empty<MethodDesc>();
        }

        public bool IsMethodInProfileData(MethodDesc method)
        {
            return _placedProfileMethodsAll.Contains(method);
        }

        public MethodProfileData this[MethodDesc method]
        {
            get
            {
                _mergedProfileData.TryGetValue(method, out var profileData);
                return profileData;
            }
        }
    }
}
