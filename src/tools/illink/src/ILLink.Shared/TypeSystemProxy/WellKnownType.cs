// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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