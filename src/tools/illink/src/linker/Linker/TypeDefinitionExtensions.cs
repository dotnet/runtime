// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker
{
	public static class TypeDefinitionExtensions
	{
		public static TypeReference GetEnumUnderlyingType (this TypeDefinition enumType)
		{
			foreach (var field in enumType.Fields) {
				if (!field.IsStatic) {
					return field.FieldType;
				}
			}

			throw new MissingFieldException ($"Enum type '{enumType.FullName}' is missing instance field");
		}

		public static bool IsMulticastDelegate (this TypeDefinition td)
		{
			return td.BaseType?.Name == "MulticastDelegate" && td.BaseType.Namespace == "System";
		}

		public static bool IsSerializable (this TypeDefinition td)
		{
			return (td.Attributes & TypeAttributes.Serializable) != 0;
		}
	}
}
