// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Mono.Linker.Linker
{
	internal struct RuntimeInterfacesAlgorithm
	{
		readonly ITryResolveMetadata _context;
		readonly Dictionary<TypeDefinition, ImmutableArray<RuntimeInterfaceImplementation>> _runtimeInterfaceImpls = new ();

		public RuntimeInterfacesAlgorithm (ITryResolveMetadata context)
		{
			this._context = context;
		}

		public ImmutableArray<RuntimeInterfaceImplementation> GetRuntimeInterfaceImplementations (TypeDefinition originalType)
		{
			if (_runtimeInterfaceImpls.TryGetValue (originalType, out var runtimeIfaces)) {
				return runtimeIfaces;
			}

			Dictionary<TypeReference, List<InterfaceImplementationChain>> interfaceTypeToImplChainMap = new (new TypeReferenceEqualityComparer (_context));

			foreach (var explicitIface in originalType.Interfaces) {
				interfaceTypeToImplChainMap.AddToList (explicitIface.InterfaceType, new InterfaceImplementationChain (originalType, [explicitIface]));

				var resolvedInterfaceType = _context.TryResolve (explicitIface.InterfaceType);
				if (resolvedInterfaceType is null) {
					continue;
				}

				// Add the recursive interfaces for each explicit interface, prepending the explicit interface on `originalType` to the chain
				var recursiveIFaces = GetRuntimeInterfaceImplementations (resolvedInterfaceType);
				foreach (var runtimeImpl in recursiveIFaces) {
					// Inflate the interface type with the explicit interfaceImpl reference
					var inflatedInterfaceType = runtimeImpl.InflatedInterfaceType.InflateFrom (explicitIface.InterfaceType);
					foreach (var existingImpl in runtimeImpl.InterfaceImplementationChains) {
						interfaceTypeToImplChainMap.AddToList (inflatedInterfaceType, new InterfaceImplementationChain (originalType, existingImpl.InterfaceImplementations.Insert (0, explicitIface)));
					}
				}
			}

			if (originalType.BaseType is not null && _context.TryResolve (originalType.BaseType) is { } baseTypeDef) {
				var baseTypeIfaces = GetRuntimeInterfaceImplementations (baseTypeDef);
				foreach (var runtimeImpl in baseTypeIfaces) {
					// Inflate the interface type with the base type reference
					var inflatedInterfaceType = runtimeImpl.InflatedInterfaceType.InflateFrom (originalType.BaseType);
					foreach (var impl in runtimeImpl.InterfaceImplementationChains) {
						// Inflate the provider for the first .impl - this could be a different recursive base type for each chain
						var inflatedImplProvider = impl.TypeWithInterfaceImplementation.InflateFrom (originalType.BaseType);
						interfaceTypeToImplChainMap.AddToList (inflatedInterfaceType, new InterfaceImplementationChain (inflatedImplProvider, impl.InterfaceImplementations));
					}
				}
			}

			if (interfaceTypeToImplChainMap.Count == 0)
				return ImmutableArray<RuntimeInterfaceImplementation>.Empty;

			// Build the ImmutableArray and cache it
			ImmutableArray<RuntimeInterfaceImplementation>.Builder builder = ImmutableArray.CreateBuilder<RuntimeInterfaceImplementation> (interfaceTypeToImplChainMap.Count);
			foreach (var kvp in interfaceTypeToImplChainMap) {
				builder.Add (new (originalType, kvp.Key, _context.TryResolve (kvp.Key), kvp.Value));
			}
			var runtimeInterfaces = builder.MoveToImmutable ();
			_runtimeInterfaceImpls[originalType] = runtimeInterfaces;

			return runtimeInterfaces;
		}
	}
}
