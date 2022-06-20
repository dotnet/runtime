// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker
{
	/// Tracks dependencies created via DynamicDependencyAttribute in the linker.
	/// This is almost identical to DynamicDependencyAttribute, but it holds a
	/// TypeReference instead of a Type, and it has a reference to the original
	/// CustomAttribute for dependency tracing. It is also a place for helper
	/// methods related to the attribute.
	[System.AttributeUsage (System.AttributeTargets.Constructor | System.AttributeTargets.Field | System.AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	internal sealed class DynamicDependency : Attribute
	{
		public CustomAttribute? OriginalAttribute { get; private set; }
		public DynamicDependency (string memberSignature)
		{
			MemberSignature = memberSignature;
		}

		public DynamicDependency (string memberSignature, TypeReference type)
		{
			MemberSignature = memberSignature;
			Type = type;
		}

		public DynamicDependency (string memberSignature, string typeName, string assemblyName)
		{
			MemberSignature = memberSignature;
			TypeName = typeName;
			AssemblyName = assemblyName;
		}

		public DynamicDependency (DynamicallyAccessedMemberTypes memberTypes, TypeReference type)
		{
			MemberTypes = memberTypes;
			Type = type;
		}

		public DynamicDependency (DynamicallyAccessedMemberTypes memberTypes, string typeName, string assemblyName)
		{
			MemberTypes = memberTypes;
			TypeName = typeName;
			AssemblyName = assemblyName;
		}

		public string? MemberSignature { get; }

		public DynamicallyAccessedMemberTypes MemberTypes { get; }

		public TypeReference? Type { get; }

		public string? TypeName { get; }

		public string? AssemblyName { get; }

		public string? Condition { get; set; }

		public static DynamicDependency? ProcessAttribute (LinkContext context, ICustomAttributeProvider provider, CustomAttribute customAttribute)
		{
			if (!(provider is IMemberDefinition member))
				return null;

			if (!(member is MethodDefinition || member is FieldDefinition))
				return null;

			// Don't honor the Condition until we have figured out the behavior for DynamicDependencyAttribute:
			// https://github.com/dotnet/linker/issues/1231
			// if (!ShouldProcess (context, customAttribute))
			// 	return null;

			var dynamicDependency = GetDynamicDependency (customAttribute);
			if (dynamicDependency != null)
				return dynamicDependency;

			context.LogWarning (member, DiagnosticId.DynamicDependencyAttributeCouldNotBeAnalyzed);
			return null;
		}

		static DynamicDependency? GetDynamicDependency (CustomAttribute ca)
		{
			var args = ca.ConstructorArguments;
			if (args.Count < 1 || args.Count > 3)
				return null;

			DynamicDependency? result = args[0].Value switch {
				string stringMemberSignature => args.Count switch {
					1 => new DynamicDependency (stringMemberSignature),
					2 when args[1].Value is TypeReference type => new DynamicDependency (stringMemberSignature, type),
					3 when args[1].Value is string typeName && args[2].Value is string assemblyName => new DynamicDependency (stringMemberSignature, typeName, assemblyName),
					_ => null,
				},
				int memberTypes => args.Count switch {
					2 when args[1].Value is TypeReference type => new DynamicDependency ((DynamicallyAccessedMemberTypes) memberTypes, type),
					3 when args[1].Value is string typeName && args[2].Value is string assemblyName => new DynamicDependency ((DynamicallyAccessedMemberTypes) memberTypes, typeName, assemblyName),
					_ => null,
				},
				_ => null,
			};

			if (result != null)
				result.OriginalAttribute = ca;

			return result;
		}

		public static bool ShouldProcess (LinkContext context, CustomAttribute ca)
		{
			if (ca.HasProperties && ca.Properties[0].Name == "Condition") {
				var condition = ca.Properties[0].Argument.Value as string;
				switch (condition) {
				case "":
				case null:
					return true;
				case "DEBUG":
					if (!context.KeepMembersForDebugger)
						return false;

					break;
				default:
					// Don't have yet a way to match the general condition so everything is excluded
					return false;
				}
			}
			return true;
		}
	}
}