// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker
{
	/// <summary>
	/// Represents an implementation of an interface on a type that may be directly on the type or on a base type or implemented interface.
	/// This type is considered to implement the interface at runtime, even though the interface may not be directly on the type.
	/// </summary>
	/// <remarks>
	/// This type should be used for marking, but should NOT be used to check if a runtime interface implementation has been marked.
	/// This type represents the most direct way an interface may be implemented, but it may be implemented in a less direct way that is not represented here.
	/// You should check all possible implementation 'paths' to determine if an interface is implemented, for example <see cref="MarkStep.IsInterfaceImplementationMarkedRecursively"/>.
	/// </remarks>
	internal sealed class RuntimeInterfaceImplementation
	{
		/// <summary>
		/// The type that implements <see cref="RuntimeInterfaceImplementation.InflatedInterfaceType"/>.
		/// </summary>
		public TypeDefinition Implementor { get; }

		/// <summary>
		/// All the chains of .interfaceImpls that cause <see cref="Implementor"/> to implement <see cref="InflatedInterfaceType"/>
		/// </summary>
		public ImmutableArray<InterfaceImplementationChain> InterfaceImplementationChains { get; }

		/// <summary>
		/// The inflated interface type reference that is implemented by <see cref="Implementor"/>.
		/// </summary>
		public TypeReference InflatedInterfaceType { get; }

		/// <summary>
		/// The <see cref="TypeDefinition"/> of <see cref="InflatedInterfaceType"/>
		/// </summary>
		public TypeDefinition? InterfaceTypeDefinition { get; }

		public RuntimeInterfaceImplementation (TypeDefinition implementor, TypeReference interfaceType, TypeDefinition? interfaceTypeDefinition, IEnumerable<InterfaceImplementationChain> interfaceImplementations)
		{
			Implementor = implementor;
			InterfaceImplementationChains = interfaceImplementations.ToImmutableArray ();
			InflatedInterfaceType = interfaceType;
			InterfaceTypeDefinition = interfaceTypeDefinition;
		}

		public bool IsAnyImplementationMarked (AnnotationStore annotations, ITryResolveMetadata context)
		{
			if (annotations.IsMarked (this))
				return true;
			foreach (var chain in InterfaceImplementationChains) {
				if (chain.IsMarked (annotations, context)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns a list of InterfaceImplementationChains for a derived type of <see cref="Implementor"/>.
		/// </summary>
		public IEnumerable<(TypeReference InterfaceType, InterfaceImplementationChain Chain)> CreateImplementationChainsForDerivedType (TypeReference baseTypeRef, ITryResolveMetadata context)
		{
			// This is only valid for classes
			Debug.Assert (Implementor.IsClass);
			Debug.Assert (Implementor == context.TryResolve (baseTypeRef));

			var inflatedInterfaceType = InflatedInterfaceType.TryInflateFrom (baseTypeRef, context);
			Debug.Assert (inflatedInterfaceType is not null);

			foreach (var impl in InterfaceImplementationChains) {
				var inflatedImplProvider = impl.TypeWithInterfaceImplementation.TryInflateFrom (baseTypeRef, context);
				Debug.Assert (inflatedImplProvider is not null);

				yield return (inflatedInterfaceType, new InterfaceImplementationChain (inflatedImplProvider, impl.InterfaceImplementations));
			}
		}

		/// <summary>
		/// Returns a list of InterfaceImplementationChains for a type that has an explicit implementation of <see cref="Implementor"/>.
		/// </summary>
		public IEnumerable<(TypeReference InterfaceType, InterfaceImplementationChain Chain)> CreateImplementationChainForImplementingType (TypeDefinition typeThatImplementsImplementor, InterfaceImplementation impl, ITryResolveMetadata context)
		{
			Debug.Assert (Implementor.IsInterface);
			Debug.Assert (typeThatImplementsImplementor.Interfaces.Contains (impl));
			Debug.Assert (context.TryResolve (impl.InterfaceType) == Implementor);

			var inflatedInterfaceType = InflatedInterfaceType.TryInflateFrom (impl.InterfaceType, context);
			Debug.Assert (inflatedInterfaceType is not null);

			foreach (var existingImpl in InterfaceImplementationChains) {
				yield return (inflatedInterfaceType, new InterfaceImplementationChain (typeThatImplementsImplementor, existingImpl.InterfaceImplementations.Insert (0, impl)));
			}
		}
	}
}
