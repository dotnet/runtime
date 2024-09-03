// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker.Dataflow
{
	public readonly struct ReflectionMarker
	{
		readonly LinkContext _context;
		readonly MarkStep _markStep;
		readonly bool _enabled;

		public ReflectionMarker (LinkContext context, MarkStep markStep, bool enabled)
		{
			_context = context;
			_markStep = markStep;
			_enabled = enabled;
		}

		internal void MarkTypeForDynamicallyAccessedMembers (in MessageOrigin origin, TypeReference type, DynamicallyAccessedMemberTypes requiredMemberTypes, DependencyKind dependencyKind, bool declaredOnly = false)
		{
			if (!_enabled)
				return;

			if (type.ResolveToTypeDefinition (_context) is not TypeDefinition typeDefinition)
				return;

			foreach (var member in typeDefinition.GetDynamicallyAccessedMembers (_context, requiredMemberTypes, declaredOnly)) {
				switch (member) {
				case MethodDefinition method:
					MarkMethod (origin, method, dependencyKind);
					break;
				case FieldDefinition field:
					MarkField (origin, field, dependencyKind);
					break;
				case TypeDefinition nestedType:
					MarkType (origin, nestedType, dependencyKind);
					break;
				case PropertyDefinition property:
					MarkProperty (origin, property, dependencyKind);
					break;
				case EventDefinition @event:
					MarkEvent (origin, @event, dependencyKind);
					break;
				case InterfaceImplementation interfaceImplementation:
					MarkInterfaceImplementation (origin, interfaceImplementation, dependencyKind);
					break;
				}
			}
		}

		// Resolve a (potentially assembly qualified) type name based on the current context (taken from DiagnosticContext) and mark the type for reflection.
		// This method will probe the current context assembly and if that fails CoreLib for the specified type. Emulates behavior of Type.GetType.
		internal bool TryResolveTypeNameAndMark (string typeName, in DiagnosticContext diagnosticContext, bool needsAssemblyName, [NotNullWhen (true)] out TypeReference? type)
		{
			if (!_context.TypeNameResolver.TryResolveTypeName (typeName, diagnosticContext, out type, out var typeResolutionRecords, needsAssemblyName)) {
				type = default;
				return false;
			}

			MarkType (diagnosticContext, type, typeResolutionRecords);
			return true;
		}

		// Resolve a type from the specified assembly and mark it for reflection.
		internal bool TryResolveTypeNameAndMark (AssemblyDefinition assembly, string typeName, in DiagnosticContext diagnosticContext, [NotNullWhen (true)] out TypeReference? type)
		{
			if (!_context.TypeNameResolver.TryResolveTypeName (assembly, typeName, out type, out var typeResolutionRecords)) {
				type = default;
				return false;
			}

			MarkType (diagnosticContext, type, typeResolutionRecords);
			return true;
		}

		void MarkType (
			in DiagnosticContext diagnosticContext,
			TypeReference typeReference,
			List<TypeNameResolver.TypeResolutionRecord> typeResolutionRecords)
		{
			if (_enabled) {
				// Mark the resolved type for reflection access, but also go over all types which were resolved in the process
				// of resolving the outer type (typically generic arguments) and make sure we mark all type forwarders
				// used for that resolution.
				// This is necessary because if the app's code contains the input string as literal (which is pretty much always the case)
				// that string has to work at runtime, and if it relies on type forwarders we need to preserve those as well.
				var origin = diagnosticContext.Origin;
				_markStep.MarkTypeVisibleToReflection (typeReference, new DependencyInfo (DependencyKind.AccessedViaReflection, origin.Provider), origin);
				foreach (var typeResolutionRecord in typeResolutionRecords) {
					_context.MarkingHelpers.MarkMatchingExportedType (typeResolutionRecord.ResolvedType, typeResolutionRecord.ReferringAssembly, new DependencyInfo (DependencyKind.DynamicallyAccessedMember, typeReference), origin);
				}
			}
		}

		internal void MarkType (in MessageOrigin origin, TypeReference typeRef, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			if (!_enabled)
				return;

			if (typeRef.ResolveToTypeDefinition (_context) is not TypeDefinition type)
				return;

			_markStep.MarkTypeVisibleToReflection (type, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		internal void MarkMethod (in MessageOrigin origin, MethodReference methodRef, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			if (!_enabled)
				return;

			if (_context.TryResolve (methodRef) is not MethodDefinition method)
				return;

			_markStep.MarkMethodVisibleToReflection (method, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		void MarkField (in MessageOrigin origin, FieldDefinition field, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			if (!_enabled)
				return;

			_markStep.MarkFieldVisibleToReflection (field, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		internal void MarkProperty (in MessageOrigin origin, PropertyDefinition property, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			if (!_enabled)
				return;

			_markStep.MarkPropertyVisibleToReflection (property, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		void MarkEvent (in MessageOrigin origin, EventDefinition @event, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			if (!_enabled)
				return;

			_markStep.MarkEventVisibleToReflection (@event, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		void MarkInterfaceImplementation (in MessageOrigin origin, InterfaceImplementation interfaceImplementation, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			if (!_enabled)
				return;

			_markStep.MarkInterfaceImplementation (interfaceImplementation, origin, new DependencyInfo (dependencyKind, origin.Provider));
		}

		internal void MarkConstructorsOnType (in MessageOrigin origin, TypeReference typeRef, Func<MethodDefinition, bool>? filter, BindingFlags? bindingFlags = null)
		{
			if (!_enabled)
				return;

			if (typeRef.ResolveToTypeDefinition (_context) is not TypeDefinition type)
				return;

			foreach (var ctor in type.GetConstructorsOnType (filter, bindingFlags))
				MarkMethod (origin, ctor);
		}

		internal void MarkFieldsOnTypeHierarchy (in MessageOrigin origin, TypeReference typeRef, Func<FieldDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			if (!_enabled)
				return;

			if (typeRef.ResolveToTypeDefinition (_context) is not TypeDefinition type)
				return;

			foreach (var field in type.GetFieldsOnTypeHierarchy (_context, filter, bindingFlags))
				MarkField (origin, field);
		}

		internal void MarkPropertiesOnTypeHierarchy (in MessageOrigin origin, TypeReference typeRef, Func<PropertyDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			if (!_enabled)
				return;

			if (typeRef.ResolveToTypeDefinition (_context) is not TypeDefinition type)
				return;

			foreach (var property in type.GetPropertiesOnTypeHierarchy (_context, filter, bindingFlags))
				MarkProperty (origin, property);
		}

		internal void MarkEventsOnTypeHierarchy (in MessageOrigin origin, TypeReference typeRef, Func<EventDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			if (!_enabled)
				return;

			if (typeRef.ResolveToTypeDefinition (_context) is not TypeDefinition type)
				return;

			foreach (var @event in type.GetEventsOnTypeHierarchy (_context, filter, bindingFlags))
				MarkEvent (origin, @event);
		}

		internal void MarkStaticConstructor (in MessageOrigin origin, TypeReference typeRef)
		{
			if (!_enabled)
				return;

			if (typeRef.ResolveToTypeDefinition (_context) is not TypeDefinition type)
				return;

			_markStep.MarkStaticConstructorVisibleToReflection (type, new DependencyInfo (DependencyKind.AccessedViaReflection, origin.Provider), origin);
		}
	}
}
