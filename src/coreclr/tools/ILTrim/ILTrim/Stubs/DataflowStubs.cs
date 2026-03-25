// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Stub types required by the shared ILCompiler.Compiler dataflow analysis code.
// ILTrim source-includes dataflow files from ILCompiler.Compiler. Those files
// reference NativeAOT-specific types that don't exist in the ILTrim world.
// We provide minimal stubs so the shared code compiles and functions correctly
// for IL trimming scenarios (no generic sharing, no NativeAOT metadata, etc.).

using System;
using System.Collections.Generic;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;

#nullable enable

namespace ILCompiler
{
    public class CompilerTypeSystemContext : MetadataTypeSystemContext
    {
        public override bool SupportsCanon => false;
        public override bool SupportsUniversalCanon => false;

        public MethodDesc? GetAsyncVariant(MethodDesc method) => null;
        public MethodDesc? GetAsyncVariantMethod(MethodDesc method) => null;
        public MethodDesc? GetTargetOfAsyncVariantMethod(MethodDesc method) => null;

        internal DefType GetClosestDefType(TypeDesc type)
        {
            if (type is DefType defType)
                return defType;
            return GetWellKnownType(WellKnownType.Array);
        }

        // Logger.cs uses these to resolve module file names for diagnostics.
        public Dictionary<string, string> ReferenceFilePaths { get; } = new();
        public Dictionary<string, string> InputFilePaths { get; } = new();
    }

    public class MetadataManager
    {
        public virtual bool IsReflectionBlocked(TypeDesc type) => false;
        public virtual bool IsReflectionBlocked(MethodDesc method) => false;
        public virtual bool IsReflectionBlocked(FieldDesc field) => false;
        public virtual bool CanGenerateMetadata(MetadataType type) => true;
    }

    public class UsageBasedMetadataManager : MetadataManager
    {
        public ILLink.Shared.TrimAnalysis.FlowAnnotations? FlowAnnotations { get; set; }
        public Logger? Logger { get; set; }
    }

    // AsyncMethodVariant exists only as a type for `is` checks.
    // CompilerGeneratedState checks `method.GetTypicalMethodDefinition() is AsyncMethodVariant`.
    // Since ILTrim never creates async variants, the check always returns false.
    public abstract class AsyncMethodVariant : MethodDesc
    {
        public MethodDesc Target => throw new NotImplementedException();
    }

    public static class AsyncMethodVariantExtensions
    {
        public static bool IsAsyncVariant(this MethodDesc method) => false;
    }

    public static class MethodExtensions
    {
        public static bool ReturnsTaskOrValueTask(this MethodSignature method)
        {
            TypeDesc ret = method.ReturnType;
            if (ret is MetadataType md
                && md.Module == md.Context.SystemModule
                && md.Namespace.SequenceEqual("System.Threading.Tasks"u8))
            {
                ReadOnlySpan<byte> name = md.Name;
                if (name.SequenceEqual("Task"u8) || name.SequenceEqual("Task`1"u8)
                    || name.SequenceEqual("ValueTask"u8) || name.SequenceEqual("ValueTask`1"u8))
                    return true;
            }
            return false;
        }
    }

    // Stub for RootingHelpers — the shared dataflow code calls these to record
    // that a type/method/field was accessed via reflection.
    public static class RootingHelpers
    {
        public static bool TryGetDependenciesForReflectedType(
            ref DependencyList dependencies, NodeFactory factory, TypeDesc type, string reason)
        {
            dependencies ??= new DependencyList();
            dependencies.Add(factory.ReflectedType(type), reason);
            return true;
        }

        public static bool TryGetDependenciesForReflectedMethod(
            ref DependencyList dependencies, NodeFactory factory, MethodDesc method, string reason)
        {
            dependencies ??= new DependencyList();
            dependencies.Add(factory.ReflectedMethod(method), reason);
            return true;
        }

        public static bool TryGetDependenciesForReflectedField(
            ref DependencyList dependencies, NodeFactory factory, FieldDesc field, string reason)
        {
            dependencies ??= new DependencyList();
            dependencies.Add(factory.ReflectedField(field), reason);
            return true;
        }
    }
}

namespace ILCompiler.DependencyAnalysis
{
    // Marker node types for the dependency graph.
    // DependencyNode seals Equals/GetHashCode, so we use reference equality via NodeCache.

    public class ReflectedTypeNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;
        public ReflectedTypeNode(TypeDesc type) => _type = type;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null!;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => $"ReflectedType({_type})";
    }

    public class ReflectedMethodNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _method;
        public ReflectedMethodNode(MethodDesc method) => _method = method;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null!;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => $"ReflectedMethod({_method})";
    }

    public class ReflectedFieldNode : DependencyNodeCore<NodeFactory>
    {
        private readonly FieldDesc _field;
        public ReflectedFieldNode(FieldDesc field) => _field = field;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null!;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => $"ReflectedField({_field})";
    }

    public class ReflectedDelegateNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc? _type;
        public ReflectedDelegateNode(TypeDesc? type) => _type = type;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null!;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => $"ReflectedDelegate({_type})";
    }

    public class StructMarshallingDataNode : DependencyNodeCore<NodeFactory>
    {
        public StructMarshallingDataNode(DefType type) { }
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null!;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => "StructMarshallingData";
    }

    public class DelegateMarshallingDataNode : DependencyNodeCore<NodeFactory>
    {
        public DelegateMarshallingDataNode(DefType type) { }
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null!;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => "DelegateMarshallingData";
    }

    public class DataflowAnalyzedTypeDefinitionNode : DependencyNodeCore<NodeFactory>
    {
        public DataflowAnalyzedTypeDefinitionNode(TypeDesc type) { }
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null!;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => "DataflowAnalyzedType";
    }

    public class ObjectGetTypeCalledNode : DependencyNodeCore<NodeFactory>
    {
        public ObjectGetTypeCalledNode(MetadataType type) { }
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null!;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => "ObjectGetTypeCalled";
    }

    public class ExternalTypeMapRequestNode : DependencyNodeCore<NodeFactory>
    {
        public ExternalTypeMapRequestNode(TypeDesc typeMapGroup) { }
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null!;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => "ExternalTypeMapRequest";
    }

    public class ProxyTypeMapRequestNode : DependencyNodeCore<NodeFactory>
    {
        public ProxyTypeMapRequestNode(TypeDesc typeMapGroup) { }
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null!;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => "ProxyTypeMapRequest";
    }
}
