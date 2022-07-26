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
				throw new ArgumentException ();

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

		public static object? GetPropertyValue (this CustomAttribute attr, string propertyName)
		{
			foreach (var prop in attr.Properties)
				if (prop.Name == propertyName)
					return prop.Argument.Value;

			return null;
		}

		public static bool IsEventMethod (this MethodDefinition md)
		{
			return (md.SemanticsAttributes & MethodSemanticsAttributes.AddOn) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.Fire) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.RemoveOn) != 0;
		}

		public static string GetDisplayName (this MethodReference method)
		{
			var sb = new System.Text.StringBuilder ();

			// Match C# syntaxis name if setter or getter
			var methodDefinition = method.Resolve ();
			if (methodDefinition != null && (methodDefinition.IsSetter || methodDefinition.IsGetter)) {
				// Append property name
				string name = GetPropertyNameFromAccessorName (methodDefinition.Name, methodDefinition.IsSetter);
				sb.Append (name);
				// Insert declaring type name and namespace
				sb.Insert (0, '.').Insert (0, method.DeclaringType?.GetDisplayName ());
				return sb.ToString ();
			}

			if (methodDefinition != null && methodDefinition.IsEventMethod ()) {
				// Append event name
				string name = methodDefinition.SemanticsAttributes switch {
					MethodSemanticsAttributes.AddOn => string.Concat (methodDefinition.Name.AsSpan (4), ".add"),
					MethodSemanticsAttributes.RemoveOn => string.Concat (methodDefinition.Name.AsSpan (7), ".remove"),
					MethodSemanticsAttributes.Fire => string.Concat (methodDefinition.Name.AsSpan (6), ".raise"),
					_ => throw new NotSupportedException (),
				};
				sb.Append (name);
				// Insert declaring type name and namespace
				sb.Insert (0, '.').Insert (0, method.DeclaringType.GetDisplayName ());
				return sb.ToString ();
			}

			// Append parameters
			sb.Append ("(");
			if (method.HasParameters) {
				for (int i = 0; i < method.Parameters.Count - 1; i++)
					sb.Append (method.Parameters[i].ParameterType.GetDisplayNameWithoutNamespace ()).Append (", ");

				sb.Append (method.Parameters[method.Parameters.Count - 1].ParameterType.GetDisplayNameWithoutNamespace ());
			}

			sb.Append (")");

			// Insert generic parameters
			if (method.HasGenericParameters) {
				PrependGenericParameters (method.GenericParameters, sb);
			}

			// Insert method name
			if (method.Name == ".ctor")
				sb.Insert (0, method.DeclaringType.Name);
			else
				sb.Insert (0, method.Name);

			// Insert declaring type name and namespace
			if (method.DeclaringType != null)
				sb.Insert (0, '.').Insert (0, method.DeclaringType.GetDisplayName ());

			return sb.ToString ();
		}

		private static string GetPropertyNameFromAccessorName (string methodName, bool isSetter) =>
			isSetter ?
			string.Concat (methodName.StartsWith ("set_") ? methodName.AsSpan (4) : methodName.Replace (".set_", "."), ".set") :
			string.Concat (methodName.StartsWith ("get_") ? methodName.AsSpan (4) : methodName.Replace (".get_", "."), ".get");

		public static string GetDisplayName (this TypeReference type)
		{
			var builder = GetDisplayNameWithoutNamespace (type);
			var namespaceDisplayName = type.GetNamespaceDisplayName ();
			if (!string.IsNullOrEmpty (namespaceDisplayName)) {
				builder.Insert (0, ".");
				builder.Insert (0, namespaceDisplayName);
			}

			return builder.ToString ();
		}

		public static string GetDisplayName (this FieldReference field)
		{
			var builder = new StringBuilder ();
			if (field.DeclaringType != null) {
				builder.Append (field.DeclaringType.GetDisplayName ());
				builder.Append (".");
			}

			builder.Append (field.Name);

			return builder.ToString ();
		}

		public static string GetNamespaceDisplayName (this MemberReference member)
		{
			var type = member is TypeReference typeReference ? typeReference : member.DeclaringType;
			while (type.DeclaringType != null)
				type = type.DeclaringType;

			return type.Namespace;
		}

		public static StringBuilder GetDisplayNameWithoutNamespace (this TypeReference type)
		{
			var sb = new StringBuilder ();
			if (type == null)
				return sb;

			Stack<TypeReference>? genericArguments = null;
			while (true) {
				switch (type) {
				case ArrayType arrayType:
					AppendArrayType (arrayType, sb);
					break;
				case GenericInstanceType genericInstanceType:
					genericArguments = new Stack<TypeReference> (genericInstanceType.GenericArguments);
					type = genericInstanceType.ElementType;
					continue;
				default:
					if (type.HasGenericParameters) {
						int genericParametersCount = type.GenericParameters.Count;
						int declaringTypeGenericParametersCount = type.DeclaringType?.GenericParameters?.Count ?? 0;

						string simpleName;
						if (genericParametersCount > declaringTypeGenericParametersCount) {
							if (genericArguments?.Count > 0)
								PrependGenericArguments (genericArguments, genericParametersCount - declaringTypeGenericParametersCount, sb);
							else
								PrependGenericParameters (type.GenericParameters.Skip (declaringTypeGenericParametersCount).ToList (), sb);

							int explicitArityIndex = type.Name.IndexOf ('`');
							simpleName = explicitArityIndex != -1 ? type.Name.Substring (0, explicitArityIndex) : type.Name;
						} else
							simpleName = type.Name;

						sb.Insert (0, simpleName);
						break;
					}

					sb.Insert (0, type.Name);
					break;
				}

				if (type.DeclaringType is not TypeReference declaringType)
					break;

				type = declaringType;

				sb.Insert (0, '.');
			}

			return sb;
		}

		public static void PrependGenericParameters (IList<GenericParameter> genericParameters, StringBuilder sb)
		{
			sb.Insert (0, '>').Insert (0, genericParameters[genericParameters.Count - 1]);
			for (int i = genericParameters.Count - 2; i >= 0; i--)
				sb.Insert (0, ',').Insert (0, genericParameters[i]);

			sb.Insert (0, '<');
		}

		static void PrependGenericArguments (Stack<TypeReference> genericArguments, int argumentsToTake, StringBuilder sb)
		{
			sb.Insert (0, '>').Insert (0, genericArguments.Pop ().GetDisplayNameWithoutNamespace ().ToString ());
			while (--argumentsToTake > 0)
				sb.Insert (0, ',').Insert (0, genericArguments.Pop ().GetDisplayNameWithoutNamespace ().ToString ());

			sb.Insert (0, '<');
		}

		static void AppendArrayType (ArrayType arrayType, StringBuilder sb)
		{
			void parseArrayDimensions (ArrayType at)
			{
				sb.Append ('[');
				for (int i = 0; i < at.Dimensions.Count - 1; i++)
					sb.Append (',');

				sb.Append (']');
			}

			sb.Append (arrayType.Name.AsSpan (0, arrayType.Name.IndexOf ('[')));
			parseArrayDimensions (arrayType);
			var element = arrayType.ElementType as ArrayType;
			while (element != null) {
				parseArrayDimensions (element);
				element = element.ElementType as ArrayType;
			}
		}
	}
}
