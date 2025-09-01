// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;
using Mono.CompilerServices.SymbolWriter;
using Mono.Linker.Steps;

using CustomAttributeWithOrigin = (Mono.Cecil.CustomAttribute Attribute, Mono.Cecil.AssemblyDefinition Origin);

namespace Mono.Linker
{
    sealed class TypeMapHandler
    {
        readonly TypeMapResolver _lazyTypeMapResolver;

        // [trim target: [type map group: custom attributes with assembly origin]]
        readonly Dictionary<TypeDefinition, Dictionary<TypeReference, List<CustomAttributeWithOrigin>>> _unmarkedExternalTypeMapEntries = [];

        // [source type: [type map group: custom attributes]]
        readonly Dictionary<TypeDefinition, Dictionary<TypeReference, List<CustomAttributeWithOrigin>>> _unmarkedProxyTypeMapEntries = [];

        // CustomAttributes that we want to mark when the type mapping APIs are used.
        // [type map group: custom attributes]
        Dictionary<TypeReference, List<CustomAttributeWithOrigin>> _pendingExternalTypeMapEntries = [];
        Dictionary<TypeReference, List<CustomAttributeWithOrigin>> _pendingProxyTypeMapEntries = [];
        HashSet<TypeReference> _referencedExternalTypeMaps = [];
        HashSet<TypeReference> _referencedProxyTypeMaps = [];

        LinkContext _context = null!;
        MarkStep _markStep = null!;

        public TypeMapHandler()
        {
            _lazyTypeMapResolver = new TypeMapResolver(new HashSet<AssemblyNameReference>());
        }

        public TypeMapHandler(AssemblyDefinition entryPointAssembly)
        {
            HashSet<AssemblyNameReference> assemblies = [AssemblyNameReference.Parse(entryPointAssembly.FullName)];
            foreach (var attr in entryPointAssembly.CustomAttributes)
            {
                if (attr.AttributeType is not GenericInstanceType
                    {
                        Namespace: "System.Runtime.InteropServices",
                        GenericArguments: [_]
                    })
                {
                    continue; // Only interested in System.Runtime.InteropServices attributes
                }

                if (attr.AttributeType.Name != "TypeMapAssemblyTarget`1"
                    || attr.ConstructorArguments[0].Value is not string str)
                {
                    // Invalid attribute, skip it.
                    // Let the runtime handle the failure.
                    continue;
                }

                assemblies.Add(AssemblyNameReference.Parse(str));
            }

            _lazyTypeMapResolver = new TypeMapResolver(assemblies);
        }

        public void Initialize(LinkContext context, MarkStep markStep)
        {
            _context = context;
            _markStep = markStep;
            _lazyTypeMapResolver.Resolve(context, this);
        }

        public void ProcessExternalTypeMapGroupSeen(MethodDefinition callingMethod, TypeReference typeMapGroup)
        {
            _referencedExternalTypeMaps.Add(typeMapGroup);
            if (!_pendingExternalTypeMapEntries.Remove(typeMapGroup, out List<CustomAttributeWithOrigin>? pendingEntries))
            {
                return;
            }

            foreach (var entry in pendingEntries)
            {
                MarkTypeMapAttribute(entry, new DependencyInfo(DependencyKind.TypeMapEntry, callingMethod));
            }
        }

        public void ProcessProxyTypeMapGroupSeen(MethodDefinition callingMethod, TypeReference typeMapGroup)
        {
            _referencedProxyTypeMaps.Add(typeMapGroup);
            if (!_pendingProxyTypeMapEntries.Remove(typeMapGroup, out List<CustomAttributeWithOrigin>? pendingEntries))
            {
                return;
            }

            foreach (var entry in pendingEntries)
            {
                MarkTypeMapAttribute(entry, new DependencyInfo(DependencyKind.TypeMapEntry, callingMethod));
            }
        }

        void MarkTypeMapAttribute(CustomAttributeWithOrigin entry, DependencyInfo info)
        {
            _markStep.MarkCustomAttribute(entry.Attribute, info, new MessageOrigin(entry.Origin));

            // Mark the target type as instantiated
            TypeReference targetType = (TypeReference)entry.Attribute.ConstructorArguments[1].Value;
            if (targetType is not null && _context.Resolve(targetType) is TypeDefinition targetTypeDef)
                _context.Annotations.MarkInstantiated(targetTypeDef);
        }

        public void ProcessType(TypeDefinition definition)
        {
            RecordTargetTypeSeen(definition, _unmarkedExternalTypeMapEntries, _referencedExternalTypeMaps, _pendingExternalTypeMapEntries);
        }

