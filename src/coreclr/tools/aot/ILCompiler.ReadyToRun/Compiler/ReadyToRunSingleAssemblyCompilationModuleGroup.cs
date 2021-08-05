// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.ReadyToRunConstants;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public class ReadyToRunSingleAssemblyCompilationModuleGroup : ReadyToRunCompilationModuleGroupBase
    {
        private ProfileDataManager _profileGuidedCompileRestriction;
        private bool _profileGuidedCompileRestrictionSet;

        public ReadyToRunSingleAssemblyCompilationModuleGroup(
            CompilerTypeSystemContext context,
            bool isCompositeBuildMode,
            bool isInputBubble,
            IEnumerable<EcmaModule> compilationModuleSet,
            IEnumerable<ModuleDesc> versionBubbleModuleSet,
            bool compileGenericDependenciesFromVersionBubbleModuleSet) :
                base(context,
                     isCompositeBuildMode,
                     isInputBubble,
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
                if (!_profileGuidedCompileRestriction.IsMethodInProfileData(method))
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
