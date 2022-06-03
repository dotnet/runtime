// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using StaticCs;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TypeSystemProxy
{
	[Closed]
	public enum WellKnownType
	{
		System_String,
		System_Nullable_T,
		System_Type,
		System_Reflection_IReflect,
		System_Array,
		System_Object,
		System_Attribute,
		System_NotSupportedException,
		System_Runtime_CompilerServices_DisablePrivateReflectionAttribute,
		System_Void
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
				WellKnownType.System_NotSupportedException => ("System", "NotSupportedException"),
				WellKnownType.System_Runtime_CompilerServices_DisablePrivateReflectionAttribute => ("System.Runtime.CompilerServices", "DisablePrivateReflectionAttribute"),
				WellKnownType.System_Void => ("System", "Void"),
			};
		}
		public static string GetNamespace (this WellKnownType type) => GetNamespaceAndName (type).Namespace;
		public static string GetName (this WellKnownType type) => GetNamespaceAndName (type).Name;
		public static WellKnownType? GetWellKnownType (string @namespace, string name)
		{
			return @namespace switch {
				"System" => name switch {
					"String" => WellKnownType.System_String,
					"Nullable`1" => WellKnownType.System_Nullable_T,
					"Type" => WellKnownType.System_Type,
					"Array" => WellKnownType.System_Array,
					"Attribute" => WellKnownType.System_Attribute,
					"Object" => WellKnownType.System_Object,
					"NotSupportedException" => WellKnownType.System_NotSupportedException,
					"Void" => WellKnownType.System_Void,
					_ => null
				},
				"System.Reflection" => name switch {
					"IReflect" => WellKnownType.System_Reflection_IReflect,
					_ => null
				},
				"System.Runtime.CompilerServices" => name switch {
					"DisablePrivateReflectionAttribute" => WellKnownType.System_Runtime_CompilerServices_DisablePrivateReflectionAttribute,
					_ => null
				},
				_ => null
			};
		}
	}
}
