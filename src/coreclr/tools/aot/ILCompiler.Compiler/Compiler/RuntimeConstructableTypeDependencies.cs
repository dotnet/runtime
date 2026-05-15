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

        public static void GetNecessaryTypeDependencies(List<DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry> dependencies, NodeFactory context, TypeDesc type, string reason)
        {
            TypeDesc effectiveType = GetEffectiveTrimTargetType(type);
            AddDependencies(dependencies, context, context.NecessaryTypeSymbol(effectiveType), effectiveType, reason, useNecessaryTypeSymbol: true);
        }

        public static void GetMaximallyConstructableTypeDependencies(List<DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry> dependencies, NodeFactory context, TypeDesc type, string reason)
        {
            AddDependencies(dependencies, context, GetRuntimeConstructableTypeNode(context, type), type, reason, useNecessaryTypeSymbol: false);
        }

        public static object GetRuntimeConstructableTypeNode(NodeFactory context, TypeDesc type)
        {
            if (type is ArrayType arrayType)
                return context.MaximallyConstructableType(GetEffectiveTrimTargetType(arrayType.ElementType));

            return context.MaximallyConstructableType(type);
        }

        private static void AddDependencies(
            List<DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry> dependencies,
            NodeFactory context,
            object conditionalDependencyNode,
            TypeDesc type,
            string reason,
            bool useNecessaryTypeSymbol)
        {
            TypeDesc canonType = type.ConvertToCanonForm(CanonicalFormKind.Specific);
            if (canonType != type && GenericTypesTemplateMap.IsEligibleToHaveATemplate(canonType))
            {
                dependencies.Add(new DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry(
                    conditionalDependencyNode,
                    context.NativeLayout.TemplateTypeLayout(canonType),
                    reason));
            }
            else if (type is ArrayType arrayType)
            {
                TypeDesc effectiveElementType = GetEffectiveTrimTargetType(arrayType.ElementType);
                TypeDesc canonElementType = effectiveElementType.ConvertToCanonForm(CanonicalFormKind.Specific);
                if (canonElementType != effectiveElementType && GenericTypesTemplateMap.IsEligibleToHaveATemplate(canonElementType))
                {
                    dependencies.Add(new DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry(
                        conditionalDependencyNode,
                        context.NativeLayout.TemplateTypeLayout(canonElementType),
                        reason));
                }

                if (!GenericTypesTemplateMap.IsArrayTypeEligibleForTemplate(arrayType))
                {
                    dependencies.Add(new DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry(
                        conditionalDependencyNode,
                        useNecessaryTypeSymbol ? context.NecessaryTypeSymbol(effectiveElementType) : context.MaximallyConstructableType(effectiveElementType),
                        reason));
                }
            }
        }
    }
}
