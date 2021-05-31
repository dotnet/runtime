// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker.Dataflow
{
	class DynamicallyAccessedMembersTypeHierarchy
	{
		readonly LinkContext _context;
		readonly MarkStep _markStep;
		readonly MarkScopeStack _scopeStack;

		// Cache of DynamicallyAccessedMembers annotations applied to types and their hierarchies
		// Values
		//   annotation - the aggregated annotation value from the entire base and interface hierarchy of the given type
		//                If the type has a base class with annotation a1 and an interface with annotation a2, the stored
		//                annotation is a1 | a2.
		//   applied - set to true once the annotation was applied to the type
		//             This only happens once the right reflection pattern is found.
		//             If a new type is being marked and one of its base types/interface has the applied set to true
		//             the new type will apply its annotation and will also set its applied to true.
		// Non-interface types
		//   - Only marked types with non-empty annotation are put into the cache
		//   - Non-marked types are not stored in the cache
		//   - Marked types which are not in the cache don't have any annotation
		// Interface types
		//   - All interface types accessible from marked types are stored in the cache
		//   - If the interface type doesn't have annotation the value None is stored here
		//
		// It's not possible to use the marking as a filter for interfaces in the cache
		// because interfaces are marked late and in effectively random order.
		// For this cache to be effective we need to be able to fill it for all base types and interfaces
		// of a type which is currently being marked - at which point the interfaces are not yet marked.
		readonly Dictionary<TypeDefinition, (DynamicallyAccessedMemberTypes annotation, bool applied)> _typesInDynamicallyAccessedMembersHierarchy;

		public DynamicallyAccessedMembersTypeHierarchy (LinkContext context, MarkStep markStep, MarkScopeStack scopeStack)
		{
			_context = context;
			_markStep = markStep;
			_scopeStack = scopeStack;
			_typesInDynamicallyAccessedMembersHierarchy = new Dictionary<TypeDefinition, (DynamicallyAccessedMemberTypes, bool)> ();
		}

		public (DynamicallyAccessedMemberTypes annotation, bool applied) ProcessMarkedTypeForDynamicallyAccessedMembersHierarchy (TypeDefinition type)
		{
			// Non-interfaces must be marked already
			Debug.Assert (type.IsInterface || _context.Annotations.IsMarked (type));

			DynamicallyAccessedMemberTypes annotation = _context.Annotations.FlowAnnotations.GetTypeAnnotation (type);
			bool apply = false;

			// We'll use the cache also as a way to detect and avoid recursion
			// There's no possiblity to have recursion among base types, so only do this for interfaces
			if (type.IsInterface) {
				if (_typesInDynamicallyAccessedMembersHierarchy.TryGetValue (type, out var existingValue))
					return existingValue;

				_typesInDynamicallyAccessedMembersHierarchy.Add (type, (annotation, false));
			}

			// Base should already be marked (since we're marking its derived type now)
			// so we should already have its cached values filled.
			TypeDefinition baseType = type.BaseType?.Resolve ();
			Debug.Assert (baseType == null || _context.Annotations.IsMarked (baseType));
			if (baseType != null && _typesInDynamicallyAccessedMembersHierarchy.TryGetValue (baseType, out var baseValue)) {
				annotation |= baseValue.annotation;
				apply |= baseValue.applied;
			}

			// For the purposes of the DynamicallyAccessedMembers type hierarchies
			// we consider interfaces of marked types to be also "marked" in that 
			// their annotations will be applied to the type regardless if later on
			// we decide to remove the interface. This is to keep the complexity of the implementation
			// relatively low. In the future it could be possibly optimized.
			if (type.HasInterfaces) {
				foreach (InterfaceImplementation iface in type.Interfaces) {
					var interfaceType = iface.InterfaceType.Resolve ();
					if (interfaceType != null) {
						var interfaceValue = ProcessMarkedTypeForDynamicallyAccessedMembersHierarchy (interfaceType);
						annotation |= interfaceValue.annotation;
						apply |= interfaceValue.applied;
					}
				}
			}

			Debug.Assert (!apply || annotation != DynamicallyAccessedMemberTypes.None);

			// Store the results in the cache
			// Don't store empty annotations for non-interface types - we can use the presence of the row
			// in the cache as indication of it instead.
			// This doesn't work for interfaces, since we can't rely on them being marked (and thus have the cache
			// already filled), so we need to always store the row (even if empty) for interfaces.
			if (annotation != DynamicallyAccessedMemberTypes.None || type.IsInterface) {
				_typesInDynamicallyAccessedMembersHierarchy[type] = (annotation, apply);
			}

			// It's important to first store the annotation in the cache and only then apply the annotation.
			// Applying the annotation will lead to marking additional types which in turn calls back into this
			// method to look for annotations. If the newly marked type derives from the one we're processing
			// it will rely on the cache to know if it's annotated - so the record must be in the cache
			// before it happens.
			if (apply) {
				// One of the base/interface types is already marked as having the annotation applied
				// so we need to apply the annotation to this type as well
				using var _ = _scopeStack.PushScope (new MessageOrigin (type));
				var reflectionMethodBodyScanner = new ReflectionMethodBodyScanner (_context, _markStep, _scopeStack);
				var reflectionPatternContext = new ReflectionPatternContext (_context, true, _scopeStack.CurrentScope.Origin, type);
				reflectionMethodBodyScanner.ApplyDynamicallyAccessedMembersToType (ref reflectionPatternContext, type, annotation);
				reflectionPatternContext.Dispose ();
			}

			return (annotation, apply);
		}

		public DynamicallyAccessedMemberTypes ApplyDynamicallyAccessedMembersToTypeHierarchy (
			ReflectionMethodBodyScanner reflectionMethodBodyScanner,
			ref ReflectionPatternContext reflectionPatternContext,
			TypeDefinition type)
		{
			Debug.Assert (_context.Annotations.IsMarked (type));

			// The type should be in our cache already
			(var annotation, var applied) = GetCachedInfoForTypeInHierarchy (type);

			// If the annotation was already applied to this type, there's no reason to repeat the operation, the result will
			// be no change.
			if (applied || annotation == DynamicallyAccessedMemberTypes.None)
				return annotation;

			// Apply the effective annotation for the type
			reflectionMethodBodyScanner.ApplyDynamicallyAccessedMembersToType (ref reflectionPatternContext, type, annotation);

			// Mark it as applied in the cache
			_typesInDynamicallyAccessedMembersHierarchy[type] = (annotation, true);

			// Propagate the newly applied annotation to all derived/implementation types
			// Since we don't have a data structure which would allow us to enumerate all derived/implementation types
			// walk all of the types in the cache. These are good candidates as types not in the cache don't apply.
			//
			// Applying annotations can lead to marking additional types which can lead to adding new records
			// to the cache. So we can't simply iterate over the cache. We also can't rely on the auto-applying annotations
			// which is triggered from marking via ProcessMarkedTypeForDynamicallyAccessedMembersHierarchy as that will
			// only reliably work once the annotations are applied to all types in the cache first. Partially
			// applied annotations to the cache are not enough. So we have to apply the annotations to any types
			// added to the cache during the application as well.
			//
			HashSet<TypeDefinition> typesProcessed = new HashSet<TypeDefinition> ();
			List<TypeDefinition> candidateTypes = new List<TypeDefinition> ();
			while (true) {
				candidateTypes.Clear ();
				foreach (var candidate in _typesInDynamicallyAccessedMembersHierarchy) {
					if (candidate.Value.annotation == DynamicallyAccessedMemberTypes.None || candidate.Value.applied)
						continue;

					if (typesProcessed.Add (candidate.Key))
						candidateTypes.Add (candidate.Key);
				}

				if (candidateTypes.Count == 0)
					break;

				foreach (var candidateType in candidateTypes) {
					ApplyDynamicallyAccessedMembersToTypeHierarchyInner (reflectionMethodBodyScanner, ref reflectionPatternContext, candidateType);
				}
			}

			return annotation;
		}

		bool ApplyDynamicallyAccessedMembersToTypeHierarchyInner (
			ReflectionMethodBodyScanner reflectionMethodBodyScanner,
			ref ReflectionPatternContext reflectionPatternContext,
			TypeDefinition type)
		{
			(var annotation, var applied) = GetCachedInfoForTypeInHierarchy (type);

			if (annotation == DynamicallyAccessedMemberTypes.None)
				return false;

			if (applied)
				return true;

			TypeDefinition baseType = type.BaseType?.Resolve ();
			if (baseType != null)
				applied = ApplyDynamicallyAccessedMembersToTypeHierarchyInner (reflectionMethodBodyScanner, ref reflectionPatternContext, baseType);

			if (!applied && type.HasInterfaces) {
				foreach (InterfaceImplementation iface in type.Interfaces) {
					var interfaceType = iface.InterfaceType.Resolve ();
					if (interfaceType != null) {
						if (ApplyDynamicallyAccessedMembersToTypeHierarchyInner (reflectionMethodBodyScanner, ref reflectionPatternContext, interfaceType)) {
							applied = true;
							break;
						}
					}
				}
			}

			if (applied) {
				reflectionMethodBodyScanner.ApplyDynamicallyAccessedMembersToType (ref reflectionPatternContext, type, annotation);
				_typesInDynamicallyAccessedMembersHierarchy[type] = (annotation, true);
			}

			return applied;
		}

		(DynamicallyAccessedMemberTypes annotation, bool applied) GetCachedInfoForTypeInHierarchy (TypeDefinition type)
		{
			Debug.Assert (type.IsInterface || _context.Annotations.IsMarked (type));

			// The type should be in our cache already
			if (!_typesInDynamicallyAccessedMembersHierarchy.TryGetValue (type, out var existingValue)) {
				// If it's not in the cache it should be a non-interface type in which case it means there were no annotations
				Debug.Assert (!type.IsInterface);
				return (DynamicallyAccessedMemberTypes.None, false);
			}

			return existingValue;
		}
	}
}
