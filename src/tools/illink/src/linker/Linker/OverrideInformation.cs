// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Mono.Cecil;
using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker
{
	[DebuggerDisplay ("{Override}")]
	public class OverrideInformation
	{
		/// <summary>
		/// The method that is being overridden or implemented
		/// </summary>
		public MethodDefinition Base { get; }

		/// <summary>
		/// The method that overrides <see cref="Base"/>. For interface methods, this may be a method on a base type, or a default interface method.
		/// </summary>
		public MethodDefinition Override { get; }

		/// <summary>
		/// For overrides of interface methods, includes info about the interface / implementor pair that correspong to the base / override pair
		/// The <see cref="Override"/> may be a method on <see cref="InterfaceImplementor.Implementor"/>, one of it's base types, or a default interface method on one of the interfaces implemented by it.
		/// </summary>
		internal InterfaceImplementor? InterfaceImplementor { get; }

		internal OverrideInformation (MethodDefinition @base, MethodDefinition @override, InterfaceImplementor? interfaceImplementor = null)
		{
			Base = @base;
			Override = @override;
			InterfaceImplementor = interfaceImplementor;
			// Ensure we have an interface implementation if the base method is from an interface and the override method is on a class
			Debug.Assert(@base.DeclaringType.IsInterface && interfaceImplementor != null
						|| !@base.DeclaringType.IsInterface && interfaceImplementor == null);
			// Ensure the interfaceImplementor is for the interface we expect
			Debug.Assert (@base.DeclaringType.IsInterface ? interfaceImplementor!.InterfaceType == @base.DeclaringType : true);
		}

		public InterfaceImplementation? MatchingInterfaceImplementation
			=> InterfaceImplementor?.InterfaceImplementations[^1];

		public TypeDefinition? InterfaceType
			=> InterfaceImplementor?.InterfaceType;

		[MemberNotNullWhen (true, nameof (InterfaceImplementor), nameof (MatchingInterfaceImplementation))]
		public bool IsOverrideOfInterfaceMember
			=> InterfaceImplementor != null;
	}
}
