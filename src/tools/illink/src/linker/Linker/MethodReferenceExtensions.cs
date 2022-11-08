// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;

namespace Mono.Linker
{
	internal static class MethodReferenceExtensions
	{
		public static string GetDisplayName (this MethodReference method)
		{
			var sb = new System.Text.StringBuilder ();

			// Match C# syntaxis name if setter or getter
#pragma warning disable RS0030 // Cecil's Resolve is banned -- this should be a very cold path and makes calling this method much simpler
			var methodDefinition = method.Resolve ();
#pragma warning restore RS0030
			if (methodDefinition != null && (methodDefinition.IsSetter || methodDefinition.IsGetter)) {
				// Append property name
				string name = methodDefinition.IsSetter ? string.Concat (methodDefinition.Name.AsSpan (4), ".set") : string.Concat (methodDefinition.Name.AsSpan (4), ".get");
				sb.Append (name);
				// Insert declaring type name and namespace
				sb.Insert (0, '.').Insert (0, method.DeclaringType.GetDisplayName ());
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
			if (method.HasMetadataParameters ()) {
#pragma warning disable RS0030 // MethodReference.Parameters is banned -- it's best to leave this as is for now
				for (int i = 0; i < method.Parameters.Count - 1; i++)
					sb.Append (method.Parameters[i].ParameterType.GetDisplayNameWithoutNamespace ()).Append (", ");
				sb.Append (method.Parameters[method.Parameters.Count - 1].ParameterType.GetDisplayNameWithoutNamespace ());
#pragma warning restore RS0030 // Do not used banned APIs
			}

			sb.Append (")");

			// Insert generic parameters
			if (method.HasGenericParameters) {
				TypeReferenceExtensions.PrependGenericParameters (method.GenericParameters, sb);
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

		public static TypeReference? GetReturnType (this MethodReference method, LinkContext context)
		{
			if (method.DeclaringType is GenericInstanceType genericInstance)
				return TypeReferenceExtensions.InflateGenericType (genericInstance, method.ReturnType, context);

			return method.ReturnType;
		}

		public static bool ReturnsVoid (this IMethodSignature method)
		{
			return method.ReturnType.WithoutModifiers ().MetadataType == MetadataType.Void;
		}

		public static TypeReference? GetInflatedParameterType (this MethodReference method, int parameterIndex, LinkContext context)
		{
#pragma warning disable RS0030 // MethodReference.Parameters is banned -- it's best to leave this as is for now
			if (method.DeclaringType is GenericInstanceType genericInstance)
				return TypeReferenceExtensions.InflateGenericType (genericInstance, method.Parameters[parameterIndex].ParameterType, context);

			return method.Parameters[parameterIndex].ParameterType;
#pragma warning restore RS0030 // Do not used banned APIs
		}

		/// <summary>
		/// Gets the number of entries in the 'Parameters' section of a method's metadata (i.e. excludes the implicit 'this' from the count)
		/// </summary>
#pragma warning disable RS0030 // MethodReference.Parameters is banned -- this provides a wrapper
		public static int GetMetadataParametersCount (this MethodReference method)
			=> method.Parameters.Count;
#pragma warning restore RS0030 // Do not used banned APIs

		/// <summary>
		/// Returns true if the method has any parameters in the .parameters section of the method's metadata
		/// </summary>
		public static bool HasMetadataParameters (this MethodReference method)
			=> method.GetMetadataParametersCount () != 0;

		/// <summary>
		/// Returns a list of the parameters in the method's 'parameters' metadata section (i.e. excluding the implicit 'this' parameter)
		/// </summary>
#pragma warning disable RS0030 // MethodReference.Parameters is banned -- this provides a wrapper
		public static int GetParametersCount (this MethodReference method)
			=> method.Parameters.Count + (method.HasImplicitThis () ? 1 : 0);
#pragma warning restore RS0030 // Do not used banned APIs

		public static bool IsDeclaredOnType (this MethodReference method, string fullTypeName)
		{
			return method.DeclaringType.IsTypeOf (fullTypeName);
		}

		public static bool HasImplicitThis (this MethodReference method)
		{
			return method.HasThis && !method.ExplicitThis;
		}

		/// <summary>
		/// Returns an IEnumerable of the ReferenceKind of each parameter, with the first being for the implicit 'this' parameter if it exists
		/// Used for better performance when it's only necessary to get the ReferenceKind of all parameters and nothing else.
		/// </summary>
		public static IEnumerable<ReferenceKind> GetParameterReferenceKinds (this MethodReference method)
		{
			if (method.HasImplicitThis ())
				yield return method.DeclaringType.IsValueType ? ReferenceKind.Ref : ReferenceKind.None;
#pragma warning disable RS0030 // MethodReference.Parameters is banned -- this provides a wrapper
			foreach (var parameter in method.Parameters)
				yield return GetReferenceKind (parameter);
#pragma warning restore RS0030 // Do not used banned APIs

			static ReferenceKind GetReferenceKind (ParameterDefinition param)
			{
				if (!param.ParameterType.IsByReference)
					return ReferenceKind.None;
				if (param.IsIn)
					return ReferenceKind.In;
				if (param.IsOut)
					return ReferenceKind.Out;
				return ReferenceKind.Ref;
			}
		}
	}
}
