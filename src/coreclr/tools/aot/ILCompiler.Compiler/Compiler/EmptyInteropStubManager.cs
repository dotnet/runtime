// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using ILCompiler.DependencyAnalysis;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using Internal.IL;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler
{
    /// <summary>
    /// This class is responsible for managing stub methods for interop
    /// </summary>
    public sealed class EmptyInteropStubManager : InteropStubManager
    {
        public override PInvokeILProvider CreatePInvokeILProvider()
        {
            return null;
        }

        public override void AddDependenciesDueToMethodCodePresence(IDependencySink<NodeFactory> dependencies, NodeFactory factory, MethodDesc method)
        {
        }

        public override void AddInterestingInteropConstructedTypeDependencies(DependencySink<NodeFactory> dependencies, NodeFactory factory, TypeDesc type)
        {
        }

        public override void AddMarshalAPIsGenericDependencies(IDependencySink<NodeFactory> dependencies, NodeFactory factory, MethodDesc method)
        {
        }
    }
}
