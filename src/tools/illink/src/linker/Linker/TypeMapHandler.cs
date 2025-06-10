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

namespace Mono.Linker
{
	sealed class TypeMapHandler
	{
		readonly TypeMapResolver _lazyTypeMapResolver;

		// [trim target: [type map group: custom attributes]]
		readonly Dictionary<TypeDefinition, Dictionary<TypeReference, List<CustomAttribute>>> _unmarkedExternalTypeMapEntries = [];

		// [source type: [type map group: custom attributes]]
		readonly Dictionary<TypeDefinition, Dictionary<TypeReference, List<CustomAttribute>>> _unmarkedProxyTypeMapEntries = [];

		// CustomAttributes that we want to mark when the type mapping APIs are used.
		// [type map group: custom attributes]
		Dictionary<TypeReference, List<CustomAttribute>> _pendingExternalTypeMapEntries = [];
		Dictionary<TypeReference, List<CustomAttribute>> _pendingProxyTypeMapEntries = [];
		HashSet<TypeReference> _referencedExternalTypeMaps = [];
		HashSet<TypeReference> _referencedProxyTypeMaps = [];

		LinkContext _context = null!;
		MarkStep _markStep = null!;

		public TypeMapHandler ()
		{
			_lazyTypeMapResolver = new TypeMapResolver (new HashSet<AssemblyNameReference>());
		}

		public TypeMapHandler (AssemblyDefinition entryPointAssembly)
		{
			HashSet<AssemblyNameReference> assemblies = [AssemblyNameReference.Parse (entryPointAssembly.FullName)];
			foreach (var attr in entryPointAssembly.CustomAttributes) {
				if (attr.AttributeType is not GenericInstanceType {
					Namespace: "System.Runtime.InteropServices",
					GenericArguments: [_]
				}) {
					continue; // Only interested in System.Runtime.InteropServices attributes
				}

				if (attr.AttributeType.Name != "TypeMapAssemblyTarget`1"
					|| attr.ConstructorArguments[0].Value is not string str) {
					// Invalid attribute, skip it.
					// Let the runtime handle the failure.
					continue;
				}

				assemblies.Add (AssemblyNameReference.Parse(str));
			}

			_lazyTypeMapResolver = new TypeMapResolver (assemblies);
		}

		public void Initialize (LinkContext context, MarkStep markStep)
		{
			_context = context;
			_markStep = markStep;
			_lazyTypeMapResolver.Resolve (context, this);
		}

		public void ProcessExternalTypeMapGroupSeen (MethodDefinition callingMethod, TypeReference typeMapGroup)
		{
			_referencedExternalTypeMaps.Add (typeMapGroup);
			if (!_pendingExternalTypeMapEntries.Remove (typeMapGroup, out List<CustomAttribute>? pendingEntries)) {
				return;
			}

			foreach (var entry in pendingEntries) {
				MarkTypeMapAttribute (entry, new DependencyInfo (DependencyKind.TypeMapEntry, callingMethod), new MessageOrigin (callingMethod));
			}
		}

		public void ProcessProxyTypeMapGroupSeen (MethodDefinition callingMethod, TypeReference typeMapGroup)
		{
			_referencedProxyTypeMaps.Add (typeMapGroup);
			if (!_pendingProxyTypeMapEntries.Remove (typeMapGroup, out List<CustomAttribute>? pendingEntries)) {
				return;
			}

			foreach (var entry in pendingEntries) {
				MarkTypeMapAttribute (entry, new DependencyInfo (DependencyKind.TypeMapEntry, callingMethod), new MessageOrigin (callingMethod));
			}
		}

		void MarkTypeMapAttribute (CustomAttribute entry, DependencyInfo info, MessageOrigin origin)
		{
			_markStep.MarkCustomAttribute (entry, info, new MessageOrigin (origin));

			// Mark the target type as instantiated
			TypeReference targetType = (TypeReference) entry.ConstructorArguments[1].Value;
			if (targetType is not null && _context.Resolve (targetType) is TypeDefinition targetTypeDef)
				_context.Annotations.MarkInstantiated (targetTypeDef);
		}

		public void ProcessType (TypeDefinition definition)
		{
			RecordTargetTypeSeen (definition, _unmarkedExternalTypeMapEntries, _referencedExternalTypeMaps, _pendingExternalTypeMapEntries);
		}

