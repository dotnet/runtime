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
        private ProfileRestrictionMode _profileRestrictionMode;
        private bool _profileGuidedCompileRestrictionSet;

        public ReadyToRunSingleAssemblyCompilationModuleGroup(
            ReadyToRunCompilationModuleGroupConfig config) :
                base(config)
        {
        }

        public sealed override bool ContainsMethodBody(MethodDesc method, bool unboxingStub)
        {
            if (!_profileGuidedCompileRestrictionSet)
                throw new InternalCompilerErrorException("Called ContainsMethodBody without setting profile guided restriction");

            if (_profileGuidedCompileRestriction != null)
            {
                bool inProfile = _profileGuidedCompileRestriction.IsMethodInInputProfileData(method);
                // ProfileComplement keeps the methods the profile did NOT select; ProfileOnly keeps those it did.
                bool keep = (_profileRestrictionMode == ProfileRestrictionMode.ProfileComplement) ? !inProfile : inProfile;
                if (!keep)
                    return false;
            }

            if (method is ArrayMethod)
            {
                // TODO-PERF: for now, we never emit native code for array methods as Crossgen ignores
                // them too. At some point we might be able to "exceed Crossgen CQ" by adding this support.
                return false;
            }

            return (ContainsType(method.OwningType) && VersionsWithMethodBody(method)) || CompileVersionBubbleGenericsIntoCurrentModule(method) || this.CrossModuleCompileable(method);
        }

        public sealed override void ApplyProfileGuidedOptimizationData(ProfileDataManager profileGuidedCompileRestriction, ProfileRestrictionMode mode)
        {
            if (_profileGuidedCompileRestrictionSet)
                throw new InternalCompilerErrorException("Called ApplyProfileGuidedOptimizationData twice.");

            _profileGuidedCompileRestrictionSet = true;
            _profileRestrictionMode = mode;
            if (mode != ProfileRestrictionMode.None)
                _profileGuidedCompileRestriction = profileGuidedCompileRestriction;

            base.ApplyProfileGuidedOptimizationData(profileGuidedCompileRestriction, mode);
        }

        public override ReadyToRunFlags GetReadyToRunFlags()
        {
            Debug.Assert(_profileGuidedCompileRestrictionSet);

            ReadyToRunFlags flags = base.GetReadyToRunFlags();
            if (_profileGuidedCompileRestriction != null)
                flags |= ReadyToRunFlags.READYTORUN_FLAG_Partial;

            return flags;
        }
    }
}
