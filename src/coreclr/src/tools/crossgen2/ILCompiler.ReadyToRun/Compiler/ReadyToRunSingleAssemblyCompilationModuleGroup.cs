// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Internal.ReadyToRunConstants;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public class ReadyToRunSingleAssemblyCompilationModuleGroup : ReadyToRunCompilationModuleGroupBase
    {
        private ProfileDataManager _profileGuidedCompileInfo;
        private bool _profileGuidedInfoSet;
        private ReadyToRunCompilationPolicy _compilationPolicy;

        public ReadyToRunSingleAssemblyCompilationModuleGroup(
            TypeSystemContext context,
            bool isCompositeBuildMode,
            bool isInputBubble,
            IEnumerable<EcmaModule> compilationModuleSet,
            IEnumerable<ModuleDesc> versionBubbleModuleSet,
            bool compileGenericDependenciesFromVersionBubbleModuleSet,
            ReadyToRunCompilationPolicy compilationPolicy) :
                base(context,
                     isCompositeBuildMode,
                     isInputBubble,
                     compilationModuleSet,
                     versionBubbleModuleSet,
                     compileGenericDependenciesFromVersionBubbleModuleSet)
        {
            _compilationPolicy = compilationPolicy;
        }

        public sealed override bool ContainsMethodBody(MethodDesc method, bool unboxingStub)
        {
            if (!_profileGuidedInfoSet)
                throw new InternalCompilerErrorException("Called ContainsMethodBody without setting profile guided restriction");

            var policy = _compilationPolicy.For(method);

            if ((policy.Flags & ReadyToRunCompilationPolicyFlags.NoMethods) != 0)
            {
                return false;
            }

            if (method is ArrayMethod)
            {
                // TODO-PERF: for now, we never emit native code for array methods as Crossgen ignores
                // them too. At some point we might be able to "exceed Crossgen CQ" by adding this support.
                return false;
            }

            if (!((ContainsType(method.OwningType) && VersionsWithMethodBody(method)) || CompileVersionBubbleGenericsIntoCurrentModule(method)))
            {
                // If the method can't be compiled in this compilation, skip
                return false;
            }

            bool methodInProfileData = false;

            if ((_profileGuidedCompileInfo != null) && !((policy.Flags & ReadyToRunCompilationPolicyFlags.IgnoreProfileData) != 0))
            {
                methodInProfileData = _profileGuidedCompileInfo.IsMethodInProfileData(method);
            }

            if ((policy.Flags & ReadyToRunCompilationPolicyFlags.OnlyProfileSpecifiedMethods) != 0)
            {
                if (!methodInProfileData)
                    return false;
            }

            MethodDesc typicalMethod = method.GetTypicalMethodDefinition();

            if (!methodInProfileData && (method != typicalMethod))
            {
                // If profile data is not present to indicate a generic is interesting, check to see if its allowed via the policy rules
                if ((policy.Flags & ReadyToRunCompilationPolicyFlags.AllowAllGenericInstantiations) != 0)
                {
                    // The generic instantiation is allowed. No complex policy required
                }
                else
                {
                    bool outOfPolicyInstantiationFound = false;
                    ModuleDesc localModule = ((MetadataType)typicalMethod.OwningType).Module;
                    bool allowPrimitives = ((policy.Flags & ReadyToRunCompilationPolicyFlags.AllowPrimitiveGenericInstantiations) != 0);
                    bool allowLocal = ((policy.Flags & ReadyToRunCompilationPolicyFlags.AllowLocalGenericInstantiations) != 0);
                    bool allowCanon = ((policy.Flags & ReadyToRunCompilationPolicyFlags.AllowGenericCanonInstantiations) != 0);

                    foreach (var type in method.Instantiation)
                    {
                        if (!IsTypeInPolicy(type, localModule, allowPrimitives, allowCanon, allowLocal))
                        {
                            outOfPolicyInstantiationFound = true;
                            break;
                        }
                    }
                    if (!outOfPolicyInstantiationFound)
                    {
                        foreach (var type in method.OwningType.Instantiation)
                        {
                            if (!IsTypeInPolicy(type, localModule, allowPrimitives, allowCanon, allowLocal))
                            {
                                outOfPolicyInstantiationFound = true;
                                break;
                            }
                        }
                    }

                    if (outOfPolicyInstantiationFound)
                        return false; // Only allow instantiations defined within policy
                }
            }

            if (method.GetGenericDepth() > policy.MaxGenericDepth)
            {
                return false;
            }

            return true;

            static bool IsTypeInPolicy(TypeDesc type, ModuleDesc localModule, bool allowPrimitives, bool allowCanon, bool allowLocal)
            {
                if (allowPrimitives)
                {
                    if (type.IsPrimitive)
                        return true;
                }
                if (allowCanon)
                {
                    if (type.IsCanonicalDefinitionType(CanonicalFormKind.Any))
                        return true;
                }
                if (allowLocal)
                {
                    if (type is MetadataType metadataType)
                    {
                        if (metadataType.Module == localModule)
                        {
                            foreach (TypeDesc instantiationType in type.Instantiation)
                            {
                                if (!IsTypeInPolicy(instantiationType, localModule, allowPrimitives, allowCanon, allowLocal))
                                {
                                    return false;
                                }
                            }

                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public sealed override void ApplyProfilerGuidedInformation(ProfileDataManager profileGuidedInfo)
        {
            if (_profileGuidedInfoSet)
                throw new InternalCompilerErrorException("Called ApplyProfilerGuidedInformation twice.");

            _profileGuidedInfoSet = true;
            _profileGuidedCompileInfo = profileGuidedInfo;
        }

        public override ReadyToRunFlags GetReadyToRunFlags()
        {
            Debug.Assert(_profileGuidedInfoSet);

            ReadyToRunFlags flags = 0;
            if ((_compilationPolicy.Global.Flags & ReadyToRunCompilationPolicyFlags.OnlyProfileSpecifiedMethods) != 0)
                flags |= ReadyToRunFlags.READYTORUN_FLAG_Partial;

            return flags;
        }
    }
}
