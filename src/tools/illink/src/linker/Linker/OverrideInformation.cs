// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace Mono.Linker
{
	[DebuggerDisplay ("{Override}")]
	public class OverrideInformation
	{
		public MethodDefinition Base { get; }

		public MethodDefinition Override { get; }

		internal RuntimeInterfaceImplementation? RuntimeInterfaceImplementation { get; }

		internal OverrideInformation (MethodDefinition @base, MethodDefinition @override, RuntimeInterfaceImplementation? runtimeInterface = null)
		{
			Base = @base;
			Override = @override;
			RuntimeInterfaceImplementation = runtimeInterface;
			// Ensure we have an interface implementation if the base method is from an interface and the override method is on a class
			Debug.Assert (@base.DeclaringType.IsInterface && runtimeInterface != null
						|| !@base.DeclaringType.IsInterface && runtimeInterface == null);
			// Ensure the interfaceImplementor is for the interface we expect
			Debug.Assert (@base.DeclaringType.IsInterface ? runtimeInterface!.InterfaceTypeDefinition == @base.DeclaringType : true);
		}

		public InterfaceImplementation? MatchingInterfaceImplementation
			=> RuntimeInterfaceImplementation?.InterfaceImplementationChains[0].InterfaceImplementations[0];

		public TypeDefinition? InterfaceType
			=> RuntimeInterfaceImplementation?.InterfaceTypeDefinition;

		[MemberNotNullWhen (true, nameof (RuntimeInterfaceImplementation))]
		public bool IsOverrideOfInterfaceMember
			=> RuntimeInterfaceImplementation != null;
	}
}
