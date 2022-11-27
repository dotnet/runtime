// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	internal static class DynamicallyAccessedMembersBinder
	{
		// Returns the members of the type bound by memberTypes. For DynamicallyAccessedMemberTypes.All, this returns all members of the type and its
		// nested types, including interface implementations, plus the same or any base types or implemented interfaces.
		// DynamicallyAccessedMemberTypes.PublicNestedTypes and NonPublicNestedTypes do the same for members of the selected nested types.
		public static IEnumerable<ISymbol> GetDynamicallyAccessedMembers (this ITypeSymbol typeDefinition, DynamicallyAccessedMemberTypes memberTypes, bool declaredOnly = false)
		{
			if (memberTypes == DynamicallyAccessedMemberTypes.None)
				yield break;

			if (memberTypes == DynamicallyAccessedMemberTypes.All) {
				var members = new List<ISymbol> ();
				typeDefinition.GetAllOnType (declaredOnly, members);
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
				foreach (var c in typeDefinition.GetConstructorsOnType (filter: m => (m.DeclaredAccessibility == Accessibility.Public) && m.Parameters.Length == 0))
					yield return c;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicMethods)) {
				foreach (var m in typeDefinition.GetMethodsOnTypeHierarchy (filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
					yield return m;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicMethods)) {
				foreach (var m in typeDefinition.GetMethodsOnTypeHierarchy (filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
					yield return m;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicFields)) {
				foreach (var f in typeDefinition.GetFieldsOnTypeHierarchy (filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
					yield return f;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicFields)) {
				foreach (var f in typeDefinition.GetFieldsOnTypeHierarchy (filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
					yield return f;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicNestedTypes)) {
				foreach (var nested in typeDefinition.GetNestedTypesOnType (filter: null, bindingFlags: BindingFlags.NonPublic)) {
					yield return nested;
					var members = new List<ISymbol> ();
					nested.GetAllOnType (declaredOnly: false, members);
					foreach (var m in members)
						yield return m;
				}
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicNestedTypes)) {
				foreach (var nested in typeDefinition.GetNestedTypesOnType (filter: null, bindingFlags: BindingFlags.Public)) {
					yield return nested;
					var members = new List<ISymbol> ();
					nested.GetAllOnType (declaredOnly: false, members);
					foreach (var m in members)
						yield return m;
				}
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicProperties)) {
				foreach (var p in typeDefinition.GetPropertiesOnTypeHierarchy (filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
					yield return p;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicProperties)) {
				foreach (var p in typeDefinition.GetPropertiesOnTypeHierarchy (filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
					yield return p;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicEvents)) {
				foreach (var e in typeDefinition.GetEventsOnTypeHierarchy (filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
					yield return e;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicEvents)) {
				foreach (var e in typeDefinition.GetEventsOnTypeHierarchy (filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
					yield return e;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.Interfaces)) {
				foreach (var i in typeDefinition.GetAllInterfaceImplementations (declaredOnly))
					yield return i;
			}
		}
		public static IEnumerable<IMethodSymbol> GetConstructorsOnType (this ITypeSymbol type, Func<IMethodSymbol, bool>? filter, BindingFlags? bindingFlags = null)
		{
			foreach (var method in type.GetMembers ().OfType<IMethodSymbol> ()) {

				if (method.MethodKind != MethodKind.Constructor)
					continue;

				if (filter != null && !filter (method))
					continue;

				if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !method.IsStatic)
					continue;

				if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && method.IsStatic)
					continue;

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && method.DeclaredAccessibility != Accessibility.Public)
					continue;

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && method.DeclaredAccessibility == Accessibility.Public)
					continue;

				yield return method;
			}
		}

		public static IEnumerable<IMethodSymbol> GetMethodsOnTypeHierarchy (this ITypeSymbol thisType, Func<IMethodSymbol, bool>? filter, BindingFlags? bindingFlags = null)
		{
			ITypeSymbol? type = thisType;
			bool onBaseType = false;
			while (type != null) {
				foreach (var method in type.GetMembers ().OfType<IMethodSymbol> ()) {
					// Ignore constructors as those are not considered methods from a reflection's point of view
					if (method.MethodKind == MethodKind.Constructor)
						continue;

					// Ignore private methods on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					if (onBaseType && method.DeclaredAccessibility == Accessibility.Private)
						continue;

					// Note that special methods like property getter/setter, event adder/remover will still get through and will be marked.
					// This is intentional as reflection treats these as methods as well.
					if (filter != null && !filter (method))
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !method.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && method.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && method.DeclaredAccessibility != Accessibility.Public)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && method.DeclaredAccessibility == Accessibility.Public)
						continue;

					yield return method;
				}

				if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
					yield break;

				type = type.BaseType;
				onBaseType = true;
			}
		}

		public static IEnumerable<IFieldSymbol> GetFieldsOnTypeHierarchy (this ITypeSymbol thisType, Func<IFieldSymbol, bool>? filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			ITypeSymbol? type = thisType;
			bool onBaseType = false;
			while (type != null) {
				foreach (var field in type.GetMembers ().OfType<IFieldSymbol> ()) {
					// Ignore private fields on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					if (onBaseType && field.DeclaredAccessibility == Accessibility.Private)
						continue;

					// Note that compiler generated fields backing some properties and events will get through here.
					// This is intentional as reflection treats these as fields as well.
					if (filter != null && !filter (field))
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !field.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && field.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && field.DeclaredAccessibility != Accessibility.Public)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && field.DeclaredAccessibility == Accessibility.Public)
						continue;

					yield return field;
				}

				if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
					yield break;

				type = type.BaseType;
				onBaseType = true;
			}
		}

		public static IEnumerable<ITypeSymbol> GetNestedTypesOnType (this ITypeSymbol type, Func<ITypeSymbol, bool>? filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var nestedType in type.GetTypeMembers ().OfType<ITypeSymbol> ()) {
				if (filter != null && !filter (nestedType))
					continue;

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public) {
					if (nestedType.DeclaredAccessibility != Accessibility.Public)
						continue;
				}

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic) {
					if (nestedType.DeclaredAccessibility == Accessibility.Public)
						continue;
				}

				yield return nestedType;
			}
		}

		public static IEnumerable<IPropertySymbol> GetPropertiesOnTypeHierarchy (this ITypeSymbol thisType, Func<IPropertySymbol, bool>? filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			ITypeSymbol? type = thisType;
			bool onBaseType = false;
			while (type != null) {
				foreach (var property in type.GetMembers ().OfType<IPropertySymbol> ()) {
					// Ignore private properties on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					// Note that properties themselves are not actually private, their accessors are
					if (onBaseType &&
						(property.GetMethod == null || property.GetMethod.DeclaredAccessibility == Accessibility.Private) &&
						(property.SetMethod == null || property.SetMethod.DeclaredAccessibility == Accessibility.Private))
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
						if ((property.GetMethod == null || (property.GetMethod.DeclaredAccessibility != Accessibility.Public))
							&& (property.SetMethod == null || (property.SetMethod.DeclaredAccessibility != Accessibility.Public)))
							continue;
					}

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic) {
						if ((property.GetMethod != null) && (property.GetMethod.DeclaredAccessibility == Accessibility.Public)) continue;
						if ((property.SetMethod != null) && (property.SetMethod.DeclaredAccessibility == Accessibility.Public)) continue;
					}

					yield return property;
				}

				if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
					yield break;

				type = type.BaseType;
				onBaseType = true;
			}
		}

		public static IEnumerable<IEventSymbol> GetEventsOnTypeHierarchy (this ITypeSymbol thisType, Func<IEventSymbol, bool>? filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			ITypeSymbol? type = thisType;
			bool onBaseType = false;
			while (type != null) {
				foreach (var @event in type.GetMembers ().OfType<IEventSymbol> ()) {

					// Ignore private properties on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					// Note that properties themselves are not actually private, their accessors are
					if (onBaseType &&
						(@event.AddMethod == null || @event.AddMethod.DeclaredAccessibility == Accessibility.Private) &&
						(@event.RemoveMethod == null || @event.RemoveMethod.DeclaredAccessibility == Accessibility.Private))
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
						if ((@event.AddMethod == null || (@event.AddMethod.DeclaredAccessibility != Accessibility.Public))
							&& (@event.RemoveMethod == null || (@event.RemoveMethod.DeclaredAccessibility != Accessibility.Public)))
							continue;
					}

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic) {
						if ((@event.AddMethod != null) && @event.AddMethod.DeclaredAccessibility == Accessibility.Public) continue;
						if ((@event.RemoveMethod != null) && @event.RemoveMethod.DeclaredAccessibility == Accessibility.Public) continue;
					}

					yield return @event;
				}

				if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
					yield break;

				type = type.BaseType;
				onBaseType = true;
			}
		}

		// declaredOnly will cause this to retrieve interfaces recursively required by the type, but doesn't necessarily
		// include interfaces required by any base types.
		public static IEnumerable<ITypeSymbol> GetAllInterfaceImplementations (this ITypeSymbol thisType, bool declaredOnly)
		{
			ITypeSymbol? type = thisType;
			while (type != null) {
				foreach (var i in type.Interfaces) {
					yield return i;

					ITypeSymbol? interfaceType = i;
					if (interfaceType != null) {
						// declaredOnly here doesn't matter since interfaces don't have base types
						foreach (var innerInterface in interfaceType.GetAllInterfaceImplementations (declaredOnly: true))
							yield return innerInterface;
					}
				}

				if (declaredOnly)
					yield break;

				type = type.BaseType;
			}
		}

		// Can not pass SymbolEqualityComparer to HashSet since the collection type is ITypeSymbol and not ISymbol.
