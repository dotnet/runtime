// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.IL;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;

namespace ILCompiler
{
    /// <summary>
    /// Represents an interop stub manager whose list of stubs has been determined ahead of time.
    /// </summary>
    public class AnalysisBasedInteropStubManager : CompilerGeneratedInteropStubManager
    {
        private readonly IEnumerable<DefType> _typesWithStructMarshalling;
        private readonly IEnumerable<DefType> _typesWithDelegateMarshalling;

        public AnalysisBasedInteropStubManager(InteropStateManager interopStateManager, PInvokeILEmitterConfiguration pInvokeILEmitterConfiguration, IEnumerable<DefType> typesWithStructMarshalling, IEnumerable<DefType> typesWithDelegateMarshalling)
            : base(interopStateManager, pInvokeILEmitterConfiguration)
        {
            _typesWithStructMarshalling = typesWithStructMarshalling;
            _typesWithDelegateMarshalling = typesWithDelegateMarshalling;
        }

        public override void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            foreach (DefType type in _typesWithStructMarshalling)
            {
                rootProvider.RootStructMarshallingData(type, "Analysis based interop root");
            }

            foreach (DefType type in _typesWithDelegateMarshalling)
            {
                rootProvider.RootDelegateMarshallingData(type, "Analysis based interop root");
            }
        }

        public override void AddDependenciesDueToPInvoke(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
        }

        public override void AddInterestingInteropConstructedTypeDependencies(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
        }

        public override void AddMarshalAPIsGenericDependencies(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
        }
    }
}
