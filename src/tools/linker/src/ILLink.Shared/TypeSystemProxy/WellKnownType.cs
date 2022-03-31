// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace ILLink.Shared.TypeSystemProxy
{
	public enum WellKnownType
	{
		System_String,
		System_Nullable_T,
		System_Type,
		System_Reflection_IReflect,
		System_Array,
		System_Object,
		System_Attribute
	}

	public static partial class WellKnownTypeExtensions
	{
		public static (string Namespace, string Name) GetNamespaceAndName (this WellKnownType type)
		{
			return type switch {
				WellKnownType.System_String => ("System", "String"),
				WellKnownType.System_Nullable_T => ("System", "Nullable`1"),
				WellKnownType.System_Type => ("System", "Type"),
				WellKnownType.System_Reflection_IReflect => ("System.Reflection", "IReflect"),
				WellKnownType.System_Array => ("System", "Array"),
				WellKnownType.System_Object => ("System", "Object"),
				WellKnownType.System_Attribute => ("System", "Attribute"),
				_ => throw new ArgumentException ($"{nameof (type)} is not a well-known type."),
			};
		}
		public static string GetNamespace (this WellKnownType type) => GetNamespaceAndName (type).Namespace;
		public static string GetName (this WellKnownType type) => GetNamespaceAndName (type).Name;
	}
}
