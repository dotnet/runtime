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
		/// The path of .interfaceimpl on <see cref="RuntimeInterfaceImplementation.Implementor"/> or a base type that terminates with <see cref="RuntimeInterfaceImplementation.InflatedInterfaceType"/>.
		/// </summary>
		public ImmutableArray<InterfaceImplementation> InterfaceImplementation { get; }

		/// <summary>
		/// The type that has the InterfaceImplementation - either the <see cref="Implementor"/> or a base type of it.
		/// </summary>
		public TypeReference TypeWithInterfaceImplementation { get; }

		/// <summary>
		/// The type of the interface that is implemented by <see cref="RuntimeInterfaceImplementation.Implementor"/>.
		/// This type may be different from the corresponding InterfaceImplementation.InterfaceType if it is generic.
		/// Generic parameters are replaces with generic arguments from the implementing type.
		/// Because of this, do not use this for comparisons or resolving. Use <see cref="InterfaceTypeDefinition"/> or <see cref="InterfaceImplementation">.Last().InterfaceType instead.
		/// </summary>
		public TypeReference InflatedInterfaceType { get; }

		/// <summary>
		/// The resolved definition of the interface type implemented by <see cref="RuntimeInterfaceImplementation.Implementor"/>.
		/// </summary>
		public TypeDefinition? InterfaceTypeDefinition { get; }

		public RuntimeInterfaceImplementation (TypeDefinition implementor, TypeReference typeWithFirstIfaceImpl, IEnumerable<InterfaceImplementation> interfaceImplementation, TypeReference inflatedInterfaceType, LinkContext resolver)
		{
			Implementor = implementor;
			TypeWithInterfaceImplementation = typeWithFirstIfaceImpl;
			InterfaceImplementation = interfaceImplementation.ToImmutableArray ();
			InflatedInterfaceType = inflatedInterfaceType;
			InterfaceTypeDefinition = resolver.Resolve (InterfaceImplementation[InterfaceImplementation.Length - 1].InterfaceType);
		}
	}
}
