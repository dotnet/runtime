// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil;

namespace Mono.Linker
{
	// Copied from https://github.com/jbevain/cecil/blob/master/Mono.Cecil/MethodReferenceComparer.cs
	internal sealed class MethodReferenceComparer : EqualityComparer<MethodReference>
	{
		// Initialized lazily for each thread
		[ThreadStatic]
		static List<MethodReference>? xComparisonStack;

		[ThreadStatic]
		static List<MethodReference>? yComparisonStack;

		public readonly ITryResolveMetadata _resolver;

		public MethodReferenceComparer(ITryResolveMetadata resolver)
		{
			_resolver = resolver;
		}

		public override bool Equals (MethodReference? x, MethodReference? y)
		{
			return AreEqual (x, y, _resolver);
		}

		public override int GetHashCode (MethodReference obj)
		{
			return GetHashCodeFor (obj);
		}

		public static bool AreEqual (MethodReference? x, MethodReference? y, ITryResolveMetadata resolver)
		{
			if (ReferenceEquals (x, y))
				return true;

			if (x is null ^ y is null)
				return false;

			Debug.Assert (x is not null);
			Debug.Assert (y is not null);

			if (x.HasThis != y.HasThis)
				return false;

#pragma warning disable RS0030 // MethodReference.HasParameters is banned - this code is copied from Cecil
			if (x.HasParameters != y.HasParameters)
				return false;
#pragma warning restore RS0030

			if (x.HasGenericParameters != y.HasGenericParameters)
				return false;

#pragma warning disable RS0030 // MethodReference.HasParameters is banned - this code is copied from Cecil
			if (x.Parameters.Count != y.Parameters.Count)
				return false;
#pragma warning restore RS0030

			if (x.Name != y.Name)
				return false;

			if (!TypeReferenceEqualityComparer.AreEqual (x.DeclaringType, y.DeclaringType, resolver))
				return false;

			var xGeneric = x as GenericInstanceMethod;
			var yGeneric = y as GenericInstanceMethod;
			if (xGeneric != null || yGeneric != null) {
				if (xGeneric == null || yGeneric == null)
					return false;

				if (xGeneric.GenericArguments.Count != yGeneric.GenericArguments.Count)
					return false;

				for (int i = 0; i < xGeneric.GenericArguments.Count; i++)
					if (!TypeReferenceEqualityComparer.AreEqual (xGeneric.GenericArguments[i], yGeneric.GenericArguments[i], resolver))
						return false;
			}

			var xResolved = resolver.TryResolve (x);
			var yResolved = resolver.TryResolve (y);

			if (xResolved != yResolved)
				return false;

			if (xResolved == null) {
				// We couldn't resolve either method. In order for them to be equal, their parameter types _must_ match. But wait, there's a twist!
				// There exists a situation where we might get into a recursive state: parameter type comparison might lead to comparing the same
				// methods again if the parameter types are generic parameters whose owners are these methods. We guard against these by using a
				// thread static list of all our comparisons carried out in the stack so far, and if we're in progress of comparing them already,
				// we'll just say that they match.

				xComparisonStack ??= new List<MethodReference> ();

				yComparisonStack ??= new List<MethodReference> ();

				for (int i = 0; i < xComparisonStack.Count; i++) {
					if (xComparisonStack[i] == x && yComparisonStack[i] == y)
						return true;
				}

				xComparisonStack.Add (x);

				try {
					yComparisonStack.Add (y);

					try {
#pragma warning disable RS0030 // MethodReference.HasParameters is banned - this code is copied from Cecil
						for (int i = 0; i < x.Parameters.Count; i++) {
							if (!TypeReferenceEqualityComparer.AreEqual (x.Parameters[i].ParameterType, y.Parameters[i].ParameterType, resolver))
								return false;
						}
#pragma warning restore RS0030
					} finally {
						yComparisonStack.RemoveAt (yComparisonStack.Count - 1);
					}
				} finally {
					xComparisonStack.RemoveAt (xComparisonStack.Count - 1);
				}
			}

			return true;
		}

		public static bool AreSignaturesEqual (MethodReference x, MethodReference y, ITryResolveMetadata resolver, TypeComparisonMode comparisonMode = TypeComparisonMode.Exact)
		{
			if (x.HasThis != y.HasThis)
				return false;

#pragma warning disable RS0030 // MethodReference.HasParameters is banned - this code is copied from Cecil
			if (x.Parameters.Count != y.Parameters.Count)
				return false;
#pragma warning restore RS0030

			if (x.GenericParameters.Count != y.GenericParameters.Count)
				return false;

#pragma warning disable RS0030 // MethodReference.HasParameters is banned - this code is copied from Cecil
			for (var i = 0; i < x.Parameters.Count; i++)
				if (!TypeReferenceEqualityComparer.AreEqual (x.Parameters[i].ParameterType, y.Parameters[i].ParameterType, resolver, comparisonMode))
					return false;
#pragma warning restore RS0030

			if (!TypeReferenceEqualityComparer.AreEqual (x.ReturnType, y.ReturnType, resolver, comparisonMode))
				return false;

			return true;
		}

		public static int GetHashCodeFor (MethodReference obj)
		{
			// a very good prime number
			const int hashCodeMultiplier = 486187739;

			var genericInstanceMethod = obj as GenericInstanceMethod;
			if (genericInstanceMethod != null) {
				var hashCode = GetHashCodeFor (genericInstanceMethod.ElementMethod);
				for (var i = 0; i < genericInstanceMethod.GenericArguments.Count; i++)
					hashCode = hashCode * hashCodeMultiplier + TypeReferenceEqualityComparer.GetHashCodeFor (genericInstanceMethod.GenericArguments[i]);
				return hashCode;
			}

			return TypeReferenceEqualityComparer.GetHashCodeFor (obj.DeclaringType) * hashCodeMultiplier + obj.Name.GetHashCode ();
		}
	}
}
