// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Mono.Cecil;

namespace Mono.Linker
{
	[DebuggerDisplay ("{Override}")]
	public class OverrideInformation
	{
		readonly ITryResolveMetadata resolver;

		public OverrideInformation (MethodDefinition @base, MethodDefinition @override, ITryResolveMetadata resolver, InterfaceImplementation? matchingInterfaceImplementation = null)
		{
			Base = @base;
			Override = @override;
			MatchingInterfaceImplementation = matchingInterfaceImplementation;
			this.resolver = resolver;
		}

		public MethodDefinition Base { get; }
		public MethodDefinition Override { get; }
		public InterfaceImplementation? MatchingInterfaceImplementation { get; }

		public bool IsOverrideOfInterfaceMember {
			get {
				if (MatchingInterfaceImplementation != null)
					return true;

				return Base.DeclaringType.IsInterface;
			}
		}

		public TypeDefinition? InterfaceType {
			get {
				if (!IsOverrideOfInterfaceMember)
					return null;

				if (MatchingInterfaceImplementation != null)
					return resolver.TryResolve (MatchingInterfaceImplementation.InterfaceType);

				return Base.DeclaringType;
			}
		}
	}
}
