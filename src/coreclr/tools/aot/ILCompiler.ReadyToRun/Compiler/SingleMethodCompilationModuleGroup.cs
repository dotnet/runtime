// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Internal.ReadyToRunConstants;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// A compilation group that only contains a single method. Useful for development purposes when investigating
    /// code generation issues.
    /// </summary>
    public class SingleMethodCompilationModuleGroup : ReadyToRunCompilationModuleGroupBase
    {
        private MethodDesc _method;

        public SingleMethodCompilationModuleGroup(
            CompilerTypeSystemContext context,
            bool isCompositeBuildMode,
            bool isInputBubble,
            IEnumerable<EcmaModule> compilationModuleSet,
            IEnumerable<ModuleDesc> versionBubbleModuleSet,
            bool compileGenericDependenciesFromVersionBubbleModuleSet,
            MethodDesc method) :
                base(context,
                     isCompositeBuildMode,
                     isInputBubble,
                     compilationModuleSet,
                     versionBubbleModuleSet,
                     compileGenericDependenciesFromVersionBubbleModuleSet)
        {
            _method = method;
        }

        public override bool ContainsMethodBody(MethodDesc method, bool unboxingStub)
        {
            return method == _method;
        }

        public override void ApplyProfilerGuidedCompilationRestriction(ProfileDataManager profileGuidedCompileRestriction)
        {
            // Profiler guided restrictions are ignored for single method compilation
            return;
        }

        public override ReadyToRunFlags GetReadyToRunFlags()
        {
            // Partial by definition.
            return ReadyToRunFlags.READYTORUN_FLAG_Partial;
        }
    }
}
