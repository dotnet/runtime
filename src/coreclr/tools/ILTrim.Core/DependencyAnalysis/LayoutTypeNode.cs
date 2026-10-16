// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents the instance fields required by a type with sequential or explicit layout.
    /// </summary>
    public sealed class LayoutTypeNode : DependencyNodeCore<NodeFactory>
    {
        private readonly EcmaType _type;

        public LayoutTypeNode(EcmaType type)
        {
            _type = type;
        }

        public static bool IsLayoutType(EcmaType type)
        {
            TypeDefinition typeDef = type.Module.MetadataReader.GetTypeDefinition(type.Handle);
            return typeDef.Attributes.HasFlag(TypeAttributes.SequentialLayout)
                || typeDef.Attributes.HasFlag(TypeAttributes.ExplicitLayout);
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            MetadataReader reader = _type.Module.MetadataReader;
            TypeDefinition typeDef = reader.GetTypeDefinition(_type.Handle);

            if (factory.IsModuleTrimmed(_type.Module) && IsLayoutType(_type))
            {
                foreach (FieldDefinitionHandle fieldHandle in typeDef.GetFields())
                {
                    FieldDefinition fieldDef = reader.GetFieldDefinition(fieldHandle);
                    if (!fieldDef.Attributes.HasFlag(FieldAttributes.Static))
                    {
                        yield return new(
                            factory.FieldDefinition(_type.Module, fieldHandle),
                            "Instance field of a type with sequential or explicit layout");
                    }
                }
            }

            if (_type.BaseType?.GetTypeDefinition() is EcmaType baseType)
            {
                yield return new(factory.LayoutType(baseType), "Base type");
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
