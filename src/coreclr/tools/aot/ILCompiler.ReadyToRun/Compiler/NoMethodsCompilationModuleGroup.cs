// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Internal.ReadyToRunConstants;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// A compilation group that only contains no methods. Used for creating an R2R image without
    /// any method compiled into it. Needed for handling inputbubble scenarios where a dependent
    /// assembly should not be R2R'd.
    /// </summary>
    public class NoMethodsCompilationModuleGroup : ReadyToRunCompilationModuleGroupBase
    {
        public NoMethodsCompilationModuleGroup(
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

        public override bool ContainsMethodBody(MethodDesc method, bool unboxingStub)
        {
            return false;
        }

        public override void ApplyProfilerGuidedCompilationRestriction(ProfileDataManager profileGuidedCompileRestriction)
        {
            return;
        }

        public override ReadyToRunFlags GetReadyToRunFlags()
        {
            // Partial by definition.
            return ReadyToRunFlags.READYTORUN_FLAG_Partial;
        }
    }
}
