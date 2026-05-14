// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

namespace ILCompiler
{
    internal static class RuntimeConstructableTypeDependencies
    {
        public static TypeDesc GetEffectiveTrimTargetType(TypeDesc trimmingTargetType)
        {
            while (trimmingTargetType is ParameterizedType parameterized && !trimmingTargetType.IsArray)
                trimmingTargetType = parameterized.ParameterType;
            return trimmingTargetType;
        }

        public static IEnumerable<DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry> GetNecessaryTypeDependencies(NodeFactory context, TypeDesc type, string reason)
        {
            TypeDesc effectiveType = GetEffectiveTrimTargetType(type);
            foreach (DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry dependency in GetDependencies(context, context.NecessaryTypeSymbol(effectiveType), effectiveType, reason, useNecessaryTypeSymbol: true))
                yield return dependency;
        }

        public static IEnumerable<DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry> GetMaximallyConstructableTypeDependencies(NodeFactory context, TypeDesc type, string reason)
        {
            // Associated source types are conditioned on the exact type that can exist in the program, unlike
            // external type map trim targets where non-array parameterized wrappers are stripped.
            foreach (DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry dependency in GetDependencies(context, context.MaximallyConstructableType(type), type, reason, useNecessaryTypeSymbol: false))
                yield return dependency;
        }

        private static IEnumerable<DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry> GetDependencies(
            NodeFactory context,
            object conditionalDependencyNode,
            TypeDesc type,
            string reason,
            bool useNecessaryTypeSymbol)
        {
            TypeDesc canonType = type.ConvertToCanonForm(CanonicalFormKind.Specific);
            if (canonType != type && GenericTypesTemplateMap.IsEligibleToHaveATemplate(canonType))
            {
                yield return new DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry(
                    conditionalDependencyNode,
                    context.NativeLayout.TemplateTypeLayout(canonType),
                    reason);
            }
            else if (type is ArrayType arrayType)
            {
                TypeDesc effectiveElementType = GetEffectiveTrimTargetType(arrayType.ElementType);
                TypeDesc canonElementType = effectiveElementType.ConvertToCanonForm(CanonicalFormKind.Specific);
                if (canonElementType != effectiveElementType && GenericTypesTemplateMap.IsEligibleToHaveATemplate(canonElementType))
                {
                    yield return new DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry(
                        conditionalDependencyNode,
                        context.NativeLayout.TemplateTypeLayout(canonElementType),
                        reason);
                }

                if (!GenericTypesTemplateMap.IsArrayTypeEligibleForTemplate(arrayType))
                {
                    yield return new DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry(
                        conditionalDependencyNode,
                        useNecessaryTypeSymbol ? context.NecessaryTypeSymbol(effectiveElementType) : context.MaximallyConstructableType(effectiveElementType),
                        reason);
                }
            }
        }
    }
}
