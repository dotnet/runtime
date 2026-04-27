// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class ExternalTypeMapNode : SortableDependencyNode, IExternalTypeMapNode
    {
        private readonly IEnumerable<KeyValuePair<string, (TypeDesc targetType, TypeDesc trimmingTargetType)>> _mapEntries;

        public ExternalTypeMapNode(TypeDesc typeMapGroup, IEnumerable<KeyValuePair<string, (TypeDesc targetType, TypeDesc trimmingTargetType)>> mapEntries)
        {
            _mapEntries = mapEntries;
            TypeMapGroup = typeMapGroup;
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => true;

        public override bool StaticDependenciesAreComputed => true;

        public TypeDesc TypeMapGroup { get; }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context)
        {
            foreach (var entry in _mapEntries)
            {
                var (targetType, trimmingTargetType) = entry.Value;
                if (trimmingTargetType is not null)
                {
                    TypeDesc effectiveTrimTargetType = GetEffectiveTrimTargetType(trimmingTargetType);

                    yield return new CombinedDependencyListEntry(
                        context.MetadataTypeSymbol(targetType),
                        context.NecessaryTypeSymbol(effectiveTrimTargetType),
                        "Type in external type map is cast target");

                    // If the trimming target type has a canonical form, it could be created at runtime by the type loader.
                    // If there is a type loader template for it, create the generic type instantiation eagerly.
                    TypeDesc canonTrimmingType = effectiveTrimTargetType.ConvertToCanonForm(CanonicalFormKind.Specific);
                    if (canonTrimmingType != effectiveTrimTargetType && GenericTypesTemplateMap.IsEligibleToHaveATemplate(canonTrimmingType))
                    {
                        yield return new CombinedDependencyListEntry(
                            context.NecessaryTypeSymbol(effectiveTrimTargetType),
                            context.NativeLayout.TemplateTypeLayout(canonTrimmingType),
                            "External type map trim target that could be loaded at runtime");
                    }
                    else if (effectiveTrimTargetType is ArrayType arrayType)
                    {
                        // Some arrays don't have array templates (e.g. multidimensional arrays, arrays of pointers).
                        // If the element type is template-loadable, the runtime can still construct the array type.
                        TypeDesc effectiveElementType = GetEffectiveTrimTargetType(arrayType.ElementType);
                        TypeDesc canonElementType = effectiveElementType.ConvertToCanonForm(CanonicalFormKind.Specific);
                        if (canonElementType != effectiveElementType && GenericTypesTemplateMap.IsEligibleToHaveATemplate(canonElementType))
                        {
                            yield return new CombinedDependencyListEntry(
                                context.NecessaryTypeSymbol(effectiveTrimTargetType),
                                context.NativeLayout.TemplateTypeLayout(canonElementType),
                                "External type map array trim target with template-loadable element type");
                        }

                        // Array types that aren't eligible for templates (MdArrays, pointer/fnptr-element SzArrays)
                        // can be constructed at runtime from just the element type's MethodTable using hardcoded
                        // templates (typeof(object[,]) for MdArrays, typeof(char*[]) for pointer arrays).
                        // If the element type is reachable, consider the array type reachable as well.
                        if (!GenericTypesTemplateMap.IsArrayTypeEligibleForTemplate(arrayType))
                        {
                            yield return new CombinedDependencyListEntry(
                                context.NecessaryTypeSymbol(effectiveTrimTargetType),
                                context.NecessaryTypeSymbol(effectiveElementType),
                                "Array without template can be constructed at runtime from element type");
                        }
                    }
                }
            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            foreach (var entry in _mapEntries)
            {
                var (targetType, trimmingTargetType) = entry.Value;
                if (trimmingTargetType is null)
                {
                    yield return new DependencyListEntry(
                        context.MetadataTypeSymbol(targetType),
                        "External type map entry target type");
                }
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => $"External type map: {TypeMapGroup}";

        public override int ClassCode => -785190502;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            ExternalTypeMapNode otherEntry = (ExternalTypeMapNode)other;
            return comparer.Compare(TypeMapGroup, otherEntry.TypeMapGroup);
        }

        private IEnumerable<(string Name, IEETypeNode target)> GetMarkedEntries(NodeFactory factory)
        {
            foreach (var entry in _mapEntries)
            {
                var (targetType, trimmingTargetType) = entry.Value;

                if (trimmingTargetType is null
                    || factory.NecessaryTypeSymbol(GetEffectiveTrimTargetType(trimmingTargetType)).Marked)
                {
                    IEETypeNode targetNode = factory.MetadataTypeSymbol(targetType);
                    Debug.Assert(targetNode.Marked);
                    yield return (entry.Key, targetNode);
                }
            }
        }

        // Strip non-array parameterized wrappers (pointers, byrefs) to get the effective
        // trimming target type. Arrays are preserved so trim dependencies can be conditioned
        // on array existence rather than just element type reachability.
        private static TypeDesc GetEffectiveTrimTargetType(TypeDesc trimmingTargetType)
        {
            while (trimmingTargetType is ParameterizedType parameterized && !trimmingTargetType.IsArray)
                trimmingTargetType = parameterized.ParameterType;
            return trimmingTargetType;
        }

        public Vertex CreateTypeMap(NodeFactory factory, NativeWriter writer, Section section, INativeFormatTypeReferenceProvider externalReferences)
        {
            VertexHashtable typeMapHashTable = new();

            foreach ((string key, IEETypeNode valueNode) in GetMarkedEntries(factory))
            {
                Vertex keyVertex = writer.GetStringConstant(key);
                Vertex valueVertex = externalReferences.EncodeReferenceToType(writer, valueNode.Type);
                Vertex entry = writer.GetTuple(keyVertex, valueVertex);
                typeMapHashTable.Append((uint)TypeHashingAlgorithms.ComputeNameHashCode(key), section.Place(entry));
            }

            Vertex typeMapStateVertex = writer.GetUnsignedConstant(1); // Valid type map state
            Vertex typeMapGroupVertex = externalReferences.EncodeReferenceToType(writer, TypeMapGroup);
            Vertex tuple = writer.GetTuple(typeMapGroupVertex, typeMapStateVertex, typeMapHashTable);
            return section.Place(tuple);
        }

        public IExternalTypeMapNode ToAnalysisBasedNode(NodeFactory factory)
            => new AnalyzedExternalTypeMapNode(
                    TypeMapGroup,
                    GetMarkedEntries(factory)
                    .ToImmutableDictionary(p => p.Name, p => p.target.Type));
    }
}
