// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker
{
	static class FieldDefinitionExtensions
	{
		public static bool IsCompilerGenerated (this FieldDefinition field)
		{
			if (!field.HasCustomAttributes)
				return false;

			foreach (var ca in field.CustomAttributes) {
				var caType = ca.AttributeType;
				if (caType.Name == "CompilerGeneratedAttribute" && caType.Namespace == "System.Runtime.CompilerServices")
					return true;
			}

			return false;
		}
	}
}
