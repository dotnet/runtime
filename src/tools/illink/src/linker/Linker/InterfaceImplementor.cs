// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Mono.Cecil;

namespace Mono.Linker
{
	internal class InterfaceImplementor
	{
		public TypeDefinition Implementor { get; }
		public InterfaceImplementation InterfaceImplementation { get; }
		public TypeDefinition? InterfaceType { get; }

		public InterfaceImplementor (TypeDefinition implementor, InterfaceImplementation interfaceImplementation, TypeDefinition? interfaceType)
		{
			Debug.Assert(implementor.Interfaces.Contains (interfaceImplementation));
			Implementor = implementor;
			InterfaceImplementation = interfaceImplementation;
			InterfaceType = interfaceType;
		}
	}
}