		void RecordTargetTypeSeen (TypeDefinition definition, Dictionary<TypeDefinition, Dictionary<TypeReference, List<CustomAttribute>>> unmarked, HashSet<TypeReference> referenced, Dictionary<TypeReference, List<CustomAttribute>> pending)
		{
			if (unmarked.Remove (definition, out Dictionary<TypeReference, List<CustomAttribute>>? entries)) {
				foreach (var (key, attributes) in entries) {

					if (referenced.Contains (key)) {
						foreach (var attr in attributes) {
							MarkTypeMapAttribute (attr, new DependencyInfo (DependencyKind.TypeMapEntry, definition), new MessageOrigin (definition));
						}
					}

					if (!pending.TryGetValue (key, out List<CustomAttribute>? value)) {
						pending[key] = [.. attributes];
					} else {
						value.AddRange (attributes);
					}
				}
				unmarked.Remove (definition);
			}
		}

		public void ProcessInstantiated (TypeDefinition definition)
		{
			RecordTargetTypeSeen (definition, _unmarkedProxyTypeMapEntries, _referencedProxyTypeMaps, _pendingProxyTypeMapEntries);
		}

		void AddExternalTypeMapEntry (TypeReference group, CustomAttribute attr)
		{
			if (attr.ConstructorArguments is [_, _, { Value: TypeReference trimTarget }]) {
				RecordTypeMapEntry (attr, group, trimTarget, _unmarkedExternalTypeMapEntries);
				return;
			}
			if (attr.ConstructorArguments is [_, { Value: TypeReference target }]) {
				// This is a TypeMapAssemblyTargetAttribute, which has a single type argument.
				RecordTypeMapEntry (attr, group, target, _unmarkedExternalTypeMapEntries);
				return;
			}
			// Invalid attribute, skip it.
			// Let the runtime handle the failure.
		}

		void AddProxyTypeMapEntry (TypeReference group, CustomAttribute attr)
		{
			if (attr.ConstructorArguments is [{ Value: TypeReference sourceType }, _]) {
				// This is a TypeMapAssociationAttribute, which has a single type argument.
				RecordTypeMapEntry (attr, group, sourceType, _unmarkedProxyTypeMapEntries);
				return;
			}
			// Invalid attribute, skip it.
			// Let the runtime handle the failure.
		}

		void RecordTypeMapEntry (CustomAttribute attr, TypeReference group, TypeReference trimTarget, Dictionary<TypeDefinition, Dictionary<TypeReference, List<CustomAttribute>>> unmarkedEntryList)
		{
			TypeDefinition? typeDef = _context.Resolve (trimTarget);
			if (typeDef is null) {
				return; // Couldn't find the type we were asked about.
			}

			if (_context.Annotations.IsMarked (typeDef)) {
				MarkTypeMapAttribute (attr, new DependencyInfo (DependencyKind.TypeMapEntry, trimTarget), new MessageOrigin(typeDef));
			} else {

				if (!unmarkedEntryList.TryGetValue (typeDef, out Dictionary<TypeReference, List<CustomAttribute>>? entries)) {
					entries = new () {
						{ group, [] }
					};
					unmarkedEntryList[typeDef] = entries;
				}

				if (!entries.TryGetValue(group, out List<CustomAttribute>? attrs)) {
					entries[group] = [attr];
				} else {
					attrs.Add (attr);
				}
			}
		}

		class TypeMapResolver (IReadOnlySet<AssemblyNameReference> assemblies)
		{
			public void Resolve (LinkContext context, TypeMapHandler manager)
			{
				foreach (AssemblyNameReference assemblyName in assemblies) {
					if (context.TryResolve(assemblyName) is not AssemblyDefinition assembly) {
						// If we cannot find the assembly, skip it.
						// We'll fail at runtime as expected.
						continue;
					}
					foreach (CustomAttribute attr in assembly.CustomAttributes) {
						if (attr.AttributeType is not GenericInstanceType {
							Namespace: "System.Runtime.InteropServices",
							GenericArguments: [TypeReference typeMapGroup]
						}) {
							continue; // Only interested in System.Runtime.InteropServices attributes
						}

						if (attr.AttributeType.Name is "TypeMapAttribute`1") {
							manager.AddExternalTypeMapEntry (typeMapGroup, attr);
						} else if (attr.AttributeType.Name is "TypeMapAssociationAttribute`1") {
							manager.AddProxyTypeMapEntry (typeMapGroup, attr);
						}
					}
				}
			}
		}
	}
}
