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

				var recursiveIFaces = GetRuntimeInterfaceImplementations (resolvedInterfaceType);

				foreach (var recursiveIface in recursiveIFaces) {
					var impls = PrependInterfaceImplToChains (recursiveIface, originalType, explicitIface);
					foreach (var impl in impls) {
						interfaceTypeToImplChainMap.AddToList (impl.InterfaceType, impl.Chain);
					}
				}
			}

			if (originalType.BaseType is not null && _context.TryResolve (originalType.BaseType) is { } baseTypeDef) {
				var baseTypeIfaces = GetRuntimeInterfaceImplementations (baseTypeDef);
				foreach (var recursiveIface in baseTypeIfaces) {
					var impls = CreateImplementationChainsForDerivedType (recursiveIface, originalType.BaseType);
					foreach (var impl in impls) {
						interfaceTypeToImplChainMap.AddToList (impl.InterfaceType, impl.Chain);
					}
				}
			}

			if (interfaceTypeToImplChainMap.Count == 0)
				return ImmutableArray<RuntimeInterfaceImplementation>.Empty;

			ImmutableArray<RuntimeInterfaceImplementation>.Builder builder = ImmutableArray.CreateBuilder<RuntimeInterfaceImplementation> (interfaceTypeToImplChainMap.Count);
			foreach (var kvp in interfaceTypeToImplChainMap) {
				builder.Add (new (originalType, kvp.Key, _context.TryResolve (kvp.Key), kvp.Value));
			}
			var runtimeInterfaces = builder.MoveToImmutable ();
			_runtimeInterfaceImpls[originalType] = runtimeInterfaces;
			return runtimeInterfaces;

		}

		/// <summary>
		/// Returns a list of InterfaceImplementationChains for a derived type of <see cref="Implementor"/>.
		/// </summary>
		IEnumerable<(TypeReference InterfaceType, InterfaceImplementationChain Chain)> CreateImplementationChainsForDerivedType (RuntimeInterfaceImplementation runtimeImpl, TypeReference baseTypeRef)
		{
			// This is only valid for classes
			Debug.Assert (runtimeImpl.Implementor.IsClass);
			Debug.Assert (runtimeImpl.Implementor == _context.TryResolve (baseTypeRef));

			var inflatedInterfaceType = runtimeImpl.InflatedInterfaceType.TryInflateFrom (baseTypeRef, _context);
			Debug.Assert (inflatedInterfaceType is not null);

			foreach (var impl in runtimeImpl.InterfaceImplementationChains) {
				var inflatedImplProvider = impl.TypeWithInterfaceImplementation.TryInflateFrom (baseTypeRef, _context);
				Debug.Assert (inflatedImplProvider is not null);
				yield return (inflatedInterfaceType, new InterfaceImplementationChain (inflatedImplProvider, impl.InterfaceImplementations));
			}
		}

		/// <summary>
		/// Returns a list of InterfaceImplementationChains for a type that has an explicit implementation of <see cref="Implementor"/>.
		/// </summary>
		IEnumerable<(TypeReference InterfaceType, InterfaceImplementationChain Chain)> PrependInterfaceImplToChains (RuntimeInterfaceImplementation runtimeImpl, TypeDefinition typeWithPrependedImpl, InterfaceImplementation implToPrepend)
		{
			Debug.Assert (runtimeImpl.Implementor.IsInterface);
			Debug.Assert (_context.TryResolve (implToPrepend.InterfaceType) == runtimeImpl.Implementor);
			Debug.Assert (typeWithPrependedImpl.Interfaces.Contains (implToPrepend));

			var inflatedInterfaceType = runtimeImpl.InflatedInterfaceType.TryInflateFrom (implToPrepend.InterfaceType, _context);
			Debug.Assert (inflatedInterfaceType is not null);

			foreach (var existingImpl in runtimeImpl.InterfaceImplementationChains) {
				yield return (inflatedInterfaceType, new InterfaceImplementationChain (typeWithPrependedImpl, existingImpl.InterfaceImplementations.Insert (0, implToPrepend)));
			}
		}
	}
}