#pragma warning disable RS1024
		// declaredOnly will cause this to retrieve only members of the type, not of its base types. This includes interfaces recursively
		// required by this type (but not members of these interfaces, or interfaces required only by base types).
		public static void GetAllOnType (this ITypeSymbol type, bool declaredOnly, List<ISymbol> members) => GetAllOnType (type, declaredOnly, members, new HashSet<ITypeSymbol> (SymbolEqualityComparer.Default));
#pragma warning restore RS1024

		static void GetAllOnType (ITypeSymbol type, bool declaredOnly, List<ISymbol> members, HashSet<ITypeSymbol> types)
		{
			if (!types.Add (type))
				return;

			foreach (var nestedType in type.GetTypeMembers ().OfType<ITypeSymbol> ()) {
				members.Add (nestedType);
				// Base types and interfaces of nested types are always included.
				GetAllOnType (nestedType, declaredOnly: false, members, types);
			}

			if (!declaredOnly) {
				var baseType = type.BaseType;
				if (baseType != null)
					GetAllOnType (baseType, declaredOnly: false, members, types);
			}

			if (!type.Interfaces.IsEmpty) {
				if (declaredOnly) {
					foreach (var iface in type.GetAllInterfaceImplementations (declaredOnly: true))
						members.Add (iface);
				} else {
					foreach (var iface in type.Interfaces) {
						members.Add (iface);
						var interfaceType = iface;
						if (interfaceType == null)
							continue;
						GetAllOnType (interfaceType, declaredOnly: false, members, types);
					}
				}
			}

			foreach (var member in type.GetMembers ()) {
				switch (member.Kind) {
				case SymbolKind.Field:
				case SymbolKind.Method:
				case SymbolKind.Property:
				case SymbolKind.Event:
					members.Add (member);
					break;
				}
			}
		}
	}
}
