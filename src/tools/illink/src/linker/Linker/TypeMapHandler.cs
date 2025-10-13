// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Steps;

using CustomAttributeWithOrigin = (Mono.Cecil.CustomAttribute Attribute, Mono.Cecil.AssemblyDefinition Origin);

namespace Mono.Linker
{
    sealed class TypeMapHandler
    {
        TypeMapResolver _lazyTypeMapResolver = null!;
        LinkContext _context = null!;
        MarkStep _markStep = null!;

        // [trim target: [type map group: custom attributes with assembly origin]]
        readonly Dictionary<TypeDefinition, Dictionary<TypeReference, List<CustomAttributeWithOrigin>>> _unmarkedExternalTypeMapEntries = [];

        // [source type: [type map group: custom attributes]]
        readonly Dictionary<TypeDefinition, Dictionary<TypeReference, List<CustomAttributeWithOrigin>>> _unmarkedProxyTypeMapEntries = [];

        // CustomAttributes that we want to mark when the type mapping APIs are used.
        // [type map group: custom attributes]
        Dictionary<TypeReference, List<CustomAttributeWithOrigin>> _pendingExternalTypeMapEntries = null!;
        Dictionary<TypeReference, List<CustomAttributeWithOrigin>> _pendingProxyTypeMapEntries = null!;
        Dictionary<TypeReference, List<CustomAttributeWithOrigin>> _pendingAssemblyTargets = null!;

        HashSet<TypeReference> _referencedExternalTypeMaps = null!;
        HashSet<TypeReference> _referencedProxyTypeMaps = null!;

        public TypeMapHandler()
        {
        }

        [Conditional("DEBUG")]
        private void EnsureInitialized()
        {
            if (_lazyTypeMapResolver is null)
                throw new InvalidOperationException("TypeMapHandler not initialized");
        }

        public void Initialize(LinkContext context, MarkStep markStep, AssemblyDefinition? entryPointAssembly)
        {
            _context = context;
            _markStep = markStep;
            var typeReferenceEqualityComparer = new TypeReferenceEqualityComparer(context);
            _pendingExternalTypeMapEntries = new(typeReferenceEqualityComparer);
            _pendingProxyTypeMapEntries = new(typeReferenceEqualityComparer);
            _pendingAssemblyTargets = new(typeReferenceEqualityComparer);
            _referencedExternalTypeMaps = new(typeReferenceEqualityComparer);
            _referencedProxyTypeMaps = new(typeReferenceEqualityComparer);
            var typeMapResolver = new TypeMapResolver(entryPointAssembly);
            typeMapResolver.Resolve(context, this);
            _lazyTypeMapResolver = typeMapResolver;
        }

        public void ProcessExternalTypeMapGroupSeen(MethodDefinition callingMethod, TypeReference typeMapGroup)
        {
            EnsureInitialized();
            _referencedExternalTypeMaps.Add(typeMapGroup);
            if (_pendingExternalTypeMapEntries.Remove(typeMapGroup, out List<CustomAttributeWithOrigin>? pendingEntries))
            {
                foreach (var entry in pendingEntries)
                {
                    MarkTypeMapAttribute(entry, new DependencyInfo(DependencyKind.TypeMapEntry, callingMethod));
                }
            }
            if (_pendingAssemblyTargets.Remove(typeMapGroup, out List<CustomAttributeWithOrigin>? assemblyTargets))
            {
                foreach (var entry in assemblyTargets)
                {
                    var info = new DependencyInfo(DependencyKind.TypeMapAssemblyTarget, callingMethod);
                    MarkTypeMapAttribute(entry, info);
                }
            }
        }

        public void ProcessProxyTypeMapGroupSeen(MethodDefinition callingMethod, TypeReference typeMapGroup)
        {
            EnsureInitialized();
            _referencedProxyTypeMaps.Add(typeMapGroup);
            if (_pendingProxyTypeMapEntries.Remove(typeMapGroup, out List<CustomAttributeWithOrigin>? pendingEntries))
            {
                foreach (var entry in pendingEntries)
                {
                    MarkTypeMapAttribute(entry, new DependencyInfo(DependencyKind.TypeMapEntry, callingMethod));
                }
            }
            if (_pendingAssemblyTargets.Remove(typeMapGroup, out List<CustomAttributeWithOrigin>? assemblyTargets))
            {
                foreach (var entry in assemblyTargets)
                {
                    var info = new DependencyInfo(DependencyKind.TypeMapAssemblyTarget, callingMethod);
                    MarkTypeMapAttribute(entry, info);
                }
            }
        }

