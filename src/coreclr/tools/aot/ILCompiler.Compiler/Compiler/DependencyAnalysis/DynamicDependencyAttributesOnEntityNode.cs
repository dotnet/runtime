// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Logging;

using ILLink.Shared;

using static ILCompiler.Dataflow.DynamicallyAccessedMembersBinder;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Computes the list of dependencies from DynamicDependencyAttribute.
    /// https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.dynamicdependencyattribute
    /// </summary>
    public class DynamicDependencyAttributesOnEntityNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeSystemEntity _entity;

        public DynamicDependencyAttributesOnEntityNode(TypeSystemEntity entity)
        {
            Debug.Assert(entity is EcmaMethod || entity is EcmaField);
            _entity = entity;
        }

        public static void AddDependenciesDueToDynamicDependencyAttribute(ref DependencyList dependencies, NodeFactory factory, EcmaMethod method)
        {
            if (method.HasCustomAttribute("System.Diagnostics.CodeAnalysis", "DynamicDependencyAttribute"))
            {
                dependencies ??= new DependencyList();
                dependencies.Add(factory.DynamicDependencyAttributesOnEntity(method), "DynamicDependencyAttribute present");
            }
        }

        public static void AddDependenciesDueToDynamicDependencyAttribute(ref DependencyList dependencies, NodeFactory factory, EcmaField field)
        {
            if (field.HasCustomAttribute("System.Diagnostics.CodeAnalysis", "DynamicDependencyAttribute"))
            {
                dependencies ??= new DependencyList();
                dependencies.Add(factory.DynamicDependencyAttributesOnEntity(field), "DynamicDependencyAttribute present");
            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList dependencies = null;
            try
            {
                (TypeDesc owningType, IEnumerable<CustomAttributeValue<TypeDesc>> attributes) = _entity switch
                {
                    EcmaMethod method => (method.OwningType, method.GetDecodedCustomAttributes("System.Diagnostics.CodeAnalysis", "DynamicDependencyAttribute")),
                    _ => (((EcmaField)_entity).OwningType, ((EcmaField)_entity).GetDecodedCustomAttributes("System.Diagnostics.CodeAnalysis", "DynamicDependencyAttribute")),
                };

                foreach (CustomAttributeValue<TypeDesc> attribute in attributes)
                {
                    AddDependenciesDueToDynamicDependencyAttribute(ref dependencies, factory, _entity, owningType, attribute);
                }
            }
            catch (TypeSystemException)
            {
                // Ignore entities with custom attributes that don't work.
            }

            return dependencies;
        }

        private static void AddDependenciesDueToDynamicDependencyAttribute(
            ref DependencyList dependencies,
            NodeFactory factory,
            TypeSystemEntity entity,
            TypeDesc owningType,
            CustomAttributeValue<TypeDesc> attribute)
        {
            IEnumerable<TypeSystemEntity> members;

            static MetadataType Linkerify(TypeDesc type)
            {
                // IL Linker compatibility: illink will call Resolve() that will strip parameter types and genericness
                // and operate on the definition.
                while (type.IsParameterizedType)
                    type = ((ParameterizedType)type).ParameterType;
                return (MetadataType)type.GetTypeDefinition();
            }

            // First figure out the list of members that this maps to.
            // These are the ways to specify the members:
            //
            // * A string that contains a documentation signature
            // * DynamicallyAccessedMembers enum

            var fixedArgs = attribute.FixedArguments;
            TypeDesc targetType;

            UsageBasedMetadataManager metadataManager = (UsageBasedMetadataManager)factory.MetadataManager;

            if (fixedArgs.Length > 0 && fixedArgs[0].Value is string sigFromAttribute)
            {
                switch (fixedArgs.Length)
                {
                    // DynamicDependencyAttribute(String)
                    case 1:
                        targetType = owningType;
                        break;

                    // DynamicDependencyAttribute(String, Type)
                    case 2 when fixedArgs[1].Value is TypeDesc typeFromAttribute:
                        targetType = typeFromAttribute;
                        break;

                    // DynamicDependencyAttribute(String, String, String)
                    case 3 when fixedArgs[1].Value is string typeStringFromAttribute
                        && fixedArgs[2].Value is string assemblyStringFromAttribute:
                        ModuleDesc asm = factory.TypeSystemContext.ResolveAssembly(new System.Reflection.AssemblyName(assemblyStringFromAttribute), throwIfNotFound: false);
                        if (asm == null)
                        {
                            metadataManager.Logger.LogWarning(
                                new MessageOrigin(entity),
                                DiagnosticId.UnresolvedAssemblyInDynamicDependencyAttribute,
                                assemblyStringFromAttribute);
                            return;
                        }

                        targetType = DocumentationSignatureParser.GetTypeByDocumentationSignature((IAssemblyDesc)asm, typeStringFromAttribute);
                        if (targetType == null)
                        {
                            metadataManager.Logger.LogWarning(
                                new MessageOrigin(entity),
                                DiagnosticId.UnresolvedTypeInDynamicDependencyAttribute,
                                typeStringFromAttribute);
                            return;
                        }
                        break;

                    default:
                        Debug.Fail("Did we introduce a new overload?");
                        return;
                }

                members = DocumentationSignatureParser.GetMembersByDocumentationSignature(Linkerify(targetType), sigFromAttribute, acceptName: true);

                if (!members.Any())
                {
                    metadataManager.Logger.LogWarning(
                        new MessageOrigin(entity),
                        DiagnosticId.NoMembersResolvedForMemberSignatureOrType,
                        sigFromAttribute,
                        targetType.GetDisplayName());
                    return;
                }
            }
            else if (fixedArgs.Length > 0 && fixedArgs[0].Value is int memberTypesFromAttribute)
            {
                if (fixedArgs.Length == 2 && fixedArgs[1].Value is TypeDesc typeFromAttribute)
                {
                    // DynamicDependencyAttribute(DynamicallyAccessedMemberTypes, Type)
                    targetType = typeFromAttribute;
                }
                else if (fixedArgs.Length == 3 && fixedArgs[1].Value is string typeStringFromAttribute
                    && fixedArgs[2].Value is string assemblyStringFromAttribute)
                {
                    // DynamicDependencyAttribute(DynamicallyAccessedMemberTypes, String, String)
                    ModuleDesc asm = factory.TypeSystemContext.ResolveAssembly(new System.Reflection.AssemblyName(assemblyStringFromAttribute), throwIfNotFound: false);
                    if (asm == null)
                    {
                        metadataManager.Logger.LogWarning(
                            new MessageOrigin(entity),
                            DiagnosticId.UnresolvedAssemblyInDynamicDependencyAttribute,
                            assemblyStringFromAttribute);
                        return;
                    }

                    targetType = DocumentationSignatureParser.GetTypeByDocumentationSignature((IAssemblyDesc)asm, typeStringFromAttribute);
                    if (targetType == null)
                    {
                        metadataManager.Logger.LogWarning(
                            new MessageOrigin(entity),
                            DiagnosticId.UnresolvedTypeInDynamicDependencyAttribute,
                            typeStringFromAttribute);
                        return;
                    }
                }
                else
                {
                    Debug.Fail("Did we introduce a new overload?");
                    return;
                }

                members = Linkerify(targetType).GetDynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)memberTypesFromAttribute);

                if (!members.Any())
                {
                    metadataManager.Logger.LogWarning(
                        new MessageOrigin(entity),
                        DiagnosticId.NoMembersResolvedForMemberSignatureOrType,
                        memberTypesFromAttribute.ToString(),
                        targetType.GetDisplayName());
                    return;
                }
            }
            else
            {
                Debug.Fail("Did we introduce a new overload?");
                return;
            }

            Debug.Assert(members.Any());

            const string reason = "DynamicDependencyAttribute";

            // Now root the discovered members
            ReflectionMarker reflectionMarker = new ReflectionMarker(
                metadataManager.Logger,
                factory,
                metadataManager.FlowAnnotations,
                typeHierarchyDataFlowOrigin: null,
                enabled: true);
            foreach (var member in members)
            {
                reflectionMarker.MarkTypeSystemEntity(new MessageOrigin(entity), member, reason);
            }

            dependencies ??= new DependencyList();
            dependencies.AddRange(reflectionMarker.Dependencies);
        }

        protected override string GetName(NodeFactory factory)
        {
            return "DynamicDependencyAttribute analysis for " + _entity.GetDisplayName();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
    }
}
