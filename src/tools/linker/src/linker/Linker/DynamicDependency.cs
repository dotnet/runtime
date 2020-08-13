using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

#nullable enable

namespace Mono.Linker
{
	/// Tracks dependencies created via DynamicDependencyAttribute in the linker.
	/// This is almost identical to DynamicDependencyAttribute, but it holds a
	/// TypeReference instead of a Type, and it has a reference to the original
	/// CustomAttribute for dependency tracing. It is also a place for helper
	/// methods related to the attribute.
	internal class DynamicDependency : Attribute
	{
		public CustomAttribute? OriginalAttribute { get; set; }
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
			// https://github.com/mono/linker/issues/1231
			// if (!ShouldProcess (context, customAttribute))
			// 	return null;

			var dynamicDependency = GetDynamicDependency (context, customAttribute);
			if (dynamicDependency != null)
				return dynamicDependency;

			context.LogWarning ($"The 'DynamicDependencyAttribute' could not be analyzed", 2034, member);
			return null;
		}

		static DynamicDependency? GetDynamicDependency (LinkContext context, CustomAttribute ca)
		{
			var args = ca.ConstructorArguments;
			if (args.Count < 1 || args.Count > 3)
				return null;

			// First argument is string or DynamicallyAccessedMemberTypes
			string? memberSignature = args[0].Value as string;
			if (args.Count == 1)
				return memberSignature == null ? null : new DynamicDependency (memberSignature);
			DynamicallyAccessedMemberTypes? memberTypes = null;
			if (memberSignature == null) {
				var argType = args[0].Type;
				if (!argType.IsTypeOf<DynamicallyAccessedMemberTypes> ())
					return null;
				try {
					memberTypes = (DynamicallyAccessedMemberTypes) args[0].Value;
				} catch (InvalidCastException) { }
				if (memberTypes == null)
					return null;
			}

			// Second argument is Type for ctors with two args, string for ctors with three args
			if (args.Count == 2) {
				if (!(args[1].Value is TypeReference type))
					return null;
				return memberSignature == null ? new DynamicDependency (memberTypes!.Value, type) : new DynamicDependency (memberSignature, type);
			}
			Debug.Assert (args.Count == 3);
			if (!(args[1].Value is string typeName))
				return null;

			// Third argument is assembly name
			if (!(args[2].Value is string assemblyName))
				return null;

			var dynamicDependency = memberSignature == null ? new DynamicDependency (memberTypes!.Value, typeName, assemblyName) : new DynamicDependency (memberSignature, typeName, assemblyName);
			dynamicDependency.OriginalAttribute = ca;

			return dynamicDependency;
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