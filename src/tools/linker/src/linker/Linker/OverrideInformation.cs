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
		readonly OverridePair _pair;

		public OverrideInformation (MethodDefinition @base, MethodDefinition @override, ITryResolveMetadata resolver, InterfaceImplementation? matchingInterfaceImplementation = null)
		{
			_pair = new OverridePair (@base, @override);
			MatchingInterfaceImplementation = matchingInterfaceImplementation;
			this.resolver = resolver;
		}

		public readonly record struct OverridePair (MethodDefinition Base, MethodDefinition Override)
		{
			public bool IsStaticInterfaceMethodPair () => Base.DeclaringType.IsInterface && Base.IsStatic && Override.IsStatic;
		}

		public MethodDefinition Base { get => _pair.Base; }
		public MethodDefinition Override { get => _pair.Override; }
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

		public bool IsStaticInterfaceMethodPair => _pair.IsStaticInterfaceMethodPair ();
	}
}
