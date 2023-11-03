// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared;
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker.Dataflow
{
	sealed class DynamicallyAccessedMembersTypeHierarchy
	{
		readonly LinkContext _context;
		readonly MarkStep _markStep;

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

		public DynamicallyAccessedMembersTypeHierarchy (LinkContext context, MarkStep markStep)
		{
			_context = context;
			_markStep = markStep;
			_typesInDynamicallyAccessedMembersHierarchy = new Dictionary<TypeDefinition, (DynamicallyAccessedMemberTypes, bool)> ();
		}

		public (DynamicallyAccessedMemberTypes annotation, bool applied) ProcessMarkedTypeForDynamicallyAccessedMembersHierarchy (TypeDefinition type)
		{
			// We'll use the cache also as a way to detect and avoid recursion for interfaces and annotated base types
			if (_typesInDynamicallyAccessedMembersHierarchy.TryGetValue (type, out var existingValue))
				return existingValue;

			DynamicallyAccessedMemberTypes annotation = _context.Annotations.FlowAnnotations.GetTypeAnnotation (type);
			bool apply = false;

			if (type.IsInterface)
				_typesInDynamicallyAccessedMembersHierarchy.Add (type, (annotation, false));

			TypeDefinition? baseType = _context.TryResolve (type.BaseType);
			if (baseType != null) {
				var baseValue = ProcessMarkedTypeForDynamicallyAccessedMembersHierarchy (baseType);
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
					var interfaceType = _context.TryResolve (iface.InterfaceType);
					if (interfaceType != null) {
						var interfaceValue = ProcessMarkedTypeForDynamicallyAccessedMembersHierarchy (interfaceType);
						annotation |= interfaceValue.annotation;
						apply |= interfaceValue.applied;
					}
				}
			}

			Debug.Assert (!apply || annotation != DynamicallyAccessedMemberTypes.None);

			// If OptimizeTypeHierarchyAnnotations is disabled, we will apply the annotations without seeing object.GetType()
			bool applyOptimizeTypeHierarchyAnnotations = (annotation != DynamicallyAccessedMemberTypes.None) && !_context.IsOptimizationEnabled (CodeOptimizations.OptimizeTypeHierarchyAnnotations, type);
			// Unfortunately, we cannot apply the annotation to type derived from EventSource - Revisit after https://github.com/dotnet/runtime/issues/54859
			// Breaking the logic to make it easier to maintain in the future since the logic is convoluted
			// DisableEventSourceSpecialHandling is closely tied to a type derived from EventSource and should always go together
			// However, logically it should be possible to use DisableEventSourceSpecialHandling to allow marking types derived from EventSource when OptimizeTypeHierarchyAnnotations is disabled
			apply |= applyOptimizeTypeHierarchyAnnotations && (_context.DisableEventSourceSpecialHandling || !BCL.EventTracingForWindows.IsEventSourceImplementation (type, _context));

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
				var origin = new MessageOrigin (type);
				var reflectionMarker = new ReflectionMarker (_context, _markStep, enabled: true);
				// Report warnings on access to annotated members, with the annotated type as the origin.
				ApplyDynamicallyAccessedMembersToType (reflectionMarker, origin, type, annotation);
			}

			return (annotation, apply);
		}

		public DynamicallyAccessedMemberTypes ApplyDynamicallyAccessedMembersToTypeHierarchy (TypeDefinition type)
		{
			(var annotation, var applied) = ProcessMarkedTypeForDynamicallyAccessedMembersHierarchy (type);

			// If the annotation was already applied to this type, there's no reason to repeat the operation, the result will
			// be no change.
			if (applied || annotation == DynamicallyAccessedMemberTypes.None)
				return annotation;

			// Apply the effective annotation for the type
			var origin = new MessageOrigin (type);
			var reflectionMarker = new ReflectionMarker (_context, _markStep, enabled: true);
			// Report warnings on access to annotated members, with the annotated type as the origin.
			ApplyDynamicallyAccessedMembersToType (reflectionMarker, origin, type, annotation);

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
					ApplyDynamicallyAccessedMembersToTypeHierarchyInner (reflectionMarker, candidateType);
				}
			}

			return annotation;
		}

		bool ApplyDynamicallyAccessedMembersToTypeHierarchyInner (
			in ReflectionMarker reflectionMarker,
			TypeDefinition type)
		{
			(var annotation, var applied) = GetCachedInfoForTypeInHierarchy (type);

			if (annotation == DynamicallyAccessedMemberTypes.None)
				return false;

			if (applied)
				return true;

			TypeDefinition? baseType = _context.TryResolve (type.BaseType);
			if (baseType != null)
				applied = ApplyDynamicallyAccessedMembersToTypeHierarchyInner (reflectionMarker, baseType);

			if (!applied && type.HasInterfaces) {
				foreach (InterfaceImplementation iface in type.Interfaces) {
					var interfaceType = _context.TryResolve (iface.InterfaceType);
					if (interfaceType != null) {
						if (ApplyDynamicallyAccessedMembersToTypeHierarchyInner (reflectionMarker, interfaceType)) {
							applied = true;
							break;
						}
					}
				}
			}

			if (applied) {
				var origin = new MessageOrigin (type);
				// Report warnings on access to annotated members, with the annotated type as the origin.
				ApplyDynamicallyAccessedMembersToType (reflectionMarker, origin, type, annotation);
				_typesInDynamicallyAccessedMembersHierarchy[type] = (annotation, true);
			}

			return applied;
		}

		void ApplyDynamicallyAccessedMembersToType (in ReflectionMarker reflectionMarker, in MessageOrigin origin, TypeDefinition type, DynamicallyAccessedMemberTypes annotation)
		{
			Debug.Assert (annotation != DynamicallyAccessedMemberTypes.None);

			// We need to apply annotations to this type, and its base/interface types (recursively)
			// But the annotations on base/interfaces are already applied so we don't need to apply those
			// again (and should avoid doing so as it would produce extra warnings).
			var baseType = _context.TryResolve (type.BaseType);
			if (baseType != null) {
				var baseAnnotation = GetCachedInfoForTypeInHierarchy (baseType);
				var annotationToApplyToBase = baseAnnotation.applied ? Annotations.GetMissingMemberTypes (annotation, baseAnnotation.annotation) : annotation;

				// Apply any annotations that didn't exist on the base type to the base type.
				// This may produce redundant warnings when the annotation is DAMT.All or DAMT.PublicConstructors and the base already has a
				// subset of those annotations.
				reflectionMarker.MarkTypeForDynamicallyAccessedMembers (origin, baseType, annotationToApplyToBase, DependencyKind.DynamicallyAccessedMemberOnType, declaredOnly: false);
			}

			// Most of the DynamicallyAccessedMemberTypes don't select members on interfaces. We only need to apply
			// annotations to interfaces separately if dealing with DAMT.All or DAMT.Interfaces.
			if (annotation.HasFlag (DynamicallyAccessedMemberTypes.Interfaces) && type.HasInterfaces) {
				var annotationToApplyToInterfaces = annotation == DynamicallyAccessedMemberTypes.All ? annotation : DynamicallyAccessedMemberTypes.Interfaces;
				foreach (var iface in type.Interfaces) {
					var interfaceType = _context.TryResolve (iface.InterfaceType);
					if (interfaceType == null)
						continue;

					var interfaceAnnotation = GetCachedInfoForTypeInHierarchy (interfaceType);
					if (interfaceAnnotation.applied && interfaceAnnotation.annotation.HasFlag (annotationToApplyToInterfaces))
						continue;

					// Apply All or Interfaces to the interface type.
					// DAMT.All may produce redundant warnings from implementing types, when the interface type already had some annotations.
					reflectionMarker.MarkTypeForDynamicallyAccessedMembers (origin, interfaceType, annotationToApplyToInterfaces, DependencyKind.DynamicallyAccessedMemberOnType, declaredOnly: false);
				}
			}

			// The annotations this type inherited from its base types or interfaces should not produce
			// warnings on the respective base/interface members, since those are already covered by applying
			// the annotations to those types. So we only need to handle the members directly declared on this type.
			reflectionMarker.MarkTypeForDynamicallyAccessedMembers (origin, type, annotation, DependencyKind.DynamicallyAccessedMemberOnType, declaredOnly: true);
		}

		(DynamicallyAccessedMemberTypes annotation, bool applied) GetCachedInfoForTypeInHierarchy (TypeDefinition type)
		{
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
