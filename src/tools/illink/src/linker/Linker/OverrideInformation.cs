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
		private InterfaceImplementation? _matchingInterfaceImplementation;

		public OverrideInformation (MethodDefinition @base, MethodDefinition @override, ITryResolveMetadata resolver, InterfaceImplementation? matchingInterfaceImplementation = null)
		{
			_pair = new OverridePair (@base, @override);
			_matchingInterfaceImplementation = matchingInterfaceImplementation;
			this.resolver = resolver;
		}
		public readonly record struct OverridePair (MethodDefinition Base, MethodDefinition Override)
		{
			public bool IsStaticInterfaceMethodPair () => Base.DeclaringType.IsInterface && Base.IsStatic && Override.IsStatic;
			public InterfaceImplementation? GetMatchingInterfaceImplementation (ITryResolveMetadata resolver)
			{
				if (!Base.DeclaringType.IsInterface)
					return null;
				var interfaceType = Base.DeclaringType;
				foreach (var @interface in Override.DeclaringType.Interfaces) {
					if (resolver.TryResolve (@interface.InterfaceType)?.Equals (interfaceType) == true) {
						return @interface;
					}
				}
				return null;
			}
		}

		public MethodDefinition Base { get => _pair.Base; }
		public MethodDefinition Override { get => _pair.Override; }
		public InterfaceImplementation? MatchingInterfaceImplementation {
			get {
				if (_matchingInterfaceImplementation is not null)
					return _matchingInterfaceImplementation;
				_matchingInterfaceImplementation = _pair.GetMatchingInterfaceImplementation (resolver);
				return _matchingInterfaceImplementation;
			}
		}

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
