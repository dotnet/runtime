// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