        void MarkTypeMapAttribute(CustomAttributeWithOrigin entry, DependencyInfo info)
        {
            _markStep.MarkCustomAttribute(entry.Attribute, info, new MessageOrigin(entry.Origin));
            _markStep.MarkAssembly(entry.Origin, info, new MessageOrigin(entry.Origin));

            // Mark the target type as instantiated
            if (entry.TargetType is { } targetType && _context.Resolve(targetType) is TypeDefinition targetTypeDef)
                _context.Annotations.MarkInstantiated(targetTypeDef);
        }

        public void ProcessType(TypeDefinition definition)
        {
            EnsureInitialized();
            RecordTargetTypeSeen(definition, _unmarkedExternalTypeMapEntries, _referencedExternalTypeMaps, _pendingExternalTypeMapEntries);
        }

        void RecordTargetTypeSeen(
            TypeDefinition targetType,
            Dictionary<TypeDefinition, Dictionary<TypeReference, List<CustomAttributeWithOrigin>>> unmarkedTypeMapAttributes,
            HashSet<TypeReference> referenceTypeMapGroups,
            Dictionary<TypeReference, List<CustomAttributeWithOrigin>> typeMapAttributesPendingUniverseMarking)
        {
            if (!unmarkedTypeMapAttributes.Remove(targetType, out Dictionary<TypeReference, List<CustomAttributeWithOrigin>>? entries))
                return;

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

        public void ProcessInstantiated(TypeDefinition definition)
        {
            EnsureInitialized();
            RecordTargetTypeSeen(definition, _unmarkedProxyTypeMapEntries, _referencedProxyTypeMaps, _pendingProxyTypeMapEntries);
        }

        void AddExternalTypeMapEntry(TypeReference group, CustomAttributeWithOrigin attr)
        {
            if (attr.Attribute.ConstructorArguments is [_, _, { Value: TypeReference trimTarget }])
            {
                RecordTypeMapEntry(attr, group, trimTarget, _unmarkedExternalTypeMapEntries, _referencedExternalTypeMaps, _pendingExternalTypeMapEntries);
            }
            else if (attr.Attribute.ConstructorArguments is [_, { Value: TypeReference }])
            {
                // There's no trim target, so include the attribute unconditionally.
                RecordTypeMapEntry(attr, group, null, _unmarkedExternalTypeMapEntries, _referencedExternalTypeMaps, _pendingExternalTypeMapEntries);
            }
            // Invalid attribute, skip it.
            // Let the runtime handle the failure.
        }

        void AddProxyTypeMapEntry(TypeReference group, CustomAttributeWithOrigin attr)
        {
            if (attr.Attribute.ConstructorArguments is [{ Value: TypeReference sourceType }, _])
            {
                // This is a TypeMapAssociationAttribute, which has a single type argument.
                RecordTypeMapEntry(attr, group, sourceType, _unmarkedProxyTypeMapEntries, _referencedProxyTypeMaps, _pendingProxyTypeMapEntries);
                return;
            }
            // Invalid attribute, skip it.
            // Let the runtime handle the failure.
        }

        private void AddAssemblyTarget(TypeReference typeMapGroup, CustomAttributeWithOrigin attr)
        {
            // Validate attribute
            if (attr.Attribute.ConstructorArguments is not ([{ Value: string }]))
                return;

            // If the type map group has been seen, mark the attribute immediately
            if (_referencedExternalTypeMaps.Contains(typeMapGroup) || _referencedProxyTypeMaps.Contains(typeMapGroup))
            {
                _markStep.MarkCustomAttribute(attr.Attribute, new DependencyInfo(DependencyKind.TypeMapEntry, null), new MessageOrigin(attr.Origin));
                return;
            }

            // Otherwise, it's pending until the type map group is seen
            _pendingAssemblyTargets.AddToList(typeMapGroup, attr);
        }


        void RecordTypeMapEntry(CustomAttributeWithOrigin attr, TypeReference group, TypeReference? dependencySource, Dictionary<TypeDefinition, Dictionary<TypeReference, List<CustomAttributeWithOrigin>>> pendingDependencySourceMarking, HashSet<TypeReference> seenTypeGroups, Dictionary<TypeReference, List<CustomAttributeWithOrigin>> pendingTypeMapGroupMarking)
        {
            if (dependencySource is null)
            {
                // Mark or directly add to pending group
                if (seenTypeGroups.Contains(group))
                {
                    // If there's no trim target, we can just mark the attribute.
                    MarkTypeMapAttribute(attr, new DependencyInfo(DependencyKind.TypeMapEntry, null));
                    return;
                }
                else
                {
                    pendingTypeMapGroupMarking.AddToList(group, attr);
                    return;
                }
            }

            TypeDefinition? dependencyTypeDef = _context.Resolve(dependencySource);
            if (dependencyTypeDef is null)
            {
                return; // Couldn't find the type we were asked about.
            }

            if (attr.DependencySourceRequiresTarget(_context, dependencyTypeDef))
            {
                if (seenTypeGroups.Contains(group))
                {
                    MarkTypeMapAttribute(attr, new DependencyInfo(DependencyKind.TypeMapEntry, dependencySource));
                }
                else
                {
                    pendingTypeMapGroupMarking.AddToList(group, attr);
                }
            }
            else
            {
                if (!pendingDependencySourceMarking.TryGetValue(dependencyTypeDef, out Dictionary<TypeReference, List<CustomAttributeWithOrigin>>? entries))
                {
                    entries = new(new TypeReferenceEqualityComparer(_context)) {
                        { group, [] }
                    };
                    pendingDependencySourceMarking[dependencyTypeDef] = entries;
                }

                entries.AddToList(group, attr);
            }
        }

        public static bool IsTypeMapAttributeType(TypeDefinition type)
        {
            return type is { Namespace: "System.Runtime.InteropServices", Name: "TypeMapAttribute`1" or "TypeMapAssociationAttribute`1" or "TypeMapAssemblyTargetAttribute`1" };
        }

        class TypeMapResolver(AssemblyDefinition? _assembly)
        {
            public void Resolve(LinkContext context, TypeMapHandler manager)
            {
                if (_assembly is null)
                    return;
                HashSet<AssemblyDefinition> seen = new();
                Queue<AssemblyDefinition> toVisit = new();
                toVisit.Enqueue(_assembly);
                while (toVisit.Count > 0)
                {
                    var assembly = toVisit.Dequeue();
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
                        else if (attr.AttributeType.Name is "TypeMapAssemblyTargetAttribute`1")
                        {
                            manager.AddAssemblyTarget(typeMapGroup, (attr, assembly));
                            if (attr.ConstructorArguments[0].Value is string str)
                            {
                                var nextAssemblyName = AssemblyNameReference.Parse(str);
                                if (context.TryResolve(nextAssemblyName) is AssemblyDefinition nextAssembly)
                                {
                                    if (seen.Add(nextAssembly))
                                    {
                                        toVisit.Enqueue(nextAssembly);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    file static class CustomAttributeWithOriginExtensions
    {
        extension(CustomAttributeWithOrigin self)
        {
            public bool DependencySourceRequiresTarget(LinkContext context, TypeDefinition sourceType)
            {
                return self.Attribute.AttributeType.Name switch
                {
                    "TypeMapAttribute`1" =>
                        sourceType is null || context.Annotations.IsMarked(sourceType),
                    "TypeMapAssociationAttribute`1" =>
                        context.Annotations.IsInstantiated(sourceType),
                    _ => false,
                };
            }

            public TypeReference? TargetType =>
                self.Attribute.AttributeType.Name switch
                {
                    "TypeMapAttribute`1" or "TypeMapAssociationAttribute`1" =>
                        (TypeReference)self.Attribute.ConstructorArguments[1].Value!,
                    _ => null
                };

        }
    }
 }