        void RecordTargetTypeSeen(
            TypeDefinition targetType,
            Dictionary<TypeDefinition, Dictionary<TypeReference, List<CustomAttributeWithOrigin>>> unmarkedTypeMapAttributes,
            HashSet<TypeReference> referenceTypeMapGroups,
            Dictionary<TypeReference, List<CustomAttributeWithOrigin>> typeMapAttributesPendingUniverseMarking)
        {
            if (unmarkedTypeMapAttributes.Remove(targetType, out Dictionary<TypeReference, List<CustomAttributeWithOrigin>>? entries))
            {
                foreach (var (typeMapGroup, attributes) in entries)
                {

                    if (referenceTypeMapGroups.Contains(typeMapGroup))
                    {
                        foreach (var attr in attributes)
                        {
                            MarkTypeMapAttribute(attr, new DependencyInfo(DependencyKind.TypeMapEntry, targetType));
                        }
                    }
                    else if (!typeMapAttributesPendingUniverseMarking.TryGetValue(typeMapGroup, out List<CustomAttributeWithOrigin>? value))
                    {
                        typeMapAttributesPendingUniverseMarking[typeMapGroup] = [.. attributes];
                    }
                    else
                    {
                        value.AddRange(attributes);
                    }
                }
            }
        }

        public void ProcessInstantiated(TypeDefinition definition)
        {
            RecordTargetTypeSeen(definition, _unmarkedProxyTypeMapEntries, _referencedProxyTypeMaps, _pendingProxyTypeMapEntries);
        }

        void AddExternalTypeMapEntry(TypeReference group, CustomAttributeWithOrigin attr)
        {
            if (attr.Attribute.ConstructorArguments is [_, _, { Value: TypeReference trimTarget }])
            {
                RecordTypeMapEntry(attr, group, trimTarget, _unmarkedExternalTypeMapEntries);
                return;
            }
            if (attr.Attribute.ConstructorArguments is [_, { Value: TypeReference }])
            {
                // There's no trim target, so include the attribute unconditionally.
                RecordTypeMapEntry(attr, group, null, _unmarkedExternalTypeMapEntries);
                return;
            }
            // Invalid attribute, skip it.
            // Let the runtime handle the failure.
        }

        void AddProxyTypeMapEntry(TypeReference group, CustomAttributeWithOrigin attr)
        {
            if (attr.Attribute.ConstructorArguments is [{ Value: TypeReference sourceType }, _])
            {
                // This is a TypeMapAssociationAttribute, which has a single type argument.
                RecordTypeMapEntry(attr, group, sourceType, _unmarkedProxyTypeMapEntries);
                return;
            }
            // Invalid attribute, skip it.
            // Let the runtime handle the failure.
        }

        void RecordTypeMapEntry(CustomAttributeWithOrigin attr, TypeReference group, TypeReference? trimTarget, Dictionary<TypeDefinition, Dictionary<TypeReference, List<CustomAttributeWithOrigin>>> unmarkedEntryList)
        {
            if (trimTarget is null)
            {
                // If there's no trim target, we can just mark the attribute.
                MarkTypeMapAttribute(attr, new DependencyInfo(DependencyKind.TypeMapEntry, null));
                return;
            }

            TypeDefinition? typeDef = _context.Resolve(trimTarget);
            if (typeDef is null)
            {
                return; // Couldn't find the type we were asked about.
            }

            if (_context.Annotations.IsMarked(typeDef))
            {
                MarkTypeMapAttribute(attr, new DependencyInfo(DependencyKind.TypeMapEntry, trimTarget));
            }
            else
            {
                if (!unmarkedEntryList.TryGetValue(typeDef, out Dictionary<TypeReference, List<CustomAttributeWithOrigin>>? entries))
                {
                    entries = new() {
                        { group, [] }
                    };
                    unmarkedEntryList[typeDef] = entries;
                }

                if (!entries.TryGetValue(group, out List<CustomAttributeWithOrigin>? attrs))
                {
                    entries[group] = [attr];
                }
                else
                {
                    attrs.Add(attr);
                }
            }
        }

        public static bool IsTypeMapAttributeType(TypeDefinition type)
        {
            return type is { Namespace: "System.Runtime.InteropServices", Name: "TypeMapAttribute`1" or "TypeMapAssociationAttribute`1" or "TypeMapAssemblyTargetAttribute`1" };
        }

        class TypeMapResolver(IReadOnlySet<AssemblyNameReference> assemblies)
        {
            public void Resolve(LinkContext context, TypeMapHandler manager)
            {
                foreach (AssemblyNameReference assemblyName in assemblies)
                {
                    if (context.TryResolve(assemblyName) is not AssemblyDefinition assembly)
                    {
                        // If we cannot find the assembly, skip it.
                        // We'll fail at runtime as expected.
                        continue;
                    }
                    foreach (CustomAttribute attr in assembly.CustomAttributes)
                    {
                        if (attr.AttributeType is not GenericInstanceType
                            {
                                Namespace: "System.Runtime.InteropServices",
                                GenericArguments: [TypeReference typeMapGroup]
                            })
                        {
                            continue; // Only interested in System.Runtime.InteropServices attributes
                        }

                        if (attr.AttributeType.Name is "TypeMapAttribute`1")
                        {
                            manager.AddExternalTypeMapEntry(typeMapGroup, (attr, assembly));
                        }
                        else if (attr.AttributeType.Name is "TypeMapAssociationAttribute`1")
                        {
                            manager.AddProxyTypeMapEntry(typeMapGroup, (attr, assembly));
                        }
                    }
                }
            }
        }
    }
}
