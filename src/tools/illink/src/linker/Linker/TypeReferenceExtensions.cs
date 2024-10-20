// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;

namespace Mono.Linker
{
	internal static class TypeReferenceExtensions
	{
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

				type = type.GetElementType ();
				if (type.DeclaringType is not TypeReference declaringType)
					break;

				type = declaringType;

				sb.Insert (0, '.');
			}

			return sb;
		}

		internal static void PrependGenericParameters (IList<GenericParameter> genericParameters, StringBuilder sb)
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

		public static TypeReference? GetInflatedDeclaringType (this TypeReference type, ITryResolveMetadata resolver)
		{
			if (type.IsGenericParameter || type.IsByReference || type.IsPointer)
				return null;

			if (type is SentinelType sentinelType)
				return sentinelType.ElementType.GetInflatedDeclaringType (resolver);

			if (type is PinnedType pinnedType)
				return pinnedType.ElementType.GetInflatedDeclaringType (resolver);

			if (type is RequiredModifierType requiredModifierType)
				return requiredModifierType.ElementType.GetInflatedDeclaringType (resolver);

			if (type is GenericInstanceType genericInstance) {
				var declaringType = genericInstance.DeclaringType;

				if (declaringType.HasGenericParameters) {
					var result = new GenericInstanceType (declaringType);
					for (var i = 0; i < declaringType.GenericParameters.Count; ++i)
						result.GenericArguments.Add (genericInstance.GenericArguments[i]);

					return result;
				}

				return declaringType;
			}

			if (type is TypeDefinition typeDefinition)
				return typeDefinition.DeclaringType;

			Debug.Assert (false);
			return null;
		}

		public static TypeReference InflateFrom (this TypeReference typeToInflate, IGenericInstance? maybeGenericInstanceProvider)
		{
			if (maybeGenericInstanceProvider is IGenericInstance genericInstanceProvider)
				return InflateGenericType (genericInstanceProvider, typeToInflate);
			return typeToInflate;
		}

		public static IEnumerable<(TypeReference InflatedInterface, InterfaceImplementation OriginalImpl)> GetInflatedInterfaces (this TypeReference typeRef, ITryResolveMetadata resolver)
		{
			var typeDef = resolver.TryResolve (typeRef);

			if (typeDef?.HasInterfaces != true)
				yield break;

			if (typeRef is GenericInstanceType genericInstance) {
				foreach (var interfaceImpl in typeDef.Interfaces)
					yield return (InflateGenericType (genericInstance, interfaceImpl.InterfaceType), interfaceImpl);
			} else {
				foreach (var interfaceImpl in typeDef.Interfaces)
					yield return (interfaceImpl.InterfaceType, interfaceImpl);
			}
		}

		public static TypeReference InflateGenericType (IGenericInstance genericInstanceProvider, TypeReference typeToInflate)
		{
			Debug.Assert (genericInstanceProvider is GenericInstanceType or GenericInstanceMethod);
			if (typeToInflate is ArrayType arrayType) {
				var inflatedElementType = InflateGenericType (genericInstanceProvider, arrayType.ElementType);

				if (inflatedElementType != arrayType.ElementType)
					return new ArrayType (inflatedElementType, arrayType.Rank);

				return arrayType;
			}

			if (typeToInflate is GenericInstanceType genericInst)
				return MakeGenericType (genericInstanceProvider, genericInst);

			if (typeToInflate is GenericParameter genericParameter) {
				if (genericParameter.Owner is MethodReference) {
					if (genericInstanceProvider is not GenericInstanceMethod)
						return typeToInflate;
					return genericInstanceProvider.GenericArguments[genericParameter.Position];
				}

				Debug.Assert (genericParameter.Owner is TypeReference);
				if (genericInstanceProvider is not GenericInstanceType) {
					if (((GenericInstanceMethod) genericInstanceProvider).DeclaringType is not GenericInstanceType genericInstanceType)
						return typeToInflate;
					genericInstanceProvider = genericInstanceType;
				}
				return genericInstanceProvider.GenericArguments[genericParameter.Position];
			}

			if (typeToInflate is FunctionPointerType functionPointerType) {
				var result = new FunctionPointerType {
					ReturnType = InflateGenericType (genericInstanceProvider, functionPointerType.ReturnType)
				};

				for (int i = 0; i < functionPointerType.Parameters.Count; i++) {
					var inflatedParameterType = InflateGenericType (genericInstanceProvider, functionPointerType.Parameters[i].ParameterType);
					result.Parameters.Add (new ParameterDefinition (inflatedParameterType));
				}

				return result;
			}

			if (typeToInflate is IModifierType modifierType) {
				var modifier = InflateGenericType (genericInstanceProvider, modifierType.ModifierType);
				var elementType = InflateGenericType (genericInstanceProvider, modifierType.ElementType);

				if (modifierType is OptionalModifierType) {
					return new OptionalModifierType (modifier, elementType);
				}

				return new RequiredModifierType (modifier, elementType);
			}

			if (typeToInflate is PinnedType pinnedType) {
				var elementType = InflateGenericType (genericInstanceProvider, pinnedType.ElementType);

				if (elementType != pinnedType.ElementType)
					return new PinnedType (elementType);

				return pinnedType;
			}

			if (typeToInflate is PointerType pointerType) {
				var elementType = InflateGenericType (genericInstanceProvider, pointerType.ElementType);

				if (elementType != pointerType.ElementType)
					return new PointerType (elementType);

				return pointerType;
			}

			if (typeToInflate is ByReferenceType byReferenceType) {
				var elementType = InflateGenericType (genericInstanceProvider, byReferenceType.ElementType);

				if (elementType != byReferenceType.ElementType)
					return new ByReferenceType (elementType);

				return byReferenceType;
			}

			if (typeToInflate is SentinelType sentinelType) {
				var elementType = InflateGenericType (genericInstanceProvider, sentinelType.ElementType);

				if (elementType != sentinelType.ElementType)
					return new SentinelType (elementType);

				return sentinelType;
			}

			return typeToInflate;
		}

		private static GenericInstanceType MakeGenericType (IGenericInstance genericInstanceProvider, GenericInstanceType type)
		{
			var result = new GenericInstanceType (type.ElementType);

			for (var i = 0; i < type.GenericArguments.Count; ++i) {
				result.GenericArguments.Add (InflateGenericType (genericInstanceProvider, type.GenericArguments[i]));
			}

			return result;
		}

		public static IEnumerable<MethodReference> GetMethods (this TypeReference type, ITryResolveMetadata resolver)
		{
			TypeDefinition? typeDef = resolver.TryResolve (type);
			if (typeDef?.HasMethods != true)
				yield break;

			if (type is GenericInstanceType genericInstanceType) {
				foreach (var methodDef in typeDef.Methods)
					yield return MakeMethodReferenceForGenericInstanceType (genericInstanceType, methodDef);
			} else {
				foreach (var method in typeDef.Methods)
					yield return method;
			}
		}

		private static MethodReference MakeMethodReferenceForGenericInstanceType (GenericInstanceType genericInstanceType, MethodDefinition methodDef)
		{
			var method = new MethodReference (methodDef.Name, methodDef.ReturnType, genericInstanceType) {
				HasThis = methodDef.HasThis,
				ExplicitThis = methodDef.ExplicitThis,
				CallingConvention = methodDef.CallingConvention
			};

#pragma warning disable RS0030 // MethodReference.Parameters is banned. It makes sense to use when needing to directly use Cecil's api.
			foreach (var parameter in methodDef.Parameters)
				method.Parameters.Add (new ParameterDefinition (parameter.Name, parameter.Attributes, parameter.ParameterType));
#pragma warning restore RS0030

			foreach (var gp in methodDef.GenericParameters)
				method.GenericParameters.Add (new GenericParameter (gp.Name, method));

			return method;
		}

		public static string ToCecilName (this string fullTypeName)
		{
			return fullTypeName.Replace ('+', '/');
		}

		public static bool HasDefaultConstructor (this TypeDefinition type, LinkContext context)
		{
			foreach (var m in type.Methods) {
				if (m.HasMetadataParameters ())
					continue;

				var definition = context.Resolve (m);
				if (definition?.IsDefaultConstructor () == true)
					return true;
			}

			return false;
		}

		public static MethodReference GetDefaultInstanceConstructor (this TypeDefinition type, LinkContext context)
		{
			foreach (var m in type.Methods) {
				if (m.HasMetadataParameters ())
					continue;

				var definition = context.Resolve (m);
				if (definition?.IsDefaultConstructor () != true)
					continue;

				return m;
			}

			throw new NotImplementedException ();
		}

		public static bool IsTypeOf (this TypeReference type, string ns, string name)
		{
			return type.Name == name
				&& type.Namespace == ns;
		}

		public static bool IsTypeOf (this TypeReference type, string fullTypeName)
		{
			var name = fullTypeName.AsSpan ();
			if (type.Name.Length + 1 > name.Length)
				return false;

			if (!name.Slice (name.Length - type.Name.Length).Equals (type.Name.AsSpan (), StringComparison.Ordinal))
				return false;

			if (name[name.Length - type.Name.Length - 1] != '.')
				return false;

			return name.Slice (0, name.Length - type.Name.Length - 1).Equals (type.Namespace, StringComparison.Ordinal);
		}

		public static bool IsTypeOf<T> (this TypeReference tr)
		{
			var type = typeof (T);
			return tr.Name == type.Name && tr.Namespace == type.Namespace;
		}

		public static bool IsTypeOf (this TypeReference tr, WellKnownType type)
		{
			return tr.TryGetWellKnownType () == type;
		}

		public static WellKnownType? TryGetWellKnownType (this TypeReference tr)
		{
			return tr.MetadataType switch {
				MetadataType.String => WellKnownType.System_String,
				MetadataType.Object => WellKnownType.System_Object,
				MetadataType.Void => WellKnownType.System_Void,
				// TypeReferences of System.Array do not have a MetadataType of MetadataType.Array -- use string checking instead
				MetadataType.Array or _ => WellKnownTypeExtensions.GetWellKnownType (tr.Namespace, tr.Name)
			};
		}

		public static bool IsSubclassOf (this TypeReference type, string ns, string name, ITryResolveMetadata resolver)
		{
			TypeDefinition? baseType = resolver.TryResolve (type);
			while (baseType != null) {
				if (baseType.IsTypeOf (ns, name))
					return true;
				baseType = resolver.TryResolve (baseType.BaseType);
			}

			return false;
		}

		public static TypeReference WithoutModifiers (this TypeReference type)
		{
			while (type is IModifierType) {
				type = ((IModifierType) type).ElementType;
			}
			return type;
		}

		// Check whether this type represents a "named type" (i.e. a type that has a name and can be resolved to a TypeDefinition),
		// not an array, pointer, byref, or generic parameter. Conceptually this is supposed to represent the same idea as Roslyn's
		// INamedTypeSymbol, or ILC's DefType/MetadataType.
		public static bool IsNamedType (this TypeReference typeReference) {
			if (typeReference.IsRequiredModifier)
				return ((RequiredModifierType) typeReference).ElementType.IsNamedType ();
			if (typeReference.IsOptionalModifier)
				return ((OptionalModifierType) typeReference).ElementType.IsNamedType ();

			if (typeReference.IsDefinition || typeReference.IsGenericInstance)
				return true;

			if (typeReference.IsArray ||
				typeReference.IsByReference ||
				typeReference.IsPointer ||
				typeReference.IsFunctionPointer ||
				typeReference.IsGenericParameter)
				return false;

			// Shouldn't get called for these cases
			Debug.Assert (!typeReference.IsPinned);
			Debug.Assert (!typeReference.IsSentinel);
			if (typeReference.IsPinned || typeReference.IsSentinel)
				return false;

			Debug.Assert (typeReference.GetType () == typeof (TypeReference));
			return true;
		}

		/// <summary>
		/// Resolves a TypeReference to a TypeDefinition if possible. Non-named types other than arrays (pointers, byrefs, function pointers) return null.
		/// Array types that are dynamically accessed resolve to System.Array instead of its element type - which is what Cecil resolves to.
		/// Any data flow annotations placed on a type parameter which receives an array type apply to the array itself. None of the members in its
		/// element type should be marked.
		/// </summary>
		public static TypeDefinition? ResolveToTypeDefinition (this TypeReference typeReference, LinkContext context)
			=> typeReference is ArrayType
				? BCL.FindPredefinedType (WellKnownType.System_Array, context)
				: typeReference.IsNamedType ()
					? context.TryResolve (typeReference)
					: null;

		public static bool IsByRefOrPointer (this TypeReference typeReference)
		{
			return typeReference.WithoutModifiers ().MetadataType switch {
				MetadataType.Pointer or MetadataType.ByReference => true,
				_ => false,
			};
		}
	}
}
