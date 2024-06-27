// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Mono.Cecil;

namespace Mono.Linker
{
	internal sealed class InterfaceImplementationChain
	{
		/// <summary>
		/// The type that has the InterfaceImplementation - either the <see cref="Implementor"/> or a base type of it.
		/// </summary>
		public TypeReference TypeWithInterfaceImplementation { get; }

		/// <summary>
		/// The path of .interfaceimpl on <see cref="RuntimeInterfaceImplementation.Implementor"/> or a base type that terminates with <see cref="RuntimeInterfaceImplementation.InflatedInterfaceType"/>.
		/// </summary>
		public ImmutableArray<InterfaceImplementation> InterfaceImplementations { get; }

		public InterfaceImplementationChain (TypeReference typeWithInterfaceImplementation, ImmutableArray<InterfaceImplementation> interfaceImplementation)
		{
			TypeWithInterfaceImplementation = typeWithInterfaceImplementation;
			InterfaceImplementations = interfaceImplementation;
		}

		/// <summary>
		/// Returns true if all the .interfaceImpls in the chain and the  are marked.
		/// </summary>
		/// <param name="annotations"></param>
		/// <returns></returns>
		public bool IsMarked (AnnotationStore annotations, ITryResolveMetadata context)
		{
			var typeDef = context.TryResolve (TypeWithInterfaceImplementation);
			// If we have the .interfaceImpls on this type, it must be resolvable
			Debug.Assert (typeDef is not null);
			if (!annotations.IsMarked (typeDef))
				return false;

			foreach (var impl in InterfaceImplementations) {
				if (!annotations.IsMarked (impl))
					return false;
			}

			return true;
		}
	}
}
