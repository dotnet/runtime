// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Mono.Cecil;

namespace Mono.Linker
{
	public class InterfaceImplementor
	{
		/// <summary>
		/// The type that implements <see cref="InterfaceImplementor.InterfaceType"/>.
		/// </summary>
		public TypeDefinition Implementor { get; }
		/// <summary>
		/// The .interfaceimpl on <see cref="InterfaceImplementor.Implementor"/>that points to <see cref="InterfaceImplementor.InterfaceType"/>
		/// </summary>
		public InterfaceImplementation InterfaceImplementation { get; }
		/// <summary>
		/// The type of the interface that is implemented by <see cref="InterfaceImplementor.Implementor"/>
		/// </summary>
		public TypeDefinition InterfaceType { get; }

		public InterfaceImplementor (TypeDefinition implementor, InterfaceImplementation interfaceImplementation, TypeDefinition interfaceType)
		{
			Implementor = implementor;
			InterfaceImplementation = interfaceImplementation;
			InterfaceType = interfaceType;
		}
	}
}
