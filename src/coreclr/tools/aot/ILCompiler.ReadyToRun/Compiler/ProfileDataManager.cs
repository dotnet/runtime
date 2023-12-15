// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using ILCompiler.IBC;
using Internal.IL;
using Internal.Pgo;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public class ProfileDataManager
    {
        private readonly IBCProfileParser _ibcParser;
        private readonly List<ProfileData> _inputData = new List<ProfileData>();
        // Profile data specified on the command line as .mibc files. This
        // cannot be modified after loading and is accessed concurrently
        // without locks.
        private readonly ProfileDataMap _inputProfileData;
        // When synthesis is enabled, this represents the map of methods with
        // synthesized PGO data. This is accessed and modified concurrently.
        private readonly ProfileDataMap _synthesizedProfileData;
        private readonly ReadyToRunCompilationModuleGroupBase _compilationGroup;
        private readonly CallChainProfile _callChainProfile;
        private readonly GdvEntityFinder _gdvEntityFinder;

        public ProfileDataManager(Logger logger,
                                  IEnumerable<ModuleDesc> possibleReferenceModules,
                                  IEnumerable<ModuleDesc> inputModules,
                                  IEnumerable<ModuleDesc> versionBubbleModules,
                                  IEnumerable<ModuleDesc> crossModuleInlineModules,
                                  ModuleDesc nonLocalGenericsHome,
                                  IReadOnlyList<string> mibcFiles,
                                  MIbcProfileParser.MibcGroupParseRules parseRule,
                                  CallChainProfile callChainProfile,
                                  CompilerTypeSystemContext context,
                                  ReadyToRunCompilationModuleGroupBase compilationGroup,
                                  bool embedPgoDataInR2RImage,
                                  bool parseIbcData,
                                  Func<MethodDesc, bool> canBeIncludedInCurrentCompilation,
                                  bool synthesizeRandomPgoData)
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

                HashSet<string> crossModuleStrings = new HashSet<string>();
                foreach (ModuleDesc crossModule in crossModuleInlineModules)
                {
                    crossModuleStrings.Add(crossModule.Assembly.GetName().Name);
                }

                foreach (string file in mibcFiles)
                {
                    using (PEReader peReader = MIbcProfileParser.OpenMibcAsPEReader(file))
                    {
                        _inputData.Add(MIbcProfileParser.ParseMIbcFile(context, peReader, versionBubbleModuleStrings, onlyParseItemsDefinedInAssembly, crossModuleInlineModules: crossModuleStrings, parseRule: parseRule));
                    }
                }
            }

            if (parseIbcData)
            {
                // Parse Ibc data
                foreach (var module in inputModules)
                {
                    _inputData.Add(_ibcParser.ParseIBCDataFromModule((EcmaModule)module));
                }
            }

            _inputProfileData = new ProfileDataMap(nonLocalGenericsHome, _compilationGroup);
            _inputProfileData.LoadByMerging(_inputData);

            if (synthesizeRandomPgoData)
            {
                _gdvEntityFinder = new GdvEntityFinder(context, versionBubble);
                _synthesizedProfileData = new ProfileDataMap(nonLocalGenericsHome, _compilationGroup);
            }
        }

        public IEnumerable<MethodDesc> GetInputProfileDataMethodsForModule(ModuleDesc moduleDesc)
        {
            return _inputProfileData.GetPlacedMethodsForModuleDesc(moduleDesc);
        }

        public bool IsMethodInInputProfileData(MethodDesc method)
        {
            return _inputProfileData.Contains(method);
        }

        public MethodProfileData this[MethodDesc method]
        {
            get
            {
                MethodProfileData mpd = _inputProfileData[method];
                if (mpd == null && _synthesizedProfileData != null)
                {
                    lock (_synthesizedProfileData)
                    {
                        mpd = _synthesizedProfileData[method];
                    }
                }

                return mpd;
            }
        }

        public bool EmbedPgoDataInR2RImage { get; }
        public bool SynthesizeRandomPgoData => _synthesizedProfileData != null;
        public CallChainProfile CallChainProfile => _callChainProfile;

        public MethodProfileData GetAllowSynthesis(Compilation comp, MethodDesc method, out bool isSynthesized)
        {
            MethodProfileData existingProfileData = _inputProfileData[method];
            if (existingProfileData != null || _synthesizedProfileData == null)
            {
                isSynthesized = false;
                return existingProfileData;
            }

            isSynthesized = true;

            lock (_synthesizedProfileData)
            {
                existingProfileData = _synthesizedProfileData[method];
            }

            if (existingProfileData != null)
            {
                return existingProfileData;
            }

            MethodProfileData profileData = null;
            // We only support synthesizing PGO data for normal methods.
            if (method.GetTypicalMethodDefinition() is EcmaMethod)
            {
                profileData = new MethodProfileData(method, MethodProfilingDataFlags.ReadMethodCode, 0, null, 0, SynthesizeSchema(comp, method));

                lock (_synthesizedProfileData)
                {
                    // We may race here but SynthesizeSchema is deterministic.
                    // Still, ensure that all threads end up seeing the same
                    // profile data instance.
                    existingProfileData = _synthesizedProfileData[method];
                    if (existingProfileData != null)
                        profileData = existingProfileData;
                    else
                        _synthesizedProfileData.Add(profileData);
                }
            }

            return profileData;
        }

        private PgoSchemaElem[] SynthesizeSchema(Compilation comp, MethodDesc method)
        {
            Random rand = new Random(method.GetHashCode());
            // We may be asked to synthesize PGO data for methods not in the
            // compilation group due to cross-version bubble inlining (e.g.
            // System.Object..ctor).
            if (!_compilationGroup.ContainsMethodBody(method, false))
            {
                return null;
            }

            List<PgoSchemaElem> elems = new();
            MethodIL il = comp.GetMethodIL(method);
            ILReader reader = new ILReader(il.GetILBytes());
            while (reader.HasNext)
            {
                int instructionOffset = reader.Offset;
                ILOpcode opcode = reader.ReadILOpcode();
                if (opcode is ILOpcode.call or ILOpcode.callvirt)
                {
                    int token = reader.ReadILToken();
                    object obj = il.GetObject(token);
                    MethodDesc targetMeth = obj as MethodDesc;
                    if (targetMeth == null)
                        continue;

                    if (targetMeth.Signature.IsStatic || !targetMeth.IsTypicalMethodDefinition)
                        continue;

                    bool isDelegateInvoke = targetMeth.OwningType.IsDelegate && targetMeth.Name == "Invoke";

                    if (opcode != ILOpcode.callvirt && !isDelegateInvoke)
                        continue;

                    List<TypeSystemEntityOrUnknown> entities;
                    PgoInstrumentationKind kind;

                    if (isDelegateInvoke)
                    {
                        entities = SampleMethodsCompatibleWithDelegateInvocation(targetMeth.Signature, rand);
                        kind = PgoInstrumentationKind.HandleHistogramMethods;
                    }
                    else
                    {
                        entities = SampleImplementers(targetMeth.OwningType, rand);
                        kind = PgoInstrumentationKind.HandleHistogramTypes;
                    }

                    if (entities.Count <= 0)
                        continue;

                    elems.Add(
                        new PgoSchemaElem
                        {
                            InstrumentationKind = PgoInstrumentationKind.HandleHistogramIntCount,
                            Count = 1,
                            ILOffset = instructionOffset,
                            DataLong = 104729,
                        });

                    elems.Add(
                        new PgoSchemaElem
                        {
                            InstrumentationKind = kind,
                            Count = entities.Count,
                            ILOffset = instructionOffset,
                            DataObject = entities.ToArray()
                        });
                }
                else
                {
                    reader.Skip(opcode);
                }
            }

            if (elems.Count > 0)
            {
                return elems.ToArray();
            }
            else
            {
                return null;
            }
        }

        private List<TypeSystemEntityOrUnknown> SampleMethodsCompatibleWithDelegateInvocation(MethodSignature delegateSignature, Random rand)
        {
            Debug.Assert(_gdvEntityFinder != null);
            IList<MethodDesc> compatible = _gdvEntityFinder.GetCompatibleWithDelegateInvoke(delegateSignature);

            return PickSample(compatible, rand);
        }

        private List<TypeSystemEntityOrUnknown> SampleImplementers(TypeDesc type, Random rand)
        {
            Debug.Assert(_gdvEntityFinder != null);
            IList<TypeDesc> implementers = _gdvEntityFinder.GetImplementers(type);

            return PickSample(implementers, rand);
        }

        private List<TypeSystemEntityOrUnknown> PickSample<T>(IList<T> list, Random rand) where T : TypeSystemEntity
        {
            const int sampleSize = 3;
            List<TypeSystemEntityOrUnknown> result = new(sampleSize);

            static TypeSystemEntityOrUnknown Create(T value)
            {
                Debug.Assert(value is MethodDesc or TypeDesc);
                if (value is MethodDesc md)
                    return new TypeSystemEntityOrUnknown(md);
                if (value is TypeDesc type)
                    return new TypeSystemEntityOrUnknown(type);

                throw new UnreachableException();
            }

            if (list.Count <= sampleSize)
            {
                foreach (T value in list)
                {
                    result.Add(Create(value));
                }
            }
            else
            {
                while (result.Count < sampleSize)
                {
                    int index = rand.Next(list.Count);
                    T value = list[index];
                    TypeSystemEntityOrUnknown schemaValue = Create(value);

                    if (!result.Contains(schemaValue))
                    {
                        result.Add(schemaValue);
                    }
                }
            }

            return result;
        }

        private class ProfileDataMap
        {
            private readonly ModuleDesc _nonLocalGenericsHome;
            private readonly ReadyToRunCompilationModuleGroupBase _compilationGroup;
            private readonly Dictionary<MethodDesc, MethodProfileData> _profileData = new();
            private readonly Dictionary<ModuleDesc, HashSet<MethodDesc>> _placedProfileMethods = new();
            private readonly HashSet<MethodDesc> _placedProfileMethodsAll = new();

            public ProfileDataMap(ModuleDesc nonLocalGenericsHome, ReadyToRunCompilationModuleGroupBase compilationGroup)
            {
                _nonLocalGenericsHome = nonLocalGenericsHome;
                _compilationGroup = compilationGroup;
            }

            public MethodProfileData this[MethodDesc method]
                => _profileData.GetValueOrDefault(method);

            public void LoadByMerging(IEnumerable<ProfileData> data)
            {
                // Merge all data together
                foreach (ProfileData profileData in data)
                {
                    ProfileData.MergeProfileData(_profileData, profileData);
                }

                // With the merged data find the set of methods to be placed within this module
                foreach ((MethodDesc method, MethodProfileData profileData) in _profileData)
                {
                    AssociateMethodProfileDataWithModule(method, profileData);
                }
            }

            public bool Contains(MethodDesc md) => _profileData.ContainsKey(md);
 
            private void AssociateMethodProfileDataWithModule(MethodDesc method, MethodProfileData profileData)
            {
                // If the method is not excluded from processing
                if (profileData.Flags.HasFlag(MethodProfilingDataFlags.ExcludeHotMethodCode) ||
                    profileData.Flags.HasFlag(MethodProfilingDataFlags.ExcludeColdMethodCode))
                {
                    return;
                }

                // Check for methods which are defined within the version bubble, and only rely on other modules within the bubble
                if (!_compilationGroup.VersionsWithMethodBody(method) && !_compilationGroup.CrossModuleCompileable(method))
                    return; // Method not contained within version bubble and not cross module compileable

                ModuleDesc home = null;
                if (_compilationGroup.ContainsType(method.OwningType) &&
                    (method.OwningType is MetadataType declaringType))
                {
                    // In this case the method is placed in its natural home (which is the defining module of the method)
                    home = declaringType.Module;
                }
                else
                {
                    // If the defining module is not within the input set, if the nonLocalGenericsHome is provided, place it there
                    if ((_nonLocalGenericsHome != null) && (method.GetTypicalMethodDefinition() != method))
                    {
                        home = _nonLocalGenericsHome;
                    }
                }

                if (home != null)
                {
                    if (!_placedProfileMethods.TryGetValue(home, out HashSet<MethodDesc> set))
                        _placedProfileMethods.Add(home, set = new HashSet<MethodDesc>());

                    set.Add(method);
                    _placedProfileMethodsAll.Add(method);
                }
            }

            public IEnumerable<MethodDesc> GetPlacedMethodsForModuleDesc(ModuleDesc moduleDesc)
            {
                if (_placedProfileMethods.TryGetValue(moduleDesc, out var precomputedProfileData))
                    return precomputedProfileData;

                return Array.Empty<MethodDesc>();
            }

            public void Add(MethodProfileData profileData)
            {
                _profileData.Add(profileData.Method, profileData);
                AssociateMethodProfileDataWithModule(profileData.Method, profileData);
            }
        }

        private class GdvEntityFinder
        {
            // Currently, we just use direct signature equality between the
            // delegate's Invoke method and the target method. This does not
            // take covariance into account in addition to other more
            // restrictive MethodSignature checks.
            private readonly Dictionary<MethodSignature, List<MethodDesc>> _delegateTargets;
            private readonly Dictionary<TypeDesc, List<TypeDesc>> _implementers;

            public GdvEntityFinder(CompilerTypeSystemContext context, IEnumerable<ModuleDesc> modules)
            {
                Dictionary<MethodSignature, HashSet<MethodDesc>> delegateTargets = new();
                Dictionary<TypeDesc, HashSet<TypeDesc>> implementers = new();
                foreach (ModuleDesc module in modules)
                {
                    foreach (MetadataType type in module.GetAllTypes())
                    {
                        if (type.IsGenericDefinition)
                            continue;

                        foreach (MethodDesc method in type.GetMethods())
                        {
                            try
                            {
                                if (!method.Signature.IsStatic && !method.IsAbstract && !method.IsGenericMethodDefinition)
                                {
                                    context.EnsureLoadableMethod(method);

                                    if (!delegateTargets.TryGetValue(method.Signature, out HashSet<MethodDesc> set))
                                        delegateTargets.Add(method.Signature, set = new HashSet<MethodDesc>());

                                    set.Add(method);
                                }
                            }
                            catch (TypeSystemException)
                            {
                            }
                        }

                        try
                        {
                            if (!type.IsAbstract && !type.IsInterface)
                            {
                                context.EnsureLoadableType(type);

                                void AddImplemented(TypeDesc implemented)
                                {
                                    if (!implementers.TryGetValue(implemented, out HashSet<TypeDesc> set))
                                        implementers.Add(implemented, set = new HashSet<TypeDesc>());

                                    set.Add(type);
                                }

                                TypeDesc implemented = type;
                                do
                                {
                                    AddImplemented(implemented);
                                    implemented = implemented.BaseType;
                                } while (implemented != null);

                                foreach (TypeDesc iface in type.RuntimeInterfaces)
                                {
                                    AddImplemented(iface);
                                }
                            }
                        }
                        catch (TypeSystemException)
                        {
                        }
                    }
                }

                _delegateTargets = delegateTargets.ToDictionary(kvp => kvp.Key, kvp =>
                {
                    List<MethodDesc> list = new(kvp.Value);
                    list.MergeSort(TypeSystemComparer.Instance.Compare);
                    return list;
                });

                _implementers = implementers.ToDictionary(kvp => kvp.Key, kvp =>
                {
                    List<TypeDesc> list = new(kvp.Value);
                    list.MergeSort(TypeSystemComparer.Instance.Compare);
                    return list;
                });
            }

            public IList<MethodDesc> GetCompatibleWithDelegateInvoke(MethodSignature delegateInvokeSignature)
            {
                return _delegateTargets.TryGetValue(delegateInvokeSignature, out List<MethodDesc> methods) ? methods : Array.Empty<MethodDesc>();
            }

            public IList<TypeDesc> GetImplementers(TypeDesc type)
            {
                return _implementers.TryGetValue(type, out List<TypeDesc> types) ? types : Array.Empty<TypeDesc>();
            }
        }
    }
}
