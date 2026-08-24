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
        public static IEETypeNode GetEffectiveTrimTargetType(NodeFactory factory, TypeDesc trimmingTargetType, bool conditionConstructed)
        {
            bool unwrapped = false;

            // Many parameterized types can be created at runtime using the type loader.
            // Pointers and byrefs can be made out of thin air since there's no code for them.
            // Arrays have some rules so we may still be able to condition on the array.
            while (trimmingTargetType.IsParameterizedType)
            {
                if (GenericTypesTemplateMap.IsArrayTypeEligibleForTemplate(trimmingTargetType)
                    // The template shouldn't be __Canon[]
                    && !trimmingTargetType.ConvertToCanonForm(CanonicalFormKind.Specific).GetParameterType().IsCanonicalDefinitionType(CanonicalFormKind.Any))
                {
                    // We can condition on this type since we'd either need this type to be
                    // present in the compilation, or template for this type to be present.
                    //
                    // We specifically exclude the `__Canon[]` template because conditioning
                    // on the __Canon[] template would be strictly worse than conditioning on
                    // the element type in a normal app.
                    break;
                }

                // The type can be just MakeArrayType/MakePointerType at runtime, so drill into
                // the element type, we need to condition on that - can't condition on the
                // constructed type.
                trimmingTargetType = trimmingTargetType.GetParameterType();

                unwrapped = true;
            }

            if (!conditionConstructed)
                return factory.NecessaryTypeSymbol(trimmingTargetType);

            // If we're conditioning on the type being constructed but we unwrapped element type,
            // the condition is now only a metadata type symbol. This is because e.g.
            // `Array.CreateInstance(typeof(Element))` needs only the typeof-level type load
            // of the element type to create a constructed array type.
            return unwrapped
                ? factory.MetadataTypeSymbol(trimmingTargetType)
                : factory.MaximallyConstructableType(trimmingTargetType);
        }

        public static void AddTypeLoaderDependencies(
            List<DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry> dependencies,
            NodeFactory factory,
            IEETypeNode dependencyType,
            string reason)
        {
            TypeDesc type = dependencyType.Type;
            TypeDesc canonType = type.ConvertToCanonForm(CanonicalFormKind.Specific);
            if (canonType != type && GenericTypesTemplateMap.IsEligibleToHaveATemplate(canonType))
            {
                dependencies.Add(new DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry(
                    dependencyType,
                    factory.NativeLayout.TemplateTypeLayout(canonType),
                    reason));
            }
       }
    }
}
