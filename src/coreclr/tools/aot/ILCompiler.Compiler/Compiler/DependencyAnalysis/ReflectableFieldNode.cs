// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a field that is gettable/settable from reflection.
    /// </summary>
    public class ReflectableFieldNode : DependencyNodeCore<NodeFactory>
    {
        private readonly FieldDesc _field;

        public ReflectableFieldNode(FieldDesc field)
        {
            Debug.Assert(!field.OwningType.IsCanonicalSubtype(CanonicalFormKind.Any)
                || field.OwningType.ConvertToCanonForm(CanonicalFormKind.Specific) == field.OwningType);
            _field = field;
        }

        public FieldDesc Field => _field;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            Debug.Assert(!factory.MetadataManager.IsReflectionBlocked(_field.GetTypicalFieldDefinition()));

            DependencyList dependencies = new DependencyList();
            factory.MetadataManager.GetDependenciesDueToReflectability(ref dependencies, factory, _field);

            // No runtime artifacts needed if this is a generic definition or literal field
            if (_field.OwningType.IsGenericDefinition || _field.IsLiteral)
            {
                return dependencies;
            }

            FieldDesc typicalField = _field.GetTypicalFieldDefinition();
            if (typicalField != _field)
            {
                // Ensure we consistently apply reflectability to all fields sharing the same definition.
                // Bases for different instantiations of the field have a conditional dependency on the definition node that
                // brings a ReflectableField of the instantiated field if it's necessary for it to be reflectable.
                dependencies.Add(factory.ReflectableField(typicalField), "Definition of the reflectable field");
            }

            // Runtime reflection stack needs to see the type handle of the owning type
            dependencies.Add(factory.MaximallyConstructableType(_field.OwningType), "Instance base of a reflectable field");

            // Root the static base of the type
            if (_field.IsStatic && !_field.OwningType.IsCanonicalSubtype(CanonicalFormKind.Any))
            {
                // Infrastructure around static constructors is stashed in the NonGC static base
                bool needsNonGcStaticBase = factory.PreinitializationManager.HasLazyStaticConstructor(Field.OwningType);

                if (_field.HasRva)
                {
                    // No reflection access right now
                }
                else if (_field.IsThreadStatic)
                {
                    dependencies.Add(factory.TypeThreadStaticIndex((MetadataType)_field.OwningType), "Threadstatic base of a reflectable field");
                }
                else if (_field.HasGCStaticBase)
                {
                    dependencies.Add(factory.TypeGCStaticsSymbol((MetadataType)_field.OwningType), "GC static base of a reflectable field");
                }
                else
                {
                    dependencies.Add(factory.TypeNonGCStaticsSymbol((MetadataType)_field.OwningType), "NonGC static base of a reflectable field");
                    needsNonGcStaticBase = false;
                }

                if (needsNonGcStaticBase)
                {
                    dependencies.Add(factory.TypeNonGCStaticsSymbol((MetadataType)_field.OwningType), "CCtor context");
                }
            }

            if (!_field.OwningType.IsCanonicalSubtype(CanonicalFormKind.Any))
            {
                // Runtime reflection stack needs to obtain the type handle of the field
                // (but there's no type handles for function pointers)
                TypeDesc fieldTypeToCheck = _field.FieldType;
                while (fieldTypeToCheck.IsParameterizedType)
                    fieldTypeToCheck = ((ParameterizedType)fieldTypeToCheck).ParameterType;

                if (!fieldTypeToCheck.IsFunctionPointer)
                    dependencies.Add(factory.MaximallyConstructableType(_field.FieldType.NormalizeInstantiation()), "Type of the field");
            }

            return dependencies;
        }
        protected override string GetName(NodeFactory factory)
        {
            return "Reflectable field: " + _field.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
