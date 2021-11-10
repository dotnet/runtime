// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;

using ILCompiler.IBC;

using Internal.Pgo;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public class ProfileDataManager
    {
        private readonly IBCProfileParser _ibcParser;
        private readonly List<ProfileData> _inputData = new List<ProfileData>();
        private readonly Dictionary<MethodDesc, MethodProfileData> _mergedProfileData = new Dictionary<MethodDesc, MethodProfileData>();
        private readonly Dictionary<ModuleDesc, HashSet<MethodDesc>> _placedProfileMethods = new Dictionary<ModuleDesc, HashSet<MethodDesc>>();
        private readonly HashSet<MethodDesc> _placedProfileMethodsAll = new HashSet<MethodDesc>();
        private readonly bool _partialNGen;
        private readonly ReadyToRunCompilationModuleGroupBase _compilationGroup;
        private readonly CallChainProfile _callChainProfile;

        public ProfileDataManager(Logger logger,
                                  IEnumerable<ModuleDesc> possibleReferenceModules,
                                  IEnumerable<ModuleDesc> inputModules,
                                  IEnumerable<ModuleDesc> versionBubbleModules,
                                  ModuleDesc nonLocalGenericsHome,
                                  IReadOnlyList<string> mibcFiles,
                                  CallChainProfile callChainProfile,
                                  CompilerTypeSystemContext context,
                                  ReadyToRunCompilationModuleGroupBase compilationGroup,
                                  bool embedPgoDataInR2RImage)
        {
            EmbedPgoDataInR2RImage = embedPgoDataInR2RImage;
            _ibcParser = new IBCProfileParser(logger, possibleReferenceModules);
            _compilationGroup = compilationGroup;
            _callChainProfile = callChainProfile;
            HashSet<ModuleDesc> versionBubble = new HashSet<ModuleDesc>(versionBubbleModules);

            {
                // Parse MIbc Data

                string onlyParseItemsDefinedInAssembly = null;
                if (nonLocalGenericsHome == null && !compilationGroup.IsCompositeBuildMode)
                {
                    onlyParseItemsDefinedInAssembly = inputModules.First().Assembly.GetName().Name;
                }
                HashSet<string> versionBubbleModuleStrings = new HashSet<string>();
                foreach (ModuleDesc versionBubbleModule in versionBubble)
                {
                    versionBubbleModuleStrings.Add(versionBubbleModule.Assembly.GetName().Name);
                }

                foreach (string file in mibcFiles)
                {
                    using (PEReader peReader = MIbcProfileParser.OpenMibcAsPEReader(file))
                    {
                        _inputData.Add(MIbcProfileParser.ParseMIbcFile(context, peReader, versionBubbleModuleStrings, onlyParseItemsDefinedInAssembly));
                    }
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
                ProfileData.MergeProfileData(ref _partialNGen, _mergedProfileData, profileData);
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

        public bool EmbedPgoDataInR2RImage { get; }
        public CallChainProfile CallChainProfile => _callChainProfile;
    }
}
