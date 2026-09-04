// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using ILCompiler.DependencyAnalysisFramework;

#nullable enable

namespace ILCompiler
{
    // Stub for RootingHelpers — the shared dataflow code calls these to record
    // that a type/method/field was accessed via reflection.
    public static class RootingHelpers
    {
        public static bool TryAddDependenciesForReflectedType(
            IDependencySink<NodeFactory> dependencies,
            NodeFactory factory,
            TypeDesc type,
            string reason)
        {
            dependencies.Add(factory.ReflectedType(type), reason);
            return true;
        }

        public static bool TryAddDependenciesForReflectedType(
            DependencySink<NodeFactory> dependencies,
            NodeFactory factory,
            TypeDesc type,
            string reason,
            DependencyNodeCore<NodeFactory> otherReasonNode)
        {
            dependencies.AddConditional(factory.ReflectedType(type), otherReasonNode, reason);
            return true;
        }

        public static bool TryAddDependenciesForReflectedMethod(
            IDependencySink<NodeFactory> dependencies,
            NodeFactory factory,
            MethodDesc method,
            string reason)
        {
            dependencies.Add(factory.ReflectedMethod(method), reason);
            return true;
        }

        public static bool TryAddDependenciesForReflectedMethod(
            DependencySink<NodeFactory> dependencies,
            NodeFactory factory,
            MethodDesc method,
            string reason,
            DependencyNodeCore<NodeFactory> otherReasonNode)
        {
            dependencies.AddConditional(factory.ReflectedMethod(method), otherReasonNode, reason);
            return true;
        }

        public static bool TryAddDependenciesForReflectedField(
            IDependencySink<NodeFactory> dependencies, NodeFactory factory, FieldDesc field, string reason)
        {
            dependencies.Add(factory.ReflectedField(field), reason);
            return true;
        }
    }
}
