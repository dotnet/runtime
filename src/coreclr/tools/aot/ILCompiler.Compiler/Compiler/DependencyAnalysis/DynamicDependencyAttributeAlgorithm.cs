// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILCompiler.Logging;

using ILLink.Shared;

using static ILCompiler.Dataflow.DynamicallyAccessedMembersBinder;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Computes the list of dependencies from DynamicDependencyAttribute.
    /// https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.dynamicdependencyattribute
    /// </summary>
    internal static class DynamicDependencyAttributeAlgorithm
    {
        public static void AddDependenciesDueToDynamicDependencyAttribute(ref DependencyList dependencies, NodeFactory factory, EcmaMethod method)
        {
            foreach (var attribute in method.GetDecodedCustomAttributes("System.Diagnostics.CodeAnalysis", "DynamicDependencyAttribute"))
            {
                AddDependenciesDueToDynamicDependencyAttribute(ref dependencies, factory, method, method.OwningType, attribute);
            }
        }

        public static void AddDependenciesDueToDynamicDependencyAttribute(ref DependencyList dependencies, NodeFactory factory, EcmaField field)
        {
            foreach (var attribute in field.GetDecodedCustomAttributes("System.Diagnostics.CodeAnalysis", "DynamicDependencyAttribute"))
            {
                AddDependenciesDueToDynamicDependencyAttribute(ref dependencies, factory, field, field.OwningType, attribute);
            }
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
                            ((UsageBasedMetadataManager)factory.MetadataManager).Logger.LogWarning(
                                new MessageOrigin(entity),
                                DiagnosticId.UnresolvedAssemblyInDynamicDependencyAttribute,
                                assemblyStringFromAttribute);
                            return;
                        }

                        targetType = DocumentationSignatureParser.GetTypeByDocumentationSignature((IAssemblyDesc)asm, typeStringFromAttribute);
                        if (targetType == null)
                        {
                            ((UsageBasedMetadataManager)factory.MetadataManager).Logger.LogWarning(
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
                    ((UsageBasedMetadataManager)factory.MetadataManager).Logger.LogWarning(
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
                        ((UsageBasedMetadataManager)factory.MetadataManager).Logger.LogWarning(
                            new MessageOrigin(entity),
                            DiagnosticId.UnresolvedAssemblyInDynamicDependencyAttribute,
                            assemblyStringFromAttribute);
                        return;
                    }

                    targetType = DocumentationSignatureParser.GetTypeByDocumentationSignature((IAssemblyDesc)asm, typeStringFromAttribute);
                    if (targetType == null)
                    {
                        ((UsageBasedMetadataManager)factory.MetadataManager).Logger.LogWarning(
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
                    ((UsageBasedMetadataManager)factory.MetadataManager).Logger.LogWarning(
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
            foreach (var member in members)
            {
                switch (member)
                {
                    case MethodDesc m:
                        RootingHelpers.TryGetDependenciesForReflectedMethod(ref dependencies, factory, m, reason);
                        break;
                    case FieldDesc field:
                        RootingHelpers.TryGetDependenciesForReflectedField(ref dependencies, factory, field, reason);
                        break;
                    case MetadataType nestedType:
                        RootingHelpers.TryGetDependenciesForReflectedType(ref dependencies, factory, nestedType, reason);
                        break;
                    case PropertyPseudoDesc property:
                        if (property.GetMethod != null)
                            RootingHelpers.TryGetDependenciesForReflectedMethod(ref dependencies, factory, property.GetMethod, reason);
                        if (property.SetMethod != null)
                            RootingHelpers.TryGetDependenciesForReflectedMethod(ref dependencies, factory, property.SetMethod, reason);
                        break;
                    case EventPseudoDesc @event:
                        if (@event.AddMethod != null)
                            RootingHelpers.TryGetDependenciesForReflectedMethod(ref dependencies, factory, @event.AddMethod, reason);
                        if (@event.RemoveMethod != null)
                            RootingHelpers.TryGetDependenciesForReflectedMethod(ref dependencies, factory, @event.RemoveMethod, reason);
                        break;
                    default:
                        Debug.Fail(member.GetType().ToString());
                        break;
                }
            }
        }
    }
}
