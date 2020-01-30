// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Internal.TypeSystem;
using Internal.ReadyToRunConstants;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public class ReadyToRunSingleAssemblyCompilationModuleGroup : ReadyToRunCompilationModuleGroupBase
    {
        private ProfileDataManager _profileGuidedCompileRestriction;
        private bool _profileGuidedCompileRestrictionSet;

        public ReadyToRunSingleAssemblyCompilationModuleGroup(
            TypeSystemContext context, 
            IEnumerable<ModuleDesc> compilationModuleSet,
            IEnumerable<ModuleDesc> versionBubbleModuleSet,
            bool compileGenericDependenciesFromVersionBubbleModuleSet) :
                base(context,
                     compilationModuleSet,
                     versionBubbleModuleSet,
                     compileGenericDependenciesFromVersionBubbleModuleSet)
        {
        }

        public sealed override bool ContainsMethodBody(MethodDesc method, bool unboxingStub)
        {
            if (!_profileGuidedCompileRestrictionSet)
                throw new InternalCompilerErrorException("Called ContainsMethodBody without setting profile guided restriction");

            if (_profileGuidedCompileRestriction != null)
            {
                bool found = false;

                if (!method.HasInstantiation && !method.OwningType.HasInstantiation)
                {
                    // Check only the defining module for non-generics
                    MetadataType mdType = method.OwningType as MetadataType;
                    if (mdType != null)
                    {
                        if (_profileGuidedCompileRestriction.GetDataForModuleDesc(mdType.Module).GetMethodProfileData(method) != null)
                        {
                            found = true;
                        }
                    }
                }
                else
                {
                    // For generics look in the profile data of all modules being compiled
                    foreach (ModuleDesc module in _compilationModuleSet)
                    {
                        if (_profileGuidedCompileRestriction.GetDataForModuleDesc(module).GetMethodProfileData(method) != null)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (found == false)
                    return false;
            }

            if (method is ArrayMethod)
            {
                // TODO-PERF: for now, we never emit native code for array methods as Crossgen ignores
                // them too. At some point we might be able to "exceed Crossgen CQ" by adding this support.
                return false;
            }

            return (ContainsType(method.OwningType) && VersionsWithMethodBody(method)) || CompileVersionBubbleGenericsIntoCurrentModule(method);
        }

        public sealed override void ApplyProfilerGuidedCompilationRestriction(ProfileDataManager profileGuidedCompileRestriction)
        {
            if (_profileGuidedCompileRestrictionSet)
                throw new InternalCompilerErrorException("Called ApplyProfilerGuidedCompilationRestriction twice.");

            _profileGuidedCompileRestrictionSet = true;
            _profileGuidedCompileRestriction = profileGuidedCompileRestriction;
        }

        public override ReadyToRunFlags GetReadyToRunFlags()
        {
            Debug.Assert(_profileGuidedCompileRestrictionSet);

            ReadyToRunFlags flags = 0;
            if (_profileGuidedCompileRestriction != null)
                flags |= ReadyToRunFlags.READYTORUN_FLAG_Partial;

            return flags;
        }
    }
}
