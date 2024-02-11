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
		public MethodDefinition Base { get; }

		public MethodDefinition Override { get; }

		public InterfaceImplementor? InterfaceImplementor { get; }

		public OverrideInformation (MethodDefinition @base, MethodDefinition @override, InterfaceImplementor? interfaceImplementor = null)
		{
			Base = @base;
			Override = @override;
			InterfaceImplementor = interfaceImplementor;
			// Ensure we have an interface implementation if the base method is from an interface and the override method is on a class
			Debug.Assert(@base.DeclaringType.IsInterface && interfaceImplementor != null
						|| !@base.DeclaringType.IsInterface && interfaceImplementor == null);
		}

		public InterfaceImplementation? MatchingInterfaceImplementation {
			get {
				return InterfaceImplementor?.InterfaceImplementation;
			}
		}

		[MemberNotNullWhen (true, nameof (InterfaceImplementor), nameof (MatchingInterfaceImplementation))]
		public bool IsOverrideOfInterfaceMember {
			get {
				return InterfaceImplementor != null;
			}
		}
	}
}
