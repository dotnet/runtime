// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker
{
	// This recursively marks members of types for serialization. This is not supposed to be complete; it is just
	// here as a heuristic to enable some serialization scenarios.
	//
	// Xamarin-android had some heuristics for serialization which behaved as follows:
	//
	// Discover members in the "link" assemblies with certain attributes:
	//   for XMLSerializer: Xml*Attribute, except XmlIgnoreAttribute
	//   for DataContractSerializer: DataContractAttribute or DataMemberAttribute
	// These members are considered "roots" for serialization.
	//
	// For each "root":
	//    in an SDK assembly, set TypePreserve.All for types, or conditionally preserve methods or property methods
	//      event methods were not preserved.
	//    in a non-SDK assembly, mark types, fields, methods, property methods, and event methods
	//    recursively scan types of properties and fields (including generic arguments)
	// For each recursive type:
	//   conditionally preserve the default ctor
	//
	// We want to match the above behavior in a more correct way, even if this means not marking some members
	// which used to be marked. We also would like to avoid serializer-specific logic in the recursive marking.
	//
	// Instead of conditionally preserving things, we will just mark them, and we will do so consistently for every
	// type discovered as part of the type graph reachable from the discovered roots. We also do not distinguish between
	// SDK and non-SDK assemblies.
	//
	// The behavior is as follows:
	//
	// Discover attributed "roots" by looking for the same attributes, but only on marked types.
	//
	// For each "root":
	//   recursively scan types of public properties and fields, and the base type (including generic arguments)
	// For each recursive type:
	//   mark the type and its public instance {fields, properties, and parameterless constructors}

	[Flags]
	public enum SerializerKind
	{
		None = 0,
		XmlSerializer = 1,
		DataContractSerializer = 2,
	}

	public class SerializationMarker
	{
		readonly LinkContext _context;

		SerializerKind ActiveSerializers { get; set; }

		Dictionary<SerializerKind, HashSet<ICustomAttributeProvider>>? _trackedRoots;
		Dictionary<SerializerKind, HashSet<ICustomAttributeProvider>> TrackedRoots {
			get {
				_trackedRoots ??= new Dictionary<SerializerKind, HashSet<ICustomAttributeProvider>> ();

				return _trackedRoots;
			}
		}

		HashSet<TypeDefinition>? _recursiveTypes;
		HashSet<TypeDefinition> RecursiveTypes {
			get {
				_recursiveTypes ??= new HashSet<TypeDefinition> ();

				return _recursiveTypes;
			}
		}

		public SerializationMarker (LinkContext context)
		{
			_context = context;
		}

		public bool IsActive (SerializerKind serializerKind) => ActiveSerializers.HasFlag (serializerKind);

		static DependencyKind ToDependencyKind (SerializerKind serializerKind) => serializerKind switch {
			SerializerKind.DataContractSerializer => DependencyKind.DataContractSerialized,
			SerializerKind.XmlSerializer => DependencyKind.XmlSerialized,
			_ => throw new ArgumentException (nameof (SerializerKind))
		};

		public void TrackForSerialization (ICustomAttributeProvider provider, SerializerKind serializerKind)
		{
			if (ActiveSerializers.HasFlag (serializerKind)) {
				MarkRecursiveMembers (provider, serializerKind);
				return;
			}

			if (!TrackedRoots.TryGetValue (serializerKind, out var roots)) {
				roots = new HashSet<ICustomAttributeProvider> ();
				TrackedRoots.Add (serializerKind, roots);
			}

			roots.Add (provider);
		}

		public void Activate (SerializerKind serializerKind)
		{
			if (!Enum.IsDefined<SerializerKind> (serializerKind) || serializerKind == SerializerKind.None)
				throw new ArgumentException ($"Unexpected serializer kind {nameof (serializerKind)}");

			if (ActiveSerializers.HasFlag (serializerKind))
				return;

			ActiveSerializers |= serializerKind;

			if (!TrackedRoots.TryGetValue (serializerKind, out var roots))
				return;

			foreach (var provider in roots)
				MarkRecursiveMembers (provider, serializerKind);

			TrackedRoots.Remove (serializerKind);
		}

		public void MarkRecursiveMembers (ICustomAttributeProvider provider, SerializerKind serializerKind)
		{
			TypeDefinition type;
			var reason = new DependencyInfo (ToDependencyKind (serializerKind), provider);
			var origin = new MessageOrigin (provider);

			// Mark field and property types up-front in case the root field/property is
			// not discovered recursively from the declaring type (for example, it may be private).
			// Also mark the root members because the recursive logic doesn't mark all member types.
			switch (provider) {
			case TypeDefinition td:
				type = td;
				break;
			case FieldDefinition field:
				type = field.DeclaringType;
				MarkRecursiveMembersInternal (field.FieldType, reason);
				_context.Annotations.Mark (field, reason, origin);
				break;
			case PropertyDefinition property:
				type = property.DeclaringType;
				MarkRecursiveMembersInternal (property.PropertyType, reason);
				if (property.GetMethod != null)
					_context.Annotations.Mark (property.GetMethod, reason, origin);
				if (property.SetMethod != null)
					_context.Annotations.Mark (property.SetMethod, reason, origin);
				break;
			case MethodDefinition method:
				type = method.DeclaringType;
				_context.Annotations.Mark (method, reason, origin);
				break;
			case EventDefinition @event:
				type = @event.DeclaringType;
				if (@event.AddMethod != null)
					_context.Annotations.Mark (@event.AddMethod, reason, origin);
				if (@event.InvokeMethod != null)
					_context.Annotations.Mark (@event.InvokeMethod, reason, origin);
				if (@event.RemoveMethod != null)
					_context.Annotations.Mark (@event.RemoveMethod, reason, origin);
				break;
			default:
				throw new ArgumentException ($"{nameof (provider)} has invalid provider type {provider.GetType ()}");
			}

			MarkRecursiveMembersInternal (type, reason);
		}

		void MarkRecursiveMembersInternal (TypeReference typeRef, in DependencyInfo reason)
		{
			if (typeRef == null)
				return;

			DependencyInfo typeReason = reason;
			while (typeRef is GenericInstanceType git) {
				if (git.HasGenericArguments) {
					foreach (var argType in git.GenericArguments)
						MarkRecursiveMembersInternal (argType, new DependencyInfo (DependencyKind.GenericArgumentType, typeRef));
				}
				_context.Tracer.AddDirectDependency (typeRef, typeReason, marked: false);
				typeReason = new DependencyInfo (DependencyKind.ElementType, typeRef);
				typeRef = git.ElementType;
			}
			// This doesn't handle other TypeSpecs. We are only matching what xamarin-android used to do.
			// Arrays will still work because Resolve returns the array element type.

			TypeDefinition? type = _context.TryResolve (typeRef);
			if (type == null)
				return;

			_context.Annotations.Mark (type, typeReason, new MessageOrigin (reason.Source as ICustomAttributeProvider));

			if (!RecursiveTypes.Add (type))
				return;

			// Unlike xamarin-android, don't preserve all members.

			// Unlike xamarin-android, we preserve base type members recursively.
			MarkRecursiveMembersInternal (type.BaseType, new DependencyInfo (DependencyKind.SerializedRecursiveType, type));

			if (type.HasFields) {
				foreach (var field in type.Fields) {
					// Unlike xamarin-android, don't preserve non-public or static fields.
					if (!field.IsPublic || field.IsStatic)
						continue;

					MarkRecursiveMembersInternal (field.FieldType, new DependencyInfo (DependencyKind.SerializedRecursiveType, type));
					_context.Annotations.Mark (field, new DependencyInfo (DependencyKind.SerializedMember, type), new MessageOrigin (type));
				}
			}

			if (type.HasProperties) {
				foreach (var property in type.Properties) {
					// Unlike xamarin-android, don't preserve non-public or static properties.
					var get = property.GetMethod;
					var set = property.SetMethod;
					if ((get == null || !get.IsPublic || get.IsStatic) &&
						(set == null || !set.IsPublic || set.IsStatic))
						continue;

					MarkRecursiveMembersInternal (property.PropertyType, new DependencyInfo (DependencyKind.SerializedRecursiveType, type));
					if (get != null)
						_context.Annotations.Mark (get, new DependencyInfo (DependencyKind.SerializedMember, type), new MessageOrigin (type));
					if (set != null)
						_context.Annotations.Mark (set, new DependencyInfo (DependencyKind.SerializedMember, type), new MessageOrigin (type));
					// The property will be marked as a consequence of marking the getter/setter.
				}
			}

			if (type.HasMethods) {
				foreach (var method in type.Methods) {
					// Unlike xamarin-android, don't preserve non-public, static, or parameterless constructors.
					if (!method.IsPublic || !method.IsDefaultConstructor ())
						continue;

					_context.Annotations.Mark (method, new DependencyInfo (DependencyKind.SerializedMember, type), new MessageOrigin (type));
				}
			}
		}
	}
}
