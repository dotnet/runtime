// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents the instance fields required by a type with layout.
    /// </summary>
    public sealed class LayoutTypeNode : DependencyNodeCore<NodeFactory>
    {
        private readonly EcmaType _type;

        public LayoutTypeNode(EcmaType type)
        {
            Debug.Assert(IsLayoutType(type));
            _type = type;
        }

        public static bool IsLayoutType(EcmaType type)
        {
            return type.IsSequentialLayout || type.IsExplicitLayout || type.IsExtendedLayout;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            MetadataReader reader = _type.Module.MetadataReader;
            TypeDefinition typeDef = reader.GetTypeDefinition(_type.Handle);

            if (factory.IsModuleTrimmed(_type.Module))
            {
                foreach (FieldDefinitionHandle fieldHandle in typeDef.GetFields())
                {
                    FieldDefinition fieldDef = reader.GetFieldDefinition(fieldHandle);
                    if (!fieldDef.Attributes.HasFlag(FieldAttributes.Static))
                    {
                        yield return new(
                            factory.FieldDefinition(_type.Module, fieldHandle),
                            "Instance field of a type with layout");
                    }
                }
            }

            for (TypeDesc baseType = _type.BaseType; baseType is not null; baseType = baseType.BaseType)
            {
                if (baseType.GetTypeDefinition() is EcmaType baseDefinition && IsLayoutType(baseDefinition))
                {
                    yield return new(factory.LayoutType(baseDefinition), "Layout base type");
                    break;
                }
            }
        }

        protected override string GetName(NodeFactory factory)
        {
            return $"{_type} layout";
        }

        public override bool HasConditionalStaticDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
