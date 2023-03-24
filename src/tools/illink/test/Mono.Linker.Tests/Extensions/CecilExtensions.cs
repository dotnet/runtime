// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Mono.Linker.Tests.Extensions
{
	public static class CecilExtensions
	{
		public static IEnumerable<TypeDefinition> AllDefinedTypes (this AssemblyDefinition assemblyDefinition)
		{
			return assemblyDefinition.Modules.SelectMany (m => m.AllDefinedTypes ());
		}

		public static IEnumerable<TypeDefinition> AllDefinedTypes (this ModuleDefinition moduleDefinition)
		{
			foreach (var typeDefinition in moduleDefinition.Types) {
				yield return typeDefinition;

				foreach (var definition in typeDefinition.AllDefinedTypes ())
					yield return definition;
			}
		}

		public static IEnumerable<TypeDefinition> AllDefinedTypes (this TypeDefinition typeDefinition)
		{
			foreach (var nestedType in typeDefinition.NestedTypes) {
				yield return nestedType;

				foreach (var definition in nestedType.AllDefinedTypes ())
					yield return definition;
			}
		}

		public static IEnumerable<IMemberDefinition> AllMembers (this ModuleDefinition module)
		{
			foreach (var type in module.AllDefinedTypes ()) {
				yield return type;

				foreach (var member in type.AllMembers ())
					yield return member;
			}
		}

		public static IEnumerable<IMemberDefinition> AllMembers (this TypeDefinition type)
		{
			foreach (var field in type.Fields)
				yield return field;

			foreach (var prop in type.Properties)
				yield return prop;

			foreach (var method in type.Methods)
				yield return method;

			foreach (var @event in type.Events)
				yield return @event;
		}

		public static IEnumerable<MethodDefinition> AllMethods (this TypeDefinition type)
		{
			foreach (var m in type.AllMembers ()) {
				switch (m) {
				case MethodDefinition method:
					yield return method;
					break;
				case PropertyDefinition @property:
					if (@property.GetMethod != null)
						yield return @property.GetMethod;

					if (@property.SetMethod != null)
						yield return @property.SetMethod;

					break;
				case EventDefinition @event:
					if (@event.AddMethod != null)
						yield return @event.AddMethod;

					if (@event.RemoveMethod != null)
						yield return @event.RemoveMethod;

					break;

				default:
					break;
				}
			}
		}

		public static bool HasAttribute (this ICustomAttributeProvider provider, string name)
		{
			return provider.CustomAttributes.Any (ca => ca.AttributeType.Name == name);
		}

		public static bool HasAttributeDerivedFrom (this ICustomAttributeProvider provider, string name)
		{
			return provider.CustomAttributes.Any (ca => ca.AttributeType.Resolve ().DerivesFrom (name));
		}

		public static bool DerivesFrom (this TypeDefinition type, string baseTypeName)
		{
			if (type.Name == baseTypeName)
				return true;

			if (type.BaseType == null)
				return false;

			if (type.BaseType.Name == baseTypeName)
				return true;

			return type.BaseType.Resolve ()?.DerivesFrom (baseTypeName) ?? false;
		}

		public static PropertyDefinition GetPropertyDefinition (this MethodDefinition method)
		{
			if (!method.IsSetter && !method.IsGetter)
				throw new ArgumentException ("Method must be a property getter or setter", nameof (method));

			var propertyName = method.Name.Substring (4);
			return method.DeclaringType.Properties.First (p => p.Name == propertyName);
		}

		public static string GetSignature (this MethodDefinition method)
		{
			var builder = new StringBuilder ();
			builder.Append (method.Name);
			if (method.HasGenericParameters) {
				builder.Append ($"<#{method.GenericParameters.Count}>");
			}

			builder.Append ("(");

			if (method.HasParameters) {
				for (int i = 0; i < method.Parameters.Count - 1; i++) {
					// TODO: modifiers
					// TODO: default values
					builder.Append ($"{method.Parameters[i].ParameterType},");
				}

				builder.Append (method.Parameters[method.Parameters.Count - 1].ParameterType);
			}

			builder.Append (")");

			return builder.ToString ();
		}

		public static object GetConstructorArgumentValue (this CustomAttribute attr, int argumentIndex)
		{
			return attr.ConstructorArguments[argumentIndex].Value;
		}

		public static object GetPropertyValue (this CustomAttribute attr, string propertyName)
		{
			foreach (var prop in attr.Properties)
				if (prop.Name == propertyName)
					return prop.Argument.Value;

			return null;
		}
	}
}