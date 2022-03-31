// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker
{
	internal static class DynamicallyAccessedMembersBinder
	{
		// Returns the members of the type bound by memberTypes. For DynamicallyAccessedMemberTypes.All, this returns all members of the type and its
		// nested types, including interface implementations, plus the same or any base types or implemented interfaces.
		// DynamicallyAccessedMemberTypes.PublicNestedTypes and NonPublicNestedTypes do the same for members of the selected nested types.
		public static IEnumerable<IMetadataTokenProvider> GetDynamicallyAccessedMembers (this TypeDefinition typeDefinition, LinkContext context, DynamicallyAccessedMemberTypes memberTypes, bool declaredOnly = false)
		{
			if (memberTypes == DynamicallyAccessedMemberTypes.None)
				yield break;

			if (memberTypes == DynamicallyAccessedMemberTypes.All) {
				var members = new List<IMetadataTokenProvider> ();
				typeDefinition.GetAllOnType (context, declaredOnly, members);
				foreach (var m in members)
					yield return m;
				yield break;
			}

			var declaredOnlyFlags = declaredOnly ? BindingFlags.DeclaredOnly : BindingFlags.Default;

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicConstructors)) {
				foreach (var c in typeDefinition.GetConstructorsOnType (filter: null, bindingFlags: BindingFlags.NonPublic))
					yield return c;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors)) {
				foreach (var c in typeDefinition.GetConstructorsOnType (filter: null, bindingFlags: BindingFlags.Public))
					yield return c;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)) {
				foreach (var c in typeDefinition.GetConstructorsOnType (filter: m => m.IsPublic && m.Parameters.Count == 0))
					yield return c;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicMethods)) {
				foreach (var m in typeDefinition.GetMethodsOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
					yield return m;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicMethods)) {
				foreach (var m in typeDefinition.GetMethodsOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
					yield return m;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicFields)) {
				foreach (var f in typeDefinition.GetFieldsOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
					yield return f;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicFields)) {
				foreach (var f in typeDefinition.GetFieldsOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
					yield return f;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicNestedTypes)) {
				foreach (var nested in typeDefinition.GetNestedTypesOnType (filter: null, bindingFlags: BindingFlags.NonPublic)) {
					yield return nested;
					var members = new List<IMetadataTokenProvider> ();
					nested.GetAllOnType (context, declaredOnly: false, members);
					foreach (var m in members)
						yield return m;
				}
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicNestedTypes)) {
				foreach (var nested in typeDefinition.GetNestedTypesOnType (filter: null, bindingFlags: BindingFlags.Public)) {
					yield return nested;
					var members = new List<IMetadataTokenProvider> ();
					nested.GetAllOnType (context, declaredOnly: false, members);
					foreach (var m in members)
						yield return m;
				}
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicProperties)) {
				foreach (var p in typeDefinition.GetPropertiesOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
					yield return p;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicProperties)) {
				foreach (var p in typeDefinition.GetPropertiesOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
					yield return p;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicEvents)) {
				foreach (var e in typeDefinition.GetEventsOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
					yield return e;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicEvents)) {
				foreach (var e in typeDefinition.GetEventsOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
					yield return e;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypesOverlay.Interfaces)) {
				foreach (var i in typeDefinition.GetAllInterfaceImplementations (context, declaredOnly))
					yield return i;
			}
		}

		public static IEnumerable<MethodDefinition> GetConstructorsOnType (this TypeDefinition type, Func<MethodDefinition, bool>? filter, BindingFlags? bindingFlags = null)
		{
			foreach (var method in type.Methods) {
				if (!method.IsConstructor)
					continue;

				if (filter != null && !filter (method))
					continue;

				if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !method.IsStatic)
					continue;

				if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && method.IsStatic)
					continue;

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && !method.IsPublic)
					continue;

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && method.IsPublic)
					continue;

				yield return method;
			}
		}

		public static IEnumerable<MethodDefinition> GetMethodsOnTypeHierarchy (this TypeDefinition thisType, LinkContext context, Func<MethodDefinition, bool>? filter, BindingFlags? bindingFlags = null)
		{
			TypeDefinition? type = thisType;
			bool onBaseType = false;
			while (type != null) {
				foreach (var method in type.Methods) {
					// Ignore constructors as those are not considered methods from a reflection's point of view
					if (method.IsConstructor)
						continue;

					// Ignore private methods on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					if (onBaseType && method.IsPrivate)
						continue;

					// Note that special methods like property getter/setter, event adder/remover will still get through and will be marked.
					// This is intentional as reflection treats these as methods as well.

					if (filter != null && !filter (method))
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !method.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && method.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && !method.IsPublic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && method.IsPublic)
						continue;

					yield return method;
				}

				if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
					yield break;

				type = context.TryResolve (type.BaseType);
				onBaseType = true;
			}
		}

		public static IEnumerable<FieldDefinition> GetFieldsOnTypeHierarchy (this TypeDefinition thisType, LinkContext context, Func<FieldDefinition, bool>? filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			TypeDefinition? type = thisType;
			bool onBaseType = false;
			while (type != null) {
				foreach (var field in type.Fields) {
					// Ignore private fields on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					if (onBaseType && field.IsPrivate)
						continue;

					// Note that compiler generated fields backing some properties and events will get through here.
					// This is intentional as reflection treats these as fields as well.

					if (filter != null && !filter (field))
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !field.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && field.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && !field.IsPublic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && field.IsPublic)
						continue;

					yield return field;
				}

				if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
					yield break;

				type = context.TryResolve (type.BaseType);
				onBaseType = true;
			}
		}

		public static IEnumerable<TypeDefinition> GetNestedTypesOnType (this TypeDefinition type, Func<TypeDefinition, bool>? filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var nestedType in type.NestedTypes) {
				if (filter != null && !filter (nestedType))
					continue;

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public) {
					if (!nestedType.IsNestedPublic)
						continue;
				}

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic) {
					if (nestedType.IsNestedPublic)
						continue;
				}

				yield return nestedType;
			}
		}

		public static IEnumerable<PropertyDefinition> GetPropertiesOnTypeHierarchy (this TypeDefinition thisType, LinkContext context, Func<PropertyDefinition, bool>? filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			TypeDefinition? type = thisType;
			bool onBaseType = false;
			while (type != null) {
				foreach (var property in type.Properties) {
					// Ignore private properties on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					// Note that properties themselves are not actually private, their accessors are
					if (onBaseType &&
						(property.GetMethod == null || property.GetMethod.IsPrivate) &&
						(property.SetMethod == null || property.SetMethod.IsPrivate))
						continue;

					if (filter != null && !filter (property))
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static) {
						if ((property.GetMethod != null) && !property.GetMethod.IsStatic) continue;
						if ((property.SetMethod != null) && !property.SetMethod.IsStatic) continue;
					}

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance) {
						if ((property.GetMethod != null) && property.GetMethod.IsStatic) continue;
						if ((property.SetMethod != null) && property.SetMethod.IsStatic) continue;
					}

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public) {
						if ((property.GetMethod == null || !property.GetMethod.IsPublic)
							&& (property.SetMethod == null || !property.SetMethod.IsPublic))
							continue;
					}

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic) {
						if ((property.GetMethod != null) && property.GetMethod.IsPublic) continue;
						if ((property.SetMethod != null) && property.SetMethod.IsPublic) continue;
					}

					yield return property;
				}

				if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
					yield break;

				type = context.TryResolve (type.BaseType);
				onBaseType = true;
			}
		}

		public static IEnumerable<EventDefinition> GetEventsOnTypeHierarchy (this TypeDefinition thisType, LinkContext context, Func<EventDefinition, bool>? filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			TypeDefinition? type = thisType;
			bool onBaseType = false;
			while (type != null) {
				foreach (var @event in type.Events) {
					// Ignore private properties on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					// Note that properties themselves are not actually private, their accessors are
					if (onBaseType &&
						(@event.AddMethod == null || @event.AddMethod.IsPrivate) &&
						(@event.RemoveMethod == null || @event.RemoveMethod.IsPrivate))
						continue;

					if (filter != null && !filter (@event))
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static) {
						if ((@event.AddMethod != null) && !@event.AddMethod.IsStatic) continue;
						if ((@event.RemoveMethod != null) && !@event.RemoveMethod.IsStatic) continue;
					}

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance) {
						if ((@event.AddMethod != null) && @event.AddMethod.IsStatic) continue;
						if ((@event.RemoveMethod != null) && @event.RemoveMethod.IsStatic) continue;
					}

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public) {
						if ((@event.AddMethod == null || !@event.AddMethod.IsPublic)
							&& (@event.RemoveMethod == null || !@event.RemoveMethod.IsPublic))
							continue;
					}

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic) {
						if ((@event.AddMethod != null) && @event.AddMethod.IsPublic) continue;
						if ((@event.RemoveMethod != null) && @event.RemoveMethod.IsPublic) continue;
					}

					yield return @event;
				}

				if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
					yield break;

				type = context.TryResolve (type.BaseType);
				onBaseType = true;
			}
		}

		// declaredOnly will cause this to retrieve interfaces recursively required by the type, but doesn't necessarily
		// include interfaces required by any base types.
		public static IEnumerable<InterfaceImplementation> GetAllInterfaceImplementations (this TypeDefinition thisType, LinkContext context, bool declaredOnly)
		{
			TypeDefinition? type = thisType;
			while (type != null) {
				foreach (var i in type.Interfaces) {
					yield return i;

					TypeDefinition? interfaceType = context.TryResolve (i.InterfaceType);
					if (interfaceType != null) {
						// declaredOnly here doesn't matter since interfaces don't have base types
						foreach (var innerInterface in interfaceType.GetAllInterfaceImplementations (context, declaredOnly: true))
							yield return innerInterface;
					}
				}

				if (declaredOnly)
					yield break;

				type = context.TryResolve (type.BaseType);
			}
		}

		// declaredOnly will cause this to retrieve only members of the type, not of its base types. This includes interfaces recursively
		// required by this type (but not members of these interfaces, or interfaces required only by base types).
		public static void GetAllOnType (this TypeDefinition type, LinkContext context, bool declaredOnly, List<IMetadataTokenProvider> members) => GetAllOnType (type, context, declaredOnly, members, new HashSet<TypeDefinition> ());

		static void GetAllOnType (TypeDefinition type, LinkContext context, bool declaredOnly, List<IMetadataTokenProvider> members, HashSet<TypeDefinition> types)
		{
			if (!types.Add (type))
				return;

			if (type.HasNestedTypes) {
				foreach (var nested in type.NestedTypes) {
					members.Add (nested);
					// Base types and interfaces of nested types are always included.
					GetAllOnType (nested, context, declaredOnly: false, members, types);
				}
			}

			if (!declaredOnly) {
				var baseType = context.TryResolve (type.BaseType);
				if (baseType != null)
					GetAllOnType (baseType, context, declaredOnly: false, members, types);
			}

			if (type.HasInterfaces) {
				if (declaredOnly) {
					foreach (var iface in type.GetAllInterfaceImplementations (context, declaredOnly: true))
						members.Add (iface);
				} else {
					foreach (var iface in type.Interfaces) {
						members.Add (iface);
						var interfaceType = context.TryResolve (iface.InterfaceType);
						if (interfaceType == null)
							continue;
						GetAllOnType (interfaceType, context, declaredOnly: false, members, types);
					}
				}
			}

			if (type.HasFields) {
				foreach (var f in type.Fields)
					members.Add (f);
			}

			if (type.HasMethods) {
				foreach (var m in type.Methods)
					members.Add (m);
			}

			if (type.HasProperties) {
				foreach (var p in type.Properties)
					members.Add (p);
			}

			if (type.HasEvents) {
				foreach (var e in type.Events)
					members.Add (e);
			}
		}
	}
}
